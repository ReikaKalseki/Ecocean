using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

using ReikaKalseki.DIAlterra;
using ReikaKalseki.SeaToSea;

using SMLHelper.V2.Assets;
using SMLHelper.V2.Handlers;
using SMLHelper.V2.Utility;

using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace ReikaKalseki.Ecocean {

	public class HeatColumnFog : Spawnable {

		public static Color fogColor0 = new Color(0.1F, 1.875F, 1.0F);//new Color(0.15F, 0.1F, 0.1F);
		public static Color fogColor1 = new Color(0.1F, 1.875F, 1.0F);
		public static Color fogColor2 = new Color(0.1F, 1.875F, 1.0F);

		internal HeatColumnFog() : base("HeatColumnFog", "", "") {
			
		}

		public override GameObject GetGameObject() {
			GameObject podRef = ObjectUtil.lookupPrefab("bfe8345c-fe3c-4c2b-9a03-51bcc5a2a782");
			GasPod pod = podRef.GetComponent<GasPod>();
			GameObject fog = pod.gasEffectPrefab;
			GameObject world = fog.clone();
			world.EnsureComponent<TechTag>().type = TechType;
			world.EnsureComponent<PrefabIdentifier>().ClassId = ClassID;
			world.EnsureComponent<HeatColumnFogTag>();
			world.removeComponent<UWE.TriggerStayTracker>();
			world.removeComponent<FMOD_StudioEventEmitter>();
			world.removeComponent<FMOD_CustomEmitter>();
			world.removeChildObject("xflash");
			Renderer[] r0 = world.GetComponentsInChildren<Renderer>();
			foreach (ParticleSystem pp in world.GetComponentsInChildren<ParticleSystem>()) {
				ParticleSystem.MainModule main = pp.main;
				main.startColor = Color.white.ToAlpha(main.startColor.color.a*0.4F);
				//main.startSizeMultiplier *= 2.5F;
				ParticleSystem.SizeOverLifetimeModule size = pp.sizeOverLifetime;
				size.sizeMultiplier *= 2.5F;
				main.startLifetimeMultiplier *= 0.67F;//15.0F;
				//world.GetComponent<VFXDestroyAfterSeconds>().lifeTime *= 15F;
				//world.GetComponent<VFXUnparentAfterSeconds>().timer *= 15F;
				ParticleSystem.VelocityOverLifetimeModule speed = pp.velocityOverLifetime;
				speed.x = 0;
				speed.y = 4.5F;
				speed.z = 0;
				ParticleSystem.EmissionModule em = pp.emission;
				ParticleSystem.ShapeModule sh = pp.shape;
				sh.shapeType = ParticleSystemShapeType.Box;
				sh.scale = Vector3.one;
			}
			foreach (Renderer r in r0) {
				GameObject go = r.gameObject;
				if (go.name == "xSmkLong")
					r.materials[0].SetColor("_Color", fogColor2);
				else if (go.name == "xSmk")
					r.materials[0].SetColor("_Color", fogColor1);
				else
					r.materials[0].SetColor("_Color", fogColor0);
				//r.materials[0].SetFloat("_SrcBlend", 5);
			}
			return world;
		}

	}

	internal class HeatColumnFogTag : HeatColumnObject {

		void Start() {

		}

		new void Update() {
			base.Update();
			if (body) {
				body.isKinematic = false;
				body.velocity = Vector3.up * 1.5F;
			}
		}

	}
}
