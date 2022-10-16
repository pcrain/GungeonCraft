using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using UnityEngine;

namespace SaveAPI
{
    /// <summary>
    /// Misc tool methods for SaveAPI
    /// </summary>
    public static class SaveTools
    {
        /// <summary>
        /// <see langword="public"/> version of <see cref="SaveManager"/>.SafeMove
        /// </summary>
        /// <param name="oldPath">Old filepath</param>
        /// <param name="newPath">New filepath</param>
        /// <param name="allowOverwritting">If <see langword="true"/>, instead of not moving the file if a file on <paramref name="newPath"/> already exists it will overwrite the file on <paramref name="newPath"/></param>
        public static bool IsReallyCompleted(this MonsterHuntQuest quest)
        {
            bool allRewardsUnlocked = true;
            foreach (GungeonFlags flag in quest.FlagsToSetUponReward)
            {
                if (flag != GungeonFlags.NONE && !GameStatsManager.Instance.GetFlag(flag))
                {
                    allRewardsUnlocked = false;
                    break;
                }
            }
            if (quest is CustomHuntQuest custom)
            {
                if (allRewardsUnlocked)
                {
                    foreach (CustomDungeonFlags flag in custom.CustomFlagsToSetUponReward)
                    {
                        if (flag != CustomDungeonFlags.NONE && !AdvancedGameStatsManager.Instance.GetFlag(flag))
                        {
                            allRewardsUnlocked = false;
                            break;
                        }
                    }
                }
                bool anyQuestFlagCompleted = false;
                if (quest.QuestFlag != GungeonFlags.NONE)
                {
                    anyQuestFlagCompleted = GameStatsManager.Instance.GetFlag(quest.QuestFlag);
                }
                else if (custom.CustomQuestFlag != CustomDungeonFlags.NONE)
                {
                    anyQuestFlagCompleted = AdvancedGameStatsManager.Instance.GetFlag(custom.CustomQuestFlag);
                }
                return anyQuestFlagCompleted && allRewardsUnlocked;
            }
            return GameStatsManager.Instance.GetFlag(quest.QuestFlag) && allRewardsUnlocked;
        }
        public static void SafeMove(string oldPath, string newPath, bool allowOverwritting = false)
        {
            if (File.Exists(oldPath) && (allowOverwritting || !File.Exists(newPath)))
            {
                string contents = SaveManager.ReadAllText(oldPath);
                try
                {
                    SaveManager.WriteAllText(newPath, contents);
                }
                catch (Exception ex)
                {
                    Debug.LogErrorFormat("Failed to move {0} to {1}: {2}", new object[]
                    {
                    oldPath,
                    newPath,
                    ex
                    });
                    return;
                }
                try
                {
                    File.Delete(oldPath);
                }
                catch (Exception ex2)
                {
                    Debug.LogErrorFormat("Failed to delete old file {0}: {1}", new object[]
                    {
                    oldPath,
                    newPath,
                    ex2
                    });
                    return;
                }
                if (File.Exists(oldPath + ".bak"))
                {
                    File.Delete(oldPath + ".bak");
                }
            }
        }

        /// <summary>
        /// Converts a List{<typeparamref name="T"/>} to List{<typeparamref name="T2"/>} using <paramref name="convertor"/>
        /// </summary>
        /// <typeparam name="T">Element type of the source list</typeparam>
        /// <typeparam name="T2">Element type of the result list</typeparam>
        /// <param name="self">The source list.</param>
        /// <param name="convertor">Delegate that converts an object of type <typeparamref name="T"/> to an object of type <typeparamref name="T2"/></param>
        /// <returns>Converted list with element type of <typeparamref name="T2"/></returns>
        public static List<T2> Convert<T, T2>(this List<T> self, Func<T, T2> convertor)
        {
            List<T2> result = new List<T2>();
            foreach(T element in self)
            {
                result.Add(convertor(element));
            }
            return result;
        }

        /// <summary>
        /// Returns <see langword="true"/> if <paramref name="enemy"/> sets a custom flag on death.
        /// </summary>
        /// <param name="enemy">Target <see cref="AIActor"/></param>
        /// <returns><see langword="true"/> if <paramref name="enemy"/> sets a custom flag on death.</returns>
        public static bool SetsCustomFlagOnDeath(this AIActor enemy)
        {
            return enemy.GetComponent<SpecialAIActor>() != null && enemy.GetComponent<SpecialAIActor>().SetsCustomFlagOnDeath;
        }

        /// <summary>
        /// Gets the custom flag that will be set on <paramref name="enemy"/>'s death.
        /// </summary>
        /// <param name="enemy">Target <see cref="AIActor"/></param>
        /// <returns>Custom flag that will be set on <paramref name="enemy"/>'s death.</returns>
        public static CustomDungeonFlags GetCustomFlagToSetOnDeath(this AIActor enemy)
        {
            return (enemy.GetComponent<SpecialAIActor>() != null && enemy.GetComponent<SpecialAIActor>().SetsCustomFlagOnDeath) ? enemy.GetComponent<SpecialAIActor>().CustomFlagToSetOnDeath : CustomDungeonFlags.NONE;
        }

        /// <summary>
        /// Sets the custom flag that will be set on <paramref name="enemy"/>'s death to <paramref name="flag"/>.
        /// </summary>
        /// <param name="enemy">Target <see cref="AIActor"/></param>
        /// <param name="flag">New custom flag that will be set on <paramref name="enemy"/>'s death</param>
        public static void SetCustomFlagToSetOnDeath(this AIActor enemy, CustomDungeonFlags flag)
        {
            SpecialAIActor aiactor = enemy.gameObject.GetOrAddComponent<SpecialAIActor>();
            if(flag == CustomDungeonFlags.NONE)
            {
                aiactor.SetsCustomFlagOnDeath = false;
            }
            else if(flag != CustomDungeonFlags.NONE)
            {
                aiactor.SetsCustomFlagOnDeath = true;
            }
            aiactor.CustomFlagToSetOnDeath = flag;
        }

        /// <summary>
        /// Returns <see langword="true"/> if <paramref name="enemy"/> sets a custom flag on activation.
        /// </summary>
        /// <param name="enemy">Target <see cref="AIActor"/></param>
        /// <returns><see langword="true"/> if <paramref name="enemy"/> sets a custom flag on activation.</returns>
        public static bool SetsCustomFlagOnActivation(this AIActor enemy)
        {
            return enemy.GetComponent<SpecialAIActor>() != null && enemy.GetComponent<SpecialAIActor>().SetsCustomFlagOnActivation;
        }

        /// <summary>
        /// Gets the custom flag that will be set when <paramref name="enemy"/> is activated.
        /// </summary>
        /// <param name="enemy">Target <see cref="AIActor"/></param>
        /// <returns>Custom flag that will be set when <paramref name="enemy"/> is activated.</returns>
        public static CustomDungeonFlags GetCustomFlagToSetOnActivation(this AIActor enemy)
        {
            return (enemy.GetComponent<SpecialAIActor>() != null && enemy.GetComponent<SpecialAIActor>().SetsCustomFlagOnActivation) ? enemy.GetComponent<SpecialAIActor>().CustomFlagToSetOnActivation : CustomDungeonFlags.NONE;
        }

        /// <summary>
        /// Sets the custom flag that will be set when <paramref name="enemy"/> is activated to <paramref name="flag"/>.
        /// </summary>
        /// <param name="enemy">Target <see cref="AIActor"/></param>
        /// <param name="flag">New custom flag that will be set when <paramref name="enemy"/> is activated</param>
        public static void SetCustomFlagToSetOnActivation(this AIActor enemy, CustomDungeonFlags flag)
        {
            SpecialAIActor aiactor = enemy.gameObject.GetOrAddComponent<SpecialAIActor>();
            if (flag == CustomDungeonFlags.NONE)
            {
                aiactor.SetsCustomFlagOnActivation = false;
            }
            else if (flag != CustomDungeonFlags.NONE)
            {
                aiactor.SetsCustomFlagOnActivation = true;
            }
            aiactor.CustomFlagToSetOnActivation = flag;
        }

