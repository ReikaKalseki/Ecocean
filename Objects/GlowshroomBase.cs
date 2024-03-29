﻿using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

using UnityEngine;

using SMLHelper.V2.Handlers;
using SMLHelper.V2.Assets;
using SMLHelper.V2.Utility;

using ReikaKalseki.DIAlterra;

namespace ReikaKalseki.Ecocean {
	
	public abstract class GlowshroomBase<T> : BasicCustomPlant, MultiTexturePrefab where T : GlowShroomTagBase {
		
		public GlowshroomBase(string localeKey) : base(EcoceanMod.locale.getEntry(localeKey), new FloraPrefabFetch(VanillaFlora.JELLYSHROOM_LIVE), "7fcf1275-0687-491e-a086-d928dd3ba67a") {
			glowIntensity = 1.5F;
			finalCutBonus = 0;
			OnFinishedPatching += () => {
				SaveSystem.addSaveHandler(ClassID, new SaveSystem.ComponentFieldSaveHandler<GlowShroomTagBase>().addField("lastEmitTime"));
			};
		}
		
		public override Vector2int SizeInInventory {
			get {return new Vector2int(2, 2);}
		}
		
		public abstract Color getLightColor();
		
		public override void prepareGameObject(GameObject go, Renderer[] r) {
			base.prepareGameObject(go, r);
			ObjectUtil.removeComponent<CrabsnakeMushroom>(go);
			ObjectUtil.removeComponent<PrefabPlaceholder>(go);
			ObjectUtil.removeComponent<PrefabPlaceholdersGroup>(go);
			ObjectUtil.removeComponent<EcoTarget>(go);
			ObjectUtil.removeChildObject(go, "CrabsnakeSpawnPoint");
			ObjectUtil.removeChildObject(go, "Jellyshroom_Loot_InsideShroom");
			ObjectUtil.removeChildObject(go, "Jellyshroom_Creature_CrabSnake");
			if (!go.GetComponentInChildren<Light>()) {
				Light l = ObjectUtil.addLight(go);
				l.color = getLightColor();
				l.range = 30F;
				l.intensity = 2;
				l.gameObject.transform.localPosition = Vector3.up*5;
			}
			go.EnsureComponent<T>();
		}
		
		public Dictionary<int, string> getTextureLayers(Renderer r) {/*
			bool hasGlow = r.materials.Length > 1;
			N = N.Substring(N.LastIndexOf('_')+1).Trim();
			return hasGlow ? new Dictionary<int, string>{{0, "Trunk_"+N}, {1, "Cap_"+N}} : new Dictionary<int, string>{{0, "Inner_"+N}};
			*/
			Dictionary<int, string> ret = new Dictionary<int, string>();
			for (int i = 0; i < r.materials.Length; i++) {
				string N = r.materials[i].name.Replace("(Instance)", "");
				N = N.Substring(N.LastIndexOf('_')+1).Trim();
				if (!N.Contains("hat")) {
					N = N.Replace("small", "05");
					if (i == 1) { //wrong tex on grown plant
						N = "hat_small";
					}
				}
				ret[i] = N;
			}
			//ReikaKalseki.DIAlterra.RenderUtil.dumpTexture(ReikaKalseki.DIAlterra.SNUtil.diDLL, "captex", (Texture2D)m.mainTexture);
			//SNUtil.log("Loading texture dict "+ret.toDebugString<int, string>());
			return ret;
		}

		public override sealed string getTextureFolder() {
			return Path.Combine(base.getTextureFolder(), getTextureSubfolder());
		}
		
		protected abstract string getTextureSubfolder();
		
		public override sealed float getScaleInGrowbed(bool indoors) {
			return 1F;
		}

		public override bool isResource() {
			return false;
		}
		
		public override sealed Plantable.PlantSize getSize() {
			return Plantable.PlantSize.Large;
		}
		/*
		public override float getGrowthTime() {
			return 6000; //5x
		}*/
		
	}
	
	public abstract class GlowShroomTagBase : MonoBehaviour {
		
		private static readonly SoundManager.SoundData fireSound = SoundManager.registerSound(EcoceanMod.modDLL, "glowshroomfire", "Sounds/glowshroom-fire.ogg", SoundManager.soundMode3D, s => {SoundManager.setup3D(s, 40);}, SoundSystem.masterBus);
		
		protected Renderer[] renderers;
		protected Light[] lights;
		
		protected bool isGrown;
		
		private float lastEmitTime;
		private float nextEmitTime;
		
		protected virtual void init() {
			
		}
		
		protected virtual void tick() {
			
		}
		
		protected abstract float getMinimumAllowableDepth();
		
		void Start() {
			isGrown = gameObject.GetComponent<GrownPlant>() != null;
			if (!isGrown && gameObject.transform.position.y > -getMinimumAllowableDepth())
    			UnityEngine.Object.Destroy(gameObject);
    		else if (isGrown) {
    			gameObject.SetActive(true);
				//gameObject.transform.localScale = Vector3.one*UnityEngine.Random.Range(0.125F, 0.15F)*getSize();
				setModel();
    		}
    		else {
				gameObject.transform.localScale = Vector3.one*UnityEngine.Random.Range(0.75F, 1F)*getSize();
    		}
    		init();
		}
		
