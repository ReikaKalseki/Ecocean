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
		internal class ECReaper : PassiveSonarEntity {
		
			private FMOD_CustomLoopingEmitter roar1;
			private FMOD_CustomLoopingEmitterWithCallback roar2;
			
			protected new void Update() {
				base.Update();
				if (!roar1) {
					foreach (FMOD_CustomLoopingEmitter em in GetComponents<FMOD_CustomLoopingEmitter>()) {
						if (em.asset != null && em.asset.path.Contains("idle")) {
							roar1 = em;
							break;
						}
					}
					roar2 = GetComponent<FMOD_CustomLoopingEmitterWithCallback>();
				}
			}
			
			protected override bool isAudible() {
				return isRoaring(roar1) || isRoaring(roar2);
			}
			
		}
}
