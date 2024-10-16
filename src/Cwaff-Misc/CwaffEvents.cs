namespace CwaffingTheGungy;

public static class CwaffEvents // global custom events we can listen for
{
    // Runs before first floor is loaded for the first run
    public static Action OnAllModsLoaded;
    // Runs before a new run is started (before level generation)
    public static Action BeforeRunStart;
    // Runs when the previous run is cleaned up (on quick restarting or returning to the breach)
    public static Action OnCleanStart;
    // Runs whenever a new run is started from first floor (floor may not be fully loaded)
    public static Action<PlayerController, PlayerController, GameManager.GameMode> OnRunStartFromFirstFloor;
    // Runs whenever a new run is started from any floor (floor may not be fully loaded)
    public static Action<PlayerController, PlayerController, GameManager.GameMode> OnRunStartFromAnyFloor;
    // Runs whenever any floor is started and fully loaded
    public static Action OnNewFloorFullyLoaded;
    // Runs whenever the Keep of the Lead Lord is started and fully loaded, regardless of whether it's the start of a run
    public static Action OnKeepFullyLoaded;
    // Runs whenever the first floor of a new run is started and fully loaded, regardless of whether it's actually the Keep of the Lead Lord
    public static Action OnFirstFloorOfRunFullyLoaded;
    // Runs whenever the Keep of the Lead Lord is started and fully loaded at the start of a new run
    public static Action OnKeepFullyLoadedForNewRun;
    // Runs whenever a bullet is spawned from an AIBulletBank and an owner is assigned to the projectile
    public static Action<Projectile> OnBankBulletOwnerAssigned;
    // Runs whenever a player enters a new room (either / both of old and new room may be null)
    public static Action<PlayerController, RoomHandler, RoomHandler> OnChangedRooms;
    // Runs whenever a corpse is created
    public static Action<DebrisObject, AIActor> OnCorpseCreated;
    // Runs whenever stats and/or synergies are recalculated
    public static Action<PlayerController> OnStatsRecalculated;

    internal static bool _OnFirstFloor = false;
    internal static bool _RunJustStarted = false;
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
            if (gm == null || gsm == null || !gsm.IsInSession)
                return;

            _OnFirstFloor = GameManager.Instance.GetLastLoadedLevelDefinition().dungeonSceneName == "tt_castle";
            _RunJustStarted = (gsm.GetSessionStatValue(TrackedStats.TIME_PLAYED) < 0.1f);
            if (_RunJustStarted)
            {
                if (OnRunStartFromAnyFloor != null)
                    OnRunStartFromAnyFloor(gm.PrimaryPlayer, gm.SecondaryPlayer, gm.CurrentGameMode);
                if (_OnFirstFloor && OnRunStartFromFirstFloor != null)
                    OnRunStartFromFirstFloor(gm.PrimaryPlayer, gm.SecondaryPlayer, gm.CurrentGameMode);
            }

