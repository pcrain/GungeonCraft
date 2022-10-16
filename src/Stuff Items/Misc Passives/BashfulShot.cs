using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Alexandria.ItemAPI;

namespace NevernamedsItems
{
    class BashfulShot : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Bashful Shot";
            string resourceName = "NevernamedsItems/Resources/bashfulshot_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<BashfulShot>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Travel Light";
            string longDesc = "These remarkably compatible musket balls prefer to keep to themselves, and don’t like other items getting in the way."+"\n\nMaybe don’t pick up that Junk.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.S;
            item.SetTag("bullet_modifier");
            BashfulShotID = item.PickupObjectId;
        }
        public static int BashfulShotID;
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
                int itemsHeld = 0;
                foreach (PassiveItem item in player.passiveItems)
                {
                    itemsHeld += 1;
                }
                foreach (Gun gun in player.inventory.AllGuns)
                {
                    itemsHeld += 1;
                }
                //ETGModConsole.Log("Current held items: " + itemsHeld);
                if (itemsHeld <= 30 && !Owner.HasPickupID(Gungeon.Game.Items["nn:crowded_clips"].PickupObjectId))
                {
                    float normalStatModAmount = 1f;
                    float damageStatModAmount = 1f;
                    for (int i = 0; i < itemsHeld; i++)
                    {
                        normalStatModAmount -= 0.03f;
                        damageStatModAmount -= 0.03f;                       
                    }
                    normalStatModAmount = Mathf.Max(0, normalStatModAmount);
                    damageStatModAmount = Mathf.Max(0, damageStatModAmount);
                    AddStat(PlayerStats.StatType.Damage, damageStatModAmount, StatModifier.ModifyMethod.ADDITIVE);
                    AddStat(PlayerStats.StatType.RateOfFire, normalStatModAmount, StatModifier.ModifyMethod.ADDITIVE);                    
                }
                else if (Owner.HasPickupID(Gungeon.Game.Items["nn:crowded_clips"].PickupObjectId))
                {
                    AddStat(PlayerStats.StatType.Damage, 1, StatModifier.ModifyMethod.ADDITIVE);
                    AddStat(PlayerStats.StatType.RateOfFire, 1, StatModifier.ModifyMethod.ADDITIVE);
                }
                lastItems = currentItems;
                lastGuns = currentGuns;
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
    }
}
