using System;
using System.IO;
using System.Xml;
using System.Linq;
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
using ECCLibrary;

namespace ReikaKalseki.Ecocean {
	
	public class SonarFlora : Spawnable {
		
		private readonly XMLLocale.LocaleEntry locale;
	        
	    internal SonarFlora(XMLLocale.LocaleEntry e) : base(e.key, e.name, e.desc) {
			locale = e;
	    }
			
	    public override GameObject GetGameObject() {
			GameObject world = new GameObject(ClassID+"(Clone)");
			world.EnsureComponent<TechTag>().type = TechType;
			world.EnsureComponent<PrefabIdentifier>().ClassId = ClassID;
			world.EnsureComponent<LargeWorldEntity>().cellLevel = LargeWorldEntity.CellLevel.VeryFar;
			SonarFloraTag g = world.EnsureComponent<SonarFloraTag>();
			g.setShape(6);
			return world;
	    }
			
	}
		
	public class SonarFloraTag : SonarOnlyRenderer {
		
		public Vector3 baseOffset = Vector3.zero;
		public float minSize = 0.125F;
		public float maxSize = 0.33F;
		public int blobCount = 120;
		internal Vector3 aoe = Vector3.one*0.01F;
		
		public void setShape(float r) {
			setShape(Vector3.one*r);
		}
		
		public void setShape(Vector3 c) {
			aoe = c;
			foreach (SonarRender r in renderers)
				UnityEngine.GameObject.DestroyImmediate(r.renderer.gameObject);
			renderers.Clear();
		}
		
		protected override void Update() {
			base.Update();
			if (renderers.Count == 0) {
				foreach (Renderer r in GetComponentsInChildren<Renderer>())
					renderers.Add(new SonarRender(r));
				if (EcoceanMod.config.getBoolean(ECConfig.ConfigEntries.GOODCREEPSONAR)) {
					for (int i = renderers.Count; i < blobCount; i++) {
						GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
						sphere.transform.localScale = Vector3.one*UnityEngine.Random.Range(minSize, maxSize);
						sphere.name = "SonarBlob";
						sphere.transform.SetParent(transform);
						sphere.transform.localPosition = MathUtil.getRandomVectorAround(baseOffset, aoe);
						ObjectUtil.removeComponent<Collider>(sphere);
						ECCHelpers.ApplySNShaders(sphere, new UBERMaterialProperties(0, 10, 8));
						Renderer r = sphere.GetComponentInChildren<Renderer>();
						RenderUtil.setEmissivity(r, 8);
						RenderUtil.setGlossiness(r, 10, 0, 0);
						r.materials[0].SetColor("_GlowColor", Color.red);
						r.receiveShadows = false;
						r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
						renderers.Add(new SonarRender(r));
					}
				}
				else if (renderers.Count == 0) {
					SNUtil.log("Adding cheap sonar flora halo");
					GameObject sphere = ObjectUtil.getChildObject(gameObject, "SonarHalo");
					if (!sphere) {
						sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
						sphere.name = "SonarHalo";
						sphere.transform.SetParent(transform);
						sphere.transform.localPosition = Vector3.zero;
						sphere.transform.localRotation = Quaternion.identity;
						sphere.transform.localScale = Vector3.one*12;
						ECCHelpers.ApplySNShaders(sphere, new UBERMaterialProperties(0, 10, 5));
						ObjectUtil.removeComponent<Collider>(sphere);
					}
					Renderer r = sphere.GetComponentInChildren<Renderer>();
					renderers.Add(prepareCheapSonarHalo(r));
				}
			}
		}
	
		public static SonarRender prepareCheapSonarHalo(Renderer r) {
			r.materials[0].SetColor("_GlowColor", Color.red);
			r.materials[0].SetColor("_Color", new Color(1, 0.1F, 0.1F, 1));
			r.receiveShadows = false;
			r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			RenderUtil.setEmissivity(r, 5);
			RenderUtil.setGlossiness(r, 10, 0, 0);
			r.materials[0].EnableKeyword("FX_BUILDING");
			r.materials[0].SetVector("_BuildParams", new Vector4(0.4F, 1, 0.25F, 0.2F));
			r.materials[0].SetColor("_BorderColor", new Color(4.25F, 0, 0, 1));
			r.materials[0].SetFloat("_NoiseThickness", 0.07F);
			r.materials[0].SetFloat("_NoiseStr", 0.15F);
			SonarRender sr = new SonarRender(r);
			sr.fadeInSpeed = 2.5F;
			sr.fadeOutSpeed = 0.5F;
			return sr;
		}
		
	}
}
