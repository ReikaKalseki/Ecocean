using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

using ECCLibrary;

using ReikaKalseki.DIAlterra;

using SMLHelper.V2.Assets;
using SMLHelper.V2.Handlers;
using SMLHelper.V2.Utility;

using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace ReikaKalseki.Ecocean {

	public class HeatColumnShell : InteractableSpawnable {

		public static float fresnelAmount = 0.925F;
		public static float specStrength = 0.02F;

		private string fixedUUID = Guid.NewGuid().ToString();

		internal HeatColumnShell(XMLLocale.LocaleEntry e) : base(e) {
			scanTime = 15;
		}

		public void register() {
			Patch();
			registerEncyPage();
		}

		public override GameObject GetGameObject() {
			GameObject world = GameObject.CreatePrimitive(PrimitiveType.Cylinder).setName("HeatColumnShell(Clone)");// ObjectUtil.createWorldObject("42b38968-bd3a-4bfd-9d93-17078d161b29").setName(ClassID+"[Clone]");
			world.GetComponent<Collider>().isTrigger = true;
			world.EnsureComponent<TechTag>().type = TechType;
			world.EnsureComponent<PrefabIdentifier>().ClassId = ClassID;
			world.EnsureComponent<LargeWorldEntity>().cellLevel = LargeWorldEntity.CellLevel.Global;
			//world.removeChildObject("xCurrenBubbles");
			world.layer = LayerID.Useable;
			world.EnsureComponent<HeatColumnShellTag>();

			ECCHelpers.ApplySNShaders(world, new UBERMaterialProperties(0, 0, 5));
			Renderer r = world.GetComponentInChildren<Renderer>();
			RenderUtil.makeTransparent(r);
			r.receiveShadows = false;
			r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			Texture2D tex = TextureManager.getTexture(EcoceanMod.modDLL, "Textures/heatcolumnshell");
			//r.materials[0].SetTexture("_MainTex", tex);
			//r.materials[0].EnableKeyword("MARMO_EMISSION");
			r.materials[0].EnableKeyword("MARMO_SPECMAP");
			r.materials[0].EnableKeyword("UWE_WAVING");
			//RenderUtil.setEmissivity(r, 0.2F);
			r.materials[0].SetFloat("_SpecInt", 0.02F);
			r.materials[0].SetFloat("_Fresnel", fresnelAmount);
			r.materials[0].SetTexture("_SpecTex", tex);
			r.materials[0].SetFloat("_MyCullVariable", 0.77F);
			r.materials[0].SetFloat("_IBLreductionAtNight", 0);
			//AcidicBrineDamageTrigger acid = world.EnsureComponent<AcidicBrineDamageTrigger>();


			return world;
		}

		protected override void ProcessPrefab(GameObject go) {
			base.ProcessPrefab(go);
			//go.EnsureComponent<PrefabIdentifier>().id = fixedUUID;
		}

	}

	public class HeatColumnShellTag : MonoBehaviour {

		//private Current current;
		private CapsuleCollider collider;
		private Renderer render;

		private float scaleFactor = UnityEngine.Random.Range(20F, 40F);

		private float age;

		void Start() {
			transform.rotation = Quaternion.identity;
			transform.localScale = new Vector3(1, 1, 1);
		}

		void Update() {
			if (!render)
				render = this.GetComponentInChildren<Renderer>();
			if (!collider)
				collider = this.GetComponentInChildren<CapsuleCollider>();

			float f = scaleFactor+2*Mathf.Sin(age*0.2F);
			transform.localScale = new Vector3(f, 300, f);
			if (Player.main)
				transform.position = transform.position.setY(Mathf.Min(-150, Player.main.transform.position.y-50));
			//collider.radius = 

			render.materials[0].SetTexture("_SpecTex", TextureManager.getTexture(EcoceanMod.modDLL, "Textures/heatcolumnshell"));

			age += Time.deltaTime;

			if (age > 1 && age < 2 && Vector3.Distance(transform.position, Vector3.zero) <= 10) {
				gameObject.destroy(false);
				return;
			}

			f = 1;
			if (age > 30) {
				f = 0;
				if (gameObject != PDAScanner.scanTarget.gameObject)
					gameObject.destroy();
			}
			else if (age > 20) {
				f = 1-((age - 20) / 10F);
			}
			else if (age < 5) {
				f = age / 5F;
			}
			if (f > 0 && Player.main) {
				double dist = (transform.position-Player.main.transform.position).setY(0).magnitude;
				f *= (float)MathUtil.linterpolate(dist, 50, 200, 0.3, 1, true);
			}
			//RenderUtil.setEmissivity(render, 0.004F*f);
			render.materials[0].SetFloat("_SpecInt", f * HeatColumnShell.specStrength);

			Color c = new Color(1F, 2.5F, 1.5F);
			render.materials[0].SetColor("_Color", c);
			f = Mathf.Max(0, 0.15F+0.85F*Mathf.Sin(age*0.34F));
			render.materials[0].SetColor("_SpecColor", new Color(1, f, 1));
			render.materials[0].color = c;
		}

	}
}
