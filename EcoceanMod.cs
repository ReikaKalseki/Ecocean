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
using ReikaKalseki.AqueousEngineering;
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
    public static WorldCollectedItem treeMushroomSpores;
    
    public static SeamothPlanktonScoop planktonScoop;
    
    public static PiezoCrystal piezo;
    public static SonarFlora sonarFlora;
    
    public static VoidBubble voidBubble;    
    public static VoidTongue tongue;
    
    //public static TreeBud mushTreeResource;
    
    public static MushroomStack mushroomStack;
    public static PinkBulbStack pinkBulbStack;
    public static MushroomVaseStrand mushroomVaseStrand;
    public static PinkLeaves pinkLeaves;
    //public static readonly MushroomTendril[] mushroomTendrils = new MushroomTendril[2];
    
    internal static TechType waterCurrentCommon;
    internal static TechType celeryTree;
		
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
	    planktonItem.Patch();
	    treeMushroomSpores = new WorldCollectedItem(locale.getEntry("treeMushroomSpores"), "1ce074ee-1a58-439b-bb5b-e5e3d9f0886f");
		treeMushroomSpores.sprite = TextureManager.getSprite(EcoceanMod.modDLL, "Textures/Items/TreeMushroomSpores");
		treeMushroomSpores.inventorySize = new Vector2int(2, 1);
	    treeMushroomSpores.Patch();
	    
	    WaterCurrent.register();
	    
	    piezo = new PiezoCrystal(locale.getEntry("piezoCrystal"));
	    piezo.register();
	    
	    sonarFlora = new SonarFlora(locale.getEntry("sonarFlora"));
	    sonarFlora.Patch();
	    
	    mushroomStack = new MushroomStack(locale.getEntry("mushroomStack"));
	    mushroomStack.Patch();
	    
	    pinkLeaves = new PinkLeaves(locale.getEntry("pinkLeaves"));
		pinkLeaves.Patch();
		
	    pinkBulbStack = new PinkBulbStack(locale.getEntry("pinkBulbStack"));
		pinkBulbStack.Patch();
		CraftData.entClassTechTable[DecoPlants.PINK_BULB_STACK.prefab] = pinkBulbStack.TechType;
		
	    mushroomVaseStrand = new MushroomVaseStrand(locale.getEntry("mushroomVaseStrand"));
		mushroomVaseStrand.Patch();
		CraftData.entClassTechTable[DecoPlants.MUSHROOM_VASE_STRANDS.prefab] = mushroomVaseStrand.TechType;
	    
	    XMLLocale.LocaleEntry e;
		/*
		e = locale.getEntry("mushroomTendril");
		mushroomTendrils[0] = new MushroomTendril(e, MushroomTendril.color1);
		mushroomTendrils[0].Patch();
		mushroomTendrils[1] = new MushroomTendril(e, MushroomTendril.color2);
		mushroomTendrils[1].Patch();
		*/
		e = locale.getEntry("celeryTree");
		celeryTree = SNUtil.addTechTypeToVanillaPrefabs(e, DecoPlants.CELERY_TREE.prefab);
		SNUtil.addPDAEntry(celeryTree, e.key, e.name, 10, "Lifeforms/Flora/Land", e.pda, e.getField<string>("header"));
	    
	    voidBubble = new VoidBubble(locale.getEntry("VoidBubble"));
	    voidBubble.register();
	    tongue = new VoidTongue(locale.getEntry("VoidTongue"));
	    tongue.register();
	    
	    //mushTreeResource = new TreeBud(locale.getEntry("TreeBud"));
	    //mushTreeResource.Patch();
	    
	    planktonScoop = new SeamothPlanktonScoop();
	    planktonScoop.register();
		
        glowShroom = new GlowOilMushroom();
		glowShroom.Patch();	
		e = locale.getEntry(glowShroom.ClassID);
		glowShroom.addPDAEntry(e.pda, 15F, e.getField<string>("header"));
		SNUtil.log(" > "+glowShroom);
		GenUtil.registerPrefabWorldgen(glowShroom, EntitySlot.Type.Medium, LargeWorldEntity.CellLevel.Far, BiomeType.Dunes_Grass, 1, 0.25F);		
		
		lavaShroom = new LavaBombMushroom();
		lavaShroom.Patch();	
		e = locale.getEntry(lavaShroom.ClassID);
		lavaShroom.addPDAEntry(e.pda, 20F, e.getField<string>("header"));
		SNUtil.log(" > "+lavaShroom);
		GenUtil.registerPrefabWorldgen(lavaShroom, EntitySlot.Type.Medium, LargeWorldEntity.CellLevel.Far, BiomeType.InactiveLavaZone_Chamber_Floor, 1, 0.08F);		
		GenUtil.registerPrefabWorldgen(lavaShroom, EntitySlot.Type.Medium, LargeWorldEntity.CellLevel.Far, BiomeType.InactiveLavaZone_Chamber_Floor_Far, 1, 0.08F);
		
		LootDistributionHandler.EditLootDistributionData(VanillaResources.DIAMOND.prefab, BiomeType.MushroomForest_GiantTreeInteriorFloor, 3F, 1);
		LootDistributionHandler.EditLootDistributionData(VanillaResources.LITHIUM.prefab, BiomeType.MushroomForest_GiantTreeInteriorFloor, 8F, 1);
		
		//GenUtil.registerSlotWorldgen(mushTreeResource.ClassID, mushTreeResource.PrefabFileName, mushTreeResource.TechType, EntitySlot.Type.Medium, LargeWorldEntity.CellLevel.Near, BiomeType.MushroomForest_GiantTreeInteriorRecess, 1, 3F);
		//GenUtil.registerSlotWorldgen(mushTreeResource.ClassID, mushTreeResource.PrefabFileName, mushTreeResource.TechType, EntitySlot.Type.Medium, LargeWorldEntity.CellLevel.Near, BiomeType.MushroomForest_GiantTreeInteriorSpecial, 1, 5F);
		
		//GenUtil.registerSlotWorldgen(pinkLeaves.ClassID, pinkLeaves.PrefabFileName, pinkLeaves.TechType, EntitySlot.Type.Medium, LargeWorldEntity.CellLevel.Medium, BiomeType.UnderwaterIslands_IslandPlants, 1, 1F);
		//GenUtil.registerSlotWorldgen(pinkLeaves.ClassID, pinkLeaves.PrefabFileName, pinkLeaves.TechType, EntitySlot.Type.Medium, LargeWorldEntity.CellLevel.Medium, BiomeType.CrashZone_TrenchSand, 1, 2F);
		
		BioReactorHandler.Main.SetBioReactorCharge(lavaShroom.seed.TechType, BaseBioReactor.GetCharge(TechType.SnakeMushroomSpore)*3);
		BioReactorHandler.Main.SetBioReactorCharge(glowOil.TechType, BaseBioReactor.GetCharge(TechType.BloodOil)*2);
		BioReactorHandler.Main.SetBioReactorCharge(mushroomStack.seed.TechType, BaseBioReactor.GetCharge(TechType.GarryFish)*0.6F);
		
		GenUtil.registerWorldgen(new PositionedPrefab(VanillaCreatures.REAPER.prefab, reaperlessTripleVent.setY(-200)));
		GenUtil.registerWorldgen(new PositionedPrefab(VanillaCreatures.REAPER.prefab, northDuneBit.setY(-320)));
		
		glowShroom.addNativeBiome(VanillaBiomes.DUNES);
		lavaShroom.addNativeBiome(VanillaBiomes.ILZ);
		
        //ConsoleCommandsHandler.Main.RegisterConsoleCommand<Action<int>>("currentFlowVec", MountainCurrentSystem.instance.registerFlowVector);
        ConsoleCommandsHandler.Main.RegisterConsoleCommand<Action<float>>("attackBase", r => {ECHooks.attractCreaturesToBase(Player.main.currentSub, r, c => c is GhostLeviathan || c is GhostLeviatanVoid || c is ReaperLeviathan || c is SeaDragon || c is Shocker || c is CrabSquid || c is BoneShark);});
                 
       	worldgen.load();
       	
       	FoodEffectSystem.instance.register();
		
		/*
		GenUtil.ContainerPrefab pfb = GenUtil.getOrCreateDatabox(planktonScoop.TechType);
		if (QModManager.API.QModServices.Main.ModPresent("SeaToSea")) {
			GenUtil.registerWorldgen(new PositionedPrefab(pfb.ClassID, new Vector3());
		}*/
		
		glowShroom.addNativeBiome(VanillaBiomes.DUNES);
		lavaShroom.addNativeBiome(VanillaBiomes.ILZ);
		pinkBulbStack.addNativeBiome(VanillaBiomes.GRANDREEF);
		pinkBulbStack.addNativeBiome(VanillaBiomes.REDGRASS, true);
		pinkBulbStack.addNativeBiome(VanillaBiomes.KOOSH);
		mushroomStack.addNativeBiome(VanillaBiomes.MOUNTAINS);
		pinkLeaves.addNativeBiome(VanillaBiomes.CRASH);
		//lavaLily.addNativeBiome(VanillaBiomes.ALZ);
		
		e = locale.getEntry("Mouseovers");
		foreach (KeyValuePair<string, string> kvp in e.getFields()) {
			if (!string.IsNullOrEmpty(kvp.Value))
				CustomLocaleKeyDatabase.registerKey(kvp.Key, kvp.Value);
		}
		
		System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(ECHooks).TypeHandle);
    }
    
    [QModPostPatch]
    public static void PostLoad() {
    	if (InstructionHandlers.getTypeBySimpleName("ReikaKalseki.AqueousEngineering.BaseSonarPinger") != null) { //AE is loaded
    		BaseSonarPinger.onBaseSonarPingedEvent += go => ECHooks.pingSonarFromObject(go.gameObject.FindAncestor<SubRoot>(), 0.67F);
    		
			BaseRoomSpecializationSystem.instance.registerModdedObject(glowOil, 0.2F);
			BaseRoomSpecializationSystem.instance.registerModdedObject(glowShroom, 0.2F);
			BaseRoomSpecializationSystem.instance.registerModdedObject(lavaShroom, 0.4F);
			BaseRoomSpecializationSystem.instance.registerModdedObject(mushroomStack, 0.15F);
			BaseRoomSpecializationSystem.instance.registerModdedObject(pinkBulbStack, -0.05F);
			BaseRoomSpecializationSystem.instance.registerModdedObject(pinkLeaves, 0.5F);
			
			ACUEcosystems.addFood(new ACUEcosystems.PlantFood(glowShroom, 0.25F, BiomeRegions.Other));
			ACUEcosystems.addFood(new ACUEcosystems.PlantFood(lavaShroom, 0.25F, BiomeRegions.LavaZone));
			ACUEcosystems.addFood(new ACUEcosystems.PlantFood(mushroomStack, 0.02F, BiomeRegions.Other));
			ACUEcosystems.addFood(new ACUEcosystems.PlantFood(pinkBulbStack, 0.1F, BiomeRegions.Koosh, BiomeRegions.GrandReef));
			ACUEcosystems.addFood(new ACUEcosystems.PlantFood(pinkLeaves, 0.1F, BiomeRegions.Other));
			
			MushroomVaseStrand.filterDrops.addEntry(TechType.Salt, 50);
			MushroomVaseStrand.filterDrops.addEntry(TechType.Copper, 10);
			MushroomVaseStrand.filterDrops.addEntry(TechType.Gold, 5);
			MushroomVaseStrand.filterDrops.addEntry(TechType.Lead, 8);
			MushroomVaseStrand.filterDrops.addEntry(TechType.Silver, 6);
			MushroomVaseStrand.filterDrops.addEntry(TechType.Quartz, 15);
			MushroomVaseStrand.filterDrops.addEntry(TechType.Lithium, 8);
    	}
    	
    	foreach (BiomeType b in Enum.GetValues(typeof(BiomeType)))
    		LootDistributionHandler.Main.EditLootDistributionData("0e67804e-4a59-449d-929a-cd3fc2bef82c", b, 0, 0);
    }

  }
}
