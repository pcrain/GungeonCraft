using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using MonoMod.RuntimeDetour;
using System.IO;
using Dungeonator;
using System.Reflection;
using System.Collections;

namespace SaveAPI
{
    /// <summary>
    /// The core class in SaveAPI
    /// </summary>
    public static class SaveAPIManager
    {
        /// <summary>
        /// Call this method in your <see cref="ETGModule.Init"/> method. Adds SaveAPI <see cref="Hook"/>s, loads <see cref="AdvancedGameStatsManager"/> and setups the custom <see cref="SaveManager.SaveType"/>s
        /// </summary>
        /// <param name="prefix">Mod prefix for SaveTypes</param>
        public static void Setup(string prefix)
        {
            if (m_loaded)
            {
                return;
            }
            AdvancedGameSave = new SaveManager.SaveType
            {
                filePattern = "Slot{0}." + prefix + "Save",
                encrypted = true,
                backupCount = 3,
                backupPattern = "Slot{0}." + prefix + "Backup.{1}",
                backupMinTimeMin = 45,
                legacyFilePattern = prefix + "GameStatsSlot{0}.txt"
            };
            AdvancedMidGameSave = new SaveManager.SaveType
            {
                filePattern = "Active{0}." + prefix + "Game",
                legacyFilePattern = prefix + "ActiveSlot{0}.txt",
                encrypted = true,
                backupCount = 0,
                backupPattern = "Active{0}." + prefix + "Backup.{1}",
                backupMinTimeMin = 60
            };
            for (int i = 0; i < 3; i++)
            {
                SaveManager.SaveSlot saveSlot = (SaveManager.SaveSlot)i;
                SaveTools.SafeMove(Path.Combine(SaveManager.OldSavePath, string.Format(AdvancedGameSave.legacyFilePattern, saveSlot)), Path.Combine(SaveManager.OldSavePath,
                    string.Format(AdvancedGameSave.filePattern, saveSlot)), false);
                SaveTools.SafeMove(Path.Combine(SaveManager.OldSavePath, string.Format(AdvancedGameSave.filePattern, saveSlot)), Path.Combine(SaveManager.OldSavePath,
                    string.Format(AdvancedGameSave.filePattern, saveSlot)), false);
                SaveTools.SafeMove(SaveTools.PathCombine(SaveManager.SavePath, "01", string.Format(AdvancedGameSave.filePattern, saveSlot)), Path.Combine(SaveManager.SavePath,
                    string.Format(AdvancedGameSave.filePattern, saveSlot)), true);
            }
            CustomHuntQuests.DoSetup();
            saveHook = new Hook(
                typeof(GameStatsManager).GetMethod("Save", BindingFlags.Public | BindingFlags.Static),
                typeof(SaveAPIManager).GetMethod("SaveHook")
            );
            loadHook = new Hook(
                typeof(GameStatsManager).GetMethod("Load", BindingFlags.Public | BindingFlags.Static),
                typeof(SaveAPIManager).GetMethod("LoadHook")
            );
            resetHook = new Hook(
                typeof(GameStatsManager).GetMethod("DANGEROUS_ResetAllStats", BindingFlags.Public | BindingFlags.Static),
                typeof(SaveAPIManager).GetMethod("ResetHook")
            );
            beginSessionHook = new Hook(
                typeof(GameStatsManager).GetMethod("BeginNewSession", BindingFlags.Public | BindingFlags.Instance),
                typeof(SaveAPIManager).GetMethod("BeginSessionHook")
            );
            endSessionHook = new Hook(
                typeof(GameStatsManager).GetMethod("EndSession", BindingFlags.Public | BindingFlags.Instance),
                typeof(SaveAPIManager).GetMethod("EndSessionHook")
            );
            clearAllStatsHook = new Hook(
                typeof(GameStatsManager).GetMethod("ClearAllStatsGlobal", BindingFlags.Public | BindingFlags.Instance),
                typeof(SaveAPIManager).GetMethod("ClearAllStatsHook")
            );
            deleteMidGameSaveHook = new Hook(
                typeof(SaveManager).GetMethod("DeleteCurrentSlotMidGameSave", BindingFlags.Public | BindingFlags.Static),
                typeof(SaveAPIManager).GetMethod("DeleteMidGameSaveHook")
            );
            midgameSaveHook = new Hook(
                typeof(GameManager).GetMethod("DoMidgameSave", BindingFlags.Public | BindingFlags.Static),
                typeof(SaveAPIManager).GetMethod("MidgameSaveHook")
            );
            invalidateSaveHook = new Hook(
                typeof(GameManager).GetMethod("InvalidateMidgameSave", BindingFlags.Public | BindingFlags.Static),
                typeof(SaveAPIManager).GetMethod("InvalidateSaveHook")
            );
            revalidateSaveHook = new Hook(
                typeof(GameManager).GetMethod("RevalidateMidgameSave", BindingFlags.Public | BindingFlags.Static),
                typeof(SaveAPIManager).GetMethod("RevalidateSaveHook")
            );
            frameDelayedInitizlizationHook = new Hook(
                typeof(Dungeon).GetMethod("FrameDelayedMidgameInitialization", BindingFlags.NonPublic | BindingFlags.Instance),
                typeof(SaveAPIManager).GetMethod("FrameDelayedInitizlizationHook")
            );
            moveSessionStatsHook = new Hook(
                typeof(GameStatsManager).GetMethod("MoveSessionStatsToSavedSessionStats", BindingFlags.Public | BindingFlags.Instance),
                typeof(SaveAPIManager).GetMethod("MoveSessionStatsHook")
            );
            prerequisiteHook = new Hook(
                typeof(DungeonPrerequisite).GetMethod("CheckConditionsFulfilled", BindingFlags.Public | BindingFlags.Instance),
                typeof(SaveAPIManager).GetMethod("PrerequisiteHook")
            );
            clearActiveGameDataHook = new Hook(
                typeof(GameManager).GetMethod("ClearActiveGameData", BindingFlags.Public | BindingFlags.Instance),
                typeof(SaveAPIManager).GetMethod("ClearActiveGameDataHook")
            );
            aiactorRewardsHook = new Hook(
                typeof(AIActor).GetMethod("HandleRewards", BindingFlags.NonPublic | BindingFlags.Instance),
                typeof(SaveAPIManager).GetMethod("AIActorRewardsHook")
            );
            aiactorEngagedHook = new Hook(
                typeof(AIActor).GetMethod("OnEngaged", BindingFlags.NonPublic | BindingFlags.Instance),
                typeof(SaveAPIManager).GetMethod("AIActorEngagedHook")
            );
            LoadGameStatsFirstLoad();
            BreachShopTool.DoSetup();
            m_loaded = true;
        }

