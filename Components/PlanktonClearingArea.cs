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
	internal class PlanktonClearingArea : MonoBehaviour {
		
		//private CapsuleCollider AoE;
		
		public float clearingRate = 1;
		
		public event Action<PlanktonCloudTag, float> onClearTick;

	    private void OnTriggerStay(Collider other) {
			if (Time.deltaTime <= 0)
				return;
			PlanktonCloudTag pc = other.gameObject.FindAncestor<PlanktonCloudTag>();
			if (pc && pc.enabled) {
				float amt = Time.deltaTime*clearingRate;
				pc.damage(this, amt);
				if (onClearTick != null)
					onClearTick.Invoke(pc, amt);
			}
	    }
		
	}
}