            gm.OnNewLevelFullyLoaded += OnNewFloorFullyLoadedTempHook;
        }
    }

    private static void OnNewFloorFullyLoadedTempHook()
    {
        GameManager.Instance.OnNewLevelFullyLoaded -= OnNewFloorFullyLoadedTempHook;
        if (OnNewFloorFullyLoaded != null)
            OnNewFloorFullyLoaded();
        if (_OnFirstFloor && OnKeepFullyLoaded != null)
            OnKeepFullyLoaded();
        if (_RunJustStarted && OnFirstFloorOfRunFullyLoaded != null)
            OnFirstFloorOfRunFullyLoaded();
        if (_OnFirstFloor && _RunJustStarted && OnKeepFullyLoadedForNewRun != null)
            OnKeepFullyLoadedForNewRun();
    }

    //NOTE: makes sure AIActor is set properly on bullet scripts; should probably factor out CheckFromReplicantOwner() and moved to a better location later
    //NOTE: could be useful for Schrodinger's Gat -> might want to set up an event listener
    //WARNING: doesn't seem to work properly on large Bullats
    //NOTE: magically fixed itself no later than 2024-05-01???
    //NOTE: ^ wrong. banked projectiles get their owners properly set eventually, but not at the time that StaticReferenceManager.ProjectileAdded is called
    [HarmonyPatch(typeof(AIBulletBank), nameof(AIBulletBank.BulletSpawnedHandler))]
    private class GetRealProjectileOwnerPatch
    {
        public static void Postfix(AIBulletBank __instance, Bullet bullet)
        {
            if (__instance.aiActor is not AIActor actor)
                return;
            if ((bullet == null) || (bullet.Parent == null) || bullet.Parent.GetComponent<Projectile>() is not Projectile p)
                return;
            p.Owner = actor;
            if (OnBankBulletOwnerAssigned != null)
                OnBankBulletOwnerAssigned(p);
        }
    }


    [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.LateUpdate))]
    private class PlayerUpdatePatch
    {
        private static RoomHandler[] _LastRoom = {null, null};

        static void Prefix(PlayerController __instance)
        {
            int id = __instance.PlayerIDX;
            if (__instance.CurrentRoom == _LastRoom[id])
                return;
            if (OnChangedRooms != null)
                OnChangedRooms(__instance, _LastRoom[id], __instance.CurrentRoom);
            _LastRoom[id] = __instance.CurrentRoom;
        }
    }

    [HarmonyPatch(typeof(AIActor), nameof(AIActor.ForceDeath))]
    private class OnCorpseCreatedPatch
    { //REFACTOR: write better IL code
        [HarmonyILManipulator]
        private static void OnCorpseCreatedIL(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            ILLabel endOfCorpseBranch = null;
            if (!cursor.TryGotoNext(MoveType.After,
              instr => instr.MatchLdloc((byte)17), // V_17 == component3 == corpse DebrisObject
              instr => instr.MatchLdnull(),
              instr => instr.MatchCall<UnityEngine.Object>("op_Inequality"),
              instr => instr.MatchBrfalse(out endOfCorpseBranch)
              ))
                return;

            cursor.GotoLabel(endOfCorpseBranch, MoveType.Before, setTarget: true);
            cursor.Emit(OpCodes.Ldloc_S, (byte)17);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.CallPrivate(typeof(OnCorpseCreatedPatch), nameof(OnCorpseCreatedFunc));
            return;
        }

        private static void OnCorpseCreatedFunc(DebrisObject debris, AIActor original)
        {
            if (OnCorpseCreated != null)
                OnCorpseCreated(debris, original);
        }
    }

    //REFACTOR: put this in some sort of caching class
    internal static List<PickupObject> _DebrisPickups = new();

    [HarmonyPatch(typeof(DebrisObject), nameof(DebrisObject.Start))]
    private class DebrisObjectStartPatch
    {
        static void Postfix(DebrisObject __instance)
        {
            if (__instance.IsPickupObject && __instance.gameObject.GetComponent<PickupObject>() is PickupObject pickup)
                _DebrisPickups.Add(pickup);
        }
    }

    [HarmonyPatch(typeof(DebrisObject), nameof(DebrisObject.OnDestroy))]
    private class DebrisObjectOnDestroyPatch
    {
        static void Prefix(DebrisObject __instance)
        {
            if (__instance.IsPickupObject && __instance.gameObject.GetComponent<PickupObject>() is PickupObject pickup)
                _DebrisPickups.TryRemove(pickup);
        }
    }

    //REFACTOR: use this for more mastery checks
    [HarmonyPatch(typeof(PlayerStats), nameof(PlayerStats.RecalculateStatsInternal))]
    private class PlayerStatsRecalculateStatsInternalPatch
    {
        private static bool _CurrentlyRecalculatingStats = false;
        static void Postfix(PlayerStats __instance, PlayerController owner)
        {
            if (_CurrentlyRecalculatingStats)
                return;

            _CurrentlyRecalculatingStats = true; // prevent infinite recursion
            if (OnStatsRecalculated != null)
                OnStatsRecalculated(owner);
            _CurrentlyRecalculatingStats = false;
        }
    }
}
