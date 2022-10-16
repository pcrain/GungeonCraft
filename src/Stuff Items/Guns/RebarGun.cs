using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Reflection;
using Gungeon;
using MonoMod;
using UnityEngine;
using ItemAPI;
using SaveAPI;

namespace NevernamedsItems
{

    public class RebarGun : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Rebar Gun", "rebargun");
            Game.Items.Rename("outdated_gun_mods:rebar_gun", "nn:rebar_gun");
            gun.gameObject.AddComponent<RebarGun>();
            gun.SetShortDescription("Raising the Bar");
            gun.SetLongDescription("An incredibly satisfying piece of industrial machinery, designed specifically for killing adorable wildlife.");

            gun.SetupSprite(null, "rebargun_idle_001", 8);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(806) as Gun).gunSwitchGroup;

            gun.SetAnimationFPS(gun.shootAnimation, 16);
            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);

            //VFX
            List<string> RebarGunImpactVFX = new List<string>()
            {
                "NevernamedsItems/Resources/MiscVFX/GunVFX/RebarGunImpactVFX2_001",
                "NevernamedsItems/Resources/MiscVFX/GunVFX/RebarGunImpactVFX2_002",
                "NevernamedsItems/Resources/MiscVFX/GunVFX/RebarGunImpactVFX2_003",
                "NevernamedsItems/Resources/MiscVFX/GunVFX/RebarGunImpactVFX2_004",
                "NevernamedsItems/Resources/MiscVFX/GunVFX/RebarGunImpactVFX2_005",
                "NevernamedsItems/Resources/MiscVFX/GunVFX/RebarGunImpactVFX2_006",
            };
            VFXPool VFXOBj = VFXToolbox.CreateVFXPool("Rebar Gun Impact VFX", RebarGunImpactVFX, 16, new IntVector2(11, 8), tk2dBaseSprite.Anchor.MiddleLeft, false, 0, true);

            //GUN STATS
            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(12) as Gun).muzzleFlashEffects;
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1.21f;
            gun.DefaultModule.cooldownTime = 0.3f;
            gun.DefaultModule.numberOfShotsInClip = 3;
            gun.barrelOffset.transform.localPosition = new Vector3(1.5f, 0.87f, 0f);
            gun.SetBaseMaxAmmo(170);
            gun.gunClass = GunClass.PISTOL;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.transform.parent = gun.barrelOffset;
                projectile.SetProjectileSpriteRight("rebargun_proj", 20, 2, false, tk2dBaseSprite.Anchor.MiddleCenter, 6, 2);
            projectile.baseData.speed *= 4f;
            projectile.baseData.damage = 20f;
            projectile.baseData.force *= 5;
            projectile.hitEffects.deathTileMapHorizontal = VFXOBj;
            projectile.hitEffects.tileMapHorizontal = VFXOBj;

            //Custom Clip VFX           
            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("Rebar Shells", "NevernamedsItems/Resources/CustomGunAmmoTypes/rebargun_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/rebargun_clipempty");

            gun.quality = PickupObject.ItemQuality.B;
            ETGMod.Databases.Items.Add(gun, null, "ANY");
            //gun.SetupUnlockOnCustomFlag(CustomDungeonFlags.PURCHASED_BLASMASTER, true);

            RebarGunID = gun.PickupObjectId;
        }
        public static int RebarGunID;
        public RebarGun()
        {

        }
    }
}
