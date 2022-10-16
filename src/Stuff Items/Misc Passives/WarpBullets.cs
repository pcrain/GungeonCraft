using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Alexandria.ItemAPI;
using UnityEngine;
using MonoMod.RuntimeDetour;
using System.Reflection;

namespace NevernamedsItems
{
    public class WarpBullets : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Warp Bullets";
            string resourceName = "NevernamedsItems/Resources/warpbullets_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<WarpBullets>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Bullets Teleport";
            string longDesc = "Your bullets have a chance to teleport behind enemies.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.C;
            item.SetTag("bullet_modifier");
        }

        private void PostProcessProjectile(Projectile sourceProjectile, float effectChanceScalar)
        {
            if (Owner)
            {
                float procChance = 0.20f;
                if (Owner.PlayerHasActiveSynergy("Corrupted By Warp")) procChance *= effectChanceScalar;
                try
                {
                    if (UnityEngine.Random.value <= procChance)
                    {
                        GameObject BaseEnemyRogueBullet = EnemyDatabase.GetOrLoadByGuid("56fb939a434140308b8f257f0f447829").bulletBank.GetBullet("rogue").BulletObject;
                        Projectile BaseEnemyProjectileComponent = BaseEnemyRogueBullet.GetComponent<Projectile>();
                        if (BaseEnemyProjectileComponent != null)
                        {
                            TeleportProjModifier tp = BaseEnemyProjectileComponent.GetComponent<TeleportProjModifier>();
                            if (tp != null)
                            {
                                PlayerProjectileTeleportModifier rogueTeleport = sourceProjectile.gameObject.AddComponent<PlayerProjectileTeleportModifier>();
                                rogueTeleport.teleportVfx = tp.teleportVfx;
                                rogueTeleport.teleportCooldown = tp.teleportCooldown;
                                rogueTeleport.teleportPauseTime = tp.teleportPauseTime;
                                rogueTeleport.trigger = PlayerProjectileTeleportModifier.TeleportTrigger.DistanceFromTarget;
                                rogueTeleport.distToTeleport = tp.distToTeleport * 2f;
                                rogueTeleport.behindTargetDistance = tp.behindTargetDistance *= 0.9f;
                                rogueTeleport.leadAmount = tp.leadAmount;
                                rogueTeleport.minAngleToTeleport = tp.minAngleToTeleport;
                                rogueTeleport.numTeleports = 2;
                                rogueTeleport.type = PlayerProjectileTeleportModifier.TeleportType.BehindTarget;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    ETGModConsole.Log(e.Message);
                }
            }
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject debrisObject = base.Drop(player);
            player.PostProcessProjectile -= this.PostProcessProjectile;
            return debrisObject;
        }
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.PostProcessProjectile += this.PostProcessProjectile;
        }
        public override void OnDestroy()
        {
            if (Owner)
            {
                Owner.PostProcessProjectile -= this.PostProcessProjectile;
            }
            base.OnDestroy();
        }
    }
}
