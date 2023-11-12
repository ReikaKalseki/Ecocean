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
	
	public class PinkLeaves : BasicCustomPlant, MultiTexturePrefab {
		
		public PinkLeaves(XMLLocale.LocaleEntry e) : base(e, new FloraPrefabFetch(DecoPlants.BANANA_LEAF.prefab), "daff0e31-dd08-4219-8793-39547fdb745e", "Cuttings") {
			finalCutBonus = 1;
			glowIntensity = 1.2F;
			collectionMethod = HarvestType.DamageAlive;
			//OnFinishedPatching += () => {addPDAEntry(e.pda, 4F, e.getField<string>("header"));};
		}
		
		public override Vector2int SizeInInventory {
			get {return new Vector2int(1, 1);}
		}
		
		public override void prepareGameObject(GameObject go, Renderer[] r0) {
			base.prepareGameObject(go, r0);
			
			Transform mdl = r0[0].transform.parent;
			
			foreach (Renderer r in r0) {
				if (r)
					UnityEngine.Object.Destroy(r.gameObject);
			}
			
			if (!go.GetComponentInChildren<Collider>()) {
				CapsuleCollider cc = go.EnsureComponent<CapsuleCollider>();
				cc.radius = 0.67F;
				cc.center = Vector3.up*0.9F;
				cc.height = 1.75F;
				cc.isTrigger = true;
			}
			
			go.layer = LayerID.Useable;
			
			go.EnsureComponent<LiveMixin>().copyObject<LiveMixin>(ObjectUtil.lookupPrefab(TechType.SeaCrown).GetComponent<LiveMixin>());
			
			GameObject pfb = ObjectUtil.lookupPrefab(DecoPlants.BANANA_LEAF.prefab);
			foreach (Renderer r in pfb.GetComponentsInChildren<Renderer>()) {
				if (!r || r.name.Contains("LOD"))
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
				if (!r)
					continue;
				RenderUtil.makeTransparent(r.materials[0]);
				if (r.materials.Length > 1)
					RenderUtil.setEmissivity(r.materials[1], 2);
				r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
				r.receiveShadows = false;
				foreach (Material m in r.materials) {
					m.SetColor("_GlowColor", Color.white);
					m.EnableKeyword("UWE_WAVING");
					m.SetVector("_Scale", new Vector4(0.05F, 0.05F, 0.05F, 0.05F));
					m.SetVector("_Frequency", new Vector4(0.3F, 0.3F, 0.3F, 0.3F));
					m.SetFloat("_Cutoff", 0.05F);
				}
			}
			
			Light l = ObjectUtil.addLight(go);
			l.intensity = 0.5F;
			l.range = 8;
			l.lightShadowCasterMode = LightShadowCasterMode.Default;
			l.shadows = LightShadows.Soft;
			l.color = new Color(1F, 153/255F, 1F, 1F);
			
			go.EnsureComponent<PinkLeavesTag>();
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
			return true;
		}
	}
		
	class PinkLeavesTag : MonoBehaviour {
		
		private GrownPlant grown;
		
		void Start() {
			grown = gameObject.GetComponent<GrownPlant>();
			if (grown) {
    			gameObject.SetActive(true);
    			gameObject.transform.localScale = Vector3.one;
    			Renderer[] r0 = GetComponentsInChildren<Renderer>();
    			if (r0.Length > 1) {
    				for (int i = 1; i < r0.Length; i++) {
    					UnityEngine.Object.Destroy(r0[i].gameObject);
	    			}
    			}
    			r0[0].transform.localScale = Vector3.one*0.2F;
				EcoceanMod.pinkLeaves.prepareGameObject(gameObject, r0);
				LiveMixin lv = gameObject.EnsureComponent<LiveMixin>();
				lv.copyObject<LiveMixin>(ObjectUtil.lookupPrefab(TechType.SeaCrown).GetComponent<LiveMixin>());
				if (lv.damageInfo == null)
					lv.damageInfo = new DamageInfo();
				lv.ResetHealth();
    		}
		}
		
	}
}
