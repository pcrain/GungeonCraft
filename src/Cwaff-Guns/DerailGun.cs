using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using UnityEngine;
using MonoMod.RuntimeDetour;

using Gungeon;
using Dungeonator;
using Alexandria.ItemAPI;
using Alexandria.Misc;

/*
TODO (hardest to easiest):
    - find and add sound effects
    - tweak stats
*/

namespace CwaffingTheGungy
{
    public class DerailGun : AdvancedGunBehavior
    {
        public static string ItemName         = "Derail Gun";
        public static string SpriteName       = "alphabeam";
        public static string ProjectileName   = "86"; //marine sidearm
        public static string ShortDescription = "I Choo Choose You";
        public static string LongDescription  = "(o:)";

        public static Projectile railBeam;
        public static Projectile trainProjectile;
        public static ExplosionData smallTrainExplosion;
        public static ExplosionData bigTrainExplosion;

        public static int trainSpriteDiameter = 30;

        public static void Add()
        {
            Gun gun = Lazy.SetupGun(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription);
            var comp = gun.gameObject.AddComponent<DerailGun>();
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
            ETGModConsole.Log($"got beam paths");
            projectile.baseData.damage         = 0f;
            projectile.baseData.force          = 0f;
            projectile.baseData.speed          = 0.1f;
            projectile.baseData.range          = 200;
            projectile.sprite.renderer.enabled = false;

            var railcomp = projectile.gameObject.AddComponent<ReplaceBulletWithRail>();

            Projectile projectile2         = Lazy.PrefabProjectileFromGun(ItemHelper.Get(Items.MarineSidearm) as Gun, false);
            projectile2.baseData.damage    = 0;
            projectile2.baseData.force     = 0;
            projectile2.baseData.range    *= 200;
            BasicBeamController beamComp2  = projectile2.GenerateBeamPrefab(
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
            beamComp2.boneType             = BasicBeamController.BeamBoneType.Straight;
            beamComp2.startAudioEvent      = "Play_WPN_radiationlaser_shot_01";
            beamComp2.endAudioEvent        = "Stop_WPN_All";
            beamComp2.penetration          = 100;
            // beamComp2.boneType = BasicBeamController.BeamBoneType.Projectile;
            // beamComp2.interpolateStretchedBones = true;
            // beamComp2.ContinueBeamArtToWall = true;
            railBeam = projectile2;

            Projectile train = Lazy.PrefabProjectileFromGun(ItemHelper.Get(Items._38Special) as Gun, false); //id 56 == 38 special
            // train.SetProjectileSpriteRight("train_projectile_001", trainSpriteDiameter, trainSpriteDiameter, true, tk2dBaseSprite.Anchor.MiddleCenter, 20, 20);
            train.AnimateProjectile(
                ResMap.Get("train_projectile").Base(),
                6, true, new IntVector2(trainSpriteDiameter, trainSpriteDiameter),
                false, tk2dBaseSprite.Anchor.LowerCenter, true, false);
            train.PenetratesInternalWalls = true;
            train.pierceMinorBreakables   = true;
            trainProjectile               = train;

            GoopModifier goopmod           = train.gameObject.AddComponent<GoopModifier>();
            goopmod.SpawnGoopOnCollision   = true;
            goopmod.CollisionSpawnRadius   = 6.5f;
            goopmod.SpawnGoopInFlight      = false;
            goopmod.goopDefinition         = EasyGoopDefinitions.FireDef;

            ExplosionData defaultExplosion = GameManager.Instance.Dungeon.sharedSettingsPrefab.DefaultExplosionData;
            smallTrainExplosion = new ExplosionData()
            {
                forceUseThisRadius     = true,
                pushRadius             = 2.5f,
                damageRadius           = 2.5f,
                damageToPlayer         = 10f,
                doDamage               = true,
                damage                 = 25,
                doDestroyProjectiles   = false,
                doForce                = true,
                debrisForce            = 30f,
                preventPlayerForce     = false,
                explosionDelay         = 0.01f,
                usesComprehensiveDelay = false,
                doScreenShake          = true,
                playDefaultSFX         = true,
                effect                 = defaultExplosion.effect,
                ignoreList             = defaultExplosion.ignoreList,
            };
            bigTrainExplosion = new ExplosionData()
            {
                forceUseThisRadius     = true,
                pushRadius             = 5f,
                damageRadius           = 5f,
                damageToPlayer         = 10f,
                doDamage               = true,
                damage                 = 25,
                doDestroyProjectiles   = false,
                doForce                = true,
                debrisForce            = 30f,
                preventPlayerForce     = false,
                explosionDelay         = 0.01f,
                usesComprehensiveDelay = false,
                doScreenShake          = true,
                playDefaultSFX         = true,
                effect                 = defaultExplosion.effect,
                ignoreList             = defaultExplosion.ignoreList,
                ss                     = new ScreenShakeSettings
                {
                    magnitude               = 7.5f,
                    speed                   = 6.5f,
                    time                    = 1f,
                    falloff                 = 0,
                    direction               = Vector2.zero,
                    vibrationType           = ScreenShakeSettings.VibrationType.Auto,
                    simpleVibrationStrength = Vibration.Strength.Hard,
                    simpleVibrationTime     = Vibration.Time.Slow
                },
            };

            ExplodeOnImpact explode       = train.gameObject.GetOrAddComponent<ExplodeOnImpact>();
            PierceProjModifier pierce     = train.gameObject.GetOrAddComponent<PierceProjModifier>();
            pierce.penetration            = 100;
            pierce.penetratesBreakables   = true;
        }
    }

