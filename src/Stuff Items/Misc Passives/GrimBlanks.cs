using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Alexandria.ItemAPI;
using UnityEngine;
using MonoMod.RuntimeDetour;
using System.Reflection;
using Alexandria.EnemyAPI;

namespace NevernamedsItems
{
    public class GrimBlanks : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Grim Blanks";
            string resourceName = "NevernamedsItems/Resources/grimblanks_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<GrimBlanks>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Bullets Die With You";
            string longDesc = "Killing an enemy erases all of their bullets."+"\n\nThese special blanks are subtle, quiet, and highly targeted.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.D;
        }
        private void OnEnemyKilled(PlayerController player, HealthHaver enemy)
        {
            if (enemy && enemy.aiActor)
            {
                enemy.aiActor.DeleteOwnedBullets(1, true);
            }
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject debrisObject = base.Drop(player);
            player.OnKilledEnemyContext -= OnEnemyKilled;
            return debrisObject;
        }
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.OnKilledEnemyContext += OnEnemyKilled;
        }
        public override void OnDestroy()
        {
            if (Owner)
            {
                Owner.OnKilledEnemyContext -= OnEnemyKilled;
            }
            base.OnDestroy();
        }
    }
}
