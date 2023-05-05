using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Scripting;
using UnityEngine.UI;
using System.Collections.Generic;
using ReikaKalseki.DIAlterra;
using SMLHelper.V2.Handlers;
using SMLHelper.V2.Utility;
using SMLHelper.V2.Assets;

namespace ReikaKalseki.Ecocean {
	
	public class VoidBubble : Spawnable {
		
		private readonly XMLLocale.LocaleEntry locale;
		
		internal static readonly Color COLOR = new Color(0.25F, 0F, 1F, 1);
		
		internal static readonly Simplex3DGenerator densityNoise = (Simplex3DGenerator)new Simplex3DGenerator(483181).setFrequency(0.005);
		
		private float lastIterateTime = -1;
	        
	    internal VoidBubble(XMLLocale.LocaleEntry e) : base(e.key, e.name, e.desc) {
			locale = e;
	    }
			
	    public override GameObject GetGameObject() {
			GameObject world = getBubbleSource();
			ObjectUtil.removeComponent<Bubble>(world);
			//ObjectUtil.removeComponent<LiveMixin>(world);
			//ObjectUtil.removeComponent<WorldForces>(world);
			//LiveMixin lv = world.EnsureComponent<LiveMixin>();
			//lv.data.knifeable = true;
			//lv.data.maxHealth = 40;
			world.EnsureComponent<TechTag>().type = TechType;
			world.EnsureComponent<PrefabIdentifier>().ClassId = ClassID;
			VoidBubbleTag g = world.EnsureComponent<VoidBubbleTag>();
			SphereCollider sc = world.GetComponentInChildren<SphereCollider>();
			sc.radius = sc.radius*0.5F;
			world.EnsureComponent<JointHelper>().jointType = ObjectUtil.lookupPrefab(VanillaCreatures.FLOATER.prefab).GetComponent<Floater>().jointHelper.jointType;
			world.EnsureComponent<LargeWorldEntity>().cellLevel = LargeWorldEntity.CellLevel.VeryFar;
			world.layer = LayerID.Useable;
			Light l = ObjectUtil.addLight(world);
			l.intensity = 3;
			l.range = 24;
			l.color = COLOR;
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
			Patch();
			SNUtil.addPDAEntry(this, 1.5F, "PlanetaryGeology", locale.pda, locale.getField<string>("header"));
			ItemRegistry.instance.addItem(this);
		}
		
		internal void tickSpawner(Player ep, float time, float dT) {
			if (time-lastIterateTime >= 5) {
				lastIterateTime = time;
				foreach (VoidBubbleTag tag in WorldUtil.getObjectsNearWithComponent<VoidBubbleTag>(ep.transform.position, 250))
					ObjectUtil.fullyEnable(tag.gameObject);
			}
			Vector3 ctr = ep.transform.position;
			float f = (float)(1+densityNoise.getValue(ctr))*0.04F;
			float depth = -ctr.y;
			if (depth > 250) {
				f = Mathf.Max(-0.04F, f-(depth-250F)/500F*0.08F);
			}
			//SNUtil.writeToChat(densityNoise.getValue(ctr).ToString("0.000")+" > "+f.ToString("0.000")+" > "+(0.94F+f).ToString("0.000"));
			if (UnityEngine.Random.Range(0F, 1F) < 0.94F+f)
				return;
			Vector3 pos = MathUtil.getRandomVectorAround(ctr, 90).setY(ctr.y-140+UnityEngine.Random.Range(0F, 30F));
			if (VanillaBiomes.VOID.isInBiome(pos)) {
				GameObject go = ObjectUtil.createWorldObject(ClassID);
				go.transform.position = pos;
				//go.transform.localScale = new Vector3(UnityEngine.Random.Range(2F, 3F), UnityEngine.Random.Range(2F, 3F), UnityEngine.Random.Range(2F, 3F));
				ObjectUtil.fullyEnable(go);
			}
		}
			
	}
		
	public class VoidBubbleTag : MonoBehaviour {
		
		private Renderer mainRender;
		private Rigidbody mainBody;
		private WorldForces forces;
		private JointHelper jointHelper;
		private Light light;
		
		private GameObject inner;
		
		private float velocity = UnityEngine.Random.Range(7.5F, 12F);
		
		private readonly SimplexNoiseGenerator xSize = (SimplexNoiseGenerator)new Simplex1DGenerator(UnityEngine.Random.Range(0, 999999)).setFrequency(0.5F);
		private readonly SimplexNoiseGenerator ySize = (SimplexNoiseGenerator)new Simplex1DGenerator(UnityEngine.Random.Range(0, 999999)).setFrequency(0.5F);
		private readonly SimplexNoiseGenerator zSize = (SimplexNoiseGenerator)new Simplex1DGenerator(UnityEngine.Random.Range(0, 999999)).setFrequency(0.5F);
		
