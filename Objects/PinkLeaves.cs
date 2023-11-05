using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

using UnityEngine;

using SMLHelper.V2.Handlers;
using SMLHelper.V2.Assets;
using SMLHelper.V2.Utility;

using ReikaKalseki.DIAlterra;

namespace ReikaKalseki.Ecocean {
	
	public class PinkLeaves : BasicCustomPlant, MultiTexturePrefab<FloraPrefabFetch> {
		
		public PinkLeaves(XMLLocale.LocaleEntry e) : base(e, new FloraPrefabFetch(DecoPlants.BANANA_LEAF.prefab), "daff0e31-dd08-4219-8793-39547fdb745e", "Cuttings") {
			finalCutBonus = 1;
			glowIntensity = 1.2F;
			//OnFinishedPatching += () => {addPDAEntry(e.pda, 4F, e.getField<string>("header"));};
		}
		
		public override Vector2int SizeInInventory {
			get {return new Vector2int(1, 1);}
		}
		
		public override void prepareGameObject(GameObject go, Renderer[] r0) {
			base.prepareGameObject(go, r0);
			
			Transform mdl = r0[0].transform.parent;
			
			foreach (Renderer r in r0)
				UnityEngine.Object.Destroy(r.gameObject);
			
			//CapsuleCollider cc = go.EnsureComponent<CapsuleCollider>();
			//cc.radius = 1;
			//cc.height = 0.4F;
			//cc.isTrigger = true;
			
			GameObject pfb = ObjectUtil.lookupPrefab(DecoPlants.BANANA_LEAF.prefab);
			foreach (Renderer r in pfb.GetComponentsInChildren<Renderer>()) {
				if (r.name.Contains("LOD"))
					continue;
				GameObject rg = UnityEngine.Object.Instantiate(r.gameObject);
				rg.transform.SetParent(mdl);
				rg.transform.localPosition = r.transform.localPosition;
				rg.transform.localRotation = r.transform.localRotation;
				rg.transform.localScale = r.transform.localScale*0.33F;
			}
			r0 = go.GetComponentsInChildren<Renderer>();
			RenderUtil.swapToModdedTextures(r0, this);
			foreach (Renderer r in r0) {
				RenderUtil.makeTransparent(r.materials[0]);
				foreach (Material m in r.materials) {
					m.SetColor("_GlowColor", Color.white);
					m.EnableKeyword("UWE_WAVING");
					m.SetVector("_Scale", new Vector4(0.05F, 0.05F, 0.05F, 0.05F));
					m.SetVector("_Frequency", new Vector4(0.3F, 0.3F, 0.3F, 0.3F));
					m.SetFloat("_Cutoff", 0.05F);
				}
			}
		}
		
		public Dictionary<int, string> getTextureLayers(Renderer r) {
			return new Dictionary<int, string>{{0, "Leaf"}, {1, ""}};
		}
		
		public override float getScaleInGrowbed(bool indoors) {
			return 0.33F;
		}
		
		public override bool isResource() {
			return false;
		}
		
		public override Plantable.PlantSize getSize() {
			return Plantable.PlantSize.Large;
		}
		
		public override bool canGrowAboveWater() {
			return true;
		}
		
		public override bool canGrowUnderWater() {
			return false;
		}
		
	}
}
