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

	public class MushroomStack : BasicCustomPlant {

		private static readonly string STEM_NAME = "column_stem_";
		private static readonly string CHILD_NAME = "column_plant_";

		public MushroomStack(XMLLocale.LocaleEntry e) : base(e, new FloraPrefabFetch("99cdec62-302b-4999-ba49-f50c73575a4d"), "10d3c291-f343-4d7c-a68a-ecc64229d086", "Samples") {
			glowIntensity = 2F;
			finalCutBonus = 0;
			OnFinishedPatching += () => { this.addPDAEntry(e.pda, 15F, e.getField<string>("header")); };
		}

		public override Vector2int SizeInInventory {
			get { return new Vector2int(1, 1); }
		}

		public override void prepareGameObject(GameObject go, Renderer[] r0) {
			//SNUtil.writeToChat("Prepared mushroomstack "+go.GetFullHierarchyPath());
			base.prepareGameObject(go, r0);
			if (r0 != null) {
				foreach (Renderer r in r0) {
					if (r)
						r.gameObject.destroy();
				}
			}
			bool grown = go.GetFullHierarchyPath().ToLowerInvariant().Contains("planter");
			this.prepareObject(go);
			go.EnsureComponent<LargeWorldEntity>().cellLevel = LargeWorldEntity.CellLevel.Medium;
			MushroomStackTag g = go.EnsureComponent<MushroomStackTag>();
			int arms = UnityEngine.Random.Range(3, grown ? 5 : 6);
			for (int i = 0; i < arms; i++) {
				GameObject stem = this.getOrCreateStem(go, i);
				if (!stem)
					continue;
				Vector3 pos = Vector3.zero;
				float lastTilt = 0;
				int steps = UnityEngine.Random.Range(11, grown ? 15 : 18);
				for (int i0 = 0; i0 <= steps; i0++) {
					GameObject child = this.getOrCreateSubplant("99cdec62-302b-4999-ba49-f50c73575a4d", grown, stem, i0, pos, lastTilt, out float tilt);
					lastTilt = tilt;
					pos += child.transform.up.normalized * 0.25F;
					this.prepareSubplant(child);
				}
				float ang = (i*360F/arms)+UnityEngine.Random.Range(-15F, 15F);
				float rad = Mathf.Deg2Rad*(-ang+90);
				stem.transform.localPosition = new Vector3(Mathf.Cos(rad) * 0.4F, 0, Mathf.Sin(rad) * 0.4F);
				stem.transform.localRotation = Quaternion.Euler(0, ang, 0);
			}
			go.layer = LayerID.Useable;
			foreach (Collider c in go.GetComponentsInChildren<Collider>(true)) {
				c.isTrigger = true;
				c.gameObject.layer = LayerID.Useable;
			}
		}

		private void prepareObject(GameObject go) {
			go.removeComponent<Pickupable>();
			go.removeComponent<PlantBehaviour>();
			go.removeComponent<DamageOnPickup>();
			go.removeComponent<Plantable>();
			go.removeComponent<FMOD_StudioEventEmitter>();
			LiveMixin lv = go.GetComponent<LiveMixin>();
			lv.data.maxHealth = 90;
			lv.health = lv.maxHealth;
		}

		private GameObject getOrCreateStem(GameObject go, int i) {
			string nm = STEM_NAME+i;
			GameObject child = go.getChildObject(nm);
			if (!child) {
				child = new GameObject(nm);
				child.transform.parent = go.transform;
				child.transform.localPosition = Vector3.zero;
				child.transform.localScale = Vector3.one;
				child.layer = LayerID.Useable;
			}
			return child;
		}

		private GameObject getOrCreateSubplant(string pfb, bool grown, GameObject go, int i0, Vector3 pos, float lastTilt, out float tilt) {
			string nm = CHILD_NAME+i0;
			GameObject child = go.getChildObject(nm);
			if (!child) {
				child = ObjectUtil.createWorldObject(pfb);
				this.prepareObject(child);
				child.name = nm;
				child.transform.parent = go.transform;
				child.transform.localPosition = pos;
				float maxTilt = grown ? 20F : 30F;
				if (pos.y < 0.5F)
					maxTilt = Mathf.Min(maxTilt, pos.y * 60);
				if (grown) {
					if (lastTilt >= 30)
						maxTilt = -5;
				}
				else {
					if (lastTilt > 60)
						maxTilt = Mathf.Max(0, 90 - lastTilt);
				}
				tilt = (i0 == 0 ? UnityEngine.Random.Range(5F, 20F) : UnityEngine.Random.Range(-20F, maxTilt)) + lastTilt;
				child.transform.localEulerAngles = new Vector3(tilt, UnityEngine.Random.Range(-10F, 10F), 0);
				//SNUtil.writeToChat(i0+":"+tilt);
				child.transform.localScale = Vector3.one;
				child.layer = LayerID.Useable;
				foreach (Renderer r in child.GetComponentsInChildren<Renderer>())
					r.transform.localRotation = Quaternion.Euler(-90, 0, 0);
			}
			else {
				tilt = lastTilt;
			}
			return child;
		}

		private void prepareSubplant(GameObject child) {
			child.removeComponent<LargeWorldEntity>();
			child.removeComponent<TechTag>();
			child.removeComponent<PrefabIdentifier>();

			foreach (Renderer r in child.GetComponentsInChildren<Renderer>(true)) {
				r.materials[0].SetColor("_GlowColor", Color.white);
				RenderUtil.makeTransparent(r);
				RenderUtil.setEmissivity(r, 1);
				RenderUtil.setGlossiness(r, 4, 0, 0.6F);
				RenderUtil.swapToModdedTextures(r, this);
			}
		}

		public override float getScaleInGrowbed(bool indoors) {
			return indoors ? 0.25F : 0.5F;
		}

		public override bool isResource() {
			return false;
		}

		protected override bool isExploitable() {
			return false;
		}

		public override Plantable.PlantSize getSize() {
			return Plantable.PlantSize.Large;
		}

		public override bool canGrowAboveWater() {
			return true;
		}

		public override bool canGrowUnderWater() {
			return true;
		}

	}

	class MushroomStackTag : MonoBehaviour {

		private readonly List<PlantSegment> segments = new List<PlantSegment>();
		private GrownPlant grown;
		private float creationTime = 999999;
		private float lastContinuityCheckTime = -1;

		class PlantSegment {

			internal readonly Renderer renderer;
			internal readonly LiveMixin live;
			internal readonly GameObject obj;
			internal readonly int index;

			internal PlantSegment(Renderer r) {
				renderer = r;
				live = r.gameObject.FindAncestor<LiveMixin>();
				obj = r.transform.parent.gameObject;
				string n = obj.name;
				index = !string.IsNullOrEmpty(n) && n.Contains("leaf_aux") ? int.Parse(n.Substring(n.Length - 1)) : -1;
			}

		}

		void Start() {
			EcoceanMod.mushroomStack.prepareGameObject(gameObject, null);
			creationTime = DayNightCycle.main.timePassedAsFloat;
			grown = gameObject.GetComponent<GrownPlant>();
			if (grown) {
				gameObject.SetActive(true);
			}
			else {
				gameObject.transform.localScale = new Vector3(1, 1, 1);
				gameObject.transform.rotation = Quaternion.identity;
			}
		}

		void Update() {
			bool isNew = DayNightCycle.main.timePassedAsFloat-creationTime <= 0.1F;
			if (segments.Count == 0 || isNew) {
				segments.Clear();
				foreach (Renderer r in gameObject.GetComponentsInChildren<Renderer>()) {
					segments.Add(new PlantSegment(r));
					if (transform.position.y < 0) {
						Planter p = gameObject.FindAncestor<Planter>();
						if (!(p && p.environment == Planter.PlantEnvironment.Air))
							RenderUtil.swapTextures(EcoceanMod.modDLL, r, "Textures/Plants/MushroomStackPink");
					}
				}
			}
			float time = DayNightCycle.main.timePassedAsFloat;
			bool kill = false;
			if (time - lastContinuityCheckTime >= 1) {
				lastContinuityCheckTime = time;
				foreach (PlantSegment s in segments) {
					if (!s.renderer) {
						kill = true;
						continue;
					}
					//float f = (float)Math.Abs(2*VentKelp.noiseField.getValue(r.gameObject.transform.position+Vector3.up*DayNightCycle.main.timePassedAsFloat*7.5F))-0.75F;
					if (!s.live || s.live.health <= 0)
						kill = true;
				}
			}
			foreach (PlantSegment s in segments) {
				if (s.renderer)
					RenderUtil.setEmissivity(s.renderer, 1 + (0.4F * Mathf.Sin((s.renderer.gameObject.GetInstanceID() * -11.7851F) + (time * 0.733F))));
			}
			if (kill && !isNew) {/*
				Planter p = gameObject.GetComponentInParent<Planter>();
				GrownPlant g = gameObject.GetComponentInParent<GrownPlant>();
				if (p && g) {
					p.RemoveItem(p.GetSlotID(g.seed));
					p.storageContainer.container.DestroyItem(SeaToSeaMod.kelp.seed.TechType);
					//p.RemoveItem(g.seed);
				}
				gameObject.destroy();*/
				gameObject.GetComponentInParent<LiveMixin>().TakeDamage(99999F);
				SNUtil.log("Killing incomplete/killed mushroom stack @ " + transform.position);
			}
		}

	}
}
