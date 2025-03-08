using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Reflection;
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
	
	public class SeamothPlanktonScoop : SeamothModule {
		
	    internal SeamothPlanktonScoop() : base(EcoceanMod.locale.getEntry("PlanktonScoop"), "d290b5da-7370-4fb8-81bc-656c6bde78f8") {
			if (QModManager.API.QModServices.Main.ModPresent("SeaToSea")) //does not work
				preventNaturalUnlock();
	    }

		public override QuickSlotType QuickSlotType {
			get {
				return QuickSlotType.Passive;
			}
		}

		public override SeamothModule.SeamothModuleStorage getStorage() {
			return new SeamothModule.SeamothModuleStorage("SCOOP STORAGE", StorageAccessType.BOX, 6, 6);
		}
		
		public void register() {
			addIngredient(TechType.VehicleStorageModule, 1);
			addIngredient(TechType.PropulsionCannon, 1);
			addIngredient(TechType.FiberMesh, 2);
			addIngredient(EcoceanMod.mushroomVaseStrand.seed.TechType, 3);
			Patch();
		}
		
		public static bool checkAndTryScoop(SeaMoth sm, float dT, TechType harvest) {
			if (sm.GetComponent<Rigidbody>().velocity.magnitude >= 4 && InventoryUtil.vehicleHasUpgrade(sm, EcoceanMod.planktonScoop.TechType)) {
				if (UnityEngine.Random.Range(0F, 1F) < 0.075F*dT*EcoceanMod.config.getFloat(ECConfig.ConfigEntries.PLANKTONRATE)) {
					foreach (SeamothStorageContainer sc in sm.GetComponentsInChildren<SeamothStorageContainer>(true)) {
						TechTag tt = sc.GetComponent<TechTag>();
						if (tt && tt.type == EcoceanMod.planktonScoop.TechType) {
							GameObject go = ObjectUtil.createWorldObject(harvest, true, false);
							sc.container.AddItem(go.GetComponentInChildren<Pickupable>());
							if (sc.container.IsFull())
								SNUtil.writeToChat("Plankton scoop is full");
							break;
						}
					}
				}
				return true;
			}
			return false;
		}
	}
}
