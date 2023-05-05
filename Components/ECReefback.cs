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
		internal class ECReefback : PassiveSonarEntity {
		
			private FMOD_CustomLoopingEmitter idleSound;
			
			protected void Update() {
				base.Update();
				if (!idleSound) {
					idleSound = GetComponent<FMOD_CustomLoopingEmitter>();
				}
			}
			
			protected override GameObject getSphereRootGO() {
				return getMainRenderer().gameObject;
			}
			
			protected override void setSonarRanges() {
				minimumDistanceSq = 120*120;
				maximumDistanceSq = 200*200;
				if (VanillaBiomes.GRANDREEF.isInBiome(transform.position)) {
					minimumDistanceSq *= 0.5F;
					maximumDistanceSq *= 0.25F;
				}
				else if (VanillaBiomes.REDGRASS.isInBiome(transform.position)) {
					minimumDistanceSq *= 1.25F;
				}
			}
			
			protected override Renderer getMainRenderer() {
				return ObjectUtil.getChildObject(gameObject, "Pivot/Reefback/Reefback").GetComponentInChildren<Renderer>();
			}
			
			protected override bool isAudible() {
				return isRoaring(idleSound);
			}
			
			protected override Vector3 getRadarSphereSize() {
				return new Vector3(45, 45, 60);
			}
			
		}
}
