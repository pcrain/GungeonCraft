using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MonoMod.RuntimeDetour;

namespace ItemAPI
{
    public static class CustomSynergies
    {
        public static Hook synergyHook = new Hook(
            typeof(StringTableManager).GetMethod("GetSynergyString", BindingFlags.Static | BindingFlags.Public),
            typeof(CustomSynergies).GetMethod("SynergyStringHook")
        );

        public static AdvancedSynergyEntry Add(string name, List<string> mandatoryConsoleIDs, List<string> optionalConsoleIDs = null, bool ignoreLichEyeBullets = true)
        {
            if (mandatoryConsoleIDs == null || mandatoryConsoleIDs.Count == 0) { ETGModConsole.Log($"Synergy {name} has no mandatory items/guns."); return null; }
            List<int>
                itemIDs = new List<int>(),
                gunIDs = new List<int>(),
                optItemIDs = new List<int>(),
                optGunIDs = new List<int>();
            PickupObject pickup;
            foreach (var id in mandatoryConsoleIDs)
            {
                pickup = Gungeon.Game.Items[id];
                if (pickup && pickup.GetComponent<Gun>())
                    gunIDs.Add(pickup.PickupObjectId);
                else if (pickup && (pickup.GetComponent<PlayerItem>() || pickup.GetComponent<PassiveItem>()))
                    itemIDs.Add(pickup.PickupObjectId);
            }

            if (optionalConsoleIDs != null)
            {
                foreach (var id in optionalConsoleIDs)
                {
                    pickup = Gungeon.Game.Items[id];
                    if (pickup && pickup.GetComponent<Gun>())
                        optGunIDs.Add(pickup.PickupObjectId);
                    else if (pickup && (pickup.GetComponent<PlayerItem>() || pickup.GetComponent<PassiveItem>()))
                        optItemIDs.Add(pickup.PickupObjectId);
                }
            }

            AdvancedSynergyEntry entry = new AdvancedSynergyEntry()
            {
                NameKey = name,
                MandatoryItemIDs = itemIDs,
                MandatoryGunIDs = gunIDs,
                OptionalItemIDs = optItemIDs,
                OptionalGunIDs = optGunIDs,
                bonusSynergies = new List<CustomSynergyType>(),
                statModifiers = new List<StatModifier>(),
            };
            Add(entry);
            return entry;
        }

        public static void Add(AdvancedSynergyEntry synergyEntry)
        {
            AdvancedSynergyEntry[] array = new AdvancedSynergyEntry[] { synergyEntry };
            GameManager.Instance.SynergyManager.synergies = GameManager.Instance.SynergyManager.synergies.Concat(array).ToArray<AdvancedSynergyEntry>();
        }

        public static string SynergyStringHook(Func<string, int, string> orig, string key, int index = -1)
        {
            string text = orig(key, index);
            bool flag = string.IsNullOrEmpty(text);
            if (flag) text = key;
            return text;
        }

        public static bool HasMTGConsoleID(this PlayerController player, string consoleID)
        {
            if (!Gungeon.Game.Items.ContainsID(consoleID)) return false;
            return player.HasPickupID(Gungeon.Game.Items[consoleID].PickupObjectId);
        }
    }
}
