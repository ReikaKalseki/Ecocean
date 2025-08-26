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
	public class PlantHidingTracker : MonoBehaviour {

		private static readonly HidingVignette visualIndicator;

		public static float shaderIntensityFactor = 0.55F;
		public static Color shaderColor = new Color(0.1F, 0.5F, 0.1F);

		static PlantHidingTracker() {
			visualIndicator = new HidingVignette();
			ScreenFXManager.instance.addOverride(visualIndicator);
		}

		class HidingVignette : ScreenFXManager.ScreenFXOverride {

			internal float intensity = 0;

			internal HidingVignette() : base(-100) {

			}

			public override void onTick() {
				if (intensity > 0) {
					//ScreenFXManager.instance.registerOverrideThisTick(ScreenFXManager.instance.mesmerShader);
					//ScreenFXManager.instance.mesmerShader.amount = intensity*0.3F;
					//ScreenFXManager.instance.mesmerShader.mat.SetVector("_ColorStrength", new Vector4(0, 0, 0, intensity));
					ScreenFXManager.instance.registerOverrideThisTick(ScreenFXManager.instance.smokeShader);
					ScreenFXManager.instance.smokeShader.intensity = intensity * shaderIntensityFactor;
					ScreenFXManager.instance.smokeShader.color = shaderColor;
					ScreenFXManager.instance.smokeShader.mat.color = shaderColor;
					ScreenFXManager.instance.smokeShader.mat.SetColor("_Color", shaderColor);
				}
			}

		}

		private readonly HashSet<PlantHidingCollider> contacts = new HashSet<PlantHidingCollider>();
		private Color renderColor;

		private float lastCheckTime = -1;

		internal float minRadius;

		void Update() {
			float time = DayNightCycle.main.timePassedAsFloat;
			if (time - lastCheckTime >= 1) {
				contacts.RemoveWhere(c => !(c && (c.transform.position - transform.position).sqrMagnitude < 900));
				lastCheckTime = time;
			}
			if (this.isPlayer() || (Player.main.GetVehicle() && Player.main.GetVehicle().GetComponent<PlantHidingTracker>() == this)) {
				float dT = Time.deltaTime;
				bool active = this.isActive();
				if (active) {
					visualIndicator.intensity = Mathf.Min(1, visualIndicator.intensity + dT);
					shaderColor = renderColor;
					shaderIntensityFactor = 0.55F + (0.025F * (contacts.Count - 1));
				}
				else {
					visualIndicator.intensity = Mathf.Max(0, visualIndicator.intensity - dT);
				}
			}
		}

		internal bool isActive() {
			return contacts.Count > 0;
		}

		internal void addContact(PlantHidingCollider pc) {
			contacts.Add(pc);
			this.computeColor();
		}

		internal void removeContact(PlantHidingCollider pc) {
			contacts.Remove(pc);
			this.computeColor();
		}

		private void computeColor() {
			renderColor = Color.clear;
			foreach (PlantHidingCollider pc in contacts)
				renderColor += pc.renderColor;
			renderColor /= contacts.Count;
			renderColor = renderColor.WithAlpha(1);
		}

	}
}
