using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using ItemAPI;

namespace NevernamedsItems
{
    public class HighVelocityRifle : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("High Velocity Rifle", "highvelocityrifle");
            Game.Items.Rename("outdated_gun_mods:high_velocity_rifle", "nn:high_velocity_rifle");
            var behav = gun.gameObject.AddComponent<HighVelocityRifle>();
            gun.SetShortDescription("Tracked");
            gun.SetLongDescription("Advanced tech designed for private security contractors on the fringes of space."+"\n\nHolding down fire allows the operator to enter 'aim time'.");

            gun.SetupSprite(null, "highvelocityrifle_idle_001", 8);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Charged;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.DefaultModule.cooldownTime = 1f;
            gun.DefaultModule.angleVariance = 0;
            gun.DefaultModule.numberOfShotsInClip = 1;
            gun.barrelOffset.transform.localPosition = new Vector3(1.87f, 0.31f, 0f);
            gun.SetBaseMaxAmmo(80);
            gun.ammo = 80;
            gun.gunClass = GunClass.RIFLE;

            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            projectile.baseData.damage *= 8f;
            projectile.baseData.force *= 3f;
            projectile.baseData.speed *= 6f;
            projectile.baseData.range *= 2f;
            PierceProjModifier pierce = projectile.gameObject.GetOrAddComponent<PierceProjModifier>();
            pierce.penetration++;
            pierce.penetratesBreakables = true;
            projectile.hitEffects.overrideMidairDeathVFX = EasyVFXDatabase.SmoothLightBlueLaserCircleVFX;
            projectile.hitEffects.alwaysUseMidair = true;
            projectile.SetProjectileSpriteRight("highvelocityrifle_projectile", 13, 5, true, tk2dBaseSprite.Anchor.MiddleCenter,11, 4);

            ProjectileModule.ChargeProjectile chargeProj = new ProjectileModule.ChargeProjectile
            {
                Projectile = projectile,
                ChargeTime = 0,
            };
            gun.DefaultModule.chargeProjectiles = new List<ProjectileModule.ChargeProjectile> { chargeProj };

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = "Toolgun Bullets";

            gun.quality = PickupObject.ItemQuality.A;
            ETGMod.Databases.Items.Add(gun, null, "ANY");
            HighVelocityRifleID = gun.PickupObjectId;
        }
        public static int HighVelocityRifleID;
        public HighVelocityRifle()
        {

        }
        private bool TimeIsFrozen = false;
        public override void OnSwitchedAwayFromThisGun()
        {
            if (TimeIsFrozen)
            {
                BraveTime.ClearMultiplier(base.gameObject);
                TimeIsFrozen = false;
            }
            base.OnSwitchedAwayFromThisGun();
        }
        protected override void Update()
        {
            if (gun.CurrentOwner && gun.CurrentOwner is PlayerController)
            {
                PlayerController owna = gun.CurrentOwner as PlayerController;
                if (owna.IsDodgeRolling)
                {
                    if (TimeIsFrozen)
                    {
                        BraveTime.ClearMultiplier(base.gameObject);
                        TimeIsFrozen = false;
                    }
                }
                if (gun.IsCharging)
                {
                    if (!TimeIsFrozen && !owna.IsDodgeRolling)
                    {
                        BraveTime.SetTimeScaleMultiplier(0.01f, base.gameObject);
                        TimeIsFrozen = true;
                    }
                }
                else
                {
                    if (TimeIsFrozen)
                    {
                        BraveTime.ClearMultiplier(base.gameObject);
                        TimeIsFrozen = false;
                    }
                }
            }
            base.Update();
        }
    }
}