        /// <summary>
        /// Returns <see langword="true"/> if <paramref name="enemy"/> sets a custom character-specific flag on death.
        /// </summary>
        /// <param name="enemy">Target <see cref="AIActor"/></param>
        /// <returns><see langword="true"/> if <paramref name="enemy"/> sets a custom character-specific flag on death.</returns>
        public static bool SetsCustomCharacterSpecificFlagOnDeath(this AIActor enemy)
        {
            return enemy.GetComponent<SpecialAIActor>() != null && enemy.GetComponent<SpecialAIActor>().SetsCustomCharacterSpecificFlagOnDeath;
        }

        /// <summary>
        /// Gets the custom character-specific flag that will be set on <paramref name="enemy"/>'s death.
        /// </summary>
        /// <param name="enemy">Target <see cref="AIActor"/></param>
        /// <returns>Custom character-specific flag that will be set on <paramref name="enemy"/>'s death.</returns>
        public static CustomCharacterSpecificGungeonFlags GetCustomCharacterSpecificFlagToSetOnDeath(this AIActor enemy)
        {
            return (enemy.GetComponent<SpecialAIActor>() != null && enemy.GetComponent<SpecialAIActor>().SetsCustomCharacterSpecificFlagOnDeath) ? enemy.GetComponent<SpecialAIActor>().CustomCharacterSpecificFlagToSetOnDeath : 
                CustomCharacterSpecificGungeonFlags.NONE;
        }

        /// <summary>
        /// Sets the custom character-specific flag that will be set on <paramref name="enemy"/>'s death to <paramref name="flag"/>.
        /// </summary>
        /// <param name="enemy">Target <see cref="AIActor"/></param>
        /// <param name="flag">New custom character-specific flag that will be set on <paramref name="enemy"/>'s death</param>
        public static void SetCustomCharacterSpecificFlagToSetOnDeath(this AIActor enemy, CustomCharacterSpecificGungeonFlags flag)
        {
            SpecialAIActor aiactor = enemy.gameObject.GetOrAddComponent<SpecialAIActor>();
            if (flag == CustomCharacterSpecificGungeonFlags.NONE)
            {
                aiactor.SetsCustomCharacterSpecificFlagOnDeath = false;
            }
            else if (flag != CustomCharacterSpecificGungeonFlags.NONE)
            {
                aiactor.SetsCustomCharacterSpecificFlagOnDeath = true;
            }
            aiactor.CustomCharacterSpecificFlagToSetOnDeath = flag;
        }

        /// <summary>
        /// Combines three pathes
        /// </summary>
        /// <param name="a">First path</param>
        /// <param name="b">Second path</param>
        /// <param name="c">Third path</param>
        /// <returns></returns>
        public static string PathCombine(string a, string b, string c)
        {
            return Path.Combine(Path.Combine(a, b), c);
        }

        /// <summary>
        /// Setups a <see cref="DungeonPrerequisite"/> with the type of <see cref="DungeonPrerequisite.PrerequisiteType.FLAG"/>, flag of <paramref name="flag"/> and requiredFlagValue of 
        /// <paramref name="requiredFlagValue"/> and adds it to <paramref name="self"/>'s list of <see cref="DungeonPrerequisite"/>s
        /// </summary>
        /// <param name="self">The <see cref="PickupObject"/> to add the <see cref="DungeonPrerequisite"/> to</param>
        /// <param name="flag">The <see cref="GungeonFlags"/> to get the value from</param>
        /// <param name="requiredFlagValue">Value to compare <paramref name="flag"/>'s value to</param>
        /// <returns>The <see cref="DungeonPrerequisite"/> that was added to the list of <see cref="DungeonPrerequisite"/>s</returns>
        public static DungeonPrerequisite SetupUnlockOnFlag(this PickupObject self, GungeonFlags flag, bool requiredFlagValue)
        {
            if(self.encounterTrackable == null)
            {
                return null;
            }
            return self.encounterTrackable.SetupUnlockOnFlag(flag, requiredFlagValue);
        }

        /// <summary>
        /// Setups a <see cref="DungeonPrerequisite"/> with the type of <see cref="DungeonPrerequisite.PrerequisiteType.FLAG"/>, flag of <paramref name="flag"/> and requiredFlagValue of 
        /// <paramref name="requiredFlagValue"/> and adds it to <paramref name="self"/>'s list of <see cref="DungeonPrerequisite"/>s
        /// </summary>
        /// <param name="self">The <see cref="EncounterTrackable"/> to add the <see cref="DungeonPrerequisite"/> to</param>
        /// <param name="flag">The <see cref="GungeonFlags"/> to get the value from</param>
        /// <param name="requiredFlagValue">Value to compare <paramref name="flag"/>'s value to</param>
        /// <returns>The <see cref="DungeonPrerequisite"/> that was added to the list of <see cref="DungeonPrerequisite"/>s</returns>
        public static DungeonPrerequisite SetupUnlockOnFlag(this EncounterTrackable self, GungeonFlags flag, bool requiredFlagValue)
        {
            return self.AddPrerequisite(new DungeonPrerequisite 
            { 
                prerequisiteType = DungeonPrerequisite.PrerequisiteType.FLAG, 
                saveFlagToCheck = flag, 
                requireFlag = requiredFlagValue 
            });
        }

        /// <summary>
        /// Setups a <see cref="DungeonPrerequisite"/> with the type of <see cref="DungeonPrerequisite.PrerequisiteType.COMPARISON"/>, prerequisiteOperation of <paramref name="comparisonOperation"/> and comparisonValue of 
        /// <paramref name="comparisonValue"/> and adds it to <paramref name="self"/>'s list of <see cref="DungeonPrerequisite"/>s
        /// </summary>
        /// <param name="self">The <see cref="PickupObject"/> to add the <see cref="DungeonPrerequisite"/> to</param>
        /// <param name="stat">The <see cref="TrackedStats"/> to get the value from</param>
        /// <param name="comparisonValue">The value to compare <paramref name="stat"/>'s value to</param>
        /// <param name="comparisonOperation">The comparison operation</param>
        /// <returns>The <see cref="DungeonPrerequisite"/> that was added to the list of <see cref="DungeonPrerequisite"/>s</returns>
        public static DungeonPrerequisite SetupUnlockOnStat(this PickupObject self, TrackedStats stat, float comparisonValue, DungeonPrerequisite.PrerequisiteOperation comparisonOperation)
        {
            if (self.encounterTrackable == null)
            {
                return null;
            }
            return self.encounterTrackable.SetupUnlockOnStat(stat, comparisonValue, comparisonOperation);
        }

        /// <summary>
        /// Setups a <see cref="DungeonPrerequisite"/> with the type of <see cref="DungeonPrerequisite.PrerequisiteType.COMPARISON"/>, prerequisiteOperation of <paramref name="comparisonOperation"/> and comparisonValue of 
        /// <paramref name="comparisonValue"/> and adds it to <paramref name="self"/>'s list of <see cref="DungeonPrerequisite"/>s
        /// </summary>
        /// <param name="self">The <see cref="EncounterTrackable"/> to add the <see cref="DungeonPrerequisite"/> to</param>
        /// <param name="stat">The <see cref="TrackedStats"/> to get the value from</param>
        /// <param name="comparisonValue">The value to compare <paramref name="stat"/>'s value to</param>
        /// <param name="comparisonOperation">The comparison operation</param>
        /// <returns>The <see cref="DungeonPrerequisite"/> that was added to the list of <see cref="DungeonPrerequisite"/>s</returns>
        public static DungeonPrerequisite SetupUnlockOnStat(this EncounterTrackable self, TrackedStats stat, float comparisonValue, DungeonPrerequisite.PrerequisiteOperation comparisonOperation)
        {
            return self.AddPrerequisite(new DungeonPrerequisite 
            { 
                prerequisiteType = DungeonPrerequisite.PrerequisiteType.COMPARISON, 
                statToCheck = stat, 
                prerequisiteOperation = comparisonOperation, 
                comparisonValue = comparisonValue 
            });
        }

