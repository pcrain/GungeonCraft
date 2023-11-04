namespace CwaffingTheGungy;

public static class CwaffEvents // global custom events we can listen for
{
    // Runs whenever a new run is started (floor may not be fully loaded)
    public static Action<PlayerController, PlayerController, GameManager.GameMode> OnRunStart;
    // Runs whenever a floor is started and fully loaded
    public static Action OnNewFloorFullyLoaded;
    // Runs whenever the first floor is started and fully loaded
    public static Action OnFirstFloorFullyLoaded;

    internal static bool _OnFirstFloor = false;

    public static void Init()
    {
        #region Set Up Hooks
            new Hook(
                typeof(Dungeon).GetMethod("FloorReached", BindingFlags.Instance | BindingFlags.Public),
                typeof(CwaffEvents).GetMethod("FloorReachedHook"));
        #endregion

        #region Set Up Events
            // OnRunStart += (_,_,_) => ETGModConsole.Log($"run started \\o/");
        #endregion
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
