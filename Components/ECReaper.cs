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
	internal class ECReaper : PassiveSonarEntity {

		private FMOD_CustomLoopingEmitter roar1;
		private FMOD_CustomLoopingEmitterWithCallback roar2;

		protected new void Update() {
			base.Update();
			if (!roar1) {
				foreach (FMOD_CustomLoopingEmitter em in this.GetComponents<FMOD_CustomLoopingEmitter>()) {
					if (em.asset != null && em.asset.path.Contains("idle")) {
						roar1 = em;
						break;
					}
				}
				roar2 = this.GetComponent<FMOD_CustomLoopingEmitterWithCallback>();
			}
		}

		protected override bool isAudible() {
			return this.isRoaring(roar1) || this.isRoaring(roar2);
		}

	}
}
