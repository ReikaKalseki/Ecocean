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
using ReikaKalseki.Ecocean;
using SMLHelper.V2.Handlers;
using SMLHelper.V2.Utility;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using ECCLibrary;

namespace ReikaKalseki.Ecocean
{
		public abstract class PassiveSonarEntity : MonoBehaviour {
			
			private static Texture flatTexture = TextureManager.getTexture(EcoceanMod.modDLL, "Textures/reapersonarglow");
		
			//private Renderer renderer;
			
			private float forcedGlowFactor;
			
			private readonly List<Renderer> spheres = new List<Renderer>();
			
			protected float minimumDistanceSq;
			protected float maximumDistanceSq;
        	
			void Start() {
				//base.InvokeRepeating("tick", 0f, 1);
			}
			
			//protected abstract Renderer getMainRenderer();
			
			protected void Update() {
				if (!MainCamera.camera)
					return;
				/*SNUtil.log("A");
				if (!renderer) {
					renderer = getMainRenderer();
					//SNUtil.log(""+renderer);
					if (!renderer)
						return;/*
					defaultGlows = new float[renderer.materials.Length];
					defaultTextures = new Texture2D[renderer.materials.Length];
					for (int i = 0; i < renderer.materials.Length; i++) {
						defaultGlows[i] = renderer.materials[i].GetFloat("_GlowStrength");
						defaultTextures[i] = renderer.materials[i].GetTexture("_Illum");
					}*//*
				}*/
				if (spheres.Count == 0) {
					GameObject go = getSphereRootGO();
					createRadarSphere(go);
					onCreateSpheres();
				}
				//SNUtil.log("B");
				float distq = (transform.position-MainCamera.camera.transform.position).sqrMagnitude;
				setSonarRanges();
				float f = Mathf.Clamp01((distq-minimumDistanceSq)/(maximumDistanceSq-minimumDistanceSq));
				float glow = 5*f*f*f*0.9F*getIntensityFactor();
				//SNUtil.writeToChat(distq.ToString("000.0")+">"+f.ToString("0.000")+">"+glow.ToString("0000000.0")+"@"+forcedGlowFactor.ToString("0.000"));
				//SNUtil.log("C");
				foreach (Renderer r in spheres) {
					//RenderUtil.setEmissivity(renderer.materials[i], Mathf.Lerp(defaultGlows[i], glow, forcedGlowFactor), "GlowStrength");
					//renderer.materials[i].SetTexture("_Illum", glow*forcedGlowFactor > 0 ? flatTexture : defaultTextures[i]);
					float f2 = glow*forcedGlowFactor;
					r.gameObject.SetActive(f2 > 0);
					if (f2 > 0) 
						r.materials[0].EnableKeyword("FX_BUILDING");
					else
						r.materials[0].DisableKeyword("FX_BUILDING");
					RenderUtil.setEmissivity(r.materials[0], f2);
					r.materials[0].SetFloat("_Built", Mathf.Lerp(0.18F, 0.3F, f2/5F));
					//sphereRenderer.materials[i].SetInt("_Cutoff", 0);
				}
				//SNUtil.log("D");
				if (isInVehicleWithSonar()) {
					float dT = Time.deltaTime;
					if (MainCamera.camera.GetComponent<SonarScreenFX>().enabled) {
						forcedGlowFactor = Mathf.Max(0, forcedGlowFactor-0.67F*dT);
					}
					else if (isAudible() && 1.001F-getTimeVariationStrength()+Mathf.Sin(DayNightCycle.main.timePassedAsFloat*0.8F+gameObject.GetInstanceID()) > 0) { //sin is just because the sound is always "playing"
						forcedGlowFactor = Mathf.Min(1, forcedGlowFactor+2.5F*dT*getFadeRate());
					}
					else {
						forcedGlowFactor = Mathf.Max(0, forcedGlowFactor-0.33F*dT*getFadeRate());
					}
				}
				else {
					forcedGlowFactor = 0;
				}
			}
			
			protected virtual float getFadeRate() {
				return 1;
			}
			
			protected virtual float getIntensityFactor() {
				return 1;
			}
			
			protected virtual float getTimeVariationStrength() {
				return 1;
			}
			
