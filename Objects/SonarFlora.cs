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
			foreach (Renderer r in renderers)
				UnityEngine.GameObject.DestroyImmediate(r.gameObject);
			renderers.Clear();
		}
		
		protected override void Update() {
			base.Update();
			if (renderers.Count == 0) {
				renderers = GetComponentsInChildren<Renderer>().ToList();
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
					renderers.Add(r);
				}
			}
		}
		
	}
}
