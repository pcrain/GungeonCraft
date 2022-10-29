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
            // comp.preventNormalFireAudio = true;

            gun.isAudioLoop                          = true;
            gun.doesScreenShake                      = false;
            gun.DefaultModule.ammoCost               = 1;
            // gun.DefaultModule.shootStyle             = ProjectileModule.ShootStyle.Beam;
            gun.DefaultModule.shootStyle             = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle          = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime                           = 1f;
            gun.muzzleFlashEffects.type              = VFXPoolType.None;
            gun.DefaultModule.cooldownTime           = 0.5f;
            // gun.DefaultModule.cooldownTime           = 0.001f;
            gun.DefaultModule.numberOfShotsInClip    = -1;
            // gun.DefaultModule.ammoType               = GameUIAmmoType.AmmoType.BEAM;
            gun.DefaultModule.ammoType               = GameUIAmmoType.AmmoType.MEDIUM_BULLET;
            gun.barrelOffset.transform.localPosition = new Vector3(2.75f, 0.43f, 0f);
            gun.ammo                                 = 600;
            gun.quality                              = PickupObject.ItemQuality.A;
            // gun.gunClass                             = GunClass.BEAM;
            gun.gunClass                             = GunClass.SILLY;
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

            Projectile projectile              = Lazy.PrefabProjectileFromGun(gun);
            projectile.baseData.damage         = 0f;
            projectile.baseData.force          = 0f;
            projectile.baseData.speed          = 0.1f;
            projectile.baseData.range          = 200;
            projectile.sprite.renderer.enabled = false;

            //BasicBeamController beamComp = projectile.GenerateBeamPrefab(
            //    /*sprite path*/                    "CwaffingTheGungy/Resources/BeamSprites/railbeam_mid_001",
            //    /*collider dimensions*/            new Vector2(15, 7),
            //    /*collider offsets*/               new Vector2(0, 4),
            //    /*beam sprites*/                   BeamAnimPaths,
            //    /*beam fps*/                       13,
            //    /*impact vfx sprites*/             BeamImpactPaths,
            //    /*beam impact fps */               13,
            //    /*impact vfx collider dimensions*/ new Vector2(7, 7),
            //    /*impact vfx collider offsets */   new Vector2(4, 4),
            //    /*end vfx sprites*/                BeamEndPaths,
            //    /*beam end fps */                  13,
            //    /*end vfx collider dimensions*/    new Vector2(15, 7),
            //    /*end vfx collider offsets */      new Vector2(0, 4),
            //    /*muzzle (start) vfx sprites*/     BeamStartPaths,
            //    /*beam muzzle fps */               13,
            //    /*muzzle vfx collider dimensions*/ new Vector2(15, 7),
            //    /*muzzle vfx collider offsets */   new Vector2(0, 4),
            //    /*emissive color*/                 0
            //    );
            // beamComp.boneType = BasicBeamController.BeamBoneType.Projectile;
            // beamComp.boneType                  = BasicBeamController.BeamBoneType.Straight;
            // beamComp.interpolateStretchedBones = false;

            var railcomp = projectile.gameObject.AddComponent<ReplaceBulletWithRail>();
            var spawntrain = projectile.gameObject.AddComponent<SpawnTrainBehavior>();

            Projectile projectile2 = Lazy.PrefabProjectileFromGun(PickupObjectDatabase.GetById(86) as Gun, false);
            projectile2.baseData.damage = 0;
            projectile2.baseData.force  = 0;
            projectile2.baseData.range *= 200;
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
            beamComp2.boneType         = BasicBeamController.BeamBoneType.Straight;
            beamComp2.startAudioEvent  = "Play_WPN_radiationlaser_shot_01";
            beamComp2.endAudioEvent    = "Stop_WPN_All";
            beamComp2.penetration     += 100;
            // beamComp2.boneType = BasicBeamController.BeamBoneType.Projectile;
            // beamComp2.interpolateStretchedBones = true;
            // beamComp2.ContinueBeamArtToWall = true;
            railBeam = projectile2;

            Projectile train = Lazy.PrefabProjectileFromGun(PickupObjectDatabase.GetById(56) as Gun, false);
            train.SetProjectileSpriteRight("train_projectile", 30, 30, true, tk2dBaseSprite.Anchor.MiddleCenter, 20, 20);
            trainProjectile = train;
        }
        public static Projectile railBeam;
        public static Projectile trainProjectile;
    }

    public class SpawnTrainBehavior : MonoBehaviour
    {
        private Projectile m_projectile;
        private PlayerController m_owner;
        private float m_angle;

        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            if (this.m_projectile.Owner && this.m_projectile.Owner is PlayerController)
            {
                this.m_owner = this.m_projectile.Owner as PlayerController;
                this.m_angle = this.m_owner.CurrentGun.CurrentAngle;
                if (this.m_angle > 180)
                {
                    this.m_angle -= 180;
                }
                else
                {
                    this.m_angle += 180;
                }
            }
        }
        public void SpawnTheTrain(Vector2 position)
        {
            SpawnManager.SpawnProjectile(RailGun.trainProjectile.gameObject, position, Quaternion.Euler(0f, 0f, this.m_angle), true);
        }
    }

    public class ReplaceBulletWithRail : MonoBehaviour
    {
        private Projectile m_projectile;
        private PlayerController m_owner;
        private float m_angle;
        private BeamController m_beam;
        private SpawnTrainBehavior m_spawn;

        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            this.m_spawn = this.m_projectile.GetComponent<SpawnTrainBehavior>();
            if (this.m_projectile.Owner && this.m_projectile.Owner is PlayerController)
            {
                this.m_owner = this.m_projectile.Owner as PlayerController;
                this.m_angle = this.m_owner.CurrentGun.CurrentAngle;
            }
            BeginBeamFire();
            this.m_projectile.enabled = false;
            Invoke("Expire", 30f); // make sure this is at least as long as the rail's lifetime
        }
        private void BeginBeamFire()
        {
            m_beam = BeamAPI.FreeFireBeamFromAnywhere(
                RailGun.railBeam, this.m_owner, this.m_projectile.gameObject, Vector2.zero, this.m_angle, 5, true, true);
            Invoke("CallUponTheTrain", 3f);
        }
        private void CallUponTheTrain()
        {
            this.m_spawn.SpawnTheTrain(m_beam.GetComponent<BasicBeamController>().GetPointOnBeam(0.9f));
        }
        private void Expire()
        {
            this.m_projectile.DieInAir(true,false,false,true);
            // UnityEngine.Object.Destroy(this.m_projectile.gameObject);
        }
    }
}
