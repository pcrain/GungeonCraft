namespace CwaffingTheGungy;

/*
    Valid level names:
        tt_castle    // Keep of the Lead Lord
        tt_catacombs
    GameManager.Instance.InjectedLevelName =
*/

public class SansDungeon
{
    public const string INTERNAL_NAME = "cg_sansfloor";
    public const string FLOOR_NAME    = "Odd Corridor";

    public static GameLevelDefinition FloorNameDefinition;
    public static GameObject GameManagerObject;
    public static tk2dSpriteCollectionData goheckyourself;

    public static Hook getOrLoadByName_Hook;
    public static Hook forceCustomMusicHook;

    public static Dungeon GetOrLoadByNameHook(Func<string, Dungeon> orig, string name)
    {
        if (!GameManager.Instance.customFloors.Contains(FloorNameDefinition))
            GameManager.Instance.customFloors.Add(FloorNameDefinition); // fixes a bug where returning to the breach deletes the floor, thanks Bunny!
        Dungeon dungeon = null;
        string dungeonPrefabTemplate = "Base_ResourcefulRat";
        if (name.ToLower() == INTERNAL_NAME)
        {
            try
            {
                dungeon = SansGeon(GetOrLoadByName_Orig(dungeonPrefabTemplate));
            }
            catch (Exception e)
            {
                ETGModConsole.Log($"exception: {e}");
            }
        }
        if (dungeon)
        {
            DebugTime.RecordStartTime();
            DebugTime.Log("AssetBundle.LoadAsset<Dungeon>({0})", new object[] { name });
            return dungeon;
        }
        else
        {
            return orig(name);
        }
    }

    internal static void PlayFloorMusicHook(Action<DungeonFloorMusicController, Dungeon> orig, DungeonFloorMusicController musicController, Dungeon d)
    {
        orig(musicController, d);
        if (d.DungeonFloorName == FLOOR_NAME)
            GameManager.Instance.DungeonMusicController.LoopMusic(musicName: "sans", loopPoint: 48800, rewindAmount: 48800);
    }

    public static void InitCustomDungeon()
    {
        getOrLoadByName_Hook = new Hook(
            typeof(DungeonDatabase).GetMethod("GetOrLoadByName", BindingFlags.Static | BindingFlags.Public),
            typeof(SansDungeon).GetMethod("GetOrLoadByNameHook", BindingFlags.Static | BindingFlags.Public)
        );
        forceCustomMusicHook = new Hook(
            typeof(DungeonFloorMusicController).GetMethod("ResetForNewFloor", BindingFlags.Instance | BindingFlags.Public),
            typeof(SansDungeon).GetMethod("PlayFloorMusicHook", BindingFlags.Static | BindingFlags.NonPublic)
        );

        AssetBundle braveResources = ResourceManager.LoadAssetBundle("brave_resources_001");
        GameManagerObject = braveResources.LoadAsset<GameObject>("_GameManager");

        FloorNameDefinition = new GameLevelDefinition()
        {
            dungeonSceneName = INTERNAL_NAME, //this is the name we will use whenever we want to load our dungeons scene
            dungeonPrefabPath = INTERNAL_NAME, //this is what we will use when we want to acess our dungeon prefab
            priceMultiplier = 1.5f, //multiplies how much things cost in the shop
            secretDoorHealthMultiplier = 1, //multiplies how much health secret room doors have, aka how many shots you will need to expose them
            enemyHealthMultiplier = 2, //multiplies how much health enemies have
            damageCap = 300, // damage cap for regular enemies
            bossDpsCap = 78, // damage cap for bosses
            flowEntries = new List<DungeonFlowLevelEntry>(0),
            predefinedSeeds = new List<int>(0)
        };

        // sets the level definition of the GameLevelDefinition in GameManager.Instance.customFloors if it exists
        foreach (GameLevelDefinition levelDefinition in GameManager.Instance.customFloors)
        {
            if (levelDefinition.dungeonSceneName == INTERNAL_NAME) { FloorNameDefinition = levelDefinition; }
        }

        GameManager.Instance.customFloors.Add(FloorNameDefinition);
        GameManagerObject.GetComponent<GameManager>().customFloors.Add(FloorNameDefinition);
    }

