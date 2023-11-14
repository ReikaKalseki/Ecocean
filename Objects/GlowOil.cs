using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Reflection;
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
	
	public class GlowOil : Spawnable {
		
		internal static readonly SoundManager.SoundData splatSound = SoundManager.registerSound(EcoceanMod.modDLL, "oilsplat", "Sounds/splat.ogg", SoundManager.soundMode3D, s => {SoundManager.setup3D(s, 40);}, SoundSystem.masterBus);
		
		internal static readonly float MAX_GLOW = 1.5F;
		internal static readonly float MAX_RADIUS = 30;//18;
		
		private readonly XMLLocale.LocaleEntry locale;
		
		private static float lastPlayerLightCheck;
		private static float lastLightRaytrace;
		
		internal static readonly Simplex3DGenerator sizeNoise = (Simplex3DGenerator)new Simplex3DGenerator(0).setFrequency(0.4);
		
		private PDAManager.PDAPage pdaPage;
	        
	    internal GlowOil(XMLLocale.LocaleEntry e) : base(e.key, e.name, e.desc) {
			locale = e;
	    }
		
		public override Vector2int SizeInInventory {
			get { return new Vector2int(1, 1); }
		}

		protected sealed override Atlas.Sprite GetItemSprite() {
			return TextureManager.getSprite(EcoceanMod.modDLL, "Textures/Items/GlowOil");
		}
		
		public Atlas.Sprite getSprite() {
			return GetItemSprite();
		}
			
	    public override GameObject GetGameObject() {
			GameObject world = ObjectUtil.createWorldObject("18229b4b-3ed3-4b35-ae30-43b1c31a6d8d"); //enzyme 42: "505e7eff-46b3-4ad2-84e1-0fadb7be306c"
			world.EnsureComponent<TechTag>().type = TechType;
			world.EnsureComponent<PrefabIdentifier>().ClassId = ClassID;
			ResourceTracker rt = world.EnsureComponent<ResourceTracker>();
			rt.techType = TechType;
			rt.overrideTechType = TechType;
			Rigidbody rb = world.GetComponent<Rigidbody>();
			rt.rb = rb;
			rt.StartUpdatePosition();
			world.GetComponentInChildren<PickPrefab>().pickTech = TechType;
			//world.GetComponent<Rigidbody>().isKinematic = true;
			Pickupable pp = world.GetComponent<Pickupable>();
			pp.overrideTechType = TechType;
			WorldForces wf = world.GetComponent<WorldForces>();
			wf.underwaterGravity = 0;
			wf.underwaterDrag *= 1;
			rb.angularDrag *= 3;
			rb.maxAngularVelocity = 6;
			rb.drag = wf.underwaterDrag;
			//ObjectUtil.removeComponent<EnzymeBall>(world);
			//ObjectUtil.removeComponent<Plantable>(world);
			Plantable p = world.GetComponent<Plantable>();
			GameObject jellyseed = ObjectUtil.lookupPrefab(TechType.SnakeMushroomSpore);
			Plantable p2 = jellyseed.GetComponent<Plantable>();
			p.plantTechType = p2.plantTechType;
			p.growingPlant = p2.growingPlant;
			p.isSeedling = p2.isSeedling;
			p.linkedGrownPlant = p2.linkedGrownPlant;
			p.model = p2.model;
			p.modelEulerAngles = p2.modelEulerAngles;
			p.modelIndoorScale = p2.modelIndoorScale;
			p.modelLocalPosition = p2.modelLocalPosition;
			p.modelScale = p2.modelScale;
			p.pickupable = pp;
			BasicCustomPlant.setPlantSeed(this, EcoceanMod.glowShroom);
			GlowOilTag g = world.EnsureComponent<GlowOilTag>();
			world.EnsureComponent<LargeWorldEntity>().cellLevel = LargeWorldEntity.CellLevel.VeryFar;
			Light l = ObjectUtil.addLight(world);
			l.bounceIntensity *= 2;
			l.color = new Color(0.5F, 0.8F, 1F, 1F);
			l.intensity = 0;
			l.range = MAX_RADIUS;
			Renderer r = world.GetComponentInChildren<Renderer>();
			RenderUtil.setEmissivity(r.materials[0], 0);
			RenderUtil.setEmissivity(r.materials[1], 0);
			r.materials[0].SetFloat("_Shininess", 10);
			r.materials[0].SetFloat("_SpecInt", 3);
			r.materials[0].SetFloat("_Fresnel", 1);
			setupRenderer(r, "Main");
			RenderUtil.makeTransparent(r.materials[1]);
			r.materials[0].EnableKeyword("FX_KELP");
			r.materials[0].SetColor("_Color", new Color(0, 0, 0, 1F));
			ObjectUtil.fullyEnable(world);
			return world;
	    }
		
		public void register() {
			Patch();
			//SNUtil.addPDAEntry(this, 3, "Advanced", locale.pda, locale.getField<string>("header"));/*
			pdaPage = PDAManager.createPage("ency_"+ClassID, FriendlyName, locale.pda, "Advanced");
			pdaPage.setHeaderImage(TextureManager.getTexture(EcoceanMod.modDLL, "Textures/PDA/"+locale.getField<string>("header")));
			pdaPage.register();
        	KnownTechHandler.Main.SetAnalysisTechEntry(TechType, new List<TechType>(){TechType});
			PDAScanner.EntryData e = new PDAScanner.EntryData();
			e.key = TechType;
			e.locked = true;
			e.scanTime = 3;
			e.encyclopedia = pdaPage.id;
			PDAHandler.AddCustomScannerEntry(e);//*/
			ItemRegistry.instance.addItem(this);
		}
		
		internal PDAManager.PDAPage getPDAEntry() {
			return pdaPage;
		}
		
		internal static void setupRenderer(Renderer r, string texName) {
			//RenderUtil.makeTransparent(r.materials[1]);
			//r.materials[1].SetFloat("_ZWrite", 1);
			for (int i = 0; i < r.materials.Length; i++) {
				Material m = r.materials[i];
				//m.DisableKeyword("UWE_WAVING");
				m.DisableKeyword("FX_KELP");
				m.SetVector("_Scale", Vector4.one*(0.06F-0.02F*i));
				m.SetVector("_Frequency", new Vector4(2.5F, 2F, 1.5F, 0.5F));
				m.SetVector("_Speed", Vector4.one*(0.1F-0.025F*i));
				m.SetVector("_ObjectUp", new Vector4(0F, 0F, 0F, 0F));
				m.SetFloat("_WaveUpMin", 2.5F);
				m.SetColor("_Color", new Color(1, 1, 1, 1));
				m.SetFloat("_minYpos", 0.7F);
				m.SetFloat("_maxYpos", 0.3F);
			}
			r.materials[0].SetColor("_Color", new Color(1, 1, 1, 0.5F));
			r.materials[1].SetColor("_Color", new Color(0, 0, 0, 1));
			RenderUtil.swapTextures(EcoceanMod.modDLL, r, "Textures/Resources/GlowOil/"+texName, new Dictionary<int, string>{{0, "Shell"}, {1, "Inner"}});
		}
		
		public static void checkPlayerLightTick(float time, Player ep) {
			if (time-lastPlayerLightCheck >= 0.25F) {
				lastPlayerLightCheck = time;
				PlayerTool pt = Inventory.main.GetHeldTool();
				if (pt && pt.energyMixin && pt.energyMixin.charge > 0) {
					if ((pt is Seaglide && ((Seaglide)pt).toggleLights.lightsActive) || (pt is FlashLight && ((FlashLight)pt).toggleLights.lightsActive)) {
						handleLightTick(MainCamera.camera.transform);
					}
				}
			}
		}
		
		public static void handleLightTick(Transform go) {
			float time = DayNightCycle.main.timePassedAsFloat;
			if (time-lastLightRaytrace >= 0.25F) {
				lastLightRaytrace = time;
				foreach (RaycastHit hit in Physics.SphereCastAll(go.position, 4F, go.forward, 180)) {
					if (hit.transform) {
						GlowOilTag g = hit.transform.GetComponentInParent<GlowOilTag>();
						if (g)
							g.onLit();
					}
				}
			}
		}
			
	}
		
	class GlowOilTag : MonoBehaviour {
		
		private Light light;
		
		private Renderer mainRender;
		private PrefabIdentifier prefab;
		private Rigidbody mainBody;
		private Collider mainHitbox;
		
		//private GameObject bubble;
		
		private float glowIntensity = 0;
		
		private float lastGlowUpdate;
		private float lastLitTime;
		private float lastRepelTime;
		
		private float lastPLayerDistanceCheckTime;
		
		private float spawnTime;
		
		private bool isExploding = false;
		
		private readonly List<GlowSeed> seeds = new List<GlowSeed>();
		private readonly List<LightCone> lightCones = new List<LightCone>();
		
		void Update() {
			if (!mainRender) {
				mainRender = GetComponentInChildren<Renderer>();
			}
			if (!mainBody) {
				mainBody = GetComponentInChildren<Rigidbody>();
			}
			if (!mainHitbox) {
				mainHitbox = GetComponentInChildren<Collider>();
			}
			if (!prefab) {
				prefab = GetComponentInChildren<PrefabIdentifier>();
			}
			if (!transform || !prefab)
				return;
			int hash = prefab.Id.GetHashCode();
			while (isNatural() && lightCones.Count < 9) {
				GameObject main = ObjectUtil.createWorldObject("4e8d9640-dd23-46ca-99f2-6924fcf250a4");
				GameObject go = ObjectUtil.getChildObject(main, "spotlight");
				if (!go)
					continue;
				Light l = go.GetComponent<Light>();
				l.intensity = 2;
				l.spotAngle = 30;
				l.range = 40;
				l.color = Color.white;
				float sc0 = UnityEngine.Random.Range(15F, 25F);
				go.transform.localScale = new Vector3(1, 1, sc0);
				go.transform.SetParent(transform);
				go.transform.localPosition = Vector3.zero;
				go.GetComponentInChildren<Renderer>().transform.localPosition = new Vector3(0, 0, -0.1F/*-1.5F/sc0*/);
				go.transform.localRotation = UnityEngine.Random.rotationUniform;
				lightCones.Add(new LightCone{go = go, light = l});
				UnityEngine.Object.DestroyImmediate(main);
			}
			while (seeds.Count < 4+((hash%5)+5)%5) {
				GameObject go = ObjectUtil.createWorldObject("18229b4b-3ed3-4b35-ae30-43b1c31a6d8d");
				if (!go)
					continue;
				RenderUtil.convertToModel(go);
				ObjectUtil.removeComponent<Collider>(go);
				ObjectUtil.removeComponent<PrefabIdentifier>(go);
				ObjectUtil.removeComponent<ChildObjectIdentifier>(go);
				go.transform.SetParent(transform);
				go.transform.localScale = Vector3.one*UnityEngine.Random.Range(0.1F, 0.15F);
				go.transform.localPosition = MathUtil.getRandomVectorAround(Vector3.zero, 0.4F);
				go.transform.localRotation = UnityEngine.Random.rotationUniform;
				Renderer r = go.GetComponentInChildren<Renderer>();
				r.materials[0].SetFloat("_Shininess", 0F);
				r.materials[0].SetFloat("_SpecInt", 0F);
				r.materials[0].SetFloat("_Fresnel", 0F);
				r.materials[0].EnableKeyword("UWE_WAVING");
				r.materials[1].EnableKeyword("UWE_WAVING");
				RenderUtil.setEmissivity(r.materials[0], 0);
				RenderUtil.setEmissivity(r.materials[1], 0);
				GlowOil.setupRenderer(r, "Seed");
				Vector3 rot = UnityEngine.Random.rotationUniform.eulerAngles.normalized*UnityEngine.Random.Range(0.75F, 1.25F);
				seeds.Add(new GlowSeed{go = go, render = r, motion = MathUtil.getRandomVectorAround(Vector3.zero, 0.07F), rotation = rot});
			}
			if (!light)
				light = GetComponentInChildren<Light>();
			float sc = (0.25F+0.05F*(float)GlowOil.sizeNoise.getValue(transform.position));
			transform.localScale = Vector3.one*sc;
			
			float time = DayNightCycle.main.timePassedAsFloat;
			float dT = time-lastGlowUpdate;
			if (dT >= 0.02F) {
				lastGlowUpdate = time;
				updateGlowStrength(time, dT);
			}
			if (spawnTime <= 0)
				spawnTime = time;
			if (spawnTime > 0 && time-lastPLayerDistanceCheckTime >= 0.5) {
				lastPLayerDistanceCheckTime = time;
				if (Player.main && Vector3.Distance(transform.position, Player.main.transform.position) > 250) {
					UnityEngine.Object.DestroyImmediate(gameObject);
				}
			}
			if (spawnTime > 0 && time-spawnTime >= 600) {
				explode();
			}
			dT = Time.deltaTime;
			if (time-lastRepelTime >= 0.5) {
				lastRepelTime = time;
				foreach (GlowOilTag g in WorldUtil.getObjectsNearWithComponent<GlowOilTag>(transform.position, 8)) {
					if (g != this && g.mainBody)
						repel(g, dT);
				}
			}
			
			foreach (GlowSeed g in seeds) {
				if (!g.go)
					continue;
				Vector3 dd = g.go.transform.localPosition;
				if (dd.HasAnyNaNs()) {
					dd = MathUtil.getRandomVectorAround(Vector3.zero, 0.4F);
					g.go.transform.localPosition = dd;
					//SNUtil.writeToChat(seeds.IndexOf(g)+" was nan pos");
				}
				else {
					//SNUtil.writeToChat(seeds.IndexOf(g)+" was NOT nan pos");
					float d = dd.sqrMagnitude;
					if (float.IsNaN(d)) {
						g.motion = MathUtil.getRandomVectorAround(Vector3.zero, 0.07F);
						//SNUtil.writeToChat(seeds.IndexOf(g)+" was nan dd - "+g.go.transform.localPosition);
						//SNUtil.log(seeds.IndexOf(g)+" was nan dd - "+g.go.transform.localPosition);
					}
					else {
						Vector3 norm = dd.normalized;
						if (norm.HasAnyNaNs()) {
							//SNUtil.writeToChat("NaN Norm");
							//SNUtil.log("NaN Norm");
							norm = Vector3.zero;
						}
						//SNUtil.writeToChat("Mot="+g.motion);
						//SNUtil.log("Mot="+g.motion);
						if (isExploding) {
							g.motion = g.motion*0.998F+norm*0.5F*dT;
						}
						else {
							g.motion = g.motion-(norm*d*6F*dT);
						}
						float maxD = 2.0F*transform.localScale.magnitude;
						if (float.IsNaN(maxD)) {
							//SNUtil.writeToChat("NaN maxD");
							//SNUtil.log("NaN maxD");
							maxD = 0;
						}
						if (!isExploding && d > maxD) {
							g.go.transform.position = norm*maxD;
						}
					}
					g.go.transform.localPosition = g.go.transform.localPosition+g.motion*dT;
					if (isExploding && g.go.transform.position.y > -0.5)
						g.go.transform.position = g.go.transform.position.setY(-0.5F);
					g.go.transform.Rotate(g.rotation, Space.Self);
				}
				RenderUtil.setEmissivity(g.render.materials[1], 0.05F+glowIntensity*8.95F);
			}
			if (isExploding) {
				mainRender.gameObject.SetActive(false);
				lastLitTime = -1;
				mainBody.velocity = Vector3.zero;
				mainBody.constraints = RigidbodyConstraints.FreezeAll;
				mainHitbox.enabled = false;
			}
			else {
				RenderUtil.setEmissivity(mainRender.materials[0], glowIntensity*5F);
			}
			
			if (light) {
				light.intensity = glowIntensity*GlowOil.MAX_GLOW;
				light.range = GlowOil.MAX_RADIUS*(0.5F+glowIntensity/2F);
				if (isNatural()) {
					light.intensity *= 1.5F;
					light.range *= 2;
				}
			}
			
			foreach (LightCone g in lightCones) {
				if (!g.go)
					continue;
				g.light.intensity = Mathf.Clamp01((glowIntensity-0.1F)*1.5F)*0.8F;
				g.light.color = Color.Lerp(Color.white, light.color, 0.5F);
				int id = g.go.GetHashCode();//g.go.GetInstanceID();
				Vector3 rotVec = new Vector3(id & 512, (id >> 9) & 512, (id >> 18) & 512).normalized;
				rotVec *= 0.5F+0.5F*Mathf.Sin(DayNightCycle.main.timePassedAsFloat*0.1F+0.137F*((id >> 27) & 32));
				g.go.transform.RotateAround(transform.position, rotVec, 0.4F*glowIntensity);
			}
		}

	    void OnCollisionEnter(Collision c) {
			//SNUtil.writeToChat("Collided with "+c.gameObject+" at speed "+c.relativeVelocity.magnitude);
	        if (spawnTime > 0 && DayNightCycle.main.timePassedAsFloat-spawnTime >= 0.1 && c.relativeVelocity.magnitude >= 6) {
				if (c.gameObject.FindAncestor<Vehicle>() || c.gameObject.FindAncestor<SubRoot>()) {
					SoundManager.playSoundAt(GlowOil.splatSound, transform.position, false, 40);
					explode();
				}
			}
	    }
		
		internal void explode() {
			if (Vector3.Distance(transform.position, Player.main.transform.position) <= 100)
				WorldUtil.spawnParticlesAt(transform.position, "0dbd3431-62cc-4dd2-82d5-7d60c71a9edf", 0.1F); //burst FX
			isExploding = true;
			UnityEngine.Object.Destroy(gameObject, 7.5F);
		}
		
		public bool isNatural() {
			return prefab && prefab.classId == EcoceanMod.naturalOil.ClassID;
		}
		
		private void updateGlowStrength(float time, float dT) {
			float delta = time-lastLitTime < 1.5F ? 0.5F : (isExploding ? -0.2F : -0.15F);
			glowIntensity = Mathf.Clamp01(glowIntensity+delta*dT);
		}
		
		internal void repel(GlowOilTag g, float dT) {
			Vector3 dd = transform.position-g.transform.position;
			//SNUtil.writeToChat("Repel from "+g.transform.position+" > "+dd);
			mainBody.AddForce(dd.normalized*(10F/dd.sqrMagnitude)*dT, ForceMode.VelocityChange);
			g.mainBody.AddForce(dd.normalized*(-10F/dd.sqrMagnitude)*dT, ForceMode.VelocityChange);
		}
		
		internal void onLit() {
			if (!isExploding)
				lastLitTime = DayNightCycle.main.timePassedAsFloat;
		}
		
		internal void onFired() {
			onLit();
			spawnTime = DayNightCycle.main.timePassedAsFloat;
		}
		
		internal void resetGlow() {
			glowIntensity = 0;
			lastLitTime = -1;
		}
		
		void OnDestroy() {
			GetComponent<ResourceTracker>().Unregister();
		}
		
		void OnDisable() {
			UnityEngine.Object.Destroy(gameObject);
		}
		
		class GlowSeed {
			
			internal GameObject go;
			internal Renderer render;
			internal Vector3 motion = Vector3.zero;
			internal Vector3 rotation = Vector3.zero;
			
		}
		
		class LightCone {
			
			internal GameObject go;
			internal Light light;
			
		}
		
	}
}
