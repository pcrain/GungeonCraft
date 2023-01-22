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
    public class GasterBlaster : AdvancedGunBehavior
    {
        public static string gunName          = "Gaster Blaster";
        public static string spriteName       = "converter";
        public static string projectileName   = "86"; //marine sidearm
        public static string shortDescription = "Not a Bad Time";
        public static string longDescription  = "(O_O)";

        public static Projectile gasterBlast;
        public static Projectile gasterBlastLauncher;

        public static int trainSpriteDiameter = 30;

        public static void Add()
        {
            Gun gun = Lazy.InitGunFromStrings(gunName, spriteName, projectileName, shortDescription, longDescription);
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

            Projectile projectile              = Lazy.PrefabProjectileFromGun(gun);
            projectile.baseData.damage         = 0f;
            projectile.baseData.force          = 0f;
            projectile.baseData.speed          = 0.1f;
            projectile.baseData.range          = 200;
            projectile.sprite.renderer.enabled = false;

            var gastercomp = projectile.gameObject.AddComponent<ReplaceBulletWithGasterBlaster>();

            List<string> BeamAnimPaths = new List<string>()
            {
                "CwaffingTheGungy/Resources/BeamSprites/alphabeam_mid_001",
                "CwaffingTheGungy/Resources/BeamSprites/alphabeam_mid_002",
                "CwaffingTheGungy/Resources/BeamSprites/alphabeam_mid_003",
                "CwaffingTheGungy/Resources/BeamSprites/alphabeam_mid_004",
            };
            List<string> BeamStartPaths = new List<string>()
            {
                "CwaffingTheGungy/Resources/BeamSprites/alphabeam_start_001",
                "CwaffingTheGungy/Resources/BeamSprites/alphabeam_start_002",
                "CwaffingTheGungy/Resources/BeamSprites/alphabeam_start_003",
                "CwaffingTheGungy/Resources/BeamSprites/alphabeam_start_004",
            };
            List<string> BeamEndPaths = new List<string>()
            {
                "CwaffingTheGungy/Resources/BeamSprites/alphabeam_end_001",
                "CwaffingTheGungy/Resources/BeamSprites/alphabeam_end_002",
                "CwaffingTheGungy/Resources/BeamSprites/alphabeam_end_003",
                "CwaffingTheGungy/Resources/BeamSprites/alphabeam_end_004",
            };
            List<string> BeamImpactPaths = new List<string>()
            {
                "CwaffingTheGungy/Resources/BeamSprites/alphabeam_impact_001",
                "CwaffingTheGungy/Resources/BeamSprites/alphabeam_impact_002",
                "CwaffingTheGungy/Resources/BeamSprites/alphabeam_impact_003",
                "CwaffingTheGungy/Resources/BeamSprites/alphabeam_impact_004",
            };

            //BULLET STATS
            Projectile projectile2 = Lazy.PrefabProjectileFromGun(PickupObjectDatabase.GetById(86) as Gun, false);

            BasicBeamController beamComp = projectile2.GenerateBeamPrefab(
                "CwaffingTheGungy/Resources/BeamSprites/alphabeam_mid_001",
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
                0
                );

            beamComp.boneType = BasicBeamController.BeamBoneType.Projectile;
            // beamComp.interpolateStretchedBones = true;
            beamComp.ContinueBeamArtToWall = true;

            projectile2.baseData.damage = 70f;
            projectile2.baseData.force *= 20f;
            projectile2.baseData.range *= 200;
            projectile2.baseData.speed *= 4;

            gasterBlast = projectile2;

            Projectile blaster = Lazy.PrefabProjectileFromGun(PickupObjectDatabase.GetById(56) as Gun, false);
            blaster.baseData.damage         = 0f;
            blaster.baseData.force          = 0f;
            blaster.baseData.speed          = 0.0f;
            blaster.baseData.range          = 200;

            blaster.AnimateProjectile(
                new List<string> {
                    "gaster_blaster",
                }, 6, true, new IntVector2(48, 36),
                false, tk2dBaseSprite.Anchor.MiddleCenter, true, false);


            blaster.PenetratesInternalWalls       = true;
            blaster.pierceMinorBreakables         = true;
            PierceProjModifier pierce             = blaster.gameObject.GetOrAddComponent<PierceProjModifier>();
            pierce.penetration                    = 100;
            pierce.penetratesBreakables           = true;

            RotateIntoPositionBehavior rotatecomp = blaster.gameObject.AddComponent<RotateIntoPositionBehavior>();

            gasterBlastLauncher                   = blaster;
        }
    }

    public class RotateIntoPositionBehavior : MonoBehaviour
    {
        public Vector2 m_fulcrum;
        public float m_radius;
        public float m_start_angle;
        public float m_end_angle;
        public float m_rotate_time;

        private Projectile m_projectile;
        private float timer;
        private float angle_delta;
        private bool has_been_init = false;

        private void Start()
        {
        }

        public void Setup()
        {
            this.m_projectile  = base.GetComponent<Projectile>();
            this.timer         = 0;
            this.angle_delta   = this.m_end_angle - this.m_start_angle;
            this.has_been_init = true;
            this.Relocate();
        }

        private void Update()
        {
            if ((!this.has_been_init) || (this.timer > m_rotate_time))
                return;
            this.timer += BraveTime.DeltaTime;
            if (this.timer > this.m_rotate_time)
                this.timer = this.m_rotate_time;
            this.Relocate();
        }

        private void Relocate()
        {
            float percentDone  = this.timer / this.m_rotate_time;
            // float curAngle     = this.m_start_angle + percentDone * this.angle_delta;
            float curAngle     = this.m_start_angle + (float)Math.Tanh(percentDone*Mathf.PI) * this.angle_delta;
            Vector2 curPos     = this.m_fulcrum + BraveMathCollege.DegreesToVector(curAngle, this.m_radius);
            this.m_projectile.transform.position = curPos.ToVector3ZisY(-1f);
            this.m_projectile.transform.rotation =
                Quaternion.Euler(0f, 0f, curAngle + (curAngle > 180 ? 180 : (-180)));
        }
    }

    public class ReplaceBulletWithGasterBlaster : MonoBehaviour
    {
        private Projectile m_projectile;
        private Projectile m_blaster;
        private PlayerController m_owner;
        private float m_angle;
        private Vector2 m_spawn;
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
                this.m_spawn      = this.m_projectile.sprite.WorldBottomCenter;

                BeginGasterRotate();
                this.m_projectile.enabled = false;
                Invoke("BeginBeamFire", 0.75f); // make sure this is at least as long as the rail's lifetime
                Invoke("Expire", 2f); // make sure this is at least as long as the rail's lifetime
            }
        }
        private void BeginGasterRotate()
        {
            this.m_blaster = SpawnManager.SpawnProjectile(
                GasterBlaster.gasterBlastLauncher.gameObject,
                this.m_spawn,
                Quaternion.Euler(0f, 0f, this.m_angle),
                true).GetComponent<Projectile>();

            RotateIntoPositionBehavior rotcomp = this.m_blaster.GetComponent<RotateIntoPositionBehavior>();
            rotcomp.m_radius                   = 16f;
            rotcomp.m_fulcrum                  = this.m_spawn + BraveMathCollege.DegreesToVector(this.m_angle,rotcomp.m_radius);
            rotcomp.m_start_angle              = this.m_angle;
            rotcomp.m_end_angle                = this.return_angle;
            rotcomp.m_rotate_time              = 0.5f;
            rotcomp.Setup();

            AkSoundEngine.PostEvent("gaster_blaster_sound_effect_stop_all", this.m_projectile.gameObject);
            AkSoundEngine.PostEvent("gaster_blaster_sound_effect", this.m_projectile.gameObject);
        }
        private void BeginBeamFire()
        {
            m_beam = BeamAPI.FreeFireBeamFromAnywhere(
                GasterBlaster.gasterBlast, this.m_owner, this.m_projectile.gameObject,
                Vector2.zero, this.m_angle, 0.75f, true, true);
        }
        private void Expire()
        {
            this.m_projectile.DieInAir(true,false,false,true);
            this.m_blaster.DieInAir(true,false,false,true);
        }
    }
}
