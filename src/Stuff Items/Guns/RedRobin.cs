using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using Alexandria.ItemAPI;

namespace NevernamedsItems
{

    public class RedRobin : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Red Robin", "redrobin");
            Game.Items.Rename("outdated_gun_mods:red_robin", "nn:red_robin");
            gun.gameObject.AddComponent<RedRobin>();
            gun.SetShortDescription("Healthy Option");
            gun.SetLongDescription("Deals bonus damage at full health." + "\n\nThe signature weapon of Gungeoneer 'Hearts Ferros', famous for never being shot in a gunfight... until he was.");

            gun.SetupSprite(null, "redrobin_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 13);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.DefaultModule.cooldownTime = 0.15f;
            gun.DefaultModule.angleFromAim = 0;
            gun.DefaultModule.angleVariance = 12;
            gun.DefaultModule.numberOfShotsInClip = 13;
            gun.barrelOffset.transform.localPosition = new Vector3(23f / 16f, 11f / 16f, 0f);
            gun.SetBaseMaxAmmo(300);
            gun.ammo = 300;
            gun.gunClass = GunClass.SILLY;

            gun.muzzleFlashEffects = VFXToolbox.CreateVFXPool("RedRobin Muzzleflash",
                new List<string>()
                {
                    "NevernamedsItems/Resources/MiscVFX/GunVFX/redrobin_muzzleflash_002",
                    "NevernamedsItems/Resources/MiscVFX/GunVFX/redrobin_muzzleflash_003",
                    "NevernamedsItems/Resources/MiscVFX/GunVFX/redrobin_muzzleflash_004",
                    "NevernamedsItems/Resources/MiscVFX/GunVFX/redrobin_muzzleflash_005",
                },
                13, //FPS
                new IntVector2(9, 16), //Dimensions
                tk2dBaseSprite.Anchor.MiddleLeft, //Anchor
                false, //Uses a Z height off the ground
                0, //The Z height, if used
                false,
               VFXAlignment.Fixed
                  );

            Projectile projectile = gun.DefaultModule.projectiles[0].gameObject.InstantiateAndFakeprefab().GetComponent<Projectile>();
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage = 5f;
            projectile.SetProjectileSpriteRight("redrobin_projectile", 12, 6, true, tk2dBaseSprite.Anchor.MiddleCenter, 12, 6);
            projectile.hitEffects.overrideMidairDeathVFX = EasyVFXDatabase.RedLaserCircleVFX;
            projectile.hitEffects.alwaysUseMidair = true;

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.SMALL_BLASTER;
            gun.quality = PickupObject.ItemQuality.D;
            ETGMod.Databases.Items.Add(gun, false, "ANY");
            ID = gun.PickupObjectId;
        }
        public static int ID;
        public override void OnReload(PlayerController player, Gun gun)
        {
            if (player && player.PlayerHasActiveSynergy("Manbat and Robin"))
            {
                CompanionisedEnemyUtility.SpawnCompanionisedEnemy(player, "2feb50a6a40f4f50982e89fd276f6f15", gun.barrelOffset.position.XY().ToIntVector2(), true, Color.black, 10, 2, false, false);
            }
            base.OnReload(player, gun);
        }
        public override void PostProcessProjectile(Projectile projectile)
        {
            if (projectile && gun && gun.GunPlayerOwner())
            {
                if (gun.GunPlayerOwner().ForceZeroHealthState)
                {
                    if (gun.GunPlayerOwner().characterIdentity == OMITBChars.Shade)
                    {
                        BuffProj(projectile);
                    }
                    else if (gun.GunPlayerOwner().healthHaver.Armor > 6)
                    {
                        BuffProj(projectile);
                    }

                }
                else if (gun.GunPlayerOwner().healthHaver.GetCurrentHealthPercentage() == 1)
                {
                    BuffProj(projectile);
                }
            }
            base.PostProcessProjectile(projectile);
        }
        private void BuffProj(Projectile proj)
        {
            if (gun.GunPlayerOwner().PlayerHasActiveSynergy("Scarlet Tanager")) proj.baseData.damage *= 2f;
            else proj.baseData.damage *= 1.75f;
            proj.RuntimeUpdateScale(1.2f);
        }
    }
}