        /// <summary>
        /// Setups a <see cref="DungeonPrerequisite"/> with the type of <see cref="DungeonPrerequisite.PrerequisiteType.MAXIMUM_COMPARISON"/>, prerequisiteOperation of <paramref name="comparisonOperation"/> and comparisonValue of 
        /// <paramref name="comparisonValue"/> and adds it to <paramref name="self"/>'s list of <see cref="DungeonPrerequisite"/>s
        /// </summary>
        /// <param name="self">The <see cref="PickupObject"/> to add the <see cref="DungeonPrerequisite"/> to</param>
        /// <param name="maximum">The <see cref="TrackedMaximums"/> to get the value from</param>
        /// <param name="comparisonValue">The value to compare <paramref name="maximum"/>'s value to</param>
        /// <param name="comparisonOperation">The comparison operation</param>
        /// <returns>The <see cref="DungeonPrerequisite"/> that was added to the list of <see cref="DungeonPrerequisite"/>s</returns>
        public static DungeonPrerequisite SetupUnlockOnMaximum(this PickupObject self, TrackedMaximums maximum, float comparisonValue, DungeonPrerequisite.PrerequisiteOperation comparisonOperation)
        {
            if (self.encounterTrackable == null)
            {
                return null;
            }
            return self.encounterTrackable.SetupUnlockOnMaximum(maximum, comparisonValue, comparisonOperation);
        }

        /// <summary>
        /// Setups a <see cref="DungeonPrerequisite"/> with the type of <see cref="DungeonPrerequisite.PrerequisiteType.MAXIMUM_COMPARISON"/>, prerequisiteOperation of <paramref name="comparisonOperation"/> and comparisonValue of 
        /// <paramref name="comparisonValue"/> and adds it to <paramref name="self"/>'s list of <see cref="DungeonPrerequisite"/>s
        /// </summary>
        /// <param name="self">The <see cref="EncounterTrackable"/> to add the <see cref="DungeonPrerequisite"/> to</param>
        /// <param name="maximum">The <see cref="TrackedMaximums"/> to get the value from</param>
        /// <param name="comparisonValue">The value to compare <paramref name="maximum"/>'s value to</param>
        /// <param name="comparisonOperation">The comparison operation</param>
        /// <returns>The <see cref="DungeonPrerequisite"/> that was added to the list of <see cref="DungeonPrerequisite"/>s</returns>
        public static DungeonPrerequisite SetupUnlockOnMaximum(this EncounterTrackable self, TrackedMaximums maximum, float comparisonValue, DungeonPrerequisite.PrerequisiteOperation comparisonOperation)
        {
            return self.AddPrerequisite(new DungeonPrerequisite 
            { 
                prerequisiteType = DungeonPrerequisite.PrerequisiteType.MAXIMUM_COMPARISON,
                maxToCheck = maximum,
                prerequisiteOperation = comparisonOperation,
                comparisonValue = comparisonValue
            });
        }

        /// <summary>
        /// Setups a <see cref="DungeonPrerequisite"/> with the type of <see cref="DungeonPrerequisite.PrerequisiteType.ENCOUNTER"/>, encounteredObjectGuid of <paramref name="encounterObjectGuid"/>, requiredNumberOfEncounters of 
        /// <paramref name="requiredNumberOfEncounters"/> and comparisonOperation of <paramref name="comparisonOperation"/> and adds it to <paramref name="self"/>'s list of <see cref="DungeonPrerequisite"/>s
        /// </summary>
        /// <param name="self">The <see cref="PickupObject"/> to add the <see cref="DungeonPrerequisite"/> to</param>
        /// <param name="encounterObjectGuid">The GUID of the object the player needs to encounter</param>
        /// <param name="requiredNumberOfEncounters">The value to compare the amount of times the player encountered the object with <paramref name="encounterObjectGuid"/> GUID to</param>
        /// <param name="comparisonOperation">Comparison operation</param>
        /// <returns>The <see cref="DungeonPrerequisite"/> that was added to the list of <see cref="DungeonPrerequisite"/>s</returns>
        public static DungeonPrerequisite SetupUnlockOnEncounter(this PickupObject self, string encounterObjectGuid, int requiredNumberOfEncounters, DungeonPrerequisite.PrerequisiteOperation comparisonOperation)
        {
            if (self.encounterTrackable == null)
            {
                return null;
            }
            return self.encounterTrackable.SetupUnlockOnEncounter(encounterObjectGuid, requiredNumberOfEncounters, comparisonOperation);
        }

        /// <summary>
        /// Setups a <see cref="DungeonPrerequisite"/> with the type of <see cref="DungeonPrerequisite.PrerequisiteType.ENCOUNTER"/>, encounteredObjectGuid of <paramref name="encounterObjectGuid"/>, requiredNumberOfEncounters of 
        /// <paramref name="requiredNumberOfEncounters"/> and comparisonOperation of <paramref name="comparisonOperation"/> and adds it to <paramref name="self"/>'s list of <see cref="DungeonPrerequisite"/>s
        /// </summary>
        /// <param name="self">The <see cref="EncounterTrackable"/> to add the <see cref="DungeonPrerequisite"/> to</param>
        /// <param name="encounterObjectGuid">The GUID of the object the player needs to encounter</param>
        /// <param name="requiredNumberOfEncounters">The value to compare the amount of times the player encountered the object with <paramref name="encounterObjectGuid"/> GUID to</param>
        /// <param name="comparisonOperation">Comparison operation</param>
        /// <returns>The <see cref="DungeonPrerequisite"/> that was added to the list of <see cref="DungeonPrerequisite"/>s</returns>
        public static DungeonPrerequisite SetupUnlockOnEncounter(this EncounterTrackable self, string encounterObjectGuid, int requiredNumberOfEncounters, DungeonPrerequisite.PrerequisiteOperation comparisonOperation)
        {
            return self.AddPrerequisite(new DungeonPrerequisite
            {
                prerequisiteType = DungeonPrerequisite.PrerequisiteType.ENCOUNTER,
                encounteredObjectGuid = encounterObjectGuid,
                requiredNumberOfEncounters = requiredNumberOfEncounters,
                prerequisiteOperation = comparisonOperation
            });
        }

        /// <summary>
        /// Setups a <see cref="DungeonPrerequisite"/> with the type of <see cref="DungeonPrerequisite.PrerequisiteType.ENCOUNTER"/>, encounteredRoom of <paramref name="encounterRoom"/>, requiredNumberOfEncounters of <paramref name="requiredNumberOfEncounters"/> and comparisonOperation of <paramref name="comparisonOperation"/> and adds it 
        /// to <paramref name="self"/>'s list of <see cref="DungeonPrerequisite"/>s
        /// </summary>
        /// <param name="self">The <see cref="PickupObject"/> to add the <see cref="DungeonPrerequisite"/> to</param>
        /// <param name="encounterRoom">The room the player needs to encounter</param>
        /// <param name="requiredNumberOfEncounters">The value to compare the amount of times the player <paramref name="encounterRoom"/> to</param>
        /// <param name="comparisonOperation">Comparison operation</param>
        /// <returns>The <see cref="DungeonPrerequisite"/> that was added to the list of <see cref="DungeonPrerequisite"/>s</returns>
        public static DungeonPrerequisite SetupUnlockOnEncounter(this PickupObject self, PrototypeDungeonRoom encounterRoom, int requiredNumberOfEncounters, DungeonPrerequisite.PrerequisiteOperation comparisonOperation)
        {
            if (self.encounterTrackable == null)
            {
                return null;
            }
            return self.encounterTrackable.SetupUnlockOnEncounter(encounterRoom, requiredNumberOfEncounters, comparisonOperation);
        }

        /// <summary>
        /// Setups a <see cref="DungeonPrerequisite"/> with the type of <see cref="DungeonPrerequisite.PrerequisiteType.ENCOUNTER"/>, encounteredRoom of <paramref name="encounterRoom"/>, requiredNumberOfEncounters of <paramref name="requiredNumberOfEncounters"/> and comparisonOperation of <paramref name="comparisonOperation"/> and adds it 
        /// to <paramref name="self"/>'s list of <see cref="DungeonPrerequisite"/>s
        /// </summary>
        /// <param name="self">The <see cref="EncounterTrackable"/> to add the <see cref="DungeonPrerequisite"/> to</param>
        /// <param name="encounterRoom">The room the player needs to encounter</param>
        /// <param name="requiredNumberOfEncounters">The value to compare the amount of times the player encountered <paramref name="encounterRoom"/> to</param>
        /// <param name="comparisonOperation">Comparison operation</param>
        /// <returns>The <see cref="DungeonPrerequisite"/> that was added to the list of <see cref="DungeonPrerequisite"/>s</returns>
        public static DungeonPrerequisite SetupUnlockOnEncounter(this EncounterTrackable self, PrototypeDungeonRoom encounterRoom, int requiredNumberOfEncounters, DungeonPrerequisite.PrerequisiteOperation comparisonOperation)
        {
            return self.AddPrerequisite(new DungeonPrerequisite
            {
                prerequisiteType = DungeonPrerequisite.PrerequisiteType.ENCOUNTER,
                encounteredRoom = encounterRoom,
                requiredNumberOfEncounters = requiredNumberOfEncounters,
                prerequisiteOperation = comparisonOperation
            });
        }

