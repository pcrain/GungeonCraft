using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using ItemAPI;

namespace NevernamedsItems
{
    public class BagOfHolding : PassiveItem
    {
        public static void Init()
        {
            //The name of the item
            string itemName = "Bag of Holding";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it
            string resourceName = "NevernamedsItems/Resources/bagofholding_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a PassiveItem component to the object
            var item = obj.AddComponent<BagOfHolding>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Space Gallore";
            string longDesc = "Drastically increases active item storage." + "\n\nThe mad wizard Alben Smallbore theorised that bags such as these could be turned into violent explosive devices if they were ever punctured. Sadly, his research was never realised.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //Do this after ItemBuilder.AddSpriteToObject!
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Adds the actual passive effect to the item
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.AdditionalItemCapacity, 10f, StatModifier.ModifyMethod.ADDITIVE);

            //Set the rarity of the item
            item.quality = PickupObject.ItemQuality.A;

            item.AddToSubShop(ItemBuilder.ShopType.Cursula);
        }
        public override void Pickup(PlayerController player)
        {
            bool pickemmed = m_pickedUpThisRun;
            base.Pickup(player);
            if (!pickemmed)
            {
                PlayerItem itemOfTypeAndQuality = LootEngine.GetItemOfTypeAndQuality<PlayerItem>(ItemQuality.A, GameManager.Instance.RewardManager.ItemsLootTable, true);
                LootEngine.TryGivePrefabToPlayer(itemOfTypeAndQuality.gameObject, Owner, false);
                //LootEngine.SpawnItem(itemOfTypeAndQuality.gameObject, LastOwner.specRigidbody.UnitCenter, Vector2.left, 1f, false, true, false);
            }
        }
    }

}
