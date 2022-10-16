using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ItemAPI;

namespace NevernamedsItems
{
    class LootEngineItem : PlayerItem
    {
        public static void Init()
        {
            string itemName = "Loot Engine";
            string resourceName = "NevernamedsItems/Resources/lootengine_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<LootEngineItem>();


            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Lengine, if you will";
            string longDesc = "Rerolls all consumables in the room into other consumables. Also works on Glass Guon Stones, and Junk." + "\n\nRumour has it that a much larger version of this machine is responsible for handling the Gungeon's notoriously stingy loot system.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.Damage, 500);

            item.consumable = false;
            item.quality = ItemQuality.C;
        }

        public override void DoEffect(PlayerController user)
        {    
            pickupsInRoom.Clear();
            DebrisObject[] shitOnGround = FindObjectsOfType<DebrisObject>();
            foreach (DebrisObject debris in shitOnGround)
            {
                bool isValid = DetermineIfValid(debris);
                if (isValid && debris.transform.position.GetAbsoluteRoom() == user.CurrentRoom)
                {
                    pickupsInRoom.Add(debris);
                }
            }
            if (pickupsInRoom.Count != 0)
            {
                DoReroll();
            }
        }
        private void DoReroll()
        {
            foreach (DebrisObject debris in pickupsInRoom)
            {
                Vector2 pos = debris.transform.position;
                LootEngine.SpawnItem(PickupObjectDatabase.GetById(BraveUtility.RandomElement(validIDs)).gameObject, pos, Vector2.zero, 1f, false, true, false);
                Destroy(debris.gameObject);
            }
        }
        private bool DetermineIfValid(DebrisObject thing)
        {
            PickupObject itemness = thing.gameObject.GetComponent<PickupObject>();
            if (itemness != null)
            {
                if (itemness.PickupObjectId == 127 || validIDs.Contains(itemness.PickupObjectId))
                {
                    return true;
                }
                else return false;
            }
            else return false;
        }
        private List<DebrisObject> pickupsInRoom = new List<DebrisObject>()
        { };
        public static List<int> validIDs = new List<int>()
        {
            78, //Ammo
            600, //Spread Ammo
            565, //Glass Guon Stone
            73, //Half Heart
            85, //Heart
            120, //Armor
            224, //Blank
            67, //Key
        };
        public override bool CanBeUsed(PlayerController user)
        {
            return base.CanBeUsed(user);
        }
    }
}