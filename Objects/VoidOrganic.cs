using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

using ReikaKalseki.DIAlterra;
using ReikaKalseki.SeaToSea;

using SMLHelper.V2.Assets;
using SMLHelper.V2.Handlers;
using SMLHelper.V2.Utility;

using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace ReikaKalseki.Ecocean {

	public class VoidOrganic : WorldCollectedItem {

		internal VoidOrganic(XMLLocale.LocaleEntry e) : base(e, "505e7eff-46b3-4ad2-84e1-0fadb7be306c") {
			OnFinishedPatching += () => {
				CraftData.pickupSoundList[TechType] = "event:/loot/pickup_seatreaderpoop";
			};
			renderModify = r => {
				GameObject world = r.gameObject.FindAncestor<PrefabIdentifier>().gameObject;
				world.removeComponent<EnzymeBall>();
				Pickupable pp = world.EnsureComponent<Pickupable>();
				pp.SetTechTypeOverride(TechType);
				world.EnsureComponent<VoidOrganicTag>();
				Color c = new Color(0.2F, 1F, 0.67F);
				r.materials[0].SetColor("_Color", c);
				r.materials[0].SetColor("_SpecColor", c);
				r.materials[0].SetFloat("_Fresnel", 1F);
				r.materials[0].SetFloat("_Shininess", 5F);
				r.materials[0].SetFloat("_SpecInt", 1.5F);
				r.materials[0].SetFloat("_EmissionLM", 200F);
				r.materials[0].SetFloat("_EmissionLMNight", 200F);
				r.materials[0].SetFloat("_MyCullVariable", 1.6F);
				Light l = world.addLight(1, 5, c);
				Light l2 = world.addLight(0.5F, 25, c);
				world.GetComponent<Collider>().isTrigger = false;
			};
		}

	}

	internal class VoidOrganicTag : HeatColumnObject {
		
		void Start() {

		}

		new void Update() {
			base.Update();
			if (body && !gameObject.FindAncestor<StorageContainer>()) {
				body.isKinematic = false;
				body.velocity = Vector3.up * 4;
			}
		}

	}
}
