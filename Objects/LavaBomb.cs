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
	
	public class LavaBomb : Spawnable {
		
		internal static readonly float HEAT_RADIUS = 60;
		internal static readonly float MAX_TEMPERATURE = 1500;
		
		internal static List<LavaBombTag> activeLavaBombs = new List<LavaBombTag>();
		
		private readonly XMLLocale.LocaleEntry locale;
	        
	    internal LavaBomb(XMLLocale.LocaleEntry e) : base(e.key, e.name, e.desc) {
			locale = e;
			OnFinishedPatching += () => {
				SaveSystem.addSaveHandler(ClassID, new SaveSystem.ComponentFieldSaveHandler<LavaBombTag>().addField("temperature").addField("spawnTime"));
			};
	    }
		
		public static void iterateLavaBombs(Action<LavaBombTag> a) {
			foreach (LavaBombTag go in activeLavaBombs) {
				if (go)
					a(go);
			}
		}
			
	    public override GameObject GetGameObject() {
			GameObject world = ObjectUtil.createWorldObject("18229b4b-3ed3-4b35-ae30-43b1c31a6d8d");
			world.EnsureComponent<TechTag>().type = TechType;
			world.EnsureComponent<PrefabIdentifier>().ClassId = ClassID;
			//world.GetComponent<Rigidbody>().isKinematic = true;
			WorldForces wf = world.GetComponent<WorldForces>();
			wf.underwaterGravity *= 2.5F;
			wf.underwaterDrag *= 0.5F;
			Rigidbody rb = world.GetComponent<Rigidbody>();
			rb.maxAngularVelocity = 0;
			//ObjectUtil.removeComponent<EnzymeBall>(world);
			ObjectUtil.removeComponent<Plantable>(world);
			ObjectUtil.removeComponent<Pickupable>(world);
			ObjectUtil.removeComponent<PickPrefab>(world);
			ObjectUtil.removeComponent<ResourceTracker>(world);
			ObjectUtil.removeComponent<ResourceTrackerUpdater>(world);
			world.EnsureComponent<LargeWorldEntity>().cellLevel = LargeWorldEntity.CellLevel.Far;
			LavaBombTag g = world.EnsureComponent<LavaBombTag>();
			Light l = ObjectUtil.addLight(world);
			l.bounceIntensity *= 3;
			l.color = new Color(1F, 0.67F, 0.1F, 1F);
			l.intensity = 3;
			l.range = 16;
			world.GetComponentInChildren<SphereCollider>().radius *= 0.8F;
			Renderer r = world.GetComponentInChildren<Renderer>();
			RenderUtil.swapTextures(EcoceanMod.modDLL, r, "Textures/LavaBomb/", new Dictionary<int, string>{{0, "Shell"}, {1, "Inner"}});
			r.materials[0].SetFloat("_SpecInt", 50);
			r.materials[0].SetFloat("_Shininess", 0);
			r.materials[0].SetFloat("_Fresnel", 1);
			r.materials[0].SetColor("_Color", new Color(1, 1, 1, 1));
			r.materials[0].EnableKeyword("FX_KELP");
			r.materials[0].SetVector("_Scale", Vector4.one*0.125F);
			r.materials[0].SetVector("_Frequency", new Vector4(0.5F, 0.5F, 0.25F, 0.0F));
			r.materials[0].SetVector("_Speed", Vector4.one*0.025F);
			r.materials[0].SetVector("_ObjectUp", new Vector4(0F, 0F, 0F, 0F));
			r.materials[0].SetFloat("_WaveUpMin", 2.5F);
			r.materials[0].SetFloat("_minYpos", 0.7F);
			r.materials[0].SetFloat("_maxYpos", 0.3F);
			r.materials[1].DisableKeyword("FX_KELP");
			/*
			RenderUtil.setEmissivity(r.materials[0], 0, "GlowStrength");
			RenderUtil.setEmissivity(r.materials[1], 0, "GlowStrength");
			r.materials[0].SetFloat("_Shininess", 10);
			r.materials[0].SetFloat("_Fresnel", 1);
			setupRenderer(r, "Main");
			RenderUtil.makeTransparent(r.materials[1]);
			
			r.materials[0].SetColor("_Color", new Color(0, 0, 0, 1F));*/
			return world;
	    }
			
	}
		
	public class LavaBombTag : MonoBehaviour {
		
		private static readonly SoundManager.SoundData fireSound = SoundManager.registerSound(EcoceanMod.modDLL, "lavabombfire", "Sounds/lavabomb-fire.ogg", SoundManager.soundMode3D, s => {SoundManager.setup3D(s, 40);}, SoundSystem.masterBus);
		private static readonly SoundManager.SoundData impactSound = SoundManager.registerSound(EcoceanMod.modDLL, "lavabombimpact", "Sounds/lavabomb-impact.ogg", SoundManager.soundMode3D, s => {SoundManager.setup3D(s, 40);}, SoundSystem.masterBus);
		
		public static event Action<LavaBombTag, GameObject> onLavaBombImpactEvent;
		
		private Light light;
		
		private Renderer mainRender;
		private PrefabIdentifier prefab;
		private Rigidbody mainBody;
		
		private float temperature;
		
		private static readonly Color glowNew = new Color(1, 0.7F, 0.1F, 1F);
		private static readonly Color glowFinal = new Color(1, 40F/255F, 0, 1);
		
		private bool isCollided;
		private float lastPLayerDistanceCheckTime;
		
		private float spawnTime;
		
		void Update() {
			if (!mainRender)
				mainRender = GetComponentInChildren<Renderer>();
			if (!mainBody)
				mainBody = GetComponentInChildren<Rigidbody>();
			if (!prefab)
				prefab = GetComponentInChildren<PrefabIdentifier>();
			if (!light)
				light = GetComponentInChildren<Light>();
			
			transform.localScale = Vector3.one*1.5F;
			
			float time = DayNightCycle.main.timePassedAsFloat;
			
			temperature = Mathf.Max(600, temperature-Time.deltaTime*100);
			
			if (mainBody && mainBody.velocity.magnitude < 0.1F) {
				explode(null);
				//SNUtil.writeToChat("Destroyed lava bomb because zero speed");
			}
			else if (spawnTime > 0 && time-spawnTime >= 45) {
				explode(null);
				//SNUtil.writeToChat("Destroyed lava bomb because age");
			}
			else if (time-lastPLayerDistanceCheckTime >= 0.5) {
				lastPLayerDistanceCheckTime = time;
				if (Vector3.Distance(transform.position, Player.main.transform.position) > 250) {
					UnityEngine.Object.Destroy(gameObject);
					//SNUtil.writeToChat("Destroyed lava bomb because far");
				}
			}
			
			float f = getIntensity();
			if (light)
				light.intensity = UnityEngine.Random.Range(1.8F, 2.2F)*f;
			if (mainRender) {
				RenderUtil.setEmissivity(mainRender.materials[0], f*45);
				RenderUtil.setEmissivity(mainRender.materials[1], (0.5F+f*0.5F)*7.5F);
				mainRender.materials[0].SetColor("_GlowColor", getColor(f));
				mainRender.materials[0].SetColor("_SpecColor", getColor(f));
				mainRender.materials[1].SetColor("_GlowColor", Color.Lerp(Color.white, Color.red, 1-f));
			}
			if (isCollided) {
				UnityEngine.Object.Destroy(gameObject);
				//SNUtil.writeToChat("Destroyed lava bomb because collided");
			}
		}
		
		internal Color getColor(float f) {
			return Color.Lerp(glowNew, glowFinal, 1-f);
		}
		
		internal void onFired() {
			temperature = LavaBomb.MAX_TEMPERATURE;
			SoundManager.playSoundAt(fireSound, transform.position, false, 40);
			if (Vector3.Distance(transform.position, Player.main.transform.position) <= 200)
				WorldUtil.spawnParticlesAt(transform.position, "db6907f8-2c37-4d0b-8eac-1b1e3b59fa71", 0.5F);
			spawnTime = DayNightCycle.main.timePassedAsFloat;
			LavaBomb.activeLavaBombs.Add(this);
		}
		
		public float getTemperature() {
			return temperature;
		}
		
		public float getIntensity() {
			return temperature/LavaBomb.MAX_TEMPERATURE;
		}

	    void OnCollisionEnter(Collision c) {
			GameObject collider = c.gameObject;
			if (spawnTime > 0 && DayNightCycle.main.timePassedAsFloat-spawnTime >= 0.1 && c.relativeVelocity.magnitude >= 2 && (collider.FindAncestor<LiveMixin>() || collider.FindAncestor<SubRoot>() || collider.FindAncestor<WorldStreaming.ClipmapChunk>())) {
			//SNUtil.writeToChat("Collided with "+collider+" at speed "+c.relativeVelocity.magnitude);
	        	explode(collider);
	        	GlowShroomTagBase gs = collider.FindAncestor<GlowShroomTagBase>();
	        	if (gs)
	        		gs.fireAsap();
			}
	    }
		
		void OnDestroy() {
			LavaBomb.activeLavaBombs.Remove(this);
		}
		
		void OnDisable() {
			UnityEngine.Object.Destroy(gameObject);
			LavaBomb.activeLavaBombs.Remove(this);
		}
		
		internal void explode(GameObject impacted) {
			LavaBomb.activeLavaBombs.Remove(this);
			if (onLavaBombImpactEvent != null)
				onLavaBombImpactEvent.Invoke(this, impacted);
			float pdist = Vector3.Distance(transform.position, Player.main.transform.position);
			if (pdist <= 80) {
				SoundManager.playSoundAt(impactSound, transform.position, false, 40);
				HashSet<int> used = new HashSet<int>();
				HashSet<LiveMixin> set = WorldUtil.getObjectsNearWithComponent<LiveMixin>(transform.position, 15);
				foreach (LiveMixin lv in set) {
					if (!lv.IsAlive() || used.Contains(lv.gameObject.GetInstanceID()))
						continue;
					bool wasHit = lv.gameObject == impacted;
					used.Add(lv.gameObject.GetInstanceID());
					Player p = lv.GetComponent<Player>();
					if (p && !p.IsSwimming())
						continue;
					
					float amt = wasHit ? 100 : 20;
					SubRoot sub = lv.GetComponent<SubRoot>();
					if (sub && sub.isCyclops)
						amt = wasHit ? 150 : 45;
					Vehicle v = lv.GetComponent<Vehicle>();
					if (v && v is SeaMoth)
						amt = wasHit ? 60 : 18;
					else if (v && v is Exosuit)
						amt = wasHit ? 100 : 35;
					if (!wasHit) {
						float f = (Vector3.Distance(lv.transform.position, transform.position))/15F;
						amt *= Mathf.Clamp01(1.5F-f*f);
					}
					amt *= 0.5F+0.5F*getIntensity();
					amt *= EcoceanMod.config.getFloat(ECConfig.ConfigEntries.BOMBDMG);
					lv.TakeDamage(amt, lv.transform.position, DamageType.Heat, gameObject);
				}
			}
			isCollided = true;
			if (pdist <= 200)
				WorldUtil.spawnParticlesAt(transform.position, "db6907f8-2c37-4d0b-8eac-1b1e3b59fa71", 0.5F);
			
			//if (impacted)
			//	SNUtil.writeToChat("Destroyed lava bomb during explode on "+impacted);
			UnityEngine.Object.Destroy(gameObject);
		}
		
	}
}
