using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

using ReikaKalseki.DIAlterra;

using SMLHelper.V2.Handlers;
using SMLHelper.V2.Utility;

using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace ReikaKalseki.Ecocean {

	[Obsolete]
	public class GrowingPlantViabilityTracker : MonoBehaviour {

		private static readonly Dictionary<TechType, BlightBehavior> maximumDensity = new Dictionary<TechType, BlightBehavior>();

		internal GrowingPlant plant;
		internal Planter growbed;
		internal TechType plantType;

		void Update() {
			if (!plant)
				plant = GetComponent<GrowingPlant>();
			if (!growbed)
				growbed = gameObject.FindAncestor<Planter>();
			if (plantType == TechType.None)
				tryFindSpecies();


			if (plant && growbed && plantType != TechType.None) {
				BlightBehavior bb = getBlight(plantType);
				if (bb != null && bb.isOverLimit(plant, growbed)) {
					if (UnityEngine.Random.Range(0F, 1F) < bb.deathChancePerFrame) {
						die();
					}
				}
			}
		}

		private void tryFindSpecies() {
			plantType = plant && plant.seed ? plant.seed.plantTechType : TechType.None;
		}

		private void die() {
			if (growbed.storageContainer.forceRemoveItem(plant.seed.pickupable)) {
				GameObject go = ObjectUtil.createWorldObject(EcoceanMod.deadBlighted.TechType);
				go.SetActive(false);
				growbed.storageContainer.container.AddItem(go.GetComponent<Pickupable>());
			}
		}

		public static void registerThresholds(TechType tt, int thresh, bool sameGrow, float perFrame) {
			BlightBehavior bb = new BlightBehavior(tt, thresh, sameGrow, perFrame);
			maximumDensity[tt] = bb;
		}

		private static BlightBehavior getBlight(TechType tt) {
			return maximumDensity.ContainsKey(tt) ? maximumDensity[tt] : null;
		}

		static GrowingPlantViabilityTracker() {
			registerThresholds(TechType.BloodVine, 2, true, 0.0001F);
			registerThresholds(TechType.BulboTree, 1, false, 0.002F);
			registerThresholds(TechType.HangingFruitTree, 1, false, 0.002F);
			registerThresholds(TechType.PurpleVegetablePlant, 2, false, 0.002F);
		}

		private class BlightBehavior {

			public readonly TechType species;
			public readonly int triggerThreshold;
			public readonly bool sameGrowbedOnly;
			public readonly float deathChancePerFrame;

			internal BlightBehavior(TechType tt, int tr, bool g, float f) {
				species = tt;
				triggerThreshold = tr;
				sameGrowbedOnly = g;
				deathChancePerFrame = f;
			}

			public bool isOverLimit(GrowingPlant gp, Planter p) {
				/* TODO
				if (sameGrowbedOnly) {

				}
				else {

				}*/
				return false;
			}

		}

	}
}
