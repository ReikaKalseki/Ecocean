using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;

using ReikaKalseki.DIAlterra;

using SMLHelper.V2.Assets;
using SMLHelper.V2.Handlers;
using SMLHelper.V2.Utility;

using UnityEngine;
using UnityEngine.UI;

namespace ReikaKalseki.Ecocean {

	public class FoodEffectSystem {

		public static readonly FoodEffectSystem instance = new FoodEffectSystem();

		private readonly MesmerVisual mesmerVisual;

		private readonly Dictionary<TechType, FoodEffect> data = new Dictionary<TechType, FoodEffect>();

		internal Action<float> moraleCallback = null;

		public event Action<Survival, InfectedMixin, float> onEatenInfectedEvent;

		private FoodEffectSystem() {
			mesmerVisual = new MesmerVisual();
			ScreenFXManager.instance.addOverride(mesmerVisual);
		}

		internal XMLLocale.LocaleEntry getLocaleEntry() {
			return EcoceanMod.locale.getEntry("FoodEffects");
		}

		public string getLocaleEntry(string key) {
			return this.getLocaleEntry().getField<string>(key);
		}

		internal void register() {
			XMLLocale.LocaleEntry e = this.getLocaleEntry();
			this.addEffect(TechType.KooshChunk, (s, go) => shiftMorale(-10), e.getField<string>("koosh"));
			this.addEffect(TechType.BulboTreePiece, (s, go) => shiftMorale(-5), e.getField<string>("bulbo"));
			this.addEffect(TechType.OrangeMushroomSpore, (s, go) => shiftMorale(-20), e.getField<string>("jaffa"));
			this.addEffect(TechType.SpottedLeavesPlantSeed, (s, go) => shiftMorale(5), e.getField<string>("dockleaf"));
			this.addEffect(TechType.PinkFlowerSeed, (s, go) => Player.main.liveMixin.TakeDamage(UnityEngine.Random.Range(15, 20), Player.main.transform.position, DamageType.Puncture, go), e.getField<string>("koosh"));

			this.addDamageOverTimeEffect(TechType.AcidMushroom, 40, 15, DamageType.Acid, e.getField<string>("acidburn"));
			this.addDamageOverTimeEffect(TechType.WhiteMushroom, 250, 10, DamageType.Acid, e.getField<string>("acidburn"));

			this.addVisualDistortionEffect(TechType.JellyPlant, 3, 15);

			this.addVomitingEffect(TechType.RedRollPlantSeed, 30, 25, 3, 5F, 10);
			this.addVomitingEffect(TechType.RedGreenTentacleSeed, 35, 40, 4, 5F, 10);
			this.addVomitingEffect(TechType.EyesPlantSeed, 25, 50, 5, 5F, 10);
			this.addVomitingEffect(TechType.SnakeMushroomSpore, 60, 60, 8, 4F, 20);
			this.addVomitingEffect(TechType.RedConePlantSeed, 20, 25, 5, 4F, 10);
			this.addVomitingEffect(TechType.PurpleFanSeed, 50, 75, 10, 4F, 10);
			this.addVomitingEffect(TechType.PurpleStalkSeed, 80, 80, 10, 2F, 8);

			this.addPoisonEffect(TechType.SnakeMushroomSpore, 40, 20);
			this.addPoisonEffect(TechType.RedGreenTentacleSeed, 30, 10);
			this.addPoisonEffect(TechType.RedRollPlantSeed, 40, 15);
			this.addPoisonEffect(TechType.PurpleStalkSeed, 50, 15);

			this.addEffect(TechType.CreepvineSeedCluster, (s, go) => PlayerMovementSpeedModifier.add(0.4F, 30), e.getField<string>("slow"));
			this.addEffect(EcoceanMod.glowOil.TechType, (s, go) => PlayerMovementSpeedModifier.add(0.33F, 60), e.getField<string>("slow"));

			this.addVomitingEffect(EcoceanMod.lavaShroom.seed.TechType, 60, 60, 8, 4F, 20);
			this.addPoisonEffect(EcoceanMod.lavaShroom.seed.TechType, 50, 30);

			this.addPoisonEffect(EcoceanMod.pinkBulbStack.seed.TechType, 25, 10);

			this.addPoisonEffect(EcoceanMod.planktonItem.TechType, 20, 10);
			this.addVisualDistortionEffect(EcoceanMod.planktonItem.TechType, 2, 60);
		}

		private void shiftMorale(float delta) {
			if (moraleCallback != null)
				moraleCallback.Invoke(delta);
		}

		public void ensureEatable(Pickupable pp) {
			TechType tt = pp.GetTechType();
			if (tt != TechType.None && data.ContainsKey(tt)) {
				Eatable ea = pp.GetComponent<Eatable>();
				if (!ea || (Mathf.Approximately(ea.foodValue, 0) && Mathf.Approximately(ea.waterValue, 0))) {
					ea = pp.gameObject.EnsureComponent<Eatable>();
					ea.foodValue = tt == TechType.SpottedLeavesPlantSeed ? 5 : UnityEngine.Random.Range(8, 16);
					ea.waterValue = tt == TechType.SpottedLeavesPlantSeed ? 2 : UnityEngine.Random.Range(5, 11);
					ea.kDecayRate = ObjectUtil.lookupPrefab(TechType.CreepvinePiece).GetComponent<Eatable>().kDecayRate;
					ea.timeDecayStart = DayNightCycle.main.timePassedAsFloat;
					ea.decomposes = true;
					//SNUtil.log("Adding eatability "+ea.foodValue+"/"+ea.waterValue+" to "+pp);
				}
			}
		}

