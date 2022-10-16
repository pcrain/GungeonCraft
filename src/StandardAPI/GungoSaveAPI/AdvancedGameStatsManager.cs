using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FullSerializer;
using UnityEngine;
using System.Collections;

namespace SaveAPI
{
    /// <summary>
    /// The class that stores all your custom information
    /// </summary>
    [fsObject]
    class AdvancedGameStatsManager
    {
        public AdvancedGameStatsManager()
        {
            this.m_flags = new HashSet<CustomDungeonFlags>(new CustomDungeonFlagsComparer());
            this.m_characterStats = new Dictionary<PlayableCharacters, AdvancedGameStats>(new PlayableCharactersComparer());
            this.m_numCharacters = -1;
            this.cachedHuntIndex = -1;
        }

        /// <summary>
        /// Nulls the instance of <see cref="AdvancedGameStatsManager"/>
        /// </summary>
        public static void Unload()
        {
            m_instance = null;
        }

        /// <summary>
        /// Sets <paramref name="character"/>'s <paramref name="flag"/> value to <paramref name="value"/>.
        /// </summary>
        /// <param name="character">The character</param>
        /// <param name="flag">Target flag</param>
        /// <param name="value">The flag's new value</param>
        public void SetCharacterSpecificFlag(PlayableCharacters character, CustomCharacterSpecificGungeonFlags flag, bool value)
        {
            if (flag == CustomCharacterSpecificGungeonFlags.NONE)
            {
                Debug.LogError("Something is attempting to set a NONE character-specific save flag!");
                return;
            }
            if (!this.m_characterStats.ContainsKey(character))
            {
                this.m_characterStats.Add(character, new AdvancedGameStats());
            }
            if (this.m_sessionStats != null && this.m_sessionCharacter == character)
            {
                this.m_sessionStats.SetFlag(flag, value);
            }
            else
            {
                this.m_characterStats[character].SetFlag(flag, value);
            }
        }

        /// <summary>
        /// Sets <paramref name="stat"/>'s value to <paramref name="value"/>
        /// </summary>
        /// <param name="stat">Target stat</param>
        /// <param name="value">The stat's new value</param>
        public void SetStat(CustomTrackedStats stat, float value)
        {
            if (float.IsNaN(value))
            {
                return;
            }
            if (float.IsInfinity(value))
            {
                return;
            }
            if (this.m_sessionStats == null)
            {
                return;
            }
            this.m_sessionStats.SetStat(stat, value);
        }

        /// <summary>
        /// Sets <paramref name="maximum"/>'s value to <paramref name="val"/> if <paramref name="maximum"/>'s current value is less than <paramref name="val"/>
        /// </summary>
        /// <param name="maximum">The maximum to set</param>
        /// <param name="val">The maximum's new value</param>
        public void UpdateMaximum(CustomTrackedMaximums maximum, float val)
        {
            if (float.IsNaN(val))
            {
                return;
            }
            if (float.IsInfinity(val))
            {
                return;
            }
            if (this.m_sessionStats == null)
            {
                return;
            }
            this.m_sessionStats.SetMax(maximum, val);
        }

        /// <summary>
        /// Gets the session character's <paramref name="flag"/> value
        /// </summary>
        /// <param name="flag">Target flag</param>
        /// <returns>The value of session character's <paramref name="flag"/></returns>
        public bool GetCharacterSpecificFlag(CustomCharacterSpecificGungeonFlags flag)
        {
            return this.GetCharacterSpecificFlag(this.m_sessionCharacter, flag);
        }

        /// <summary>
        /// Gets <paramref name="character"/>'s <paramref name="flag"/> value
        /// </summary>
        /// <param name="character">Target character</param>
        /// <param name="flag">The flag to check</param>
        /// <returns><paramref name="character"/>'s <paramref name="flag"/> value</returns>
        public bool GetCharacterSpecificFlag(PlayableCharacters character, CustomCharacterSpecificGungeonFlags flag)
        {
            if (flag == CustomCharacterSpecificGungeonFlags.NONE)
            {
                Debug.LogError("Something is attempting to get a NONE character-specific save flag!");
                return false;
            }
            if (this.m_sessionStats != null && this.m_sessionCharacter == character)
            {
                if (this.m_sessionStats.GetFlag(flag))
                {
                    return true;
                }
                if (this.m_savedSessionStats.GetFlag(flag))
                {
                    return true;
                }
            }
            AdvancedGameStats gameStats;
            return this.m_characterStats.TryGetValue(character, out gameStats) && gameStats.GetFlag(flag);
        }

