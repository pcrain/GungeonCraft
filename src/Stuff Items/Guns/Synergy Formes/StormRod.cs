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
    public class StormRod : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Storm Rod", "stormrod");
            Game.Items.Rename("outdated_gun_mods:storm_rod", "nn:lightning_rod+storm_rod");
            var behav = gun.gameObject.AddComponent<StormRod>();
            behav.preventNormalFireAudio = true;
            gun.SetShortDescription("");
            gun.SetLongDescription("");

            gun.SetupSprite(null, "stormrod_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 8);
            gun.isAudioLoop = true;
            gun.gunClass = GunClass.BEAM;
            int iterator = 0;
            for (int i = 0; i < 3; i++)
            {
                gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);
            }
            foreach (ProjectileModule mod in gun.Volley.projectiles)
            {
                if (iterator == 1) mod.angleFromAim = 30;
                if (iterator == 2) mod.angleFromAim = -30;
                mod.ammoCost = 10;
                if (mod != gun.DefaultModule) { mod.ammoCost = 0; }
                mod.shootStyle = ProjectileModule.ShootStyle.Beam;
                mod.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
                mod.cooldownTime = 0.001f;
                mod.numberOfShotsInClip = -1;
                mod.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
                mod.customAmmoType = "Y-Beam Laser";

                List<string> StartAnimPaths = new List<string>()
            {
                "NevernamedsItems/Resources/BeamSprites/stormrodbeam_start_001",
                "NevernamedsItems/Resources/BeamSprites/stormrodbeam_start_002",
                "NevernamedsItems/Resources/BeamSprites/stormrodbeam_start_003",
                "NevernamedsItems/Resources/BeamSprites/stormrodbeam_start_004",
            };
                List<string> BeamAnimPaths = new List<string>()
            {
                "NevernamedsItems/Resources/BeamSprites/stormrodbeam_mid_001",
                "NevernamedsItems/Resources/BeamSprites/stormrodbeam_mid_002",
                "NevernamedsItems/Resources/BeamSprites/stormrodbeam_mid_003",
                "NevernamedsItems/Resources/BeamSprites/stormrodbeam_mid_004",
            };
                List<string> BeamImpactPaths = new List<string>()
            {
                "NevernamedsItems/Resources/BeamSprites/stormrodbeam_impact_001",
                "NevernamedsItems/Resources/BeamSprites/stormrodbeam_impact_002",
                "NevernamedsItems/Resources/BeamSprites/stormrodbeam_impact_003",
                "NevernamedsItems/Resources/BeamSprites/stormrodbeam_impact_004",
            };

                Projectile projectile = ProjectileUtility.SetupProjectile(86); 

                BasicBeamController beamComp = projectile.GenerateBeamPrefab(
               "NevernamedsItems/Resources/BeamSprites/stormrodbeam_mid_001",
               new Vector2(17, 7),
               new Vector2(0, 5),
               BeamAnimPaths,
               10,
               //Impact
               BeamImpactPaths,
               10,
               new Vector2(4, 4),
               new Vector2(7, 7),
               //End
               null,
               -1,
               null,
               null,
               //Beginning
               StartAnimPaths,
               10,
               new Vector2(4, 4),
               new Vector2(7, 7),
               10,
               10
               );
                
                projectile.baseData.damage = 30f;
                projectile.baseData.force *= 1f;
                projectile.damageTypes |= CoreDamageTypes.Electric;
                projectile.baseData.range = 1000;
                projectile.baseData.speed *= 10f;
                beamComp.boneType = BasicBeamController.BeamBoneType.Straight;           
                if (iterator == 0)
                {
                    beamComp.startAudioEvent = "Play_ElectricSoundLoop";
                    beamComp.endAudioEvent = "Stop_ElectricSoundLoop";
                }
                iterator++;

                mod.projectiles[0] = projectile;
            }

            //GUN STATS
            gun.doesScreenShake = false;
            gun.reloadTime = 1f;
            gun.muzzleFlashEffects.type = VFXPoolType.None;
            gun.barrelOffset.transform.localPosition = new Vector3(0.87f, 0.93f, 0f);
            gun.SetBaseMaxAmmo(1700);
            gun.ammo = 1700;

            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).wrapMode = tk2dSpriteAnimationClip.WrapMode.LoopSection;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).loopStart = 1;

            gun.quality = PickupObject.ItemQuality.EXCLUDED; 
            ETGMod.Databases.Items.Add(gun, false, "ANY");
            StormRodID = gun.PickupObjectId;

        }
        public static int StormRodID;
        public StormRod()
        {

        }
    }
}

