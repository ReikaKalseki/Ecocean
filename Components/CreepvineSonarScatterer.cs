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
	public class CreepvineSonarScatterer : MonoBehaviour {
		
		private SonarFloraTag sonarFlora;
		
		public Action<SonarFloraTag> modify = null;
		
		void Update() {
			if (!sonarFlora) {
				sonarFlora = GetComponentInChildren<SonarFloraTag>();
				if (!sonarFlora) {
					Bounds extents = new Bounds(transform.position, Vector3.zero);
					foreach (Renderer r in GetComponentsInChildren<Renderer>())
						extents.Encapsulate(r.bounds);
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
		
	}
}