        /// <summary>
        /// Setups a <see cref="DungeonPrerequisite"/> with the type of <see cref="DungeonPrerequisite.PrerequisiteType.ENCOUNTER_OR_CUSTOM_FLAG"/>, flag of <paramref name="flag"/>, requiredFlagValue of 
        /// <paramref name="requiredFlagValue"/>, encounteredObjectGuid of <paramref name="encounterObjectGuid"/>, requiredNumberOfEncounters of <paramref name="requiredNumberOfEncounters"/> and comparisonOperation of <paramref name="comparisonOperation"/> and adds it 
        /// to <paramref name="self"/>'s list of <see cref="DungeonPrerequisite"/>s
        /// </summary>
        /// <param name="self">The <see cref="PickupObject"/> to add the <see cref="DungeonPrerequisite"/> to</param>
        /// <param name="flag">The <see cref="GungeonFlags"/> to get the value from</param>
        /// <param name="requiredFlagValue">Value to compare <paramref name="flag"/>'s value to</param>
        /// <param name="encounterObjectGuid">The GUID of the object the player needs to encounter</param>
        /// <param name="requiredNumberOfEncounters">The value to compare the amount of times the player encountered the object with <paramref name="encounterObjectGuid"/> GUID to</param>
        /// <param name="comparisonOperation">Comparison operation</param>
        /// <returns>The <see cref="DungeonPrerequisite"/> that was added to the list of <see cref="DungeonPrerequisite"/>s</returns>
        public static DungeonPrerequisite SetupUnlockOnEncounterOrFlag(this PickupObject self, GungeonFlags flag, bool requiredFlagValue, string encounterObjectGuid, int requiredNumberOfEncounters, 
            DungeonPrerequisite.PrerequisiteOperation comparisonOperation)
        {
            if (self.encounterTrackable == null)
            {
                return null;
            }
            return self.encounterTrackable.SetupUnlockOnEncounterOrFlag(flag, requiredFlagValue, encounterObjectGuid, requiredNumberOfEncounters, comparisonOperation);
        }

        /// <summary>
        /// Setups a <see cref="DungeonPrerequisite"/> with the type of <see cref="DungeonPrerequisite.PrerequisiteType.ENCOUNTER_OR_CUSTOM_FLAG"/>, flag of <paramref name="flag"/>, requiredFlagValue of 
        /// <paramref name="requiredFlagValue"/>, encounteredObjectGuid of <paramref name="encounterObjectGuid"/>, requiredNumberOfEncounters of <paramref name="requiredNumberOfEncounters"/> and comparisonOperation of <paramref name="comparisonOperation"/> and adds it 
        /// to <paramref name="self"/>'s list of <see cref="DungeonPrerequisite"/>s
        /// </summary>
        /// <param name="self">The <see cref="EncounterTrackable"/> to add the <see cref="DungeonPrerequisite"/> to</param>
        /// <param name="flag">The <see cref="GungeonFlags"/> to get the value from</param>
        /// <param name="requiredFlagValue">Value to compare <paramref name="flag"/>'s value to</param>
        /// <param name="encounterObjectGuid">The GUID of the object the player needs to encounter</param>
        /// <param name="requiredNumberOfEncounters">The value to compare the amount of times the player encountered the object with <paramref name="encounterObjectGuid"/> GUID to</param>
        /// <param name="comparisonOperation">Comparison operation</param>
        /// <returns>The <see cref="DungeonPrerequisite"/> that was added to the list of <see cref="DungeonPrerequisite"/>s</returns>
        public static DungeonPrerequisite SetupUnlockOnEncounterOrFlag(this EncounterTrackable self, GungeonFlags flag, bool requiredFlagValue, string encounterObjectGuid, int requiredNumberOfEncounters, 
            DungeonPrerequisite.PrerequisiteOperation comparisonOperation)
        {
            return self.AddPrerequisite(new DungeonPrerequisite
            {
                prerequisiteType = DungeonPrerequisite.PrerequisiteType.ENCOUNTER_OR_FLAG,
                saveFlagToCheck = flag,
                requireFlag = requiredFlagValue,
                encounteredObjectGuid = encounterObjectGuid,
                requiredNumberOfEncounters = requiredNumberOfEncounters,
                prerequisiteOperation = comparisonOperation
            });
        }

        /// <summary>
        /// Setups a <see cref="DungeonPrerequisite"/> with the type of <see cref="DungeonPrerequisite.PrerequisiteType.ENCOUNTER_OR_CUSTOM_FLAG"/>, flag of <paramref name="flag"/>, requiredFlagValue of 
        /// <paramref name="requiredFlagValue"/>, encounterRoom of <paramref name="encounterRoom"/>, requiredNumberOfEncounters of <paramref name="requiredNumberOfEncounters"/> and comparisonOperation of <paramref name="comparisonOperation"/> and adds it 
        /// to <paramref name="self"/>'s list of <see cref="DungeonPrerequisite"/>s
        /// </summary>
        /// <param name="self">The <see cref="PickupObject"/> to add the <see cref="DungeonPrerequisite"/> to</param>
        /// <param name="flag">The <see cref="GungeonFlags"/> to get the value from</param>
        /// <param name="requiredFlagValue">Value to compare <paramref name="flag"/>'s value to</param>
        /// <param name="encounterRoom">The room the player needs to encounter</param>
        /// <param name="requiredNumberOfEncounters">The value to compare the amount of times the player encountered <paramref name="encounterRoom"/> to</param>
        /// <param name="comparisonOperation">Comparison operation</param>
        /// <returns>The <see cref="DungeonPrerequisite"/> that was added to the list of <see cref="DungeonPrerequisite"/>s</returns>
        public static DungeonPrerequisite SetupUnlockOnEncounterOrFlag(this PickupObject self, GungeonFlags flag, bool requiredFlagValue, PrototypeDungeonRoom encounterRoom, int requiredNumberOfEncounters,
            DungeonPrerequisite.PrerequisiteOperation comparisonOperation)
        {
            if (self.encounterTrackable == null)
            {
                return null;
            }
            return self.encounterTrackable.SetupUnlockOnEncounterOrFlag(flag, requiredFlagValue, encounterRoom, requiredNumberOfEncounters, comparisonOperation);
        }

        /// <summary>
        /// Setups a <see cref="DungeonPrerequisite"/> with the type of <see cref="DungeonPrerequisite.PrerequisiteType.ENCOUNTER_OR_CUSTOM_FLAG"/>, flag of <paramref name="flag"/>, requiredFlagValue of 
        /// <paramref name="requiredFlagValue"/>, encounterRoom of <paramref name="encounterRoom"/>, requiredNumberOfEncounters of <paramref name="requiredNumberOfEncounters"/> and comparisonOperation of <paramref name="comparisonOperation"/> and adds it 
        /// to <paramref name="self"/>'s list of <see cref="DungeonPrerequisite"/>s
        /// </summary>
        /// <param name="self">The <see cref="EncounterTrackable"/> to add the <see cref="DungeonPrerequisite"/> to</param>
        /// <param name="flag">The <see cref="GungeonFlags"/> to get the value from</param>
        /// <param name="requiredFlagValue">Value to compare <paramref name="flag"/>'s value to</param>
        /// <param name="encounterRoom">The room the player needs to encounter</param>
        /// <param name="requiredNumberOfEncounters">The value to compare the amount of times the player encountered <paramref name="encounterRoom"/> to</param>
        /// <param name="comparisonOperation">Comparison operation</param>
        /// <returns>The <see cref="DungeonPrerequisite"/> that was added to the list of <see cref="DungeonPrerequisite"/>s</returns>
        public static DungeonPrerequisite SetupUnlockOnEncounterOrFlag(this EncounterTrackable self, GungeonFlags flag, bool requiredFlagValue, PrototypeDungeonRoom encounterRoom, int requiredNumberOfEncounters, 
            DungeonPrerequisite.PrerequisiteOperation comparisonOperation)
        {
            return self.AddPrerequisite(new DungeonPrerequisite
            {
                prerequisiteType = DungeonPrerequisite.PrerequisiteType.ENCOUNTER_OR_FLAG,
                saveFlagToCheck = flag,
                requireFlag = requiredFlagValue,
                encounteredRoom = encounterRoom,
                requiredNumberOfEncounters = requiredNumberOfEncounters,
                prerequisiteOperation = comparisonOperation
            });
        }

