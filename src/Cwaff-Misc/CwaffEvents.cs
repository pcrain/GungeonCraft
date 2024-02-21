namespace CwaffingTheGungy;

public static class CwaffEvents // global custom events we can listen for
{
    // Runs before first floor is loaded for the first run
    public static Action OnAllModsLoaded;
    // Runs before a new run is started (before level generation)
    public static Action BeforeRunStart;
    // Runs when the previous run is cleaned up (on quick restarting or returning to the breach)
    public static Action OnCleanStart;
    // Runs whenever a new run is started (floor may not be fully loaded)
    public static Action<PlayerController, PlayerController, GameManager.GameMode> OnRunStart;
    // Runs whenever a floor is started and fully loaded
    public static Action OnNewFloorFullyLoaded;
    // Runs whenever the first floor is started and fully loaded
    public static Action OnFirstFloorFullyLoaded;

    internal static bool _OnFirstFloor = false;
    internal static bool _AllModsLoaded = false;

    [HarmonyPatch(typeof(FinalIntroSequenceManager), nameof(FinalIntroSequenceManager.Start))]
    private class FinalIntroSequenceManagerStartPatch // doesn't actually hook the ienumerator itsef, only the implicit method that calls it
    {
        static void Prefix()
        {
            if (_AllModsLoaded)
                return;

            _AllModsLoaded = true;
            if (OnAllModsLoaded != null)
                OnAllModsLoaded();

            if (OnCleanStart != null)
                OnCleanStart(); // the first run counts as a clean start as well

            // for some reason, if we don't do this, custom rooms won't be loaded on the first run started from the breach
            //   this method is already called if we quickstart a run
            GameManager.Instance.GlobalInjectionData.PreprocessRun();
        }
    }

    [HarmonyPatch(typeof(GameManager), nameof(GameManager.LoadNextLevel))]
    private class LoadNextLevelPatch
    {
        static void Prefix(GameManager __instance)
        {
            if (BeforeRunStart != null && (__instance.nextLevelIndex == 1 || (GameManager.SKIP_FOYER && __instance.nextLevelIndex == 0)))
            {
                bool reallyStartedNewRun = GameStatsManager.Instance.GetSessionStatValue(TrackedStats.TIME_PLAYED) < 0.1f;
                if (reallyStartedNewRun)
                    BeforeRunStart();
            }
        }
    }

    [HarmonyPatch(typeof(GameManager), nameof(GameManager.ClearActiveGameData))]
    private class ClearActiveGameDataPatch
    {
        static void Postfix(bool destroyGameManager, bool endSession)
        {
            if (OnCleanStart != null)
                OnCleanStart();
        }
    }

    [HarmonyPatch(typeof(Dungeon), nameof(Dungeon.FloorReached))]
    private class FloorReachedPatch
    {
        static void Postfix()
        {
            GameManager gm = GameManager.Instance;
            GameStatsManager gsm = GameStatsManager.Instance;
            if (gm == null || !(gsm?.IsInSession ?? false))
                return;

            _OnFirstFloor = gsm.GetSessionStatValue(TrackedStats.TIME_PLAYED) < 0.1f;
            if (_OnFirstFloor && OnRunStart != null)
                OnRunStart(gm.PrimaryPlayer, gm.SecondaryPlayer, gm.CurrentGameMode);

            gm.OnNewLevelFullyLoaded += OnNewFloorFullyLoadedTempHook;
        }
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
