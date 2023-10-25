using System;
using System.IO;

using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

using SMLHelper.V2.Assets;
using SMLHelper.V2.Handlers;
using SMLHelper.V2.Utility;

using ReikaKalseki.DIAlterra;

namespace ReikaKalseki.Ecocean {
	
	public class FoodEffectSystem {
		
		public static readonly FoodEffectSystem instance = new FoodEffectSystem();
		
		private readonly Dictionary<TechType, FoodEffect> data = new Dictionary<TechType, FoodEffect>();
		
		private FoodEffectSystem() {
			
		}
		
		internal void register() {     	
			XMLLocale.LocaleEntry e = EcoceanMod.locale.getEntry("FoodEffects");
			addEffect(TechType.KooshChunk, (s, go) => Player.main.liveMixin.TakeDamage(UnityEngine.Random.Range(5, 10), Player.main.transform.position, DamageType.Puncture, go), e.getField<string>("koosh"));
			addDamageOverTimeEffect(TechType.AcidMushroom, 40, 15, DamageType.Acid, e.getField<string>("acidburn"));
			addDamageOverTimeEffect(TechType.WhiteMushroom, 250, 10, DamageType.Acid, e.getField<string>("acidburn"));
			
			addVisualDistortionEffect(TechType.JellyPlant, 1, 15);
			
			addVomitingEffect(TechType.RedRollPlantSeed, 30, 25, 3, 2.5F, 5);
			addVomitingEffect(TechType.RedGreenTentacleSeed, 35, 40, 4, 2.5F, 5);
			addVomitingEffect(TechType.EyesPlantSeed, 25, 50, 5, 2.5F, 5);
			addVomitingEffect(TechType.SnakeMushroomSpore, 60, 60, 8, 2F, 10);
			addVomitingEffect(TechType.RedConePlantSeed, 20, 25, 5, 2F, 5);
			addVomitingEffect(TechType.PurpleFanSeed, 50, 75, 10, 2F, 5);
			addVomitingEffect(TechType.PurpleStalkSeed, 80, 80, 10, 1F, 4);
			
			addPoisonEffect(TechType.SnakeMushroomSpore, 40, 20);
			addPoisonEffect(TechType.RedGreenTentacleSeed, 30, 10);
			addPoisonEffect(TechType.RedRollPlantSeed, 40, 15);
			addPoisonEffect(TechType.PurpleStalkSeed, 50, 15);
			
			//unimplemented
			//addEffect(TechType.CreepvineSeedCluster, (s, go) => Player.main.gameObject.EnsureComponent<SlowMovement>().time = DayNightCycle.main.timePassedAsFloat, e.getField<string>("slow"));
		
			addVomitingEffect(EcoceanMod.lavaShroom.seed.TechType, 60, 60, 8, 2F, 10);
			addPoisonEffect(EcoceanMod.lavaShroom.seed.TechType, 50, 30);
			
			addPoisonEffect(EcoceanMod.planktonItem.TechType, 20, 10);
			addVisualDistortionEffect(EcoceanMod.planktonItem.TechType, 2, 60);
		}
		
		public void ensureEatable(Pickupable pp) {
			TechType tt = pp.GetTechType();
			SNUtil.log(pp+" > "+tt+" > "+data.ContainsKey(tt));
			if (tt != TechType.None && data.ContainsKey(tt)) {
				Eatable ea = pp.GetComponent<Eatable>();
				if (!ea || (Mathf.Approximately(ea.foodValue, 0) && Mathf.Approximately(ea.waterValue, 0))) {
					ea = pp.gameObject.EnsureComponent<Eatable>();
					ea.foodValue = UnityEngine.Random.Range(8, 16);
					ea.waterValue = UnityEngine.Random.Range(5, 11);
					ea.kDecayRate = ObjectUtil.lookupPrefab(TechType.CreepvinePiece).GetComponent<Eatable>().kDecayRate;
					ea.timeDecayStart = DayNightCycle.main.timePassedAsFloat;
					ea.decomposes = true;
					SNUtil.log("Adding eatability "+ea.foodValue+"/"+ea.waterValue+" to "+pp);
				}
			}
		}
		
		public void clearNegativeEffects() {
			ObjectUtil.removeComponent<DamageOverTime>(Player.main.gameObject);
			ObjectUtil.removeComponent<VomitingEffect>(Player.main.gameObject);
			ObjectUtil.removeComponent<VisualDistortionEffect>(Player.main.gameObject);
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
			SNUtil.log("Adding eat effect "+act.Method.Name+" to "+tt+": "+tooltip);
			if (!string.IsNullOrEmpty(tooltip))
				e.tooltip.Add(tooltip);
		}
		
