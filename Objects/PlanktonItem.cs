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
	
	public class PlanktonItem : BasicCraftingItem {
	        
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

		public override CraftTree.Type FabricatorType {
			get {
				return CraftTree.Type.None;
			}
		}

		public override TechGroup GroupForPDA {
			get {
				return TechGroup.Uncategorized;
			}
		}

		public override TechCategory CategoryForPDA {
			get {
				return TechCategory.Misc;
			}
		}
/*
		protected sealed override Atlas.Sprite GetItemSprite() {
			return TextureManager.getSprite(EcoceanMod.modDLL, "Textures/Items/PlanktonItem");
		}
		
		public Atlas.Sprite getSprite() {
			return GetItemSprite();
		}
*//*			
	    public override GameObject GetGameObject() {
			GameObject world = ObjectUtil.createWorldObject("18229b4b-3ed3-4b35-ae30-43b1c31a6d8d"); //enzyme 42: "505e7eff-46b3-4ad2-84e1-0fadb7be306c"
			world.EnsureComponent<TechTag>().type = TechType;
			world.EnsureComponent<PrefabIdentifier>().ClassId = ClassID;
			ResourceTracker rt = world.EnsureComponent<ResourceTracker>();
			rt.techType = TechType;
			rt.overrideTechType = TechType;
			world.GetComponentInChildren<PickPrefab>().pickTech = TechType;
			//world.GetComponent<Rigidbody>().isKinematic = true;
			Pickupable pp = world.GetComponent<Pickupable>();
			pp.overrideTechType = TechType;
			WorldForces wf = world.GetComponent<WorldForces>();
			wf.underwaterGravity = 0;
			wf.underwaterDrag *= 1;
			Rigidbody rb = world.GetComponent<Rigidbody>();
			rb.angularDrag *= 3;
			rb.maxAngularVelocity = 6;
			rb.drag = wf.underwaterDrag;
			//ObjectUtil.removeComponent<EnzymeBall>(world);
			//ObjectUtil.removeComponent<Plantable>(world);
			Plantable p = world.GetComponent<Plantable>();
			GameObject jellyseed = UnityEngine.Object.Instantiate(CraftData.GetPrefabForTechType(TechType.SnakeMushroomSpore));
			Plantable p2 = jellyseed.GetComponent<Plantable>();
			p.plantTechType = p2.plantTechType;
			p.growingPlant = p2.growingPlant;
			p.isSeedling = p2.isSeedling;
			p.linkedGrownPlant = p2.linkedGrownPlant;
			p.model = p2.model;
			p.modelEulerAngles = p2.modelEulerAngles;
			p.modelIndoorScale = p2.modelIndoorScale;
			p.modelLocalPosition = p2.modelLocalPosition;
			p.modelScale = p2.modelScale;
			p.pickupable = pp;
			//jellyseed.GetComponent<Plantable>().CopyFields<Plantable>(p, BindingFlags.Public | BindingFlags.NonPublic);
			//ObjectUtil.dumpObjectData(p);
			BasicCustomPlant.setPlantSeed(TechType, EcoceanMod.glowShroom);
			PlanktonItemTag g = world.EnsureComponent<PlanktonItemTag>();
			world.EnsureComponent<LargeWorldEntity>().cellLevel = LargeWorldEntity.CellLevel.VeryFar;
			Light l = ObjectUtil.addLight(world);
			l.bounceIntensity *= 2;
			l.color = new Color(0.5F, 0.8F, 1F, 1F);
			l.intensity = 0;
			l.range = MAX_RADIUS;
			Renderer r = world.GetComponentInChildren<Renderer>();
			RenderUtil.setEmissivity(r.materials[0], 0, "GlowStrength");
			RenderUtil.setEmissivity(r.materials[1], 0, "GlowStrength");
			r.materials[0].SetFloat("_Shininess", 10);
			r.materials[0].SetFloat("_SpecInt", 3);
			r.materials[0].SetFloat("_Fresnel", 1);
			setupRenderer(r, "Main");
			RenderUtil.makeTransparent(r.materials[1]);
			r.materials[0].EnableKeyword("FX_KELP");
			r.materials[0].SetColor("_Color", new Color(0, 0, 0, 1F));
			return world;
	    }
		*/
		public void register() {
			Patch();
		}
			
	}
}
