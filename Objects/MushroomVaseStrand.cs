using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using ReikaKalseki.DIAlterra;

using SMLHelper.V2.Assets;
using SMLHelper.V2.Handlers;
using SMLHelper.V2.Utility;

using UnityEngine;

namespace ReikaKalseki.Ecocean {

	public class MushroomVaseStrand : BasicCustomPlant, CustomHarvestBehavior {

		public static readonly Dictionary<BiomeBase, VaseStrandPlanktonSpawnData> planktonAreas = new Dictionary<BiomeBase, VaseStrandPlanktonSpawnData>();
		public static readonly WeightedRandom<TechType> filterDrops = new WeightedRandom<TechType>();

		public static event Action<MushroomVaseStrandTag, TechType> vaseStrandFilterCollectEvent;

		public MushroomVaseStrand(XMLLocale.LocaleEntry e) : base(e, new FloraPrefabFetch(VanillaFlora.REDWORT), "8bc4f11e-17b9-447e-be0c-2fbe324e64f5", "Tendrils") {
			finalCutBonus = 0;
			collectionMethod = HarvestType.None;
			OnFinishedPatching += () => {
				this.addPDAEntry(e.pda, 2F, e.getField<string>("header"));
				SaveSystem.addSaveHandler(ClassID, new SaveSystem.ComponentFieldSaveHandler<MushroomVaseStrandTag>().addField("resourceGenerationProgress").addField("lastHarvest"));
			};

			PlanktonCloud.forSpawnData(s => addSpawnData(s.biome, s.spawnSuccessRate * 0.05F));
			addSpawnData(VanillaBiomes.DUNES, planktonAreas[VanillaBiomes.CRAG].spawnRate * 1.5F);
			addSpawnData(VanillaBiomes.TREADER, planktonAreas[VanillaBiomes.CRAG].spawnRate * 1.5F);
			addSpawnData(VanillaBiomes.REDGRASS, planktonAreas[VanillaBiomes.SPARSE].spawnRate * 0.5F);
			addSpawnData(VanillaBiomes.MUSHROOM, planktonAreas[VanillaBiomes.SPARSE].spawnRate * 0.5F);
		}

		private static void addSpawnData(BiomeBase bb, float r) {
			planktonAreas[bb] = new VaseStrandPlanktonSpawnData(bb, r);
		}

		internal static float getSpawnRate(BiomeBase biome) {
			return planktonAreas.ContainsKey(biome) ? planktonAreas[biome].spawnRate : 0;
		}

		protected override bool generateSeed() {
			return true;
		}

		public override Vector2int SizeInInventory {
			get { return new Vector2int(1, 1); }
		}

		public override void modifySeed(GameObject go) {

		}

		public override void prepareGameObject(GameObject go, Renderer[] r0) {
			base.prepareGameObject(go, r0);

			Transform mdl = r0[0].transform.parent;
			LiveMixin lv = go.GetComponent<LiveMixin>();
			lv.data.maxHealth = 80; //only applies to farmed
			lv.data.knifeable = true;
			lv.health = lv.maxHealth;

			//SNUtil.log("r0: "+r0.Length+" = "+r0.Select(r => r.name+" @ "+r.gameObject.GetFullHierarchyPath()).toDebugString());

			foreach (Renderer r in r0)
				r.gameObject.destroy(false);

			GameObject pfb = ObjectUtil.lookupPrefab(DecoPlants.MUSHROOM_VASE_STRANDS.prefab);
			Animator a = pfb.GetComponentInChildren<Animator>();
			GameObject rg = UnityEngine.Object.Instantiate(a.gameObject);
			rg.transform.SetParent(mdl);
			rg.transform.localPosition = Vector3.zero;
			rg.transform.localRotation = Quaternion.Euler(90, 0, 0);

			go.EnsureComponent<MushroomVaseStrandTag>();
		}

		public static void setupCollider(GameObject go) {
			CapsuleCollider cc = go.EnsureComponent<CapsuleCollider>();
			cc.radius = 0.15F;
			cc.height = 2.5F;
			cc.center = Vector3.up;
			cc.isTrigger = false;
		}

		public override float getScaleInGrowbed(bool indoors) {
			return 0.33F;
		}

		public override bool isResource() {
			return false;
		}

		public override Plantable.PlantSize getSize() {
			return Plantable.PlantSize.Small;
		}

		public override bool canGrowAboveWater() {
			return false;
		}

		public override bool canGrowUnderWater() {
			return true;
		}

		public bool canBeAutoharvested() {
			return true;
		}

		public GameObject tryHarvest(GameObject go) {
			return go.GetComponent<MushroomVaseStrandTag>().tryHarvest() ? ObjectUtil.createWorldObject(seed.TechType) : null;
		}

		public class MushroomVaseStrandTag : MonoBehaviour, IHandTarget {

			private static readonly string PLANKTON_AOE = "PlanktonAoE";
			//private static readonly string INTERACT_TRIGGER = "InteractBox";
			private static readonly float GROW_TIME = 300; //5 min
			private static readonly float HARVEST_CYCLE_TIME = 180;

