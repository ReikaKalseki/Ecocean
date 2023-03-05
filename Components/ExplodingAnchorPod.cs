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
		
		private float lastExplodeTime;
		private float lastRegenTime;
		private float explodeIn = -1;
		
		private GameObject podGO;
		private Material podTexture;
		
		private Vector3 effectivePodCenter;
		
		private Collider[] colliders;
        	
		void Start() {
			podGO = ObjectUtil.getChildObject(gameObject, "stone_*");
			podTexture = podGO.GetComponentInChildren<Renderer>().materials[0];
			colliders = gameObject.GetComponentsInChildren<Collider>();
			effectivePodCenter = transform.position+Vector3.up*17.5F;
		}
			
		void Update() {
			float time = DayNightCycle.main.timePassedAsFloat;
			if (isExploded) {
				if (UnityEngine.Random.Range(0F, 1F) <= 0.01F && time-lastExplodeTime >= 4) {
					isExploded = false;
					Invoke("showPod", 2.5F);
					//lastRegenTime = time;
					spawnParticleShell("f39e56b9-9a11-4582-875f-c37f1ed37314"/*"a5b073a5-4bce-4bcf-8aaf-1e7f57851ba0"*/, 2, Vector3.down*2);
				}
			}
			else if (!isExploded && time-lastRegenTime >= 10) {
				if ((explodeIn > 0 && time >= explodeIn && isPlayerInRange(2)) || (UnityEngine.Random.Range(0F, 1F) <= 0.0000015F && canExplodeRandom()))
					explode();
			}
		}
		
		void showPod() {
			//podGO.SetActive(true);
			podTexture.DisableKeyword("FX_BURST");
			foreach (Collider c in colliders) {
				c.enabled = true;
				c.gameObject.SetActive(true);
			}
		}
		
		private bool isPlayerInRange(float sc = 1) {
			return Vector3.Distance(Player.main.transform.position, transform.position) <= 120*sc;
		}
		
		private bool canExplodeRandom() {
			return isPlayerInRange(transform.position.y <= -500 ? 0.4F : 1);
		}
		
		private void scheduleExplode(float sec) {
			explodeIn = DayNightCycle.main.timePassedAsFloat+sec;
		}

	    void OnCollisionEnter(Collision c) {
			//SNUtil.writeToChat("Collided at speed "+c.relativeVelocity.magnitude);
			GameObject collider = c.gameObject;
			if (collider.gameObject.FindAncestor<Player>())
				return;
			float thresh = 4;
			Creature cc = collider.gameObject.FindAncestor<Creature>();
			if (cc)
				thresh = cc is ReaperLeviathan || cc is GhostLeviathan || cc is GhostLeviatanVoid ? 6 : 10;
			SubRoot sub = collider.gameObject.FindAncestor<SubRoot>();
			if (sub && sub.isCyclops)
				thresh = 1F;
	        if (!isExploded && c.relativeVelocity.magnitude >= thresh && isPlayerInRange())
	        	explode();
	    }
		
		private void spawnParticleShell(string prefab, float dur, Vector3 offset) {
			if (Vector3.Distance(transform.position, Player.main.transform.position) <= 100) {
				for (int i = 0; i < 8; i++) {
					Vector3 pos = MathUtil.getRandomVectorAround(effectivePodCenter+Vector3.down*3.5F+offset, 7.5F);
					ParticleSystem go = WorldUtil.spawnParticlesAt(pos, prefab, dur); //burst FX
					if (go) {
						ParticleSystem.SizeOverLifetimeModule sz = go.sizeOverLifetime;
						sz.sizeMultiplier *= 2;
					}
					//if (go)
					//	go.transform.localScale = Vector3.one*4;
				}
			}
		}
		
		internal void explode() {
			if (!canExplode())
				return;
			spawnParticleShell("f39e56b9-9a11-4582-875f-c37f1ed37314", 0.5F, Vector3.zero);
			Invoke("explodePart2", 0.5F);
		}
		
		internal bool canExplode() {
			return !gameObject.GetFullHierarchyPath().Contains("ACUDecoHolder") && transform.position.y <= -100;
		}
		
		internal void explodePart2() {
			float time = DayNightCycle.main.timePassedAsFloat;
			if (isExploded || time-lastRegenTime < 10)
				return;
			explodeIn = -1;
			lastExplodeTime = time;
			isExploded = true;
			//podGO.SetActive(false);
			podTexture.EnableKeyword("FX_BURST");
			foreach (Collider c in colliders) {
				c.enabled = false;
				c.gameObject.SetActive(false);
			}
			GameObject coral;
			UWE.PrefabDatabase.TryGetPrefab("171c6a5b-879b-4785-be7a-6584b2c8c442", out coral);
			IntermittentInstantiate ii = coral.GetComponent<IntermittentInstantiate>();
			GameObject bubble = ii.prefab;
			int n = UnityEngine.Random.Range(8, 12);
			for (int i = 0; i < n; i++) {
				Vector3 pos = MathUtil.getRandomVectorAround(effectivePodCenter, 4F);
				GameObject go = UnityEngine.Object.Instantiate(bubble);
				go.transform.position = pos;
				float f = UnityEngine.Random.Range(1.5F, 3F);
				go.transform.localScale = Vector3.one*f;
				Bubble b = go.GetComponent<Bubble>();
				b.oxygenSeconds *= f;
			}
			SoundManager.playSoundAt(explosionSound, effectivePodCenter, false, 64);
			HashSet<GameObject> set = WorldUtil.getObjectsNear(transform.position, 35);
			HashSet<int> used = new HashSet<int>();
			foreach (GameObject go in set) {
				if (used.Contains(go.GetInstanceID()))
					continue;
				used.Add(go.GetInstanceID());
				Player p = go.GetComponent<Player>();
				if (p && !p.IsSwimming())
					continue;
				float dd = Vector3.Distance(go.transform.position, effectivePodCenter);
				ExplodingAnchorPod pod = go.GetComponent<ExplodingAnchorPod>();
				if (pod && !pod.isExploded && UnityEngine.Random.Range(0F, 1F) <= 0.5F*Mathf.Max(0, 1-dd/30F)) {
					pod.scheduleExplode(UnityEngine.Random.Range(0.2F, 0.67F));
					continue;
				}
				LiveMixin lv = go.GetComponent<LiveMixin>();
				if (lv && lv.IsAlive()) {
					float amt = getDamageToDeal(go);
					if (amt < 0.1F)
						continue;
					float f = (dd-10)/35F;
					amt *= Mathf.Clamp01(1.5F-f*f);
					float depth2 = (-(go.transform.position.y))-400;
					amt *= 1+depth2/400F;
					amt *= EcoceanMod.config.getFloat(ECConfig.ConfigEntries.ANCHORDMG);
					lv.TakeDamage(amt, go.transform.position, DamageType.Explosive, gameObject);
					Rigidbody rb = go.GetComponent<Rigidbody>();
					if (rb) {
						Vector3 vec = go.transform.position-effectivePodCenter;
						rb.AddForce(vec.normalized*140/vec.magnitude, ForceMode.VelocityChange);
					}
				}
			}
		}
		
		private float getDamageToDeal(GameObject go) {
			BaseCell b = go.GetComponent<BaseCell>();
			if (b)
				return 10;
			Constructable c = go.FindAncestor<Constructable>();
			if (c)
				return 0;//c.constructedAmount >= 1 && ObjectUtil.isBaseModule(c.techType, false) ? 10 : 0;
			SubRoot sub = go.GetComponent<SubRoot>();
			if (sub)
				return sub.isCyclops ? 30 : 0;
			Vehicle v = go.GetComponent<Vehicle>();
			if (v && v is SeaMoth)
				return 15;
			else if (v && v is Exosuit)
				return 30;
			return 15;
		}
		
	}
}
