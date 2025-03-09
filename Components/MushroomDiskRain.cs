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

namespace ReikaKalseki.Ecocean {
	
	public class MushroomDiskRain : MonoBehaviour {
		
		public static readonly Color color1 = new Color(0.4F, 1.0F, 1.5F, 1F);
		public static readonly Color color2 = new Color(1.8F, 1.1F, 0.5F, 1F);
		
		private BoxCollider collider;
		private Color renderColor;
		private ParticleSystem particles;
		
		public static bool locked = false;
			
		private bool rainOn;
		
		void Start() {
			toggleOff();
		}
			
		private void prepare() {
			renderColor = transform.position.x > 0 ? color2 : color1;
			BoxCollider refC = transform.parent.GetComponentInChildren<BoxCollider>();
			collider = gameObject.EnsureComponent<BoxCollider>();
			collider.isTrigger = true;
			collider.size = new Vector3(refC.size.x*0.9F, refC.size.y*8, refC.size.z*0.9F);//new Vector3(0.6F, 1.6F, 0.6F);
			collider.center = refC.center+Vector3.down*2F;//Vector3.down * 0.8F;
			GameObject fx = ObjectUtil.getChildObject(gameObject, "Particles");
			if (!fx) {
				fx = UnityEngine.Object.Instantiate(ObjectUtil.lookupPrefab("0e67804e-4a59-449d-929a-cd3fc2bef82c").GetComponent<ParticleSystem>().gameObject);
				ObjectUtil.removeComponent<Creature>(fx);
				ObjectUtil.removeComponent<BloomCreature>(fx);
				ObjectUtil.removeComponent<SwimRandom>(fx);
				ObjectUtil.removeComponent<StayAtLeashPosition>(fx);
				ObjectUtil.removeComponent<CreatureUtils>(fx);
				ObjectUtil.removeComponent<LiveMixin>(fx);
				ObjectUtil.removeComponent<BehaviourLOD>(fx);
				ObjectUtil.removeComponent<LastScarePosition>(fx);
				ObjectUtil.removeComponent<WorldForces>(fx);
				ObjectUtil.removeComponent<SwimBehaviour>(fx);
				ObjectUtil.removeComponent<SplineFollowing>(fx);
				ObjectUtil.removeComponent<Locomotion>(fx);
				ObjectUtil.removeComponent<Rigidbody>(fx);
				ObjectUtil.removeComponent<PrefabIdentifier>(fx);
				ObjectUtil.removeComponent<EntityTag>(fx);
				ObjectUtil.removeComponent<TechTag>(fx);
				ObjectUtil.removeComponent<LargeWorldEntity>(fx);
				fx.name = "Particles";
				fx.transform.SetParent(transform);
			}
			foreach (Transform t in fx.transform)
				UnityEngine.Object.Destroy(t.gameObject);
			fx.transform.localPosition = collider.center;//+Vector3.down*0.7F;
			particles = fx.GetComponent<ParticleSystem>();
			ParticleSystem.MainModule main = particles.main;
			main.gravityModifier = 0.2F;
			ParticleSystem.EmissionModule emit = particles.emission;
			ParticleSystem.ShapeModule sh = particles.shape;
			ParticleSystem.ColorOverLifetimeModule clr = particles.colorOverLifetime;
			sh.shapeType = ParticleSystemShapeType.Circle;
			sh.rotation = new Vector3(0, 0, 0);
			sh.radius = Mathf.Max(collider.size.x, collider.size.z)*1.2F;
			Color c = renderColor.exponent(1.25F).WithAlpha(1);
			clr.color = c;
			main.startColor = c;
			emit.rateOverTimeMultiplier = 2.5F;
		}
			
		public void toggleOn() {
			if (!collider)
				prepare();
			if (locked)
				return;
			rainOn = true;
			collider.enabled = true;
			particles.Play(true);
			Invoke("toggleOff", UnityEngine.Random.Range(1F, 6F));
			CancelInvoke("toggleOn");
		}
			
		public void toggleOff() {
			if (locked)
				return;
			rainOn = false;
			Invoke("toggleOn", UnityEngine.Random.Range(5F, 90F));
			CancelInvoke("toggleOff");
			if (!collider || !particles)
				return;
			collider.enabled = false;
			particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
		}
			
		void OnTriggerStay(Collider other) {
			if (rainOn && !other.isTrigger) {
				if (ObjectUtil.isPlayer(other)) {
					FoodEffectSystem.VisualDistortionEffect e = Player.main.gameObject.EnsureComponent<FoodEffectSystem.VisualDistortionEffect>();
					e.intensity = 2;
					e.timeRemaining = 10;
					e.effectColor = renderColor.toVectorA().exponent(4F);
					e.tintIntensity = 0.32F; //0.28
					e.tintColor = (renderColor.exponent(2) * 4).WithAlpha(1);
				}
				else {
					SeaMoth sm = other.gameObject.FindAncestor<SeaMoth>();
					if (sm) {
						SeamothPlanktonScoop.checkAndTryScoop(sm, Time.deltaTime, EcoceanMod.treeMushroomSpores.TechType);
					}
					PlanktonClearingArea area = other.gameObject.FindAncestor<PlanktonClearingArea>();
					if (area) {
						area.tickExternal();
					}
				}
			}
		}
			
	}
}