    public class ExplodeOnImpact : MonoBehaviour
    {
        private Projectile m_projectile;
        private PlayerController m_owner;
        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            if (this.m_projectile.Owner && this.m_projectile.Owner is PlayerController)
                this.m_owner = this.m_projectile.Owner as PlayerController;
            this.m_projectile.OnDestruction += this.OnDestruction;
        }
        private void OnDestruction(Projectile obj)
        {
            Vector2 deathPos = this.m_projectile.sprite.WorldCenter;
            Exploder.Explode(deathPos, DerailGun.bigTrainExplosion, Vector2.zero);
        }
    }

    public class ReplaceBulletWithRail : MonoBehaviour
    {
        private Projectile m_projectile;
        private PlayerController m_owner;
        private float m_angle;
        private float return_angle;
        private BeamController m_beam;

        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            if (this.m_projectile.Owner && this.m_projectile.Owner is PlayerController)
            {
                this.m_owner      = this.m_projectile.Owner as PlayerController;
                this.m_angle      = this.m_owner.CurrentGun.CurrentAngle;
                this.return_angle = this.m_angle + (this.m_angle > 180 ? 180 : (-180));
            }
            BeginBeamFire();
            this.m_projectile.enabled = false;
            Invoke("Expire", 30f); // make sure this is at least as long as the rail's lifetime
        }
        private void BeginBeamFire()
        {
            m_beam = BeamAPI.FreeFireBeamFromAnywhere(
                DerailGun.railBeam, this.m_owner, this.m_projectile.gameObject,
                Vector2.zero, this.m_angle, 5, true, true);
            Invoke("BeginScreenShake", 2f);
        }
        private void BeginScreenShake()
        {
            ScreenShakeSettings ss = new ScreenShakeSettings
            {
                magnitude               = 5f,
                speed                   = 6.5f,
                time                    = 1.75f,
                falloff                 = 0,
                direction               = Vector2.zero,
                vibrationType           = ScreenShakeSettings.VibrationType.Auto,
                simpleVibrationStrength = Vibration.Strength.Hard,
                simpleVibrationTime     = Vibration.Time.Slow
            };
            GameManager.Instance.MainCameraController.DoScreenShake(
                ss, new Vector2?(this.m_owner.specRigidbody.UnitCenter), false);
            Invoke("CallUponTheTrain", 1.5f);
        }
        private void CallUponTheTrain()
        {
            Vector2 endOfBeam =
                m_beam.GetComponent<BasicBeamController>().GetPointOnBeam(1.0f);
            Vector2 dontImmediatelyCollideWithWallOffset =
                BraveMathCollege.DegreesToVector(this.return_angle, DerailGun.trainSpriteDiameter/C.PIXELS_PER_TILE);  //16f = tile size
            Vector2 spawnPoint =
                endOfBeam + dontImmediatelyCollideWithWallOffset;
            Exploder.Explode(spawnPoint, DerailGun.smallTrainExplosion, Vector2.zero);
            SpawnManager.SpawnProjectile(
                DerailGun.trainProjectile.gameObject,
                spawnPoint,
                Quaternion.Euler(0f, 0f, this.return_angle),
                true);
        }
        private void Expire()
        {
            this.m_projectile.DieInAir(true,false,false,true);
            // UnityEngine.Object.Destroy(this.m_projectile.gameObject);
        }
    }
}
