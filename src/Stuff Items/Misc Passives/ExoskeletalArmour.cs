using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using UnityEngine;
using ItemAPI;
using SaveAPI;

namespace NevernamedsItems
{
    class ExoskeletalArmour : PassiveItem
    {
        public static void Init()
        {
            //The name of the item
            string itemName = "Meat Shield";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it
            string resourceName = "NevernamedsItems/Resources/meatshield_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a PassiveItem component to the object
            var item = obj.AddComponent<ExoskeletalArmour>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Self Sacrifice";
            string longDesc = "Causes health to take damage before armour, and gives a little of both.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //Do this after ItemBuilder.AddSpriteToObject!
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Adds the actual passive effect to the item            
            //Set the rarity of the item
            item.quality = PickupObject.ItemQuality.D;

            item.SetupUnlockOnCustomFlag(CustomDungeonFlags.PURCHASED_MEATSHIELD, true);
            item.AddItemToDougMetaShop(15);
            item.ArmorToGainOnInitialPickup = 2;
            MeatShieldID = item.PickupObjectId;
        }
        public static int MeatShieldID;
        public override void Update()
        {
            if (Owner)
            {
                if (!Owner.ForceZeroHealthState)
                {
                    if (!Owner.healthHaver.NextDamageIgnoresArmor && Owner.healthHaver.GetCurrentHealth() >= 0.5f)
                    {
                        Owner.healthHaver.NextDamageIgnoresArmor = true;
                    }
                }
            }
        }
        public override void Pickup(PlayerController player)
        {
            if (!this.m_pickedUpThisRun)
            {
                player.healthHaver.ApplyHealing(1);
            }
            base.Pickup(player);
        }
    }
}
