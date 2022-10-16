using System;
using System.Collections.Generic;
using System.Linq;
using Dungeonator;
using System.Text;

using ItemAPI;
using UnityEngine;

namespace NevernamedsItems
{
    class Roulette : PlayerItem
    {
        public static int RouletteID;
        public static void Init()
        {
            string itemName = "Roulette";
            string resourceName = "NevernamedsItems/Resources/workinprogress_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<Roulette>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Spin To Win";
            string longDesc = "";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.None, 0);
            item.consumable = false;
            item.quality = ItemQuality.EXCLUDED; //A
            RouletteID = item.PickupObjectId;
        }

        public override bool CanBeUsed(PlayerController user)
        {
            return true;
        }
        public override void DoEffect(PlayerController user)
        {
            try
            {
                int gunsToGive = 0;
                int itemsToGive = 0;
                int activeItemsToGive = 0;

                cachedPassiveItemsToKillDrop.Clear();
                cachedGunsToDestroy.Clear();
                cachedActiveItemsToKillDrop.Clear();
                if (user.passiveItems.Count > 0)
                {
                    foreach (PassiveItem item in user.passiveItems)
                    {
                        bool itemNeedsToBeIgnored = passiveItemsToIgnore.Contains(item.PickupObjectId);
                        if (item.CanActuallyBeDropped(user) && !itemNeedsToBeIgnored)
                        {
                            itemsToGive += 1;
                            cachedPassiveItemsToKillDrop.Add(item);
                        }
                    }
                }
                if (user.inventory.AllGuns.Count > 0)
                {
                    foreach (Gun gun in user.inventory.AllGuns)
                    {
                        if (gun.CanActuallyBeDropped(user))
                        {
                            gunsToGive += 1;
                            cachedGunsToDestroy.Add(gun);
                        }
                    }
                }
                if (user.activeItems.Count > 0)
                {
                    foreach (PlayerItem item in user.activeItems)
                    {
                        bool itemNeedsToBeIgnored = passiveItemsToIgnore.Contains(item.PickupObjectId);
                        if (item.CanActuallyBeDropped(user) && !itemNeedsToBeIgnored)
                        {
                            activeItemsToGive += 1;
                            cachedActiveItemsToKillDrop.Add(item);
                        }
                    }
                }



                //ACTUALLY REMOVE ALL THE ITEMS
                if (cachedActiveItemsToKillDrop.Count > 0)
                {
                    for (int i = (cachedActiveItemsToKillDrop.Count - 1); i >= 0; i--)
                    {
                        if (cachedActiveItemsToKillDrop[i].PickupObjectId != RouletteID)
                        {
                            KillDropWeirdActive(cachedActiveItemsToKillDrop[i]);
                        }
                    }
                }
                if (cachedPassiveItemsToKillDrop.Count > 0)
                {
                    for (int i = (cachedPassiveItemsToKillDrop.Count - 1); i >= 0; i--)
                    {
                        KillDropWeirdItem(cachedPassiveItemsToKillDrop[i]);
                    }
                }
                if (cachedGunsToDestroy.Count > 0)
                {
                    for (int i = (cachedGunsToDestroy.Count - 1); i >= 0; i--)
                    {
                        user.inventory.RemoveGunFromInventory(cachedGunsToDestroy[i]);
                    }
                }

                user.stats.RecalculateStats(user);

                //GIVE ITEMS
                if (itemsToGive > 0)
                {
                    for (int i = 0; i < itemsToGive; i++)
                    {
                        PickupObject item = LootEngine.GetItemOfTypeAndQuality<PassiveItem>(PickupObject.ItemQuality.A, GameManager.Instance.RewardManager.ItemsLootTable, true);
                        LootEngine.GivePrefabToPlayer(item.gameObject, user);
                    }
                }
                user.stats.RecalculateStats(user);
                if (activeItemsToGive > 0)
                {
                    int amtOfOpenSlots = (int)user.stats.GetStatValue(PlayerStats.StatType.AdditionalItemCapacity);
                    if (amtOfOpenSlots > 0)
                    {
                        for (int i = 0; i < amtOfOpenSlots; i++)
                        {
                            PickupObject item = LootEngine.GetItemOfTypeAndQuality<PlayerItem>(PickupObject.ItemQuality.A, GameManager.Instance.RewardManager.ItemsLootTable, true);
                            LootEngine.GivePrefabToPlayer(item.gameObject, user);
                        }
                    }
                    for (int i = 0; i < (activeItemsToGive - amtOfOpenSlots); i++)
                    {
                        PickupObject item = LootEngine.GetItemOfTypeAndQuality<PassiveItem>(PickupObject.ItemQuality.A, GameManager.Instance.RewardManager.ItemsLootTable, true);
                        LootEngine.GivePrefabToPlayer(item.gameObject, user);
                    }
                }
                user.stats.RecalculateStats(user);
                for (int i = 0; i < gunsToGive; i++)
                {
                    PickupObject item = LootEngine.GetItemOfTypeAndQuality<Gun>(PickupObject.ItemQuality.A, GameManager.Instance.RewardManager.GunsLootTable, true);
                    LootEngine.GivePrefabToPlayer(item.gameObject, user);
                }
            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message);
                ETGModConsole.Log(e.StackTrace);
            }
        }
        public static List<PlayerItem> cachedActiveItemsToKillDrop = new List<PlayerItem>();
        public static List<PassiveItem> cachedPassiveItemsToKillDrop = new List<PassiveItem>();
        public static List<Gun> cachedGunsToDestroy = new List<Gun>();


        public static List<int> passiveItemsToIgnore = new List<int>()
        {
            467, //Master Round 5
            468, //Master Round 3
            469, //Master Round 1
            470, //Master Round 4
            471, //Master Round 2
            127, //Junk
            565, //Glass Guon Stone
            316, //Gnawed Key
            RouletteID, //Inevitus Itself
        };
        public void KillDropWeirdItem(PassiveItem item)
        {
            DebrisObject debrisObject = LastOwner.DropPassiveItem(item);
            UnityEngine.Object.Destroy(debrisObject.gameObject, 0.01f);
        }
        public void KillDropWeirdActive(PlayerItem item)
        {
            DebrisObject debrisObject = LastOwner.DropActiveItem(item);
            UnityEngine.Object.Destroy(debrisObject.gameObject, 0.01f);
        }
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);

        }


    }
}