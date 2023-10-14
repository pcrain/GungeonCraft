using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.ComponentModel;  //debug
using System.IO;
using System.Runtime.InteropServices; // audio loading
using System.Text.RegularExpressions;

using UnityEngine;
using MonoMod.RuntimeDetour;

using Gungeon;
using Dungeonator;
using Alexandria.ItemAPI;

namespace CwaffingTheGungy
{
    public static class CwaffEvents // global custom events we can listen for
    {
        // Runs whenever a new run is started (floor may not be fully loaded)
        public static Action<PlayerController, PlayerController, GameManager.GameMode> OnRunStart;
        public static Action OnNewFloorFullyLoaded;

        // Runs whenever a floor is started (floor may not be fully loaded)

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

            if (gsm.GetSessionStatValue(TrackedStats.TIME_PLAYED) < 0.1f && OnRunStart != null)
                OnRunStart(gm.PrimaryPlayer, gm.SecondaryPlayer, gm.CurrentGameMode);

            gm.OnNewLevelFullyLoaded += OnNewFloorFullyLoadedTempHook;
        }

        private static void OnNewFloorFullyLoadedTempHook()
        {
            GameManager.Instance.OnNewLevelFullyLoaded -= OnNewFloorFullyLoadedTempHook;
            if (OnNewFloorFullyLoaded != null)
                OnNewFloorFullyLoaded();
        }
    }
}
