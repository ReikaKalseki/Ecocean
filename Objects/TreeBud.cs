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

	[Obsolete]
	public class TreeBud : Spawnable {

		private static readonly List<TechType> drops = new List<TechType>();

		private readonly XMLLocale.LocaleEntry locale;

		internal TreeBud(XMLLocale.LocaleEntry e) : base(e.key, e.name, e.desc) {
			locale = e;
		}

		static TreeBud() {
			//addDrop(TechType.TreeMushroomPiece, 250);
			addDrop(TechType.Lithium);
			addDrop(TechType.Diamond);
		}

		public static void addDrop(TechType drop) {
			drops.Add(drop);
		}

		public override GameObject GetGameObject() {
			GameObject world = ObjectUtil.createWorldObject(VanillaFlora.PINECONE.getRandomPrefab(false));
			world.EnsureComponent<TechTag>().type = TechType;
			world.EnsureComponent<PrefabIdentifier>().ClassId = ClassID;
			Renderer r = world.GetComponentInChildren<Renderer>();
			RenderUtil.swapTextures(EcoceanMod.modDLL, r, "Textures/Plants/TreeBud");
			r.material.EnableKeyword("UWE_WAVING");
			r.material.SetFloat("_Shininess", 0F);
			r.material.SetFloat("_SpecInt", 0F);
			r.material.SetColor("_Color", Color.white);
			r.material.SetVector("_Scale", new Vector4(0.24F, 0.1F, 0.24F, 0.2F));
			r.material.SetVector("_Frequency", new Vector4(1.0F, 1.5F, 1.5F, 1.2F));
			r.material.SetVector("_Speed", new Vector4(0.1F, 0.05F, 0.0F, 0.0F));
			r.material.SetVector("_ObjectUp", new Vector4(0F, 0F, 1F, 0F));
			r.material.SetFloat("_WaveUpMin", 0F);
			RenderUtil.setEmissivity(r, 1);
			BreakableResource res = world.GetComponent<BreakableResource>();
			res.breakText = "Harvest fungal bud";
			res.prefabList.Clear();
			res.numChances = 0;
			res.defaultPrefab = ObjectUtil.lookupPrefab(TechType.TreeMushroomPiece);
			res.breakSound = SoundManager.getSound(CraftData.GetPickupSound(TechType.SeaTreaderPoop));
			world.EnsureComponent<TreeBudTag>();
			return world;
		}

		class TreeBudTag : MonoBehaviour {

			void OnBreakResource() {
				BreakableResource res = this.GetComponent<BreakableResource>();
				foreach (TechType tt in drops) {
					res.SpawnResourceFromPrefab(ObjectUtil.lookupPrefab(tt));
				}
				int n = UnityEngine.Random.Range(2, 6); //2-5
				for (int i = 0; i < n; i++)
					this.dropFungalSample(res);
				SoundManager.playSoundAt(SoundManager.buildSound("event:/loot/pickup_seatreaderpoop"), transform.position);
			}

			private void dropFungalSample(BreakableResource res) {
				GameObject go = UnityEngine.Object.Instantiate<GameObject>(ObjectUtil.lookupPrefab("01de572d-5549-44c6-97cf-645b07d1c79d"), transform.position + (transform.up * res.verticalSpawnOffset), Quaternion.identity);
				if (!go.GetComponent<Rigidbody>()) {
					go.AddComponent<Rigidbody>();
				}
				go.GetComponent<Rigidbody>().isKinematic = false;
				go.GetComponent<Rigidbody>().AddTorque(Vector3.right * UnityEngine.Random.Range(3, 6));
				go.GetComponent<Rigidbody>().AddForce(base.transform.up * 0.1f);
				go.GetComponent<Pickupable>().SetTechTypeOverride(TechType.TreeMushroomPiece);
			}

			void Update() {
				transform.localScale = new Vector3(1, 2, 1);
			}

		}

	}
}
