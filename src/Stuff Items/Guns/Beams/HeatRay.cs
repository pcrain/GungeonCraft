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
    public class HeatRay : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Heat Ray", "heatray");
            Game.Items.Rename("outdated_gun_mods:heat_ray", "nn:heat_ray");
       var behav =      gun.gameObject.AddComponent<HeatRay>();
            behav.overrideNormalFireAudio = "Play_ENM_shelleton_beam_01";
            behav.preventNormalFireAudio = true;
            gun.SetShortDescription("Set to Defrost");
            gun.SetLongDescription("An old weaponised heating coil, will burn enemies it's focused on for long enough.");

            gun.SetupSprite(null, "heatray_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 8);
            gun.isAudioLoop = true;
            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);

            //GUN STATS
            gun.doesScreenShake = false;
            gun.DefaultModule.ammoCost = 30;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Beam;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.muzzleFlashEffects.type = VFXPoolType.None;
            gun.DefaultModule.cooldownTime = 0.001f;
            gun.DefaultModule.numberOfShotsInClip = 3500;
            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.BEAM;
            gun.barrelOffset.transform.localPosition = new Vector3(1.06f, 0.31f, 0f);
            gun.SetBaseMaxAmmo(3500);
            gun.ammo = 3500;
            gun.gunClass = GunClass.SHITTY;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).wrapMode = tk2dSpriteAnimationClip.WrapMode.Loop;

            List<string> BeamAnimPaths = new List<string>()
            {
                "NevernamedsItems/Resources/BeamSprites/heatray_seg_001",
                "NevernamedsItems/Resources/BeamSprites/heatray_seg_002",
                "NevernamedsItems/Resources/BeamSprites/heatray_seg_003",
                "NevernamedsItems/Resources/BeamSprites/heatray_seg_004",
                "NevernamedsItems/Resources/BeamSprites/heatray_seg_005",
                "NevernamedsItems/Resources/BeamSprites/heatray_seg_006",
                "NevernamedsItems/Resources/BeamSprites/heatray_seg_007",
                "NevernamedsItems/Resources/BeamSprites/heatray_seg_008",
            };
            List<string> BeamImpactPaths = new List<string>()
            {
                "NevernamedsItems/Resources/BeamSprites/heatray_impact_001",
                "NevernamedsItems/Resources/BeamSprites/heatray_impact_002",
                "NevernamedsItems/Resources/BeamSprites/heatray_impact_003",
                "NevernamedsItems/Resources/BeamSprites/heatray_impact_004",
                "NevernamedsItems/Resources/BeamSprites/heatray_impact_005",
                "NevernamedsItems/Resources/BeamSprites/heatray_impact_006",
                "NevernamedsItems/Resources/BeamSprites/heatray_impact_007",
                "NevernamedsItems/Resources/BeamSprites/heatray_impact_008",
            };

            //BULLET STATS
            Projectile projectile = ProjectileUtility.SetupProjectile(86);

            BasicBeamController beamComp = projectile.GenerateBeamPrefab(
                "NevernamedsItems/Resources/BeamSprites/heatray_seg_001", 
                new Vector2(10, 2), 
                new Vector2(0, 3), 
                BeamAnimPaths, 
                16, 
                BeamImpactPaths, 
                20, 
                new Vector2(6,6), 
                new Vector2(2, 4)
                );

            projectile.baseData.damage = 16f;
            projectile.baseData.force *= 0.1f;
            projectile.baseData.range *= 2;
            projectile.baseData.speed *= 0.7f;
            beamComp.boneType = BasicBeamController.BeamBoneType.Projectile;
            beamComp.interpolateStretchedBones = false;
            beamComp.startAudioEvent = "Play_WPN_radiationlaser_shot_01";
            beamComp.endAudioEvent = "Stop_WPN_All";
            projectile.AppliesFire = true;
            projectile.fireEffect = StaticStatusEffects.hotLeadEffect;
            beamComp.statusEffectChance = 0.5f;
            beamComp.TimeToStatus = 1;

            gun.DefaultModule.projectiles[0] = projectile;

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = "Y-Beam Laser";

            gun.quality = PickupObject.ItemQuality.D; //D
            ETGMod.Databases.Items.Add(gun, null, "ANY");
            HeatRayID = gun.PickupObjectId;
        }
        public static int HeatRayID;
        protected override void PostProcessBeam(BeamController beam)
        {
            if (beam && beam.projectile && beam.projectile.ProjectilePlayerOwner())
            {
                if (beam.projectile.ProjectilePlayerOwner().PlayerHasActiveSynergy("It won't actually show up on screen, but there's no hard limit on how long you can make synergy names."))
                {
                    beam.AdjustPlayerBeamTint(Color.green, 1);
                    beam.projectile.fireEffect = StaticStatusEffects.greenFireEffect;
                }
                if (beam.projectile.ProjectilePlayerOwner().PlayerHasActiveSynergy("Re-Heat"))
                {
                    beam.GetComponent<BasicBeamController>().TimeToStatus = 0.1f;
                    beam.GetComponent<BasicBeamController>().statusEffectChance = 1f;
                }
                if (beam.projectile.ProjectilePlayerOwner().PlayerHasActiveSynergy("Don't Trust The Toaster"))
                {
                    beam.projectile.baseData.speed *= 5;
                    beam.projectile.UpdateSpeed();
                    beam.projectile.RuntimeUpdateScale(1.3f);
                    beam.projectile.baseData.damage *= 2;
                    EmmisiveBeams emission = beam.projectile.gameObject.GetOrAddComponent<EmmisiveBeams>();
                }
            }
            base.PostProcessBeam(beam);
        }
        public HeatRay()
        {

        }
    }

}

