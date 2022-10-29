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

namespace CwaffingTheGungy
{
    public class RailGun : AdvancedGunBehavior
    {
        public static string gunName          = "Rail Gun";
        public static string spriteName       = "alphabeam";
        public static string projectileName   = "86"; //marine sidearm
        public static string shortDescription = "I Choo Choose You";
        public static string longDescription  = "(o:)";

        public static void Add()
        {
            Gun gun = Lazy.InitGunFromStrings(gunName, spriteName, projectileName, shortDescription, longDescription);
            var comp = gun.gameObject.AddComponent<RailGun>();
            comp.preventNormalFireAudio = true;

            gun.isAudioLoop                          = true;
            gun.doesScreenShake                      = false;
            gun.DefaultModule.ammoCost               = 5;
            gun.DefaultModule.shootStyle             = ProjectileModule.ShootStyle.Beam;
            gun.DefaultModule.sequenceStyle          = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime                           = 1f;
            gun.muzzleFlashEffects.type              = VFXPoolType.None;
            gun.DefaultModule.cooldownTime           = 0.001f;
            gun.DefaultModule.numberOfShotsInClip    = -1;
            gun.DefaultModule.ammoType               = GameUIAmmoType.AmmoType.BEAM;
            gun.barrelOffset.transform.localPosition = new Vector3(2.75f, 0.43f, 0f);
            gun.ammo                                 = 600;
            gun.quality                              = PickupObject.ItemQuality.A;
            gun.gunClass                             = GunClass.BEAM;
            gun.SetBaseMaxAmmo(600);
            gun.SetAnimationFPS(gun.shootAnimation, 20);
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).wrapMode = tk2dSpriteAnimationClip.WrapMode.LoopSection;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).loopStart = 1;

            List<string> BeamAnimPaths = new List<string>()
            {
                "CwaffingTheGungy/Resources/BeamSprites/railbeam_mid_001",
            };
            List<string> BeamStartPaths = new List<string>()
            {
                "CwaffingTheGungy/Resources/BeamSprites/railbeam_mid_001",
            };
            List<string> BeamEndPaths = new List<string>()
            {
                "CwaffingTheGungy/Resources/BeamSprites/railbeam_mid_001",
            };
            List<string> BeamImpactPaths = new List<string>()
            {
                "CwaffingTheGungy/Resources/BeamSprites/railbeam_mid_001",
            };

            //BULLET STATS
            // Projectile projectile = ProjectileUtility.SetupProjectile(86);
            Projectile projectile = Lazy.PrefabProjectileFromGun(gun);
            projectile.baseData.damage = 0f;
            projectile.baseData.force  = 0f;
            projectile.baseData.range *= 200;
            projectile.baseData.speed *= 1;

            BasicBeamController beamComp = projectile.GenerateBeamPrefab(
                /*sprite path*/                    "CwaffingTheGungy/Resources/BeamSprites/railbeam_mid_001",
                /*collider dimensions*/            new Vector2(15, 7),
                /*collider offsets*/               new Vector2(0, 4),
                /*beam sprites*/                   BeamAnimPaths,
                /*beam fps*/                       13,
                /*impact vfx sprites*/             BeamImpactPaths,
                /*beam impact fps */               13,
                /*impact vfx collider dimensions*/ new Vector2(7, 7),
                /*impact vfx collider offsets */   new Vector2(4, 4),
                /*end vfx sprites*/                BeamEndPaths,
                /*beam end fps */                  13,
                /*end vfx collider dimensions*/    new Vector2(15, 7),
                /*end vfx collider offsets */      new Vector2(0, 4),
                /*muzzle (start) vfx sprites*/     BeamStartPaths,
                /*beam muzzle fps */               13,
                /*muzzle vfx collider dimensions*/ new Vector2(15, 7),
                /*muzzle vfx collider offsets */   new Vector2(0, 4),
                /*emissive color*/                 0
                );

            //projectile.gameObject.AddComponent<EnemyBulletConverterBeam>();

            // beamComp.boneType = BasicBeamController.BeamBoneType.Projectile;
            beamComp.boneType                  = BasicBeamController.BeamBoneType.Straight;
            beamComp.interpolateStretchedBones = false;
            beamComp.startAudioEvent           = "Play_WPN_radiationlaser_shot_01";
            beamComp.endAudioEvent             = "Stop_WPN_All";
            beamComp.penetration               += 100;
            gun.DefaultModule.projectiles[0]   = projectile;


            Projectile projectile2 = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(86) as Gun).DefaultModule.projectiles[0]);
            BasicBeamController beamComp2 = projectile2.GenerateBeamPrefab(
                /*sprite path*/                    "CwaffingTheGungy/Resources/BeamSprites/railbeam_mid_001",
                /*collider dimensions*/            new Vector2(15, 7),
                /*collider offsets*/               new Vector2(0, 4),
                /*beam sprites*/                   BeamAnimPaths,
                /*beam fps*/                       13//,
                // /*impact vfx sprites*/             BeamImpactPaths,
                // /*beam impact fps */               13,
                // /*impact vfx collider dimensions*/ new Vector2(7, 7),
                // /*impact vfx collider offsets */   new Vector2(4, 4),
                // /*end vfx sprites*/                BeamEndPaths,
                // /*beam end fps */                  13,
                // /*end vfx collider dimensions*/    new Vector2(15, 7),
                // /*end vfx collider offsets */      new Vector2(0, 4),
                // /*muzzle (start) vfx sprites*/     BeamStartPaths,
                // /*beam muzzle fps */               13,
                // /*muzzle vfx collider dimensions*/ new Vector2(15, 7),
                // /*muzzle vfx collider offsets */   new Vector2(0, 4),
                // /*emissive color*/                 0
                );
            projectile2.gameObject.SetActive(false);
            projectile2.baseData.damage = 0;
            projectile2.baseData.force = 0;
            projectile2.baseData.range *= 200;
            FakePrefab.MarkAsFakePrefab(projectile2.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile2);
            beamComp2.boneType = BasicBeamController.BeamBoneType.Straight;
            beamComp2.interpolateStretchedBones = false;
            // beamComp2.ContinueBeamArtToWall = true;
            simpleBeam = projectile2;
        }
        public static Projectile simpleBeam;
        public override void OnPostFired(PlayerController player, Gun gun)
        {
            base.OnPostFired(player, gun); //called when a gun is fired
            int angle = 135;
            BeamController beam2 = BeamAPI.FreeFireBeamFromAnywhere(
                simpleBeam, player, player.gameObject, Vector2.zero,  angle, 1, true, true);
        }
        protected override void PostProcessBeam(BeamController beam)
        {
            // if (beam && beam.projectile && beam.projectile.ProjectilePlayerOwner() && beam.projectile.ProjectilePlayerOwner().PlayerHasActiveSynergy("Absolute Radiance"))
            // {
            //     BeamSplittingModifier split = beam.gameObject.GetOrAddComponent<BeamSplittingModifier>();
            //     split.dmgMultOnSplit = 0.25f;
            //     split.amtToSplitTo += 10;
            //     split.distanceTilSplit = 1;
            //     split.splitAngles = 90;
            // }
            // if (beam && beam.projectile && beam.projectile.ProjectilePlayerOwner())
            // {
            //     int angle = 135;
            //     PlayerController pc = beam.projectile.ProjectilePlayerOwner();
            //     BeamController beam2 = BeamAPI.FreeFireBeamFromAnywhere(
            //         simpleBeam, pc, pc.gameObject, Vector2.zero,  angle, 1, true, true);
            // }
            base.PostProcessBeam(beam);
        }
    }
}