		public void clearNegativeEffects() {
			Player.main.gameObject.removeComponent<DamageOverTime>();
			Player.main.gameObject.removeComponent<VomitingEffect>();
			Player.main.gameObject.removeComponent<VisualDistortionEffect>();
		}

		public void addEffect(TechType tt, Action<Survival, GameObject> act, string tooltip = null) {
			FoodEffect e;
			if (data.ContainsKey(tt)) {
				e = data[tt];
				e.onEaten += act;
			}
			else {
				e = new FoodEffect(tt, act);
				data[tt] = e;
			}
			SNUtil.log("Adding eat effect " + act.Method.Name + " to " + tt + ": " + tooltip);
			if (!string.IsNullOrEmpty(tooltip))
				e.tooltip.Add(tooltip);
		}

		public void addDamageOverTimeEffect(TechType tt, float totalDamage, float duration, DamageType type, string tooltip = null) {
			this.addEffect(tt, (s, go) => {
				DamageOverTime dot = Player.main.gameObject.EnsureComponent<DamageOverTime>();
				dot.doer = Player.main.gameObject;//go;
				dot.damageType = type;
				dot.totalDamage = totalDamage;
				dot.duration = duration;
				dot.ActivateInterval(type == DamageType.Poison ? 2F : 0.125F);
			},
			tooltip);
		}

		public void addPoisonEffect(TechType tt, float totalDamage, float duration) {
			this.addDamageOverTimeEffect(tt, totalDamage, duration, DamageType.Poison, this.getLocaleEntry().getField<string>("poison"));
		}

		public void addVisualDistortionEffect(TechType tt, float intensity, float duration) {
			this.addEffect(tt, (s, go) => {
				VisualDistortionEffect e = Player.main.gameObject.EnsureComponent<VisualDistortionEffect>();
				e.intensity = intensity;
				e.timeRemaining = duration;
			}, this.getLocaleEntry("visual"));
		}

		public void addVomitingEffect(TechType tt, float totalFoodLoss, float totalWaterLoss, int maxEvents, float minDelay, float maxDelay) {
			this.addEffect(tt, (s, go) => {
				PlayerMovementSpeedModifier.add(0.5F, 10F);
				VomitingEffect e = Player.main.gameObject.EnsureComponent<VomitingEffect>();
				e.remainingFood = totalFoodLoss;
				e.remainingWater = totalWaterLoss;
				e.maxEvents = maxEvents;
				e.minDelay = minDelay;
				e.maxDelay = maxDelay;
				e.survivalObject = s;
				instance.shiftMorale(-40);
			}, this.getLocaleEntry("vomit"));
		}

		internal void applyTooltip(System.Text.StringBuilder sb, TechType tt, GameObject go) {
			if (data.ContainsKey(tt))
				data[tt].applyTooltip(sb, go);
		}

		internal void onEaten(Survival s, GameObject go) {
			TechType tt = CraftData.GetTechType(go);
			InfectedMixin mix = go.GetComponent<InfectedMixin>();
			if (mix) {
				float f = Mathf.Clamp01(0.25F*2*mix.GetInfectedAmount());
				TemporaryBreathPrevention.add(f * 60);
				if (onEatenInfectedEvent != null)
					onEatenInfectedEvent.Invoke(s, mix, f);
				if (f > 0 && UnityEngine.Random.Range(0F, 1F) <= f) {
					DamageOverTime dot = Player.main.gameObject.EnsureComponent<DamageOverTime>();
					dot.doer = Player.main.gameObject;//go;
					dot.damageType = DamageType.Poison;
					dot.totalDamage = 30;
					dot.duration = 15;
					dot.ActivateInterval(2F);
				}
			}
			if (data.ContainsKey(tt))
				data[tt].trigger(s, go);
		}

		public class VisualDistortionEffect : MonoBehaviour, CustomSerializedComponent {

			internal Vector4 effectColor = ScreenFXManager.instance.defaultMesmerShaderColors;
			internal float intensity;

			internal Color tintColor = ScreenFXManager.instance.defaultSmokeShaderColors;
			internal float tintIntensity;

			private float useIntensity = 0;

			internal float timeRemaining = 0;

			void Update() {
				if (timeRemaining > 0) {
					timeRemaining -= Time.deltaTime;
					if (intensity > 0) {
						FoodEffectSystem.instance.mesmerVisual.effect = useIntensity * Mathf.Clamp01(timeRemaining);
						FoodEffectSystem.instance.mesmerVisual.color = effectColor;
					}
					if (tintIntensity > 0) {
						FoodEffectSystem.instance.mesmerVisual.tintEffect = useIntensity * tintIntensity * Mathf.Clamp01(timeRemaining);
						FoodEffectSystem.instance.mesmerVisual.tintColor = tintColor;
					}
				}
				else {
					this.destroy(false);
				}
				if (useIntensity < intensity)
					useIntensity = Mathf.Min(useIntensity + (3 * Time.deltaTime), intensity);
			}