        /// <summary>
        /// Setups a <see cref="DungeonPrerequisite"/> with the type of <see cref="DungeonPrerequisite.PrerequisiteType.TILESET"/>, requireTileset of <paramref name="requiredTilesetValue"/> and requiredTileset of 
        /// <paramref name="requiredTileset"/> and adds it to <paramref name="self"/>'s list of <see cref="DungeonPrerequisite"/>s
        /// </summary>
        /// <param name="self">The <see cref="PickupObject"/> to add the <see cref="DungeonPrerequisite"/> to</param>
        /// <param name="requiredTileset">The tileset to check</param>
        /// <param name="requiredTilesetValue">If <see langword="true"/>, the conditions will be fulfilled if the current tileset is <paramref name="requiredTilesetValue"/>, if <see langword="false"/>, the conditions will be fulfilled if the current
        /// tileset isn't <paramref name="requiredTilesetValue"/></param>
        /// <returns>The <see cref="DungeonPrerequisite"/> that was added to the list of <see cref="DungeonPrerequisite"/>s</returns>
        public static DungeonPrerequisite SetupUnlockOnTileset(this PickupObject self, GlobalDungeonData.ValidTilesets requiredTileset, bool requiredTilesetValue)
        {
            if (self.encounterTrackable == null)
            {
                return null;
            }
            return self.encounterTrackable.SetupUnlockOnTileset(requiredTileset, requiredTilesetValue);
        }

        /// <summary>
        /// Setups a <see cref="DungeonPrerequisite"/> with the type of <see cref="DungeonPrerequisite.PrerequisiteType.CHARACTER"/>, requireTileset of <paramref name="requiredTilesetValue"/> and requiredTileset of 
        /// <paramref name="requiredTileset"/> and adds it to <paramref name="self"/>'s list of <see cref="DungeonPrerequisite"/>s
        /// </summary>
        /// <param name="self">The <see cref="EncounterTrackable"/> to add the <see cref="DungeonPrerequisite"/> to</param>
        /// <param name="requiredTileset">The tileset to check</param>
        /// <param name="requiredTilesetValue">If <see langword="true"/>, the conditions will be fulfilled if the current tileset is <paramref name="requiredTilesetValue"/>, if <see langword="false"/>, the conditions will be fulfilled if the current
        /// tileset isn't <paramref name="requiredTilesetValue"/></param>
        /// <returns>The <see cref="DungeonPrerequisite"/> that was added to the list of <see cref="DungeonPrerequisite"/>s</returns>
        public static DungeonPrerequisite SetupUnlockOnTileset(this EncounterTrackable self, GlobalDungeonData.ValidTilesets requiredTileset, bool requiredTilesetValue)
        {
            return self.AddPrerequisite(new DungeonPrerequisite
            {
                prerequisiteType = DungeonPrerequisite.PrerequisiteType.TILESET,
                requireTileset = requiredTilesetValue,
                requiredTileset = requiredTileset
            });
        }

        /// <summary>
        /// Setups a <see cref="DungeonPrerequisite"/> with the type of <see cref="DungeonPrerequisite.PrerequisiteType.CHARACTER"/>, requireCharacter of <paramref name="requiredCharacterValue"/> and requiredCharacter of 
        /// <paramref name="requiredCharacter"/> and adds it to <paramref name="self"/>'s list of <see cref="DungeonPrerequisite"/>s
        /// </summary>
        /// <param name="self">The <see cref="PickupObject"/> to add the <see cref="DungeonPrerequisite"/> to</param>
        /// <param name="requiredCharacter">The character to check</param>
        /// <param name="requiredCharacterValue">If <see langword="true"/>, the conditions will be fulfilled if the current character is <paramref name="requiredCharacter"/>, if <see langword="false"/>, the conditions will be fulfilled if the current
        /// character isn't <paramref name="requiredCharacter"/></param>
        /// <returns>The <see cref="DungeonPrerequisite"/> that was added to the list of <see cref="DungeonPrerequisite"/>s</returns>
        public static DungeonPrerequisite SetupUnlockOnCharacter(this PickupObject self, PlayableCharacters requiredCharacter, bool requiredCharacterValue)
        {
            if (self.encounterTrackable == null)
            {
                return null;
            }
            return self.encounterTrackable.SetupUnlockOnCharacter(requiredCharacter, requiredCharacterValue);
        }

        /// <summary>
        /// Setups a <see cref="DungeonPrerequisite"/> with the type of <see cref="DungeonPrerequisite.PrerequisiteType.CHARACTER"/>, requireCharacter of <paramref name="requiredCharacterValue"/> and requiredCharacter of 
        /// <paramref name="requiredCharacter"/> and adds it to <paramref name="self"/>'s list of <see cref="DungeonPrerequisite"/>s
        /// </summary>
        /// <param name="self">The <see cref="EncounterTrackable"/> to add the <see cref="DungeonPrerequisite"/> to</param>
        /// <param name="requiredCharacter">The character to check</param>
        /// <param name="requiredCharacterValue">If <see langword="true"/>, the conditions will be fulfilled if the current character is <paramref name="requiredCharacter"/>, if <see langword="false"/>, the conditions will be fulfilled if the current
        /// character isn't <paramref name="requiredCharacter"/></param>
        /// <returns>The <see cref="DungeonPrerequisite"/> that was added to the list of <see cref="DungeonPrerequisite"/>s</returns>
        public static DungeonPrerequisite SetupUnlockOnCharacter(this EncounterTrackable self, PlayableCharacters requiredCharacter, bool requiredCharacterValue)
        {
            return self.AddPrerequisite(new DungeonPrerequisite
            {
                prerequisiteType = DungeonPrerequisite.PrerequisiteType.CHARACTER,
                requireCharacter = requiredCharacterValue,
                requiredCharacter = requiredCharacter
            });
        }

        /// <summary>
        /// Setups a <see cref="DungeonPrerequisite"/> with the type of <see cref="CustomDungeonPrerequisite.AdvancedPrerequisiteType.ENCOUNTER_OR_CUSTOM_FLAG"/>, flag of <paramref name="flag"/>, requiredFlagValue of 
        /// <paramref name="requiredFlagValue"/>, encounteredObjectGuid of <paramref name="encounterObjectGuid"/>, requiredNumberOfEncounters of <paramref name="requiredNumberOfEncounters"/> and comparisonOperation of <paramref name="comparisonOperation"/> and adds it 
        /// to <paramref name="self"/>'s list of <see cref="DungeonPrerequisite"/>s
        /// </summary>
        /// <param name="self">The <see cref="PickupObject"/> to add the <see cref="DungeonPrerequisite"/> to</param>
        /// <param name="flag">The <see cref="CustomDungeonFlags"/> to get the value from</param>
        /// <param name="requiredFlagValue">Value to compare <paramref name="flag"/>'s value to</param>
        /// <param name="encounterObjectGuid">The GUID of the object the player needs to encounter</param>
        /// <param name="requiredNumberOfEncounters">The value to compare the amount of times the player encountered the object with <paramref name="encounterObjectGuid"/> GUID to</param>
        /// <param name="comparisonOperation">Comparison operation</param>
        /// <returns>The <see cref="DungeonPrerequisite"/> that was added to the list of <see cref="DungeonPrerequisite"/>s</returns>
        public static DungeonPrerequisite SetupUnlockOnEncounterOrCustomFlag(this PickupObject self, CustomDungeonFlags flag, bool requiredFlagValue, string encounterObjectGuid, int requiredNumberOfEncounters,
            DungeonPrerequisite.PrerequisiteOperation comparisonOperation)
        {
            if (self.encounterTrackable == null)
            {
                return null;
            }
            return self.encounterTrackable.SetupUnlockOnEncounterOrCustomFlag(flag, requiredFlagValue, encounterObjectGuid, requiredNumberOfEncounters, comparisonOperation);
        }

