﻿using System;
using System.IO;
using System.Xml;
using System.Linq;
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
	
	public class PlanktonCloud : Spawnable {
		
		private readonly XMLLocale.LocaleEntry locale;
		
		internal static readonly float BASE_RANGE = 4;
		internal static readonly float MAX_RANGE = 8;
		internal static readonly float VOID_RANGE_SCALE = 2;
		internal static readonly float LEVI_RANGE_SCALE = 3;
		
		internal static readonly Simplex3DGenerator densityNoise = (Simplex3DGenerator)new Simplex3DGenerator(3340487).setFrequency(0.05);
		
		private readonly Dictionary<string, BiomeSpawnData> spawnData = new Dictionary<string, BiomeSpawnData>();
	        
	    internal PlanktonCloud(XMLLocale.LocaleEntry e) : base(e.key, e.name, e.desc) {
			locale = e;
			
			spawnData["sparsereef"] = new BiomeSpawnData(5, 0.5F, 1F, 120);
			spawnData["mountains"] = new BiomeSpawnData(6, 1F, 0F, 120);
			spawnData["cragfield"] = new BiomeSpawnData(12, 1, 0.5F, 150);
			spawnData["void"] = new BiomeSpawnData(25, 4, 1, 400);
			
			OnFinishedPatching += () => {
				SaveSystem.addSaveHandler(ClassID, new SaveSystem.ComponentFieldSaveHandler<PlanktonCloudTag>().addField("touchIntensity"));
			};
	    }
			
	    public override GameObject GetGameObject() {
			GameObject world = ObjectUtil.createWorldObject("0e67804e-4a59-449d-929a-cd3fc2bef82c");
			world.EnsureComponent<TechTag>().type = TechType;
			world.EnsureComponent<PrefabIdentifier>().ClassId = ClassID;
			world.GetComponent<Rigidbody>().isKinematic = true;
			ObjectUtil.removeComponent<Collider>(world);
			SphereCollider rootCollide = world.EnsureComponent<SphereCollider>();
			rootCollide.center = Vector3.zero;
			rootCollide.radius = 8;
			rootCollide.isTrigger = true;
			//ObjectUtil.removeComponent<BloomCreature>(world);
			BloomCreature bc = world.GetComponent<BloomCreature>();
			PlanktonCloudTag g = world.EnsureComponent<PlanktonCloudTag>();
			//
			UnityEngine.Object.Destroy(bc);
			ObjectUtil.removeComponent<StayAtLeashPosition>(world);
			ObjectUtil.removeComponent<SwimBehaviour>(world);
			ObjectUtil.removeComponent<SplineFollowing>(world);
			ObjectUtil.removeComponent<SwimRandom>(world);
			ObjectUtil.removeComponent<Locomotion>(world);
			ObjectUtil.removeComponent<CreatureUtils>(world);
			//ObjectUtil.removeComponent<LiveMixin>(world);
			ObjectUtil.removeComponent<BehaviourLOD>(world);
			ObjectUtil.removeComponent<LastScarePosition>(world);
			SphereCollider sc = world.EnsureComponent<SphereCollider>();
			sc.isTrigger = true;
			sc.center = Vector3.zero;
			sc.radius = BASE_RANGE;
			world.EnsureComponent<LargeWorldEntity>().cellLevel = LargeWorldEntity.CellLevel.VeryFar;
			world.layer = LayerID.Useable;
			return world;
	    }
		
		public void register() {
			Patch();
			SNUtil.addPDAEntry(this, 3, "Lifeforms/Flora/Sea", locale.pda, locale.getField<string>("header"));/*
			PDAManager.PDAPage pdaPage = PDAManager.createPage("ency_"+ClassID, FriendlyName, locale.pda, "Lifeforms/Flora/Sea");
			pdaPage.setHeaderImage(TextureManager.getTexture(EcoceanMod.modDLL, "Textures/PDA/"+locale.getField<string>("header")));
			pdaPage.register();
        	KnownTechHandler.Main.SetAnalysisTechEntry(TechType, new List<TechType>(){TechType});
			PDAScanner.EntryData e = new PDAScanner.EntryData();
			e.key = TechType;
			e.locked = true;
			e.scanTime = 3;
			e.encyclopedia = pdaPage.id;
			PDAHandler.AddCustomScannerEntry(e);*/
			ItemRegistry.instance.addItem(this);
			//GenUtil.registerSlotWorldgen(ClassID, PrefabFileName, TechType, EntitySlot.Type.Creature, LargeWorldEntity.CellLevel.Far, BiomeType.CragField_OpenDeep_CreatureOnly, 1, 1F);
		}
		
		internal void tickSpawner(Player ep, BiomeSpawnData data, float dT) {
			HashSet<PlanktonCloudTag> clouds = WorldUtil.getObjectsNearWithComponent<PlanktonCloudTag>(ep.transform.position, 150);
			//SNUtil.writeToChat(data.spawnSuccessRate+" > "+clouds.Count+"/"+data.maxDensity);
			float f = (float)(1+densityNoise.getValue(ep.transform.position))*data.densityNoiseIntensity;
			if (clouds.Count < data.maxDensity*f) {
				for (int i = 0; i < 16; i++) {
					Vector3 pos = getRandomPosition(ep);
					BiomeSpawnData data2 = getSpawnData(WaterBiomeManager.main.GetBiome(pos, false));
		    		if (data2 != null && UnityEngine.Random.Range(0F, 1F) <= data2.spawnSuccessRate) {
						pos = pos.setY(-UnityEngine.Random.Range(data2.minDepth, data2.maxDepth));
						while (Vector3.Distance(pos, ep.transform.position) < 50 || (ep.GetVehicle() is SeaMoth && ep.GetVehicle().useRigidbody && Vector3.Distance(pos, ep.transform.position+ep.GetVehicle().useRigidbody.velocity.normalized*20) < 30)) {
							pos = getRandomPosition(ep, data2);
						}
						GameObject go = ObjectUtil.createWorldObject(ClassID);
						go.transform.position = pos;
						ObjectUtil.fullyEnable(go);
						//SNUtil.writeToChat("spawned plankton at "+go.transform.position+" dist="+Vector3.Distance(pos, ep.transform.position));
						break;
		    		}
				}
		    }
		}
		
		internal Vector3 getRandomPosition(Player ep, BiomeSpawnData data = null) {
			Vector3 pos = MathUtil.getRandomVectorAround(ep.transform.position, 120);
			Vector3 dist = pos-ep.transform.position;
			pos = ep.transform.position+dist.setLength(UnityEngine.Random.Range(50F, 150F));
			if (data != null)
				pos = pos.setY(-UnityEngine.Random.Range(data.minDepth, data.maxDepth));
			return pos;
		}
		
		internal BiomeSpawnData getSpawnData(string biome) {
			if (string.IsNullOrEmpty(biome))
				biome = "void";
			biome = biome.ToLowerInvariant();
			return spawnData.ContainsKey(biome) ? spawnData[biome] : null;
		}
			
	}
	
	class BiomeSpawnData {
		
		public readonly int maxDensity;
		public readonly float spawnSuccessRate;
		public readonly float densityNoiseIntensity;
		public readonly int minDepth;
		public readonly int maxDepth;
		
		internal BiomeSpawnData(int d, float r, float n, int maxd, int mind = 15) {
			maxDensity = d;
			spawnSuccessRate = r;
			densityNoiseIntensity = n;
			minDepth = mind;
			maxDepth = maxd;
		}
		
	}
	
	public class PlanktonCloudLeviDetector : MonoBehaviour {
		
		private PlanktonCloudTag root;
		private Collider aoe;
		//private Collider touchingEntity;
		
		internal void init(PlanktonCloudTag e, Collider c) {
			root = e;
			aoe = c;
		}
		/*
		private void Update() {
			if (!aoe)
				aoe = GetComponent<SphereCollider>();
			if (touchingEntity) {
				if (touchingEntity.o.intersects(aoe))
					root.touch(Time.deltaTime, touchingEntity);
				else
					touchingEntity = null;
			}
		}*/

		private void OnTriggerEnter(Collider other) {
			if (!root)
				return;
			float time = DayNightCycle.main.timePassedAsFloat;
			Creature c = other.gameObject.FindAncestor<Creature>();
			if (c && (c is ReaperLeviathan || c is GhostLeviatanVoid || c is GhostLeviathan || c is SeaDragon/* || c is Reefback*/)) {
				//touchingEntity = other;
				//root.touch(Time.deltaTime, other);
				root.addTouchIntensity(4);
			}
	    }
		
	}
		
	public class PlanktonCloudTag : MonoBehaviour {
		
		private Light light;
		
		private Renderer mainRender;
		private ParticleSystem particles;
		private ParticleSystem.MainModule particleCore;
		private Rigidbody mainBody;
		private LiveMixin health;
		private SphereCollider aoe;
		private SphereCollider leviAOE;
		
		private GameObject leviSphere;
		
		public static event Action<PlanktonCloudTag, Collider> onPlanktonActivationEvent;
		public static event Action<PlanktonCloudTag, SeaMoth> onPlanktonScoopEvent;
		
		private static readonly Color glowNew = new Color(0, 0.75F, 0.1F, 1F);
		private static readonly Color glowFinal = new Color(0.4F, 1F, 0.8F, 1);
		private static readonly Color touchColor = new Color(0.15F, 0.5F, 1F, 1);
		private static readonly Color scoopColor = new Color(0.75F, 0.25F, 1F, 1);
		
		private Color currentColor;
		private float touchIntensity;
		
		//private float lastContactTime;
		private float lastScoopTime;
		
		private float lastActivatorCheckTime;
		private List<GameObject> forcedActivators = new List<GameObject>();
		
		private float minParticleSize = 2;
		private float maxParticleSize = 5;
		
		private bool isDead = false;
		
		private float activation = 0;
		
		void Update() {
			if (!mainRender)
				mainRender = GetComponentInChildren<Renderer>();
			if (!mainBody)
				mainBody = GetComponentInChildren<Rigidbody>();
			if (!health)
				health = GetComponentInChildren<LiveMixin>();
			if (!particles) {
				particles = GetComponentInChildren<ParticleSystem>();
				particleCore = particles.main;
			}
			if (!light)
				light = GetComponentInChildren<Light>();
			if (!aoe)
				aoe = GetComponentInChildren<SphereCollider>();
			
			if (!leviSphere) {
				leviSphere = ObjectUtil.getChildObject(gameObject, "leviSphere");
				if (!leviSphere) {
					leviSphere = new GameObject("leviSphere");
					leviSphere.transform.SetParent(transform);
					leviSphere.transform.localPosition = Vector3.zero;
					leviAOE = leviSphere.EnsureComponent<SphereCollider>();
					leviAOE.isTrigger = true;
					leviSphere.EnsureComponent<PlanktonCloudLeviDetector>().init(this, leviAOE);
				}
			}
			
			leviAOE.center = Vector3.zero;
			leviSphere.transform.localPosition = Vector3.zero;
			transform.localScale = Vector3.one*0.5F;
			
			mainBody.constraints = RigidbodyConstraints.FreezeAll;
			
			string biome = WaterBiomeManager.main.GetBiome(transform.position);
			bool isVoid = string.IsNullOrEmpty(biome) || biome.ToLowerInvariant().Contains("void");
			
			float time = DayNightCycle.main.timePassedAsFloat;
			//float dtl = time-lastContactTime;
			float dscl = time-lastScoopTime;
			
			float dT = Time.deltaTime;
			if (time-ECHooks.getLastSonarUse() <= 10 || time-ECHooks.getLastHornUse() <= 10) {
				touchIntensity = Mathf.Max(1, touchIntensity);
			}
			if (time-lastActivatorCheckTime >= 0.5F) {
				lastActivatorCheckTime = time;
				forcedActivators.RemoveAll(go => !go || (go.transform.position-transform.position).sqrMagnitude >= 900);
				if (forcedActivators.Count > 0)
					addTouchIntensity(2);
			}
			
			if (isDead) {
				activation = Mathf.Clamp01(activation-2F*dT);
				touchIntensity = Mathf.Max(0, activation-2F*dT);
			}
			else if (touchIntensity > 0) {
				activation += 2*dT;
				touchIntensity = Mathf.Max(0, touchIntensity-0.1F*dT);
			}
			else {
				activation *= (1-0.2F*dT);
				activation -= 0.05F*dT;
				if (Player.main) {
					float dd = (Player.main.transform.position-transform.position).sqrMagnitude;
					if (dd < 1024) { //32m
						float vel = Player.main.rigidBody.velocity.magnitude;
						Vehicle v = Player.main.GetVehicle();
						if (v) {
							vel = v.GetComponent<Rigidbody>().velocity.magnitude;
							if (v is SeaMoth) {
								vel *= 2.5F;
							}
						}
						activation += vel/dd*dT;
					}
				}
			}
			activation = Mathf.Clamp(activation, isVoid ? 0.25F : 0F, 2F);
			float f = Mathf.Clamp01(activation);
			float f2 = 0;
			Color tgt = Color.Lerp(glowNew, glowFinal, f);
			if (dscl <= 10) {
				f2 = 1-dscl/10F;
				tgt = Color.Lerp(tgt, scoopColor, f2);
			}
			else if (touchIntensity > 0) {
				f2 = Mathf.Clamp01(touchIntensity);
				tgt = Color.Lerp(tgt, touchColor, f2);
			}
			float f3 = isVoid ? PlanktonCloud.VOID_RANGE_SCALE : 1;
			currentColor = Color.Lerp(currentColor, tgt, dT*5);
			aoe.center = Vector3.zero;
			float r = (float)MathUtil.linterpolate(f, 0, 1, PlanktonCloud.BASE_RANGE, PlanktonCloud.MAX_RANGE)*f3;
			aoe.radius = r*0.75F;
			particleCore.startColor = currentColor;
			particleCore.startSize = ((minParticleSize + (maxParticleSize - minParticleSize) * f)*(1+f2))*1.5F;
			particleCore.startLifetimeMultiplier = 1+f*1.5F+f2*2.5F;
			ParticleSystem.EmissionModule emit = particles.emission;
			emit.rateOverTimeMultiplier = 2+f+2*f2;
			ParticleSystem.ShapeModule shape = particles.shape;
			shape.radius = r*2;
			light.intensity = f+Mathf.Max(0, 2*f2);
			light.color = currentColor;
			light.range = f*16+f2*16*f3;
			leviAOE.radius = r*PlanktonCloud.LEVI_RANGE_SCALE;
		}
		
		void OnDestroy() {
			
		}
		
		void OnDisable() {
			UnityEngine.Object.Destroy(gameObject);
		}

	    private void OnTriggerStay(Collider other) { //scoop with seamoth
			if (other.gameObject != gameObject) {
				Vehicle v = other.gameObject.FindAncestor<Vehicle>();
				Player ep = other.gameObject.FindAncestor<Player>();
				float dT = Time.deltaTime;
				if (v || ep || other.gameObject.FindAncestor<Creature>() || other.gameObject.FindAncestor<SubRoot>())
					touch(dT, other);//lastContactTime = DayNightCycle.main.timePassedAsFloat;
				if (v is SeaMoth)
					checkAndTryScoop((SeaMoth)v, dT);
				if (ep)
					ep.liveMixin.TakeDamage(5*dT, ep.transform.position, DamageType.Poison, gameObject);
			}
			//SNUtil.writeToChat(other+" touch plankton @ "+this.transform.position+" @ "+lastContactTime);
	    }
		
		public void activateBy(GameObject go) {
			forcedActivators.Add(go);
		}
		
		internal void addTouchIntensity(float amt) {
			touchIntensity = Mathf.Max(0, touchIntensity+amt);
		}
		
		internal void touch(float dT, Collider touch) {
			addTouchIntensity(2F*dT);
			if (onPlanktonActivationEvent != null)
				onPlanktonActivationEvent.Invoke(this, touch);
		}
		
		private void checkAndTryScoop(SeaMoth sm, float dT) {
			if (sm.GetComponent<Rigidbody>().velocity.magnitude >= 4 && Vector3.Distance(sm.transform.position, transform.position) <= 5 && InventoryUtil.vehicleHasUpgrade(sm, EcoceanMod.planktonScoop.TechType)) {
				lastScoopTime = DayNightCycle.main.timePassedAsFloat;
				if (UnityEngine.Random.Range(0F, 1F) < 0.075F*dT*EcoceanMod.config.getFloat(ECConfig.ConfigEntries.PLANKTONRATE)) {
					foreach (SeamothStorageContainer sc in sm.GetComponentsInChildren<SeamothStorageContainer>(true)) {
						TechTag tt = sc.GetComponent<TechTag>();
						if (tt && tt.type == EcoceanMod.planktonScoop.TechType) {
							GameObject go = ObjectUtil.createWorldObject(EcoceanMod.planktonItem.TechType, true, false);
							sc.container.AddItem(go.GetComponentInChildren<Pickupable>());
							if (sc.container.IsFull())
								SNUtil.writeToChat("Plankton scoop is full");
							break;
						}
					}
				}
				if (health.health < health.maxHealth*0.1F) {
					isDead = true;
					particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
					UnityEngine.Object.Destroy(gameObject, 10);
				}
				else {
					health.TakeDamage(health.maxHealth*0.05F*dT, sm.transform.position, DamageType.Drill, sm.gameObject);
				}
				if (onPlanktonScoopEvent != null)
					onPlanktonScoopEvent.Invoke(this, sm);
			}
		}
		
	}
}
