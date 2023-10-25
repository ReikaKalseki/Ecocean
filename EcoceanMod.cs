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
    
    public static readonly Config<ECConfig.ConfigEntries> config = new Config<ECConfig.ConfigEntries>(modDLL);
    internal static readonly XMLLocale locale = new XMLLocale(modDLL, "XML/locale.xml");
    
    public static readonly WorldgenDatabase worldgen = new WorldgenDatabase();
    
    public static GlowOilMushroom glowShroom;
    public static GlowOil glowOil;
    internal static GlowOilNatural naturalOil;
    
    public static LavaBombMushroom lavaShroom;
    public static LavaBomb lavaBomb;
    
    public static PlanktonCloud plankton;
    public static PlanktonItem planktonItem;
    
    public static SeamothPlanktonScoop planktonScoop;
    
    public static PiezoCrystal piezo;
    
    public static VoidBubble voidBubble;    
    public static VoidTongue tongue;
    
    public static TreeBud mushTreeResource;
    
    public static MushroomStack mushroomStack;
    
    internal static TechType waterCurrentCommon;
		
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
        
        ModVersionCheck.getFromGitVsInstall("Ecocean", modDLL, "Ecocean").register();
        SNUtil.checkModHash(modDLL);
        
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
	    
	    WaterCurrent.register();
	    
	    piezo = new PiezoCrystal(locale.getEntry("piezoCrystal"));
	    piezo.register();
	    
	    mushroomStack = new MushroomStack(locale.getEntry("mushroomStack"));
	    mushroomStack.Patch();
	    
	    voidBubble = new VoidBubble(locale.getEntry("VoidBubble"));
	    voidBubble.register();
	    tongue = new VoidTongue(locale.getEntry("VoidTongue"));
	    tongue.register();
	    
	    mushTreeResource = new TreeBud(locale.getEntry("TreeBud"));
	    mushTreeResource.Patch();
	    
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
		
		LootDistributionHandler.EditLootDistributionData(VanillaResources.DIAMOND.prefab, BiomeType.MushroomForest_GiantTreeInteriorFloor, 3F, 1);
		LootDistributionHandler.EditLootDistributionData(VanillaResources.LITHIUM.prefab, BiomeType.MushroomForest_GiantTreeInteriorFloor, 8F, 1);
		
		//GenUtil.registerSlotWorldgen(mushTreeResource.ClassID, mushTreeResource.PrefabFileName, mushTreeResource.TechType, EntitySlot.Type.Medium, LargeWorldEntity.CellLevel.Near, BiomeType.MushroomForest_GiantTreeInteriorRecess, 1, 3F);
		//GenUtil.registerSlotWorldgen(mushTreeResource.ClassID, mushTreeResource.PrefabFileName, mushTreeResource.TechType, EntitySlot.Type.Medium, LargeWorldEntity.CellLevel.Near, BiomeType.MushroomForest_GiantTreeInteriorSpecial, 1, 5F);
		
		BioReactorHandler.Main.SetBioReactorCharge(lavaShroom.seed.TechType, BaseBioReactor.GetCharge(TechType.SnakeMushroomSpore)*3);
		BioReactorHandler.Main.SetBioReactorCharge(glowOil.TechType, BaseBioReactor.GetCharge(TechType.BloodOil)*6);
		BioReactorHandler.Main.SetBioReactorCharge(mushroomStack.seed.TechType, BaseBioReactor.GetCharge(TechType.GarryFish)*0.6F);
		
		GenUtil.registerWorldgen(new PositionedPrefab(VanillaCreatures.REAPER.prefab, reaperlessTripleVent.setY(-200)));
		GenUtil.registerWorldgen(new PositionedPrefab(VanillaCreatures.REAPER.prefab, northDuneBit.setY(-320)));
		
		glowShroom.addNativeBiome(VanillaBiomes.DUNES);
		lavaShroom.addNativeBiome(VanillaBiomes.ILZ);
		
        ConsoleCommandsHandler.Main.RegisterConsoleCommand<Action<int>>("currentFlowVec", MountainCurrentSystem.instance.registerFlowVector);
                 
       	worldgen.load();
       	
       	FoodEffectSystem.instance.register();
		
		/*
		GenUtil.ContainerPrefab pfb = GenUtil.getOrCreateDatabox(planktonScoop.TechType);
		if (QModManager.API.QModServices.Main.ModPresent("SeaToSea")) {
			GenUtil.registerWorldgen(new PositionedPrefab(pfb.ClassID, new Vector3());
		}*/
		
		System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(ECHooks).TypeHandle);
    }
    
    [QModPostPatch]
    public static void PostLoad() {
    	if (InstructionHandlers.getTypeBySimpleName("ReikaKalseki.AqueousEngineering.BaseSonarPinger") != null)
    		ReikaKalseki.AqueousEngineering.BaseSonarPinger.onBaseSonarPingedEvent += go => ECHooks.pingSonarFromObject(go.gameObject.GetComponentInChildren<CustomMachineLogic>(), 0.67F);
    	
    	foreach (BiomeType b in Enum.GetValues(typeof(BiomeType)))
    		LootDistributionHandler.Main.EditLootDistributionData("0e67804e-4a59-449d-929a-cd3fc2bef82c", b, 0, 0);
    }

  }
}
