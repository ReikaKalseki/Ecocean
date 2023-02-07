using UnityEngine;  //Needed for most Unity Enginer manipulations: Vectors, GameObjects, Audio, etc.
using System.IO;    //For data read/write methods
using System;    //For data read/write methods
using System.Collections.Generic;   //Working with Lists and Collections
using System.Reflection;
using System.Linq;   //More advanced manipulation of lists/collections
using HarmonyLib;
using QModManager.API.ModLoading;
using ReikaKalseki.DIAlterra;
using ReikaKalseki.Ecocean;
using SMLHelper.V2.Handlers;
using SMLHelper.V2.Utility;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Assets;

namespace ReikaKalseki.Ecocean
{
  [QModCore]
  public static class EcoceanMod {
  	
    public const string MOD_KEY = "ReikaKalseki.Ecocean";
    
    //public static readonly ModLogger logger = new ModLogger();
	public static readonly Assembly modDLL = Assembly.GetExecutingAssembly();
    
    public static readonly Config<ECConfig.ConfigEntries> config = new Config<ECConfig.ConfigEntries>();
    internal static readonly XMLLocale locale = new XMLLocale("XML/locale.xml");
    
    internal static GlowOilMushroom glowShroom;
    internal static GlowOil glowOil;
    internal static GlowOilNatural naturalOil;
    
    internal static LavaBombMushroom lavaShroom;
    internal static LavaBomb lavaBomb;
    
    internal static PlanktonCloud plankton;
    internal static PlanktonItem planktonItem;
    
    internal static SeamothPlanktonScoop planktonScoop;
		
	internal static readonly Vector3 reaperlessTripleVent = new Vector3(-1150, -240, -258);
	internal static readonly Vector3 northDuneBit = new Vector3(-1151, -340, 1444);

    [QModPatch]
    public static void Load() {
        config.load();
        
        Harmony harmony = new Harmony(MOD_KEY);
        Harmony.DEBUG = true;
        FileLog.logPath = Path.Combine(Path.GetDirectoryName(modDLL.Location), "harmony-log.txt");
        FileLog.Log("Ran mod register, started harmony (harmony log)");
        SNUtil.log("Ran mod register, started harmony");
        try {
        	harmony.PatchAll(modDLL);
        }
        catch (Exception ex) {
			FileLog.Log("Caught exception when running patcher!");
			FileLog.Log(ex.Message);
			FileLog.Log(ex.StackTrace);
			FileLog.Log(ex.ToString());
        }
        
        locale.load();
	    
	    glowOil = new GlowOil(locale.getEntry("GlowOil"));
	    glowOil.register();
	    naturalOil = new GlowOilNatural();
	    naturalOil.register();
	    
	    lavaBomb = new LavaBomb(locale.getEntry("LavaBomb"));
	    lavaBomb.Patch();
	    
	    plankton = new PlanktonCloud(locale.getEntry("plankton"));
	    plankton.register();
	    planktonItem = new PlanktonItem(locale.getEntry("planktonItem"));
	    planktonItem.register();
	    
	    planktonScoop = new SeamothPlanktonScoop();
	    planktonScoop.register();
		
        glowShroom = new GlowOilMushroom();
		glowShroom.Patch();	
		XMLLocale.LocaleEntry e = locale.getEntry(glowShroom.ClassID);
		glowShroom.addPDAEntry(e.pda, 15F, e.getField<string>("header"));
		SNUtil.log(" > "+glowShroom);
		GenUtil.registerSlotWorldgen(glowShroom.ClassID, glowShroom.PrefabFileName, glowShroom.TechType, EntitySlot.Type.Medium, LargeWorldEntity.CellLevel.Far, BiomeType.Dunes_Grass, 1, 0.25F);		
		
		lavaShroom = new LavaBombMushroom();
		lavaShroom.Patch();	
		e = locale.getEntry(lavaShroom.ClassID);
		lavaShroom.addPDAEntry(e.pda, 20F, e.getField<string>("header"));
		SNUtil.log(" > "+lavaShroom);
		GenUtil.registerSlotWorldgen(lavaShroom.ClassID, lavaShroom.PrefabFileName, lavaShroom.TechType, EntitySlot.Type.Medium, LargeWorldEntity.CellLevel.Far, BiomeType.InactiveLavaZone_Chamber_Floor, 1, 0.08F);		
		GenUtil.registerSlotWorldgen(lavaShroom.ClassID, lavaShroom.PrefabFileName, lavaShroom.TechType, EntitySlot.Type.Medium, LargeWorldEntity.CellLevel.Far, BiomeType.InactiveLavaZone_Chamber_Floor_Far, 1, 0.08F);
		
		BioReactorHandler.Main.SetBioReactorCharge(glowShroom.seed.TechType, BaseBioReactor.GetCharge(TechType.SnakeMushroomSpore)*3);
		BioReactorHandler.Main.SetBioReactorCharge(glowOil.TechType, BaseBioReactor.GetCharge(TechType.BloodOil)*6);
		
		GenUtil.registerWorldgen(new PositionedPrefab(VanillaCreatures.REAPER.prefab, reaperlessTripleVent.setY(-200)));
		GenUtil.registerWorldgen(new PositionedPrefab(VanillaCreatures.REAPER.prefab, northDuneBit.setY(-320)));
		
		/*
		GenUtil.ContainerPrefab pfb = GenUtil.getOrCreateDatabox(planktonScoop.TechType);
		if (QModManager.API.QModServices.Main.ModPresent("SeaToSea")) {
			GenUtil.registerWorldgen(new PositionedPrefab(pfb.ClassID, new Vector3());
		}*/
		
		System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(ECHooks).TypeHandle);
    }
    
    [QModPostPatch]
    public static void PostLoad() {
        
    }

  }
}
