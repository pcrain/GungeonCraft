using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using ItemAPI;
using SaveAPI;

namespace NevernamedsItems
{
    public class Ringer : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Ringer", "ringer");
            Game.Items.Rename("outdated_gun_mods:ringer", "nn:ringer");
            gun.gameObject.AddComponent<Ringer>();
            gun.SetShortDescription("Been Through It");
            gun.SetLongDescription("Blasts you forwards with each shot, and has a chance to negate damage while held." + "\n\nWhoever made this must really have liked to go fast.");

            gun.SetupSprite(null, "ringer_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 10);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(647) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Automatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.DefaultModule.cooldownTime = 0.1f;
            gun.DefaultModule.numberOfShotsInClip = 40;
            gun.barrelOffset.transform.localPosition = new Vector3(2.18f, 0.43f, 0f);
            gun.SetBaseMaxAmmo(550);
            gun.ammo = 550;
            gun.gunClass = GunClass.FULLAUTO;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            projectile.baseData.damage *= 1.8f;
            projectile.baseData.force *= 1.2f;

            gun.DefaultModule.projectiles[0] = projectile;

            gun.quality = PickupObject.ItemQuality.C;
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            RingerID = gun.PickupObjectId;

            gun.SetupUnlockOnCustomFlag(CustomDungeonFlags.BEATEN_MINES_BOSS_TURBO_MODE, true);
        }
        public static int RingerID;
        private void ModifyDamage(HealthHaver player, HealthHaver.ModifyDamageEventArgs args)
        {
            if (player.gameActor is PlayerController && (player.gameActor as PlayerController).CurrentGun.PickupObjectId == RingerID)
            {
                if (UnityEngine.Random.value <= 0.25f)
                {
                    args.ModifiedDamage = 0;
                    float dir = 0;
                    for (int i = 0; i < 12; i++)
                    {
                        dir += 30;
                        GameObject gameObject = SpawnManager.SpawnProjectile(gun.DefaultModule.projectiles[0].gameObject, player.sprite.WorldCenter, Quaternion.Euler(0f, 0f, dir), true);
                        Projectile component = gameObject.GetComponent<Projectile>();
                        if (component != null)
                        {
                            component.Owner = Owner;
                            component.Shooter = Owner.specRigidbody;
                        }
                    }
                }
            }
        }
        protected override void OnPickedUpByPlayer(PlayerController player)
        {
            player.healthHaver.ModifyDamage += this.ModifyDamage;
            base.OnPickedUpByPlayer(player);
        }
        public override void OnDestroy()
        {
            if (gun && gun.CurrentOwner)
            {
                gun.CurrentOwner.healthHaver.ModifyDamage -= this.ModifyDamage;
            }
            base.OnDestroy();
        }
        protected override void OnPostDroppedByPlayer(PlayerController player)
        {
            player.healthHaver.ModifyDamage -= this.ModifyDamage;
            base.OnPostDroppedByPlayer(player);
        }
        public override void OnPostFired(PlayerController player, Gun gun)
        {
            player.knockbackDoer.ApplyKnockback(((player.sprite.WorldCenter - player.unadjustedAimPoint.XY()).normalized) * -1, 30f);
            base.OnPostFired(player, gun);
        }
        public Ringer()
        {

        }
    }
}
