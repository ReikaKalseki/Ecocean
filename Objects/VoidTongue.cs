using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

using FMOD;

using FMODUnity;

using ReikaKalseki.DIAlterra;

using SMLHelper.V2.Assets;
using SMLHelper.V2.Handlers;
using SMLHelper.V2.Utility;

using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.Serialization;
using UnityEngine.UI;

using Util = UWE.Utils;

namespace ReikaKalseki.Ecocean {

	public class VoidTongue : Spawnable {

		private readonly XMLLocale.LocaleEntry locale;

		public PDAManager.PDAPage pdaPage { get; private set; }

		internal VoidTongue(XMLLocale.LocaleEntry e) : base(e.key, e.name, e.desc) {
			locale = e;
		}

		public override GameObject GetGameObject() {
			GameObject world = ObjectUtil.createWorldObject("8914acde-168e-438f-9b2b-6b9332d8c1a1");
			world.removeComponent<HangingStinger>();
			world.removeComponent<LiveMixin>();
			world.removeComponent<WorldForces>();
			world.removeComponent<Collider>();
			world.removeComponent<Light>();
			world.EnsureComponent<TechTag>().type = TechType;
			world.EnsureComponent<PrefabIdentifier>().ClassId = ClassID;
			VoidTongueTag g = world.EnsureComponent<VoidTongueTag>();
			world.EnsureComponent<JointHelper>().jointType = ObjectUtil.lookupPrefab(VanillaCreatures.FLOATER.prefab).GetComponent<Floater>().jointHelper.jointType;
			world.EnsureComponent<LargeWorldEntity>().cellLevel = LargeWorldEntity.CellLevel.Global;
			Light l = world.addLight(3, 2, Color.white);
			world.layer = LayerID.Useable;
			foreach (Renderer r in world.GetComponentsInChildren<Renderer>()) {
				RenderUtil.swapTextures(EcoceanMod.modDLL, r, "Textures/VoidTongue/");
				r.material.SetFloat("_SpecInt", 2);
				r.material.SetFloat("_Shininess", 0);
				r.material.SetFloat("_Fresnel", 0);
				r.material.SetColor("_Color", new Color(1, 1, 1, 1));
				r.material.DisableKeyword("UWE_WAVING");
			}
			world.fullyEnable();
			return world;
		}

		public void register() {
			this.Patch();
			pdaPage = SNUtil.addPDAEntry(this, -1, "Lifeforms/Fauna/Leviathans", locale.pda, locale.getString("header"));
			ItemRegistry.instance.addItem(this);
		}

	}

	public class VoidTongueTag : MonoBehaviour {

		private Renderer mainRender;
		private Rigidbody mainBody;
		private JointHelper jointHelper;
		//private CapsuleCollider collider;
		private Animator animator;
		private Light light;

		private SonarScreenFX sonarShader;

		private Rigidbody stuckTo;
		private SubRoot stuckCyclops;
		private Creature stuckCreature;
		private FixedJoint joint;

		private Channel? currentPullingSound;

		private float length = 0.2F;

		private bool isGrabbing = false;
		private float willReleaseAtDepth = -1;
		private bool hasBeenUsed = false;

		private GameObject tip;

		private float currentSpeed = 0;

		private float age;
		private float nextCyclopsReleaseTime;

		private Vector3 relativePlayerCyclops = Vector3.zero;

		private static readonly SoundManager.SoundData grabSound = SoundManager.registerSound(EcoceanMod.modDLL, "tonguegrab", "Sounds/tonguegrab.ogg", SoundManager.soundMode3D, s => {SoundManager.setup3D(s, 120);}, SoundSystem.masterBus);
		private static readonly SoundManager.SoundData pullSound = SoundManager.registerSound(EcoceanMod.modDLL, "tonguepull", "Sounds/tonguepull.ogg", SoundManager.soundMode3D, s => {SoundManager.setup3D(s, 120);}, SoundSystem.masterBus);

		private static readonly float SPEED = 45;//50;//65;
		private static readonly float SPEED_CYCLOPS = 30;
		private static readonly float KILL_DEPTH = 1800;

