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
		internal class ECDragon : PassiveSonarEntity {
		
			private FMOD_CustomLoopingEmitterWithCallback roar;
			
			protected void Update() {
				base.Update();
				if (!roar) {
					roar = GetComponent<FMOD_CustomLoopingEmitterWithCallback>();
				}
			}
			
			protected override void setSonarRanges() {
				minimumDistanceSq = 125*125;
				maximumDistanceSq = 250*250;
			}
			
			protected override Renderer getMainRenderer() {
				return ObjectUtil.getChildObject(gameObject, "Sea_Dragon_wholeBody_anim/Sea_Dragon_geo").GetComponentInChildren<Renderer>();
			}
			
			protected override bool isAudible() {
				return isRoaring(roar);
			}
			
		}
}
