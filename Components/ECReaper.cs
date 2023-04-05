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
		internal class ECReaper : MonoBehaviour {
		
			//private static float[] defaultGlows;
			//private static Texture[] defaultTextures;
			
			private static Texture flatTexture = TextureManager.getTexture(EcoceanMod.modDLL, "Textures/reapersonarglow");
		
			private Renderer renderer;
			private FMOD_CustomLoopingEmitter roar1;
			private FMOD_CustomLoopingEmitterWithCallback roar2;
			
			private float forcedGlowFactor;
			
			private readonly List<Renderer> spheres = new List<Renderer>();
        	
			void Start() {
				//base.InvokeRepeating("tick", 0f, 1);
			}
			
			void Update() {
				if (!MainCamera.camera)
					return;
				//SNUtil.log("A");
				if (!renderer) {
					renderer = ObjectUtil.getChildObject(gameObject, "reaper_leviathan/Reaper_Leviathan_geo").GetComponentInChildren<Renderer>();
					//SNUtil.log(""+renderer);
					if (!renderer)
						return;/*
					defaultGlows = new float[renderer.materials.Length];
					defaultTextures = new Texture2D[renderer.materials.Length];
					for (int i = 0; i < renderer.materials.Length; i++) {
						defaultGlows[i] = renderer.materials[i].GetFloat("_GlowStrength");
						defaultTextures[i] = renderer.materials[i].GetTexture("_Illum");
					}*/
				}
				if (!roar1) {
					foreach (FMOD_CustomLoopingEmitter em in GetComponents<FMOD_CustomLoopingEmitter>()) {
						if (em.asset != null && em.asset.path.Contains("idle")) {
							roar1 = em;
							break;
						}
					}
					roar2 = GetComponent<FMOD_CustomLoopingEmitterWithCallback>();
				}
				if (spheres.Count == 0) {
					GameObject go = GetComponentInChildren<TrailManager>().gameObject;
					createRadarSphere(go);
				}
				//SNUtil.log("B");
				float distq = (transform.position-MainCamera.camera.transform.position).sqrMagnitude;
				float f = Mathf.Clamp01((distq-14400F)/(900F));
				float glow = 5*f*f*f;
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
					RenderUtil.setEmissivity(r.materials[0], f2, "GlowStrength");
					r.materials[0].SetFloat("_Built", Mathf.Lerp(0.18F, 0.3F, f2/5F));
					//sphereRenderer.materials[i].SetInt("_Cutoff", 0);
				}
				//SNUtil.log("D");
				if (isInVehicleWithSonar()) {
					float dT = Time.deltaTime;
					if (MainCamera.camera.GetComponent<SonarScreenFX>().enabled) {
						forcedGlowFactor = Mathf.Max(0, forcedGlowFactor-0.67F*dT);
					}
					else if ((isRoaring(roar1) || isRoaring(roar2)) && Mathf.Sin(DayNightCycle.main.timePassedAsFloat*0.8F+gameObject.GetInstanceID()) > 0) { //sin is just because the sound is always "playing"
						forcedGlowFactor = Mathf.Min(1, forcedGlowFactor+2.5F*dT);
					}
					else {
						forcedGlowFactor = Mathf.Max(0, forcedGlowFactor-0.33F*dT);
					}
				}
				else {
					forcedGlowFactor = 0;
				}
			}
			
			private void createRadarSphere(GameObject go) {
				SNUtil.log("Creating radar sphere for "+go.GetFullHierarchyPath());
				GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
				sphere.transform.localScale = new Vector3(15, 15, 15);
				sphere.name = "RadarHalo";
				sphere.transform.SetParent(go.transform);
				sphere.transform.localPosition = Vector3.zero;
				ObjectUtil.removeComponent<Collider>(sphere);
				ECCHelpers.ApplySNShaders(sphere, new UBERMaterialProperties(0, 0, 5));
				Renderer r = sphere.GetComponentInChildren<Renderer>();
				r.materials[0].SetTexture("_Illum", flatTexture);
				r.materials[0].SetFloat("_Built", 0.3F);
				r.materials[0].SetFloat("_BuildLinear", 0.1F);
				r.materials[0].SetFloat("_NoiseThickness", 1F);
				r.materials[0].SetFloat("_NoiseStr", 1F);
				r.materials[0].SetColor("_BorderColor", new Color(2.5F, 0.4F, 0.5F, 1));
				r.materials[0].SetVector("_BuildParams", new Vector4(0.1F, 0.62F, 0.03F, 0.1F));
				spheres.Add(r);
				
				foreach (Transform child in go.transform) {
					if (child == go.transform || child.gameObject == sphere)
						continue;
					createRadarSphere(child.gameObject);
				}
			}
			
			private bool isRoaring(FMOD_CustomEmitter emit) {/*
				if (!emit._playing || !emit.evt.hasHandle())
					return false;/*
				int ms;
				if (emit.evt.getTimelinePosition(out ms) == FMOD.RESULT.OK) {
					SNUtil.writeToChat(ms+"ms for "+emit.GetType().Name);
					return ms >= 0 && ms <= 1500;
				}*//*
				return false;*/
					return emit.playing;
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