        /// <summary>
        /// Setups a <see cref="DungeonPrerequisite"/> with the type of <see cref="CustomDungeonPrerequisite.AdvancedPrerequisiteType.ENCOUNTER_OR_CUSTOM_FLAG"/>, flag of <paramref name="flag"/>, requiredFlagValue of 
        /// <paramref name="requiredFlagValue"/>, encounteredObjectGuid of <paramref name="encounterObjectGuid"/>, requiredNumberOfEncounters of <paramref name="requiredNumberOfEncounters"/> and comparisonOperation of <paramref name="comparisonOperation"/> and adds it 
        /// to <paramref name="self"/>'s list of <see cref="DungeonPrerequisite"/>s
        /// </summary>
        /// <param name="self">The <see cref="EncounterTrackable"/> to add the <see cref="DungeonPrerequisite"/> to</param>
        /// <param name="flag">The <see cref="CustomDungeonFlags"/> to get the value from</param>
        /// <param name="requiredFlagValue">Value to compare <paramref name="flag"/>'s value to</param>
        /// <param name="encounterObjectGuid">The GUID of the object the player needs to encounter</param>
        /// <param name="requiredNumberOfEncounters">The value to compare the amount of times the player encountered the object with <paramref name="encounterObjectGuid"/> GUID to</param>
        /// <param name="comparisonOperation">Comparison operation</param>
        /// <returns>The <see cref="DungeonPrerequisite"/> that was added to the list of <see cref="DungeonPrerequisite"/>s</returns>
        public static DungeonPrerequisite SetupUnlockOnEncounterOrCustomFlag(this EncounterTrackable self, CustomDungeonFlags flag, bool requiredFlagValue, string encounterObjectGuid, int requiredNumberOfEncounters,
            DungeonPrerequisite.PrerequisiteOperation comparisonOperation)
        {
            return self.AddPrerequisite(new CustomDungeonPrerequisite
            {
                advancedPrerequisiteType = CustomDungeonPrerequisite.AdvancedPrerequisiteType.ENCOUNTER_OR_CUSTOM_FLAG,
                customFlagToCheck = flag,
                requireCustomFlag = requiredFlagValue,
                encounteredObjectGuid = encounterObjectGuid,
                requiredNumberOfEncounters = requiredNumberOfEncounters,
                prerequisiteOperation = comparisonOperation
            });
        }

        /// <summary>
        /// Setups a <see cref="DungeonPrerequisite"/> with the type of <see cref="CustomDungeonPrerequisite.AdvancedPrerequisiteType.ENCOUNTER_OR_CUSTOM_FLAG"/>, flag of <paramref name="flag"/>, requiredFlagValue of 
        /// <paramref name="requiredFlagValue"/>, encounterRoom of <paramref name="encounterRoom"/>, requiredNumberOfEncounters of <paramref name="requiredNumberOfEncounters"/> and comparisonOperation of <paramref name="comparisonOperation"/> and adds it 
        /// to <paramref name="self"/>'s list of <see cref="DungeonPrerequisite"/>s
        /// </summary>
        /// <param name="self">The <see cref="PickupObject"/> to add the <see cref="DungeonPrerequisite"/> to</param>
        /// <param name="flag">The <see cref="CustomDungeonFlags"/> to get the value from</param>
        /// <param name="requiredFlagValue">Value to compare <paramref name="flag"/>'s value to</param>
        /// <param name="encounterRoom">The room the player needs to encounter</param>
        /// <param name="requiredNumberOfEncounters">The value to compare the amount of times the player encountered <paramref name="encounterRoom"/> to</param>
        /// <param name="comparisonOperation">Comparison operation</param>
        /// <returns>The <see cref="DungeonPrerequisite"/> that was added to the list of <see cref="DungeonPrerequisite"/>s</returns>
        public static DungeonPrerequisite SetupUnlockOnEncounterOrCustomFlag(this PickupObject self, CustomDungeonFlags flag, bool requiredFlagValue, PrototypeDungeonRoom encounterRoom, int requiredNumberOfEncounters,
            DungeonPrerequisite.PrerequisiteOperation comparisonOperation)
        {
            if (self.encounterTrackable == null)
            {
                return null;
            }
            return self.encounterTrackable.SetupUnlockOnEncounterOrCustomFlag(flag, requiredFlagValue, encounterRoom, requiredNumberOfEncounters, comparisonOperation);
        }

        /// <summary>
        /// Setups a <see cref="DungeonPrerequisite"/> with the type of <see cref="CustomDungeonPrerequisite.AdvancedPrerequisiteType.ENCOUNTER_OR_CUSTOM_FLAG"/>, flag of <paramref name="flag"/>, requiredFlagValue of 
        /// <paramref name="requiredFlagValue"/>, encounterRoom of <paramref name="encounterRoom"/>, requiredNumberOfEncounters of <paramref name="requiredNumberOfEncounters"/> and comparisonOperation of <paramref name="comparisonOperation"/> and adds it 
        /// to <paramref name="self"/>'s list of <see cref="DungeonPrerequisite"/>s
        /// </summary>
        /// <param name="self">The <see cref="EncounterTrackable"/> to add the <see cref="DungeonPrerequisite"/> to</param>
        /// <param name="flag">The <see cref="CustomDungeonFlags"/> to get the value from</param>
        /// <param name="requiredFlagValue">Value to compare <paramref name="flag"/>'s value to</param>
        /// <param name="encounterRoom">The room the player needs to encounter</param>
        /// <param name="requiredNumberOfEncounters">The value to compare the amount of times the player encountered <paramref name="encounterRoom"/> to</param>
        /// <param name="comparisonOperation">Comparison operation</param>
        /// <returns>The <see cref="DungeonPrerequisite"/> that was added to the list of <see cref="DungeonPrerequisite"/>s</returns>
        public static DungeonPrerequisite SetupUnlockOnEncounterOrCustomFlag(this EncounterTrackable self, CustomDungeonFlags flag, bool requiredFlagValue, PrototypeDungeonRoom encounterRoom, int requiredNumberOfEncounters,
            DungeonPrerequisite.PrerequisiteOperation comparisonOperation)
        {
            return self.AddPrerequisite(new CustomDungeonPrerequisite
            {
                advancedPrerequisiteType = CustomDungeonPrerequisite.AdvancedPrerequisiteType.ENCOUNTER_OR_CUSTOM_FLAG,
                customFlagToCheck = flag,
                requireCustomFlag = requiredFlagValue,
                encounteredRoom = encounterRoom,
                requiredNumberOfEncounters = requiredNumberOfEncounters,
                prerequisiteOperation = comparisonOperation
            });
        }

        /// <summary>
        /// Setups a <see cref="DungeonPrerequisite"/> with the type of <see cref="CustomDungeonPrerequisite.AdvancedPrerequisiteType.CUSTOM_FLAG"/>, flag of <paramref name="flag"/> and requiredFlagValue of 
        /// <paramref name="requiredFlagValue"/> and adds it to <paramref name="self"/>'s list of <see cref="DungeonPrerequisite"/>s
        /// </summary>
        /// <param name="self">The <see cref="PickupObject"/> to add the <see cref="DungeonPrerequisite"/> to</param>
        /// <param name="flag">The <see cref="CustomDungeonFlags"/> to get the value from</param>
        /// <param name="requiredFlagValue">Value to compare <paramref name="flag"/>'s value to</param>
        /// <returns>The <see cref="DungeonPrerequisite"/> that was added to the list of <see cref="DungeonPrerequisite"/>s</returns>
        public static DungeonPrerequisite SetupUnlockOnCustomFlag(this PickupObject self, CustomDungeonFlags flag, bool requiredFlagValue)
        {
            if (self.encounterTrackable == null)
            {
                return null;
            }
            return self.encounterTrackable.SetupUnlockOnCustomFlag(flag, requiredFlagValue);
        }

        /// <summary>
        /// Setups a <see cref="DungeonPrerequisite"/> with the type of <see cref="CustomDungeonPrerequisite.AdvancedPrerequisiteType.CUSTOM_FLAG"/>, flag of <paramref name="flag"/> and requiredFlagValue of 
        /// <paramref name="requiredFlagValue"/> and adds it to <paramref name="self"/>'s list of <see cref="DungeonPrerequisite"/>s
        /// </summary>
        /// <param name="self">The <see cref="EncounterTrackable"/> to add the <see cref="DungeonPrerequisite"/> to</param>
        /// <param name="flag">The <see cref="CustomDungeonFlags"/> to get the value from</param>
        /// <param name="requiredFlagValue">Value to compare <paramref name="flag"/>'s value to</param>
        /// <returns>The <see cref="DungeonPrerequisite"/> that was added to the list of <see cref="DungeonPrerequisite"/>s</returns>
        public static DungeonPrerequisite SetupUnlockOnCustomFlag(this EncounterTrackable self, CustomDungeonFlags flag, bool requiredFlagValue)
        {
            return self.AddPrerequisite(new CustomDungeonPrerequisite
            {
                advancedPrerequisiteType = CustomDungeonPrerequisite.AdvancedPrerequisiteType.CUSTOM_FLAG,
                requireCustomFlag = requiredFlagValue,
                customFlagToCheck = flag
            });
        }