		private Rigidbody stuckTo;
		private FixedJoint joint;
		
		private float attachCooldown = 0;
		
		private float age;
		
		void Update() {
			if (!mainRender)
				mainRender = GetComponentInChildren<Renderer>();
			if (!mainBody)
				mainBody = GetComponentInChildren<Rigidbody>();
			if (!jointHelper)
				jointHelper = GetComponentInChildren<JointHelper>();
			if (!forces) {
				forces = GetComponentInChildren<WorldForces>();
				forces.underwaterGravity = 2;
			}
			if (!light)
				light = GetComponentInChildren<Light>();
			if (!inner) {
				inner = ObjectUtil.getChildObject(gameObject, "InnerShine");
				if (!inner) {
					inner = VoidBubble.getBubbleSource();
					RenderUtil.convertToModel(inner);
					inner.name = "InnerShine";
					inner.transform.SetParent(transform);
					inner.transform.localPosition = Vector3.zero;
					Renderer r = inner.GetComponentInChildren<Renderer>();
					r.material.SetFloat("_SpecInt", 24);
					r.material.SetFloat("_Shininess", 4.5F);
					r.material.SetFloat("_Fresnel", 0);
					r.material.SetColor("_Color", new Color(1, 1, 1, 1));
					r.material.SetColor("_SpecColor", Color.Lerp(VoidBubble.COLOR, Color.white, 0.5F));
					r.material.SetVector("_Scale", new Vector4(0.04F, 0.04F, 0.04F, 0.04F));
					r.material.SetVector("_Frequency", new Vector4(8.0F, 8.0F, 8.0F, 8.0F));
					r.material.SetVector("_Speed", new Vector4(0.05F, 0.05F, 0.05F, 0.05F));
					r.material.SetVector("_ObjectUp", new Vector4(0F, 0F, 1F, 0F));
				}
			}
			
			if (joint && !joint.connectedBody) {
				Disconnect();
			}
			
			float time = DayNightCycle.main.timePassedAsFloat;/*
			forces.underwaterGravity *= 10F;
			if (!mainBody.velocity.HasAnyNaNs() && !mainBody.velocity.HasAnyInfs())
				mainBody.velocity = mainBody.velocity.setY(Mathf.Min(mainBody.velocity.y, 3.5F));
				*/
			forces.enabled = stuckTo;
			if (mainBody) {
				mainBody.isKinematic = false;
				if (!stuckTo)
					mainBody.velocity = Vector3.up*velocity;
			}
			float dT = Time.deltaTime;
			if (stuckTo) {
				stuckTo.isKinematic = false;
				float force = stuckTo.GetComponent<SubRoot>() ? 20 : 30;
				stuckTo.AddForce(Vector3.down*dT*force, ForceMode.Acceleration);
				//SNUtil.writeToChat("parented to "+transform.parent+" of "+stuckTo);
			}
				
			age += dT;
			if (attachCooldown > 0)
				attachCooldown -= dT;
			
			Vector3 vec = new Vector3(age, 0, 0);
			float depth = Ocean.main ? (Ocean.main.GetOceanLevel()-0.8F)-transform.position.y : -1000;
			float f = depth > 20 ? 1 : depth/20F;
			if (stuckTo)
				f *= 0.8F;
			transform.localScale = new Vector3(evalSizeNoise(xSize, vec), evalSizeNoise(ySize, vec), evalSizeNoise(zSize, vec))*f;
			inner.transform.localPosition = Vector3.zero;
			inner.transform.localScale = transform.localScale*0.25F;
			
			if (age > 1) {
				if (depth <= 0)
					burst();
			}
			
			light.intensity = transform.localScale.magnitude*(stuckTo ? 0.67F : 1F);
			light.range = stuckTo ? 16 : 24;
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
			return 2.25F+(float)(0.25*val);
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
				ObjectUtil.addCyclopsHologramWarning(rb, gameObject, Sprite.Create(TextureManager.getTexture(EcoceanMod.modDLL, "Textures/CyclopsVoidBubbleIcon"), new Rect (0, 0, 100, 100), new Vector2(0, 0)));
			}
		}
		
		void OnDestroy() {
			UnityEngine.Object.Destroy(gameObject);
		}
		
		void OnDisable() {
			UnityEngine.Object.Destroy(gameObject);
		}
		
		private void burst() {
			UnityEngine.Object.Destroy(gameObject);
		}
		
	}
}
