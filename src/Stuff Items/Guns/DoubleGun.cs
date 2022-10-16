using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using Alexandria.ItemAPI;
using Alexandria.Misc;

namespace NevernamedsItems
{

    public class DoubleGun : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Double Gun", "doublegun");
            Game.Items.Rename("outdated_gun_mods:double_gun", "nn:double_gun");
            gun.gameObject.AddComponent<DoubleGun>();
            gun.SetShortDescription("Better Than One");
            gun.SetLongDescription("The result of a one night stand between a shotgun and a revolver. Fires two shots at once.");

            gun.SetupSprite(null, "doublegun_idle_001", 8);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(51) as Gun).gunSwitchGroup;
            gun.SetAnimationFPS(gun.shootAnimation, 15);
            gun.SetAnimationFPS(gun.reloadAnimation, 13);
            gun.SetAnimationFPS(gun.idleAnimation, 5);

            for (int i = 0; i < 2; i++)
            {
                gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(181) as Gun, true, false);
            }
            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(23) as Gun).muzzleFlashEffects;
            //GUN STATS
            foreach (ProjectileModule mod in gun.Volley.projectiles)
            {

                mod.ammoCost = 1;
                mod.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
                mod.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
                mod.cooldownTime = 0.25f;
                mod.angleVariance = 1f;
                mod.numberOfShotsInClip = 3;
                Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(mod.projectiles[0]);
                mod.projectiles[0] = projectile;
                projectile.gameObject.SetActive(false);
                FakePrefab.MarkAsFakePrefab(projectile.gameObject);
                UnityEngine.Object.DontDestroyOnLoad(projectile);
                projectile.baseData.damage = 7.5f;
                projectile.baseData.speed *= 0.7f;
                mod.positionOffset = new Vector2(0, -0.3f);
                if (mod != gun.DefaultModule)
                {
                    mod.positionOffset = new Vector2(0, 0.3f);
                    mod.ammoCost = 0;
                }
                projectile.transform.parent = gun.barrelOffset;
            }

            gun.reloadTime = 1f;
            gun.barrelOffset.transform.localPosition = new Vector3(1.43f, 0.87f, 0f);
            gun.SetBaseMaxAmmo(200);
            gun.gunClass = GunClass.PISTOL;
            gun.quality = PickupObject.ItemQuality.D;
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            //gun.Volley.UsesShotgunStyleVelocityRandomizer = true;

            DoubleGunID = gun.PickupObjectId;
        }
        public override void PostProcessProjectile(Projectile projectile)
        {
            if (projectile && projectile.ProjectilePlayerOwner())
            {
                if (projectile.ProjectilePlayerOwner().PlayerHasActiveSynergy("Double Or Nothing"))
                {
                    if (projectile.ProjectilePlayerOwner().HasPickupID(168))
                    {
                        bool nuthin = true;
                        foreach (PlayerItem active in projectile.ProjectilePlayerOwner().activeItems)
                        {
                            if (active.PickupObjectId == 168 && active.IsCurrentlyActive) nuthin = false;
                        }
                        if (nuthin)
                        {
                            projectile.baseData.damage *= 0;
                            projectile.AdditionalScaleMultiplier *= 0.1f;
                        }
                        else
                        {
                            projectile.AdditionalScaleMultiplier *= 1.2f;
                            projectile.baseData.damage *= 2;
                        }
                    }
                }
            }
            base.PostProcessProjectile(projectile);
        }
        public static int DoubleGunID;
        public DoubleGun()
        {

        }
    }
}