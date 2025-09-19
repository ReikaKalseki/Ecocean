using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

using ReikaKalseki.DIAlterra;

using SMLHelper.V2.Handlers;
using SMLHelper.V2.Utility;

using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace ReikaKalseki.Ecocean {
	public class PredatoryBloodvine : MonoBehaviour {

		public static event Action<PredatoryBloodvine, GameObject> onBloodKelpGrabEvent;

		private static readonly float DURATION = 10F;
		private static readonly float SNAP_TIME = 0.2F;
		private static readonly float RELEASE_TIME = 2-SNAP_TIME;
		private static readonly float RELEASE_START = DURATION-RELEASE_TIME;

		private FruitPlant fruiter;

		private GameObject topGO;
		private SphereCollider triggerBox;
		private Vector3 centerTarget;

		private float eatStart = -1;

		private LiveMixin target;

		private float lastTime;

		private float topScale = 1;

		void Start() {
			topGO = gameObject.getChildObject("*_end");
			triggerBox = topGO.AddComponent<SphereCollider>();
			triggerBox.center = new Vector3(0, 0, 6);//topGO.transform.position;
			triggerBox.radius = 5.2F;
			triggerBox.isTrigger = true;
			centerTarget = topGO.transform.position + (Vector3.up * 7.75F);
			topGO.EnsureComponent<PredatoryBloodvineTop>().root = this;
			fruiter = this.GetComponent<FruitPlant>();
		}

		void Update() {
			float time = DayNightCycle.main.timePassedAsFloat;
			float dt = time-eatStart;
			if (dt <= DURATION) {
				//SNUtil.writeToChat("dt: "+dt.ToString("00.000"));
				topScale = dt <= SNAP_TIME
					? 1 - (dt * 0.5F / SNAP_TIME)
					: dt > DURATION - RELEASE_TIME ? 0.5F + ((dt - RELEASE_START) / RELEASE_TIME * 0.5F) : 0.5F;
			}
			else {
				target = null;
				topScale = 1;
			}
			if (!Mathf.Approximately(topScale, transform.localScale.x))
				topGO.transform.localScale = new Vector3(topScale, topScale, 1F / topScale); //axes on this are weird
			float dT = time-lastTime;
			if (target && dT > 0) {
				target.TakeDamage(dT * 1.2F, target.transform.position, DamageType.Puncture, gameObject);
				if (fruiter)
					fruiter.timeNextFruit -= dT * 6;
				if (!target.GetComponent<SubRoot>()) {
					Vector3 tgt = centerTarget;
					if (target.GetComponent<Vehicle>())
						tgt += Vector3.up;
					Vector3 dd = tgt-target.transform.position;
					if (dd.sqrMagnitude <= 2.25F) {
						target.transform.position = tgt;
					}
					else {
						Rigidbody rb = target.GetComponent<Rigidbody>();
						if (rb) {
							float f = dT*50F*Mathf.Clamp01(dd.sqrMagnitude*dd.sqrMagnitude/2);
							if (target.GetComponent<Vehicle>())
								f *= 4;
							//SNUtil.writeToChat("Bloodvine at "+transform.position+" pulling "+target+" dist="+dd.magnitude+" > force = "+f);
							rb.AddForce(dd.normalized * f, ForceMode.VelocityChange);
						}
					}
				}
			}
			lastTime = time;
		}

		public void release() {
			if (!target)
				return;
			target = null;
			eatStart = DayNightCycle.main.timePassedAsFloat - DURATION + RELEASE_TIME;
			//SNUtil.writeToChat(eatStart.ToString("00.000"));
		}

		internal bool canAttack(GameObject go, out LiveMixin live) {
			live = go.FindAncestor<LiveMixin>();
			if (gameObject.FindAncestor<WaterPark>())
				return false;
			if (!live || !live.IsAlive())
				return false;
			Player p = go.FindAncestor<Player>();
			if (gameObject.FindAncestor<Planter>() && (p || go.FindAncestor<Vehicle>()))
				return false;
			if (p && !p.IsSwimming())
				return false;
			Creature c = go.FindAncestor<Creature>();
			return !(c is ReaperLeviathan) && !(c is GhostLeviathan) && !(c is GhostLeviatanVoid) && !(c is SeaDragon) && !(c is Reefback);
		}

		internal void trigger(GameObject go) {
			float time = DayNightCycle.main.timePassedAsFloat;
			if (time - eatStart <= DURATION + 2)
				return;
			if (!this.canAttack(go, out LiveMixin live))
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
			//SNUtil.writeToChat(eatStart.ToString("00.000"));
			target.gameObject.SendMessage("OnBloodKelpGrab", this, SendMessageOptions.DontRequireReceiver);
			live.TakeDamage(amt, go.transform.position, DamageType.Puncture, gameObject);
			if (onBloodKelpGrabEvent != null)
				onBloodKelpGrabEvent.Invoke(this, go);
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
