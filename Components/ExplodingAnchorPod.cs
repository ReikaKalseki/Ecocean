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

namespace ReikaKalseki.Ecocean
{
	internal class ExplodingAnchorPod : MonoBehaviour {
		
		private static readonly SoundManager.SoundData explosionSound = SoundManager.registerSound(EcoceanMod.modDLL, "reefpodexplode", "Sounds/reefpod.ogg", SoundManager.soundMode3D);
		
		//model/stone_mid_01
		//model/stone_mid_02
		//model/Coral_reef_floating_stones_big_02/stone_big_02
		
		private static readonly float REGEN_DURATION = 15;
		
		private bool isExploded;
		private bool isGrown;
		
		private float lastExplodeTime;
		private float lastRegenTime;
		private float explodeIn = -1;
		
		private GameObject podGO;
		
		private Vector3 effectivePodCenter;
		
		private Collider[] colliders;
        	
		void Start() {
			podGO = ObjectUtil.getChildObject(gameObject, "stone_*");
			colliders = gameObject.GetComponentsInChildren<Collider>();
			effectivePodCenter = transform.position+Vector3.up*17.5F;
		}
			
		void Update() {
			float time = DayNightCycle.main.timePassedAsFloat;
			if (podGO.activeSelf) {
				float f = Mathf.Clamp01((time-lastRegenTime)/REGEN_DURATION);
				podGO.transform.localPosition = Vector3.up*(1-f)*15;
				podGO.transform.localScale = Vector3.one*f;
				isGrown = f >= 1;
			}
			if (isExploded) {
				isGrown = false;
				if (UnityEngine.Random.Range(0F, 1F) <= 0.01F) {
					isExploded = false;
					podGO.SetActive(true);
					foreach (Collider c in colliders) {
						c.enabled = true;
						c.gameObject.SetActive(true);
					}
					lastRegenTime = time;
				}
			}
			else if (!isExploded && isGrown && time-lastRegenTime >= 10) {
				if ((explodeIn > 0 && time >= explodeIn && isPlayerInRange(2)) || (UnityEngine.Random.Range(0F, 1F) <= 0.000004F && isPlayerInRange()))
					explode();
			}
		}
		
		private bool isPlayerInRange(float sc = 1) {
			return Vector3.Distance(Player.main.transform.position, transform.position) <= 120*sc;
		}
		
		private void scheduleExplode(float sec) {
			explodeIn = DayNightCycle.main.timePassedAsFloat+sec;
		}

	    void OnCollisionEnter(Collision c) {
			//SNUtil.writeToChat("Collided at speed "+c.relativeVelocity.magnitude);
			GameObject collider = c.gameObject;
			if (collider.GetComponent<Player>())
				return;
			float thresh = 4;
			SubRoot sub = collider.GetComponent<SubRoot>();
			if (sub && sub.isCyclops)
				thresh = 0;
	        if (!isExploded && isGrown && c.relativeVelocity.magnitude >= thresh && isPlayerInRange())
	        	explode();
	    }
		
		internal void explode() {
			float time = DayNightCycle.main.timePassedAsFloat;
			if (isExploded || !isGrown || time-lastRegenTime < 10)
				return;
			explodeIn = -1;
			lastExplodeTime = time;
			isExploded = true;
			SoundManager.playSoundAt(explosionSound, effectivePodCenter, false, 64);
			podGO.SetActive(false);
			foreach (Collider c in colliders) {
				c.enabled = false;
				c.gameObject.SetActive(false);
			}
			RaycastHit[] hit = Physics.SphereCastAll(effectivePodCenter, 35, new Vector3(1, 1, 1), 35);
			HashSet<int> used = new HashSet<int>();
			foreach (RaycastHit rh in hit) {
				if (rh.transform != null && rh.transform.gameObject) {
					if (used.Contains(rh.transform.gameObject.GetInstanceID()))
						continue;
					used.Add(rh.transform.gameObject.GetInstanceID());
					Player p = rh.transform.GetComponent<Player>();
					if (p && !p.IsSwimming())
						continue;
					float dd = Vector3.Distance(rh.transform.position, effectivePodCenter);
					ExplodingAnchorPod pod = rh.transform.GetComponent<ExplodingAnchorPod>();
					if (pod && !pod.isExploded && pod.isGrown && UnityEngine.Random.Range(0F, 1F) <= 0.5F*Mathf.Max(0, 1-dd/30F)) {
						pod.scheduleExplode(UnityEngine.Random.Range(0.2F, 0.67F));
						continue;
					}
					LiveMixin lv = rh.transform.GetComponent<LiveMixin>();
					if (lv && lv.IsAlive()) {
						float amt = 15;
						SubRoot sub = rh.transform.GetComponent<SubRoot>();
						if (sub && sub.isCyclops)
							amt = 60;
						Vehicle v = rh.transform.GetComponent<Vehicle>();
						if (v && v is SeaMoth)
							amt = 20;
						else if (v && v is Exosuit)
							amt = 50;
						float f = (dd-10)/35F;
						amt *= Mathf.Clamp01(1.5F-f*f);
						amt *= EcoceanMod.config.getFloat(ECConfig.ConfigEntries.ANCHORDMG);
						lv.TakeDamage(amt, rh.transform.position, DamageType.Explosive, gameObject);
						Rigidbody rb = rh.transform.GetComponent<Rigidbody>();
						if (rb) {
							Vector3 vec = rh.transform.position-effectivePodCenter;
							rb.AddForce(vec.normalized*140/vec.magnitude, ForceMode.VelocityChange);
						}
					}
				}
			}
		}
		
	}
}
