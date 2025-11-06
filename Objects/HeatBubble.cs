using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;

using FMOD;

using FMODUnity;

using ReikaKalseki.DIAlterra;

using SMLHelper.V2;
using SMLHelper.V2.Assets;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Handlers;
using SMLHelper.V2.Interfaces;
using SMLHelper.V2.Utility;

using UnityEngine;
using UnityEngine.UI;

namespace ReikaKalseki.Ecocean {
	public class HeatBubble : Spawnable {

		public HeatBubble() : base("HeatColumnBubble", "", "") {

		}

		public override GameObject GetGameObject() {
			GameObject world = ObjectUtil.createAirBubble();
			world.removeComponent<Bubble>();
			world.removeComponent<Collider>();
			world.removeComponent<LiveMixin>();
			world.EnsureComponent<TechTag>().type = TechType;
			world.EnsureComponent<PrefabIdentifier>().ClassId = ClassID;
			//SphereCollider sc = world.GetComponentInChildren<SphereCollider>();
			//sc.radius *= 0.2F;
			//sc.gameObject.EnsureComponent<HeatBubbleTagRelay>();
			HeatBubbleTag g = world.EnsureComponent<HeatBubbleTag>();
			world.EnsureComponent<LargeWorldEntity>().cellLevel = LargeWorldEntity.CellLevel.VeryFar;
			Renderer r = world.GetComponentInChildren<Renderer>();
			//r.transform.localScale = Vector3.one * 0.2F;
			world.GetComponent<WorldForces>().underwaterGravity = 0;
			/*
			r.material.SetFloat("_SpecInt", 12);
			r.material.SetFloat("_Shininess", 6);
			r.material.SetFloat("_Fresnel", 0);
			r.material.SetColor("_Color", new Color(1, 1, 1, 1));
			r.material.SetColor("_SpecColor", COLOR);
			r.material.SetVector("_Scale", new Vector4(0.06F, 0.06F, 0.06F, 0.06F));
			r.material.SetVector("_Frequency", new Vector4(8.0F, 8.0F, 8.0F, 8.0F));
			r.material.SetVector("_Speed", new Vector4(0.05F, 0.05F, 0.05F, 0.05F));
			r.material.SetVector("_ObjectUp", new Vector4(0F, 0F, 1F, 0F));
			*/
			//Light l = world.addLight(1, 20, new Color(0.65F, 1, 0));
			return world;
		}
	}

	class HeatBubbleTag : HeatColumnObject {

		new void Update() {
			base.Update();
			if (!transform)
				return;
			transform.localScale = Vector3.one * scaleFactor * 0.15F;
			if (body)
				body.velocity = Vector3.up * 7 * speedFactor;
		}

		/*
		internal void onTouch(Collision c) {
			ECHooks.ECMoth ec = c.rigidbody.gameObject.FindAncestor<ECHooks.ECMoth>();
			if (ec) {
				ec.lastTouchHeatBubble = DayNightCycle.main.timePassedAsFloat;
			}
			else if (c.gameObject.isPlayer(true)) {
				//handled automatically by ECHooks
				//c.gameObject.FindAncestor<Player>().liveMixin.TakeDamage(2*Time.deltaTime, type:DamageType.Heat);
				//c.rigidbody.AddForce(Vector3.up * Time.deltaTime * 150, ForceMode.Acceleration);
			}
		}*/

	}
	/*
	class HeatBubbleTagRelay : HeatColumnObject {

		private HeatBubbleTag parent;
		
		private void OnCollisionEnter(Collision c) {
			if (parent && c.rigidbody)
				parent.onTouch(c);
		}

	}*/
}
