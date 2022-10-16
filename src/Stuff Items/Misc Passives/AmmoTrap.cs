using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using ItemAPI;
using UnityEngine;
using MonoMod.RuntimeDetour;
using System.Reflection;
using SaveAPI;
using Alexandria.Misc;

namespace NevernamedsItems
{
    public class AmmoTrap : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Ammo Trap";
            string resourceName = "NevernamedsItems/Resources/ammotrap_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<AmmoTrap>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Clickity Clack";
            string longDesc = "Prevents ammo from being stolen by the dastardly Rat, and even causes him to drop some of his own upon going in for the grabby."+"\n\nWhy didn't we think of this sooner.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.B;
            item.SetupUnlockOnCustomFlag(CustomDungeonFlags.ALLJAMMED_BEATEN_RAT, true);
            AmmoTrapID = item.PickupObjectId;

            ammoSpawnHook = new Hook(
                    typeof(LootEngine).GetMethod("PostprocessItemSpawn", BindingFlags.Static | BindingFlags.NonPublic),
                    typeof(AmmoTrap).GetMethod("doEffect", BindingFlags.Static | BindingFlags.Public)
                );
        }
        public static List<int> bannedIDs = new List<int>()
        {
            78, //Ammo
            600, //Spread Ammo
            565, //Glass Guon Stone
            120, //Armor
            224, //Blank
            67, //Key
        };
        public static void doEffect(Action<DebrisObject> orig, DebrisObject spawnedItem)
        {
            try
            {
                orig(spawnedItem);
                if (GameManager.Instance.AnyPlayerHasPickupID(AmmoTrapID))
                {
                    AmmoPickup itemness = spawnedItem.gameObject.GetComponent<AmmoPickup>();
                    if (itemness != null)
                    {
                        itemness.IgnoredByRat = true;
                    }
                }
            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message);
                ETGModConsole.Log(e.StackTrace);
            }
        }
        float lastCheckedRatItemStoleded;
        public override void Update()
        {
            if (GameStatsManager.Instance.GetPlayerStatValue(TrackedStats.ITEMS_TAKEN_BY_RAT) != lastCheckedRatItemStoleded)
            {
                lastCheckedRatItemStoleded = GameStatsManager.Instance.GetPlayerStatValue(TrackedStats.ITEMS_TAKEN_BY_RAT);
                DoAmmoSpawn();
            }
            base.Update();
        }
        private void DoAmmoSpawn()
        {
            TalkDoerLite[] allChests = FindObjectsOfType<TalkDoerLite>();
            foreach (TalkDoerLite chest in allChests)
            {
                if (chest.name.Contains("ResourcefulRat_Thief"))
                {
                    LootEngine.SpawnItem(PickupObjectDatabase.GetById(78).gameObject, chest.transform.position, Vector2.zero, 0);
                }
            }
        }
        public override void Pickup(PlayerController player)
        {
            lastCheckedRatItemStoleded = GameStatsManager.Instance.GetPlayerStatValue(TrackedStats.ITEMS_TAKEN_BY_RAT);
            base.Pickup(player);
            if (!m_pickedUpThisRun)
            {
                foreach(Gun gun in player.inventory.AllGuns)
                {
                    if (gun.CanGainAmmo)
                    {
                        gun.GainAmmo(gun.AdjustedMaxAmmo);
                    }
                }
            }
        }
        public static Hook ammoSpawnHook;
        public static int AmmoTrapID;
    }
}
