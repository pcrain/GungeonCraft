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
                10
                );

            beamComp.boneType = BasicBeamController.BeamBoneType.Projectile;
            // beamComp.interpolateStretchedBones = true;
            beamComp.ContinueBeamArtToWall = true;

            projectile2.baseData.damage = 70f;
            projectile2.baseData.force *= 20f;
            projectile2.baseData.range *= 200;
            projectile2.baseData.speed *= 4;

            gasterBlast = projectile2;
        }
    }

    public class ReplaceBulletWithGasterBlaster : MonoBehaviour
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
            Invoke("Expire", 3f); // make sure this is at least as long as the rail's lifetime
        }
        private void BeginBeamFire()
        {
            m_beam = BeamAPI.FreeFireBeamFromAnywhere(
                GasterBlaster.gasterBlast, this.m_owner, this.m_projectile.gameObject,
                Vector2.zero, this.m_angle, 2f, true, true);
            AkSoundEngine.PostEvent("gaster_blaster_sound_effect", this.m_projectile.gameObject);
        }
        private void Expire()
        {
            this.m_projectile.DieInAir(true,false,false,true);
            // UnityEngine.Object.Destroy(this.m_projectile.gameObject);
        }
    }
}
