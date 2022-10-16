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

    public class DiamondCutterRangerClass : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Red Diamond Cutter", "reddiamondcutter");
            Game.Items.Rename("outdated_gun_mods:red_diamond_cutter", "nn:diamond_cutter+ranger_class");
            var behav = gun.gameObject.AddComponent<DiamondCutterRangerClass>();
            behav.preventNormalReloadAudio = true;
            behav.preventNormalFireAudio = true;
            behav.overrideNormalFireAudio = "Play_WPN_blasphemy_shot_01";
            gun.SetShortDescription("Face It!");
            gun.SetLongDescription("Fires piercing gemstones." + "\n\nLeft in a chest by a powerful warrior of shimmering crystal... who didn't show up to work today.");

            gun.SetupSprite(null, "reddiamondcutter_idle_001", 8);
            gun.SetAnimationFPS(gun.reloadAnimation, 1);

            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(97) as Gun).muzzleFlashEffects;

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);
            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);
            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);

            //GUN STATS
            foreach (ProjectileModule mod in gun.Volley.projectiles)
            {
                mod.ammoCost = 1;
                mod.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
                mod.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
                mod.cooldownTime = 0.2f;
                mod.numberOfShotsInClip = 3;
                mod.angleVariance = 20f;
                Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(mod.projectiles[0]);
                projectile.gameObject.SetActive(false);
                FakePrefab.MarkAsFakePrefab(projectile.gameObject);
                UnityEngine.Object.DontDestroyOnLoad(projectile);
                projectile.baseData.damage = 10f;
                projectile.baseData.speed *= 1.5f;
                projectile.hitEffects.alwaysUseMidair = true;
                projectile.hitEffects.overrideMidairDeathVFX = (PickupObjectDatabase.GetById(506) as Gun).DefaultModule.projectiles[0].hitEffects.overrideMidairDeathVFX;

                PierceProjModifier pierce = projectile.gameObject.AddComponent<PierceProjModifier>();
                pierce.penetration = 10;
                MaintainDamageOnPierce maintain = projectile.gameObject.AddComponent<MaintainDamageOnPierce>();
                maintain.damageMultOnPierce = 1.2f;
                projectile.pierceMinorBreakables = true;

                projectile.SetProjectileSpriteRight("diamondcutter_proj", 23, 13, false, tk2dBaseSprite.Anchor.MiddleCenter, 17, 5);
                mod.projectiles[0] = projectile;
                mod.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
                mod.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("RedDiamondCutter Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/reddiamondcutter_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/diamondcutter_clipempty");
                if (mod != gun.DefaultModule) mod.ammoCost = 0;
            }


            gun.reloadTime = 0.8f;
            gun.barrelOffset.transform.localPosition = new Vector3(1.81f, 1.43f, 0f);
            gun.SetBaseMaxAmmo(100);
            gun.ammo = 100;
            gun.gunClass = GunClass.SILLY;

            //gun.AddPassiveStatModifier(PlayerStats.StatType.Curse, 1f, StatModifier.ModifyMethod.ADDITIVE);

            //BULLET STATS


            gun.quality = PickupObject.ItemQuality.EXCLUDED;
            ETGMod.Databases.Items.Add(gun, null, "ANY");
            RedDiamondCutterID = gun.PickupObjectId;

        }
        public static int RedDiamondCutterID;
    }
}
