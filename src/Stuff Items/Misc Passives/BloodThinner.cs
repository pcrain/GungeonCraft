using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ItemAPI;
using MonoMod.RuntimeDetour;
using System.Reflection;

namespace NevernamedsItems
{
    public class BloodThinner : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Blood Thinner";
            string resourceName = "NevernamedsItems/Resources/bloodthinner_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<BloodThinner>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Unnecessary Necessities";
            string longDesc = "Turns hearts into other consumables at full HP. Does not affect shops." + "\n\nPrevents blood clots. If you experience side effects such as light headedness, vomiting, nausea, projectile dysfunction, drowsiness, decreased apetite, or death, consult your doctor immediately.";

            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            item.CanBeDropped = true;
            item.quality = PickupObject.ItemQuality.C;

            Hook itemSpawnHook = new Hook(
                    typeof(LootEngine).GetMethod("PostprocessItemSpawn", BindingFlags.Static | BindingFlags.NonPublic),
                    typeof(BloodThinner).GetMethod("doEffect", BindingFlags.Static | BindingFlags.Public)
                );

            BloodThinnerID = item.PickupObjectId;
        }
        public static int BloodThinnerID;
        public static List<int> NonHeartLoot = new List<int>()
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
                PickupObject itemness = spawnedItem.gameObject.GetComponent<PickupObject>();
                if (itemness != null)
                {
                    if (itemness.PickupObjectId == 73 || itemness.PickupObjectId == 85)
                    {
                        AttemptReroll(itemness);
                    }
                }
            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message);
                ETGModConsole.Log(e.StackTrace);
            }
        }
        public static void AttemptReroll(PickupObject thing)
        {
            bool shouldRerollHearts = false;
            PlayerController owner = null;
            if (GameManager.Instance.PrimaryPlayer.HasPickupID(BloodThinnerID)) { owner = GameManager.Instance.PrimaryPlayer; }
            else if (GameManager.Instance.SecondaryPlayer != null && GameManager.Instance.SecondaryPlayer.HasPickupID(BloodThinnerID)) { owner = GameManager.Instance.SecondaryPlayer; }
            if (owner != null)
            {
                if (owner.healthHaver.GetCurrentHealthPercentage() == 1 || owner.ForceZeroHealthState) shouldRerollHearts = true;
                if (shouldRerollHearts)
                {
                    Vector2 pos = thing.transform.position;
                    bool doDoubleLoot = false;
                    if (thing.PickupObjectId == 85 && owner.PlayerHasActiveSynergy("Thicker Than Water")) doDoubleLoot = true;
                    LootEngine.SpawnItem(PickupObjectDatabase.GetById(BraveUtility.RandomElement(NonHeartLoot)).gameObject, pos, Vector2.zero, 1f, false, true, false);
                    if (doDoubleLoot) LootEngine.SpawnItem(PickupObjectDatabase.GetById(BraveUtility.RandomElement(NonHeartLoot)).gameObject, pos, Vector2.zero, 1f, false, true, false);
                    Destroy(thing.gameObject);
                }
            }
        }
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            
        }

        public override DebrisObject Drop(PlayerController player)
        {
            return base.Drop(player);
        }
    }

}