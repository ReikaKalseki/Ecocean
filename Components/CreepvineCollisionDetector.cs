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
	internal class CreepvineCollisionDetector : MonoBehaviour {

		private float lastImpact = -1;

		void OnTriggerEnter(Collider other) {
			float time = DayNightCycle.main.timePassedAsFloat;
			//SNUtil.writeToChat(time+" vs "+lastImpact);
			if (!other.isTrigger && time - lastImpact > 1.5F) {
				SeaMoth sm = other.gameObject.FindAncestor<SeaMoth>();
				if (sm) {
					Rigidbody rb = sm.GetComponent<Rigidbody>();
					float speed = rb.velocity.magnitude;
					if (speed > 5) {
						float seeds = 0;
						int seedCount = 0;
						foreach (PickPrefab pp in gameObject.FindAncestor<PrefabIdentifier>().GetComponentsInChildren<PickPrefab>()) {
							if (!pp.GetPickedState()) {
								seeds++;
							}
							seedCount++;
						}
						seeds /= seedCount;
						if (seeds > 0 && UnityEngine.Random.Range(0F, 1F) < seeds) {
							lastImpact = time;
							float f = Mathf.Pow(speed-5, 1.5F)*0.3F;
							//SNUtil.writeToChat(speed+" > "+f);
							SoundManager.playSoundAt(GlowOil.splatSound, transform.position, false, 40);
							sm.liveMixin.TakeDamage(2 * f, transform.position, DamageType.Collide, gameObject);
							foreach (PickPrefab pp in gameObject.FindAncestor<PrefabIdentifier>().GetComponentsInChildren<PickPrefab>()) {
								if (!pp.GetPickedState() && UnityEngine.Random.Range(0F, 1F) < 0.5F + f) {
									GameObject go = CraftData.GetPrefabForTechType(pp.pickTech);
									pp.SetPickedUp();
									if (go) {
										GameObject go2 = UnityEngine.Object.Instantiate(go);
										go2.transform.position = MathUtil.getRandomVectorAround(transform.position, 1.5F);
										go2.transform.localScale = Vector3.one * 2.5F;
										Rigidbody seed = go2.GetComponent<Rigidbody>();
										if (seed) {
											seed.isKinematic = false;
											seed.AddForce(MathUtil.getRandomVectorAround(rb.velocity, 0.5F) * 2, ForceMode.VelocityChange);
										}
										go2.SetActive(true);
									}
								}
							}
						}
					}
				}
			}
		}

		internal static GameObject addCreepvineSeedCollision(GameObject go) {
			//FruitPlant fp = go.GetComponent<FruitPlant>(); average of centers
			GameObject light = go.GetComponentInChildren<Light>().gameObject;
			GameObject put = light.getChildObject("SeedSphere");
			if (!put) {
				put = new GameObject("SeedSphere");
				put.transform.SetParent(light.transform);
			}
			Utils.ZeroTransform(put.transform);
			SphereCollider sc = put.EnsureComponent<SphereCollider>();
			sc.radius = 1.5F;
			sc.center = Vector3.zero;
			sc.isTrigger = true;
			put.EnsureComponent<CreepvineCollisionDetector>();
			return put;
		}

	}
}
