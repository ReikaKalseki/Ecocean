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
using ReikaKalseki.Ecocean;
using SMLHelper.V2.Handlers;
using SMLHelper.V2.Utility;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using ECCLibrary;

namespace ReikaKalseki.Ecocean {
	internal class ECEmperor : MonoBehaviour {
		
		void Start() {
			InvokeRepeating("applyPassivity", 0, 0.5F);
		}
		
		void OnDisable() {
			CancelInvoke("applyPassivity");
		}
		
		void OnDestroy() {
			OnDisable();
		}
		
		void applyPassivity() {
			foreach (AggressiveWhenSeeTarget a in WorldUtil.getObjectsNearWithComponent<AggressiveWhenSeeTarget>(transform.position, 100)) {
				a.creature.Aggression.Add(-1);
				a.lastTarget.target = null;
			}
		}
			
	}
}
