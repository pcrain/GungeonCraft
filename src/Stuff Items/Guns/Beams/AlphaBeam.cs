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
    public class AlphaBeam : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Alpha Beam", "alphabeam");
            Game.Items.Rename("outdated_gun_mods:alpha_beam", "nn:alpha_beam");
            var behav = gun.gameObject.AddComponent<AlphaBeam>();
            behav.preventNormalFireAudio = true;
            gun.SetShortDescription("Ancient Tech");
            gun.SetLongDescription("A powerful ion-beam that stings like hell."+"\n\nEvidence suggests this may be the first energy beam to ever find it's way to the Gungeon.");

            gun.SetupSprite(null, "alphabeam_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 20);
            gun.isAudioLoop = true;
            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);

            //GUN STATS
            gun.doesScreenShake = false;
            gun.DefaultModule.ammoCost = 5;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Beam;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.muzzleFlashEffects.type = VFXPoolType.None;
            gun.DefaultModule.cooldownTime = 0.001f;
            gun.DefaultModule.numberOfShotsInClip = -1;
            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.BEAM;
            gun.barrelOffset.transform.localPosition = new Vector3(2.75f, 0.43f, 0f);
            gun.SetBaseMaxAmmo(600);
            gun.ammo = 600;
            gun.gunClass = GunClass.BEAM;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).wrapMode = tk2dSpriteAnimationClip.WrapMode.LoopSection;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).loopStart = 1;

            List<string> BeamAnimPaths = new List<string>()
            {
                "NevernamedsItems/Resources/BeamSprites/alphabeam_mid_001",
                "NevernamedsItems/Resources/BeamSprites/alphabeam_mid_002",
                "NevernamedsItems/Resources/BeamSprites/alphabeam_mid_003",
                "NevernamedsItems/Resources/BeamSprites/alphabeam_mid_004",
            };
            List<string> BeamStartPaths = new List<string>()
            {
                "NevernamedsItems/Resources/BeamSprites/alphabeam_start_001",
                "NevernamedsItems/Resources/BeamSprites/alphabeam_start_002",
                "NevernamedsItems/Resources/BeamSprites/alphabeam_start_003",
                "NevernamedsItems/Resources/BeamSprites/alphabeam_start_004",
            };
            List<string> BeamEndPaths = new List<string>()
            {
                "NevernamedsItems/Resources/BeamSprites/alphabeam_end_001",
                "NevernamedsItems/Resources/BeamSprites/alphabeam_end_002",
                "NevernamedsItems/Resources/BeamSprites/alphabeam_end_003",
                "NevernamedsItems/Resources/BeamSprites/alphabeam_end_004",
            };
            List<string> BeamImpactPaths = new List<string>()
            {
                "NevernamedsItems/Resources/BeamSprites/alphabeam_impact_001",
                "NevernamedsItems/Resources/BeamSprites/alphabeam_impact_002",
                "NevernamedsItems/Resources/BeamSprites/alphabeam_impact_003",
                "NevernamedsItems/Resources/BeamSprites/alphabeam_impact_004",
            };

            //BULLET STATS
            Projectile projectile = ProjectileUtility.SetupProjectile(86);

            BasicBeamController beamComp = projectile.GenerateBeamPrefab(
                "NevernamedsItems/Resources/BeamSprites/alphabeam_mid_001",
                new Vector2(15, 7),
                new Vector2(0, 4),
                BeamAnimPaths,
                13,
                //Impact
                BeamImpactPaths,
                13,
                new Vector2(7, 7),
                new Vector2(4, 4),
                //End
                BeamEndPaths,
                13,
                new Vector2(15, 7),
                new Vector2(0, 4),
                //Beginning
                BeamStartPaths,
                13,
                new Vector2(15, 7),
                new Vector2(0, 4),
                //Other Variables
                100
                );

            projectile.baseData.damage = 70f;
            projectile.baseData.force *= 20f;
            projectile.baseData.range *= 200;
            projectile.baseData.speed *= 4;

            //projectile.gameObject.AddComponent<EnemyBulletConverterBeam>();

            beamComp.boneType = BasicBeamController.BeamBoneType.Projectile;
            beamComp.startAudioEvent = "Play_WPN_radiationlaser_shot_01";
            beamComp.endAudioEvent = "Stop_WPN_All";
            //beamComp.interpolateStretchedBones = false;
            beamComp.penetration += 100; 
            gun.DefaultModule.projectiles[0] = projectile;

            gun.quality = PickupObject.ItemQuality.A;
            ETGMod.Databases.Items.Add(gun, null, "ANY");

        }
        protected override void PostProcessBeam(BeamController beam)
        {
            if (beam && beam.projectile && beam.projectile.ProjectilePlayerOwner() && beam.projectile.ProjectilePlayerOwner().PlayerHasActiveSynergy("Absolute Radiance"))
            {
                BeamSplittingModifier split = beam.gameObject.GetOrAddComponent<BeamSplittingModifier>();
                split.dmgMultOnSplit = 0.25f;
                split.amtToSplitTo += 10;
                split.distanceTilSplit = 1;
                split.splitAngles = 90;
            }
            base.PostProcessBeam(beam);
        }
        public AlphaBeam()
        {

        }
    }
}
