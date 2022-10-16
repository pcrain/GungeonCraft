using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ItemAPI;
using SaveAPI;
using UnityEngine;

namespace NevernamedsItems
{
    class Recyclinder : PlayerItem
    {
        //Call this method from the Start() method of your ETGModule extension class
        public static void Init()
        {
            //The name of the item
            string itemName = "Recyclinder";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it.
            string resourceName = "NevernamedsItems/Resources/recyclinder_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a ActiveItem component to the object
            var item = obj.AddComponent<Recyclinder>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Lean Green Machine";
            string longDesc = "An environmentally friendly alternative to the methane-belching Gun Munchers, the Recyclinder uses proprietary technology to convert guns into items of equal quality. Waste not, want not." + "\n\nIt's probably nanites. It's ALWAYS nanites with these things, right? I'm not just going cazy?";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //"kts" here is the item pool. In the console you'd type kts:sweating_bullets
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Set the cooldown type and duration of the cooldown
            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.Damage, 200);

            //Adds a passive modifier, like curse, coolness, damage, etc. to the item. Works for passives and actives.


            //Set some other fields
            item.consumable = false;
            item.quality = ItemQuality.A;
            item.AddToSubShop(ItemBuilder.ShopType.Trorc);
            item.SetupUnlockOnCustomFlag(CustomDungeonFlags.PURCHASED_RECYCLINDER, true);
            RecyclinderID = item.PickupObjectId;
        }
        public static int RecyclinderID;

        PickupObject.ItemQuality itemToGiveQuality = PickupObject.ItemQuality.D;
        public override void DoEffect(PlayerController user)
        {
            if (user.CharacterUsesRandomGuns)
            {
                itemToGiveQuality = PickupObject.ItemQuality.D;
                spawnRecycledItem();
                itemToGiveQuality = PickupObject.ItemQuality.C;
                spawnRecycledItem();
                itemToGiveQuality = PickupObject.ItemQuality.B;
                spawnRecycledItem();
                itemToGiveQuality = PickupObject.ItemQuality.A;
                spawnRecycledItem();
                itemToGiveQuality = PickupObject.ItemQuality.S;
                spawnRecycledItem();
                user.RemoveActiveItem(this.PickupObjectId);
            }
            else
            {
                if (user.CurrentGun.CanActuallyBeDropped(user))
                {
                    Gun currentGun = user.CurrentGun;
                    PickupObject.ItemQuality itemQuality = currentGun.quality;
                    user.inventory.DestroyCurrentGun();
                    if (currentGun.quality == PickupObject.ItemQuality.D)
                    {
                        itemToGiveQuality = PickupObject.ItemQuality.D;
                        spawnRecycledItem();
                    }
                    else if (currentGun.quality == PickupObject.ItemQuality.C)
                    {
                        itemToGiveQuality = PickupObject.ItemQuality.C;
                        spawnRecycledItem();
                    }
                    else if (currentGun.quality == PickupObject.ItemQuality.B)
                    {
                        itemToGiveQuality = PickupObject.ItemQuality.B;
                        spawnRecycledItem();
                    }
                    else if (currentGun.quality == PickupObject.ItemQuality.A)
                    {
                        itemToGiveQuality = PickupObject.ItemQuality.A;
                        spawnRecycledItem();
                    }
                    else if (currentGun.quality == PickupObject.ItemQuality.S)
                    {
                        itemToGiveQuality = PickupObject.ItemQuality.S;
                        spawnRecycledItem();
                    }
                    else
                    {
                        LootEngine.SpawnItem(PickupObjectDatabase.GetById(127).gameObject, LastOwner.specRigidbody.UnitCenter, Vector2.zero, 1f, false, true, false);
                    }
                }
                else return;
            }
        }

        private void spawnRecycledItem()
        {
            PickupObject itemOfTypeAndQuality = LootEngine.GetItemOfTypeAndQuality<PickupObject>(itemToGiveQuality, GameManager.Instance.RewardManager.ItemsLootTable, false);
            LootEngine.SpawnItem(itemOfTypeAndQuality.gameObject, LastOwner.specRigidbody.UnitCenter, Vector2.left, 1f, false, true, false);
        }
        public override bool CanBeUsed(PlayerController user)
        {
            if (user.CurrentGun.CanActuallyBeDropped(user))
            {
                return true;
            }
            else
            {
                return false;
            }

        }
    }
}
