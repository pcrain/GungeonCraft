namespace CwaffingTheGungy;

public class CwaffDungeons
{
    public string internalName                     = null;
    public string floorName                        = null;
    public string floorMusic                       = null;
    public int    loopPoint                        = -1;
    public int    rewindAmount                     = -1;
    public string dungeonPrefabTemplate            = null;
    public GameLevelDefinition gameLevelDefinition = null;
    public Func<Dungeon, Dungeon> dungeonGenerator = null;

    public static Dictionary<string, CwaffDungeons> Flows = new();

    private static Hook _GetOrLoadByNameHook = null;
    private static Hook _ForceCustomMusicHook;

    public static void Init()
    {
        _GetOrLoadByNameHook = new Hook(
            typeof(DungeonDatabase).GetMethod("GetOrLoadByName", BindingFlags.Static | BindingFlags.Public),
            typeof(CwaffDungeons).GetMethod("GetOrLoadByNameHook", BindingFlags.Static | BindingFlags.Public)
        );

        _ForceCustomMusicHook = new Hook(
            typeof(DungeonFloorMusicController).GetMethod("ResetForNewFloor", BindingFlags.Instance | BindingFlags.Public),
            typeof(CwaffDungeons).GetMethod("PlayFloorMusicHook", BindingFlags.Static | BindingFlags.NonPublic)
        );
    }

    public static CwaffDungeons Register(string internalName, string floorName, Func<Dungeon, Dungeon> dungeonGenerator,
        string dungeonPrefabTemplate = null, GameLevelDefinition gameLevelDefinition = null, string floorMusic = null,
        int loopPoint = -1, int rewindAmount = -1)
    {
        if (Flows.ContainsKey(internalName))
            return Flows[internalName]; // already registered

        // sets the level definition of the GameLevelDefinition in GameManager.Instance.customFloors if it exists
        foreach (GameLevelDefinition levelDefinition in GameManager.Instance.customFloors)
            if (levelDefinition.dungeonSceneName == internalName)
                gameLevelDefinition = levelDefinition;

        GameManager.Instance.customFloors.Add(gameLevelDefinition);
        ResourceManager.LoadAssetBundle("brave_resources_001").LoadAsset<GameObject>("_GameManager").GetComponent<GameManager>().customFloors.Add(gameLevelDefinition);

        Flows[internalName] = new CwaffDungeons() {
            internalName          = internalName,
            floorName             = floorName,
            floorMusic            = floorMusic,
            loopPoint             = loopPoint,
            rewindAmount          = rewindAmount,
            dungeonPrefabTemplate = dungeonPrefabTemplate,
            gameLevelDefinition   = gameLevelDefinition,
            dungeonGenerator      = dungeonGenerator,
        };
        return Flows[internalName];
    }

    public static Dungeon GetOrLoadByNameHook(Func<string, Dungeon> orig, string name)
    {
        CwaffDungeons dungeonData;
        ETGModConsole.Log($"attempting to find custom flow with name {name}");
        if (!Flows.TryGetValue(name, out dungeonData))
            return orig(name);
        ETGModConsole.Log($"  found {name}!!");

        if (dungeonData.gameLevelDefinition != null && !GameManager.Instance.customFloors.Contains(dungeonData.gameLevelDefinition))
            GameManager.Instance.customFloors.Add(dungeonData.gameLevelDefinition); // fixes a bug where returning to the breach deletes the floor, thanks Bunny!

        Dungeon dungeon = null;
        if (name.ToLower() == dungeonData.internalName)
        {
            try
            {
                dungeon = dungeonData.dungeonGenerator(GetOrLoadByName_Orig(dungeonData.dungeonPrefabTemplate ?? "Base_Gungeon"));
            }
            catch (Exception e)
            {
                ETGModConsole.Log($"exception: {e}");
            }
        }
        return dungeon ?? orig(name);
    }

    public static Dungeon GetOrLoadByName_Orig(string name)
    {
        AssetBundle assetBundle = ResourceManager.LoadAssetBundle("dungeons/" + name.ToLower());
        DebugTime.RecordStartTime();
        Dungeon component = assetBundle.LoadAsset<GameObject>(name).GetComponent<Dungeon>();
        DebugTime.Log("AssetBundle.LoadAsset<Dungeon>({0})", new object[] { name });
        return component;
    }

