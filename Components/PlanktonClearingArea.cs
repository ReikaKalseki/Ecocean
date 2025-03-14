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
	public class PlanktonClearingArea : MonoBehaviour {
		
		public static bool skipPlanktonClear;
		
		//private CapsuleCollider AoE;
		
		public float clearingRate = 1;
		
		public event Action<PlanktonCloudTag, float> onClearTick;

	    private void OnTriggerStay(Collider other) {
			if (Time.deltaTime <= 0)
				return;
			//SNUtil.writeToChat("Plankton clearer "+gameObject.name+" ticking with collider "+other.gameObject.GetFullHierarchyPath());
			if (skipPlanktonClear)
				return;
			PlanktonCloudTag pc = other.GetComponent<PlanktonCloudTag>(); //NOT ancestor - only interact with main entity
			if (pc && pc.enabled) {
				float amt = Time.deltaTime*clearingRate;
				pc.damage(this, amt);
				if (onClearTick != null)
					onClearTick.Invoke(pc, amt);
			}
	    }
		
		internal void tickExternal() {
			if (onClearTick != null)
				onClearTick.Invoke(null, Time.deltaTime*clearingRate);
		}
		
	}
}
