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
	
	public class PiezoCrystal : Spawnable {
		
		private readonly XMLLocale.LocaleEntry locale;
	        
	    internal PiezoCrystal(XMLLocale.LocaleEntry e) : base(e.key, e.name, e.desc) {
			locale = e;			
	    }
			
	    public override GameObject GetGameObject() {
			GameObject world = ObjectUtil.createWorldObject(/*VanillaResources.DIAMOND.prefab*/"30fb51ee-73b6-4609-8e02-2804201987fb");
			ObjectUtil.removeChildObject(world, "Starship_exploded_debris_13");
			ObjectUtil.removeChildObject(world, "Cube");
			
			int n = UnityEngine.Random.Range(4, 7); //4-6 horizontal
			Vector3[] angs = new Vector3[n+2];
			for (int i = 0; i < n; i++) {
				float ang = (360F/n)*i;
				ang += UnityEngine.Random.Range(-15F, 15F);
				angs[i] = new Vector3(UnityEngine.Random.Range(0, 360F), ang, UnityEngine.Random.Range(60F, 120F));
			}
			angs[angs.Length-1] = new Vector3(UnityEngine.Random.Range(-30F, 30F), UnityEngine.Random.Range(0, 360F), 0);
			angs[angs.Length-2] = new Vector3(UnityEngine.Random.Range(150F, 210F), UnityEngine.Random.Range(0, 360F), 0);
			foreach (Vector3 ang in angs) {
				GameObject go = ObjectUtil.createWorldObject(VanillaResources.DIAMOND.prefab);
				go.transform.SetParent(world.transform);
				go.transform.localPosition = Vector3.zero;
				go.transform.localRotation = Quaternion.Euler(ang.x, ang.y, ang.z);//UnityEngine.Random.rotationUniform;
				go.transform.localPosition = go.transform.localPosition+go.transform.up*UnityEngine.Random.Range(0.25F, 0.4F)/8F;
				go.name = "CrystalChunk";
				ObjectUtil.removeComponent<PrefabIdentifier>(go);
				ObjectUtil.removeComponent<LargeWorldEntity>(go);
				ObjectUtil.removeComponent<EntityTag>(go);
				ObjectUtil.removeComponent<WorldForces>(go);
				ObjectUtil.removeComponent<Pickupable>(go);
				ObjectUtil.removeComponent<ResourceTracker>(go);
				ObjectUtil.removeComponent<ResourceTrackerUpdater>(go);
			}
			world.EnsureComponent<TechTag>().type = TechType;
			world.EnsureComponent<PrefabIdentifier>().ClassId = ClassID;
			world.EnsureComponent<LargeWorldEntity>().cellLevel = LargeWorldEntity.CellLevel.VeryFar;
			world.EnsureComponent<PiezoCrystalTag>();
			
			foreach (Renderer r in world.GetComponentsInChildren<Renderer>()) {
				RenderUtil.swapTextures(EcoceanMod.modDLL, r, "Textures/Piezo/");
				r.materials[0].SetFloat("_SpecInt", 300);
				r.materials[0].SetFloat("_Shininess", 8);
				r.materials[0].SetFloat("_Fresnel", 0.4F);
				r.materials[0].SetColor("_Color", new Color(1, 1, 1, 1));
			}
			
			return world;
	    }
		
		public void register() {
			Patch();
			SNUtil.addPDAEntry(this, 8, "PlanetaryGeology", locale.pda, locale.getField<string>("header"));
			ItemRegistry.instance.addItem(this);
			//GenUtil.registerSlotWorldgen(ClassID, PrefabFileName, TechType, EntitySlot.Type.Medium, LargeWorldEntity.CellLevel.VeryFar, BiomeType.UnderwaterIslands_IslandSides, 1, 1F);
		}
			
	}
		
	public class PiezoCrystalTag : MonoBehaviour {
		
		private readonly List<PiezoSource> sources = new List<PiezoSource>();
				
		private float lastDischargeTime;
		private float nextDischargeTime;
		
		private static readonly SoundManager.SoundData dischargeSound = SoundManager.registerSound(EcoceanMod.modDLL, "piezoblast", "Sounds/piezo.ogg", SoundManager.soundMode3D);
		
		internal PiezoCrystalTag() {
			
		}
		
		void Update() {
			if (sources.Count == 0) {
				MeshRenderer[] rs = GetComponentsInChildren<MeshRenderer>();
				foreach (MeshRenderer r in rs) {
					sources.Add(new PiezoSource(this, r));
				}
			}
			transform.localScale = Vector3.one*8;			
			float time = DayNightCycle.main.timePassedAsFloat;
			foreach (PiezoSource r in sources)
				r.render.materials[0].SetFloat("_SpecInt", (float)MathUtil.linterpolate(DayNightCycle.main.GetLightScalar(), 0, 1, 300, 15));
			if (lastDischargeTime <= 1) {
				lastDischargeTime = time;
				nextDischargeTime = time+UnityEngine.Random.Range(20F, 45F);
				return;
			}
			float charge = (float)MathUtil.linterpolate(time, lastDischargeTime, nextDischargeTime, 0, 1, true);
			foreach (PiezoSource r in sources) {
				r.updateCharge(time, charge);
			}		
			if (charge >= 1) {
				lastDischargeTime = time;
				nextDischargeTime = time+UnityEngine.Random.Range(20F, 45F);
				if (Vector3.Distance(Player.main.transform.position, transform.position) <= 200)
					spawnEMP();
				foreach (PiezoSource r in sources) {
					r.resetIntensities();
				}
			}
		}
	    
		internal void spawnEMP() {
			SoundManager.playSoundAt(dischargeSound, transform.position, false, -1, 1);
		   	GameObject pfb = ObjectUtil.lookupPrefab(VanillaCreatures.CRABSQUID.prefab).GetComponent<EMPAttack>().ammoPrefab;
		   	for (int i = 0; i < 180; i += 30) {
				GameObject emp = UnityEngine.Object.Instantiate(pfb);
		    	emp.transform.position = transform.position;
		    	emp.transform.localRotation = Quaternion.Euler(i, 0, 0);
		    	Renderer r = emp.GetComponentInChildren<Renderer>();
		    	r.materials[0].color = new Color(84/255F, 206/255F, 1F, 1F);
		    	r.materials[1].color = new Color(0.4F, 0.6F, 1F, 1F);
		    	r.materials[0].SetColor("_ColorStrength", new Color(1, 1, 100, 1));
		    	r.materials[1].SetColor("_ColorStrength", new Color(1, 1, 100, 1));
		    	EMPBlast e = emp.GetComponent<EMPBlast>();
		    	ObjectUtil.removeComponent<VFXLerpColor>(emp);
		    	e.blastRadius = AnimationCurve.Linear(0f, 0f, 1f, 40f);
		    	e.blastHeight = AnimationCurve.Linear(0f, 0f, 1f, 40f);
		    	e.lifeTime = 0.33F;
		    	e.disableElectronicsTime = 0;//UnityEngine.Random.Range(1F, 5F);
		    	emp.name = "PiezoCrystal_EMPulse"+i;
		    	emp.SetActive(true);
		   	}
		   	Player ep = Player.main;
		   	if (Vector3.Distance(transform.position, ep.transform.position) <= 30) {
		   		ep.GetComponent<LiveMixin>().TakeDamage(UnityEngine.Random.Range(5F, 10F), ep.transform.position, DamageType.Electrical, gameObject);
		   	}
		}
		
		void OnDestroy() {
			
		}
		
		void OnDisable() {
			
		}
		
		class PiezoSource {
			
			internal readonly PiezoCrystalTag component;
			internal readonly MeshRenderer render;
			internal readonly GameObject sparker;		
			internal readonly ParticleSystem[] particles;
			
			private float intensityOffset = 1;
		
			private float nextSparkAdjust = -1;
			
			internal PiezoSource(PiezoCrystalTag tag, MeshRenderer r) {
				component = tag;
				render = r;
				
				resetIntensities();
				
				sparker = ObjectUtil.createWorldObject("ff8e782e-e6f3-40a6-9837-d5b6dcce92bc");
				particles = sparker.GetComponentsInChildren<ParticleSystem>();
				sparker.transform.SetParent(render.transform.parent);
				sparker.transform.localPosition = new Vector3(-0.15F, 0.5F, 0);
				//sparker.transform.eulerAngles = new Vector3(325, 180, 0);
				ObjectUtil.removeComponent<DamagePlayerInRadius>(sparker);
				ObjectUtil.removeComponent<PlayerDistanceTracker>(sparker);
				//ObjectUtil.removeChildObject(sparker, "ElecLight");
			}
			
			internal void resetIntensities() {
				intensityOffset = UnityEngine.Random.Range(0.8F, 1.25F);
			}
			
			internal void updateCharge(float time, float charge) {
				charge = Mathf.Clamp01(charge*intensityOffset);
				float f = (charge-0.5F)*2;
				if (time >= nextSparkAdjust) {
					sparker.SetActive(f > 0 && UnityEngine.Random.Range(0F, 1F) < f);
					nextSparkAdjust = time+0.2F;
				}
				foreach (ParticleSystem p in particles) {
					ParticleSystem.MainModule pm = p.main;
					pm.startSize = Mathf.Max(0.1F, f*12F);
					//SNUtil.writeToChat(""+f*15F);
				}
				float f2 = Mathf.Max(0.25F, f*2*UnityEngine.Random.Range(0.9F, 1F));
				if (UnityEngine.Random.Range(0F, 1F) < 0.2F)
					f2 = Mathf.Min(f2, UnityEngine.Random.Range(0.25F, 0.5F));
				RenderUtil.setEmissivity(render, f2, "GlowStrength");
			}
			
		}
		
	}
}
