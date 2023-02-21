using System;
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
		private static float lastPiezoEMPDamage = -1;
	    
	    static ECHooks() {
	    	DIHooks.onSkyApplierSpawnEvent += onSkyApplierSpawn;
	    	DIHooks.onDamageEvent += onTakeDamage;
	    	DIHooks.onKnifedEvent += onKnifed;
	    	DIHooks.onItemPickedUpEvent += onPickup;
			
	    	DIHooks.onPlayerTickEvent += tickPlayer;
	    	DIHooks.onSeamothTickEvent += tickSeamoth;
	    	DIHooks.onPrawnTickEvent += tickPrawn;
	    	DIHooks.onCyclopsTickEvent += tickCyclops;
	    	
	    	DIHooks.onEMPHitEvent += onEMPHit;
	    	
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
	    	float f = 0.3F-DayNightCycle.main.GetLightScalar()*0.15F;
		    string biome = ep.GetBiomeString();
		    	//SNUtil.writeToChat("Doing plankton spawn check - "+biome);
		    BiomeSpawnData data = EcoceanMod.plankton.getSpawnData(biome);
		    if (data != null) {
	    		if (UnityEngine.Random.Range(0F, 1F) <= f*dT*data.spawnSuccessRate)
	    			EcoceanMod.plankton.tickSpawner(ep, data, dT);
	    	}
		}
	    
	    public static void onEMPHit(EMPBlast e, GameObject go) { //might be called many times
	    	if (e.gameObject.name.StartsWith("PiezoCrystal_EMPulse", StringComparison.InvariantCultureIgnoreCase)) {
	    		//SNUtil.writeToChat("Match");
	    		float time = DayNightCycle.main.timePassedAsFloat;
	    		Vehicle v = go.gameObject.FindAncestor<Vehicle>();
	    		SubRoot sub = go.gameObject.FindAncestor<SubRoot>();
		    	float amt = UnityEngine.Random.Range(1F, 4F);
		    	if (v) {
		    		if (time >= lastPiezoEMPDamage+1F) {
		    			go.GetComponent<LiveMixin>().TakeDamage(UnityEngine.Random.Range(10F, 20F), v.transform.position, DamageType.Electrical, e.gameObject);
		    			lastPiezoEMPDamage = time;
		    		}
		    		v.ConsumeEnergy(amt*3); //must be first as will no-op if electronics is disabled
		    		if (amt > 3)
		    			v.energyInterface.DisableElectronicsForTime(amt-3);
		    		if (v is SeaMoth)
		    			ObjectUtil.createSeamothSparkSphere((SeaMoth)v);
		    	}
	    		else if (sub && sub.isCyclops) {
		    		float trash;
		    		sub.powerRelay.ConsumeEnergy(amt*6, out trash);
		    		if (amt > 2)
		    			sub.powerRelay.DisableElectronicsForTime((amt-2)*3);
	    		}
	    	}
	    }
		
		public static void onTakeDamage(DIHooks.DamageToDeal dmg) {
			//dmg.target.GetComponent<ExplodingAnchorPod>());
			Player ep = dmg.target.gameObject.FindAncestor<Player>();
			//SNUtil.writeToChat("Player '"+ep+"' took damage");
			if (ep) {
				foreach (Biter b in WorldUtil.getObjectsNearWithComponent<Biter>(ep.transform.position, 60)) {
					attractCreatureToTarget(b, ep, false);
					//SNUtil.writeToChat("Attracted biter "+b+" @ "+b.transform.position);
				}
			}
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
	    		else if (pi.classId == "8d3d3c8b-9290-444a-9fea-8e5493ecd6fe") //reefback
	    			go.EnsureComponent<ReefbackJetSuctionManager>();
	    	}
	    }
		
		public static void tickObjectInGeyser(Geyser g, Collider c) {
			if (g.erupting) {
				Vehicle v = c.gameObject.FindAncestor<Vehicle>();
				if (v) {
					if (v is SeaMoth) { //will set temp and do a ton of damage
						v.GetComponentInChildren<DIHooks.LavaWarningTriggerDetector>().markGeyserDetected();
					}
					else if (v is Exosuit) {
						v.liveMixin.TakeDamage(g.damage*0.04F*Time.deltaTime, c.transform.position, DamageType.Fire, g.gameObject);
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
						attractCreatureToTarget(c, obj, isHorn);
				}
			}
		}
		
		internal static void attractCreatureToTarget(Creature c, MonoBehaviour obj, bool isHorn) {
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
			private StayAtLeashPosition leash;
			private AttackCyclops cyclopsAttacker;
			private LastTarget targeter;
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
				if (!leash)
					leash = GetComponent<StayAtLeashPosition>();
				if (!cyclopsAttacker)
					cyclopsAttacker = GetComponent<AttackCyclops>();
				if (!targeter)
					targeter = GetComponent<LastTarget>();
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
				if (targeter)
					targeter.SetTarget(target.gameObject);
				//if (leash)
				//	leash.
		    	foreach (MeleeAttack a in attacks)
		    		a.lastTarget.SetTarget(target.gameObject);
		    	foreach (AggressiveWhenSeeTarget a in targeting)
		    		a.lastTarget.SetTarget(target.gameObject);
			}
			
		}
	}
}
