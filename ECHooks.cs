﻿using System;
using System.IO;
using System.Xml;
using System.Reflection;

using System.Collections.Generic;
using System.Linq;
using SMLHelper.V2.Handlers;
using SMLHelper.V2.Assets;
using SMLHelper.V2.Utility;

using UnityEngine;

using ReikaKalseki.DIAlterra;
using ReikaKalseki.Ecocean;

namespace ReikaKalseki.Ecocean {
	
	public static class ECHooks {
	    
	    private static readonly HashSet<string> anchorPods = new HashSet<string>() {
			VanillaFlora.ANCHOR_POD_SMALL1.getPrefabID(),
			VanillaFlora.ANCHOR_POD_SMALL2.getPrefabID(),
			VanillaFlora.ANCHOR_POD_MED1.getPrefabID(),
			VanillaFlora.ANCHOR_POD_MED2.getPrefabID(),
			VanillaFlora.ANCHOR_POD_LARGE.getPrefabID(),
	    };
	    
		private static readonly HashSet<string> bloodVine = new HashSet<string>();
		
		private static bool addingExtraGlowOil = false;
	    
	    static ECHooks() {
	    	DIHooks.onSkyApplierSpawnEvent += onSkyApplierSpawn;
	    	//DIHooks.onDamageEvent += onTakeDamage;
	    	DIHooks.onKnifedEvent += onKnifed;
	    	DIHooks.onItemPickedUpEvent += onPickup;
			
	    	DIHooks.onPlayerTickEvent += tickPlayer;
	    	DIHooks.onSeamothTickEvent += tickSeamoth;
	    	DIHooks.onPrawnTickEvent += tickPrawn;
	    	DIHooks.onCyclopsTickEvent += tickCyclops;
	    	
	    	DIHooks.getTemperatureEvent += getWaterTemperature;
	    	
	    	DIHooks.onSeamothSonarUsedEvent += pingSeamothSonar;
	    	DIHooks.onCyclopsSonarUsedEvent += pingCyclopsSonar;
	    	
	    	bloodVine.AddRange(VanillaFlora.BLOOD_KELP.getPrefabs(true, true));
	    }
	    
	    public static void tickSeamoth(SeaMoth sm) {
	    	if (sm.toggleLights.lightsActive)
	    		GlowOil.handleLightTick(sm.transform);
	    }
	    
	    public static void tickPrawn(Exosuit e) {
	    	if (true) //lights always on
	    		GlowOil.handleLightTick(e.transform);
	    }
	    
	    public static void tickCyclops(SubRoot sub) {
	    	if (sub.subLightsOn) //lights always on
	    		GlowOil.handleLightTick(sub.transform);
	    }
	    
	    public static void tickPlayer(Player ep) {	    	
	    	GlowOil.checkPlayerLightTick(ep);
	    	
	    	float dT = Time.deltaTime;
	    	float f = 0.15F-DayNightCycle.main.GetLightScalar()*0.05F;
	    	if (UnityEngine.Random.Range(0F, 1F) <= f*dT) {
	    		EcoceanMod.plankton.tickSpawner(ep, dT);
	    	}
		}
		
		public static void onTakeDamage(DIHooks.DamageToDeal dmg) {
			//dmg.target.GetComponent<ExplodingAnchorPod>());
		}
		
		public static void onKnifed(GameObject go) {
			ExplodingAnchorPod e = go.FindAncestor<ExplodingAnchorPod>();
			if (e)
				e.explode();
		}
		
		public static void onPickup(Pickupable pp) {
			GlowOilTag g = pp.GetComponent<GlowOilTag>();
			if (g) {
				g.resetGlow();
			}
		}
		
		public static void getWaterTemperature(DIHooks.WaterTemperatureCalculation calc) {
			//SNUtil.writeToChat("EC: Checking water temp @ "+calc.position+" def="+calc.originalValue);
			LavaBomb.iterateLavaBombs(lb => {
				float dist = Vector3.Distance(lb.transform.position, calc.position);
				if (dist <= LavaBomb.HEAT_RADIUS) {
					float f = 1F-(dist/LavaBomb.HEAT_RADIUS);
					//SNUtil.writeToChat("Found lava bomb "+lb.transform.position+" at dist "+dist+" > "+f+" > "+(f*lb.getTemperature()));
					calc.setValue(Mathf.Max(calc.getTemperature(), f*lb.getTemperature()));
				}
			});
		}
	    
	    public static void onSkyApplierSpawn(SkyApplier pk) {
	    	GameObject go = pk.gameObject;
	    	PrefabIdentifier pi = go.GetComponentInParent<PrefabIdentifier>();
	    	if (pi) {
	    		if (anchorPods.Contains(pi.ClassId))
	    			go.EnsureComponent<ExplodingAnchorPod>();
	    		else if (bloodVine.Contains(pi.ClassId))
	    			go.EnsureComponent<PredatoryBloodvine>();
	    	}
	    }
		
