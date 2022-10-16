using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using ItemAPI;

namespace NevernamedsItems
{
    public class KeyBullwark : PassiveItem
    {
        public static void Init()
        {
            //The name of the item
            string itemName = "Key Bulwark";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it
            string resourceName = "NevernamedsItems/Resources/keybulwark_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a PassiveItem component to the object
            var item = obj.AddComponent<KeyBullwark>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Keyfensive Maneuver";
            string longDesc = "Converts all your keys into armour upon entering a new floor. Every key converted gives a small, permanent damage upgrade.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //Do this after ItemBuilder.AddSpriteToObject!
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Adds the actual passive effect to the item

            //Set the rarity of the item
            item.quality = PickupObject.ItemQuality.B;
            item.AddToSubShop(ItemBuilder.ShopType.Flynt);
          KeyBulwarkID =  item.PickupObjectId;
        }
        public static int KeyBulwarkID;
        private void OnNewFloor()
        {
            PlayerController player = this.Owner;
            int currentKeys = player.carriedConsumables.KeyBullets;
            player.healthHaver.Armor += currentKeys;
            float keysToDamage = currentKeys * 0.05f;
            float curDamage = player.stats.GetBaseStatValue(PlayerStats.StatType.Damage);
            float damageToGive = curDamage + keysToDamage;
            player.stats.SetBaseStatValue(PlayerStats.StatType.Damage, damageToGive, player);
            player.carriedConsumables.KeyBullets -= currentKeys;
        }

        public override void Pickup(PlayerController player)
        {
            bool hasntAlreadyBeenCollected = !this.m_pickedUpThisRun;
            if (hasntAlreadyBeenCollected)
            {
                LootEngine.GivePrefabToPlayer(PickupObjectDatabase.GetById(67).gameObject, player);
            }
            GameManager.Instance.OnNewLevelFullyLoaded += this.OnNewFloor;
            base.Pickup(player);
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject result = base.Drop(player);
            GameManager.Instance.OnNewLevelFullyLoaded -= this.OnNewFloor;
            return result;
        }
        public override void OnDestroy()
        {
            GameManager.Instance.OnNewLevelFullyLoaded -= this.OnNewFloor;
            base.OnDestroy();
        }
    }
}
