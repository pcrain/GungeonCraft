using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using ItemAPI;
using System.Collections;

namespace NevernamedsItems
{
    public class MobiusClip : PassiveItem
    {
        public static void Init()
        {
            //The name of the item
            string itemName = "Mobius Clip";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it
            string resourceName = "NevernamedsItems/Resources/mobiusclip_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a PassiveItem component to the object
            var item = obj.AddComponent<MobiusClip>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "The Power of Infinity";
            string longDesc = "Triples the damage of all infinite ammo guns. Does not work on guns that are A tier or above." + "\n\nA peculiar mathematical concept repurposed to store powerful ammunition.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //Do this after ItemBuilder.AddSpriteToObject!
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Adds the actual passive effect to the item

            //Set the rarity of the item
            item.quality = PickupObject.ItemQuality.C;
        }
        private Gun currentHeldGun, lastHeldGun;
        public override void Update()
        {
            if (Owner)
            {
                currentHeldGun = Owner.CurrentGun;
                if (currentHeldGun != lastHeldGun)
                {
                    if (Owner.CurrentGun.InfiniteAmmo && Owner.CurrentGun.quality != PickupObject.ItemQuality.A && Owner.CurrentGun.quality != PickupObject.ItemQuality.S)
                    {
                        GiveSynergyBoost();
                    }
                    else
                    {
                        RemoveSynergyBoost();
                    }
                    lastHeldGun = currentHeldGun;
                }
            }
            base.Update();
        }
        private void GiveSynergyBoost()
        {
            RemoveStat(PlayerStats.StatType.Damage);
            AddStat(PlayerStats.StatType.Damage, 3f, StatModifier.ModifyMethod.MULTIPLICATIVE);
            Owner.stats.RecalculateStats(Owner, true, false);

        }
        private void RemoveSynergyBoost()
        {
            RemoveStat(PlayerStats.StatType.Damage);
            Owner.stats.RecalculateStats(Owner, true, false);

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
    }
}