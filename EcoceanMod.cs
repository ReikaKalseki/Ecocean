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
    internal static readonly XMLLocale itemLocale = new XMLLocale("XML/items.xml");
    
    internal static GlowOilMushroom glowShroom;
    internal static GlowOil glowOil;

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
	    
	    glowOil = new GlowOil(itemLocale.getEntry("GlowOil"));
	    glowOil.register();
		
        glowShroom = new GlowOilMushroom();
		glowShroom.Patch();	
		XMLLocale.LocaleEntry e = itemLocale.getEntry(glowShroom.ClassID);
		glowShroom.addPDAEntry(e.pda, 15F, e.getField<string>("header"));
		SNUtil.log(" > "+glowShroom);
		GenUtil.registerSlotWorldgen(glowShroom.ClassID, glowShroom.PrefabFileName, glowShroom.TechType, EntitySlot.Type.Medium, LargeWorldEntity.CellLevel.Far, BiomeType.Dunes_Grass, 1, 0.3F);		
		BioReactorHandler.Main.SetBioReactorCharge(glowShroom.seed.TechType, BaseBioReactor.GetCharge(TechType.SnakeMushroomSpore)*3);
		
		System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(ECHooks).TypeHandle);
    }

  }
}
