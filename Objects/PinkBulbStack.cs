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

	public class PinkBulbStack : BasicCustomPlant {

		public PinkBulbStack(XMLLocale.LocaleEntry e) : base(e, new FloraPrefabFetch(VanillaFlora.REDWORT), "daff0e31-dd08-4219-8793-39547fdb745e", "Samples") {
			finalCutBonus = 4;
			OnFinishedPatching += () => { this.addPDAEntry(e.pda, 4F, e.getField<string>("header")); };
		}

		public override Vector2int SizeInInventory {
			get { return new Vector2int(1, 1); }
		}

		public override void modifySeed(GameObject go) {
			Eatable ea = go.EnsureComponent<Eatable>();
			ea.decomposes = true;
			ea.foodValue = 6;
			ea.waterValue = 4;
			ea.kDecayRate = ObjectUtil.lookupPrefab(TechType.CreepvinePiece).GetComponent<Eatable>().kDecayRate;
			ea.timeDecayStart = DayNightCycle.main.timePassedAsFloat;
		}

		public override void prepareGameObject(GameObject go, Renderer[] r0) {
			base.prepareGameObject(go, r0);

			Transform mdl = r0[0].transform.parent;
			LiveMixin lv = go.GetComponent<LiveMixin>();
			lv.data.maxHealth = 2; //only applies to farmed
			lv.data.knifeable = true;
			lv.health = lv.maxHealth;

			foreach (Renderer r in r0)
				r.gameObject.destroy(false);

			CapsuleCollider cc = go.GetComponentInChildren<CapsuleCollider>();
			cc.radius *= 0.75F;
			cc.height *= 0.875F;
			cc.isTrigger = false;

			GameObject pfb = ObjectUtil.lookupPrefab(DecoPlants.PINK_BULB_STACK.prefab);
			foreach (Renderer r in pfb.GetComponentsInChildren<Renderer>()) {
				if (r.name.Contains("LOD"))
					continue;
				GameObject rg = UnityEngine.Object.Instantiate(r.gameObject);
				rg.transform.SetParent(mdl);
				rg.transform.localPosition = r.transform.localPosition;
				rg.transform.localRotation = r.transform.localRotation;
				rg.transform.Rotate(new Vector3(-90, 0, 0), Space.Self);
				rg.transform.localScale = r.transform.localScale * 81.70F; //why do they DO this; grue clusters are still worse though
			}

			go.EnsureComponent<PinkBulbStackTag>();
		}

		public override float getScaleInGrowbed(bool indoors) {
			return 0.33F;
		}

		public override bool isResource() {
			return false;
		}

		protected override bool isExploitable() {
			return false;
		}

		public override Plantable.PlantSize getSize() {
			return Plantable.PlantSize.Small;
		}

		public override bool canGrowAboveWater() {
			return false;
		}

		public override bool canGrowUnderWater() {
			return true;
		}

		class PinkBulbStackTag : MonoBehaviour {

			void Start() {
				foreach (Renderer r in this.GetComponentsInChildren<Renderer>()) {
					if (r.gameObject.name.StartsWith("coral_reef_plant_middle_05", StringComparison.InvariantCultureIgnoreCase))
						r.gameObject.destroy(false);
				}
			}

		}

	}
}