        /// <summary>
        /// Setups a <see cref="DungeonPrerequisite"/> with the type of <see cref="CustomDungeonPrerequisite.AdvancedPrerequisiteType.CUSTOM_STAT_COMPARISION"/>, prerequisiteOperation of <paramref name="comparisonOperation"/> and comparisonValue of 
        /// <paramref name="comparisonValue"/> and adds it to <paramref name="self"/>'s list of <see cref="DungeonPrerequisite"/>s
        /// </summary>
        /// <param name="self">The <see cref="PickupObject"/> to add the <see cref="DungeonPrerequisite"/> to</param>
        /// <param name="stat">The <see cref="CustomTrackedStats"/> to get the value from</param>
        /// <param name="comparisonValue">The value to compare <paramref name="stat"/>'s value to</param>
        /// <param name="comparisonOperation">The comparison operation</param>
        /// <returns>The <see cref="DungeonPrerequisite"/> that was added to the list of <see cref="DungeonPrerequisite"/>s</returns>
        public static DungeonPrerequisite SetupUnlockOnCustomStat(this PickupObject self, CustomTrackedStats stat, float comparisonValue, DungeonPrerequisite.PrerequisiteOperation comparisonOperation)
        {
            if (self.encounterTrackable == null)
            {
                return null;
            }
            return self.encounterTrackable.SetupUnlockOnCustomStat(stat, comparisonValue, comparisonOperation);
        }

        /// <summary>
        /// Setups a <see cref="DungeonPrerequisite"/> with the type of <see cref="CustomDungeonPrerequisite.AdvancedPrerequisiteType.CUSTOM_STAT_COMPARISION"/>, prerequisiteOperation of <paramref name="comparisonOperation"/> and comparisonValue of 
        /// <paramref name="comparisonValue"/> and adds it to <paramref name="self"/>'s list of <see cref="DungeonPrerequisite"/>s
        /// </summary>
        /// <param name="self">The <see cref="EncounterTrackable"/> to add the <see cref="DungeonPrerequisite"/> to</param>
        /// <param name="stat">The <see cref="CustomTrackedStats"/> to get the value from</param>
        /// <param name="comparisonValue">The value to compare <paramref name="stat"/>'s value to</param>
        /// <param name="comparisonOperation">The comparison operation</param>
        /// <returns>The <see cref="DungeonPrerequisite"/> that was added to the list of <see cref="DungeonPrerequisite"/>s</returns>
        public static DungeonPrerequisite SetupUnlockOnCustomStat(this EncounterTrackable self, CustomTrackedStats stat, float comparisonValue, DungeonPrerequisite.PrerequisiteOperation comparisonOperation)
        {
            return self.AddPrerequisite(new CustomDungeonPrerequisite
            {
                advancedPrerequisiteType = CustomDungeonPrerequisite.AdvancedPrerequisiteType.CUSTOM_STAT_COMPARISION,
                customStatToCheck = stat,
                prerequisiteOperation = comparisonOperation,
                comparisonValue = comparisonValue
            });
        }

        /// <summary>
        /// Setups a <see cref="DungeonPrerequisite"/> with the type of <see cref="CustomDungeonPrerequisite.AdvancedPrerequisiteType.CUSTOM_MAXIMUM_COMPARISON"/>, prerequisiteOperation of <paramref name="comparisonOperation"/> and comparisonValue of 
        /// <paramref name="comparisonValue"/> and adds it to <paramref name="self"/>'s list of <see cref="DungeonPrerequisite"/>s
        /// </summary>
        /// <param name="self">The <see cref="PickupObject"/> to add the <see cref="DungeonPrerequisite"/> to</param>
        /// <param name="maximum">The <see cref="CustomTrackedMaximums"/> to get the value from</param>
        /// <param name="comparisonValue">The value to compare <paramref name="maximum"/>'s value to</param>
        /// <param name="comparisonOperation">The comparison operation</param>
        /// <returns>The <see cref="DungeonPrerequisite"/> that was added to the list of <see cref="DungeonPrerequisite"/>s</returns>
        public static DungeonPrerequisite SetupUnlockOnCustomMaximum(this PickupObject self, CustomTrackedMaximums maximum, float comparisonValue, DungeonPrerequisite.PrerequisiteOperation comparisonOperation)
        {
            if (self.encounterTrackable == null)
            {
                return null;
            }
            return self.encounterTrackable.SetupUnlockOnCustomMaximum(maximum, comparisonValue, comparisonOperation);
        }

        /// <summary>
        /// Setups a <see cref="DungeonPrerequisite"/> with the type of <see cref="CustomDungeonPrerequisite.AdvancedPrerequisiteType.CUSTOM_MAXIMUM_COMPARISON"/>, prerequisiteOperation of <paramref name="comparisonOperation"/> and comparisonValue of 
        /// <paramref name="comparisonValue"/> and adds it to <paramref name="self"/>'s list of <see cref="DungeonPrerequisite"/>s
        /// </summary>
        /// <param name="self">The <see cref="EncounterTrackable"/> to add the <see cref="DungeonPrerequisite"/> to</param>
        /// <param name="maximum">The <see cref="CustomTrackedMaximums"/> to get the value from</param>
        /// <param name="comparisonValue">The value to compare <paramref name="maximum"/>'s value to</param>
        /// <param name="comparisonOperation">The comparison operation</param>
        /// <returns>The <see cref="DungeonPrerequisite"/> that was added to the list of <see cref="DungeonPrerequisite"/>s</returns>
        public static DungeonPrerequisite SetupUnlockOnCustomMaximum(this EncounterTrackable self, CustomTrackedMaximums maximum, float comparisonValue, DungeonPrerequisite.PrerequisiteOperation comparisonOperation)
        {
            return self.AddPrerequisite(new CustomDungeonPrerequisite
            {
                advancedPrerequisiteType = CustomDungeonPrerequisite.AdvancedPrerequisiteType.CUSTOM_MAXIMUM_COMPARISON,
                customMaximumToCheck = maximum,
                prerequisiteOperation = comparisonOperation,
                comparisonValue = comparisonValue
            });
        }

        /// <summary>
        /// Setups a <see cref="DungeonPrerequisite"/> with the type of <see cref="CustomDungeonPrerequisite.AdvancedPrerequisiteType.NUMBER_PASTS_COMPLETED_BETTER"/>, prerequisiteOperation of <paramref name="comparisonOperation"/> and comparisonValue of 
        /// <paramref name="comparisonValue"/> and adds it to <paramref name="self"/>'s list of <see cref="DungeonPrerequisite"/>s
        /// </summary>
        /// <param name="self">The <see cref="PickupObject"/> to add the <see cref="DungeonPrerequisite"/> to</param>
        /// <param name="comparisonValue">The value to comprare the amount of pasts beaten to</param>
        /// <param name="comparisonOperation">The comparison operation</param>
        /// <returns>The <see cref="DungeonPrerequisite"/> that was added to the list of <see cref="DungeonPrerequisite"/>s</returns>
        public static DungeonPrerequisite SetupUnlockOnPastsBeaten(this PickupObject self, float comparisonValue, DungeonPrerequisite.PrerequisiteOperation comparisonOperation)
        {
            if (self.encounterTrackable == null)
            {
                return null;
            }
            return self.encounterTrackable.SetupUnlockOnPastsBeaten(comparisonValue, comparisonOperation);
        }

