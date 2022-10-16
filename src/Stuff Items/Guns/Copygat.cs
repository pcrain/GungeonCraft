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

namespace NevernamedsItems
{

    public class Copygat : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Copygat", "copygat");
            Game.Items.Rename("outdated_gun_mods:copygat", "nn:copygat");
            var behav = gun.gameObject.AddComponent<Copygat>();
            behav.overrideNormalFireAudio = "Play_BOSS_DragunGold_Baby_Death_01";
            behav.preventNormalFireAudio = true;
            gun.SetShortDescription("Follow The Leader");
            gun.SetLongDescription("Mimics the projectiles of other guns."+"\n\nThis industrial mimigoo weapon has attained a low level of sentience, and now communicates solely in meowing and scratching.");

            gun.SetupSprite(null, "copygat_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 12);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Automatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1.5f;
            gun.muzzleFlashEffects.type = VFXPoolType.None;
            gun.DefaultModule.cooldownTime = 0.15f;
            gun.DefaultModule.numberOfShotsInClip = 10;
            gun.barrelOffset.transform.localPosition = new Vector3(2.5f, 0.56f, 0f);
            gun.SetBaseMaxAmmo(200);
            gun.ammo = 200;
            gun.gunClass = GunClass.FULLAUTO;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);

            projectile.baseData.damage = 20f;
            projectile.SetProjectileSpriteRight("wrench_null_projectile", 13, 7, false, tk2dBaseSprite.Anchor.MiddleCenter, 12, 7);
            projectile.hitEffects.alwaysUseMidair = true;
            projectile.hitEffects.overrideMidairDeathVFX = EasyVFXDatabase.RedLaserCircleVFX;
            projectile.transform.parent = gun.barrelOffset;

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("Copygat Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/copygat_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/copygat_clipempty");

            gun.DefaultModule.projectiles[0] = projectile;
            gun.quality = PickupObject.ItemQuality.A;
            ETGMod.Databases.Items.Add(gun, false, "ANY");

            CopygatID = gun.PickupObjectId;
        }
        public static int CopygatID;
        public override Projectile OnPreFireProjectileModifier(Gun gun, Projectile defaultproj, ProjectileModule mod)
        {
            if (gun && gun.CurrentOwner && gun.GunPlayerOwner())
            {
                if (gun.GunPlayerOwner().inventory.AllGuns.Count > 1)
                {
                    PlayerController mowner = gun.GunPlayerOwner();
                    List<Projectile> ValidBullets = new List<Projectile>();
                    List<Projectile> ValidBeams = new List<Projectile>();
                    if (mowner && mowner.inventory != null)
                    {
                        for (int j = 0; j < mowner.inventory.AllGuns.Count; j++)
                        {
                            if (mowner.inventory.AllGuns[j] && !mowner.inventory.AllGuns[j].InfiniteAmmo)
                            {
                                ProjectileModule defaultModule = mowner.inventory.AllGuns[j].DefaultModule;
                                if (defaultModule.shootStyle == ProjectileModule.ShootStyle.Beam)
                                {
                                    ValidBeams.Add(defaultModule.GetCurrentProjectile());
                                }
                                else if (defaultModule.shootStyle == ProjectileModule.ShootStyle.Charged)
                                {
                                    Projectile projectile = null;
                                    for (int k = 0; k < 15; k++)
                                    {
                                        ProjectileModule.ChargeProjectile chargeProjectile = defaultModule.chargeProjectiles[UnityEngine.Random.Range(0, defaultModule.chargeProjectiles.Count)];
                                        if (chargeProjectile != null) projectile = chargeProjectile.Projectile;
                                        if (projectile) break;
                                    }
                                    ValidBullets.Add(projectile);
                                }
                                else
                                {
                                    ValidBullets.Add(defaultModule.GetCurrentProjectile());
                                }
                            }
                        }

                        int listsCombined = ValidBullets.Count + ValidBeams.Count;
                        if (listsCombined > 0)
                        {
                            int randomSelection = UnityEngine.Random.Range(0, listsCombined);
                            if (randomSelection > ValidBullets.Count) //Beams
                            {
                                return BraveUtility.RandomElement(ValidBeams);
                            }
                            else //Projectiles
                            {
                                return BraveUtility.RandomElement(ValidBullets);
                            }
                        }
                    }
                }
            }
            return base.OnPreFireProjectileModifier(gun, defaultproj, mod);
        }
        public Copygat()
        {

        }
    }
}