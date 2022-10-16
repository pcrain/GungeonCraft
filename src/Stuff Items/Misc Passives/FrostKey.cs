using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using UnityEngine;
using ItemAPI;

namespace NevernamedsItems
{
    class FrostKey : PassiveItem
    {
        public static void Init()
        {
            //The name of the item
            string itemName = "Frost Key";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it
            string resourceName = "NevernamedsItems/Resources/frostkey_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a PassiveItem component to the object
            var item = obj.AddComponent<FrostKey>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Cold Open";
            string longDesc = "Keys increase coolness."+"\n\nDespite his age, Flynt remains stubbornly convinced that he is, and always will be, a 'cool dude' (as the kids say).";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //Do this after ItemBuilder.AddSpriteToObject!
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Adds the actual passive effect to the item            
            //Set the rarity of the item
            item.AddToSubShop(ItemBuilder.ShopType.Flynt);
            item.quality = PickupObject.ItemQuality.B;
        }
        private float currentKeys, lastKeys;
        private float currentItems, lastItems;
        public override void Update()
        {
            if (Owner)
            {
                currentKeys = Owner.carriedConsumables.KeyBullets;
                currentItems = Owner.passiveItems.Count;
                if (currentKeys != lastKeys || currentItems != lastItems)
                {
                    int coolnessToGive = Owner.carriedConsumables.KeyBullets;
                    int matchingItems = 0;
                    RemoveStat(PlayerStats.StatType.Coolness);

                    foreach (PassiveItem item in Owner.passiveItems)
                    {
                        if (frostAndGunfireSynergyItems.Contains(item.PickupObjectId))
                        {
                            matchingItems += 2;
                        }
                    }
                    AddStat(PlayerStats.StatType.Coolness, coolnessToGive + matchingItems, StatModifier.ModifyMethod.ADDITIVE);
                    Owner.stats.RecalculateStats(Owner, true, false);

                    lastKeys = currentKeys;
                    lastItems = currentItems;
                }
                else { return; }
            }
        }
        public static List<int> frostAndGunfireSynergyItems = new List<int>()
        {
            146, //Dragunfire
            191, //Ring of Fire Resistance
            395, //Staff of Firepower
            670, //High Dragunfire
        };
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