        /// <summary>
        /// <see cref="AdvancedGameStatsManager"/>.DoMidgameSave() is only used for hooks. Use <see cref="GameManager"/>.DoMidgameSave() instead
        /// </summary>
        public static void DoMidgameSave()
        {
            string midGameSaveGuid = Guid.NewGuid().ToString();
            AdvancedMidGameSaveData obj = new AdvancedMidGameSaveData(midGameSaveGuid);
            SaveManager.Save(obj, SaveAPIManager.AdvancedMidGameSave, GameStatsManager.Instance.PlaytimeMin, 0u, null);
            Instance.midGameSaveGuid = midGameSaveGuid;
            Save();
        }

        /// <summary>
        /// Increments <paramref name="stat"/>'s value by <paramref name="value"/>
        /// </summary>
        /// <param name="stat">Stat to increment</param>
        /// <param name="value">Increment value</param>
        public void RegisterStatChange(CustomTrackedStats stat, float value)
        {
            if (this.m_sessionStats == null)
            {
                Debug.LogError("No session stats active and we're registering a stat change!");
                return;
            }
            if (float.IsNaN(value))
            {
                return;
            }
            if (float.IsInfinity(value))
            {
                return;
            }
            if (Mathf.Abs(value) > 10000f)
            {
                return;
            }
            this.m_sessionStats.IncrementStat(stat, value);
        }

        /// <summary>
        /// Invalidates the current <see cref="AdvancedMidGameSaveData"/>
        /// </summary>
        /// <param name="saveStats">If true, it will also save <see cref="AdvancedGameStats"/></param>
        public static void InvalidateMidgameSave(bool saveStats)
        {
            AdvancedMidGameSaveData midGameSaveData = null;
            if (VerifyAndLoadMidgameSave(out midGameSaveData, false))
            {
                midGameSaveData.Invalidate();
                SaveManager.Save(midGameSaveData, SaveAPIManager.AdvancedMidGameSave, GameStatsManager.Instance.PlaytimeMin, 0u, null);
                GameStatsManager.Instance.midGameSaveGuid = midGameSaveData.midGameSaveGuid;
                if (saveStats)
                {
                    GameStatsManager.Save();
                }
            }
        }

        /// <summary>
        /// Revalidates the current <see cref="AdvancedMidGameSaveData"/>
        /// </summary>
        /// <param name="saveStats">If true, it will also save <see cref="AdvancedGameStats"/></param>
        public static void RevalidateMidgameSave(bool saveStats)
        {
            AdvancedMidGameSaveData midGameSaveData = null;
            if (VerifyAndLoadMidgameSave(out midGameSaveData, false))
            {
                midGameSaveData.Revalidate();
                SaveManager.Save(midGameSaveData, SaveAPIManager.AdvancedMidGameSave, GameStatsManager.Instance.PlaytimeMin, 0u, null);
                GameStatsManager.Instance.midGameSaveGuid = midGameSaveData.midGameSaveGuid;
                if (saveStats)
                {
                    GameStatsManager.Save();
                }
            }
        }

