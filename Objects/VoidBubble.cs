using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

using ReikaKalseki.DIAlterra;

using SMLHelper.V2.Assets;
using SMLHelper.V2.Handlers;
using SMLHelper.V2.Utility;

using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace ReikaKalseki.Ecocean {

	public class VoidBubble : Spawnable {

		private readonly XMLLocale.LocaleEntry locale;

		internal static readonly Color COLOR = new Color(0.25F, 0F, 1F, 1);
		internal static readonly Color COLOR_ATTACHED = new Color(0.75F, 0.5F, 1F, 1);

		internal static readonly Simplex3DGenerator densityNoise = (Simplex3DGenerator)new Simplex3DGenerator(483181).setFrequency(0.005);

		public static event Action<VoidBubbleSpawnerTick> voidBubbleSpawnerTickEvent;
		public static event Action<VoidBubbleTag> voidBubbleTickEvent;

		private float lastIterateTime = -1;

		internal VoidBubble(XMLLocale.LocaleEntry e) : base(e.key, e.name, e.desc) {
			locale = e;
		}

		public override GameObject GetGameObject() {
			GameObject world = getBubbleSource();
			world.removeComponent<Bubble>();
			//world.removeComponent<LiveMixin>();
			//world.removeComponent<WorldForces>();
			//LiveMixin lv = world.EnsureComponent<LiveMixin>();
			//lv.data.knifeable = true;
			//lv.data.maxHealth = 40;
			world.EnsureComponent<TechTag>().type = TechType;
			world.EnsureComponent<PrefabIdentifier>().ClassId = ClassID;
			VoidBubbleTag g = world.EnsureComponent<VoidBubbleTag>();
			SphereCollider sc = world.GetComponentInChildren<SphereCollider>();
			sc.radius *= 0.5F;
			world.EnsureComponent<JointHelper>().jointType = ObjectUtil.lookupPrefab(VanillaCreatures.FLOATER.prefab).GetComponent<Floater>().jointHelper.jointType;
			world.EnsureComponent<LargeWorldEntity>().cellLevel = LargeWorldEntity.CellLevel.VeryFar;
			world.layer = LayerID.Useable;
			Light l = world.addLight(3, 24, COLOR);
			Renderer r = world.GetComponentInChildren<Renderer>();
			r.material.SetFloat("_SpecInt", 12);
			r.material.SetFloat("_Shininess", 6);
			r.material.SetFloat("_Fresnel", 0);
			r.material.SetColor("_Color", new Color(1, 1, 1, 1));
			r.material.SetColor("_SpecColor", COLOR);
			r.material.SetVector("_Scale", new Vector4(0.06F, 0.06F, 0.06F, 0.06F));
			r.material.SetVector("_Frequency", new Vector4(8.0F, 8.0F, 8.0F, 8.0F));
			r.material.SetVector("_Speed", new Vector4(0.05F, 0.05F, 0.05F, 0.05F));
			r.material.SetVector("_ObjectUp", new Vector4(0F, 0F, 1F, 0F));
			return world;
		}

		internal static GameObject getBubbleSource() {
			GameObject coral = ObjectUtil.lookupPrefab("171c6a5b-879b-4785-be7a-6584b2c8c442");
			IntermittentInstantiate ii = coral.GetComponent<IntermittentInstantiate>();
			return UnityEngine.Object.Instantiate(ii.prefab);
		}

		public void register() {
			this.Patch();
			SNUtil.addPDAEntry(this, 1.5F, "PlanetaryGeology", locale.pda, locale.getField<string>("header"));
			ItemRegistry.instance.addItem(this);
		}

		internal static void tickBubble(VoidBubbleTag b) {
			if (voidBubbleTickEvent != null)
				voidBubbleTickEvent.Invoke(b);
		}

		internal void tickSpawner(Player ep, float time, float dT) {
			if (time - lastIterateTime >= 5) {
				lastIterateTime = time;
				foreach (VoidBubbleTag tag in WorldUtil.getObjectsNearWithComponent<VoidBubbleTag>(ep.transform.position, 250))
					tag.gameObject.fullyEnable();
			}
			Vector3 ctr = ep.transform.position;
			float f = (float)(1+densityNoise.getValue(ctr))*0.04F;
			float depth = -ctr.y;
			if (depth > 250) {
				f = Mathf.Max(-0.04F, f - ((depth - 250F) / 500F * 0.08F));
			}
			if (voidBubbleTickEvent != null) {
				VoidBubbleSpawnerTick tick = new VoidBubbleSpawnerTick(ep, f);
				voidBubbleSpawnerTickEvent.Invoke(tick);
				f = tick.spawnChance;
			}
			//SNUtil.writeToChat(densityNoise.getValue(ctr).ToString("0.000")+" > "+f.ToString("0.000")+" > "+(0.94F+f).ToString("0.000"));
			if (UnityEngine.Random.Range(0F, 1F) < 0.94F + f)
				return;
			Vector3 pos = MathUtil.getRandomVectorAround(ctr, 90).setY(ctr.y-140+UnityEngine.Random.Range(0F, 30F));
			if (VanillaBiomes.VOID.isInBiome(pos) && (pos.y >= -50 || VanillaBiomes.VOID.isInBiome(pos + (Vector3.up * 50)))) {
				GameObject go = ObjectUtil.createWorldObject(ClassID);
				go.transform.position = pos;
				//go.transform.localScale = new Vector3(UnityEngine.Random.Range(2F, 3F), UnityEngine.Random.Range(2F, 3F), UnityEngine.Random.Range(2F, 3F));
				go.fullyEnable();
			}
		}

	}

	public class VoidBubbleSpawnerTick {

		public readonly Player player;
		public readonly float originalChance;

		public float spawnChance;

		internal VoidBubbleSpawnerTick(Player ep, float c) {
			player = ep;
			originalChance = c;
			spawnChance = c;
		}

	}

	public class VoidBubbleTag : MonoBehaviour {

		private Renderer mainRender;
		private Rigidbody mainBody;
		private WorldForces forces;
		private JointHelper jointHelper;
		private Light light;

		private GameObject inner;
		private Renderer innerRender;

		private float velocity = UnityEngine.Random.Range(7.5F, 12F);

		private readonly SimplexNoiseGenerator xSize = (SimplexNoiseGenerator)new Simplex1DGenerator(UnityEngine.Random.Range(0, 999999)).setFrequency(0.5F);
		private readonly SimplexNoiseGenerator ySize = (SimplexNoiseGenerator)new Simplex1DGenerator(UnityEngine.Random.Range(0, 999999)).setFrequency(0.5F);
		private readonly SimplexNoiseGenerator zSize = (SimplexNoiseGenerator)new Simplex1DGenerator(UnityEngine.Random.Range(0, 999999)).setFrequency(0.5F);

		private readonly Vector3 rotation = UnityEngine.Random.onUnitSphere*UnityEngine.Random.Range(0F, 3F);

		private Rigidbody stuckTo;
		private FixedJoint joint;

		private float attachCooldown = 0;
		private float attachFade = 0;
		private float fadingTime = -1;
		private float fadeDuration = -1;

		private float age;

		void Update() {
			if (!mainRender)
				mainRender = this.GetComponentInChildren<Renderer>();
			if (!mainBody)
				mainBody = this.GetComponentInChildren<Rigidbody>();
			if (!jointHelper)
				jointHelper = this.GetComponentInChildren<JointHelper>();
			if (!forces) {
				forces = this.GetComponentInChildren<WorldForces>();
				forces.underwaterGravity = 2;
			}
			if (!light)
				light = this.GetComponentInChildren<Light>();
			if (!inner) {
				inner = gameObject.getChildObject("InnerShine");
				if (!inner) {
					inner = VoidBubble.getBubbleSource().setName("InnerShine");
					inner.convertToModel();
					inner.transform.SetParent(transform);
					inner.transform.localPosition = Vector3.zero;
					innerRender = inner.GetComponentInChildren<Renderer>();
					innerRender.material.SetFloat("_SpecInt", 24);
					innerRender.material.SetFloat("_Shininess", 4.5F);
					innerRender.material.SetFloat("_Fresnel", 0);
					innerRender.material.SetColor("_Color", new Color(1, 1, 1, 1));
					innerRender.material.SetColor("_SpecColor", Color.Lerp(VoidBubble.COLOR, Color.white, 0.5F));
					innerRender.material.SetVector("_Scale", new Vector4(0.04F, 0.04F, 0.04F, 0.04F));
					innerRender.material.SetVector("_Frequency", new Vector4(8.0F, 8.0F, 8.0F, 8.0F));
					innerRender.material.SetVector("_Speed", new Vector4(0.05F, 0.05F, 0.05F, 0.05F));
					innerRender.material.SetVector("_ObjectUp", new Vector4(0F, 0F, 1F, 0F));
				}
			}

			if (joint && !joint.connectedBody) {
				this.Disconnect();
			}

			VoidBubble.tickBubble(this);

			float time = DayNightCycle.main.timePassedAsFloat;/*
			forces.underwaterGravity *= 10F;
			if (!mainBody.velocity.HasAnyNaNs() && !mainBody.velocity.HasAnyInfs())
				mainBody.velocity = mainBody.velocity.setY(Mathf.Min(mainBody.velocity.y, 3.5F));
				*/
			forces.enabled = stuckTo;
			if (mainBody) {
				mainBody.isKinematic = false;
				if (stuckTo) {
					mainBody.angularVelocity = Vector3.zero;
				}
				else {
					mainBody.velocity = Vector3.up * velocity;
					mainBody.angularVelocity = rotation;
				}
			}
			float dT = Time.deltaTime;
			if (stuckTo) {
				stuckTo.isKinematic = false;
				float force = stuckTo.GetComponent<SubRoot>() ? 20 : 30;
				stuckTo.AddForce(Vector3.down * dT * force, ForceMode.Acceleration);
				//SNUtil.writeToChat("parented to "+transform.parent+" of "+stuckTo);
				attachFade = Mathf.Min(1, attachFade + (1.5F * dT));
			}
			else {
				attachFade = Mathf.Max(0, attachFade - (0.5F * dT));
			}

			Color color = Color.Lerp(VoidBubble.COLOR, VoidBubble.COLOR_ATTACHED, attachFade);

			age += dT;
			if (attachCooldown > 0)
				attachCooldown -= dT;

			Vector3 vec = new Vector3(age, 0, 0);
			float depth = Ocean.main ? Ocean.main.GetOceanLevel()-0.8F-transform.position.y : -1000;
			float f = depth > 20 ? 1 : depth/20F;
			if (stuckTo)
				f *= 0.8F;
			if (fadeDuration > 0)
				f *= fadingTime / fadeDuration;
			f *= Mathf.Lerp(1, 0.5F, attachFade);
			transform.localScale = new Vector3(this.evalSizeNoise(xSize, vec), this.evalSizeNoise(ySize, vec), this.evalSizeNoise(zSize, vec)) * f;
			inner.transform.localPosition = Vector3.zero;
			inner.transform.localScale = transform.localScale * Mathf.Lerp(0.25F, 0.67F, attachFade);
			innerRender.material.SetColor("_SpecColor", Color.Lerp(color, Color.white, 0.5F));
			innerRender.material.SetFloat("_SpecInt", Mathf.Lerp(24, 6, attachFade));

			if (age > 1) {
				if (depth <= 0)
					this.burst();
			}
			if (fadingTime >= 0) {
				fadingTime -= dT;
				if (fadingTime <= 0)
					this.burst();
			}

			mainRender.material.SetColor("_SpecColor", color);
			light.intensity = transform.localScale.magnitude * (stuckTo ? 0.67F : 1F);
			light.range = stuckTo ? 16 : 24;
			light.color = color;

			if ((transform.position.y < -50 && !VanillaBiomes.VOID.isInBiome(transform.position + (Vector3.up * 50))) || UWE.Utils.RaycastIntoSharedBuffer(new Ray(transform.position, Vector3.up), 12, Voxeland.GetTerrainLayerMask()) > 0)
				this.burst();
		}

		public void fade(float time) {
			if (fadeDuration > 0) //do not jump fade forward if called again, nor reset it
				return;
			fadingTime = time;
			fadeDuration = time;
		}

		public bool isStuckTo(Rigidbody rb) {
			return stuckTo == rb;
		}

		public void Disconnect() {
			if (stuckTo != null) {
				CyclopsHolographicHUD hud = stuckTo.GetComponentInChildren<CyclopsHolographicHUD>();
				if (hud) {
					hud.DetachedLavaLarva(gameObject);
				}
				if (joint != null)
					JointHelper.Disconnect(joint, false);
				joint = null;
				stuckTo = null;
				attachCooldown = 1.5F;
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

		private float evalSizeNoise(NoiseGeneratorBase gen, Vector3 vec) {
			double val = gen.getValue(vec);
			return 2.25F + (float)(0.25 * val);
		}

		public void OnCollisionEnter(Collision c) {
			if (stuckTo || attachCooldown > 0)
				return;
			Component hit = c.collider.gameObject.FindAncestor<Player>();
			if (!hit)
				hit = c.collider.gameObject.FindAncestor<Vehicle>();
			//if (!hit)
			//	hit = c.collider.gameObject.FindAncestor<Creature>();
			if (!hit)
				hit = c.collider.gameObject.FindAncestor<SubRoot>();

			Rigidbody rb = hit ? hit.GetComponentInChildren<Rigidbody>() : null;
			if (rb) {
				//SNUtil.writeToChat("Stick to "+rb);
				/*
				Vector3 rel = transform.position-rb.transform.position;
				mainBody.velocity = rb.velocity;
				transform.SetParent(rb.transform);
				transform.localPosition = rel;*/
				stuckTo = rb;
				JointHelper.ConnectFixed(jointHelper, rb);/*
				CyclopsHolographicHUD hud = rb.GetComponentInChildren<CyclopsHolographicHUD>();
				if (hud) {
					hud.AttachedLavaLarva(gameObject);
					foreach (CyclopsHolographicHUD_WarningPings ping in rb.GetComponentsInChildren<CyclopsHolographicHUD_WarningPings>()) {
						if (ping.name.Contains("Larva")) {
							ping.warningPing.GetComponentInChildren<Image>().sprite = Sprite.Create(ReikaKalseki.DIAlterra.TextureManager.getTexture(ReikaKalseki.Ecocean.EcoceanMod.modDLL, "Textures/CyclopsVoidBubbleIcon"), new Rect (0, 0, 100, 100), new Vector2(0, 0));
						}
					}
				}*/
				ObjectUtil.addCyclopsHologramWarning(rb, gameObject, Sprite.Create(TextureManager.getTexture(EcoceanMod.modDLL, "Textures/CyclopsVoidBubbleIcon"), new Rect(0, 0, 100, 100), new Vector2(0, 0)));
			}
			else {
				VoidBubbleReaction r = c.collider.GetComponentInParent<VoidBubbleReaction>();
				if (r != null)
					r.onVoidBubbleTouch(this);
			}
		}

		void OnDestroy() {
			gameObject.destroy(false);
		}

		void OnDisable() {
			gameObject.destroy(false);
		}

		private void burst() {
			gameObject.destroy(false);
		}

	}

	public interface VoidBubbleReaction {

		void onVoidBubbleTouch(VoidBubbleTag tag);

	}
}