			internal GrownPlant grown;
			private CapsuleCollider planktonClearingArea;
			private PlanktonClearingArea planktonClearingMgr;

			private Animator animator;

			private LiveMixin health;

			private CapsuleCollider interactTrigger;

			private float lastSiblingCheck;
			private int siblingCount; //includes self

			internal float resourceGenerationProgress;
			internal float lastHarvest;

			private Renderer[] renderers;

			private bool strandsShowing = true;
			private bool strandsShowingPrev = true;

			void Start() {
				grown = gameObject.GetComponent<GrownPlant>();
				if (grown) {
					gameObject.SetActive(true);
					BoxCollider bc = this.GetComponentInChildren<BoxCollider>();
					bool flag = false;
					if (bc) {
						bc.size = new Vector3(0.25F, 0.25F, 1F);
						bc.isTrigger = false;
						flag = true;
						bc.gameObject.EnsureComponent<MushroomVaseStrandTagInteractRelay>().owner = this;
					}
					else {
						GameObject cdr = gameObject.getChildObject("Capsule");
						if (cdr) {
							CapsuleCollider cc = cdr.GetComponent<CapsuleCollider>();
							if (cc) {
								cc.radius = 0.1F;
								cc.height = 0.5F;
								cc.transform.localPosition = new Vector3(0, 0, -0.3F);
								cc.transform.localEulerAngles = new Vector3(90, 0, 0);
								cc.isTrigger = false;
								flag = true;
							}
						}
					}
					if (!flag)
						SNUtil.log("Failed to initialize " + this + " collider, was missing");
					foreach (Renderer r in this.GetComponentsInChildren<Renderer>()) {
						if (r.gameObject.name.StartsWith("coral_reef_plant_middle_05", StringComparison.InvariantCultureIgnoreCase))
							r.gameObject.destroy(false);
					}
					if (!planktonClearingArea) {
						GameObject go = gameObject.getChildObject(PLANKTON_AOE);
						if (!go) {
							go = new GameObject(PLANKTON_AOE);
							go.transform.SetParent(transform);
							Utils.ZeroTransform(go.transform);
						}
						planktonClearingArea = go.EnsureComponent<CapsuleCollider>();
						planktonClearingArea.height = 7.5F;
						planktonClearingArea.radius = 4.0F;
						planktonClearingArea.isTrigger = true;
						planktonClearingMgr = go.EnsureComponent<PlanktonClearingArea>();
						planktonClearingMgr.clearingRate = 0.4F;
						planktonClearingMgr.transform.localPosition = new Vector3(0, 0, 2);
						planktonClearingMgr.transform.localRotation = Quaternion.Euler(90, 0, 0);
						planktonClearingMgr.onClearTick += (pc, amt) => { resourceGenerationProgress = Mathf.Min(1, resourceGenerationProgress + (amt / HARVEST_CYCLE_TIME * this.getResourceFilterEfficiency())); };
					}

					if (!animator)
						animator = this.GetComponentInChildren<Animator>();

					if (!interactTrigger) {
						GameObject go = gameObject;/*gameObject.getChildObject(INTERACT_TRIGGER);
	    				if (!go) {
	    					go = new GameObject(INTERACT_TRIGGER);
	    					go.transform.SetParent(transform);
	    					Utils.ZeroTransform(go.transform);
	    				}*/
						interactTrigger = go.EnsureComponent<CapsuleCollider>();
						interactTrigger.height = 2.0F;
						interactTrigger.radius = 0.25F;
						interactTrigger.isTrigger = true;
						interactTrigger.direction = 2; //upright
						interactTrigger.center = new Vector3(0, 0, 1.25F);
						//interactTrigger.transform.localPosition = new Vector3(0, 0, 1.5F);
						//interactTrigger.transform.localRotation = Quaternion.Euler(90, 0, 0);
						//go.EnsureComponent<MushroomVaseStrandInteraction>().owner = this;
					}

					renderers = this.GetComponentsInChildren<Renderer>();
					foreach (Renderer r in renderers) {
						if (r.gameObject.name.Contains("_LOD"))
							r.gameObject.SetActive(false);
					}

					health = this.GetComponentInChildren<LiveMixin>();
					health.data.maxHealth = 80; //only applies to farmed
					health.data.knifeable = true;
					health.health = health.maxHealth;
				}
			}