		public void addDamageOverTimeEffect(TechType tt, float totalDamage, float duration, DamageType type, string tooltip = null) {
			addEffect(tt, (s, go) => {
				DamageOverTime dot = Player.main.gameObject.EnsureComponent<DamageOverTime>();
				dot.doer = go;
				dot.damageType = type;
				dot.totalDamage = totalDamage;
				dot.duration = duration;
				dot.ActivateInterval(0.125F);
			},
			tooltip);
		}
		
		public void addPoisonEffect(TechType tt, float totalDamage, float duration) {
			addDamageOverTimeEffect(tt, totalDamage, duration, DamageType.Poison, EcoceanMod.locale.getEntry("FoodEffects").getField<string>("poison"));
		}
		
		public void addVisualDistortionEffect(TechType tt, float intensity, float duration) {
			addEffect(tt, (s, go) => {
				VisualDistortionEffect e = Player.main.gameObject.EnsureComponent<VisualDistortionEffect>();
				e.intensity = intensity;
				e.timeRemaining = duration;
			}, EcoceanMod.locale.getEntry("FoodEffects").getField<string>("visual"));
		}
		
		public void addVomitingEffect(TechType tt, float totalFoodLoss, float totalWaterLoss, int maxEvents, float minDelay, float maxDelay) {
			addEffect(tt, (s, go) => {
				VomitingEffect e = Player.main.gameObject.EnsureComponent<VomitingEffect>();
				e.remainingFood = totalFoodLoss;
				e.remainingWater = totalWaterLoss;
				e.maxEvents = maxEvents;
				e.minDelay = minDelay;
				e.maxDelay = maxDelay;
				e.survivalObject = s;
			}, EcoceanMod.locale.getEntry("FoodEffects").getField<string>("vomit"));
		}
		
		internal void applyTooltip(System.Text.StringBuilder sb, TechType tt, GameObject go) {
			if (data.ContainsKey(tt))
				data[tt].applyTooltip(sb, go);
		}
		
		internal void onEaten(Survival s, GameObject go) {			
			TechType tt = CraftData.GetTechType(go);
			if (data.ContainsKey(tt))
				data[tt].trigger(s, go);
		}
		
		class SlowMovement : MonoBehaviour {
			
			internal float time;
			
		}
		
		class VisualDistortionEffect : MonoBehaviour {
			
		    private static MesmerizedScreenFXController mesmerController;
		    private static MesmerizedScreenFX mesmerShader;
			
			internal float intensity;			
			internal float timeRemaining = 0;
			
			void Update() {					
				if (!mesmerShader) {
			    	mesmerController = Camera.main.GetComponent<MesmerizedScreenFXController>();
			    	mesmerShader = Camera.main.GetComponent<MesmerizedScreenFX>();
				}
				
				if (timeRemaining > 0 && mesmerShader) {					
					mesmerController.enabled = false;
					mesmerShader.amount = intensity*Mathf.Clamp01(timeRemaining);
					mesmerShader.enabled = true;
					
					timeRemaining -= Time.deltaTime;
				}
				else {
					UnityEngine.Object.Destroy(this);
					if (mesmerController)
						mesmerController.enabled = true;
				}
			}
			
		}
		
		class VomitingEffect : MonoBehaviour {
			
			internal float remainingFood;
			internal float remainingWater;
			internal int maxEvents = 1;
			
			internal float minDelay;
			internal float maxDelay;
			
			internal Survival survivalObject;
			
			private float nextVomitTime = -1;
			private int eventCount;
			
			void Update() {
				float time = DayNightCycle.main.timePassedAsFloat;
				if (time >= nextVomitTime) {
					doEffect();
					nextVomitTime = time+UnityEngine.Random.Range(minDelay, maxDelay);
				}
				if (eventCount >= maxEvents || (remainingFood <= 0 && remainingWater <= 0))
					UnityEngine.Object.Destroy(this);
			}
			
			private void doEffect() {
				eventCount++;
				bool all = eventCount >= maxEvents;
				survivalObject.food = Mathf.Max(1, survivalObject.food-remainingFood*(all ? 1 : MathUtil.getRandomPlusMinus(1F/maxEvents, 0.2F)));
				survivalObject.water = Mathf.Max(1, survivalObject.water-remainingWater*(all ? 1 : MathUtil.getRandomPlusMinus(1F/maxEvents, 0.2F)));
				SoundManager.playSoundAt(SoundManager.buildSound(Player.main.IsUnderwater() ? "event:/player/Puke_underwater" : "event:/player/Puke"), transform.position, false, 12);
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
				triggeredGoal = new Story.StoryGoal("ExperiencedEatEffect_"+itemType.AsString(), Story.GoalType.Story, 0);
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
