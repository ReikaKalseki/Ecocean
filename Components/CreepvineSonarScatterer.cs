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
using ECCLibrary;

namespace ReikaKalseki.Ecocean
{
	public class CreepvineSonarScatterer : MonoBehaviour {
		
		private SonarFloraTag sonarFlora;		
		public Action<SonarFloraTag> modify = null;
		
		private SonarOnlyRenderer cheapController;
		private float lastSonarFloraCleanupTime = -1;
		
		void Update() {
			if (EcoceanMod.config.getBoolean(ECConfig.ConfigEntries.GOODCREEPSONAR)) {
				if (!sonarFlora) {				
					if (!sonarFlora) {
						Bounds extents = getRenderBox();
						GameObject go = ObjectUtil.createWorldObject(EcoceanMod.sonarFlora.ClassID);
						//Light l = GetComponentInChildren<Light>();
						go.transform.SetParent(/*l ? l.transform : */transform);
						go.transform.localPosition = extents.center-transform.position;//l ? Vector3.zero : Vector3.up*12;
						go.transform.localRotation = Quaternion.identity;
						go.transform.localScale = Vector3.one;
						sonarFlora = go.GetComponent<SonarFloraTag>();
						if (modify != null)
							modify.Invoke(sonarFlora);
						sonarFlora.setShape(/*new Vector3(2.5F, 16, 2.5F)*/extents.extents*0.9F-new Vector3(0.5F, 1F, 0.5F));
					}
				}
				if (sonarFlora && sonarFlora.aoe.sqrMagnitude <= 0.01F) {
					UnityEngine.Object.Destroy(sonarFlora.gameObject);
					sonarFlora = null;
				}
			}
			else {
				if (!cheapController)
					cheapController = gameObject.EnsureComponent<SonarOnlyRenderer>();
				
				if (cheapController.renderers.Count == 0) {
					SNUtil.log("Adding cheap creepvine sonar halo");
					GameObject capsule = ObjectUtil.getChildObject(gameObject, "SonarHalo");
					if (!capsule) {
						Bounds extents = getRenderBox();
						capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
						capsule.name = "SonarHalo";
						capsule.transform.SetParent(transform);
						capsule.transform.localPosition = extents.center-transform.position;
						capsule.transform.localRotation = Quaternion.identity;
						capsule.transform.localScale = extents.extents+Vector3.one;
						ECCHelpers.ApplySNShaders(capsule, new UBERMaterialProperties(0, 10, 5));
						ObjectUtil.removeComponent<Collider>(capsule);
					}
					Renderer r = capsule.GetComponentInChildren<Renderer>();
					cheapController.renderers.Add(SonarFloraTag.prepareCheapSonarHalo(r));
				}
				foreach (SonarOnlyRenderer.SonarRender r in cheapController.renderers) {
					r.renderer.materials[0].SetFloat("_Built", Mathf.Lerp(0.475F, 0.52F, r.intensity));
				}
				float time = DayNightCycle.main.timePassedAsFloat;
				if (time-lastSonarFloraCleanupTime >= 1) {
					lastSonarFloraCleanupTime = time;
					SonarFloraTag sf = GetComponentInChildren<SonarFloraTag>();
					UnityEngine.Object.Destroy(sf.gameObject);
				}
			}
		}
		
		private Bounds getRenderBox() {
			Bounds extents = new Bounds(transform.position, Vector3.zero);
			foreach (Renderer r in GetComponentsInChildren<Renderer>())
				extents.Encapsulate(r.bounds);
			return extents;
		}
		
	}
}