    public static Dungeon SansGeon(Dungeon dungeon)
    {
        Dungeon MinesDungeonPrefab = GetOrLoadByName_Orig("Base_Mines");
        Dungeon CatacombsPrefab = GetOrLoadByName_Orig("Base_Catacombs");
        Dungeon RatDungeonPrefab = GetOrLoadByName_Orig("Base_ResourcefulRat");
        DungeonMaterial FinalScenario_MainMaterial = UnityEngine.Object.Instantiate(RatDungeonPrefab.roomMaterialDefinitions[0]);
        FinalScenario_MainMaterial.supportsPits = true;
        FinalScenario_MainMaterial.doPitAO = false;
        // FinalScenario_MainMaterial.pitsAreOneDeep = true;
        FinalScenario_MainMaterial.useLighting = true;
        // FinalScenario_MainMaterial.supportsLavaOrLavalikeSquares = true;
        FinalScenario_MainMaterial.lightPrefabs.elements[0].rawGameObject = MinesDungeonPrefab.roomMaterialDefinitions[0].lightPrefabs.elements[0].rawGameObject;
        FinalScenario_MainMaterial.roomFloorBorderGrid = MinesDungeonPrefab.roomMaterialDefinitions[0].roomFloorBorderGrid;
        FinalScenario_MainMaterial.pitLayoutGrid = MinesDungeonPrefab.roomMaterialDefinitions[0].pitLayoutGrid;
        FinalScenario_MainMaterial.pitBorderFlatGrid = MinesDungeonPrefab.roomMaterialDefinitions[0].pitBorderFlatGrid;

        DungeonTileStampData m_FloorNameStampData = ScriptableObject.CreateInstance<DungeonTileStampData>();
        m_FloorNameStampData.name = "ENV_FloorName_STAMP_DATA";
        m_FloorNameStampData.tileStampWeight = 0;
        m_FloorNameStampData.spriteStampWeight = 0;
        m_FloorNameStampData.objectStampWeight = 1;
        m_FloorNameStampData.stamps = new TileStampData[0];
        m_FloorNameStampData.spriteStamps = new SpriteStampData[0];
        m_FloorNameStampData.objectStamps = RatDungeonPrefab.stampData.objectStamps;
        m_FloorNameStampData.SymmetricFrameChance = 0.25f;
        m_FloorNameStampData.SymmetricCompleteChance = 0.6f;

        dungeon.gameObject.name = INTERNAL_NAME;
        dungeon.contentSource = ContentSource.CONTENT_UPDATE_03;
        dungeon.DungeonSeed = 0;
        dungeon.DungeonFloorName = FLOOR_NAME; // what shows up At the top when floor is loaded
        dungeon.DungeonShortName = FLOOR_NAME; // no clue lol, just make it the same
        dungeon.DungeonFloorLevelTextOverride = "Unknown Location"; // what shows up below the floorname
        dungeon.LevelOverrideType = GameManager.LevelOverrideState.NONE;
        dungeon.debugSettings = new DebugDungeonSettings()
        {
            RAPID_DEBUG_DUNGEON_ITERATION_SEEKER = false,
            RAPID_DEBUG_DUNGEON_ITERATION = false,
            RAPID_DEBUG_DUNGEON_COUNT = 50,
            GENERATION_VIEWER_MODE = false,
            FULL_MINIMAP_VISIBILITY = false,
            COOP_TEST = false,
            DISABLE_ENEMIES = false,
            DISABLE_LOOPS = false,
            DISABLE_SECRET_ROOM_COVERS = false,
            DISABLE_OUTLINES = false,
            WALLS_ARE_PITS = false
        };
        dungeon.ForceRegenerationOfCharacters = false;
        dungeon.ActuallyGenerateTilemap = true;
        goheckyourself ??= RatDungeonPrefab.tileIndices.dungeonCollection;
        dungeon.tileIndices = new TileIndices()
        {
            // tilesetId = (GlobalDungeonData.ValidTilesets)CustomValidTilesets.FLOORNAMEGEON, //sets it to our floors CustomValidTileset
            tilesetId = GlobalDungeonData.ValidTilesets.RATGEON,

            //since the tileset im using here is a copy of the Rat dungeon tileset, the first variable in ReplaceDungeonCollection is RatDungeonPrefab.tileIndices.dungeonCollection,
            //otherwise we will use a different dungeon prefab
            // dungeonCollection = toolbox.ReplaceDungeonCollection(gofuckyourself, ModPrefabs.ENV_Tileset_FloorName),
            dungeonCollection = RatDungeonPrefab.tileIndices.dungeonCollection,
            dungeonCollectionSupportsDiagonalWalls = false,
            aoTileIndices = RatDungeonPrefab.tileIndices.aoTileIndices,
            placeBorders = true,
            placePits = false,
            chestHighWallIndices = new List<TileIndexVariant>() {
                new TileIndexVariant() {
                    index = 41,
                    likelihood = 0.5f,
                    overrideLayerIndex = 0,
                    overrideIndex = 0
                }
            },
            decalIndexGrid = null,
            patternIndexGrid = RatDungeonPrefab.tileIndices.patternIndexGrid,
            globalSecondBorderTiles = new List<int>(0),
            edgeDecorationTiles = null
        };
        dungeon.tileIndices.dungeonCollection.name = "ENV_FloorName_Collection";
        dungeon.roomMaterialDefinitions = new DungeonMaterial[] {
            FinalScenario_MainMaterial,
            FinalScenario_MainMaterial,
            FinalScenario_MainMaterial,
            FinalScenario_MainMaterial,
            FinalScenario_MainMaterial,
            FinalScenario_MainMaterial,
            FinalScenario_MainMaterial
        };
        dungeon.dungeonWingDefinitions = new DungeonWingDefinition[0];

        //This section can be used to take parts from other floors and use them as our own.
        //we can make the running dust from one floor our own, the tables from another our own,
        //we can use all of the stuff from the same floor, or if you want, you can make your own.
        dungeon.pathGridDefinitions = new List<TileIndexGrid>() { MinesDungeonPrefab.pathGridDefinitions[0] };
        dungeon.dungeonDustups = new DustUpVFX()
        {
            runDustup = MinesDungeonPrefab.dungeonDustups.runDustup,
            waterDustup = MinesDungeonPrefab.dungeonDustups.waterDustup,
            additionalWaterDustup = MinesDungeonPrefab.dungeonDustups.additionalWaterDustup,
            rollNorthDustup = MinesDungeonPrefab.dungeonDustups.rollNorthDustup,
            rollNorthEastDustup = MinesDungeonPrefab.dungeonDustups.rollNorthEastDustup,
            rollEastDustup = MinesDungeonPrefab.dungeonDustups.rollEastDustup,
            rollSouthEastDustup = MinesDungeonPrefab.dungeonDustups.rollSouthEastDustup,
            rollSouthDustup = MinesDungeonPrefab.dungeonDustups.rollSouthDustup,
            rollSouthWestDustup = MinesDungeonPrefab.dungeonDustups.rollSouthWestDustup,
            rollWestDustup = MinesDungeonPrefab.dungeonDustups.rollWestDustup,
            rollNorthWestDustup = MinesDungeonPrefab.dungeonDustups.rollNorthWestDustup,
            rollLandDustup = MinesDungeonPrefab.dungeonDustups.rollLandDustup
        };
        dungeon.PatternSettings = new SemioticDungeonGenSettings()
        {
            flows = new List<DungeonFlow>() {
                FlowSimpleAsItGets.Init(),
            },
            mandatoryExtraRooms = new List<ExtraIncludedRoomData>(0),
            optionalExtraRooms = new List<ExtraIncludedRoomData>(0),
            MAX_GENERATION_ATTEMPTS = 250,
            DEBUG_RENDER_CANVASES_SEPARATELY = false
        };

        dungeon.damageTypeEffectMatrix = MinesDungeonPrefab.damageTypeEffectMatrix;
        dungeon.stampData = m_FloorNameStampData;
        dungeon.UsesCustomFloorIdea = false;
        dungeon.FloorIdea = new RobotDaveIdea()
        {
            ValidEasyEnemyPlaceables = new DungeonPlaceable[0],
            ValidHardEnemyPlaceables = new DungeonPlaceable[0],
            UseWallSawblades = false,
            UseRollingLogsVertical = true,
            UseRollingLogsHorizontal = true,
            UseFloorPitTraps = false,
            UseFloorFlameTraps = true,
            UseFloorSpikeTraps = true,
            UseFloorConveyorBelts = true,
            UseCaveIns = true,
            UseAlarmMushrooms = false,
            UseChandeliers = true,
            UseMineCarts = false,
            CanIncludePits = false
        };

        //more variable we can copy from other floors, or make our own
        dungeon.PlaceDoors = true;
        dungeon.doorObjects = CatacombsPrefab.doorObjects;
        dungeon.oneWayDoorObjects = MinesDungeonPrefab.oneWayDoorObjects;
        dungeon.oneWayDoorPressurePlate = MinesDungeonPrefab.oneWayDoorPressurePlate;
        dungeon.phantomBlockerDoorObjects = MinesDungeonPrefab.phantomBlockerDoorObjects;
        dungeon.UsesWallWarpWingDoors = false;
        dungeon.baseChestContents = CatacombsPrefab.baseChestContents;
        dungeon.SecretRoomSimpleTriggersFacewall = new List<GameObject>() { CatacombsPrefab.SecretRoomSimpleTriggersFacewall[0] };
        dungeon.SecretRoomSimpleTriggersSidewall = new List<GameObject>() { CatacombsPrefab.SecretRoomSimpleTriggersSidewall[0] };
        dungeon.SecretRoomComplexTriggers = new List<ComplexSecretRoomTrigger>(0);
        dungeon.SecretRoomDoorSparkVFX = CatacombsPrefab.SecretRoomDoorSparkVFX;
        dungeon.SecretRoomHorizontalPoofVFX = CatacombsPrefab.SecretRoomHorizontalPoofVFX;
        dungeon.SecretRoomVerticalPoofVFX = CatacombsPrefab.SecretRoomVerticalPoofVFX;
        dungeon.sharedSettingsPrefab = CatacombsPrefab.sharedSettingsPrefab;
        dungeon.NormalRatGUID = string.Empty;
        dungeon.BossMasteryTokenItemId = CatacombsPrefab.BossMasteryTokenItemId;
        dungeon.UsesOverrideTertiaryBossSets = false;
        dungeon.OverrideTertiaryRewardSets = new List<TertiaryBossRewardSet>(0);
        dungeon.defaultPlayerPrefab = MinesDungeonPrefab.defaultPlayerPrefab;
        dungeon.StripPlayerOnArrival = false;
        dungeon.SuppressEmergencyCrates = false;
        dungeon.SetTutorialFlag = false;
        dungeon.PlayerIsLight = true;
        dungeon.PlayerLightColor = CatacombsPrefab.PlayerLightColor;
        dungeon.PlayerLightIntensity = 4;
        dungeon.PlayerLightRadius = 4;
        dungeon.PrefabsToAutoSpawn = new GameObject[0];

        //include this for custom floor audio (we need to prevent music from playing manually se our hook can properly loop it)
        dungeon.musicEventName = "fakedummymusiceventthatdoesntexist";

        CatacombsPrefab = null;
        RatDungeonPrefab = null;
        MinesDungeonPrefab = null;

        return dungeon;
    }
    public static Dungeon GetOrLoadByName_Orig(string name)
    {
        AssetBundle assetBundle = ResourceManager.LoadAssetBundle("dungeons/" + name.ToLower());
        DebugTime.RecordStartTime();
        Dungeon component = assetBundle.LoadAsset<GameObject>(name).GetComponent<Dungeon>();
        DebugTime.Log("AssetBundle.LoadAsset<Dungeon>({0})", new object[] { name });
        return component;
    }
}
