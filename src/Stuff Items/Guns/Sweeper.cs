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

    public class Sweeper : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Sweeper", "sweeper");
            Game.Items.Rename("outdated_gun_mods:sweeper", "nn:sweeper");
            var behav = gun.gameObject.AddComponent<Sweeper>();
            gun.SetShortDescription("B)");
            gun.SetLongDescription("Used for clearing minefields with it's forceful blast.\n\n"+"The numbers, what do they mean!?");

            gun.SetupSprite(null, "sweeper_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 13);
            gun.SetAnimationFPS(gun.idleAnimation, 5);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(51) as Gun).gunSwitchGroup;
            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(38) as Gun).muzzleFlashEffects;

            for (int i = 0; i < 4; i++)
            {
                gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);
            }

            //GUN STATS
            int iterator = 1;
            foreach (ProjectileModule mod in gun.Volley.projectiles)
            {
                mod.ammoCost = 1;
                mod.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
                mod.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
                mod.cooldownTime = 0.5f;
                mod.angleVariance = 20f;
                mod.numberOfShotsInClip = 8;
                mod.projectiles.Clear();

                if (iterator != 1) //Numbers
                {
                    int ForIterator = 1;
                    for (int i = 0; i < 8; i++)
                    {
                        Projectile projectile = ProjectileSetupUtility.MakeProjectile(86, ForIterator, 15, 23);
                        projectile.hitEffects.alwaysUseMidair = true;
                        switch (ForIterator)
                        {
                            case 1:
                                projectile.SetProjectileSpriteRight("sweeper_1", 6, 10, false, tk2dBaseSprite.Anchor.MiddleCenter, 4, 8);
                                projectile.hitEffects.overrideMidairDeathVFX = RainbowGuonStone.BlueGuonTransitionVFX;
                                break;
                            case 2:
                                projectile.SetProjectileSpriteRight("sweeper_2", 7, 10, false, tk2dBaseSprite.Anchor.MiddleCenter, 5, 8);
                                projectile.hitEffects.overrideMidairDeathVFX = RainbowGuonStone.GreenGuonTransitionVFX;
                                break;
                            case 3:
                                projectile.SetProjectileSpriteRight("sweeper_3", 7, 10, false, tk2dBaseSprite.Anchor.MiddleCenter, 5, 8);
                                projectile.hitEffects.overrideMidairDeathVFX = RainbowGuonStone.RedGuonTransitionVFX;
                                break;
                            case 4:
                                projectile.SetProjectileSpriteRight("sweeper_4", 7, 10, false, tk2dBaseSprite.Anchor.MiddleCenter, 5, 8);
                                projectile.hitEffects.overrideMidairDeathVFX = RainbowGuonStone.BlueGuonTransitionVFX;
                                break;
                            case 5:
                                projectile.SetProjectileSpriteRight("sweeper_5", 7, 10, false, tk2dBaseSprite.Anchor.MiddleCenter, 5, 8);
                                projectile.hitEffects.overrideMidairDeathVFX = RainbowGuonStone.RedGuonTransitionVFX;
                                break;
                            case 6:
                                projectile.SetProjectileSpriteRight("sweeper_6", 7, 10, false, tk2dBaseSprite.Anchor.MiddleCenter, 5, 8);
                                projectile.hitEffects.overrideMidairDeathVFX = RainbowGuonStone.CyanGuonTransitionVFX;
                                break;
                            case 7:
                                projectile.SetProjectileSpriteRight("sweeper_7", 7, 10, false, tk2dBaseSprite.Anchor.MiddleCenter, 5, 8);
                                projectile.hitEffects.overrideMidairDeathVFX = RainbowGuonStone.GreyGuonTransitionVFX;
                                break;
                            case 8:
                                projectile.SetProjectileSpriteRight("sweeper_8", 7, 10, false, tk2dBaseSprite.Anchor.MiddleCenter, 5, 8);
                                projectile.hitEffects.overrideMidairDeathVFX = RainbowGuonStone.GreyGuonTransitionVFX;
                                break;
                        }
                        mod.projectiles.Add(projectile);
                        ForIterator++;
                    }
                }
                else //Bomb
                {
                    Projectile projectile = ProjectileSetupUtility.MakeProjectile(86, 9, 10, 18);
                    projectile.SetProjectileSpriteRight("sweeper_bomb", 13, 13, false, tk2dBaseSprite.Anchor.MiddleCenter, 11, 11);
                    ExplosiveModifier boom = projectile.gameObject.GetOrAddComponent<ExplosiveModifier>();
                    boom.doExplosion = true;
                    boom.explosionData = StaticExplosionDatas.explosiveRoundsExplosion;
                    mod.projectiles.Add(projectile);
                }



                gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
                gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("Sweeper Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/sweeper_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/sweeper_clipempty");
                if (mod != gun.DefaultModule) { mod.ammoCost = 0; }
                iterator++;
            }

            gun.reloadTime = 1.4f;
            gun.barrelOffset.transform.localPosition = new Vector3(25f / 16f, 11f / 16f, 0f);
            gun.SetBaseMaxAmmo(80);
            gun.gunClass = GunClass.SHOTGUN;

            //CLIP OBJECT
            gun.gameObject.transform.Find("Clip").transform.position = new Vector3(6 / 16f, 9 / 16f);//used to position where your clips will spawn
            gun.clipObject = BreakAbleAPI.BreakableAPIToolbox.GenerateDebrisObject("NevernamedsItems/Resources/Debris/sweeper_clip.png", true, 1, 5, 60, 20, null, 1, null, null, 1).gameObject;
            gun.reloadClipLaunchFrame = 3; //the frame on which the clip/s will spawn when the gun reloads
            gun.clipsToLaunchOnReload = 1;//self explanatory

            gun.quality = PickupObject.ItemQuality.B;
            ETGMod.Databases.Items.Add(gun, null, "ANY");
            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.Volley.UsesShotgunStyleVelocityRandomizer = true;

            ID = gun.PickupObjectId;
        }
        public static int ID;
    }
}