		public static void tickObjectInGeyser(Geyser g, Collider c) {
			if (g.erupting) {
				Vehicle v = c.gameObject.FindAncestor<Vehicle>();
				if (v) {
					float f = v is Exosuit ? 1 : 0.2F;
					v.liveMixin.TakeDamage(g.damage*f*Time.deltaTime, c.transform.position, DamageType.Fire, g.gameObject);
					if (v is SeaMoth) {
						v.GetComponent<DIHooks.LavaWarningTriggerDetector>().markLavaDetected();
					}
				}
				SubRoot sub = c.gameObject.FindAncestor<SubRoot>();
				if (sub && sub.isCyclops && sub.thermalReactorUpgrade) {
					float num;
					sub.powerRelay.AddEnergy(5*Time.deltaTime, out num);
				}
			}
		}
		
		public static void honkCyclopsHorn(CyclopsHornButton b) {
			attractToSoundPing(b.gameObject.FindAncestor<SubRoot>(), true);
		}
	    
	    public static void pingSeamothSonar(SeaMoth sm) {
			attractToSoundPing(sm, false);
	    }
	    
		public static void pingCyclopsSonar(SubRoot sm) {
			attractToSoundPing(sm, false);
	    }
		
		private static void attractToSoundPing(MonoBehaviour obj, bool isHorn) {
			if (obj is SubRoot) {
				CyclopsNoiseManager noise = obj.gameObject.GetComponentInChildren<CyclopsNoiseManager>();
				noise.noiseScalar *= isHorn ? 6 : 2;
				noise.Invoke("RecalculateNoiseValues", isHorn ? 15 : 10);
			}
			HashSet<Creature> set = WorldUtil.getObjectsNearWithComponent<Creature>(obj.transform.position, 400);
			foreach (Creature c in set) {
				if (!c.GetComponent<WaterParkCreature>()) {
					float chance = Mathf.Clamp01(1F-Vector3.Distance(c.transform.position, obj.transform.position)/400F);
					if (isHorn) {
						chance *= 2;
						chance = Mathf.Min(chance, 0.05F);
						if (c is Reefback || c is GhostLeviathan || c is GhostLeviatanVoid || c is ReaperLeviathan || c is SeaDragon) {
							chance *= 5;
							chance = Mathf.Min(chance, 0.25F);
						}
						if (c is Reefback && isHorn) {
							chance *= 5;
							chance = Mathf.Min(chance, 0.5F);
						}
					}
					if (UnityEngine.Random.Range(0F, 1F) <= chance)
						attractCreatureToSoundSource(c, obj, isHorn);
				}
			}
		}
		
		private static void attractCreatureToSoundSource(Creature c, MonoBehaviour obj, bool isHorn) {
			AttractToTarget ac = c.gameObject.EnsureComponent<AttractToTarget>();
			ac.fire(obj, isHorn);
			if (c is Reefback && isHorn)
				SoundManager.playSoundAt(c.GetComponent<FMOD_CustomLoopingEmitter>().asset, c.transform.position, false, -1, 1);
		}
		
		class AttractToTarget : MonoBehaviour {
			
			private MonoBehaviour target;
			private bool isHorn;
			
			private Creature owner;
			private SwimBehaviour swimmer;
			private AttackCyclops cyclopsAttacker;
			private MeleeAttack[] attacks;
			private AggressiveWhenSeeTarget[] targeting;
			
			private float lastTick;
			
			private float delete;
			
			internal void fire(MonoBehaviour from, bool horn) {
				target = from;
				isHorn |= horn;
				delete = Mathf.Max(delete, DayNightCycle.main.timePassedAsFloat+20);
			}
			
			void Update() {
				if (!owner)
					owner = GetComponent<Creature>();
				if (!swimmer)
					swimmer = GetComponent<SwimBehaviour>();
				if (!cyclopsAttacker)
					cyclopsAttacker = GetComponent<AttackCyclops>();
				if (attacks == null)
					attacks = GetComponents<MeleeAttack>();
				if (targeting == null)
					targeting = GetComponents<AggressiveWhenSeeTarget>();
				
				float time = DayNightCycle.main.timePassedAsFloat;
				if (time >= delete) {
					UnityEngine.Object.DestroyImmediate(this);
					return;
				}
				
				if (time-lastTick <= 0.5)
					return;
				lastTick = time;
				
				if (owner is Reefback && isHorn) {
					Reefback r = (Reefback)owner;
					swimmer.SwimTo(target.transform.position, r.maxMoveSpeed);
					r.friend = target.gameObject;
					return;
				}
				
				if (target is SubRoot && !(cyclopsAttacker && cyclopsAttacker.isActiveAndEnabled))
					return;
				
				if (Vector3.Distance(transform.position, target.transform.position) >= 40)
					swimmer.SwimTo(target.transform.position, 10);
				
				owner.Aggression.Add(isHorn ? 0.5F : 0.05F);
				cyclopsAttacker.SetCurrentTarget(target.gameObject, false);
		    	foreach (MeleeAttack a in attacks)
		    		a.lastTarget.SetTarget(target.gameObject);
		    	foreach (AggressiveWhenSeeTarget a in targeting)
		    		a.lastTarget.SetTarget(target.gameObject);
			}
			
		}
	}
}
