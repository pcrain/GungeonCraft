using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ItemAPI;
using UnityEngine;

namespace NevernamedsItems
{
    public class ItemCoupon : PlayerItem
    {
        public static void Init()
        {
            //The name of the item
            string itemName = "Coupon";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it
            string resourceName = "NevernamedsItems/Resources/shopcoupon_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a PassiveItem component to the object
            var item = obj.AddComponent<ItemCoupon>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "100th Lucky Gungeoneer";
            string longDesc = "Entitles you to one free item at most Gungeon based merchanteering establishments. Simply use the coupon, and select your item in the alloted time.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //Do this after ItemBuilder.AddSpriteToObject!
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.None, 500);


            //Set the rarity of the item
            item.quality = PickupObject.ItemQuality.B;

            List<string> mandatorySynergyItems = new List<string>() { "nn:coupon", "ring_of_miserly_protection" };
            CustomSynergies.Add("Livin' Off Discounts", mandatorySynergyItems);

        }

        float duration = 5;
        bool playerHasMiserlyRing = false;
        public override void DoEffect(PlayerController user)
        {
            if (user.HasPickupID(132))
            {
                playerHasMiserlyRing = true;
                //ETGModConsole.Log("You have the Miserly Ring without the Synergy");
            }
                //Play a sound effect
                //Activates the effect
                StartEffect(user);

            //start a coroutine which calls the EndEffect method when the item's effect duration runs out
            StartCoroutine(ItemBuilder.HandleDuration(this, duration, user, EndEffect));
        }

        bool couponActive = false;
        float priceChange = -1;
        private void StartEffect(PlayerController user)
        {
            //ETGModConsole.Log("You activated the Coupon.");
            couponActive = true;
            float curPriceMod = user.stats.GetBaseStatValue(PlayerStats.StatType.GlobalPriceMultiplier); //Get's the player's speed and stores it in a var called 'curMovement'
            float newPriceMod = curPriceMod * 0.001f; //Makes a variable named 'newMovement by multiplying 'curMovement' by 2
            user.stats.SetBaseStatValue(PlayerStats.StatType.GlobalPriceMultiplier, newPriceMod, user);
            //Give the player an extra active item slot
            float curActiveSlots = user.stats.GetBaseStatValue(PlayerStats.StatType.AdditionalItemCapacity);
            curActiveSlots += 1;
            user.stats.SetBaseStatValue(PlayerStats.StatType.AdditionalItemCapacity, curActiveSlots, user);
            priceChange = curPriceMod - newPriceMod;
        }
        private void EndEffect(PlayerController user)
        {
            if (priceChange <= 0) return;
            float curPriceMod = user.stats.GetBaseStatValue(PlayerStats.StatType.GlobalPriceMultiplier);
            float newPriceMod = curPriceMod + priceChange;
            user.stats.SetBaseStatValue(PlayerStats.StatType.GlobalPriceMultiplier, newPriceMod, user);
            //Remove the extra active item slot
            float curActiveSlots = user.stats.GetBaseStatValue(PlayerStats.StatType.AdditionalItemCapacity);
            curActiveSlots -= 1;
            user.stats.SetBaseStatValue(PlayerStats.StatType.AdditionalItemCapacity, curActiveSlots, user);
            priceChange = -1;
            couponActive = false;
            playerHasMiserlyRing = false;
        }
        private void OnItemPurchased(PlayerController player, ShopItemController obj)
        {
            //ETGModConsole.Log("You purchased an item");
            if (couponActive)
            {
                if (priceChange <= 0) return;
                float curPriceMod = player.stats.GetBaseStatValue(PlayerStats.StatType.GlobalPriceMultiplier);
                float newPriceMod = curPriceMod + priceChange;
                player.stats.SetBaseStatValue(PlayerStats.StatType.GlobalPriceMultiplier, newPriceMod, player);
                //Give the miserly ring                
                if (playerHasMiserlyRing == true && !player.HasPickupID(451))
                {
                    PickupObject byId = PickupObjectDatabase.GetById(132);
                    player.AcquirePassiveItemPrefabDirectly(byId as PassiveItem);
                    if (player.ForceZeroHealthState)
                    {
                        LootEngine.GivePrefabToPlayer(PickupObjectDatabase.GetById(120).gameObject, LastOwner);
                        LootEngine.GivePrefabToPlayer(PickupObjectDatabase.GetById(120).gameObject, LastOwner);
                    }
                }
                else if (playerHasMiserlyRing == true && player.HasPickupID(451))
                {
                    applyWeirdHealing();
                }
                //Remove the second active slot upon purchasing an item
                float curActiveSlots = player.stats.GetBaseStatValue(PlayerStats.StatType.AdditionalItemCapacity);
                curActiveSlots -= 1;
                player.stats.SetBaseStatValue(PlayerStats.StatType.AdditionalItemCapacity, curActiveSlots, player);
                priceChange = -1;
                player.RemoveActiveItem(this.PickupObjectId);
            }
        }

        private void applyWeirdHealing()
        {
            
                LastOwner.healthHaver.ApplyHealing(2);
            
            if (LastOwner.ForceZeroHealthState)
            {
                LootEngine.GivePrefabToPlayer(PickupObjectDatabase.GetById(120).gameObject, LastOwner);
                LootEngine.GivePrefabToPlayer(PickupObjectDatabase.GetById(120).gameObject, LastOwner);
            }
        }
        public override void OnPreDrop(PlayerController user)
        {
            if (base.IsCurrentlyActive)
            {
                base.IsCurrentlyActive = false; 
                EndEffect(user);
            }
        }
        public override void Pickup(PlayerController player)
        {
            player.OnItemPurchased += this.OnItemPurchased;
            base.Pickup(player);
            CanBeDropped = true;
        }
        public void Break()
        {
            this.m_pickedUp = true;
            UnityEngine.Object.Destroy(base.gameObject, 1f);
        }
        public DebrisObject Drop(PlayerController player)
        {
            DebrisObject debrisObject = base.Drop(player);
            player.OnItemPurchased -= this.OnItemPurchased;
            base.IsCurrentlyActive = false;
            EndEffect(player);
            ItemCoupon component = debrisObject.GetComponent<ItemCoupon>();
            component.Break();
            return debrisObject;
        }
    }
}
