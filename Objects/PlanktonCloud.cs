using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

	public class PlanktonCloud : Spawnable {

		private readonly XMLLocale.LocaleEntry locale;

		internal static readonly float BASE_RANGE = 10;
		internal static readonly float MAX_RANGE = 18;
		internal static readonly float VOID_RANGE_SCALE = 2;
		internal static readonly float LEVI_RANGE_SCALE = 3;

		internal static readonly Simplex3DGenerator densityNoise = (Simplex3DGenerator)new Simplex3DGenerator(3340487).setFrequency(0.05);

		private static readonly Dictionary<BiomeBase, BiomeSpawnData> spawnData = new Dictionary<BiomeBase, BiomeSpawnData>();

		internal PlanktonCloud(XMLLocale.LocaleEntry e) : base(e.key, e.name, e.desc) {
			locale = e;

			addSpawnData(VanillaBiomes.SPARSE, 5, 0.5F, 1F, 120);
			addSpawnData(VanillaBiomes.MOUNTAINS, 6, 1F, 0F, 120);
			addSpawnData(VanillaBiomes.CRAG, 12, 1, 0.5F, 150);
			addSpawnData(VanillaBiomes.VOID, 25, 4, 1, 400);

			OnFinishedPatching += () => {
				SaveSystem.addSaveHandler(ClassID, new SaveSystem.ComponentFieldSaveHandler<PlanktonCloudTag>().addField("touchIntensity"));
			};
		}

		private static void addSpawnData(BiomeBase bb, int d, float r, float n, int maxd, int mind = 15) {
			spawnData[bb] = new BiomeSpawnData(bb, d, r, n, maxd, mind);
		}

		public override GameObject GetGameObject() {
			GameObject world = ObjectUtil.createWorldObject("0e67804e-4a59-449d-929a-cd3fc2bef82c");
			world.EnsureComponent<TechTag>().type = TechType;
			world.EnsureComponent<PrefabIdentifier>().ClassId = ClassID;
			world.removeComponent<Collider>();
			SphereCollider rootCollide = world.EnsureComponent<SphereCollider>();
			rootCollide.center = Vector3.zero;
			rootCollide.radius = 8;
			rootCollide.isTrigger = true;
			PlanktonCloudTag g = world.EnsureComponent<PlanktonCloudTag>();
			g.init();
			SphereCollider sc = world.EnsureComponent<SphereCollider>();
			sc.isTrigger = true;
			sc.center = Vector3.zero;
			sc.radius = BASE_RANGE;
			world.EnsureComponent<LargeWorldEntity>().cellLevel = LargeWorldEntity.CellLevel.VeryFar;
			world.layer = LayerID.Useable;
			//SNUtil.log("plankton wiht components: "+world.GetComponents<MonoBehaviour>().Select(c => c.GetType().Name+"="+c.enabled).toDebugString());
			return world;
		}

		public void register() {
			this.Patch();
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
			if (clouds.Count < data.maxDensity * f) {
				for (int i = 0; i < 16; i++) {
					Vector3 pos = this.getRandomPosition(ep);
					BiomeSpawnData data2 = getSpawnData(BiomeBase.getBiome(pos));
					if (data2 != null && UnityEngine.Random.Range(0F, 1F) <= data2.spawnSuccessRate) {
						pos = pos.setY(-UnityEngine.Random.Range(data2.minDepth, data2.maxDepth));
						while (Vector3.Distance(pos, ep.transform.position) < 50 || (ep.GetVehicle() is SeaMoth && ep.GetVehicle().useRigidbody && Vector3.Distance(pos, ep.transform.position + (ep.GetVehicle().useRigidbody.velocity.normalized * 20)) < 30)) {
							pos = this.getRandomPosition(ep, data2);
							pos = pos.setY(-UnityEngine.Random.Range(data2.minDepth, data2.maxDepth));
						}
						GameObject go = ObjectUtil.createWorldObject(ClassID);
						go.transform.position = pos;
						go.fullyEnable();
						//SNUtil.writeToChat("spawned plankton at "+go.transform.position+" dist="+Vector3.Distance(pos, ep.transform.position));
						break;
					}
				}
			}
		}

		internal Vector3 getRandomPosition(Player ep, BiomeSpawnData data = null) {
			Vector3 pos = MathUtil.getRandomVectorAround(ep.transform.position, 120);
			Vector3 dist = pos-ep.transform.position;
			pos = ep.transform.position + dist.setLength(UnityEngine.Random.Range(50F, 150F));
			if (data != null)
				pos = pos.setY(-UnityEngine.Random.Range(data.minDepth, data.maxDepth));
			return pos;
		}

		internal static BiomeSpawnData getSpawnData(BiomeBase biome) {
			return spawnData.ContainsKey(biome) ? spawnData[biome] : null;
		}

		internal static void forSpawnData(Action<BiomeSpawnData> call) {
			spawnData.Values.ForEach(call);
		}

	}

	public class BiomeSpawnData {

		public readonly BiomeBase biome;
		public readonly int maxDensity;
		public readonly float spawnSuccessRate;
		public readonly float densityNoiseIntensity;
		public readonly int minDepth;
		public readonly int maxDepth;

		internal BiomeSpawnData(BiomeBase b, int d, float r, float n, int maxd, int mind = 15) {
			biome = b;
			maxDensity = d;
			spawnSuccessRate = r;
			densityNoiseIntensity = n;
			minDepth = mind;
			maxDepth = maxd;
		}

	}

	public class PlanktonCloudLeviDetector : MonoBehaviour { //also detects cyclops

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
			if (!root.enabled) {
				root.gameObject.fullyEnable();
				return;
			}
			if (root.isTouchDisabled())
				return;
			float time = DayNightCycle.main.timePassedAsFloat;
			Creature c = other.gameObject.FindAncestor<Creature>();
			SubRoot sub = other.gameObject.FindAncestor<SubRoot>();
			if ((c && c.GetComponent<AggressiveWhenSeeTarget>()) || (sub && sub.isCyclops)) {
				//touchingEntity = other;
				//root.touch(Time.deltaTime, other);
				root.addTouchIntensity(sub ? 15 : 8);
				//SNUtil.writeToChat("Levisense Touching "+other.name+" > "+other.gameObject.name+" @ "+DayNightCycle.main.timePassedAsFloat);
			}
		}

	}

	public class PlanktonCloudTag : PlanktonCloudClearableContactZone {

		private Light light;

		private Renderer mainRender;
		private ParticleSystem particles;
		private ParticleSystem.MainModule particleCore;
		//private Rigidbody mainBody;
		private LiveMixin health;
		private SphereCollider aoe;

		private AuxSphere leviSphere = new AuxSphere("leviSphere");
		private AuxSphere clearingSphere = new AuxSphere("clearingSphere");

		public BaseCellEnviroHandler isBaseBound;

		public static event Action<PlanktonCloudTag, GameObject> onPlanktonActivationEvent;
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
		private List<GameObject> touching = new List<GameObject>();

		private float minParticleSize = 2;
		private float maxParticleSize = 5;

		private bool isDead = false;

		private float age;

		private float activation = 0;

		void Awake() {
			if (DIHooks.getWorldAge() < 0.5F)
				gameObject.destroy(false, 0.1F);
			else
				this.Invoke("init", 0.5F);
		}

		internal void init() {
			SNUtil.log("Forcing enable of plankton @ " + transform.position + " @ " + DayNightCycle.main.timePassedAsFloat);
			gameObject.removeComponent<BloomCreature>();
			gameObject.removeComponent<SwimRandom>();
			gameObject.removeComponent<StayAtLeashPosition>();
			gameObject.removeComponent<SwimBehaviour>();
			gameObject.removeComponent<SplineFollowing>();
			gameObject.removeComponent<Locomotion>();
			gameObject.removeComponent<CreatureUtils>();
			gameObject.removeComponent<BehaviourLOD>();
			gameObject.removeComponent<LastScarePosition>();
			health = gameObject.EnsureComponent<LiveMixin>();
			gameObject.fullyEnable();
			gameObject.GetComponent<Rigidbody>().isKinematic = true;
		}

		void Update() {
			parent = this;

			if (!mainRender)
				mainRender = this.GetComponentInChildren<Renderer>();
			//if (!mainBody)
			//	mainBody = GetComponentInChildren<Rigidbody>();
			if (!health)
				health = this.GetComponentInChildren<LiveMixin>();
			if (!particles) {
				particles = this.GetComponentInChildren<ParticleSystem>();
				particleCore = particles.main;
			}
			if (!particles || !health) {
				SNUtil.log("destroying incomplete plankton object: " + this.GetComponents<Component>().Select(c => c.name + " [" + c.GetType() + "]").toDebugString());
				gameObject.destroy(false);
				return;
			}
			if (!light)
				light = this.GetComponentInChildren<Light>();
			if (!aoe)
				aoe = this.GetComponentInChildren<SphereCollider>();

			this.setupAuxSphere(leviSphere, s => s.entity.EnsureComponent<PlanktonCloudLeviDetector>().init(this, s.collider));
			this.setupAuxSphere(clearingSphere, s => s.entity.EnsureComponent<PlanktonCloudClearableContactZone>().parent = this);

			transform.localScale = Vector3.one * 0.5F;

			//mainBody.constraints = RigidbodyConstraints.FreezeAll;

			string biome = WaterBiomeManager.main.GetBiome(transform.position);
			bool isVoid = string.IsNullOrEmpty(biome) || biome.ToLowerInvariant().Contains("void");

			float time = DayNightCycle.main.timePassedAsFloat;

			//float dtl = time-lastContactTime;
			float dscl = time-lastScoopTime;

			float dT = Time.deltaTime;

			age += dT;

			if (age > 0.5F) { //do not apply effects for first 0.5s, prevent pulses of damage from spawned and then killed plankton
							  //if (touching.Count > 0)
							  //	SNUtil.writeToChat("ticking plankton with "+touching.Count+" contacts");
				foreach (GameObject other in touching) {
					if (!other)
						continue;
					this.touch(dT, other);//lastContactTime = DayNightCycle.main.timePassedAsFloat;
					SeaMoth s = other.GetComponent<SeaMoth>();
					if (s && !isBaseBound)
						this.checkAndTryScoop(s, dT);
					if (other.isPlayer() && !Player.main.currentSub && !Player.main.GetVehicle()) {
						float hf = health.GetHealthFraction();
						float amt = isBaseBound ? (hf < 0.25 ? 0 : 15 * dT * hf * hf) : 5 * dT * hf;
						if (isBaseBound) {
							if (age < 0.6F || DayNightCycle.main.timePassedAsFloat - lastScoopTime <= 0.5F)
								amt = 0;
							else
								amt *= Player.main.liveMixin.GetHealthFraction();
						}
						if (amt > 0)
							Player.main.liveMixin.TakeDamage(amt, Player.main.transform.position, DamageType.Poison, gameObject);
					}
				}
			}

			if (isBaseBound && age >= 300) { //5 min
				health.TakeDamage(dT * 50, transform.position);
			}

			if (time - ECHooks.getLastSonarUse() <= 10 || time - ECHooks.getLastHornUse() <= 10) {
				touchIntensity = Mathf.Max(1, touchIntensity);
			}
			if (time - lastActivatorCheckTime >= 0.5F) {
				if (this.GetComponent<BloomCreature>()) //force cleanup
					this.init();
				lastActivatorCheckTime = time;
				forcedActivators.RemoveAll(go => !go || (go.transform.position - transform.position).sqrMagnitude >= 900);
				if (forcedActivators.Count > 0)
					this.addTouchIntensity(2);
			}

			if (isDead) {
				activation = Mathf.Clamp01(activation - (2F * dT));
				touchIntensity = Mathf.Max(0, activation - (2F * dT));
			}
			else if (touchIntensity > 0) {
				activation += 2 * dT;
				touchIntensity = Mathf.Max(0, touchIntensity - (0.1F * dT));
			}
			else {
				activation *= 1 - (0.2F * dT);
				activation -= 0.05F * dT;
				if (Player.main) {
					float dd = (Player.main.transform.position-transform.position).sqrMagnitude;
					if (dd < 1600) { //40m
						float vel = Player.main.rigidBody.velocity.magnitude;
						Vehicle v = Player.main.GetVehicle();
						if (v) {
							vel = v.useRigidbody.velocity.magnitude * 2.0F;
							if (v is SeaMoth) {
								vel *= 2.5F;
							}
						}
						activation += vel / dd * dT;
					}
				}
			}
			activation = Mathf.Clamp(activation, isVoid ? 0.25F : 0F, 2F);
			float f = Mathf.Clamp01(activation);
			float f2 = 0;
			Color tgt = Color.Lerp(glowNew, glowFinal, f);
			if (dscl <= 10) {
				f2 = 1 - (dscl / 10F);
				tgt = Color.Lerp(tgt, scoopColor, f2);
			}
			else if (touchIntensity > 0) {
				f2 = Mathf.Clamp01(touchIntensity);
				tgt = Color.Lerp(tgt, touchColor, f2);
			}
			float f3 = isVoid ? PlanktonCloud.VOID_RANGE_SCALE : 1;
			currentColor = Color.Lerp(currentColor, tgt, dT * 5);
			aoe.center = Vector3.zero;
			float r = (float)MathUtil.linterpolate(f, 0, 1, PlanktonCloud.BASE_RANGE, PlanktonCloud.MAX_RANGE)*f3;
			aoe.radius = r * 0.75F * (isBaseBound ? 0.75F : 1);
			particleCore.startColor = currentColor;
			float f4 = isBaseBound ? 0.2F : f;
			particleCore.startSize = (minParticleSize + ((maxParticleSize - minParticleSize) * f4)) * (1 + f2) * 1.5F * (isBaseBound ? 0.67F : 1);
			particleCore.startLifetimeMultiplier = 1 + (f * 1.5F) + (f2 * 2.5F);
			ParticleSystem.EmissionModule emit = particles.emission;
			emit.rateOverTimeMultiplier = (2 + f + (2 * f2)) * (isBaseBound ? 0.5F : 1);
			ParticleSystem.ShapeModule shape = particles.shape;
			shape.radius = r * (isBaseBound ? 0.6F : 2F);
			light.intensity = (f + Mathf.Max(0, 2 * f2)) * (isBaseBound ? 0.07F : 1);
			light.color = currentColor;
			light.range = (f * 16) + (f2 * 16 * f3);
			leviSphere.radius = r * PlanktonCloud.LEVI_RANGE_SCALE * (isBaseBound ? 0.2F : 1);
			clearingSphere.radius = isBaseBound ? 7.5F : 20;
		}

		private void setupAuxSphere(AuxSphere field, Action<AuxSphere> onSetup = null) {
			if (!field.entity) {
				field.entity = gameObject.getChildObject(field.name);
				if (!field.entity) {
					field.entity = new GameObject(field.name);//GameObject.CreatePrimitive(PrimitiveType.Sphere);
					field.entity.transform.SetParent(transform);
					field.entity.transform.localPosition = Vector3.zero;
					field.collider = field.entity.EnsureComponent<SphereCollider>();
					field.collider.isTrigger = true;
					if (onSetup != null)
						onSetup.Invoke(field);
				}
			}
			field.entity.transform.localPosition = Vector3.zero;
			field.collider.center = Vector3.zero;
		}

		void OnDestroy() {
			touching.Clear();
		}

		void OnDisable() {
			gameObject.destroy(false);
		}

		public static bool skipPlanktonTouch;

		public bool isTouchDisabled() {
			return isBaseBound && Player.main.currentSub && Player.main.currentSub.isBase;
		}

		private void OnTriggerEnter(Collider other) { //scoop with seamoth
			if (other.isTrigger || skipPlanktonTouch || this.isTouchDisabled())
				return;
			if (!enabled) {
				this.init();
				return;
			}
			if (other.isPlayer() && (Player.main.currentSub || Player.main.GetVehicle()))
				return;
			if (other.gameObject != gameObject) {
				GameObject go = this.getRoot(other);
				bool flag = false;
				if (go && (go.isPlayer() || go.GetComponent<SeaMoth>() || (!isBaseBound && go.GetComponent<Creature>()))) {
					touching.Add(go);
					flag = true;
				}
				//SNUtil.writeToChat("Touching "+other.name+" > "+go.name+" > "+flag+" @ "+DayNightCycle.main.timePassedAsFloat);
			}
			//SNUtil.writeToChat(other+" touch plankton @ "+this.transform.position+" @ "+lastContactTime);
		}

		private void OnTriggerExit(Collider other) {
			touching.Remove(this.getRoot(other));
		}

		private GameObject getRoot(Collider other) {
			return other.isPlayer() ? other.gameObject : UWE.Utils.GetEntityRoot(other.gameObject);
		}

		public void activateBy(GameObject go) {
			forcedActivators.Add(go);
		}

		internal void addTouchIntensity(float amt) {
			touchIntensity = Mathf.Clamp(touchIntensity + amt, 0, 10);
		}

		internal void touch(float dT, GameObject c) {
			this.addTouchIntensity(2F * dT);
			if (onPlanktonActivationEvent != null)
				onPlanktonActivationEvent.Invoke(this, c);
		}

		private void checkAndTryScoop(SeaMoth sm, float dT) {
			if (Vector3.Distance(sm.transform.position, transform.position) <= 5) {
				if (SeamothPlanktonScoop.checkAndTryScoop(sm, dT, EcoceanMod.planktonItem.TechType)) {
					this.damage(sm, dT);
					if (onPlanktonScoopEvent != null)
						onPlanktonScoopEvent.Invoke(this, sm);
				}
			}
		}

		public void damage(Component pos, float dT) {
			if (!pos) {
				SNUtil.log("Cannot damage plankton from null pos");
				return;
			}
			lastScoopTime = DayNightCycle.main.timePassedAsFloat;
			if (!health) {
				SNUtil.log("Cannot damage plankton without health");
				this.init();
				return;
			}
			if (health.health < health.maxHealth * 0.1F) {
				isDead = true;
				if (particles)
					particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
				gameObject.destroy(false, 10);
			}
			else {
				health.TakeDamage(health.maxHealth * 0.05F * dT, pos.transform.position, DamageType.Drill, pos.gameObject);
			}
		}

	}

	public class PlanktonCloudClearableContactZone : MonoBehaviour {

		internal PlanktonCloudTag parent;

	}

	internal class AuxSphere {

		internal GameObject entity;
		internal SphereCollider collider;
		internal readonly string name;

		public float radius {
			get {
				return collider.radius;
			}
			set {
				collider.radius = value;
				entity.transform.localScale = Vector3.one;//*2*value; //default sphere has a radius of 0.5F
			}
		}

		internal AuxSphere(string n) {
			name = n;
		}

	}
}
