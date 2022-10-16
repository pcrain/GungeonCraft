using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using UnityEngine;
using Alexandria.ItemAPI;
using SaveAPI;

namespace NevernamedsItems
{
    public class MinersBullets : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Miners Bullets";
            string resourceName = "NevernamedsItems/Resources/minersbullets_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<MinersBullets>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "So we back in the mine";
            string longDesc = "Allows for the effortless destruction of cubes.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.D;
            item.SetTag("bullet_modifier");
            item.SetupUnlockOnCustomFlag(CustomDungeonFlags.PURCHASED_MINERSBULLETS, true);
            item.AddItemToDougMetaShop(8);
        }
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.PostProcessProjectile += this.PostProcessProjectile;
            player.PostProcessBeam += this.PostProcessBeam;
            player.OnAnyEnemyReceivedDamage += this.OnEnemyDamaged;
        }

        private void PostProcessProjectile(Projectile sourceProjectile, float effectChanceScalar)
        {
            ProjectileInstakillBehaviour instakill = sourceProjectile.gameObject.GetOrAddComponent<ProjectileInstakillBehaviour>();
            instakill.tagsToKill.AddRange(new List<string>() { "sliding_cube", "cube_blobulon" });
            if (Owner.PlayerHasActiveSynergy("Eye of the Spider")) instakill.enemyGUIDsToKill.Add(EnemyGuidDatabase.Entries["phaser_spider"]);
        }
        private void PostProcessBeam(BeamController sourceBeam)
        {
            if (sourceBeam.projectile) this.PostProcessProjectile(sourceBeam.projectile, 1);
        }
        private void OnEnemyDamaged(float damage, bool fatal, HealthHaver enemyHealth)
        {
            if (fatal && enemyHealth && enemyHealth.aiActor && Owner && Owner.PlayerHasActiveSynergy("Miiiining Away~"))
            {
                string enemyGuid = enemyHealth.aiActor.EnemyGuid;
                if (enemyGuid == "98ca70157c364750a60f5e0084f9d3e2" && Owner.PlayerHasActiveSynergy("Eye of the Spider")) LootEngine.SpawnCurrency(enemyHealth.sprite.WorldCenter, 5);
                else if (enemyHealth.aiActor.HasTag("sliding_cube") || enemyHealth.aiActor.HasTag("cube_blobulon")) LootEngine.SpawnCurrency(enemyHealth.sprite.WorldCenter, 5);
            }
        }
        public override void DisableEffect(PlayerController player)
        {
            player.PostProcessProjectile -= this.PostProcessProjectile;
            player.PostProcessBeam -= this.PostProcessBeam;
            player.OnAnyEnemyReceivedDamage -= this.OnEnemyDamaged;
            base.DisableEffect(player);
        }       
    }
}

