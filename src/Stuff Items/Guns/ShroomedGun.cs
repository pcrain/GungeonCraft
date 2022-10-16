using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Reflection;
using Gungeon;
using MonoMod;
using UnityEngine;
using Alexandria.ItemAPI;
using SaveAPI;

namespace NevernamedsItems
{

    public class ShroomedGun : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Shroomed Gun", "shroomedgun");
            Game.Items.Rename("outdated_gun_mods:shroomed_gun", "nn:shroomed_gun");
            var behav = gun.gameObject.AddComponent<ShroomedGun>();

            gun.SetShortDescription("Looney");
            gun.SetLongDescription("The classic result of a misfired magnum." + "\n\nLooks like someone stuck their finger in the barrel.");

            gun.SetupSprite(null, "shroomedgun_idle_001", 8);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(38) as Gun).gunSwitchGroup;


            gun.SetAnimationFPS(gun.shootAnimation, 15);

            for (int i = 0; i < 2; i++)
            {
                gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(38) as Gun, true, false);
            }
            //Easy Variables to Change all the Modules
            float AllModCooldown = 0.15f;
            int AllModClipshots = 6;


            int i2 = 0;
            foreach (ProjectileModule mod in gun.Volley.projectiles)
            {
                mod.numberOfShotsInClip = AllModClipshots;

                Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(mod.projectiles[0]);
                mod.projectiles[0] = projectile;
                projectile.gameObject.SetActive(false);
                FakePrefab.MarkAsFakePrefab(projectile.gameObject);
                UnityEngine.Object.DontDestroyOnLoad(projectile);
                projectile.baseData.damage *= 0.5f;
                projectile.baseData.speed *= 1f;
                projectile.baseData.range *= 0.5f;

                if (i2 <= 0) //First Common Proj (-30)
                {
                    mod.ammoCost = 0;
                    mod.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
                    mod.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
                    mod.cooldownTime = AllModCooldown;
                    mod.angleVariance = 7f;
                    mod.angleFromAim = -30f;
                    i2++;
                }
                else if (i2 >= 1) //Second Common Proj (0)
                {
                    mod.ammoCost = 1;
                    mod.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
                    mod.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
                    mod.cooldownTime = AllModCooldown;
                    mod.angleVariance = 7f;
                    mod.angleFromAim = 30f;

                    i2++;
                }
            }
            gun.reloadTime = 1f;
            gun.barrelOffset.transform.localPosition = new Vector3(1.0f, 0.81f, 0f);
            gun.SetBaseMaxAmmo(140);
            gun.gunClass = GunClass.SHITTY;
            gun.quality = PickupObject.ItemQuality.D;
            ETGMod.Databases.Items.Add(gun, false, "ANY");
            gun.SetupUnlockOnCustomFlag(CustomDungeonFlags.HURT_BY_SHROOMER, true);
            ShroomedGunID = gun.PickupObjectId;
        }
        public static int ShroomedGunID;
        protected override void Update()
        {
            if (gun.GunPlayerOwner() != null)
            {
                PlayerController player = gun.CurrentOwner as PlayerController;
                if (player.PlayerHasActiveSynergy("Ballistic Fingers"))
                {
                    if (gun.Volley.projectiles[0].angleFromAim == -30f)
                    {
                        gun.Volley.projectiles[0].angleFromAim = -10f;
                        gun.Volley.projectiles[1].angleFromAim = 10f;
                    }
                }
                else
                {
                    if (gun.Volley.projectiles[0].angleFromAim == -10f)
                    {
                        gun.Volley.projectiles[0].angleFromAim = -30f;
                        gun.Volley.projectiles[1].angleFromAim = 30f;
                    }
                }
            }
            base.Update();
        }
        public ShroomedGun()
        {

        }
    }
}

