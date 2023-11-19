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

namespace ReikaKalseki.Ecocean
{
		internal class GeyserSonarSignal : PassiveSonarEntity {
		
			private Geyser geyser;
			
			protected new void Update() {
				if (!geyser)
					geyser = GetComponent<Geyser>();
				base.Update();
			}
			
			protected override float getFadeRate() {
				return 2;
			}
			
			protected override float getTimeVariationStrength() {
				return 0.05F;
			}
			
			protected override float getIntensityFactor() {
				return 1.25F;
			}
			
			protected override GameObject getSphereRootGO() {
				return gameObject;
			}
			
			protected override void setSonarRanges() {
				minimumDistanceSq = 30*30;
				maximumDistanceSq = 50*50;
			}
			
			protected override bool isAudible() {
				return geyser && geyser.erupting;
			}
			
			protected override Vector3 getRadarSphereSize() {
				return new Vector3(36, 36, 36);
			}
			
			protected override Vector3 getRadarSphereOffset() {
				return Vector3.up*20;
			}
			
		}
}
