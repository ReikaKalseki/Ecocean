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
	
	public class PlanktonItem : WorldCollectedItem {
	        
	    internal PlanktonItem(XMLLocale.LocaleEntry e) : base(e, "18229b4b-3ed3-4b35-ae30-43b1c31a6d8d") {
			sprite = TextureManager.getSprite(EcoceanMod.modDLL, "Textures/Items/PlanktonItem");
			//inventorySize = new Vector2int(2, 1);
			
			this.renderModify = r => {
				GameObject root = r.gameObject.FindAncestor<PrefabIdentifier>().gameObject;
				root.transform.localScale = Vector3.one*1F;
				ObjectUtil.removeComponent<PickPrefab>(root);
				Eatable ea = root.EnsureComponent<Eatable>();
				ea.decomposes = false;
				ea.foodValue = 6;
				ea.waterValue = 2;
				r.materials[0].DisableKeyword("FX_KELP");
				r.materials[0].color = Color.clear;
				r.materials[0].SetColor("_Color", Color.clear);
				r.materials[1].SetColor("_Color", new Color(0.5F, 8F, 2F, 1F));
				r.materials[1].EnableKeyword("FX_BUILDING");
				r.materials[1].SetFloat(ShaderPropertyID._Cutoff, 0.5F);
				r.materials[1].SetColor(ShaderPropertyID._BorderColor, new Color(0, 1, 0, 1));
				r.materials[1].SetVector(ShaderPropertyID._BuildParams, new Vector4(0.2f, 1f, 5f, -0.2f)); //last arg is speed, +ve is down
				r.materials[1].SetFloat(ShaderPropertyID._NoiseStr, 0.25f);
				r.materials[1].SetFloat(ShaderPropertyID._NoiseThickness, 0.0f);
				r.materials[1].SetFloat(ShaderPropertyID._BuildLinear, 0.22f);
				r.materials[1].SetFloat(ShaderPropertyID._MyCullVariable, 0f);
				r.materials[1].SetFloat(ShaderPropertyID._Built, 0.22f);
				((MeshRenderer)r).shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
				((MeshRenderer)r).receiveShadows = false;
			};
	    }
			
	}
}
