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
    public class UziSpineMM : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Uzi SPINE-mm", "uzispinemm");
            Game.Items.Rename("outdated_gun_mods:uzi_spinemm", "nn:uzi_spine_mm");
            var behav = gun.gameObject.AddComponent<UziSpineMM>();
            gun.SetShortDescription("Boned");
            gun.SetLongDescription("The favoured sidearm of the dark sorcerer Nuign, and his first foray into the apocryphal field of necro-gunsmithing.");

            gun.SetupSprite(null, "uzispinemm_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 12);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(29) as Gun).gunSwitchGroup;

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Automatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 0.8f;
            gun.DefaultModule.cooldownTime = 0.11f;
            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(29) as Gun).muzzleFlashEffects;
            gun.DefaultModule.numberOfShotsInClip = 20;
            gun.barrelOffset.transform.localPosition = new Vector3((19f / 16f), (9f / 16f), 0f);
            gun.SetBaseMaxAmmo(600);
            gun.gunClass = GunClass.FULLAUTO;

            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage = 3f;
            projectile.baseData.range = 15f;
            projectile.hitEffects.alwaysUseMidair = true;
            projectile.hitEffects.overrideMidairDeathVFX = EasyVFXDatabase.SmoothLightBlueLaserCircleVFX;

            HomingModifier homing = projectile.gameObject.AddComponent<HomingModifier>();
            homing.AngularVelocity = 40f;
            homing.HomingRadius = 50f;

            projectile.SetProjectileSpriteRight("uzispinemm_proj", 8, 11, false, tk2dBaseSprite.Anchor.MiddleCenter, 8, 7);

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("UziSpineMM Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/uzispinemm_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/uzispinemm_clipempty");

            gun.quality = PickupObject.ItemQuality.C;
            ETGMod.Databases.Items.Add(gun, false, "ANY");
            gun.AddToSubShop(ItemBuilder.ShopType.Cursula);

            ID = gun.PickupObjectId;

        }
        public override void PostProcessProjectile(Projectile projectile)
        {
            if (projectile && projectile.ProjectilePlayerOwner())
            {
                if (projectile.ProjectilePlayerOwner().PlayerHasActiveSynergy("The Bone Zone"))
                {
                    HomingModifier homing = projectile.GetComponent<HomingModifier>();
                    if (homing)
                    {
                        homing.AngularVelocity *= 1.5f;
                    }
                    projectile.pierceMinorBreakables = true;
                    PierceProjModifier piercing = projectile.GetComponent<PierceProjModifier>();
                    if (piercing != null)
                    {
                        piercing.penetration++;
                    }
                    else
                    {
                        piercing = projectile.gameObject.AddComponent<PierceProjModifier>();
                        piercing.penetration = 1;
                    }
                }
            }
            base.PostProcessProjectile(projectile);
        }
        public static int ID;
        public UziSpineMM()
        {

        }
    }
}

