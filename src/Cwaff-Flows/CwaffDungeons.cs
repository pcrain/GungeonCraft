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
        // Lazy.DebugLog($"attempting to find custom flow with name {name}");
        if (!Flows.TryGetValue(name, out dungeonData))
            return orig(name);
        Lazy.DebugLog($"  found {name}!!");

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
                Lazy.DebugLog($"  exception: {e}");
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
}
