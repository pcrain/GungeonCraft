using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FullSerializer;
using UnityEngine;

namespace SaveAPI
{
    /// <summary>
    /// Class for storing game stats like tracked stats, tracked maximums and character-specific flags
    /// </summary>
    [fsObject]
    public class AdvancedGameStats
    {
        public AdvancedGameStats()
        {
            this.m_flags = new HashSet<CustomCharacterSpecificGungeonFlags>();
            this.stats = new Dictionary<CustomTrackedStats, float>(new CustomTrackedStatsComparer());
            this.maxima = new Dictionary<CustomTrackedMaximums, float>(new CustomTrackedMaximumsComparer());
        }

        public float GetStatValue(CustomTrackedStats statToCheck)
        {
            if (!this.stats.ContainsKey(statToCheck))
            {
                return 0f;
            }
            return this.stats[statToCheck];
        }

        public float GetMaximumValue(CustomTrackedMaximums maxToCheck)
        {
            if (!this.maxima.ContainsKey(maxToCheck))
            {
                return 0f;
            }
            return this.maxima[maxToCheck];
        }

        public bool GetFlag(CustomCharacterSpecificGungeonFlags flag)
        {
            if (flag == CustomCharacterSpecificGungeonFlags.NONE)
            {
                Debug.LogError("Something is attempting to get a NONE character-specific save flag!");
                return false;
            }
            return this.m_flags.Contains(flag);
        }

        public void SetStat(CustomTrackedStats stat, float val)
        {
            if (this.stats.ContainsKey(stat))
            {
                this.stats[stat] = val;
            }
            else
            {
                this.stats.Add(stat, val);
            }
        }

        public void SetMax(CustomTrackedMaximums max, float val)
        {
            if (this.maxima.ContainsKey(max))
            {
                this.maxima[max] = Mathf.Max(this.maxima[max], val);
            }
            else
            {
                this.maxima.Add(max, val);
            }
        }

        public void SetFlag(CustomCharacterSpecificGungeonFlags flag, bool value)
        {
            if (flag == CustomCharacterSpecificGungeonFlags.NONE)
            {
                Debug.LogError("Something is attempting to set a NONE character-specific save flag!");
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

        public void IncrementStat(CustomTrackedStats stat, float val)
        {
            if (this.stats.ContainsKey(stat))
            {
                this.stats[stat] = this.stats[stat] + val;
            }
            else
            {
                this.stats.Add(stat, val);
            }
        }

        public void AddStats(AdvancedGameStats otherStats)
        {
            foreach (KeyValuePair<CustomTrackedStats, float> keyValuePair in otherStats.stats)
            {
                this.IncrementStat(keyValuePair.Key, keyValuePair.Value);
            }
            foreach (KeyValuePair<CustomTrackedMaximums, float> keyValuePair2 in otherStats.maxima)
            {
                this.SetMax(keyValuePair2.Key, keyValuePair2.Value);
            }
            foreach (CustomCharacterSpecificGungeonFlags item in otherStats.m_flags)
            {
                this.m_flags.Add(item);
            }
        }

        public void ClearAllState()
        {
            List<CustomTrackedStats> list = new List<CustomTrackedStats>();
            foreach (KeyValuePair<CustomTrackedStats, float> keyValuePair in this.stats)
            {
                list.Add(keyValuePair.Key);
            }
            foreach (CustomTrackedStats key in list)
            {
                this.stats[key] = 0f;
            }
            List<CustomTrackedMaximums> list2 = new List<CustomTrackedMaximums>();
            foreach (KeyValuePair<CustomTrackedMaximums, float> keyValuePair2 in this.maxima)
            {
                list2.Add(keyValuePair2.Key);
            }
            foreach (CustomTrackedMaximums key2 in list2)
            {
                this.maxima[key2] = 0f;
            }
        }

        [fsProperty]
        private Dictionary<CustomTrackedStats, float> stats;
        [fsProperty]
        private Dictionary<CustomTrackedMaximums, float> maxima;
        [fsProperty]
        public HashSet<CustomCharacterSpecificGungeonFlags> m_flags;
    }
}