			void OnDisable() {
				FoodEffectSystem.instance.mesmerVisual.effect = 0;
			}

			void OnDestroy() {
				this.OnDisable();
			}

			public virtual void saveToXML(XmlElement e) {
				e.addProperty("timer", timeRemaining);
				e.addProperty("intensity", intensity);
			}

			public virtual void readFromXML(XmlElement e) {
				timeRemaining = (float)e.getFloat("timer", 0);
				intensity = (float)e.getFloat("intensity", 0);
			}

		}

		class MesmerVisual : ScreenFXManager.ScreenFXOverride {

			internal float effect = 0;
			internal Vector4 color;

			internal float tintEffect = 0;
			internal Color tintColor;

			internal MesmerVisual() : base(200) {

			}

			public override void onTick() {
				if (effect > 0) {
					ScreenFXManager.instance.registerOverrideThisTick(ScreenFXManager.instance.mesmerShader);
					ScreenFXManager.instance.mesmerShader.mat.SetVector("_ColorStrength", color);
					ScreenFXManager.instance.mesmerShader.amount = effect;
				}
				if (tintEffect > 0) {
					ScreenFXManager.instance.registerOverrideThisTick(ScreenFXManager.instance.smokeShader);
					ScreenFXManager.instance.smokeShader.intensity = tintEffect;
					ScreenFXManager.instance.smokeShader.color = tintColor;
					ScreenFXManager.instance.smokeShader.mat.color = tintColor;
					ScreenFXManager.instance.smokeShader.mat.SetColor("_Color", tintColor);
				}
			}

		}

		class VomitingEffect : MonoBehaviour, CustomSerializedComponent {

			internal float remainingFood;
			internal float remainingWater;
			internal int maxEvents = 1;

			internal float minDelay;
			internal float maxDelay;

			internal Survival survivalObject;

			private float nextVomitTime = -1;
			private int eventCount;

			void Start() {
				nextVomitTime = DayNightCycle.main.timePassedAsFloat + UnityEngine.Random.Range(1F, 4F);
			}

			void Update() {
				float time = DayNightCycle.main.timePassedAsFloat;
				if (time >= nextVomitTime && !Player.main.GetPDA().isInUse) {
					this.doEffect();
					nextVomitTime = time + UnityEngine.Random.Range(minDelay, maxDelay);
				}
				if (eventCount >= maxEvents || (remainingFood <= 0 && remainingWater <= 0))
					this.destroy(false);
			}

			private void doEffect() {
				eventCount++;
				bool all = eventCount >= maxEvents;
				float subFood = remainingFood*(all ? 1 : MathUtil.getRandomPlusMinus(1F/maxEvents, 0.2F));
				float subWater = remainingWater*(all ? 1 : MathUtil.getRandomPlusMinus(1F/maxEvents, 0.2F));
				SNUtil.vomit(survivalObject, subFood, subWater);
				remainingFood -= subFood;
				remainingWater -= subWater;
			}

			public virtual void saveToXML(XmlElement e) {
				e.addProperty("food", remainingFood);
				e.addProperty("water", remainingWater);
				e.addProperty("mindelay", minDelay);
				e.addProperty("maxdelay", maxDelay);
				e.addProperty("events", maxEvents);
			}

			public virtual void readFromXML(XmlElement e) {
				remainingFood = (float)e.getFloat("food", 0);
				remainingWater = (float)e.getFloat("water", 0);
				minDelay = (float)e.getFloat("mindelay", 0);
				maxDelay = (float)e.getFloat("maxdelay", 0);
				maxEvents = e.getInt("intensity", 1);
			}
		}

		class FoodEffect {

			public readonly TechType itemType;

			internal Action<Survival, GameObject> onEaten;
			public readonly List<string> tooltip = new List<string>();

			internal readonly Story.StoryGoal triggeredGoal;

			internal FoodEffect(TechType tt, Action<Survival, GameObject> a) {
				itemType = tt;
				onEaten = a;
				triggeredGoal = new Story.StoryGoal("ExperiencedEatEffect_" + itemType.AsString(), Story.GoalType.Story, 0);
			}

			internal void applyTooltip(System.Text.StringBuilder sb, GameObject go) {
				//SNUtil.writeToChat("Getting tooltip of "+itemType+" ("+Story.StoryGoalManager.main.IsGoalComplete(triggeredGoal.key)+") "+": ["+string.Join(" & ", tooltip)+"]");
				if (tooltip.Count > 0 && Story.StoryGoalManager.main.IsGoalComplete(triggeredGoal.key)) {
					foreach (string s in tooltip)
						TooltipFactory.WriteDescription(sb, s);
				}
			}

			internal void trigger(Survival s, GameObject go) {
				triggeredGoal.Trigger();
				onEaten.Invoke(s, go);
			}

		}

	}

}