		private void setModel() {
			GameObject pfb = ObjectUtil.lookupPrefab("3e199d12-2d75-4c58-a819-d78beeb24e2c");
			Animator a = GetComponentInChildren<Animator>();
			if (a) {
				MeshRenderer r = pfb.GetComponentInChildren<MeshRenderer>();
				GameObject mdl = UnityEngine.Object.Instantiate(r.gameObject);
				mdl.transform.SetParent(transform);
				mdl.transform.localPosition = a.transform.localPosition;
				mdl.transform.localRotation = Quaternion.Euler(-90, a.transform.localEulerAngles.y, 0);
				mdl.transform.localScale = a.transform.localScale;
				RenderUtil.swapToModdedTextures(mdl.GetComponentInChildren<Renderer>(), getPrefab());
				UnityEngine.Object.Destroy(a.gameObject);
			}
			GameObject coll = ObjectUtil.getChildObject(gameObject, "collision");
			if (coll) {
				GameObject cap = ObjectUtil.getChildObject(pfb, "Capsule");
				GameObject coll2 = UnityEngine.Object.Instantiate(cap);
				coll2.transform.SetParent(transform);
				coll2.transform.localPosition = coll.transform.localPosition;
				coll2.transform.localRotation = Quaternion.Euler(-90, coll.transform.localEulerAngles.y, 0);
				coll2.transform.localScale = coll.transform.localScale;
				UnityEngine.Object.Destroy(coll);
			}
		}
		
		protected abstract DIPrefab<FloraPrefabFetch> getPrefab();
		
		void Update() {
			if (renderers == null) {
				renderers = GetComponentsInChildren<Renderer>();
			}
			if (lights == null) {
				lights = GetComponentsInChildren<Light>();
			}
			
			float time = DayNightCycle.main.timePassedAsFloat;
			if (isGrown) { //0.5 is the max it reaches before the quick burst before firing
				float sp = 1+0.4F*Mathf.Cos((0.2F*transform.position.magnitude)%(600*Mathf.PI));
				float tt = (sp*time+gameObject.GetHashCode())%(200*Mathf.PI);
				float lt = Mathf.Sin(tt)+0.33F*Mathf.Sin(tt*3.93F+2367.2F);
				setBrightness(0.5F+0.125F*lt);
			}
			else {
				float dT = nextEmitTime-time;
				if (dT <= 0 && Vector3.Distance(transform.position, Player.main.transform.position) <= getFireDistance()) {
					emit(time);
				}
				else {
					float dT2 = time-lastEmitTime;
					if (dT <= 0)
						setBrightness(1);
					else if (dT <= 1)
						setBrightness(1-dT/2);
					else if (dT <= 20)
						setBrightness(0.5F-dT/40F);
					else if (dT2 <= 1)
						setBrightness(1-dT2);
					else
						setBrightness(0);
				}
			}
			tick();
		}
		
		internal void fireAsap() {
			nextEmitTime = Mathf.Min(nextEmitTime, DayNightCycle.main.timePassedAsFloat+15);
		}
		
		private void setBrightness(float f) {
			if (lights != null) {
				foreach (Light l in lights) {
					l.intensity = (isGrown ? 1 : 2)*f;
				}
			}
			if (renderers != null) {
				foreach (Renderer r in renderers) {
					if (!r)
						continue;
					if (r.materials.Length > 1) { //outer stem and cap
						RenderUtil.setEmissivity(r.materials[0], 0.75F+f*0.5F);
						RenderUtil.setEmissivity(r.materials[1], 0.4F+f*3.6F);
					}
					else { //inner
						RenderUtil.setEmissivity(r.materials[0], f);
					}
				}
			}
			updateBrightness(f);
		}
		
		protected virtual void updateBrightness(float f) {
			
		}
		
		public float getLastFiredTime() {
			return lastEmitTime;
		}
		
		private void emit(float time) {
			lastEmitTime = time;
			nextEmitTime = time+getNextFireInterval();
			GameObject go = createProjectile();
			ObjectUtil.fullyEnable(go);
			ObjectUtil.ignoreCollisions(go, gameObject);
			go.transform.position = transform.position+transform.up*3.5F*transform.localScale.magnitude*(1+1.5F*Mathf.Clamp01((Vector3.Distance(transform.position, Player.main.transform.position)-60)/100F));
			Rigidbody rb = go.GetComponent<Rigidbody>();
			rb.isKinematic = false;
			rb.angularVelocity = MathUtil.getRandomVectorAround(Vector3.zero, 15);
			Vector3 vec = MathUtil.getRandomVectorAround(transform.up.normalized*UnityEngine.Random.Range(10F, 15F)*getFireVelocity(), 0.5F);
			rb.AddForce(vec, ForceMode.VelocityChange);
			SoundManager.playSoundAt(fireSound, transform.position, false, 40);
			onFire(go);
			//rb.drag = go.GetComponent<WorldForces>().underwaterDrag;
		}
		
		internal virtual void onFire(GameObject go) {
			
		}
		
		protected virtual float getSize() {
			return 1;
		}
		
		protected virtual float getFireDistance() {
			return 300;
		}
		
		protected virtual float getNextFireInterval() {
			return 1;
		}
		
		protected virtual float getFireVelocity() {
			return 1;
		}
		
		protected abstract GameObject createProjectile();
		
	}
}