		public static event Action<VoidTongueTag, Rigidbody> onVoidTongueGrabEvent;
		public static event Action<VoidTongueTag, Rigidbody> onVoidTongueReleaseEvent;
		public static event Action<VoidTongueTag, Rigidbody> voidTongueHeldTickEvent;

		void Update() {
			if (Camera.main && !sonarShader)
				sonarShader = Camera.main.GetComponent<SonarScreenFX>();

			if (!mainRender)
				mainRender = this.GetComponentInChildren<Renderer>();
			if (!mainBody)
				mainBody = this.GetComponentInChildren<Rigidbody>();
			if (!jointHelper)
				jointHelper = this.GetComponentInChildren<JointHelper>();/*
			if (!collider) {
				collider = GetComponentInChildren<CapsuleCollider>();
				collider.gameObject.EnsureComponent<VoidTongueGrabDetector>().tongue = this;
			}*/
			if (!tip) {
				tip = gameObject.getChildObject("TongueTip");
				if (!tip) {
					tip = new GameObject("TongueTip");
					tip.transform.SetParent(transform);
					tip.transform.localPosition = Vector3.zero;
					tip.EnsureComponent<VoidTongueGrabDetector>().tongue = this;
					light = tip.addLight();
					SphereCollider sc = tip.EnsureComponent<SphereCollider>();
					sc.radius = 1;
					sc.center = Vector3.zero;
					sc.isTrigger = true;
				}
			}
			if (!animator)
				animator = this.GetComponentInChildren<Animator>();

			if (joint && !joint.connectedBody) {
				this.Disconnect();
			}

			if (!jointHelper.enabled)
				gameObject.fullyEnable();

			if (animator.enabled) {
				animator.Rebind();
				animator.Update(2.85F);
				animator.enabled = false;
			}

			if (!Player.main || !VanillaBiomes.VOID.isInBiome(Player.main.transform.position)) {
				gameObject.destroy();
				return;
			}

			float time = DayNightCycle.main.timePassedAsFloat;
			if (mainBody) {
				mainBody.isKinematic = true;
			}
			float dT = Time.deltaTime;
			age += dT;

			if (age >= 30) {
				this.Disconnect();
				gameObject.destroy();
				return;
			}

			Transform tt = this.getCollider(this.getPriorityTarget());

			if (stuckTo) {
				if (voidTongueHeldTickEvent != null)
					voidTongueHeldTickEvent.Invoke(this, stuckTo);
				if (sonarShader)
					sonarShader.enabled = false;
				stuckTo.isKinematic = false;
				if (currentPullingSound == null) {
					currentPullingSound = SoundManager.playSoundAt(pullSound, stuckTo.transform.position, false, -1, 1);
				}
				if (currentPullingSound != null && currentPullingSound.Value.hasHandle()) {
					ATTRIBUTES_3D attr = stuckTo.transform.position.To3DAttributes();
					currentPullingSound.Value.set3DAttributes(ref attr.position, ref attr.velocity, ref attr.forward);
				}
				//float force = 100;
				//stuckTo.AddForce((transform.position-stuckTo.transform.position).normalized*dT*force, ForceMode.Acceleration);
				//SNUtil.writeToChat("pulling "+stuckTo+" at L="+length+", sp = "+currentSpeed+" towards "+transform.position);
				length -= dT * currentSpeed;
				if (length < 1)
					length = 1;
				//Vector3 playerRel = Player.main.transform.position-stuckTo.transform.position;
				if (stuckCyclops) {
					//stuckCyclops.live.TakeDamage(0.1F);
					stuckCyclops.EndSubShielded();
					//Player.main.transform.localPosition = relativePlayerCyclops;
					stuckTo.AddForce((transform.position - stuckTo.position).normalized * dT * 5000, ForceMode.Acceleration);
					length = (stuckTo.transform.position - transform.position).magnitude / 3F;
					if (time >= nextCyclopsReleaseTime) {
						willReleaseAtDepth = 0;
						//SNUtil.writeToChat("Queuing cyclops release @ "+time);
					}
				}
				else {
					//Quaternion targetRotation = Quaternion.LookRotation(transform.up, Vector3.up);//MathUtil.unitVecToRotation(-transform.up);
					//stuckTo.transform.localRotation = Quaternion.Lerp(stuckTo.transform.rotation, targetRotation, 0.02F);
					stuckTo.transform.LookAt(transform, Vector3.up);
					stuckTo.velocity = currentSpeed * transform.up.normalized;
					stuckTo.transform.position = tip.transform.position;
					CrushDamage ch = stuckTo.GetComponent<CrushDamage>();
					if (ch)
						ch.depthCache.currValue = -tip.transform.position.y;
				}
				//if (stuckCyclops && stuckCyclops == Player.main.currentSub)
				//Player.main.transform.position = stuckTo.transform.position+playerRel;
				if (stuckCreature)
					VoidGhostLeviathansSpawner.main.timeNextSpawn = Mathf.Max(Time.time + 90, VoidGhostLeviathansSpawner.main.timeNextSpawn);
				if (stuckTo.transform.position.y < Mathf.Min(-KILL_DEPTH, transform.position.y + 150)) {
					this.doKill();
				}
				else if (stuckTo.transform.position.y < -willReleaseAtDepth || stuckTo.transform.position.y > -600) {
					this.Disconnect();
					isGrabbing = false;
					currentSpeed = SPEED / 2;
					this.destroy(false, 1.5F);
				}
			}
			else if (isGrabbing) {
				length += dT * currentSpeed;/*
				if (tip.transform.position.y >= tt.transform.position.y) {
					grab(tt.GetComponentInParent<Rigidbody>());
				}
				else */
				if (length > 600 || (tt && tip.transform.position.y >= tt.transform.position.y + 100)) {
					isGrabbing = false;
					ECHooks.nextVoidTongueGrab = time + 1.5F;
				}
				else if (tt && !Util.GetComponentInHierarchy<SubRoot>(tt.gameObject)) {
					Quaternion targetRotation = Quaternion.LookRotation(transform.up, Vector3.up);//MathUtil.unitVecToRotation(-transform.up);
					tt.rotation = Quaternion.RotateTowards(tt.rotation, targetRotation, dT * 180);
				}
			}
			else {
				transform.position = transform.position + Vector3.down * 5;
				length -= dT * SPEED;
				if (length < 1)
					length = 1;
				if (hasBeenUsed)
					this.destroy(false, 1.5F);
			}

			currentSpeed = Mathf.Min(stuckCyclops ? SPEED_CYCLOPS : SPEED, (currentSpeed * 1.05F) + (dT * SPEED * 0.33F));

			Vector3 unitVec = (transform.position-tt.position).normalized;
			transform.rotation = MathUtil.unitVecToRotation(unitVec);

			animator.gameObject.SetActive(false);
			animator.transform.localScale = new Vector3(30, length, 30);
			animator.gameObject.SetActive(true);/*
			collider.center = Vector3.zero;
			collider.height = length;
			collider.radius = 10;*/
			tip.transform.position = transform.position + (transform.up * length * -3F);

			light.intensity = stuckTo ? 2F : 1F;
			light.range = 18;

			float delay = UnityEngine.Random.Range(8F, 15F);
			if (stuckCyclops) {
				delay = UnityEngine.Random.Range(2F, 10F);
			}
			else if (stuckCreature) {
				delay = UnityEngine.Random.Range(1F, 3F);
			}
			ECHooks.nextVoidTongueGrab = Mathf.Max(ECHooks.nextVoidTongueGrab, time + delay);
		}

