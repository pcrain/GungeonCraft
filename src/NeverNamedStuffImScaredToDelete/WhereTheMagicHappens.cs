using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CwaffingTheGungy
{
    public static class WhereTheMagicHappens
    {
        public static bool PlayerHasActiveSynergy(this PlayerController player, string synergyNameToCheck)
        {
            foreach (int index in player.ActiveExtraSynergies)
            {
                AdvancedSynergyEntry synergy = GameManager.Instance.SynergyManager.synergies[index];
                if (synergy.NameKey == synergyNameToCheck)
                {
                    return true;
                }
            }
            return false;
        }
        public static void AddPassiveStatModifier(this Gun gun, PlayerStats.StatType statType, float amount, StatModifier.ModifyMethod modifyMethod)
        {
            gun.passiveStatModifiers = gun.passiveStatModifiers.Concat(new StatModifier[] { new StatModifier { statToBoost = statType, amount = amount, modifyType = modifyMethod } }).ToArray();
        }

        public static void AddCurrentGunStatModifier(this Gun gun, PlayerStats.StatType statType, float amount, StatModifier.ModifyMethod modifyMethod)
        {
            gun.currentGunStatModifiers = gun.currentGunStatModifiers.Concat(new StatModifier[] { new StatModifier { statToBoost = statType, amount = amount, modifyType = modifyMethod } }).ToArray();
        }

        public static void AddCurrentGunDamageTypeModifier(this Gun gun, CoreDamageTypes damageTypes, float damageMultiplier)
        {
            gun.currentGunDamageTypeModifiers = gun.currentGunDamageTypeModifiers.Concat(new DamageTypeModifier[] { new DamageTypeModifier { damageType = damageTypes, damageMultiplier = damageMultiplier } }).ToArray();
        }
    }
}
