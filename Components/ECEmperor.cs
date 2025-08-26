using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

using ECCLibrary;

using FMOD;
using FMOD.Studio;

using FMODUnity;

using ReikaKalseki.DIAlterra;
using ReikaKalseki.Ecocean;

using SMLHelper.V2.Handlers;
using SMLHelper.V2.Utility;

using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace ReikaKalseki.Ecocean {
	internal class ECEmperor : MonoBehaviour {

		void Start() {
			this.InvokeRepeating("applyPassivity", 0, 0.5F);
		}

		void OnDisable() {
			this.CancelInvoke("applyPassivity");
		}

		void OnDestroy() {
			this.OnDisable();
		}

		void applyPassivity() {
			foreach (AggressiveWhenSeeTarget a in WorldUtil.getObjectsNearWithComponent<AggressiveWhenSeeTarget>(transform.position, 100)) {
				a.creature.Aggression.Add(-1);
				a.lastTarget.target = null;
			}
		}

	}
}