        public static bool IsFirstLoad
        {
            get
            {
                return FirstLoad;
            }
        }

        /// <summary>
        /// Unloads SaveAPI and then setups SaveAPI again
        /// </summary>
        /// <param name="prefix">Mod prefix for SaveTypes</param>
        public static void Reload(string prefix)
        {
            Unload();
            Setup(prefix);
        }

        private static void LoadGameStatsFirstLoad()
        {
            bool cachedvalue = FirstLoad;
            FirstLoad = true;
            GameStatsManager.Load();
            FirstLoad = cachedvalue;
        }

        /// <summary>
        /// Disposes SaveAPI <see cref="Hook"/>s, unloads <see cref="AdvancedGameStatsManager"/> and nulls custom <see cref="SaveManager.SaveType"/>s
        /// </summary>
        public static void Unload()
        {
            if (!m_loaded)
            {
                return;
            }
            AdvancedGameSave = null;
            AdvancedMidGameSave = null;
            saveHook?.Dispose();
            loadHook?.Dispose();
            resetHook?.Dispose();
            beginSessionHook?.Dispose();
            endSessionHook?.Dispose();
            clearAllStatsHook?.Dispose();
            deleteMidGameSaveHook?.Dispose();
            midgameSaveHook?.Dispose();
            invalidateSaveHook?.Dispose();
            revalidateSaveHook?.Dispose();
            frameDelayedInitizlizationHook?.Dispose();
            moveSessionStatsHook?.Dispose();
            prerequisiteHook?.Dispose();
            clearActiveGameDataHook?.Dispose();
            aiactorRewardsHook?.Dispose();
            aiactorEngagedHook?.Dispose();
            CustomHuntQuests.Unload();
            AdvancedGameStatsManager.Save();
            AdvancedGameStatsManager.Unload();
            BreachShopTool.Unload();
            m_loaded = false;
        }

        /// <summary>
        /// Gets <paramref name="flag"/>'s value
        /// </summary>
        /// <param name="flag">The flag to check</param>
        /// <returns>The value of <paramref name="flag"/> or <see langword="false"/> if <see cref="AdvancedGameStatsManager"/> doesn't have an instance</returns>
        public static bool GetFlag(CustomDungeonFlags flag)
        {
            if (!AdvancedGameStatsManager.HasInstance)
            {
                return false;
            }
            return AdvancedGameStatsManager.Instance.GetFlag(flag);
        }

