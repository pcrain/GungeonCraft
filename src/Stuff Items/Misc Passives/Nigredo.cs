using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using ItemAPI;

namespace NevernamedsItems
{
    public class Nigredo : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Nigredo";
            string resourceName = "NevernamedsItems/Resources/nigredo_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<Nigredo>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Decay";
            string longDesc = "Curse increases rate of fire."+"\n\nA toxic solution of putrifying chemicals, making up the first state of the process to formulate the Philosopher's Stone.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.Curse, 1f, StatModifier.ModifyMethod.ADDITIVE);
            item.quality = PickupObject.ItemQuality.D;
            item.AddToSubShop(ItemBuilder.ShopType.Goopton);
        }
        private float lastCurse;
        public override void Update()
        {
            if (Owner)
            {
                CalculateHealth(Owner);
            }
            else { return; }
        }

        private void CalculateHealth(PlayerController player)
        {
            float curseAmt = player.stats.GetStatValue(PlayerStats.StatType.Curse);
            if (curseAmt != lastCurse)
            {
                RemoveStat(PlayerStats.StatType.RateOfFire);
                float multiplier = (curseAmt * 0.05f) + 1;
                
                AddStat(PlayerStats.StatType.RateOfFire, multiplier, StatModifier.ModifyMethod.MULTIPLICATIVE);
                lastCurse = curseAmt;
                player.stats.RecalculateStats(player, true, false);
            }
        }
        private void AddStat(PlayerStats.StatType statType, float amount, StatModifier.ModifyMethod method = StatModifier.ModifyMethod.ADDITIVE)
        {
            StatModifier modifier = new StatModifier
            {
                amount = amount,
                statToBoost = statType,
                modifyType = method
            };

            if (this.passiveStatModifiers == null)
                this.passiveStatModifiers = new StatModifier[] { modifier };
            else
                this.passiveStatModifiers = this.passiveStatModifiers.Concat(new StatModifier[] { modifier }).ToArray();
        }
        private void RemoveStat(PlayerStats.StatType statType)
        {
            var newModifiers = new List<StatModifier>();
            for (int i = 0; i < passiveStatModifiers.Length; i++)
            {
                if (passiveStatModifiers[i].statToBoost != statType)
                    newModifiers.Add(passiveStatModifiers[i]);
            }
            this.passiveStatModifiers = newModifiers.ToArray();
        }
    }
}
