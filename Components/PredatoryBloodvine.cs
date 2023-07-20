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
	internal class PredatoryBloodvine : MonoBehaviour {
		
		private static readonly float DURATION = 10F;
		private static readonly float SNAP_TIME = 0.2F;
		private static readonly float RELEASE_TIME = 2-SNAP_TIME;
		private static readonly float RELEASE_START = DURATION-RELEASE_TIME;
		
		private GameObject topGO;
		private SphereCollider triggerBox;
		private Vector3 centerTarget;
		
		private float eatStart = -1;
		
		private LiveMixin target;
		
		private float lastTime;
		
		private float topScale = 1;
        	
		void Start() {
			topGO = ObjectUtil.getChildObject(gameObject, "*_end");
			triggerBox = topGO.AddComponent<SphereCollider>();
			triggerBox.center = new Vector3(0, 0, 6);//topGO.transform.position;
			triggerBox.radius = 5.2F;
			triggerBox.isTrigger = true;
			centerTarget = topGO.transform.position+Vector3.up*7.5F;
			topGO.EnsureComponent<PredatoryBloodvineTop>().root = this;
		}
			
		void Update() {
			float time = DayNightCycle.main.timePassedAsFloat;
			float dt = time-eatStart;
			if (dt <= DURATION) {
				if (dt <= SNAP_TIME) {
					topScale = 1-(dt*0.5F/SNAP_TIME);
				}
				else if (dt > DURATION-RELEASE_TIME) {
					topScale = 0.5F+(dt-RELEASE_START)/RELEASE_TIME*0.5F;
				}
				else {
					topScale = 0.5F;
				}
			}
			else {
				target = null;
				topScale = 1;
			}
			if (!Mathf.Approximately(topScale, transform.localScale.x))
				topGO.transform.localScale = new Vector3(topScale, topScale, 1F/topScale); //axes on this are weird
			float dT = time-lastTime;
			if (target && dT > 0) {
				target.TakeDamage(dT*1.2F, target.transform.position, DamageType.Puncture, gameObject);
				if (!target.GetComponent<SubRoot>()) {
					Vector3 dd = centerTarget-target.transform.position;
					if (dd.sqrMagnitude <= 2.25F) {
						target.transform.position = centerTarget;
					}
					else {
						Rigidbody rb = target.GetComponent<Rigidbody>();
						if (rb) {
							float f = dT*50F*Mathf.Clamp01(dd.sqrMagnitude*dd.sqrMagnitude/2);
							if (target.GetComponent<Vehicle>())
								f *= 4;
							//SNUtil.writeToChat("Bloodvine at "+transform.position+" pulling "+target+" dist="+dd.magnitude+" > force = "+f);
							rb.AddForce(dd.normalized*f, ForceMode.VelocityChange);
						}
					}
				}
			}
			lastTime = time;
		}
		
		internal bool canAttack(GameObject go, out LiveMixin live) {
			live = go.FindAncestor<LiveMixin>();
			if (gameObject.FindAncestor<WaterPark>())
				return false;
			if (!live || !live.IsAlive())
				return false;
			Player p = go.FindAncestor<Player>();
			if (p && gameObject.FindAncestor<Planter>())
				return false;
			if (p && !p.IsSwimming())
				return false;
			return true;
		}
		
		internal void trigger(GameObject go) {
			float time = DayNightCycle.main.timePassedAsFloat;
			if (time-eatStart <= 4)
				return;
			LiveMixin live;
			if (!canAttack(go, out live))
				return;
			//if (go.GetComponent<SubRoot>())
			//	return;
			float amt = 5;
			Vehicle v = go.GetComponent<Vehicle>();
			if (v) {
				if (v.docked)
					return;
				else
					amt *= 2;
			}
			amt *= EcoceanMod.config.getFloat(ECConfig.ConfigEntries.BLOODDMG);
			target = live;
			eatStart = time;
			live.TakeDamage(amt, go.transform.position, DamageType.Puncture, gameObject);
			SoundManager.playSoundAt(SoundManager.buildSound("event:/creature/reaper/attack_player_claw"), centerTarget, false, 40);
		}
/*
	    void OnCollisionEnter(Collision c) {
			GameObject collider = c.gameObject;
			//SNUtil.writeToChat("Collided with "+c.collider);
	        if (collider)
	        	trigger(collider);
	    }
*/			
	}
	
	internal class PredatoryBloodvineTop : MonoBehaviour {
		
		internal PredatoryBloodvine root;
		
	    void OnTriggerEnter(Collider other) {
			if (!other.isTrigger)
	        	root.trigger(other.gameObject);
	    }
		
		void Update() {
			if (!root) {
				root = gameObject.FindAncestor<PredatoryBloodvine>();
			}
		}
		
	}
}