        /// <summary>
        /// Gets the total value of <paramref name="stat"/>
        /// </summary>
        /// <param name="stat">Target stat.</param>
        /// <returns>The value of <paramref name="stat"/> or 0 if <see cref="AdvancedGameStatsManager"/> doesn't have an instance</returns>
        public static float GetPlayerStatValue(CustomTrackedStats stat)
        {
            if (!AdvancedGameStatsManager.HasInstance)
            {
                return 0f;
            }
            return AdvancedGameStatsManager.Instance.GetPlayerStatValue(stat);
        }

        /// <summary>
        /// Gets the session value of <paramref name="stat"/>
        /// </summary>
        /// <param name="stat">Target stat</param>
        /// <returns>The value of <paramref name="stat"/> in the current session or 0 if <see cref="AdvancedGameStatsManager"/> doesn't have an instance or the player isn't in a session</returns>
        public static float GetSessionStatValue(CustomTrackedStats stat)
        {
            if (AdvancedGameStatsManager.HasInstance && AdvancedGameStatsManager.Instance.IsInSession)
            {
                return AdvancedGameStatsManager.Instance.GetSessionStatValue(stat);
            }
            return 0f;
        }

        /// <summary>
        /// Gets <paramref name="character"/>'s <paramref name="stat"/> value.
        /// </summary>
        /// <param name="stat">Target stat</param>
        /// <param name="character">The character</param>
        /// <returns><paramref name="character"/>'s <paramref name="stat"/> value or 0 if <see cref="AdvancedGameStatsManager"/> doesn't have an instance</returns>
        public static float GetCharacterStatValue(PlayableCharacters character, CustomTrackedStats stat)
        {
            if (AdvancedGameStatsManager.HasInstance)
            {
                return AdvancedGameStatsManager.Instance.GetCharacterStatValue(character, stat);
            }
            return 0f;
        }

        /// <summary>
        /// Gets the primary player's or the Pilot's (if primary player doesn't exist) <paramref name="stat"/> value.
        /// </summary>
        /// <param name="stat">Target stat</param>
        /// <returns>Primary player's or the Pilot's (if primary player doesn't exist) <paramref name="stat"/> value or 0 if <see cref="AdvancedGameStatsManager"/> doesn't haven an instance</returns>
        public static float GetCharacterStatValue(CustomTrackedStats stat)
        {
            if (AdvancedGameStatsManager.HasInstance)
            {
                if(GameManager.HasInstance && GameManager.Instance.PrimaryPlayer != null)
                {
                    return AdvancedGameStatsManager.Instance.GetCharacterStatValue(stat);
                }
                return AdvancedGameStatsManager.Instance.GetCharacterStatValue(PlayableCharacters.Pilot, stat);
            }
            return 0f;
        }

        /// <summary>
        /// Gets the session character's or the Pilot's (if the player isn't in session) <paramref name="flag"/> value
        /// </summary>
        /// <param name="flag">The character-specific flag to check</param>
        /// <returns>The session character's or the Pilot's (if the player isn't in session) <paramref name="flag"/> value or 0 if <see cref="AdvancedGameStatsManager"/> doesn't have an instance</returns>
        public static bool GetCharacterSpecificFlag(CustomCharacterSpecificGungeonFlags flag)
        {
            if (AdvancedGameStatsManager.HasInstance)
            {
                if (AdvancedGameStatsManager.Instance.IsInSession)
                {
                    return AdvancedGameStatsManager.Instance.GetCharacterSpecificFlag(flag);
                }
                return AdvancedGameStatsManager.Instance.GetCharacterSpecificFlag(PlayableCharacters.Pilot, flag);
            }
            return false;
        }

        /// <summary>
        /// Gets <paramref name="character"/>'s <paramref name="flag"/> value
        /// </summary>
        /// <param name="character">Target character</param>
        /// <param name="flag">The character-specific flag to check</param>
        /// <returns><paramref name="character"/>'s <paramref name="flag"/> value or 0 if <see cref="AdvancedGameStatsManager"/> doesn't have an instance</returns>
        public static bool GetCharacterSpecificFlag(PlayableCharacters character, CustomCharacterSpecificGungeonFlags flag)
        {
            if (AdvancedGameStatsManager.HasInstance)
            {
                return AdvancedGameStatsManager.Instance.GetCharacterSpecificFlag(character, flag);
            }
            return false;
        }

        /// <summary>
        /// Gets <paramref name="maximum"/>'s value in total.
        /// </summary>
        /// <param name="maximum">Target maximum</param>
        /// <returns><paramref name="maximum"/> value or 0 if <see cref="AdvancedGameStatsManager"/> doesn't have an instance</returns>
        public static float GetPlayerMaximum(CustomTrackedMaximums maximum)
        {
            if (AdvancedGameStatsManager.HasInstance)
            {
                return AdvancedGameStatsManager.Instance.GetPlayerMaximum(maximum);
            }
            return 0f;
        }

