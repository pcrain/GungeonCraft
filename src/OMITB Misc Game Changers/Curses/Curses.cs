using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Alexandria.ItemAPI;
using Alexandria.Misc;
using System.Collections;
using System.Reflection;
using MonoMod.RuntimeDetour;
using SaveAPI;

namespace NevernamedsItems
{
    public class CurseManager
    {
        public static void Init()
        {
            CurrentActiveCurses = new List<CurseData>();
            CursePrefabs = new List<CurseData>();
            FloorAndGenerationToolbox.OnFloorEntered += LevelLoaded;
            FloorAndGenerationToolbox.OnNewGame += OnNewRun;
            CurseEffects.Init();
            VFXScapegoat = new GameObject("CurseVFXScapegoat");
            VFXScapegoat.gameObject.SetActive(false);
            UnityEngine.Object.DontDestroyOnLoad(VFXScapegoat);
            CurseIconCollection = SpriteBuilder.ConstructCollection(VFXScapegoat, "CurseIcon_Collection");
            UnityEngine.Object.DontDestroyOnLoad(CurseIconCollection);

            //ETGModConsole.Log("Hook Passed");

            #region SetUpCurses
            CurseData curseOfInfestation = new CurseData();
            curseOfInfestation.curseName = "Curse of Infestation";
            curseOfInfestation.curseSubtitle = "They crawl beneath the surface";
            curseOfInfestation.curseIconId = SpriteBuilder.AddSpriteToCollection("NevernamedsItems/Resources/CurseIcons/infestation_icon", CurseIconCollection);
            CursePrefabs.Add(curseOfInfestation);

            CurseData curseOfSludge = new CurseData();
            curseOfSludge.curseName = "Curse of Sludge";
            curseOfSludge.curseSubtitle = "You. Will. Love. My... Toxic love";
            curseOfSludge.curseIconId = SpriteBuilder.AddSpriteToCollection("NevernamedsItems/Resources/CurseIcons/sludge_icon", CurseIconCollection);
            CursePrefabs.Add(curseOfSludge);

            CurseData curseOfTheHive = new CurseData();
            curseOfTheHive.curseName = "Curse of The Hive";
            curseOfTheHive.curseSubtitle = "You hear a faint buzzing";
            curseOfTheHive.curseIconId = SpriteBuilder.AddSpriteToCollection("NevernamedsItems/Resources/CurseIcons/hive_icon", CurseIconCollection);
            CursePrefabs.Add(curseOfTheHive);

            CurseData curseOfTheFlames = new CurseData();
            curseOfTheFlames.curseName = "Curse of The Flames";
            curseOfTheFlames.curseSubtitle = "Cannot live with me.";
            curseOfTheFlames.curseIconId = SpriteBuilder.AddSpriteToCollection("NevernamedsItems/Resources/CurseIcons/flames_icon", CurseIconCollection);
            CursePrefabs.Add(curseOfTheFlames);

            CurseData curseOfButterfingers = new CurseData();
            curseOfButterfingers.curseName = "Curse of Butterfingers";
            curseOfButterfingers.curseSubtitle = "Be careful not to slip up";
            curseOfButterfingers.curseIconId = SpriteBuilder.AddSpriteToCollection("NevernamedsItems/Resources/CurseIcons/butterfingers_icon", CurseIconCollection);
            CursePrefabs.Add(curseOfButterfingers);

            CurseData curseOfDarkness = new CurseData();
            curseOfDarkness.curseName = "Curse of Darkness";
            curseOfDarkness.curseSubtitle = "Spirit of the Night";
            curseOfDarkness.curseIconId = SpriteBuilder.AddSpriteToCollection("NevernamedsItems/Resources/CurseIcons/darkness_icon", CurseIconCollection);
            CursePrefabs.Add(curseOfDarkness);
            #endregion
        }
        public static Hook floorLoadPlayerHook;
        private static tk2dSpriteCollectionData CurseIconCollection;
        private static GameObject VFXScapegoat;
        public static bool levelOnCooldown = false;
        public static void OnNewRun(PlayerController player)
        {
            cursesLastFloor = new List<string>();
            bannedCursesThisRun = new List<string>();
            previousCursesThisRun = new List<string>();
            RemoveAllCurses();
        }
        public static void LevelLoaded()
        {
            if (cursesLastFloor == null) cursesLastFloor = new List<string>();
            foreach (CurseData curse in CurrentActiveCurses)
            {
                cursesLastFloor.Add(curse.curseName);
            }
            CurseManager.RemoveAllCurses();
            float allCurse = GameManager.Instance.GetCombinedPlayersStatAmount(PlayerStats.StatType.Curse);
            float ran = UnityEngine.Random.value;
            Debug.Log("Running Curse Check on Floor Load - Random (" + ran + ") - CurseTotal (" + allCurse + ")");
            float curseChance = 0.0666f;

            if (!SaveAPIManager.GetFlag(CustomDungeonFlags.CURSES_DISABLED))
            {
                if (UnityEngine.Random.value <= (allCurse * curseChance))
                {
                    float hellclears = GameStatsManager.Instance.GetPlayerStatValue(TrackedStats.TIMES_CLEARED_BULLET_HELL);
                    if (hellclears > 0)
                    {
                        AddRandomCurse();
                    }
                }
            }
            if (PostNewLevelCurseProcessing != null) CurseManager.PostNewLevelCurseProcessing();
            InherentPostLevelCurseProcessing();
            GameManager.Instance.StartCoroutine(DoCursePopups());
        }
        public static void AddRandomCurse(bool doPopup = false)
        {
            List<CurseData> refinedData = new List<CurseData>();
            refinedData.AddRange(CursePrefabs);
            if (CurrentActiveCurses.Count > 0)
            {
                for (int i = (refinedData.Count - 1); i >= 0; i--)
                {
                    if (CurseManager.CurseIsActive(refinedData[i].curseName))
                    {
                        refinedData.RemoveAt(i);
                    }
                    else if (cursesLastFloor != null && cursesLastFloor.Contains(refinedData[i].curseName))
                    {
                        refinedData.RemoveAt(i);
                    }
                    else if (bannedCursesThisRun != null && bannedCursesThisRun.Contains(refinedData[i].curseName))
                    {
                        refinedData.RemoveAt(i);
                    }
                }
            }
            if (refinedData.Count > 0)
            {
                CurseData pickedCurse = BraveUtility.RandomElement(refinedData);
                AddCurse(pickedCurse.curseName, doPopup);
            }
        }
        private static IEnumerator DoCursePopups()
        {
            yield return new WaitForSeconds(1);
            if (CurrentActiveCurses.Count > 0)
            {
                foreach (CurseData curse in CurrentActiveCurses)
                {
                    Debug.Log("CursePopup Processed: " + curse.curseName);
                    DoSpecificCursePopup(curse.curseName, curse.curseSubtitle, curse.curseIconId);
                }
            }
            yield break;
        }
        private static void DoSpecificCursePopup(string cursename, string curseSubtext, int id)
        {
            GameUIRoot.Instance.notificationController.DoCustomNotification(
                   cursename,
                    curseSubtext,
                    CurseIconCollection,
                    id,
                    UINotificationController.NotificationColor.PURPLE,
                    true,
                    false
                    );
        }
        public static void AddCurse(string CurseName, bool dopopup = false)
        {
            if (CurseManager.CurseIsActive(CurseName))
            {
                Debug.LogWarning("Attempted to Add Curse (" + CurseName + ") but it was already active!");
                return;
            }
            CurseData newCurse = null;
            foreach (CurseData data in CursePrefabs)
            {
                if (data.curseName == CurseName) newCurse = data;
            }
            if (!(GameManager.Instance.AnyPlayerHasPickupID(HoleyWater.HoleyWaterID) && !GameManager.Instance.AnyPlayerHasActiveSynergy("The Last Crusade")))
            {
                CurrentActiveCurses.Add(newCurse);
                if (previousCursesThisRun == null) previousCursesThisRun = new List<string>();
                if (previousCursesThisRun.Contains(newCurse.curseName))
                {
                    if (bannedCursesThisRun == null) bannedCursesThisRun = new List<string>();
                    bannedCursesThisRun.Add(newCurse.curseName);
                }
                else
                {
                    previousCursesThisRun.Add(newCurse.curseName);
                }
                Debug.Log("Added New Curse: " + newCurse.curseName + " (Popup: " + dopopup + ")");
                if (dopopup) DoSpecificCursePopup(CurseName, newCurse.curseSubtitle, newCurse.curseIconId);
            }
            InherentPostLevelCurseProcessing();
        }
        private static void InherentPostLevelCurseProcessing()
        {
            if (CurseIsActive("Curse of Darkness") && !CustomDarknessHandler.isDark)
            {
                if (GameManager.Instance.AnyPlayerHasActiveSynergy("The Last Crusade"))
                {
                    Minimap.Instance.RevealAllRooms(false);
                }
                else
                {
                    CustomDarknessHandler.shouldBeDark.SetOverride("DarknessCurse", true);
                }
            }
        }
        public static void RemoveCurse(string name)
        {
            if (CurseIsActive(name))
            {
                for (int i = (CurrentActiveCurses.Count - 1); i >= 0; i--)
                {
                    if (CurrentActiveCurses[i].curseName == name)
                    {
                        if (CurrentActiveCurses[i].curseName == "Curse of Darkness")
                        {
                            CustomDarknessHandler.shouldBeDark.RemoveOverride("DarknessCurse");
                        }
                        CurrentActiveCurses.RemoveAt(i);
                    }
                }
            }
        }
        public static void RemoveAllCurses()
        {
            List<string> cursesToRemove = new List<string>();
            if (CurrentActiveCurses.Count > 0)
            {
                foreach (CurseData curse in CurrentActiveCurses)
                {
                    cursesToRemove.Add(curse.curseName);
                }
                foreach (string curse in cursesToRemove)
                {
                    RemoveCurse(curse);
                }
            }
        }
        public static event Action PostNewLevelCurseProcessing;
        public static List<CurseData> CurrentActiveCurses;
        public static List<CurseData> CursePrefabs;
        public static bool CurseIsActive(string CurseName)
        {
            foreach (CurseData data in CurrentActiveCurses)
            {
                if (data.curseName == CurseName) return true;
            }
            return false;
        }
        public static List<string> previousCursesThisRun;
        public static List<string> bannedCursesThisRun;
        public static List<string> cursesLastFloor;

        public class CurseData
        {
            public string curseName = null;
            public string curseSubtitle = null;
            public int curseIconId = -1;
        }
    }
}
