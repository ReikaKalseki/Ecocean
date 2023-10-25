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
	    
		private static readonly HashSet<string> bloodVine = new HashSet<string>();
		
		private static float lastPiezoEMPDamage = -1;
		
		private static float lastSonarUsed = -1;
		private static float lastHornUsed = -1;
		
		internal static float nextVoidTongueGrab = -1;
	    
	    static ECHooks() {
	    	DIHooks.onSkyApplierSpawnEvent += onSkyApplierSpawn;
	    	DIHooks.onDamageEvent += onTakeDamage;
	    	DIHooks.onKnifedEvent += onKnifed;
	    	DIHooks.onItemPickedUpEvent += onPickup;
	    	
	    	DIHooks.itemTooltipEvent += FoodEffectSystem.instance.applyTooltip;
	    	DIHooks.onEatEvent += FoodEffectSystem.instance.onEaten;
			
	    	DIHooks.onPlayerTickEvent += tickPlayer;
	    	DIHooks.onPrawnTickEvent += tickPrawn;
	    	DIHooks.onCyclopsTickEvent += tickCyclops;
	    	
	    	DIHooks.onEMPHitEvent += onEMPHit;
	    	DIHooks.onEMPTouchEvent += onEMPTouch;
	    	
	    	
	    	DIHooks.getTemperatureEvent += getWaterTemperature;
	    	
	    	DIHooks.onSeamothSonarUsedEvent += pingSeamothSonar;
	    	DIHooks.onCyclopsSonarUsedEvent += pingCyclopsSonar;
	    	
	    	DIHooks.drillableDrillTickEvent += onDrillableTick;
	    	
	    	bloodVine.AddRange(VanillaFlora.BLOOD_KELP.getPrefabs(true, true));
	    }
	    
		public class ECMoth : MonoBehaviour {
			
			private SeaMoth seamoth;
			
			private bool lightsOn;
			
			public Func<float> getLightIntensity = () => 1;
			
			private readonly LinkedList<float> lightToggles = new LinkedList<float>();
			
			void Update() {
				if (!seamoth)
					seamoth = GetComponent<SeaMoth>();
				float time = DayNightCycle.main.timePassedAsFloat;
				while (lightToggles.First != null && time-lightToggles.First.Value > 3) {
					lightToggles.RemoveFirst();
				}
				if (seamoth.toggleLights.lightsActive != lightsOn) {
					lightToggles.AddLast(time);
					lightsOn = seamoth.toggleLights.lightsActive;
				}
				int flashCount = lightToggles.Count;
				//SNUtil.writeToChat(flashCount+" > "+((flashCount-5)/200F).ToString("0.0000"));
				if (flashCount > 5 && UnityEngine.Random.Range(0F, 1F) < (flashCount-5)/250F*getLightIntensity()/* && seamoth.mainAnimator.GetBool("reaper_attack")*/) {
					GameObject go = WorldUtil.areAnyObjectsNear(transform.position, 60, obj => {
							ReaperLeviathan rl = obj.GetComponent<ReaperLeviathan>();
							return rl && rl.holdingVehicle == seamoth;
					    }
					);
					//SNUtil.writeToChat("Found object "+go);
					if (go) {
						go.GetComponent<ReaperLeviathan>().ReleaseVehicle();
					}
				}
				if (lightsOn) {
		    		GlowOil.handleLightTick(transform);
		    		if (UnityEngine.Random.Range(0F, 1F) <= 0.02F)
		    			attractToLight(seamoth);
				}
			}
	    }
	    
	    public static void tickPrawn(Exosuit e) {
	    	if (true) //lights always on
	    		GlowOil.handleLightTick(e.transform);
	    }
	    
	    public static void tickCyclops(SubRoot sub) {
			if (sub.subLightsOn) {
	    		GlowOil.handleLightTick(sub.transform);
	    		if (UnityEngine.Random.Range(0F, 1F) <= 0.04F)
	    			attractToLight(sub);
			}
	    }
	    
	    public static void tickPlayer(Player ep) {	    
			float time = DayNightCycle.main.timePassedAsFloat;	
	    	GlowOil.checkPlayerLightTick(time, ep);
	    	
	    	float dT = Time.deltaTime;
	    	CompassDistortionSystem.instance.tick(time, dT);
	    	
	    	float f = 0.3F-DayNightCycle.main.GetLightScalar()*0.15F;
	    	if (!ep.IsInBase()) {
			    string biome = ep.GetBiomeString();
			    	//SNUtil.writeToChat("Doing plankton spawn check - "+biome);
			    BiomeSpawnData data = EcoceanMod.plankton.getSpawnData(biome);
			    if (data != null) {
		    		if (UnityEngine.Random.Range(0F, 1F) <= f*dT*data.spawnSuccessRate)
		    			EcoceanMod.plankton.tickSpawner(ep, data, dT);
		    	}
	    	}
		    Vector3 pos = ep.transform.position;
		    if (pos.setY(0).magnitude >= 1700) //more than 1200m from center
		    	EcoceanMod.voidBubble.tickSpawner(ep, time, dT);
		    if (pos.y <= -UnityEngine.Random.Range(1000F, 1200F) && VanillaBiomes.VOID.isInBiome(pos)) {
		    	//if (UnityEngine.Object.FindObjectsOfType<VoidTongueTag>().Length == 0)
		    	//	SNUtil.writeToChat("Check void grab time = "+time.ToString("000.0")+"/"+nextVoidTongueGrab.ToString("000.0"));
		    	if (time >= nextVoidTongueGrab) {
		    		nextVoidTongueGrab = time+10;
		    		GameObject go = ObjectUtil.createWorldObject(EcoceanMod.tongue.ClassID);
		    		ObjectUtil.fullyEnable(go);
		    		float depth = Mathf.Min(pos.y-UnityEngine.Random.Range(400F, 500F)*(ep.currentSub ? 2 : 1));
		    		Vector3 put = MathUtil.getRandomVectorAround(pos, 60).setY(depth);
		    		go.transform.position = put;
		    		//go.transform.position = MathUtil.getRandomVectorAround(pos+Camera.main.transform.forward.normalized*400, 40).setY(-1600);
		    		VoidTongueTag v = go.GetComponent<VoidTongueTag>();
		    		v.enabled = true;
		    		v.startGrab(Mathf.Max(-depth-(ep.currentSub ? 250 : 150), -pos.y+UnityEngine.Random.Range(200F, 400F)*(ep.currentSub ? 0.75F : 1)));
		    	}
		    }
		}
		
		public static void onEMPTouch(EMPBlast e, Collider c) {
	    	Player ep = c.gameObject.FindAncestor<Player>();
	    	if (ep) {
	    		CompassDistortionSystem.instance.onHitByEMP(e, isPiezo(e) ? 10 : 1); //piezo is only the 15s base since electrionicsDisableTime is zero for piezo
	    	}
		}
	    
	    public static void onEMPHit(EMPBlast e, GameObject go) { //might be called many times
			if (isPiezo(e)) {
	    		//SNUtil.writeToChat("Match");
	    		float time = DayNightCycle.main.timePassedAsFloat;
	    		SubRoot sub = go.gameObject.FindAncestor<SubRoot>();
		    	float amt = UnityEngine.Random.Range(1F, 4F);
	    		Vehicle v = go.gameObject.FindAncestor<Vehicle>();
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
		
		private static bool isPiezo(EMPBlast e) {
			return e.gameObject.name.StartsWith("PiezoCrystal_EMPulse", StringComparison.InvariantCultureIgnoreCase);
		}
		
		public static float getLastSonarUse() {
			return lastSonarUsed;
		}
		
		public static float getLastHornUse() {
			return lastHornUsed;
		}
		
		public static void onTakeDamage(DIHooks.DamageToDeal dmg) {
			Player ep = dmg.target.gameObject.FindAncestor<Player>();
			if (ep) {
				float f = 0;
				switch(dmg.type) {
					case DamageType.Normal:
					case DamageType.Puncture:
						f = 1;
						break;
					case DamageType.Collide:
					case DamageType.Drill:
						f = 0.67F;
						break;
					case DamageType.Heat:
					case DamageType.Acid:
					case DamageType.Explosive:
					case DamageType.LaserCutter:
					case DamageType.Fire:
						f = 0.33F;
						break;
				}
				if (f > 0) {
					//dmg.target.GetComponent<ExplodingAnchorPod>());
					//SNUtil.writeToChat("Player '"+ep+"' took damage");
					foreach (Biter b in WorldUtil.getObjectsNearWithComponent<Biter>(ep.transform.position, 60*f)) {
						attractCreatureToTarget(b, ep, false);
						//SNUtil.writeToChat("Attracted biter "+b+" @ "+b.transform.position);
					}
				}
			}
			if (dmg.type == DamageType.Electrical && dmg.dealer && dmg.dealer.GetComponentInParent<ElectricalDefense>()) {
				dmg.setValue(Mathf.Min(EcoceanMod.config.getFloat(ECConfig.ConfigEntries.DEFENSECLAMP), dmg.getAmount()));
	    		//SNUtil.writeToChat(dmg.target+" > "+dmg.getAmount()+" from "+dmg.originalAmount);
			}
			Creature c = dmg.target.gameObject.FindAncestor<Creature>();
			if (c is SeaDragon || c is GhostLeviatanVoid || c is GhostLeviathan || c is ReaperLeviathan || c is Reefback) {
				float f = EcoceanMod.config.getFloat(ECConfig.ConfigEntries.LEVIIMMUNE);
				if (f > 0) {
					dmg.setValue(Mathf.Max(0.001F, dmg.getAmount()*(1-f)));
				}
			}
		}
		
		public static void onKnifed(GameObject go) {
			ExplodingAnchorPod e = go.FindAncestor<ExplodingAnchorPod>();
			if (e) {
				e.explode();
				return;
			}
			VoidBubbleTag vb = go.FindAncestor<VoidBubbleTag>();
			if (vb) {
				vb.Disconnect();
				return;
			}
		}
		
		public static void onPickup(Pickupable pp) {
	    	FoodEffectSystem.instance.ensureEatable(pp);
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
					float f2 = dist/LavaBomb.HEAT_RADIUS;
					float f = 1F-(f2*f2*4);
					//SNUtil.writeToChat("Found lava bomb "+lb.transform.position+" at dist "+dist+" > "+f+" > "+(f*lb.getTemperature()));
					calc.setValue(Mathf.Max(calc.getTemperature(), f*lb.getTemperature()));
				}
			});
		}
	    
	    public static void onSkyApplierSpawn(SkyApplier pk) {
	    	GameObject go = pk.gameObject;
	    	PrefabIdentifier pi = go.FindAncestor<PrefabIdentifier>();
	    	if (pi) {
	    		if (ObjectUtil.isAnchorPod(go) && !isSeaTreaderCave(go))
	    			go.EnsureComponent<ExplodingAnchorPod>();
	    		else if (pi && bloodVine.Contains(pi.ClassId))
	    			go.EnsureComponent<PredatoryBloodvine>();
	    		else if (pi && pi.ClassId == VanillaCreatures.REEFBACK.prefab) {
					go.EnsureComponent<ECReefback>();
	    			go.EnsureComponent<ReefbackJetSuctionManager>();
	    		}
				else if (pi && pi.ClassId == VanillaCreatures.REAPER.prefab)
					go.EnsureComponent<ECReaper>();
				else if (pi && pi.ClassId == VanillaCreatures.SEADRAGON.prefab)
					go.EnsureComponent<ECDragon>();
				else if (pi && VanillaFlora.getFromID(pi.ClassId) == VanillaFlora.CREEPVINE_FERTILE)
					CreepvineCollisionDetector.addCreepvineSeedCollision(go);
				else if (pi && (pi.ClassId == VanillaResources.NICKEL.prefab || pi.ClassId == VanillaResources.LARGE_NICKEL.prefab)) {
					foreach (Renderer r in pi.GetComponentsInChildren<Renderer>()) {
						RenderUtil.swapTextures(EcoceanMod.modDLL, r, "Textures/Nickel");
						RenderUtil.setGlossiness(r, 4, 2, 0.4F);
					}
				}
				else if (pi && pi.ClassId == "1c34945a-656d-4f70-bf86-8bc101a27eee") {
	    			go.EnsureComponent<ECMoth>();
	    		}
	    	}
	    }
		
		private static bool isSeaTreaderCave(GameObject go) { //skip the c2c prop ones
			return Vector3.Distance(go.transform.position, new Vector3(-1264, -281, -728)) <= 30;
		}
		
		public static void onGeyserSpawn(Geyser g) {
			CapsuleCollider cc = g.GetComponent<CapsuleCollider>();
			cc.center += Vector3.down*1.5F;
			cc.height += 3.5F;
		}
		
		public static void tickObjectInGeyser(Geyser g, Collider c) {
			if (g.erupting) {
				//SNUtil.writeToChat(c.gameObject.name);
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
				if (!sub) {
					Rigidbody rb = c.gameObject.FindAncestor<Rigidbody>();
					if (rb && !rb.isKinematic) {	
						float dh = rb.transform.position.y-(g.transform.position.y-1.5F);
						float f = 1F-(dh)/30;
						if (v)
							f *= 0.1F;/*
						Vector3 vec = (rb.transform.position.setY(0)-g.transform.position.setY(0)).normalized;
						float f2 = (dh/50F)+1;
						vec *= f2*f2*f2*f2*0.025F;
						vec.y = 1;*/
						Vector3 vec = Vector3.up;
						vec *= 80*f;
						rb.AddForce(vec, ForceMode.Force);
						if (!v && !rb.gameObject.FindAncestor<Player>()) {
							GeyserDisplacement obj = rb.gameObject.EnsureComponent<GeyserDisplacement>();
							obj.geyser = g;
							obj.destroyIn = DayNightCycle.main.timePassedAsFloat+1.5F;
						}
					}
				}
			}
		}
		
		class GeyserDisplacement : MonoBehaviour {
			
			internal Geyser geyser;
			internal float destroyIn;
			
			private Rigidbody body;
			
			void Update() {
				if (!body)
					body = GetComponent<Rigidbody>();
				if (DayNightCycle.main.timePassedAsFloat >= destroyIn) {
					UnityEngine.Object.DestroyImmediate(this);
					return;
				}
				if (geyser && geyser.erupting) {
					float dh = transform.position.y-geyser.transform.position.y;
					Vector3 vec = (transform.position.setY(0)-geyser.transform.position.setY(0));
					if (vec.sqrMagnitude >= 1600) {
						UnityEngine.Object.DestroyImmediate(this);
					}
					else {
						//float f2 = (dh/40F)+1;
						//vec *= f2*f2*f2*f2*Time.deltaTime;
						vec *= Time.deltaTime;
						body.AddForce(vec.normalized*20, ForceMode.Force);
						Vector3 vel = body.velocity;
						vel.y *= 0.99F;
						body.velocity = vel;
					}
				}
			}
			
		}
		
		public static void honkCyclopsHorn(CyclopsHornButton b) {
			attractToSoundPing(b.gameObject.FindAncestor<SubRoot>(), true, 1);
			lastHornUsed = DayNightCycle.main.timePassedAsFloat;
		}
	    
	    public static void pingSeamothSonar(SeaMoth sm) {
			pingSonarFromObject(sm);
	    }
	    
		public static void pingCyclopsSonar(SubRoot sb) {
			pingSonarFromObject(sb);
	    }
	    
		public static void pingSonarFromObject(MonoBehaviour mb, float strength = 1) {
			attractToSoundPing(mb, false, strength);
			lastSonarUsed = DayNightCycle.main.timePassedAsFloat;
	    }
		
		public static void attractToSoundPing(MonoBehaviour obj, bool isHorn, float strength) {
			if (obj is SubRoot) {
				CyclopsNoiseManager noise = obj.gameObject.GetComponentInChildren<CyclopsNoiseManager>();
				noise.noiseScalar *= isHorn ? 6 : 2;
				noise.Invoke("RecalculateNoiseValues", isHorn ? 15 : 10);
			}
			float range = 400*strength;
			WorldUtil.getGameObjectsNear(obj.transform.position, range, go => tryAttractToSound(go, obj, isHorn, strength, range));
		}
		
		private static void tryAttractToSound(GameObject go, MonoBehaviour obj, bool isHorn, float strength, float range) {
			Creature c = go.GetComponent<Creature>();
			if (c && attractedToSound(c, isHorn) && !c.GetComponent<WaterParkCreature>()) {
				float chance = 0.5F*Mathf.Clamp01(1F-Vector3.Distance(c.transform.position, obj.transform.position)/range);
				if (!Mathf.Approximately(strength, 1))
					chance *= Mathf.Sqrt(strength);
				if (isHorn) {
					chance *= 4;
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
		
		internal static bool attractedToSound(Creature c, bool horn) {
			if (c is GhostLeviathan || c is GhostLeviatanVoid || c is ReaperLeviathan || c is SeaDragon)
				return true;
			if (c is Reefback || c is BoneShark)
				return horn;
			if (c is CrabSnake || c is CrabSquid)
				return !horn;
			return false;
		}
		
		internal static bool attractedToLight(Creature c, MonoBehaviour obj) {
			if (c is SeaDragon)
				return true;
			if (c is BoneShark)
				return !(obj is SubRoot);
			return false;
		}
		
		public static void attractToLight(MonoBehaviour obj) {
			float range = obj is SubRoot ? 150 : 80;
			WorldUtil.getGameObjectsNear(obj.transform.position, range, go => tryAttractToLight(go, obj, range));
		}
		
		private static bool tryAttractToLight(GameObject go, MonoBehaviour obj, float range) {
			Creature c = go.GetComponent<Creature>();
			if (c && attractedToLight(c, obj) && !c.GetComponent<WaterParkCreature>() && (obj is SubRoot || ObjectUtil.isLookingAt(obj.transform, c.transform.position, 45))) {
				float chance = Mathf.Clamp01(1F-Vector3.Distance(c.transform.position, obj.transform.position)/range);
				if (obj is SeaMoth)
					chance *= obj.GetComponent<ECMoth>().getLightIntensity();
				if (UnityEngine.Random.Range(0F, 1F) <= chance) {
					attractCreatureToTarget(c, obj, false);
					return true;
				}
			}
			return false;
		}
		
		internal static void attractCreatureToTarget(Creature c, MonoBehaviour obj, bool isHorn) {
			AttractToTarget ac = c.gameObject.EnsureComponent<AttractToTarget>();
			ac.fire(obj, isHorn);
			if (c is Reefback && isHorn)
				SoundManager.playSoundAt(c.GetComponent<FMOD_CustomLoopingEmitter>().asset, c.transform.position, false, -1, 1);
		}
		
		public static void applyCurrentForce(Rigidbody rb, Vector3 force, ForceMode mode, Current c) {
			WaterCurrentTag wc = c.GetComponent<WaterCurrentTag>();
			float str = wc ? wc.getCurrentStrength(rb.transform.position) : 1;
			if (str > 0)
				rb.AddForce(force*str, mode);
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
				if (cyclopsAttacker)
					cyclopsAttacker.SetCurrentTarget(target.gameObject, false);
				if (targeter)
					targeter.SetTarget(target.gameObject);
				if (owner is CrabSnake) {
					CrabSnake cs = (CrabSnake)owner;
					if (cs.IsInMushroom()) {
						cs.ExitMushroom(target.transform.position);
					}
				}
				//if (leash)
				//	leash.
		    	foreach (MeleeAttack a in attacks)
		    		a.lastTarget.SetTarget(target.gameObject);
		    	foreach (AggressiveWhenSeeTarget a in targeting)
		    		a.lastTarget.SetTarget(target.gameObject);
			}
			
		}
		
		public static void setHUDCompassDirection(uGUI_Compass compass, float dir) { /* 0-1 for 360 */
			compass.direction = (dir+CompassDistortionSystem.instance.getTotalDisplacement(Player.main.transform.position)/360F)%1F;
		}
		
		public static void setCyclopsCompassDirection(Transform obj, Quaternion dir) {
			obj.rotation = dir;
			obj.Rotate(0, CompassDistortionSystem.instance.getTotalDisplacement(obj.position), 0);
		}
		
		public static void onDrillableTick(Drillable d, Vector3 vec, Exosuit s) {
			PiezoCrystalTag pt = d.GetComponent<PiezoCrystalTag>();
			if (pt)
				pt.onDrilled();
		}/*
		
		public static void tickVortexTorpedo(SeamothTorpedoWhirlpool go) {
			
		}*/
	}
}
