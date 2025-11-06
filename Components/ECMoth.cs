using System;
using System.Collections.Generic;

using ReikaKalseki.DIAlterra;

using UnityEngine;

namespace ReikaKalseki.Ecocean {

	public class ECMoth : MonoBehaviour {

		private SeaMoth seamoth;
		private Rigidbody body;

		private bool lightsOn;

		public Func<float> getLightIntensity = () => 1;

		private readonly LinkedList<float> lightToggles = new LinkedList<float>();

		public int stuckCells { get; private set; }
		private float lastCellCheckTime;

		public PredatoryBloodvine holdingBloodKelp { get; private set; }

		public bool touchingKelp { get; private set; }

		public float lastTouchHeatBubble { get; internal set; }

		public float lastGeyserTime { get; internal set; }

		public bool heatColumn { get; private set; }

		public float lastVoidOrganicCollectTime { get; private set; }

		private VehicleAccelerationModifier speedModifier;

		void Update() {
			if (!seamoth)
				seamoth = this.GetComponent<SeaMoth>();
			if (!body)
				body = this.GetComponent<Rigidbody>();

			if (!speedModifier)
				speedModifier = seamoth.addSpeedModifier();

			float time = DayNightCycle.main.timePassedAsFloat;
			while (lightToggles.First != null && time - lightToggles.First.Value > 3) {
				lightToggles.RemoveFirst();
			}
			if (seamoth.toggleLights.lightsActive != lightsOn) {
				lightToggles.AddLast(time);
				lightsOn = seamoth.toggleLights.lightsActive;
			}
			int flashCount = lightToggles.Count;
			//SNUtil.writeToChat(flashCount+" > "+((flashCount-5)/200F).ToString("0.0000"));
			if (flashCount > 5 && UnityEngine.Random.Range(0F, 1F) < (flashCount - 5) / 250F * this.getFlashEffectiveness() * getLightIntensity()/* && seamoth.mainAnimator.GetBool("reaper_attack")*/) {
				GameObject go = WorldUtil.areAnyObjectsNear(transform.position, 60, obj => {
					ReaperLeviathan rl = obj.GetComponent<ReaperLeviathan>();
					return rl && rl.holdingVehicle == seamoth;
				}
					);
				//SNUtil.writeToChat("Found object "+go);
				if (go) {
					go.GetComponent<ReaperLeviathan>().ReleaseVehicle();
				}
			}
			if (lightsOn) {
				GlowOil.handleLightTick(transform);
				if (UnityEngine.Random.Range(0F, 1F) <= 0.02F)
					ECHooks.attractToLight(seamoth);
			}

			if (time - lastCellCheckTime >= 1) {
				lastCellCheckTime = time;
				//stuckCells = GetComponentsInChildren<VoidBubbleTag>().Length;
				stuckCells = 0;
				foreach (VoidBubbleTag vb in WorldUtil.getObjectsNearWithComponent<VoidBubbleTag>(transform.position, 24)) {
					if (vb.isStuckTo(body))
						stuckCells++;
				}
			}

			heatColumn = ECHooks.isVoidHeatColumn(transform.position, out Vector3 trash);// time-ecocean.lastTouchHeatBubble <= 0.5F;
			if (heatColumn) {
				body.AddForce(Vector3.up * Time.deltaTime * 150, ForceMode.Acceleration);
				if (time - lastVoidOrganicCollectTime >= 5 && UnityEngine.Random.Range(0F, 1F) < 0.1F && body.velocity.y < -6) { //maxes ~8
					SeamothPlanktonScoop.checkAndTryScoop(seamoth, Time.deltaTime, EcoceanMod.voidOrganic.TechType, out GameObject drop);
					if (drop)
						lastVoidOrganicCollectTime = time;
				}
			}

			speedModifier.accelerationMultiplier = 1;
			if (stuckCells > 0)
				speedModifier.accelerationMultiplier *= Mathf.Exp(-stuckCells * 0.2F);
			if (touchingKelp)
				speedModifier.accelerationMultiplier *= 0.3F;
		}

		private float getFlashEffectiveness() {
			float brightness = DayNightCycle.main.GetLightScalar();
			return 1.2F - (brightness * 0.8F);
		}

		public void OnBloodKelpGrab(PredatoryBloodvine c) {
			holdingBloodKelp = c;
		}
	}
}
