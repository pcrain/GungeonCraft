using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Alexandria.ItemAPI;
using SaveAPI;

namespace NevernamedsItems
{
    class NitroBullets : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Nitro Bullets";
            string resourceName = "NevernamedsItems/Resources/nitrobullets_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<NitroBullets>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Badda Bing...";
            string longDesc = "50% chance for enemies to explode violently on death."+"\n\nMade by a lunatic who loved the way the ground shook when he used his special brand of... making things go away."+"\n\nYou are not immune to these explosions. You have been warned.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.C;
            item.AddToSubShop(ItemBuilder.ShopType.Trorc);
            item.SetTag("bullet_modifier");
            item.SetupUnlockOnCustomFlag(CustomDungeonFlags.PURCHASED_NITROBULLETS, true);
            item.AddItemToDougMetaShop(15);
            NitroBulletsID = item.PickupObjectId;
        }
        public static int NitroBulletsID;
        bool hasSynergy;

        private void OnEnemyDamaged(float damage, bool fatal, HealthHaver enemyHealth)
        {
            if (Owner.HasPickupID(304) || Owner.HasPickupID(Gungeon.Game.Items["nn:nitroglycylinder"].PickupObjectId))
            {
                hasSynergy = true;
            }
            else
            {
                hasSynergy = false;
            }
            
            if (enemyHealth.aiActor && enemyHealth && !enemyHealth.IsBoss && fatal && enemyHealth.aiActor.IsNormalEnemy && hasSynergy == false)
            {
                if (UnityEngine.Random.value < .50f) Exploder.DoDefaultExplosion(enemyHealth.aiActor.CenterPosition, new Vector2());
            }
            else if (enemyHealth && !enemyHealth.IsBoss && fatal && enemyHealth.aiActor && enemyHealth.aiActor.IsNormalEnemy && hasSynergy == true)
            {
                Exploder.DoDefaultExplosion(enemyHealth.aiActor.CenterPosition, new Vector2());
            }

        }
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.OnAnyEnemyReceivedDamage += this.OnEnemyDamaged;
        }
        public override DebrisObject Drop(PlayerController player)
        {
            player.OnAnyEnemyReceivedDamage -= this.OnEnemyDamaged;
            return base.Drop(player);
        }
        public override void OnDestroy()
        {
            if (Owner) Owner.OnAnyEnemyReceivedDamage -= this.OnEnemyDamaged;
            base.OnDestroy();
        }
    }
}