		private void doKill() {
			if (stuckCyclops) {/*
				if (!stuckCyclops.subDestroyed) {
					stuckCyclops.PowerDownCyclops();
					stuckCyclops.DestroyCyclopsSubRoot();
					stuckCyclops.gameObject, 10.destroy(false);
				}
				if (stuckCyclops == Player.main.lastValidSub)
					Player.main.lastValidSub = null;*/
			}
			else {
				LiveMixin lv = stuckTo.GetComponentInChildren<LiveMixin>();
				if (lv) {
					lv.Kill();
					if (lv.GetComponent<Creature>()) {
						lv.gameObject.destroy(false, 2);
					}
				}
			}
			this.Disconnect();
			gameObject.destroy();
		}

		private Transform getCollider(Transform t) {/*
			SubRoot sub = Util.GetComponentInHierarchy<SubRoot>(t.gameObject);
			if (sub) {
				Collider[] c = t.GetComponentsInChildren<Collider>().Where(c2 => !c2.isTrigger && c2.GetComponentInParent<PrefabIdentifier>() == sub.GetComponent<PrefabIdentifier>()).ToArray();
				if (c == null || c.Length <= 0)
					return t;
				int idx = ((t.gameObject.GetInstanceID()%c.Length)+c.Length)%c.Length;
				return c[idx].transform;
			}*/
			//if (t.GetComponentInParent<Creature>())
			return t;
			//Collider c = t ? t.GetComponentInChildren<Collider>() : null;
			//return c ? c.transform : t;
		}