        /// <summary>
        /// Sets <paramref name="flag"/>'s value to <paramref name="value"/>
        /// </summary>
        /// <param name="flag">The target flag</param>
        /// <param name="value">Value to set</param>
        public static void SetFlag(CustomDungeonFlags flag, bool value)
        {
            if (AdvancedGameStatsManager.HasInstance)
            {
                AdvancedGameStatsManager.Instance.SetFlag(flag, value);
            }
        }

        /// <summary>
        /// Sets <paramref name="stat"/>'s value to <paramref name="value"/>
        /// </summary>
        /// <param name="stat">The target stat</param>
        /// <param name="value">Value to set</param>
        public static void SetStat(CustomTrackedStats stat, float value)
        {
            if (AdvancedGameStatsManager.HasInstance)
            {
                AdvancedGameStatsManager.Instance.SetStat(stat, value);
            }
        }

        /// <summary>
        /// Increments <paramref name="stat"/> value by <paramref name="value"/>
        /// </summary>
        /// <param name="stat">Target stat</param>
        /// <param name="value">Increment value</param>
        public static void RegisterStatChange(CustomTrackedStats stat, float value)
        {
            if (AdvancedGameStatsManager.HasInstance)
            {
                AdvancedGameStatsManager.Instance.RegisterStatChange(stat, value);
            }
        }

        /// <summary>
        /// Sets <paramref name="maximum"/>'s value to <paramref name="value"/> if <paramref name="maximum"/>'s current value is less than <paramref name="value"/>
        /// </summary>
        /// <param name="maximum">Target maximum</param>
        /// <param name="value">Value to set</param>
        public static void UpdateMaximum(CustomTrackedMaximums maximum, float value)
        {
            if (AdvancedGameStatsManager.HasInstance)
            {
                AdvancedGameStatsManager.Instance.UpdateMaximum(maximum, value);
            }
        }

        /// <summary>
        /// Sets the session character's or the Pilot's (if the player isn't in a session) <paramref name="flag"/> value
        /// </summary>
        /// <param name="flag">Target flag</param>
        /// <param name="value">Value to set</param>
        public static void SetCharacterSpecificFlag(CustomCharacterSpecificGungeonFlags flag, bool value)
        {
            if (AdvancedGameStatsManager.HasInstance)
            {
                AdvancedGameStatsManager.Instance.SetCharacterSpecificFlag(flag, value);
            }
        }

        /// <summary>
        /// Sets <paramref name="character"/>'s <paramref name="flag"/> value
        /// </summary>
        /// <param name="character">Target character</param>
        /// <param name="flag">Target flag</param>
        /// <param name="value">Value to set</param>
        public static void SetCharacterSpecificFlag(PlayableCharacters character, CustomCharacterSpecificGungeonFlags flag, bool value)
        {
            if (AdvancedGameStatsManager.HasInstance)
            {
                AdvancedGameStatsManager.Instance.SetCharacterSpecificFlag(character, flag, value);
            }
        }

        public static void AIActorEngagedHook(Action<AIActor, bool> orig, AIActor self, bool isReinforcement)
        {
            if (!self.HasBeenEngaged)
            {
                if (self.SetsCustomFlagOnActivation())
                {
                    AdvancedGameStatsManager.Instance.SetFlag(self.GetCustomFlagToSetOnActivation(), true);
                }
            }
            orig(self, isReinforcement);
        }

        public static void AIActorRewardsHook(Action<AIActor> orig, AIActor self)
        {
            FieldInfo i = typeof(AIActor).GetField("m_hasGivenRewards", BindingFlags.NonPublic | BindingFlags.Instance);
            if (!(bool)i.GetValue(self) && !self.IsTransmogrified)
            {
                if (self.SetsCustomFlagOnDeath())
                {
                    AdvancedGameStatsManager.Instance.SetFlag(self.GetCustomFlagToSetOnDeath(), true);
                }
                if (self.SetsCustomCharacterSpecificFlagOnDeath())
                {
                    AdvancedGameStatsManager.Instance.SetCharacterSpecificFlag(self.GetCustomCharacterSpecificFlagToSetOnDeath(), true);
                }
            }
            orig(self);
        }

        public static bool SaveHook(Func<bool> orig)
        {
            bool result = orig();
            AdvancedGameStatsManager.Save();
            return result;
        }

        public static void LoadHook(Action orig)
        {
            AdvancedGameStatsManager.Load();
            orig();
        }

