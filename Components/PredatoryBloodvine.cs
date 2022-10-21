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
		
		private GameObject topGO;
		private SphereCollider triggerBox;
		private Vector3 centerTarget;
		
		private float eatStart = -1;
		
		private GameObject target;
		
		private float lastTime;
        	
		void Start() {
			topGO = ObjectUtil.getChildObject(gameObject, "*_end");
			triggerBox = topGO.AddComponent<SphereCollider>();
			triggerBox.center = new Vector3(0, 0, 6);//topGO.transform.position;
			triggerBox.radius = 2.0F;
			triggerBox.isTrigger = true;
			centerTarget = topGO.transform.position+Vector3.up*7.5F;
			topGO.EnsureComponent<PredatoryBloodvineTop>().root = this;
		}
			
		void Update() {
			float time = DayNightCycle.main.timePassedAsFloat;
			float sc = 1;
			float dt = time-eatStart;
			if (dt <= 2) {
				if (dt <= 0.2F) {
					sc = 1-(dt*2.5F);
				}
				else {
					sc = 0.5F+(dt-0.2F)/1.8F*0.5F;
				}
			}
			else {
				target = null;
			}
			topGO.transform.localScale = new Vector3(sc, sc, 1F/sc); //axes on this are weird
			float dT = time-lastTime;
			if (target && dT > 0) {
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
			float amt = 10;
			Vehicle v = go.GetComponent<Vehicle>();
			if (v) {
				if (v.docked)
					return;
				else
					amt *= 2;
			}
			amt *= EcoceanMod.config.getFloat(ECConfig.ConfigEntries.BLOODDMG);
			target = go;
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
	        root.trigger(other.gameObject);
	    }
		
		void Update() {
			if (!root) {
				root = gameObject.FindAncestor<PredatoryBloodvine>();
			}
		}
		
	}
}
