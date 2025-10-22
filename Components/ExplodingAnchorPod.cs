using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
	public class ExplodingAnchorPod : PassiveSonarEntity {

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
		private Renderer[] podRenders;

		private Vector3 effectivePodCenter;

		private Collider[] colliders;

		public static event Action<ExplodingAnchorPodDamage> onExplodingAnchorPodDamageEvent;

		void Start() {
			podGO = gameObject.getChildObject("stone_*");
			podRenders = podGO.GetComponentsInChildren<Renderer>();
			colliders = gameObject.GetComponentsInChildren<Collider>();
			effectivePodCenter = podRenders.First(r => !ObjectUtil.isLODRenderer(r)).bounds.center;//transform.position+Vector3.up*17.5F;
		}

		new void Update() {
			base.Update();
			float time = DayNightCycle.main.timePassedAsFloat;
			if (isExploded) {
				if (UnityEngine.Random.Range(0F, 1F) <= 0.01F && time - lastExplodeTime >= 4) {
					isExploded = false;
					this.Invoke("showPod", 2.5F);
					//lastRegenTime = time;
					this.spawnParticleShell("f39e56b9-9a11-4582-875f-c37f1ed37314"/*"a5b073a5-4bce-4bcf-8aaf-1e7f57851ba0"*/, 2, Vector3.down * 2);
				}
			}
			else if (!isExploded && time - lastRegenTime >= 10) {
				if ((explodeIn > 0 && time >= explodeIn && this.isPlayerInRange(2)) || (UnityEngine.Random.Range(0F, 1F) <= 0.0000015F && this.canExplodeRandom()))
					this.explode();
			}
		}

		public Vector3 getEffectivePodCenter() {
			return effectivePodCenter;
		}

		protected override GameObject getSphereRootGO() {
			return podGO;
		}

		protected override void setSonarRanges() {
			minimumDistanceSq = 20 * 20;
			maximumDistanceSq = 50 * 50;
		}

		protected override bool isAudible() {
			return DayNightCycle.main.timePassedAsFloat - lastExplodeTime <= 2.5F;
		}

		protected override float getFadeRate() {
			return 5;
		}

		protected override float getTimeVariationStrength() {
			return 0;
		}

		protected override Vector3 getRadarSphereSize() {
			return new Vector3(15, 15, 15);
		}

		protected override Vector3 getRadarSphereOffset() {
			return effectivePodCenter - transform.position;
		}

		void showPod() {
			//podGO.SetActive(true);
			foreach (Renderer r in podRenders) {
				foreach (Material m in r.materials)
					m.DisableKeyword("FX_BURST");
			}
			foreach (Collider c in colliders) {
				c.enabled = true;
				c.gameObject.SetActive(true);
			}
		}

		private bool isPlayerInRange(float sc = 1) {
			return Vector3.Distance(Player.main.transform.position, transform.position) <= 120 * sc;
		}

		private bool canExplodeRandom() {
			return this.isPlayerInRange(transform.position.y <= -500 ? 0.4F : 1);
		}

		internal void scheduleExplode(float sec) {
			explodeIn = DayNightCycle.main.timePassedAsFloat + sec;
		}

		void OnCollisionEnter(Collision c) {
			//SNUtil.writeToChat("Collided at speed "+c.relativeVelocity.magnitude);
			if (c.collider.isPlayer())
				return;
			GameObject collider = c.gameObject;
			float thresh = 4;
			Creature cc = collider.gameObject.FindAncestor<Creature>();
			if (cc)
				thresh = cc is ReaperLeviathan || cc is GhostLeviathan/* || cc is GhostLeviatanVoid*/ ? 6 : 10;
			SubRoot sub = collider.gameObject.FindAncestor<SubRoot>();
			if (sub && sub.isCyclops)
				thresh = 1F;
			if (!isExploded && c.relativeVelocity.magnitude >= thresh && this.isPlayerInRange())
				this.explode();
		}

		private void spawnParticleShell(string prefab, float dur, Vector3 offset) {
			if (Vector3.Distance(transform.position, Player.main.transform.position) <= 100) {
				for (int i = 0; i < 8; i++) {
					Vector3 pos = MathUtil.getRandomVectorAround(effectivePodCenter+(Vector3.down*3.5F)+offset, 7.5F);
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
			if (!this.canExplode())
				return;
			this.spawnParticleShell("f39e56b9-9a11-4582-875f-c37f1ed37314", 0.5F, Vector3.zero);
			this.Invoke("explodePart2", 0.5F);
		}

		internal bool canExplode() {
			return !gameObject.GetFullHierarchyPath().Contains("ACUDecoHolder") && transform.position.y <= -100;
		}

		internal void explodePart2() {
			float time = DayNightCycle.main.timePassedAsFloat;
			if (isExploded || time - lastRegenTime < 10)
				return;
			explodeIn = -1;
			lastExplodeTime = time;
			isExploded = true;
			//podGO.SetActive(false);
			foreach (Renderer r in podRenders) {
				foreach (Material m in r.materials)
					m.EnableKeyword("FX_BURST");
			}
			foreach (Collider c in colliders) {
				c.enabled = false;
				c.gameObject.SetActive(false);
			}
			int n = UnityEngine.Random.Range(8, 12);
			for (int i = 0; i < n; i++) {
				Vector3 pos = MathUtil.getRandomVectorAround(effectivePodCenter, 4F);
				GameObject go = ObjectUtil.createAirBubble();
				go.transform.position = pos;
				float f = UnityEngine.Random.Range(1.5F, 3F);
				go.transform.localScale = Vector3.one * f;
				Bubble b = go.GetComponent<Bubble>();
				b.oxygenSeconds *= f;
				WorldForces wf = go.GetComponent<WorldForces>();
				wf.underwaterGravity *= 0.5F;
				go.GetComponent<Rigidbody>().drag *= 2F;
			}
			SoundManager.playSoundAt(explosionSound, effectivePodCenter, false, 64);
			HashSet<GameObject> set = WorldUtil.getObjectsNear(effectivePodCenter, 35);
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
				if (pod && !pod.isExploded && UnityEngine.Random.Range(0F, 1F) <= 0.5F * Mathf.Max(0, 1 - (dd / 30F))) {
					pod.scheduleExplode(UnityEngine.Random.Range(0.2F, 0.67F));
					continue;
				}
				LiveMixin lv = go.GetComponent<LiveMixin>();
				if (lv && lv.IsAlive()) {
					float amt = this.getDamageToDeal(go);
					if (amt < 0.1F)
						continue;
					float f = (dd-10)/35F;
					amt *= Mathf.Clamp01(1.5F - (f * f));
					if (this.isInGrandReef()) {
						float depth2 = (-go.transform.position.y)-400;
						amt *= 1 + (depth2 / 400F);
					}
					amt *= EcoceanMod.config.getFloat(ECConfig.ConfigEntries.ANCHORDMG);
					if (onExplodingAnchorPodDamageEvent != null) {
						ExplodingAnchorPodDamage e = new ExplodingAnchorPodDamage(this, lv, amt);
						onExplodingAnchorPodDamageEvent.Invoke(e);
						amt = e.damageAmount;
					}
					//SNUtil.writeToChat("Damaging "+lv+" from anchor pod explosion @ "+effectivePodCenter+" x"+amt);
					lv.TakeDamage(amt, go.transform.position, DamageType.Explosive, gameObject);
					Rigidbody rb = go.GetComponent<Rigidbody>();
					if (rb) {
						Vector3 vec = go.transform.position-effectivePodCenter;
						rb.AddForce(vec.normalized * 140 / vec.magnitude, ForceMode.VelocityChange);
					}
				}
				Creature c = go.GetComponent<Creature>();
				if (c && (c is GhostLeviathan || c is GhostLeviatanVoid || c is ReaperLeviathan || c is SeaDragon)) {
					c.Aggression.Add(-1F);
					foreach (LastTarget lt in go.GetComponentsInChildren<LastTarget>())
						lt.SetTarget(null);
					foreach (AggressiveWhenSeeTarget lt in go.GetComponentsInChildren<AggressiveWhenSeeTarget>())
						lt.lastTarget.SetTarget(null);
					foreach (AttackLastTarget lt in go.GetComponentsInChildren<AttackLastTarget>())
						lt.StopAttack();
				}
			}
		}

		private bool isInGrandReef() {
			return VanillaBiomes.DEEPGRAND.isInBiome(transform.position) || VanillaBiomes.GRANDREEF.isInBiome(transform.position);
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

	public class ExplodingAnchorPodDamage {

		public readonly ExplodingAnchorPod pod;
		public readonly LiveMixin toDamage;
		public readonly float originalDamageAmount;

		public float damageAmount;

		internal ExplodingAnchorPodDamage(ExplodingAnchorPod e, LiveMixin lv, float amt) {
			pod = e;
			toDamage = lv;
			originalDamageAmount = amt;
			damageAmount = originalDamageAmount;
		}

	}
}
