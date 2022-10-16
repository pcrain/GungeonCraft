using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Alexandria.ItemAPI;

namespace NevernamedsItems
{
    class FullArmourJacket : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Full Armour Jacket";
            string resourceName = "NevernamedsItems/Resources/fullarmourjacket_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<FullArmourJacket>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Live n' Lerm";
            string longDesc = "Every piece of Armour increases damage by 6%." + "\n\nThe best defence is a good offence.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.B;
            item.SetTag("bullet_modifier");
            item.ArmorToGainOnInitialPickup = 1;
            FullArmourJacketID = item.PickupObjectId;
        }
        public static int FullArmourJacketID;
        private float currentArmour, lastArmour;
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
            currentArmour = player.healthHaver.Armor;
            if (currentArmour != lastArmour)
            {
                RemoveStat(PlayerStats.StatType.Damage);
                float damageAmountMult;
                if (player.HasPickupID(Gungeon.Game.Items["nn:junkllets"].PickupObjectId))
                {
                    damageAmountMult = currentArmour * 0.07f;
                    damageAmountMult += 1f;
                }
                else
                {
                    damageAmountMult = currentArmour * 0.06f;
                    damageAmountMult += 1f;
                }
                AddStat(PlayerStats.StatType.Damage, damageAmountMult, StatModifier.ModifyMethod.MULTIPLICATIVE);
                lastArmour = currentArmour;
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
        public override void Pickup(PlayerController player)
        {

            base.Pickup(player);
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject debrisObject = base.Drop(player);
            return debrisObject;
        }
        public override void OnDestroy()
        {
            base.OnDestroy();
        }
    }
}