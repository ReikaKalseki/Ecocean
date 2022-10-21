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
			if (g)
				g.resetGlow();
		}
		
		public static void getWaterTemperature(DIHooks.WaterTemperatureCalculation calc) {
			foreach (LavaBombTag lb in UnityEngine.Object.FindObjectsOfType<LavaBombTag>()) {
				if (lb) {
					float dist = Vector3.Distance(lb.transform.position, calc.position);
					if (dist <= LavaBomb.HEAT_RADIUS) {
						float f = 1F-(dist/LavaBomb.HEAT_RADIUS);
						calc.setValue(Mathf.Max(calc.getTemperature(), f*lb.getTemperature()));
					}
				}
			}
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
	}
}