        /// <summary>
        /// Setups a <see cref="DungeonPrerequisite"/> with the type of <see cref="CustomDungeonPrerequisite.AdvancedPrerequisiteType.NUMBER_PASTS_COMPLETED_BETTER"/>, prerequisiteOperation of <paramref name="comparisonOperation"/> and comparisonValue of 
        /// <paramref name="comparisonValue"/> and adds it to <paramref name="self"/>'s list of <see cref="DungeonPrerequisite"/>s
        /// </summary>
        /// <param name="self">The <see cref="EncounterTrackable"/> to add the <see cref="DungeonPrerequisite"/> to</param>
        /// <param name="comparisonValue">The value to comprare the amount of pasts beaten to</param>
        /// <param name="comparisonOperation">The comparison operation</param>
        /// <returns>The <see cref="DungeonPrerequisite"/> that was added to the list of <see cref="DungeonPrerequisite"/>s</returns>
        public static DungeonPrerequisite SetupUnlockOnPastsBeaten(this EncounterTrackable self, float comparisonValue, DungeonPrerequisite.PrerequisiteOperation comparisonOperation)
        {
            return self.AddPrerequisite(new CustomDungeonPrerequisite
            {
                advancedPrerequisiteType = CustomDungeonPrerequisite.AdvancedPrerequisiteType.NUMBER_PASTS_COMPLETED_BETTER,
                prerequisiteOperation = comparisonOperation,
                comparisonValue = comparisonValue
            });
        }

        /// <summary>
        /// Adds <paramref name="prereq"/> to <paramref name="self"/>'s list of <see cref="DungeonPrerequisite"/>s
        /// </summary>
        /// <typeparam name="T">Prerequisite type</typeparam>
        /// <param name="self">The <see cref="PickupObject"/> to add <paramref name="prereq"/> to</param>
        /// <param name="prereq"><see cref="DungeonPrerequisite"/> to add</param>
        /// <returns><paramref name="prereq"/></returns>
        public static T AddPrerequisite<T>(this PickupObject self, T prereq) where T : DungeonPrerequisite
        {
            return (T)self.AddPrerequisite((DungeonPrerequisite)prereq);
        }

        /// <summary>
        /// Adds <paramref name="prereq"/> to <paramref name="self"/>'s list of <see cref="DungeonPrerequisite"/>s
        /// </summary>
        /// <typeparam name="T">Prerequisite type</typeparam>
        /// <param name="self">The <see cref="EncounterTrackable"/> to add <paramref name="prereq"/> to</param>
        /// <param name="prereq"><see cref="DungeonPrerequisite"/> to add</param>
        /// <returns><paramref name="prereq"/></returns>
        public static T AddPrerequisite<T>(this EncounterTrackable self, T prereq) where T : DungeonPrerequisite
        {
            return (T)self.AddPrerequisite((DungeonPrerequisite)prereq);
        }

        /// <summary>
        /// Adds <paramref name="prereq"/> to <paramref name="self"/>'s list of <see cref="DungeonPrerequisite"/>s
        /// </summary>
        /// <param name="self">The <see cref="PickupObject"/> to add <paramref name="prereq"/> to</param>
        /// <param name="prereq"><see cref="DungeonPrerequisite"/> to add</param>
        /// <returns><paramref name="prereq"/></returns>
        public static DungeonPrerequisite AddPrerequisite(this PickupObject self, DungeonPrerequisite prereq)
        {
            return self.encounterTrackable.AddPrerequisite(prereq);
        }

        /// <summary>
        /// Adds <paramref name="prereq"/> to <paramref name="self"/>'s list of <see cref="DungeonPrerequisite"/>s
        /// </summary>
        /// <param name="self">The <see cref="EncounterTrackable"/> to add <paramref name="prereq"/> to</param>
        /// <param name="prereq"><see cref="DungeonPrerequisite"/> to add</param>
        /// <returns><paramref name="prereq"/></returns>
        public static DungeonPrerequisite AddPrerequisite(this EncounterTrackable self, DungeonPrerequisite prereq)
        {
            if (!string.IsNullOrEmpty(self.ProxyEncounterGuid))
            {
                self.ProxyEncounterGuid = "";
            }
            if (self.prerequisites == null)
            {
                self.prerequisites = new DungeonPrerequisite[] { prereq };
            }
            else
            {
                self.prerequisites = self.prerequisites.Concat(new DungeonPrerequisite[] { prereq } ).ToArray();
            }
            EncounterDatabaseEntry databaseEntry = EncounterDatabase.GetEntry(self.EncounterGuid);
            if (!string.IsNullOrEmpty(databaseEntry.ProxyEncounterGuid))
            {
                databaseEntry.ProxyEncounterGuid = "";
            }
            if (databaseEntry.prerequisites == null)
            {
                databaseEntry.prerequisites = new DungeonPrerequisite[] { prereq };
            }
            else
            {
                databaseEntry.prerequisites = databaseEntry.prerequisites.Concat(new DungeonPrerequisite[] { prereq }).ToArray();
            }
            return prereq;
        }

        public static string ListToString<T>(List<T> list)
        {
            string str = "(";
            for (int j = 0; j < list.Count; j++)
            {
                string guid = list[j].ToString();
                str += guid;
                if (j < list.Count - 1)
                {
                    str += ", ";
                }
            }
            str += ")";
            return str;
        }

        public static void InsertOrAdd<T>(this List<T> self, int index, T toAdd)
        {
            if (index < 0 || index > self.Count)
            {
                self.Add(toAdd);
            }
            else
            {
                self.Insert(index, toAdd);
            }
        }

        public static void LogSmart(string text, bool debuglog = false)
        {
            if(ETGModConsole.Instance != null)
            {
                ETGModConsole.Log(text, debuglog);
            }
            else
            {
                Debug.Log(text);
            }
        }

        /// <summary>
        /// Clones <paramref name="orig"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="orig">List to clone</param>
        /// <returns>The clone of <paramref name="orig"/></returns>
        public static List<T> CloneList<T>(List<T> orig)
        {
            List<T> result = new List<T>();
            for (int i = 0; i < orig.Count; i++)
            {
                result.Add(orig[i]);
            }
            return result;
        }

        /// <summary>
        /// Loads an asset from any asset bundles
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="path">Object's path</param>
        /// <returns>The loaded object or null if it didn't find the object</returns>
        public static T LoadAssetFromAnywhere<T>(string path) where T : UnityEngine.Object
        {
            string[] BundlePrereqs = new string[]
            {
                "brave_resources_001",
                "dungeon_scene_001",
                "encounters_base_001",
                "enemies_base_001",
                "flows_base_001",
                "foyer_001",
                "foyer_002",
                "foyer_003",
                "shared_auto_001",
                "shared_auto_002",
                "shared_base_001",
                "dungeons/base_bullethell",
                "dungeons/base_castle",
                "dungeons/base_catacombs",
                "dungeons/base_cathedral",
                "dungeons/base_forge",
                "dungeons/base_foyer",
                "dungeons/base_gungeon",
                "dungeons/base_mines",
                "dungeons/base_nakatomi",
                "dungeons/base_resourcefulrat",
                "dungeons/base_sewer",
                "dungeons/base_tutorial",
                "dungeons/finalscenario_bullet",
                "dungeons/finalscenario_convict",
                "dungeons/finalscenario_coop",
                "dungeons/finalscenario_guide",
                "dungeons/finalscenario_pilot",
                "dungeons/finalscenario_robot",
                "dungeons/finalscenario_soldier"
            };
            T obj = null;
            foreach (string name in BundlePrereqs)
            {
                try
                {
                    obj = ResourceManager.LoadAssetBundle(name).LoadAsset<T>(path);
                }
                catch
                {
                }
                if (obj != null)
                {
                    break;
                }
            }
            return obj;
        }

        public static void SetComplex(this StringDBTable self, string key, params string[] values)
        {
            StringTableManager.StringCollection stringCollection = new StringTableManager.ComplexStringCollection();
            foreach (string value in values)
            {
                stringCollection.AddString(value, 1f);
            }
            self[key] = stringCollection;
        }

        /// <summary>
        /// Checks if  <paramref name="enemy"/> is in the required jammed state.
        /// </summary>
        /// <param name="enemy">Enemy to check</param>
        /// <param name="requiredState">Jammed state required for the enemy to be valid</param>
        /// <returns><see langword="true"/> if the enemy is in the required jammed state, <see langword="false"/> otherwise</returns>
        public static bool IsEnemyStateValid(AIActor enemy, JammedEnemyState requiredState)
        {
            if (requiredState == JammedEnemyState.NoCheck)
            {
                return true;
            }
            return requiredState == JammedEnemyState.Jammed ? enemy.IsBlackPhantom : requiredState == JammedEnemyState.Unjammed && !enemy.IsBlackPhantom;
        }
    }
}
