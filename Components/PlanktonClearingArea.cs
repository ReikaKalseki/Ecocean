using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

using ReikaKalseki.DIAlterra;

using SMLHelper.V2.Handlers;
using SMLHelper.V2.Utility;

using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace ReikaKalseki.Ecocean {
	public class PlanktonClearingArea : MonoBehaviour {

		public static bool skipPlanktonClear;

		//private CapsuleCollider AoE;

		public float clearingRate = 1;

		public event Action<PlanktonCloudTag, float> onClearTick;

		private Dictionary<string, object> properties = new Dictionary<string, object>();

		void Start() {
			Rigidbody rb = gameObject.EnsureComponent<Rigidbody>();
			rb.isKinematic = true;
			rb.mass = 9999999;
			rb.useGravity = false;
			rb.freezeRotation = true;
			rb.constraints = RigidbodyConstraints.FreezeAll;
		}

		private void OnTriggerStay(Collider other) {
			if (Time.deltaTime <= 0)
				return;
			//SNUtil.writeToChat("Plankton clearer "+gameObject.name+" ticking with collider "+other.gameObject.GetFullHierarchyPath());
			if (skipPlanktonClear)
				return;
			PlanktonCloudClearableContactZone pc = other.GetComponent<PlanktonCloudClearableContactZone>(); //NOT ancestor - only interact with specific colliders
			if (pc && pc.parent && pc.parent.enabled) {
				float amt = Time.deltaTime*clearingRate;
				pc.parent.damage(this, amt);
				if (onClearTick != null)
					onClearTick.Invoke(pc.parent, amt);
			}
		}

		public void tickExternal(float r = 1) {
			if (onClearTick != null)
				onClearTick.Invoke(null, Time.deltaTime * clearingRate * r);
		}

		public void setProperty(string name, object value) {
			properties[name] = value;
		}

		public object getProperty(string name) {
			return properties.ContainsKey(name) ? properties[name] : null;
		}

		public E getProperty<E>(string name) {
			object o = getProperty(name);
			return o != null && o is E ? (E)o : default(E);
		}

		public bool getBooleanProperty(string name) {
			object o = getProperty(name);
			return o != null && o is bool b && b;
		}

	}
}
