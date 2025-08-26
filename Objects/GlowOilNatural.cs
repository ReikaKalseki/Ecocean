using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;

using ReikaKalseki.DIAlterra;

using SMLHelper.V2.Assets;
using SMLHelper.V2.Handlers;
using SMLHelper.V2.Utility;

using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace ReikaKalseki.Ecocean {

	public class GlowOilNatural : PickedUpAsOtherItem {

		internal GlowOilNatural() : base("NaturalGlowOil", EcoceanMod.glowOil.TechType) {

		}

		protected sealed override Atlas.Sprite GetItemSprite() {
			return EcoceanMod.glowOil.getSprite();
		}

		public override GameObject GetGameObject() {
			GameObject world = UnityEngine.Object.Instantiate(EcoceanMod.glowOil.GetGameObject());
			world.EnsureComponent<TechTag>().type = TechType;
			world.EnsureComponent<PrefabIdentifier>().ClassId = ClassID;
			world.EnsureComponent<Pickupable>().SetTechTypeOverride(TechType);
			world.fullyEnable();
			return world;
		}

		protected override void ProcessPrefab(GameObject go) {
			base.ProcessPrefab(go);
			go.EnsureComponent<GlowOilTag>().enabled = true;
		}

		public override int getNumberCollectedAs() {
			return EcoceanMod.config.getInt(ECConfig.ConfigEntries.GLOWCOUNT);
		}

		public void register() {
			this.Patch();
			PDAManager.PDAPage p = EcoceanMod.glowOil.getPDAEntry();
			KnownTechHandler.Main.SetAnalysisTechEntry(TechType, new List<TechType>() { template });
			PDAScanner.EntryData e = new PDAScanner.EntryData();
			e.key = TechType;
			e.locked = true;
			e.scanTime = 3;
			e.encyclopedia = p.id;
			PDAHandler.AddCustomScannerEntry(e);
		}
	}
}