			protected virtual GameObject getSphereRootGO() {
				return GetComponentInChildren<TrailManager>().gameObject;
			}
			
			protected abstract bool isAudible();
			
			protected bool isRoaring(FMOD_CustomEmitter emit) {/*
				if (!emit._playing || !emit.evt.hasHandle())
					return false;/*
				int ms;
				if (emit.evt.getTimelinePosition(out ms) == FMOD.RESULT.OK) {
					SNUtil.writeToChat(ms+"ms for "+emit.GetType().Name);
					return ms >= 0 && ms <= 1500;
				}*//*
				return false;*/
					return emit && emit.playing;
			}
			
			protected virtual void onCreateSpheres() {
				
			}
			
			protected virtual void setSonarRanges() {
				float m = 80;
				float x = 150;
				float day = DayNightCycle.main.GetLightScalar();
				if (VanillaBiomes.MOUNTAINS.isInBiome(transform.position)) {
					m += 30+50*day;
					x += 50+100*day;
				}
				else if (VanillaBiomes.DUNES.isInBiome(transform.position)) {
					m += 20+20*day;
					x += 50+50*day;
				}
				else if (VanillaBiomes.ILZ.isInBiome(transform.position)) {
					m = 100;
					x = 200;
				}
				minimumDistanceSq = m*m;
				maximumDistanceSq = x*x*0.8F;
			}
			
			protected void createRadarSphere(GameObject go, float scale = 1) {
				createRadarSphere(go, new Vector3(scale, scale, scale));
			}
			
			protected void createRadarSphere(GameObject go, Vector3 scale) {
				SNUtil.log("Creating radar sphere for "+go.GetFullHierarchyPath());
				GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
				sphere.transform.localScale = Vector3.Scale(getRadarSphereSize(), scale);
				sphere.name = "RadarHalo";
				sphere.transform.SetParent(go.transform);
				sphere.transform.localPosition = getRadarSphereOffset();
				ObjectUtil.removeComponent<Collider>(sphere);
				ECCHelpers.ApplySNShaders(sphere, new UBERMaterialProperties(0, 0, 5));
				Renderer r = sphere.GetComponentInChildren<Renderer>();
				r.receiveShadows = false;
				r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
				r.materials[0].SetTexture("_Illum", flatTexture);
				r.materials[0].SetFloat("_Built", 0.3F);
				r.materials[0].SetFloat("_BuildLinear", 0.1F);
				r.materials[0].SetFloat("_NoiseThickness", 1F);
				r.materials[0].SetFloat("_NoiseStr", 1F);
				r.materials[0].SetColor("_BorderColor", new Color(2.5F, 0.4F, 0.5F, 1));
				r.materials[0].SetVector("_BuildParams", new Vector4(0.1F, 0.62F, 0.03F, 0.1F));
				spheres.Add(r);
				
				foreach (Transform child in go.transform) {
					if (child == go.transform || child.gameObject == sphere || child.GetComponent<ParticleSystem>())
						continue;
					createRadarSphere(child.gameObject, scale);
				}
			}
			
			protected virtual Vector3 getRadarSphereSize() {
				return Vector3.one*15;
			}
			
			protected virtual Vector3 getRadarSphereOffset() {
				return Vector3.zero;
			}
			
			private bool isInVehicleWithSonar() {
				if (Player.main) {
					Vehicle v = Player.main.GetVehicle();
					//SNUtil.writeToChat(v.activeSlot+" > "+(v.activeSlot >= 0 ? ""+v.GetSlotItem(v.activeSlot).item : "null")); 
					if (InventoryUtil.isVehicleUpgradeSelected(v, TechType.SeamothSonarModule))
						return true;
					SubRoot sub = Player.main.currentSub;
					if (sub && sub.isCyclops && InventoryUtil.cyclopsHasUpgrade(sub, TechType.CyclopsSonarModule))
						return true;
				}
				return false;
			}

			private void OnKill() {
				UnityEngine.Object.Destroy(this);
			}
			
			void OnDisable() {
				//base.CancelInvoke("tick");
			}
			
			internal void fireRoar() {
				forcedGlowFactor = 1;
			}
			
		}
}
