using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using ItemAPI;
using SaveAPI;
using Gungeon;

namespace NevernamedsItems
{
    class AmuletOfShelltan : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Amulet of Shell'tan";
            string resourceName = "NevernamedsItems/Resources/amuletofshelltan_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<AmuletOfShelltan>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Promise of Ammo";
            string longDesc = "All bosses drop ammo." + "\n\nThis pendant denotes devotion to the elemental lord of ammunition, Shell'tan.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.D;

            item.AddToSubShop(ItemBuilder.ShopType.Cursula);

            Game.Items.Rename("nn:amulet_of_shell'tan", "nn:amulet_of_shelltan");
        }

        private void OnEnemyDamaged(float damage, bool fatal, HealthHaver enemy)
        {
            if (enemy.aiActor && enemy.IsBoss && fatal == true)
            {
                if (UnityEngine.Random.value <= 0.5f)
                {
                    LootEngine.SpawnItem(PickupObjectDatabase.GetById(78).gameObject, enemy.specRigidbody.UnitCenter, Vector2.zero, 0);
                }
                else
                {
                    LootEngine.SpawnItem(PickupObjectDatabase.GetById(600).gameObject, enemy.specRigidbody.UnitCenter, Vector2.zero, 0);
                }
            }

        }
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.OnAnyEnemyReceivedDamage += this.OnEnemyDamaged;
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject debrisObject = base.Drop(player);
            player.OnAnyEnemyReceivedDamage -= this.OnEnemyDamaged;
            return debrisObject;
        }
        public override void OnDestroy()
        {
            if (Owner)
            {
                Owner.OnAnyEnemyReceivedDamage -= this.OnEnemyDamaged;
            }
            base.OnDestroy();
        }
    }
}