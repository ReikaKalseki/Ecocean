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
	internal class ReefbackJetSuctionManager : MonoBehaviour {
		
		private Reefback creature;
		private SphereCollider leftAoE;
		private SphereCollider rightAoE;
			
		void Update() {
			if (!creature)
				creature = gameObject.GetComponent<Reefback>();
			leftAoE = ensureAoE(leftAoE, "leftAoE");
			rightAoE = ensureAoE(rightAoE, "rightAoE");
			leftAoE.center = Vector3.zero;
			rightAoE.center = Vector3.zero;
		}
		
		private SphereCollider ensureAoE(SphereCollider c, string nm) {
			if (!c) {
				GameObject go = ObjectUtil.getChildObject(gameObject, nm);
				if (!go) {
					float sign = nm.Contains("left") ? -1 : 1;
					go = new GameObject(nm);
					go.transform.SetParent(transform);
					go.transform.localPosition = new Vector3(8.4F*sign, 0.1F, 0);
					c = go.EnsureComponent<SphereCollider>();
					c.center = Vector3.zero;
					c.radius = 5F;//1.75F;
					c.isTrigger = true;
					go.EnsureComponent<ReefbackJetSuctionTrigger>();
				}
			}
			return c;
		}
		
	}
	
	internal class ReefbackJetSuctionTrigger : MonoBehaviour {
		
		private ReefbackJetSuctionManager root;
		
		void Update() {
			if (!root) {
				root = gameObject.FindAncestor<ReefbackJetSuctionManager>();
			}
			float sign = gameObject.name.Contains("left") ? -1 : 1;
			transform.localPosition = new Vector3(8.4F*sign, 0, 0);
			transform.localRotation = Quaternion.identity;
		}

	    private void OnTriggerStay(Collider other) {
			if (other.gameObject.FindAncestor<Reefback>())
				return;
			Rigidbody rb = other.gameObject.FindAncestor<Rigidbody>();
			//SNUtil.writeToChat("Jet "+gameObject.name+" sucking "+other);
			if (rb) {
				Vector3 dd = transform.position-other.transform.position;
				rb.AddForce((-root.transform.forward*1.2F+dd.normalized*15F/dd.sqrMagnitude)*10F*Time.deltaTime, ForceMode.VelocityChange);
			}
	    }
		
	}
}
