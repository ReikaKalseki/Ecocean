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
using SMLHelper.V2.Assets;

namespace ReikaKalseki.Ecocean {
		
	public class CVWaterCurrentBehavior : MonoBehaviour {
		
		internal float baseStrength = 5;
		internal float strengthFalloff = 1; //this is an exponent, 1 for linear
		
		internal float effectLength = 12;
		
		internal float coreEffectRadius = 4; //how wide the inner region is, where speed is always maximum
		internal float outerEffectRadius = 9; //the radius of the outer annulus where speed tapers off as distance from the centerline increases
		
		private float age;
		
		private Collider collider;
		
		private Vector3 p1;
		private Vector3 p2;
		
		private readonly HashSet<Rigidbody> pushedEntities = new HashSet<Rigidbody>();
		
		//You can set this to any callback to do anything to an entity in the current each frame, eg hurt it, infect it, add to the player's inventory, whatever
		private Action<GameObject> entityEffect = null;
		
		private ParticleSystem[] particles = new ParticleSystem[0]; //one or more particle system instances; I do not know how to build one of these, and 2.0 fetching them requires async and I am not using 2.0 DLLs

		void Start() {
			this.collider = GetComponent<CapsuleCollider>();
			
			GameObject pk = ObjectUtil.createWorldObject("ff8e782e-e6f3-40a6-9837-d5b6dcce92bc");
			particles = pk.GetComponentsInChildren<ParticleSystem>();
			pk.transform.SetParent(transform);
			Utils.ZeroTransform(pk.transform);
			pk.transform.SetParent(transform);
			ObjectUtil.removeChildObject(pk, "ElecLight");
			pk.transform.localEulerAngles = new Vector3(0, -90, 0);
		}
		
		void Update() {
			age += Time.deltaTime;
			
			p1 = transform.position-transform.forward*effectLength*0.5F;
			p2 = transform.position+transform.forward*effectLength*0.5F;
			Vector3 axis = (p2-p1).normalized;
			float strengthFactor = getModifiedStrength();
			
			if (collider is CapsuleCollider) {
				CapsuleCollider cc = (CapsuleCollider)collider;
				cc.height = effectLength;
				cc.radius = outerEffectRadius;
			}
			
			foreach (Rigidbody rb in pushedEntities) {
				if (rb) {
					rb.AddForce(transform.forward * baseStrength * strengthFactor * getPositionalStrength(rb.transform.position) * Time.timeScale, ForceMode.Acceleration);
					if (entityEffect != null)
						entityEffect.Invoke(rb.gameObject);
				}
			}
			
			foreach (ParticleSystem pp in particles) {
				pp.transform.position = p1;
				pp.gameObject.SetActive(true);
				if (strengthFactor > 0) {
					pp.Play(false);
					ParticleSystem.MainModule main = pp.main;
					main.startLifetime = effectLength/baseStrength;
					ParticleSystem.EmissionModule emit = pp.emission;
					emit.rateOverTimeMultiplier = strengthFactor*4;
					ParticleSystem.VelocityOverLifetimeModule speed = pp.velocityOverLifetime;
					speed.enabled = true;
					speed.x = axis.x*baseStrength;
					speed.y = axis.y*baseStrength;
					speed.z = axis.z*baseStrength;
					ParticleSystem.ShapeModule shape = pp.shape;
					shape.shapeType = ParticleSystemShapeType.Sphere;
					shape.radius = coreEffectRadius*0.67F;
				}
				else {
					pp.Stop(false, ParticleSystemStopBehavior.StopEmitting);
				}
			}
		}
		
		//You could use this to make the speed of the current vary with anything, eg time of day or (computationally expensively) proximity of other objects
		private float getModifiedStrength() {
			return this.baseStrength;
		}
		
		public float getPositionalStrength(Vector3 pos) {
			float dist = getDistanceToLine(pos);
			if (dist <= coreEffectRadius)
				return 1;
			float distFac = (dist-coreEffectRadius)/(outerEffectRadius-coreEffectRadius);
			return Mathf.Clamp01(Mathf.Pow(1-distFac, strengthFalloff));
		}
		
		internal float getDistanceToLine(Vector3 point) {
			/*
		    Vector3 d = (p2 - p1) / Vector3.Distance(p1, p2);
		    Vector3 v = point - p1;
		    double t = v.Dot(d);
		    Vector3 P = p1 + t * d;
		    return Vector3.Distance(P, point);
		    */
		   //better way with native unity			
			Ray ray = new Ray(p1, p2-p1);
			return Vector3.Cross(ray.direction, point - ray.origin).magnitude;
		}
		
		void OnDestroy() {
			OnDisable();
		}
		
		void OnDisable() {
			pushedEntities.Clear();	
		}
		
		void OnTriggerEnter(Collider c) {
			Rigidbody rb = c.gameObject.FindAncestor<Rigidbody>();
			if (rb && !rb.isKinematic && isValidTarget(rb)) {
				pushedEntities.Add(rb);
			}
		}
	
		void OnTriggerExit(Collider c) {
			pushedEntities.Remove(c.gameObject.FindAncestor<Rigidbody>());
		}
		
		//You can use this to filter objects, for example restrict the current to only affect the player or vehicles, or not affect creatures based on species, or whatever
		protected bool isValidTarget(Rigidbody rb) {
			return !rb.isKinematic;
		}
		
	}
}
