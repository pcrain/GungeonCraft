namespace CwaffingTheGungy;

public class CwaffDungeons
{
    internal static List<DungeonFlow> _KnownFlows;

    public string internalName                     = null;
    public string floorName                        = null;
    public string floorMusic                       = null;
    public int    loopPoint                        = -1;
    public int    rewindAmount                     = -1;
    public string dungeonPrefabTemplate            = null;
    public GameLevelDefinition gameLevelDefinition = null;
    public Func<Dungeon, Dungeon> dungeonGenerator = null;

    public static Dictionary<string, CwaffDungeons> Flows = new();

    public static void InitDungeonFlows(AssetBundle sharedAssets2, bool refreshFlows = false) {
        _KnownFlows = new List<DungeonFlow>(){
            SansDungeonFlow.Init(),
        };
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

    /// <summary>Fixes a bug where returning to the breach deletes the floor, thanks Bunny!</summary>
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.LoadCustomLevel))]
    private class ReregisterOurCustomLevelIfNecessaryPatch
    {
        static void Prefix(GameManager __instance, string custom)
        {
            if (!Flows.TryGetValue(custom, out CwaffDungeons dungeonData))
                return;  // not trying to load one of our custom flows, so not our problem
            if (dungeonData.gameLevelDefinition == null || GameManager.Instance.customFloors.Contains(dungeonData.gameLevelDefinition))
                return;  // our flow is already know about by the game manager, so nothing to be done
            // Lazy.DebugLog($"reinjecting floor {custom} in new GameManager instance");
            GameManager.Instance.customFloors.Add(dungeonData.gameLevelDefinition);
        }
    }

    [HarmonyPatch(typeof(DungeonDatabase), nameof(DungeonDatabase.GetOrLoadByName))]
    private class GetOrLoadCustomDungeonPatch
    {
        static bool Prefix(string name, ref Dungeon __result)
        {
            // Lazy.DebugLog($"attempting to find custom flow with name {name}");
            if (!Flows.TryGetValue(name, out CwaffDungeons dungeonData))
                return true; // fall back on original method if it's not one of our flows
            // Lazy.DebugLog($"  found {name}!!");

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
            if (!dungeon)
                return true; // fall back on original method

            __result = dungeon;
            return false; // skip original method
        }
    }

    public static Dungeon GetOrLoadByName_Orig(string name)
    {
        AssetBundle assetBundle = ResourceManager.LoadAssetBundle("dungeons/" + name.ToLower());
        return assetBundle.LoadAsset<GameObject>(name).GetComponent<Dungeon>();
    }

    [HarmonyPatch(typeof(DungeonFloorMusicController), nameof(DungeonFloorMusicController.ResetForNewFloor))]
    private class ForceCustomMusicPatch
    {
        static void Postfix(Dungeon d)
        {
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

    [HarmonyPatch(typeof(FlowDatabase), nameof(FlowDatabase.GetOrLoadByName))]
    private class LoadCustomFlowPatch // Patch for making sure we can load custom flows okay
    {
        static bool Prefix(string name, ref DungeonFlow __result)
        {
            if (_KnownFlows == null || _KnownFlows.Count <= 0)
                return true;

            string flowName = name.ToLower();
            if (flowName.Contains("/"))
                flowName = flowName.Substring(flowName.LastIndexOf("/") + 1);

            foreach (DungeonFlow flow in _KnownFlows)
            {
                if (!string.IsNullOrEmpty(flow.name) && flowName == flow.name.ToLower())
                {
                    __result = flow;
                    return false;
                }
            }
            return true;
        }
    }
}