        /// <summary>
        /// Verifies and loads the current <see cref="AdvancedMidGameSaveData"/>
        /// </summary>
        /// <param name="midgameSave">The loaded midgame save</param>
        /// <param name="checkValidity">If <see langword="true"/>, it will not load invalid <see cref="AdvancedMidGameSaveData"/>s</param>
        /// <returns><see langword="true"/> if it succeeded, <see langword="false"/> if not</returns>
        public static bool VerifyAndLoadMidgameSave(out AdvancedMidGameSaveData midgameSave, bool checkValidity = true)
        {
            if (!SaveManager.Load(SaveAPIManager.AdvancedGameSave, out midgameSave, true, 0u, null, null))
            {
                Debug.LogError("No mid game save found");
                return false;
            }
            if (midgameSave == null)
            {
                Debug.LogError("Failed to load mid game save (0)");
                return false;
            }
            if (checkValidity && !midgameSave.IsValid())
            {
                return false;
            }
            if (GameStatsManager.Instance.midGameSaveGuid == null || GameStatsManager.Instance.midGameSaveGuid != midgameSave.midGameSaveGuid)
            {
                Debug.LogError("Failed to load mid game save (1)");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Clears all <see cref="CustomTrackedStats"/>, <see cref="CustomTrackedMaximums"/> and <see cref="CustomCharacterSpecificGungeonFlags"></see>
        /// </summary>
        public void ClearAllStatsGlobal()
        {
            this.m_sessionStats.ClearAllState();
            this.m_savedSessionStats.ClearAllState();
            if (this.m_numCharacters <= 0)
            {
                this.m_numCharacters = Enum.GetValues(typeof(PlayableCharacters)).Length;
            }
            for (int i = 0; i < this.m_numCharacters; i++)
            {
                AdvancedGameStats gameStats;
                if (this.m_characterStats.TryGetValue((PlayableCharacters)i, out gameStats))
                {
                    gameStats.ClearAllState();
                }
            }
        }

        /// <summary>
        /// Clears a <paramref name="stat"/>'s value from session stats, saved session stats and character stats
        /// </summary>
        /// <param name="stat"></param>
        public void ClearStatValueGlobal(CustomTrackedStats stat)
        {
            this.m_sessionStats.SetStat(stat, 0f);
            this.m_savedSessionStats.SetStat(stat, 0f);
            if (this.m_numCharacters <= 0)
            {
                this.m_numCharacters = Enum.GetValues(typeof(PlayableCharacters)).Length;
            }
            for (int i = 0; i < this.m_numCharacters; i++)
            {
                AdvancedGameStats gameStats;
                if (this.m_characterStats.TryGetValue((PlayableCharacters)i, out gameStats))
                {
                    gameStats.SetStat(stat, 0f);
                }
            }
        }

        private PlayableCharacters GetCurrentCharacter()
        {
            return GameManager.Instance.PrimaryPlayer.characterIdentity;
        }

        /// <summary>
        /// Gets <paramref name="maximum"/>'s value in total.
        /// </summary>
        /// <param name="maximum">Target maximum</param>
        /// <returns><paramref name="maximum"/> value</returns>
        public float GetPlayerMaximum(CustomTrackedMaximums maximum)
        {
            if (this.m_numCharacters <= 0)
            {
                this.m_numCharacters = Enum.GetValues(typeof(PlayableCharacters)).Length;
            }
            float num = 0f;
            if (this.m_sessionStats != null)
            {
                num = Mathf.Max(new float[]
                {
                num,
                this.m_sessionStats.GetMaximumValue(maximum),
                this.m_savedSessionStats.GetMaximumValue(maximum)
                });
            }
            for (int i = 0; i < this.m_numCharacters; i++)
            {
                AdvancedGameStats gameStats;
                if (this.m_characterStats.TryGetValue((PlayableCharacters)i, out gameStats))
                {
                    num = Mathf.Max(num, gameStats.GetMaximumValue(maximum));
                }
            }
            return num;
        }

        /// <summary>
        /// Gets the value of <paramref name="stat"/> in total
        /// </summary>
        /// <param name="stat">Target stat.</param>
        /// <returns>The value of <paramref name="stat"/></returns>
        public float GetPlayerStatValue(CustomTrackedStats stat)
        {
            if (this.m_numCharacters <= 0)
            {
                this.m_numCharacters = Enum.GetValues(typeof(PlayableCharacters)).Length;
            }
            float num = 0f;
            if (this.m_sessionStats != null)
            {
                num += this.m_sessionStats.GetStatValue(stat);
            }
            for (int i = 0; i < this.m_numCharacters; i++)
            {
                AdvancedGameStats gameStats;
                if (this.m_characterStats.TryGetValue((PlayableCharacters)i, out gameStats))
                {
                    num += gameStats.GetStatValue(stat);
                }
            }
            return num;
        }

        /// <summary>
        /// Sets the session character's <paramref name="flag"/> value
        /// </summary>
        /// <param name="flag">Target flag</param>
        /// <param name="value">Value to set</param>
        public void SetCharacterSpecificFlag(CustomCharacterSpecificGungeonFlags flag, bool value)
        {
            this.SetCharacterSpecificFlag(this.m_sessionCharacter, flag, value);
        }

        /// <summary>
        /// Gets the session value of <paramref name="stat"/>
        /// </summary>
        /// <param name="stat"></param>
        /// <returns></returns>
        public float GetSessionStatValue(CustomTrackedStats stat)
        {
            return this.m_sessionStats.GetStatValue(stat) + this.m_savedSessionStats.GetStatValue(stat);
        }

        /// <summary>
        /// Gets the primary player's <paramref name="stat"/> value.
        /// </summary>
        /// <param name="stat">Target stat</param>
        /// <returns>Primary player's or the Pilot's (if primary player doesn't exist) <paramref name="stat"/> value</returns>
        /// <exception cref="T:System.NullReferenceException">
		///   Primary player is null</exception>
        public float GetCharacterStatValue(CustomTrackedStats stat)
        {
            return this.GetCharacterStatValue(this.GetCurrentCharacter(), stat);
        }

        /// <summary>
        /// Moves session stats to saved session stats
        /// </summary>
        /// <returns>Saved session stats</returns>
        public AdvancedGameStats MoveSessionStatsToSavedSessionStats()
        {
            if (!this.IsInSession)
            {
                return null;
            }
            if (this.m_sessionStats != null)
            {
                if (this.m_characterStats.ContainsKey(this.m_sessionCharacter))
                {
                    this.m_characterStats[this.m_sessionCharacter].AddStats(this.m_sessionStats);
                }
                this.m_savedSessionStats.AddStats(this.m_sessionStats);
                this.m_sessionStats.ClearAllState();
            }
            return this.m_savedSessionStats;
        }

        /// <summary>
        /// Gets <paramref name="character"/>'s <paramref name="stat"/> value.
        /// </summary>
        /// <param name="stat">Target stat</param>
        /// <param name="character">The character</param>
        /// <returns><paramref name="character"/>'s <paramref name="stat"/> value</returns>
        public float GetCharacterStatValue(PlayableCharacters character, CustomTrackedStats stat)
        {
            float num = 0f;
            if (this.m_sessionCharacter == character)
            {
                num += this.m_sessionStats.GetStatValue(stat);
            }
            if (this.m_characterStats.ContainsKey(character))
            {
                num += this.m_characterStats[character].GetStatValue(stat);
            }
            return num;
        }

        /// <summary>
        /// AdvancedGameStatsManager.BeginNewSession() is only used for hooks. Use GameStatsManager.BeginNewSession() instead
        /// </summary>
        /// <param name="player">Session character</param>
        public void BeginNewSession(PlayerController player)
        {
            if (this.m_characterStats == null)
            {
                this.m_characterStats = new Dictionary<PlayableCharacters, AdvancedGameStats>(new PlayableCharactersComparer());
            }
            if (this.IsInSession)
            {
                this.m_sessionCharacter = player.characterIdentity;
                if (!this.m_characterStats.ContainsKey(player.characterIdentity))
                {
                    this.m_characterStats.Add(player.characterIdentity, new AdvancedGameStats());
                }
            }
            else
            {
                this.m_sessionCharacter = player.characterIdentity;
                this.m_sessionStats = new AdvancedGameStats();
                this.m_savedSessionStats = new AdvancedGameStats();
                if (!this.m_characterStats.ContainsKey(player.characterIdentity))
                {
                    this.m_characterStats.Add(player.characterIdentity, new AdvancedGameStats());
                }
            }
        }

        /// <summary>
        /// AdvancedGameStatsManager.EndSession() is only used for hooks. Use GameStatsManager.EndSession() instead
        /// </summary>
        /// <param name="recordSessionStats">If <see langword="true"/>, moves session stats to character stats</param>
        public void EndSession(bool recordSessionStats)
        {
            if (!this.IsInSession)
            {
                return;
            }
            if (this.m_sessionStats != null)
            {
                if (recordSessionStats)
                {
                    if (this.m_characterStats.ContainsKey(this.m_sessionCharacter))
                    {
                        this.m_characterStats[this.m_sessionCharacter].AddStats(this.m_sessionStats);
                    }
                    else
                    {
                    }
                }
                this.m_sessionStats = null;
                this.m_savedSessionStats = null;
            }
        }

        /// <summary>
        /// Returns <see langword="true"/> if the player is in a session
        /// </summary>
        [fsIgnore]
        public bool IsInSession
        {
            get
            {
                return this.m_sessionStats != null;
            }
        }

        /// <summary>
        /// Loads <see cref="AdvancedGameStatsManager"/> from AdvancedGameSave <see cref="SaveManager.SaveType"/>
        /// </summary>
        public static void Load()
        {
            SaveManager.Init(); 
            bool hasPrevInstance = false;
            SaveManager.SaveSlot? prevInstanceSaveSlot = null;
            int prevInstanceHuntIndex = -1;
            if (m_instance != null)
            {
                hasPrevInstance = true;
                prevInstanceSaveSlot = m_instance.cachedSaveSlot;
                prevInstanceHuntIndex = m_instance.cachedHuntIndex;
            }
            if (!SaveManager.Load(SaveAPIManager.AdvancedGameSave, out m_instance, true, 0u, null, null))
            {
                m_instance = new AdvancedGameStatsManager();
            }
            m_instance.cachedSaveSlot = SaveManager.CurrentSaveSlot;
            if (hasPrevInstance && prevInstanceSaveSlot != null && m_instance.cachedSaveSlot == prevInstanceSaveSlot.Value)
            {
                m_instance.cachedHuntIndex = prevInstanceHuntIndex;
            }
            else
            {
                m_instance.cachedHuntIndex = -1;
            }
        }

        /// <summary>
        /// Makes a new Instance and deletes <see cref="AdvancedGameStatsManager"/> backups
        /// </summary>
        public static void DANGEROUS_ResetAllStats()
        {
            m_instance = new AdvancedGameStatsManager();
            SaveManager.DeleteAllBackups(SaveAPIManager.AdvancedGameSave, null);
        }

        /// <summary>
        /// Gets the value of <paramref name="flag"/>
        /// </summary>
        /// <param name="flag">Flag to check</param>
        /// <returns>The value of <paramref name="flag"/></returns>
        public bool GetFlag(CustomDungeonFlags flag)
        {
            if (flag == CustomDungeonFlags.NONE)
            {
                Debug.LogError("Something is attempting to get a NONE save flag!");
                return false;
            }
            return this.m_flags.Contains(flag);
        }

        /// <summary>
        /// Sets <paramref name="flag"/>'s value to <paramref name="value"/>
        /// </summary>
        /// <param name="flag">Flag to set</param>
        /// <param name="value">The flag's new value</param>
        public void SetFlag(CustomDungeonFlags flag, bool value)
        {
            if (flag == CustomDungeonFlags.NONE)
            {
                Debug.LogError("Something is attempting to set a NONE save flag!");
                return;
            }
            if (value)
            {
                this.m_flags.Add(flag);
            }
            else
            {
                this.m_flags.Remove(flag);
            }
        }

        /// <summary>
        /// Saves <see cref="AdvancedGameStatsManager"/> to AdvancedGameSave <see cref="SaveManager.SaveType"/>
        /// </summary>
        /// <returns></returns>
        public static bool Save()
        {
            bool result = false;
            try
            {
                result = SaveManager.Save<AdvancedGameStatsManager>(m_instance, SaveAPIManager.AdvancedGameSave, GameStatsManager.Instance.PlaytimeMin, 0u, null);
            }
            catch (Exception ex)
            {
                Debug.LogErrorFormat("SAVE FAILED: {0}", new object[]
                {
                    ex
                });
            }
            return result;
        }

        /// <summary>
        /// Adds stats from <paramref name="source"/> to saved session stats
        /// </summary>
        /// <param name="source">Stats to add</param>
        public void AssignMidGameSavedSessionStats(AdvancedGameStats source)
        {
            if (!this.IsInSession)
            {
                return;
            }
            if (this.m_savedSessionStats != null)
            {
                this.m_savedSessionStats.AddStats(source);
            }
        }

        /// <summary>
        /// Returns <see langword="true"/> if <see cref="AdvancedGameStatsManager"/> has an instance
        /// </summary>
        public static bool HasInstance
        {
            get
            {
                return m_instance != null;
            }
        }

        /// <summary>
        /// Returns the instance of <see cref="AdvancedGameStatsManager"/>
        /// </summary>
        public static AdvancedGameStatsManager Instance
        {
            get
            {
                return m_instance;
            }
        }

        private static AdvancedGameStatsManager m_instance;
        [fsProperty]
        public HashSet<CustomDungeonFlags> m_flags;
        [fsProperty]
        public string midGameSaveGuid;
        [fsProperty]
        public Dictionary<PlayableCharacters, AdvancedGameStats> m_characterStats;
        private AdvancedGameStats m_sessionStats;
        private AdvancedGameStats m_savedSessionStats;
        private PlayableCharacters m_sessionCharacter;
        private int m_numCharacters;
        [fsIgnore]
        public int cachedHuntIndex;
        [fsIgnore]
        public SaveManager.SaveSlot cachedSaveSlot;
    }
}
