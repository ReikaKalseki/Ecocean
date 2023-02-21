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
			GameObject world = ObjectUtil.createWorldObject(VanillaResources.DIAMOND.prefab);
			world.EnsureComponent<TechTag>().type = TechType;
			world.EnsureComponent<PrefabIdentifier>().ClassId = ClassID;
			world.EnsureComponent<LargeWorldEntity>().cellLevel = LargeWorldEntity.CellLevel.VeryFar;
			world.EnsureComponent<PiezoCrystalTag>();
			ObjectUtil.removeComponent<Pickupable>(world);
			ObjectUtil.removeComponent<ResourceTracker>(world);
			ObjectUtil.removeComponent<ResourceTrackerUpdater>(world);
			
			Renderer r = world.GetComponentInChildren<Renderer>();
			RenderUtil.swapTextures(EcoceanMod.modDLL, r, "Textures/Piezo/");
			r.materials[0].SetFloat("_SpecInt", 300);
			r.materials[0].SetFloat("_Shininess", 8);
			r.materials[0].SetFloat("_Fresnel", 0.4F);
			r.materials[0].SetColor("_Color", new Color(1, 1, 1, 1));
			
			return world;
	    }
		
		public void register() {
			Patch();
			SNUtil.addPDAEntry(this, 8, "PlanetaryGeology", locale.pda, locale.getField<string>("header"));
			ItemRegistry.instance.addItem(this);
			//GenUtil.registerSlotWorldgen(ClassID, PrefabFileName, TechType, EntitySlot.Type.Creature, LargeWorldEntity.CellLevel.Far, BiomeType.CragField_OpenDeep_CreatureOnly, 1, 1F);
		}
			
	}
		
	public class PiezoCrystalTag : MonoBehaviour {
		
		private MeshRenderer render;
		
		private GameObject sparker;
		
		private ParticleSystem[] particles;
		
		private float lastDischargeTime;
		private float nextDischargeTime;
		
		private float nextSparkAdjust = -1;
		
		private static readonly SoundManager.SoundData dischargeSound = SoundManager.registerSound(EcoceanMod.modDLL, "piezoblast", "Sounds/piezo.ogg", SoundManager.soundMode3D);
		
		internal PiezoCrystalTag() {
			
		}
		
		void Update() {
			if (!render) {
				render = GetComponentInChildren<MeshRenderer>();
			}
			if (!sparker) {
				sparker = ObjectUtil.createWorldObject("ff8e782e-e6f3-40a6-9837-d5b6dcce92bc");
				sparker.transform.parent = transform;
				//sparker.transform.eulerAngles = new Vector3(325, 180, 0);
				ObjectUtil.removeComponent<DamagePlayerInRadius>(sparker);
				ObjectUtil.removeComponent<PlayerDistanceTracker>(sparker);
				//ObjectUtil.removeChildObject(sparker, "ElecLight");
				
				sparker.transform.localPosition = new Vector3(-0.15F, 0.5F, 0);
			}
			transform.localScale = Vector3.one*8;
			if (particles == null) {
				particles = sparker.GetComponentsInChildren<ParticleSystem>();
			}
			
			float time = DayNightCycle.main.timePassedAsFloat;
			render.materials[0].SetFloat("_SpecInt", (float)MathUtil.linterpolate(DayNightCycle.main.GetLightScalar(), 0, 1, 300, 15));
			if (lastDischargeTime <= 1) {
				lastDischargeTime = time;
				nextDischargeTime = time+UnityEngine.Random.Range(20F, 45F);
				return;
			}
			float charge = (float)MathUtil.linterpolate(time, lastDischargeTime, nextDischargeTime, 0, 1, true);
			
			charge += Time.deltaTime*UnityEngine.Random.Range(0.033F, 0.1F);
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
		
			if (charge >= 1) {
				lastDischargeTime = time;
				nextDischargeTime = time+UnityEngine.Random.Range(20F, 45F);
				if (Vector3.Distance(Player.main.transform.position, transform.position) <= 200)
					spawnEMP();
			}
		}
		
		void OnDestroy() {
			
		}
		
		void OnDisable() {
			
		}
	    
	    internal void spawnEMP() {
			SoundManager.playSoundAt(dischargeSound, sparker.transform.position, false, -1, 1);
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
	    	if (Vector3.Distance(sparker.transform.position, ep.transform.position) <= 30) {
	    		ep.GetComponent<LiveMixin>().TakeDamage(UnityEngine.Random.Range(5F, 10F), ep.transform.position, DamageType.Electrical, gameObject);
	    	}
	    }
		
	}
}
