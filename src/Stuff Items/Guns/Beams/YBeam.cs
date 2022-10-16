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
    public class YBeam : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Y-Beam", "ybeam");
            Game.Items.Rename("outdated_gun_mods:ybeam", "nn:y_beam");
            var behav = gun.gameObject.AddComponent<YBeam>();
            behav.preventNormalFireAudio = true;
            gun.SetShortDescription("Energy Fissure");
            gun.SetLongDescription("The ion-plate in this powerful anti-air laser is faulty, causing the fired beam to delaminate after travelling a certain distance." + "\n\nThe unregulated energy state of the bifurcated segments causes them to do much more damage to combatant targets than the singular beam.");

            gun.SetupSprite(null, "ybeam_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 20);
            gun.isAudioLoop = true;
            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);

            //GUN STATS
            gun.doesScreenShake = false;
            gun.DefaultModule.ammoCost = 8;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Beam;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.muzzleFlashEffects.type = VFXPoolType.None;
            gun.DefaultModule.cooldownTime = 0.001f;
            gun.DefaultModule.numberOfShotsInClip = 2000;
            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.BEAM;
            gun.barrelOffset.transform.localPosition = new Vector3(1.93f, 0.31f, 0f);
            gun.SetBaseMaxAmmo(1000);
            gun.ammo = 1000;
            gun.gunClass = GunClass.BEAM;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).wrapMode = tk2dSpriteAnimationClip.WrapMode.LoopSection;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).loopStart = 1;

            List<string> BeamAnimPaths = new List<string>()
            {
                "NevernamedsItems/Resources/BeamSprites/ybeam_mid_001",
                "NevernamedsItems/Resources/BeamSprites/ybeam_mid_002",
                "NevernamedsItems/Resources/BeamSprites/ybeam_mid_003",
                "NevernamedsItems/Resources/BeamSprites/ybeam_mid_004",
                "NevernamedsItems/Resources/BeamSprites/ybeam_mid_005",
            };
            List<string> BeamStartPaths = new List<string>()
            {
                "NevernamedsItems/Resources/BeamSprites/ybeam_start_001",
                "NevernamedsItems/Resources/BeamSprites/ybeam_start_002",
                "NevernamedsItems/Resources/BeamSprites/ybeam_start_003",
                "NevernamedsItems/Resources/BeamSprites/ybeam_start_004",
                "NevernamedsItems/Resources/BeamSprites/ybeam_start_005",
            };
            List<string> BeamImpactPaths = new List<string>()
            {
                "NevernamedsItems/Resources/BeamSprites/ybeam_impact_001",
                "NevernamedsItems/Resources/BeamSprites/ybeam_impact_002",
                "NevernamedsItems/Resources/BeamSprites/ybeam_impact_003",
                "NevernamedsItems/Resources/BeamSprites/ybeam_impact_004",
            };

            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(86) as Gun).DefaultModule.projectiles[0]);

            BasicBeamController beamComp = projectile.GenerateBeamPrefab(
                "NevernamedsItems/Resources/BeamSprites/ybeam_mid_001",
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

            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            projectile.baseData.damage = 25f;
            projectile.baseData.force *= 1f;
            projectile.baseData.range *= 200;
            projectile.baseData.speed *= 6;
            BeamSplittingModifier split = projectile.gameObject.AddComponent<BeamSplittingModifier>();
            split.dmgMultOnSplit = 2;
            split.distanceTilSplit = 6;
            split.amtToSplitTo = 2;
            split.splitAngles = 45;

            beamComp.boneType = BasicBeamController.BeamBoneType.Projectile;
            beamComp.startAudioEvent = "Play_WPN_radiationlaser_shot_01";
            beamComp.endAudioEvent = "Stop_WPN_All";
            //beamComp.interpolateStretchedBones = false;

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("Y-Beam Laser", "NevernamedsItems/Resources/CustomGunAmmoTypes/ybeam_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/genericbeam_clipempty");

            gun.DefaultModule.projectiles[0] = projectile;

            gun.quality = PickupObject.ItemQuality.B;
            ETGMod.Databases.Items.Add(gun, false, "ANY");

        }
        protected override void PostProcessBeam(BeamController beam)
        {
            if (beam.projectile && beam.projectile.ProjectilePlayerOwner())
            {
                if (beam.projectile.ProjectilePlayerOwner().PlayerHasActiveSynergy("Centerfold"))
                {
                    beam.projectile.baseData.damage *= 0.6f;
                    if (beam.GetComponent<BeamSplittingModifier>())
                    {
                        beam.GetComponent<BeamSplittingModifier>().amtToSplitTo++;
                    }
                }
            }
            base.PostProcessBeam(beam);
        }
        public YBeam()
        {

        }
    }
}