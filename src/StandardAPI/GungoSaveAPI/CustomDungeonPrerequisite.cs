using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SaveAPI
{
    /// <summary>
    /// Overridable <see cref="DungeonPrerequisite"/>
    /// </summary>
    public class CustomDungeonPrerequisite : DungeonPrerequisite
    {
        /// <summary>
        /// Overridable condition checker method
        /// </summary>
        /// <returns><see langword="true"/> if all conditions are fulfilled</returns>
        public virtual new bool CheckConditionsFulfilled()
        {
            if (this.advancedPrerequisiteType == AdvancedPrerequisiteType.CUSTOM_FLAG)
            {
                return AdvancedGameStatsManager.Instance.GetFlag(this.customFlagToCheck) == this.requireCustomFlag;
            }
            else if (this.advancedPrerequisiteType == AdvancedPrerequisiteType.CUSTOM_STAT_COMPARISION)
            {
                float playerStatValue = AdvancedGameStatsManager.Instance.GetPlayerStatValue(this.customStatToCheck);
                switch (this.prerequisiteOperation)
                {
                    case PrerequisiteOperation.LESS_THAN:
                        return playerStatValue < this.comparisonValue;
                    case PrerequisiteOperation.EQUAL_TO:
                        return playerStatValue == this.comparisonValue;
                    case PrerequisiteOperation.GREATER_THAN:
                        return playerStatValue > this.comparisonValue;
                    default:
                        Debug.LogError("Switching on invalid stat comparison operation!");
                        break;
                }
            }
            else if(this.advancedPrerequisiteType == AdvancedPrerequisiteType.CUSTOM_MAXIMUM_COMPARISON)
            {
                float playerMaximum = AdvancedGameStatsManager.Instance.GetPlayerMaximum(this.customMaximumToCheck);
                switch (this.prerequisiteOperation)
                {
                    case PrerequisiteOperation.LESS_THAN:
                        return playerMaximum < this.comparisonValue;
                    case PrerequisiteOperation.EQUAL_TO:
                        return playerMaximum == this.comparisonValue;
                    case PrerequisiteOperation.GREATER_THAN:
                        return playerMaximum > this.comparisonValue;
                    default:
                        Debug.LogError("Switching on invalid stat comparison operation!");
                        break;
                }
            }
            else if (this.advancedPrerequisiteType == AdvancedPrerequisiteType.NUMBER_PASTS_COMPLETED_BETTER)
            {
                float pastsBeaten = GameStatsManager.Instance.GetNumberPastsBeaten();
                switch (this.prerequisiteOperation)
                {
                    case PrerequisiteOperation.LESS_THAN:
                        return pastsBeaten < this.comparisonValue;
                    case PrerequisiteOperation.EQUAL_TO:
                        return pastsBeaten == this.comparisonValue;
                    case PrerequisiteOperation.GREATER_THAN:
                        return pastsBeaten > this.comparisonValue;
                    default:
                        Debug.LogError("Switching on invalid stat comparison operation!");
                        break;
                }
            }
            else if(this.advancedPrerequisiteType == AdvancedPrerequisiteType.ENCOUNTER_OR_CUSTOM_FLAG)
            {
                EncounterDatabaseEntry encounterDatabaseEntry = null;
                if (!string.IsNullOrEmpty(this.encounteredObjectGuid))
                {
                    encounterDatabaseEntry = EncounterDatabase.GetEntry(this.encounteredObjectGuid);
                }
                if (AdvancedGameStatsManager.Instance.GetFlag(this.customFlagToCheck) == this.requireCustomFlag)
                {
                    return true;
                }
                if (encounterDatabaseEntry != null)
                {
                    int num3 = GameStatsManager.Instance.QueryEncounterable(encounterDatabaseEntry);
                    switch (this.prerequisiteOperation)
                    {
                        case PrerequisiteOperation.LESS_THAN:
                            return num3 < this.requiredNumberOfEncounters;
                        case PrerequisiteOperation.EQUAL_TO:
                            return num3 == this.requiredNumberOfEncounters;
                        case PrerequisiteOperation.GREATER_THAN:
                            return num3 > this.requiredNumberOfEncounters;
                        default:
                            Debug.LogError("Switching on invalid stat comparison operation!");
                            break;
                    }
                }
                else if (this.encounteredRoom != null)
                {
                    int num4 = GameStatsManager.Instance.QueryRoomEncountered(this.encounteredRoom.GUID);
                    switch (this.prerequisiteOperation)
                    {
                        case PrerequisiteOperation.LESS_THAN:
                            return num4 < this.requiredNumberOfEncounters;
                        case PrerequisiteOperation.EQUAL_TO:
                            return num4 == this.requiredNumberOfEncounters;
                        case PrerequisiteOperation.GREATER_THAN:
                            return num4 > this.requiredNumberOfEncounters;
                        default:
                            Debug.LogError("Switching on invalid stat comparison operation!");
                            break;
                    }
                }
                return false;
            }
            else
            {
                return this.CheckConditionsFulfilledOrig();
            }
            return false;
        }

        /// <summary>
        /// Base condition checker method
        /// </summary>
        /// <returns><see langword="true"/> if all conditions are fulfilled</returns>
        public bool CheckConditionsFulfilledOrig()
        {
            EncounterDatabaseEntry encounterDatabaseEntry = null;
            if (!string.IsNullOrEmpty(this.encounteredObjectGuid))
            {
                encounterDatabaseEntry = EncounterDatabase.GetEntry(this.encounteredObjectGuid);
            }
            switch (this.prerequisiteType)
            {
                case PrerequisiteType.ENCOUNTER:
                    if (encounterDatabaseEntry == null && this.encounteredRoom == null)
                    {
                        return true;
                    }
                    if (encounterDatabaseEntry != null)
                    {
                        int num = GameStatsManager.Instance.QueryEncounterable(encounterDatabaseEntry);
                        switch (this.prerequisiteOperation)
                        {
                            case PrerequisiteOperation.LESS_THAN:
                                return num < this.requiredNumberOfEncounters;
                            case PrerequisiteOperation.EQUAL_TO:
                                return num == this.requiredNumberOfEncounters;
                            case PrerequisiteOperation.GREATER_THAN:
                                return num > this.requiredNumberOfEncounters;
                            default:
                                Debug.LogError("Switching on invalid stat comparison operation!");
                                break;
                        }
                    }
                    else if (this.encounteredRoom != null)
                    {
                        int num2 = GameStatsManager.Instance.QueryRoomEncountered(this.encounteredRoom.GUID);
                        switch (this.prerequisiteOperation)
                        {
                            case PrerequisiteOperation.LESS_THAN:
                                return num2 < this.requiredNumberOfEncounters;
                            case PrerequisiteOperation.EQUAL_TO:
                                return num2 == this.requiredNumberOfEncounters;
                            case PrerequisiteOperation.GREATER_THAN:
                                return num2 > this.requiredNumberOfEncounters;
                            default:
                                Debug.LogError("Switching on invalid stat comparison operation!");
                                break;
                        }
                    }
                    return false;
                case PrerequisiteType.COMPARISON:
                    {
                        float playerStatValue = GameStatsManager.Instance.GetPlayerStatValue(this.statToCheck);
                        switch (this.prerequisiteOperation)
                        {
                            case PrerequisiteOperation.LESS_THAN:
                                return playerStatValue < this.comparisonValue;
                            case PrerequisiteOperation.EQUAL_TO:
                                return playerStatValue == this.comparisonValue;
                            case PrerequisiteOperation.GREATER_THAN:
                                return playerStatValue > this.comparisonValue;
                            default:
                                Debug.LogError("Switching on invalid stat comparison operation!");
                                break;
                        }
                        break;
                    }
                case PrerequisiteType.CHARACTER:
                    {
                        PlayableCharacters playableCharacters = (PlayableCharacters)(-1);
                        if (!BraveRandom.IgnoreGenerationDifferentiator)
                        {
                            if (GameManager.Instance.PrimaryPlayer != null)
                            {
                                playableCharacters = GameManager.Instance.PrimaryPlayer.characterIdentity;
                            }
                            else if (GameManager.PlayerPrefabForNewGame != null)
                            {
                                playableCharacters = GameManager.PlayerPrefabForNewGame.GetComponent<PlayerController>().characterIdentity;
                            }
                            else if (GameManager.Instance.BestGenerationDungeonPrefab != null)
                            {
                                playableCharacters = GameManager.Instance.BestGenerationDungeonPrefab.defaultPlayerPrefab.GetComponent<PlayerController>().characterIdentity;
                            }
                        }
                        return this.requireCharacter == (playableCharacters == this.requiredCharacter);
                    }
                case PrerequisiteType.TILESET:
                    if (GameManager.Instance.BestGenerationDungeonPrefab != null)
                    {
                        return this.requireTileset == (GameManager.Instance.BestGenerationDungeonPrefab.tileIndices.tilesetId == this.requiredTileset);
                    }
                    return this.requireTileset == (GameManager.Instance.Dungeon.tileIndices.tilesetId == this.requiredTileset);
                case PrerequisiteType.FLAG:
                    return GameStatsManager.Instance.GetFlag(this.saveFlagToCheck) == this.requireFlag;
                case PrerequisiteType.DEMO_MODE:
                    return !this.requireDemoMode;
                case PrerequisiteType.MAXIMUM_COMPARISON:
                    {
                        float playerMaximum = GameStatsManager.Instance.GetPlayerMaximum(this.maxToCheck);
                        switch (this.prerequisiteOperation)
                        {
                            case PrerequisiteOperation.LESS_THAN:
                                return playerMaximum < this.comparisonValue;
                            case PrerequisiteOperation.EQUAL_TO:
                                return playerMaximum == this.comparisonValue;
                            case PrerequisiteOperation.GREATER_THAN:
                                return playerMaximum > this.comparisonValue;
                            default:
                                Debug.LogError("Switching on invalid stat comparison operation!");
                                break;
                        }
                        break;
                    }
                case PrerequisiteType.ENCOUNTER_OR_FLAG:
                    if (GameStatsManager.Instance.GetFlag(this.saveFlagToCheck) == this.requireFlag)
                    {
                        return true;
                    }
                    if (encounterDatabaseEntry != null)
                    {
                        int num3 = GameStatsManager.Instance.QueryEncounterable(encounterDatabaseEntry);
                        switch (this.prerequisiteOperation)
                        {
                            case PrerequisiteOperation.LESS_THAN:
                                return num3 < this.requiredNumberOfEncounters;
                            case PrerequisiteOperation.EQUAL_TO:
                                return num3 == this.requiredNumberOfEncounters;
                            case PrerequisiteOperation.GREATER_THAN:
                                return num3 > this.requiredNumberOfEncounters;
                            default:
                                Debug.LogError("Switching on invalid stat comparison operation!");
                                break;
                        }
                    }
                    else if (this.encounteredRoom != null)
                    {
                        int num4 = GameStatsManager.Instance.QueryRoomEncountered(this.encounteredRoom.GUID);
                        switch (this.prerequisiteOperation)
                        {
                            case PrerequisiteOperation.LESS_THAN:
                                return num4 < this.requiredNumberOfEncounters;
                            case PrerequisiteOperation.EQUAL_TO:
                                return num4 == this.requiredNumberOfEncounters;
                            case PrerequisiteOperation.GREATER_THAN:
                                return num4 > this.requiredNumberOfEncounters;
                            default:
                                Debug.LogError("Switching on invalid stat comparison operation!");
                                break;
                        }
                    }
                    return false;
                case PrerequisiteType.NUMBER_PASTS_COMPLETED:
                    return (float)GameStatsManager.Instance.GetNumberPastsBeaten() >= this.comparisonValue;
                default:
                    Debug.LogError("Switching on invalid prerequisite type!!!");
                    break;
            }
            return false;
        }

        public AdvancedPrerequisiteType advancedPrerequisiteType;
        public CustomDungeonFlags customFlagToCheck;
        public bool requireCustomFlag;
        public Type requiredPassiveFlag;
        public CustomTrackedMaximums customMaximumToCheck;
        public CustomTrackedStats customStatToCheck;
        public enum AdvancedPrerequisiteType
        {
            NONE,
            CUSTOM_FLAG,
            CUSTOM_STAT_COMPARISION,
            CUSTOM_MAXIMUM_COMPARISON,
            NUMBER_PASTS_COMPLETED_BETTER,
            ENCOUNTER_OR_CUSTOM_FLAG
        }
    }
}