		private Transform getPriorityTarget() {
			if (stuckTo)
				return stuckTo.transform;
			Player ep = Player.main;
			foreach (GameObject go in VoidGhostLeviathansSpawner.main.spawnedCreatures) {
				if (go && go.activeInHierarchy && go.GetComponent<LiveMixin>().IsAlive() && Vector3.Distance(ep.transform.position, go.transform.position) <= 120)
					return go.transform;
			}
			if (ep.transform.position.y <= -1800)
				return ep.transform;
			foreach (SubRoot sub in UnityEngine.Object.FindObjectsOfType<SubRoot>()) {
				if (sub && sub.gameObject.activeInHierarchy && sub.isCyclops && !sub.subDestroyed && VanillaBiomes.VOID.isInBiome(sub.transform.position))
					return sub.transform;
			}
			foreach (Vehicle v in UnityEngine.Object.FindObjectsOfType<Vehicle>()) {
				if (v && v.gameObject.activeInHierarchy && v.liveMixin.IsAlive() && VanillaBiomes.VOID.isInBiome(v.transform.position))
					return v.transform;
			}
			/*
			if (ep.currentSub && ep.currentSub.isCyclops)
				return ep.currentSub.transform;
			Vehicle v = ep.GetVehicle();
			if (v)
				return v.transform;*/
			return ep.transform;
		}

		public void startGrab(float releaseAt) {
			if (hasBeenUsed)
				return;
			length = 0.2F;
			isGrabbing = true;
			currentSpeed = SPEED / 2;
			willReleaseAtDepth = releaseAt;
			hasBeenUsed = true;
			//SNUtil.writeToChat("Grab from "+transform.position+", will release at "+(-releaseAt).ToString("0000.0"));
			//if (releaseAt > -transform.position.y)
			//	SNUtil.writeToChat("RELEASING TOO LOW");
		}

		public bool isStuckTo(Rigidbody rb) {
			return stuckTo == rb;
		}

		public void Disconnect() {
			if (stuckTo != null) {
				if (onVoidTongueReleaseEvent != null)
					onVoidTongueReleaseEvent.Invoke(this, stuckTo);
				//SNUtil.writeToChat("Releasing "+stuckTo);
				CyclopsHolographicHUD hud = stuckTo.GetComponentInChildren<CyclopsHolographicHUD>();
				if (hud) {
					hud.DetachedLavaLarva(tip);
				}
				if (joint != null)
					JointHelper.Disconnect(joint, false);
				if (currentPullingSound != null && currentPullingSound.Value.hasHandle())
					currentPullingSound.Value.stop();
				joint = null;
				stuckTo = null;
				stuckCyclops = null;
				stuckCreature = null;
			}
		}

		private void OnJointConnected(FixedJoint j) {
			joint = j;
		}

		private void FindConnectedJoint() {
			FixedJoint component = base.GetComponent<FixedJoint>();
			if (!component)
				return;
			joint = component;
		}

