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
	public class PlantHidingCollider : MonoBehaviour {
		
		private Collider collider;
		internal Color renderColor;
		
		internal void initialize(Collider c, Color clr) {
			collider = c;
			renderColor = clr;
		}
			
		void Update() {
			if (!collider)
				collider = GetComponent<Collider>();
		}

	    private void OnTriggerStay(Collider other) {
			
	    }

	    private void OnTriggerEnter(Collider other) {
			if (!collider)
				return;
			PlantHidingTracker pc = other.gameObject.FindAncestor<PlantHidingTracker>();
			if (pc && pc.minRadius*pc.minRadius <= collider.bounds.size.sqrMagnitude)
				pc.addContact(this);
	    }

	    private void OnTriggerExit(Collider other) {
			if (!collider)
				return;
			PlantHidingTracker pc = other.gameObject.FindAncestor<PlantHidingTracker>();
			if (pc && pc.minRadius*pc.minRadius <= collider.bounds.size.sqrMagnitude)
				pc.removeContact(this);
	    }
	    
		public static void addToObject(Collider c, Color clr) {
			if (!c)
				return;
			c.gameObject.EnsureComponent<PlantHidingCollider>().initialize(c, clr);
		}
	    
		public static void addToObject(InteractionVolumeCollider c, Color clr) {
			if (!c)
				return;
			c.gameObject.EnsureComponent<PlantHidingCollider>().initialize(c.GetComponent<Collider>(), clr);
		}
		
	}
}
