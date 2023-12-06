namespace CwaffingTheGungy;

public static class CwaffEvents // global custom events we can listen for
{
    // Runs before first floor is loaded for the first run
    public static Action OnAllModsLoaded;
    // Runs before a new run is started (before level generation)
    public static Action OnCleanStart;
    // Runs whenever a new run is started (floor may not be fully loaded)
    public static Action<PlayerController, PlayerController, GameManager.GameMode> OnRunStart;
    // Runs whenever a floor is started and fully loaded
    public static Action OnNewFloorFullyLoaded;
    // Runs whenever the first floor is started and fully loaded
    public static Action OnFirstFloorFullyLoaded;

    internal static bool _OnFirstFloor = false;
    internal static bool _AllModsLoaded = false;

    public static void Init()
    {
        new Hook(
            typeof(Dungeon).GetMethod("FloorReached", BindingFlags.Instance | BindingFlags.Public),
            typeof(CwaffEvents).GetMethod("FloorReachedHook"));

        new Hook(
            typeof(GameManager).GetMethod("ClearActiveGameData", BindingFlags.Instance | BindingFlags.Public),
            typeof(CwaffEvents).GetMethod("ClearActiveGameDataHook"));

        new Hook(
            typeof(GameManager).GetMethod("SetNextLevelIndex", BindingFlags.Instance | BindingFlags.Public),
            typeof(CwaffEvents).GetMethod("SetNextLevelIndexHook"));
    }

    public static void SetNextLevelIndexHook(Action<GameManager, int> orig, GameManager self, int index)
    {
        orig(self, index);
        if (_AllModsLoaded)
            return;

        _AllModsLoaded = true;
        if (OnAllModsLoaded != null)
            OnAllModsLoaded();
    }

    public static void ClearActiveGameDataHook(Action<GameManager, bool, bool> orig, GameManager self, bool destroyGameManager, bool endSession)
    {
        orig(self, destroyGameManager, endSession);
        if (OnCleanStart != null)
            OnCleanStart();
    }

    public static void FloorReachedHook(Action<Dungeon> orig, Dungeon self)
    {
        orig(self);
        GameManager gm = GameManager.Instance;
        GameStatsManager gsm = GameStatsManager.Instance;
        if (gm == null || !(gsm?.IsInSession ?? false))
            return;

        _OnFirstFloor = gsm.GetSessionStatValue(TrackedStats.TIME_PLAYED) < 0.1f;
        if (_OnFirstFloor && OnRunStart != null)
            OnRunStart(gm.PrimaryPlayer, gm.SecondaryPlayer, gm.CurrentGameMode);

        gm.OnNewLevelFullyLoaded += OnNewFloorFullyLoadedTempHook;
    }

    private static void OnNewFloorFullyLoadedTempHook()
    {
        GameManager.Instance.OnNewLevelFullyLoaded -= OnNewFloorFullyLoadedTempHook;
        if (OnNewFloorFullyLoaded != null)
            OnNewFloorFullyLoaded();
        if (_OnFirstFloor && OnFirstFloorFullyLoaded != null)
            OnFirstFloorFullyLoaded();
    }
}
