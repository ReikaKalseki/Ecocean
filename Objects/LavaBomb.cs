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
		
		internal static readonly float HEAT_RADIUS = 30;
		internal static readonly float MAX_TEMPERATURE = 2000;
		
		private readonly XMLLocale.LocaleEntry locale;
	        
	    internal LavaBomb(XMLLocale.LocaleEntry e) : base(e.key, e.name, e.desc) {
			locale = e;
	    }
			
	    public override GameObject GetGameObject() {
			GameObject world = ObjectUtil.createWorldObject("18229b4b-3ed3-4b35-ae30-43b1c31a6d8d");
			world.EnsureComponent<TechTag>().type = TechType;
			world.EnsureComponent<PrefabIdentifier>().ClassId = ClassID;
			//world.GetComponent<Rigidbody>().isKinematic = true;
			WorldForces wf = world.GetComponent<WorldForces>();
			wf.underwaterGravity *= 2.0F;
			wf.underwaterDrag *= 0.7F;
			Rigidbody rb = world.GetComponent<Rigidbody>();
			//ObjectUtil.removeComponent<EnzymeBall>(world);
			ObjectUtil.removeComponent<Plantable>(world);
			ObjectUtil.removeComponent<Pickupable>(world);
			ObjectUtil.removeComponent<PickPrefab>(world);
			LavaBombTag g = world.EnsureComponent<LavaBombTag>();
			Light l = ObjectUtil.addLight(world);
			l.bounceIntensity *= 3;
			l.color = new Color(1F, 0.67F, 0.1F, 1F);
			l.intensity = 2;
			l.range = 8;
			Renderer r = world.GetComponentInChildren<Renderer>();
			RenderUtil.swapTextures(EcoceanMod.modDLL, r, "Textures/LavaBomb/", new Dictionary<int, string>{{0, "Shell"}, {1, "Inner"}});
			/*
			RenderUtil.setEmissivity(r.materials[0], 0, "GlowStrength");
			RenderUtil.setEmissivity(r.materials[1], 0, "GlowStrength");
			r.materials[0].SetFloat("_Shininess", 10);
			r.materials[0].SetFloat("_SpecInt", 3);
			r.materials[0].SetFloat("_Fresnel", 1);
			setupRenderer(r, "Main");
			RenderUtil.makeTransparent(r.materials[1]);
			r.materials[0].EnableKeyword("FX_KELP");
			r.materials[0].SetColor("_Color", new Color(0, 0, 0, 1F));*/
			return world;
	    }
			
	}
		
	class LavaBombTag : MonoBehaviour {
		
		private Light light;
		
		private Renderer mainRender;
		private PrefabIdentifier prefab;
		private Rigidbody mainBody;
		
		private float temperature;
		
		private static readonly Color glowNew;
		private static readonly Color glowFinal;
		
		void Update() {
			if (!mainRender)
				mainRender = GetComponentInChildren<Renderer>();
			if (!mainBody)
				mainBody = GetComponentInChildren<Rigidbody>();
			if (!prefab)
				prefab = GetComponentInChildren<PrefabIdentifier>();
			if (!light)
				light = GetComponentInChildren<Light>();
			
			transform.localScale = Vector3.one*2.5F;
			
			float time = DayNightCycle.main.timePassedAsFloat;
			
			temperature = Mathf.Max(600, temperature-Time.deltaTime*80);
			
			float f = getIntensity();
			if (light)
				light.intensity = UnityEngine.Random.Range(1.8F, 2.2F)*f;
			RenderUtil.setEmissivity(mainRender.materials[0], f*2, "GlowStrength");
			RenderUtil.setEmissivity(mainRender.materials[1], (0.5F+f*0.5F)*2, "GlowStrength");
			mainRender.materials[0].SetColor("_GlowColor", getColor(f));
			mainRender.materials[1].SetColor("_GlowColor", Color.Lerp(Color.white, Color.red, 1-f));
		}
		
		internal Color getColor(float f) {
			return Color.Lerp(glowNew, glowFinal, 1-f);
		}
		
		internal void onFired() {
			temperature = LavaBomb.MAX_TEMPERATURE;
		}
		
		internal float getIntensity() {
			return temperature/LavaBomb.MAX_TEMPERATURE;
		}

	    void OnCollisionEnter(Collision c) {
			//SNUtil.writeToChat("Collided at speed "+c.relativeVelocity.magnitude);
			GameObject collider = c.gameObject;
			if (c.relativeVelocity.magnitude >= 2) {
	        	explode(collider);
			}
	    }
		
		internal void explode(GameObject impacted) {
			SoundManager.playSoundAt(SoundManager.buildSound("event:/env/background/debris_fall_fire"), transform.position, false, 40);
			RaycastHit[] hit = Physics.SphereCastAll(transform.position, 35, new Vector3(1, 1, 1), 35);
			HashSet<int> used = new HashSet<int>();
			foreach (RaycastHit rh in hit) {
				if (rh.transform != null && rh.transform.gameObject) {
					if (used.Contains(rh.transform.gameObject.GetInstanceID()))
						continue;
					bool wasHit = rh.transform.gameObject == impacted;
					used.Add(rh.transform.gameObject.GetInstanceID());
					Player p = rh.transform.GetComponent<Player>();
					if (p && !p.IsSwimming())
						continue;
					LiveMixin lv = rh.transform.GetComponent<LiveMixin>();
					if (lv && lv.IsAlive()) {
						float amt = wasHit ? 100 : 20;
						SubRoot sub = rh.transform.GetComponent<SubRoot>();
						if (sub && sub.isCyclops)
							amt = wasHit ? 150 : 45;
						Vehicle v = rh.transform.GetComponent<Vehicle>();
						if (v && v is SeaMoth)
							amt = wasHit ? 60 : 18;
						else if (v && v is Exosuit)
							amt = wasHit ? 100 : 35;
						if (!wasHit) {
							float f = (Vector3.Distance(rh.transform.position, transform.position)-10)/35F;
							amt *= Mathf.Clamp01(1.5F-f*f);
						}
						amt *= 0.5F+0.5F*getIntensity();
						lv.TakeDamage(amt, rh.transform.position, DamageType.Heat, gameObject);
					}
				}
			}
			UnityEngine.Object.Destroy(this);
	        	//TODO particles, spawn an emitter which dies soon after
		}
		
	}
}