        public static void ResetHook(Action orig)
        {
            AdvancedGameStatsManager.DANGEROUS_ResetAllStats();
            orig();
        }

        public static void BeginSessionHook(Action<GameStatsManager, PlayerController> orig, GameStatsManager self, PlayerController player)
        {
            orig(self, player);
            AdvancedGameStatsManager.Instance.BeginNewSession(player);
        }

        public static void EndSessionHook(Action<GameStatsManager, bool, bool> orig, GameStatsManager self, bool recordSessionStats, bool decrementDifferentiator = true)
        {
            orig(self, recordSessionStats, decrementDifferentiator);
            AdvancedGameStatsManager.Instance.EndSession(recordSessionStats);
        }

        public static void ClearAllStatsHook(Action<GameStatsManager> orig, GameStatsManager self)
        {
            orig(self);
            AdvancedGameStatsManager.Instance.ClearAllStatsGlobal();
        }

        public static void DeleteMidGameSaveHook(Action<SaveManager.SaveSlot?> orig, SaveManager.SaveSlot? overrideSaveSlot)
        {
            orig(overrideSaveSlot);
            if (AdvancedGameStatsManager.HasInstance)
            {
                AdvancedGameStatsManager.Instance.midGameSaveGuid = null;
            }
            string path = string.Format(SaveManager.MidGameSave.filePattern, (overrideSaveSlot == null) ? SaveManager.CurrentSaveSlot : overrideSaveSlot.Value);
            string path2 = Path.Combine(SaveManager.SavePath, path);
            if (File.Exists(path2))
            {
                File.Delete(path2);
            }
        }

        public static void MidgameSaveHook(Action<GlobalDungeonData.ValidTilesets> orig, GlobalDungeonData.ValidTilesets tileset)
        {
            AdvancedGameStatsManager.DoMidgameSave();
            orig(tileset);
        }

        public static void InvalidateSaveHook(Action<bool> orig, bool savestats)
        {
            AdvancedGameStatsManager.InvalidateMidgameSave(false);
            orig(savestats);
        }

        public static void RevalidateSaveHook(Action orig)
        {
            AdvancedGameStatsManager.RevalidateMidgameSave(false);
            orig();
        }

        public static IEnumerator FrameDelayedInitizlizationHook(Func<Dungeon, MidGameSaveData, IEnumerator> orig, Dungeon self, MidGameSaveData data)
        {
            yield return orig(self, data);
            AdvancedMidGameSaveData midgameSave;
            if (AdvancedGameStatsManager.VerifyAndLoadMidgameSave(out midgameSave, true))
            {
                midgameSave.LoadDataFromMidGameSave();
            }
            yield break;
        }

        public static GameStats MoveSessionStatsHook(Func<GameStatsManager, GameStats> orig, GameStatsManager self)
        {
            AdvancedGameStatsManager.Instance.MoveSessionStatsToSavedSessionStats();
            return orig(self);
        }

        public static bool PrerequisiteHook(Func<DungeonPrerequisite, bool> orig, DungeonPrerequisite self)
        {
            if (self is CustomDungeonPrerequisite)
            {
                return (self as CustomDungeonPrerequisite).CheckConditionsFulfilled();
            }
            return orig(self);
        }

        public static void ClearActiveGameDataHook(Action<GameManager, bool, bool> orig, GameManager self, bool destroyGameManager, bool endSession)
        {
            orig(self, destroyGameManager, endSession);
            OnActiveGameDataCleared?.Invoke(self, destroyGameManager, endSession);
        }

        private static Hook saveHook;
        private static Hook loadHook;
        private static Hook resetHook;
        private static Hook beginSessionHook;
        private static Hook endSessionHook;
        private static Hook clearAllStatsHook;
        private static Hook deleteMidGameSaveHook;
        private static Hook midgameSaveHook;
        private static Hook invalidateSaveHook;
        private static Hook revalidateSaveHook;
        private static Hook frameDelayedInitizlizationHook;
        private static Hook moveSessionStatsHook;
        private static Hook prerequisiteHook;
        private static Hook clearActiveGameDataHook;
        private static Hook aiactorRewardsHook;
        private static Hook aiactorEngagedHook;
        private static bool m_loaded;
        public static SaveManager.SaveType AdvancedGameSave;
        public static SaveManager.SaveType AdvancedMidGameSave;
        public static OnActiveGameDataClearedDelegate OnActiveGameDataCleared;
        public delegate void OnActiveGameDataClearedDelegate(GameManager manager, bool destroyGameManager, bool endSession);
        private static bool FirstLoad;
    }
}
