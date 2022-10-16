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
using Alexandria.Misc;

namespace NevernamedsItems
{

    public class RustyShotgun : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Rusty Shotgun", "rustyshotgun");
            Game.Items.Rename("outdated_gun_mods:rusty_shotgun", "nn:rusty_shotgun");
            var behav = gun.gameObject.AddComponent<RustyShotgun>();
            gun.SetShortDescription("Past It's Prime");
            gun.SetLongDescription("This shotgun was cast aside to rust in a gutter years ago. Some of it's shots never even manage to fire!"+"\n\nPerhaps it just needs an understanding user to let it shine.");

            gun.SetupSprite(null, "rustyshotgun_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 13);
            gun.SetAnimationFPS(gun.idleAnimation, 5);
            gun.SetAnimationFPS(gun.reloadAnimation, 1);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(51) as Gun).gunSwitchGroup;

            for (int i = 0; i < 5; i++)
            {
                gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);
            }

            //GUN STATS
            int iterator = 0;
            foreach (ProjectileModule mod in gun.Volley.projectiles)
            {

                mod.ammoCost = 1;
                mod.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
                mod.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
                mod.cooldownTime = 0.5f;
                mod.angleVariance = 20f;
                mod.numberOfShotsInClip = 3;
                Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(mod.projectiles[0]);
                mod.projectiles[0] = projectile;
                projectile.gameObject.SetActive(false);
                FakePrefab.MarkAsFakePrefab(projectile.gameObject);
                UnityEngine.Object.DontDestroyOnLoad(projectile);
                projectile.baseData.range *= 0.7f;
                projectile.baseData.damage = 8f;
                if (iterator.isEven())
                {
                    BounceProjModifier Bouncing = projectile.gameObject.GetOrAddComponent<BounceProjModifier>();
                    Bouncing.numberOfBounces = 1;
                }
                else
                {
                    PierceProjModifier pierce = projectile.gameObject.GetOrAddComponent<PierceProjModifier>();
                    pierce.penetration = 1;
                }
                InstantDestroyProjOnSpawn death = projectile.gameObject.AddComponent<InstantDestroyProjOnSpawn>();
                death.chance = 0.25f;
                if (mod != gun.DefaultModule) { mod.ammoCost = 0; }
                projectile.transform.parent = gun.barrelOffset;
                iterator++;
            }

            gun.reloadTime = 1.5f;
            gun.barrelOffset.transform.localPosition = new Vector3(2.0f, 0.62f, 0f);
            gun.SetBaseMaxAmmo(200);
            gun.gunClass = GunClass.SHOTGUN;
            //BULLET STATS
            gun.quality = PickupObject.ItemQuality.D;
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            gun.Volley.UsesShotgunStyleVelocityRandomizer = true;

            RustyShotgunID = gun.PickupObjectId;
            gun.SetupUnlockOnCustomStat(CustomTrackedStats.RUSTY_ITEMS_STOLEN, 0, DungeonPrerequisite.PrerequisiteOperation.GREATER_THAN);
        }
        public static int RustyShotgunID;
    }
}
