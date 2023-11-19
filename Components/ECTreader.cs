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
		internal class ECTreader : PassiveSonarEntity {
		
			private SeaTreaderSounds sounds;
			
			protected new void Update() {
				base.Update();
				if (!sounds) {
					sounds = GetComponentInChildren<SeaTreaderSounds>();
					createRadarSphere(sounds.frontLeg.gameObject, 0.33F);
					createRadarSphere(sounds.leftLeg.gameObject, 0.33F);
					createRadarSphere(sounds.rightLeg.gameObject, 0.33F);
				}
			}
			
			protected override GameObject getSphereRootGO() {
				return ObjectUtil.getChildObject(gameObject, "Sea_Treader/Sea_Treader_Geo/Sea_Treader");
			}
			
			protected override void setSonarRanges() {
				minimumDistanceSq = 50*50;
				maximumDistanceSq = 90*90;
			}
			
			protected override bool isAudible() {
				return sounds && (sounds.attackSound.evt.hasHandle() || sounds.stepSound.evt.hasHandle() || sounds.stompSound.evt.hasHandle());
			}
			
			protected override Vector3 getRadarSphereSize() {
				return new Vector3(20, 30, 20);
			}
			
		}
}
