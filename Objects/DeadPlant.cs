using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using ReikaKalseki.DIAlterra;

using SMLHelper.V2.Assets;
using SMLHelper.V2.Handlers;
using SMLHelper.V2.Utility;

using UnityEngine;

namespace ReikaKalseki.Ecocean {

	public class DeadPlant : BasicCustomPlant, MultiTexturePrefab {

		public DeadPlant() : base("DeadFarmPlant", "Dead Plant", "", new FloraPrefabFetch(VanillaFlora.MARBLEMELON), "f2ae9bd0-c6ac-46c8-82c0-ce31ebdfb75c") {
			finalCutBonus = 0;
			glowIntensity = 0;
			collectionMethod = HarvestType.None;
		}

		protected override bool generateSeed() {
			return true;
		}

		public override Vector2int SizeInInventory {
			get { return new Vector2int(1, 1); }
		}

		public override void prepareGameObject(GameObject go, Renderer[] r0) {
			base.prepareGameObject(go, r0);

			RenderUtil.swapToModdedTextures(r0, this);

			foreach (Renderer r in r0) {
				if (!r)
					continue;
				r.transform.localScale = Vector3.one * 0.6F;
				foreach (Material m in r.materials) {
					//m.SetVector("_Frequency", new Vector4(0.3F, 0.3F, 0.3F, 0.3F));
					//m.SetFloat("_Cutoff", 0.05F);
				}
			}

			go.removeComponent<PickPrefab>();
			//go.removeComponent<LiveMixin>();
			go.removeComponent<Pickupable>();
		}

		public Dictionary<int, string> getTextureLayers(Renderer r) {
			return new Dictionary<int, string> { { 0, "" }, { 1, "" } };
		}

		public override float getScaleInGrowbed(bool indoors) {
			return 0.33F;
		}

		public override bool isResource() {
			return false;
		}

		public override Plantable.PlantSize getSize() {
			return Plantable.PlantSize.Small;
		}

		public override bool canGrowAboveWater() {
			return true;
		}

		public override bool canGrowUnderWater() {
			return true;
		}
	}
}