			void Update() {
				if (grown) {
					if (renderers == null || renderers.Length == 0 || !renderers[0]) {
						renderers = this.GetComponentsInChildren<Renderer>();
						foreach (Renderer r in renderers) {
							if (r.gameObject.name.Contains("_LOD"))
								r.gameObject.SetActive(false);
						}
					}
					float time = DayNightCycle.main.timePassedAsFloat;
					if (DIHooks.getWorldAge() > 0.5F && time - lastSiblingCheck >= 2.5F) {
						Planter p = grown.gameObject.FindAncestor<Planter>();
						if (!p) {
							SNUtil.log("Farmed mushroom vase strand without a planter?! " + gameObject.GetFullHierarchyPath());
							gameObject.destroy(false);
							return;
						}
						siblingCount = p.GetComponentsInChildren<MushroomVaseStrandTag>().Length;
						lastSiblingCheck = time;
					}

					animator.transform.localScale = Vector3.one;

					strandsShowing = !this.isHarvested();
					foreach (Renderer r in renderers) {
						if (strandsShowing != strandsShowingPrev) {
							RenderUtil.swapTextures(EcoceanMod.modDLL, r, "Textures/Plants/MushroomVaseStrand_" + (strandsShowing ? "Full" : "Cut"));
							RenderUtil.enableAlpha(r.materials[0]);
						}
						r.materials[0].SetColor("_GlowColor", new Color(1, 1, 1 + (3 * (int)Mathf.Clamp01(resourceGenerationProgress)), 1));
					}
				}

				if (health && health.maxHealth < 80) {
					health.data.maxHealth = 80;
					health.data.knifeable = true;
					health.health = health.maxHealth;
				}

				strandsShowingPrev = strandsShowing;
				if (this.isHarvested())
					resourceGenerationProgress = 0;
			}

			internal float getResourceFilterEfficiency() {
				return (float)MathUtil.linterpolate(siblingCount, 2, 12, 1, 0.033F, true);
			}

			internal bool isHarvested() {
				return lastHarvest >= 0 && DayNightCycle.main.timePassedAsFloat - lastHarvest < GROW_TIME;
			}

			internal float getGrowthProgress() {
				return (DayNightCycle.main.timePassedAsFloat - lastHarvest) / GROW_TIME;
			}

			internal bool tryHarvest() {
				if (this.isHarvested()) {
					if (DayNightCycle.main.timePassedAsFloat - lastHarvest > 0.25F) { //in case double code call, or a misclick
						SNUtil.log("Destroying already-harvested mushroom vase strand, dT=" + (DayNightCycle.main.timePassedAsFloat - lastHarvest));
						this.GetComponent<LiveMixin>().TakeDamage(9999, transform.position); //destroy
					}
					return false;
				}
				this.pickResource();
				for (int i = 0; i < 2; i++) //two since three tendrils and vanilla code already drops one
					InventoryUtil.addItem(EcoceanMod.mushroomVaseStrand.seed.TechType);
				lastHarvest = DayNightCycle.main.timePassedAsFloat;
				return true;
			}

			public void OnHandHover(GUIHand hand) {
				if (!grown)
					return;
				if (resourceGenerationProgress >= 1) {
					HandReticle.main.SetIcon(HandReticle.IconType.Interact, 1f);
					HandReticle.main.SetInteractText("VaseStrandClick");
					HandReticle.main.SetTargetDistance(8);
				}
				else if (!this.isHarvested()) { //tendril grown, collecting resources
					float spawnRate = getSpawnRate(BiomeBase.getBiome(transform.position));
					if (spawnRate > 0) {
						HandReticle.main.SetProgress(Mathf.Min(1, resourceGenerationProgress));
						HandReticle.main.SetIcon(HandReticle.IconType.Progress, 1f);
						HandReticle.main.SetInteractText("VaseStrandCollecting");
					}
					else {
						HandReticle.main.SetIcon(HandReticle.IconType.Info, 1f);
						HandReticle.main.SetInteractText("VaseStrandNoSpawns");
					}
					HandReticle.main.SetTargetDistance(8);
				}
				else { //harvested, plant regrowing tendril
					HandReticle.main.SetProgress(Mathf.Min(1, this.getGrowthProgress()));
					HandReticle.main.SetIcon(HandReticle.IconType.Progress, 1f);
					HandReticle.main.SetInteractText("VaseStrandGrowing");
					HandReticle.main.SetTargetDistance(8);
				}
			}

			public void OnHandClick(GUIHand hand) {
				if (!grown)
					return;
				if (this.isHarvested())
					return;
				this.pickResource();
			}

			private void pickResource() {
				if (resourceGenerationProgress >= 1) {
					TechType tt = MushroomVaseStrand.filterDrops.getRandomEntry();
					InventoryUtil.addItem(tt);
					if (vaseStrandFilterCollectEvent != null) {
						vaseStrandFilterCollectEvent.Invoke(this, tt);
					}
					resourceGenerationProgress -= 1;
				}
			}
		}

		class MushroomVaseStrandTagInteractRelay : MonoBehaviour, IHandTarget {

			internal MushroomVaseStrandTag owner;

			void Start() {
				if (!owner)
					owner = gameObject.FindAncestor<MushroomVaseStrandTag>();
			}

			public void OnHandHover(GUIHand hand) {
				if (owner)
					owner.OnHandHover(hand);
			}

			public void OnHandClick(GUIHand hand) {
				if (owner)
					owner.OnHandClick(hand);
			}

		}

		public class VaseStrandPlanktonSpawnData {

			public readonly BiomeBase biome;
			public readonly float spawnRate;

			public VaseStrandPlanktonSpawnData(BiomeBase bb, float amt) {
				biome = bb;
				spawnRate = amt;
			}

		}

	}
}