		public void grab(Rigidbody rb) {
			if (stuckTo || !isGrabbing)
				return;
			if (rb) {
				Player p = rb.GetComponentInChildren<Player>();
				bool pda = p;
				if (p)
					p.liveMixin.TakeDamage(15, p.transform.position, DamageType.Normal, gameObject);
				//SNUtil.writeToChat("Stick to "+rb);
				/*
				Vector3 rel = transform.position-rb.transform.position;
				mainBody.velocity = rb.velocity;
				transform.SetParent(rb.transform);
				transform.localPosition = rel;*/
				currentSpeed = 0;
				stuckTo = rb;
				stuckCyclops = Util.GetComponentInHierarchy<SubRoot>(rb.gameObject);
				stuckCreature = Util.GetComponentInHierarchy<Creature>(rb.gameObject);
				Vehicle v = Util.GetComponentInHierarchy<Vehicle>(rb.gameObject);
				//SNUtil.writeToChat("V: "+v);
				SoundManager.playSoundAt(grabSound, rb.transform.position, false, -1, 2);
				if (stuckCyclops) {
					stuckCyclops.EndSubShielded();
					SoundManager.playSoundAt(SoundManager.buildSound("event:/sub/cyclops/impact_solid_hard"), rb.transform.position);
					//stuckCyclops.voiceNotificationManager.PlayVoiceNotification(stuckCyclops.creatureAttackNotification, false, true);
					//stuckCyclops.damageManager.NotifyAllOfDamage();
					stuckCyclops.GetComponentInChildren<CyclopsHelmHUDManager>().OnTakeCreatureDamage();
					if (stuckCyclops == Player.main.currentSub) {
						//relativePlayerCyclops = Player.main.transform.position-stuckCyclops.transform.position;
						//Player.main.transform.SetParent(stuckCyclops.transform);
						pda = true;
					}
					nextCyclopsReleaseTime = DayNightCycle.main.timePassedAsFloat + UnityEngine.Random.Range(1F, 1.5F);
					//SNUtil.writeToChat("Time is "+DayNightCycle.main.timePassedAsFloat+" will release at "+nextCyclopsReleaseTime);
				}
				else if (stuckCreature) {
					willReleaseAtDepth = 9999;
				}
				else if (v) {
					SoundManager.playSoundAt(SoundManager.buildSound("event:/sub/seamoth/impact_solid_hard"), rb.transform.position);
					v.liveMixin.TakeDamage(30, v.transform.position, DamageType.Collide, gameObject);
					pda = v == Player.main.GetVehicle();
				}
				ReikaKalseki.DIAlterra.SNUtil.log("", ReikaKalseki.DIAlterra.SNUtil.diDLL);
				if (!stuckCyclops)
					JointHelper.ConnectFixed(jointHelper, rb);
				ObjectUtil.addCyclopsHologramWarning(rb, tip, Sprite.Create(TextureManager.getTexture(EcoceanMod.modDLL, "Textures/CyclopsVoidTongueIcon"), new Rect(0, 0, 100, 100), new Vector2(0, 0)));
				if (pda)
					PDAManager.getPage("ency_VoidTongue").unlock();
				if (onVoidTongueGrabEvent != null)
					onVoidTongueGrabEvent.Invoke(this, rb);
			}
		}

		void OnDestroy() {

		}

		void OnDisable() {
			gameObject.destroy(false);
		}

		private void burst() {
			gameObject.destroy(false);
		}

	}

	class VoidTongueGrabDetector : MonoBehaviour {

		internal VoidTongueTag tongue;

		private SphereCollider collider;

		void Update() {
			if (!collider) {
				collider = this.GetComponentInChildren<SphereCollider>();
			}
			collider.center = Vector3.zero;
		}

		public void OnTriggerEnter(Collider other) {
			if (!other)
				return;
			Component hit = other.gameObject.FindAncestor<Player>();
			if (!hit)
				hit = other.gameObject.FindAncestor<Creature>();
			if (!hit)
				hit = other.gameObject.FindAncestor<Vehicle>();
			if (!hit) {
				hit = other.gameObject.FindAncestor<SubRoot>();
				if (hit && !((SubRoot)hit).isCyclops)
					hit = null;
			}
			Rigidbody rb = hit ? hit.GetComponentInChildren<Rigidbody>() : null;
			//SNUtil.writeToChat("grab "+hit);
			tongue.grab(rb);
		}

	}
}
