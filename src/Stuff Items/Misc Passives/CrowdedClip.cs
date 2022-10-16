using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Alexandria.ItemAPI;

namespace NevernamedsItems
{
    class CrowdedClip : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Crowded Clips";
            string resourceName = "NevernamedsItems/Resources/crowdedclips_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<CrowdedClip>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "More the Merrier";
            string longDesc = "+1% Damage and Firerate but -1% accuracy for every item or gun held." + "\n\nThis extroverted artefact enjoys the company of other items, and rewards their presence.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.A;
            item.SetTag("bullet_modifier");
        }
        private int currentItems, lastItems;
        private int currentGuns, lastGuns;
        public override void Update()
        {
            if (Owner)
            {
                CalculateStats(Owner);
            }

            else { return; }
        }

        private void CalculateStats(PlayerController player)
        {
            currentItems = player.passiveItems.Count;
            currentGuns = player.inventory.AllGuns.Count;
            bool itemsChanged = currentItems != lastItems;
            bool gunsChanged = currentGuns != lastGuns;
            if (itemsChanged || gunsChanged)
            {
                RemoveStat(PlayerStats.StatType.Damage);
                RemoveStat(PlayerStats.StatType.RateOfFire);
                RemoveStat(PlayerStats.StatType.Accuracy);
                if (!Owner.HasPickupID(Gungeon.Game.Items["nn:bashful_shot"].PickupObjectId))
                {
                    foreach (PassiveItem item in player.passiveItems)
                    {
                        AddStat(PlayerStats.StatType.Damage, 1.01f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                        AddStat(PlayerStats.StatType.RateOfFire, 1.01f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                        AddStat(PlayerStats.StatType.Accuracy, 1.01f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                    }
                    foreach (Gun gun in player.inventory.AllGuns)
                    {
                        AddStat(PlayerStats.StatType.Damage, 1.01f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                        AddStat(PlayerStats.StatType.RateOfFire, 1.01f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                        AddStat(PlayerStats.StatType.Accuracy, 1.01f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                    }
                }
                else if (Owner.HasPickupID(Gungeon.Game.Items["nn:bashful_shot"].PickupObjectId))
                {
                    AddStat(PlayerStats.StatType.Damage, 1f, StatModifier.ModifyMethod.ADDITIVE);
                    AddStat(PlayerStats.StatType.RateOfFire, 1f, StatModifier.ModifyMethod.ADDITIVE);
                    AddStat(PlayerStats.StatType.Accuracy, 1f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                }
                lastItems = currentItems;
                lastGuns = currentGuns;
                player.stats.RecalculateStats(player, true, false);
            }
        }


        private void AddStat(PlayerStats.StatType statType, float amount, StatModifier.ModifyMethod method = StatModifier.ModifyMethod.ADDITIVE)
        {
            /*foreach (var m in passiveStatModifiers)
            {
                if (m.statToBoost == statType) return; //don't add duplicates
            }*/

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
    }
}