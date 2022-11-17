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
			//preventNaturalUnlock();
	    }

		public override QuickSlotType QuickSlotType {
			get {
				return QuickSlotType.Passive;
			}
		}

		public override SeamothModule.SeamothModuleStorage getStorage() {
			return new SeamothModule.SeamothModuleStorage("SCOOP STORAGE", 6, 6);
		}
		
		public void register() {
			addIngredient(TechType.VehicleStorageModule, 1);
			addIngredient(TechType.PropulsionCannon, 1);
			addIngredient(TechType.FiberMesh, 2);
			if (EcoceanMod.lockPlanktonScoop)
				preventNaturalUnlock();
			Patch();
		}
	}
}
