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
	internal class ECDragon : PassiveSonarEntity {

		private FMOD_CustomLoopingEmitterWithCallback roar;

		protected new void Update() {
			base.Update();
			if (!roar) {
				roar = this.GetComponent<FMOD_CustomLoopingEmitterWithCallback>();
			}
		}

		protected override void setSonarRanges() {
			minimumDistanceSq = 125 * 125;
			maximumDistanceSq = 250 * 250;
		}

		protected override bool isAudible() {
			return this.isRoaring(roar);
		}

	}
}
