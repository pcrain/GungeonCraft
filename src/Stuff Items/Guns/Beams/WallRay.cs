using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using Alexandria.Misc;
using Alexandria.ItemAPI;

namespace NevernamedsItems
{
    public class WallRay : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Wall-Ray", "wallray");
            Game.Items.Rename("outdated_gun_mods:wallray", "nn:wall_ray");
            var behav = gun.gameObject.AddComponent<WallRay>();
            behav.preventNormalFireAudio = true;
            gun.SetShortDescription("90 Degrees Are Best Degrees");
            gun.SetLongDescription("Creates two beams at ninety degree angles from the direction aimed."+"\n\nInvented and used primarily for window washing.");

            gun.SetupSprite(null, "wallray_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 8);
            gun.isAudioLoop = true;
            gun.gunClass = GunClass.SHITTY;
            int iterator = 0;
            for (int i = 0; i < 2; i++)
            {
                gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);
            }
            foreach (ProjectileModule mod in gun.Volley.projectiles)
            {
                if (iterator == 0) { mod.angleFromAim = 90; mod.positionOffset = new Vector2(0, 0.56f); }
                if (iterator == 1) { mod.angleFromAim = -90; mod.positionOffset = new Vector2(0, -0.56f); }
            
                mod.ammoCost = 10;
                if (mod != gun.DefaultModule) { mod.ammoCost = 0; }
                mod.shootStyle = ProjectileModule.ShootStyle.Beam;
                mod.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
                mod.cooldownTime = 0.001f;
                mod.numberOfShotsInClip = -1;
                mod.angleVariance = 0;
                mod.ammoType = GameUIAmmoType.AmmoType.BEAM;

                List<string> BeamAnimPaths = new List<string>()
            {
                "NevernamedsItems/Resources/BeamSprites/thickredbeam_mid_001",
                "NevernamedsItems/Resources/BeamSprites/thickredbeam_mid_002",
                "NevernamedsItems/Resources/BeamSprites/thickredbeam_mid_003",
                "NevernamedsItems/Resources/BeamSprites/thickredbeam_mid_004",
                "NevernamedsItems/Resources/BeamSprites/thickredbeam_mid_005",
            };
                List<string> BeamStartPaths = new List<string>()
            {
                "NevernamedsItems/Resources/BeamSprites/thickredbeam_start_001",
                "NevernamedsItems/Resources/BeamSprites/thickredbeam_start_002",
                "NevernamedsItems/Resources/BeamSprites/thickredbeam_start_003",
                "NevernamedsItems/Resources/BeamSprites/thickredbeam_start_004",
                "NevernamedsItems/Resources/BeamSprites/thickredbeam_start_005",
            };
                List<string> BeamImpactPaths = new List<string>()
            {
                "NevernamedsItems/Resources/BeamSprites/thickredbeam_impact_001",
                "NevernamedsItems/Resources/BeamSprites/thickredbeam_impact_002",
                "NevernamedsItems/Resources/BeamSprites/thickredbeam_impact_003",
                "NevernamedsItems/Resources/BeamSprites/thickredbeam_impact_004",
            };

                //BULLET STATS
                Projectile projectile = ProjectileUtility.SetupProjectile(86);

                BasicBeamController beamComp = projectile.GenerateBeamPrefab(
                    "NevernamedsItems/Resources/BeamSprites/thickredbeam_mid_001",
                    new Vector2(9, 9),
                    new Vector2(0, 1),
                    BeamAnimPaths,
                    13,
                    //Impact
                    BeamImpactPaths,
                    13,
                    new Vector2(3, 3),
                    new Vector2(5, 5),
                    //End
                    null,
                    -1,
                    null,
                    null,
                    //Beginning
                    BeamStartPaths,
                    13,
                    new Vector2(9, 9),
                    new Vector2(0, 1),
                    //Other Variables
                    100
                    );

                
                projectile.baseData.damage = 25f;
                projectile.baseData.force *= 2f;
                projectile.baseData.range *= 10;
                projectile.baseData.speed *= 10f;
                beamComp.boneType = BasicBeamController.BeamBoneType.Projectile;

                beamComp.interpolateStretchedBones = false;
                if (iterator == 0)
                {
                    beamComp.endAudioEvent = "Stop_WPN_All";
                    beamComp.startAudioEvent = "Play_WPN_moonscraperLaser_shot_01";
                }
                iterator++;

                mod.projectiles[0] = projectile;
            }

            //GUN STATS
            gun.doesScreenShake = false;
            gun.reloadTime = 1f;
            gun.muzzleFlashEffects.type = VFXPoolType.None;
            gun.barrelOffset.transform.localPosition = new Vector3(1.56f, 0.62f, 0f);
            gun.SetBaseMaxAmmo(2000);
            gun.ammo = 2000;

            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).wrapMode = tk2dSpriteAnimationClip.WrapMode.LoopSection;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).loopStart = 1;

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("OMITB GenericRed Laser", "NevernamedsItems/Resources/CustomGunAmmoTypes/genericredbeam_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/genericbeam_clipempty");

            gun.quality = PickupObject.ItemQuality.C; 
            ETGMod.Databases.Items.Add(gun, null, "ANY");

        }
        public WallRay()
        {

        }
    }
}

