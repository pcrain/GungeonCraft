using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using UnityEngine;
using MonoMod;
using MonoMod.RuntimeDetour;
using Gungeon;
using Alexandria.Misc;
using Alexandria.ItemAPI;

namespace CwaffingTheGungy
{
    public class GamblersFallacy : AdvancedGunBehavior
    {
        public static string gunName          = "Gambler's Fallacy";
        public static string spriteName       = "grandfatherglock";
        public static string projectileName   = "ak-47";
        public static string shortDescription = "This Time for Sure!";
        public static string longDescription  = "(1/30 chance of exploding and self-destructing, taking health in the process)";

        private static VFXPool vfx = null;

        public static void Add()
        {
            Gun gun = Lazy.InitGunFromStrings(gunName, spriteName, projectileName, shortDescription, longDescription);
            var comp = gun.gameObject.AddComponent<GamblersFallacy>();

            gun.gunSwitchGroup                    = (PickupObjectDatabase.GetById(198) as Gun).gunSwitchGroup;
            gun.DefaultModule.ammoCost            = 1;
            gun.DefaultModule.shootStyle          = ProjectileModule.ShootStyle.Automatic;
            gun.DefaultModule.sequenceStyle       = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime                        = 1.1f;
            gun.DefaultModule.angleVariance       = 15.0f;
            gun.DefaultModule.cooldownTime        = 0.4f;
            gun.DefaultModule.numberOfShotsInClip = 8;
            gun.quality                           = PickupObject.ItemQuality.D;
            gun.InfiniteAmmo                      = true;
            gun.SetBaseMaxAmmo(800);
            gun.SetAnimationFPS(gun.shootAnimation, 24);

            Projectile projectile       = Lazy.PrefabProjectileFromGun(gun);
            projectile.baseData.damage  = 3f;
            projectile.baseData.speed   = 20.0f;
            projectile.transform.parent = gun.barrelOffset;
        }

        public override void OnPostFired(PlayerController player, Gun gun)
        {
            base.OnPostFired(player, gun);
            if (gun && gun.GunPlayerOwner())
            {
                int percent = UnityEngine.Random.Range(0, 100);
                if (percent < 1)
                {
                    ETGModConsole.Log("1 in 100 O:");
                    player.inventory.DestroyCurrentGun();
                }
                else if (percent < 3)
                {
                    ETGModConsole.Log("1 in 33 O:");
                    // deal damage
                    vfx ??= VFX.CreatePoolFromVFXGameObject(
                        (PickupObjectDatabase.GetById(0) as Gun).DefaultModule.projectiles[0].hitEffects.overrideMidairDeathVFX);

                    Vector2 position = player.sprite.WorldCenter;
                    for (int i = 0; i < 4; ++i)
                    {
                        Vector2 finalpos = position + BraveMathCollege.DegreesToVector(90*i,1);
                        vfx.SpawnAtPosition(
                            finalpos.ToVector3ZisY(-1f), /* -1 = above player sprite */
                            90*i, null, null, null, -0.05f);
                    }

                     player.healthHaver.ApplyDamage(0.5f, Vector2.zero, "Gambling Addiction :/", CoreDamageTypes.None, DamageCategory.Normal, true, null, false);
                }
                else if (percent < 10)
                {
                    ETGModConsole.Log("1 in 10 O:");
                    var enemyToSpawn = EnemyDatabase.GetOrLoadByGuid(EnemyGuidDatabase.Entries["gunreaper"]);
                    Vector2 position = player.sprite.WorldCenter;
                    AIActor TargetActor = AIActor.Spawn(
                        enemyToSpawn, position, GameManager.Instance.Dungeon.data.GetAbsoluteRoomFromPosition(
                            position.ToIntVector2()), true, AIActor.AwakenAnimationType.Default, true);
                }
            }
        }

    }
}