    internal static void PlayFloorMusicHook(Action<DungeonFloorMusicController, Dungeon> orig, DungeonFloorMusicController musicController, Dungeon d)
    {
        orig(musicController, d);
        foreach (CwaffDungeons cd in Flows.Values)
        {
            if (d.DungeonFloorName != cd.floorName)
                continue;
            if (!string.IsNullOrEmpty(cd.floorMusic))
                GameManager.Instance.DungeonMusicController.LoopMusic(
                    musicName: cd.floorMusic, loopPoint: cd.loopPoint, rewindAmount: cd.rewindAmount);
            break;
        }
    }

    private const string INTERNAL_NAME = "One-off";
    private const string FLOOR_NAME    = "One-off Floor";
    public static Dungeon GenericGenerator(Dungeon dungeon)
    {
        Dungeon MinesDungeonPrefab                                        = CwaffDungeons.GetOrLoadByName_Orig("Base_Mines");
        Dungeon CatacombsPrefab                                           = CwaffDungeons.GetOrLoadByName_Orig("Base_Catacombs");
        Dungeon RatDungeonPrefab                                          = CwaffDungeons.GetOrLoadByName_Orig("Base_ResourcefulRat");

        DungeonMaterial mat                        = UnityEngine.Object.Instantiate(RatDungeonPrefab.roomMaterialDefinitions[0]);
        mat.supportsPits                           = true;
        mat.doPitAO                                = false;
        mat.useLighting                            = true;
        mat.lightPrefabs.elements[0].rawGameObject = MinesDungeonPrefab.roomMaterialDefinitions[0].lightPrefabs.elements[0].rawGameObject;
        mat.roomFloorBorderGrid                    = MinesDungeonPrefab.roomMaterialDefinitions[0].roomFloorBorderGrid;
        mat.pitLayoutGrid                          = MinesDungeonPrefab.roomMaterialDefinitions[0].pitLayoutGrid;
        mat.pitBorderFlatGrid                      = MinesDungeonPrefab.roomMaterialDefinitions[0].pitBorderFlatGrid;

        DungeonTileStampData stampData    = ScriptableObject.CreateInstance<DungeonTileStampData>();
        stampData.name                    = "ENV_FloorName_STAMP_DATA";
        stampData.tileStampWeight         = 0;
        stampData.spriteStampWeight       = 0;
        stampData.objectStampWeight       = 1;
        stampData.stamps                  = new TileStampData[0];
        stampData.spriteStamps            = new SpriteStampData[0];
        stampData.objectStamps            = RatDungeonPrefab.stampData.objectStamps;
        stampData.SymmetricFrameChance    = 0.25f;
        stampData.SymmetricCompleteChance = 0.6f;

        dungeon.gameObject.name               = INTERNAL_NAME;
        dungeon.contentSource                 = ContentSource.CONTENT_UPDATE_03;
        dungeon.DungeonSeed                   = 0;
        dungeon.DungeonFloorName              = FLOOR_NAME; // what shows up At the top when floor is loaded
        dungeon.DungeonShortName              = FLOOR_NAME; // no clue lol, just make it the same
        dungeon.DungeonFloorLevelTextOverride = "Debug Time"; // what shows up below the floorname
        dungeon.LevelOverrideType             = GameManager.LevelOverrideState.NONE;
        dungeon.debugSettings                 = new DebugDungeonSettings()
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
        dungeon.tileIndices = new TileIndices()
        {
            // tilesetId = (GlobalDungeonData.ValidTilesets)CustomValidTilesets.FLOORNAMEGEON, //sets it to our floors CustomValidTileset
            tilesetId = GlobalDungeonData.ValidTilesets.CASTLEGEON,

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
            mat,
            mat,
            mat,
            mat,
            mat,
            mat,
            mat
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
                OneOffDebugDungeonFlow.GetCurrentCustomDebugFlow(),
            },
            mandatoryExtraRooms = new List<ExtraIncludedRoomData>(0),
            optionalExtraRooms = new List<ExtraIncludedRoomData>(0),
            MAX_GENERATION_ATTEMPTS = 250,
            DEBUG_RENDER_CANVASES_SEPARATELY = false
        };

        dungeon.damageTypeEffectMatrix = MinesDungeonPrefab.damageTypeEffectMatrix;
        dungeon.stampData = stampData;
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
        // dungeon.musicEventName = "fakedummymusiceventthatdoesntexist";

        CatacombsPrefab    = null;
        RatDungeonPrefab   = null;
        MinesDungeonPrefab = null;

        return dungeon;
    }
}
