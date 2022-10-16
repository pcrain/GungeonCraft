using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dungeonator;
using UnityEngine;
using ItemAPI;
using SaveAPI;

namespace NevernamedsItems
{
    class CheeseHeart : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Cheese Heart";
            string resourceName = "NevernamedsItems/Resources/cheeseheart_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<CheeseHeart>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Eat Your Heart Out";
            string longDesc = "Sprays cheese everywhere on hit."+"\n\nCarefully sculpted, and completely anatomically correct!";

            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            item.quality = PickupObject.ItemQuality.C;
            item.SetupUnlockOnCustomFlag(CustomDungeonFlags.FAILEDRATMAZE, true);

            CheeseHeartID = item.PickupObjectId;
        }
        public static int CheeseHeartID;
        public override void Pickup(PlayerController player)
        {
            player.OnReceivedDamage += OnHitEffect;
            base.Pickup(player);
        }
        public override void DisableEffect(PlayerController player)
        {
            player.OnReceivedDamage -= OnHitEffect;
            base.DisableEffect(player);
        }       
        private void OnHitEffect(PlayerController user)
        {
            DeadlyDeadlyGoopManager.GetGoopManagerForGoopType(EasyGoopDefinitions.CheeseDef).TimedAddGoopCircle(Owner.sprite.WorldCenter, 10, 1, false);
        }
    }
}
