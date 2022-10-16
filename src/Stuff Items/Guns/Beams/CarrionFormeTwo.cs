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
    public class CarrionFormeTwo : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("carrion_second_form", "carrionformtwo");
            Game.Items.Rename("outdated_gun_mods:carrion_second_form", "nn:carrion_second_form");
            var behav = gun.gameObject.AddComponent<CarrionFormeTwo>();
            behav.preventNormalFireAudio = true;
            gun.SetShortDescription("");
            gun.SetLongDescription("");

            gun.SetupSprite(null, "carrionformtwo_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 13);
            gun.SetAnimationFPS(gun.idleAnimation, 13);
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
            gun.DefaultModule.numberOfShotsInClip = 600;
            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.BEAM;
            gun.barrelOffset.transform.localPosition = new Vector3(0.75f, 0.56f, 0f);
            gun.SetBaseMaxAmmo(600);
            gun.ammo = 600;

            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).wrapMode = tk2dSpriteAnimationClip.WrapMode.LoopSection;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).loopStart = 0;

            //SUB TENTACLE
            #region subtentacle
            Projectile subTendrilProj = ProjectileUtility.SetupProjectile(86);

            BasicBeamController subTendrilComp = subTendrilProj.GenerateBeamPrefab(
                "NevernamedsItems/Resources/BeamSprites/carrionsubtendril_mid_001",
                new Vector2(4, 2),
                new Vector2(0, 1),
                new List<string>() { "NevernamedsItems/Resources/BeamSprites/carrionsubtendril_mid_001" },
                13,
                //Impact
                null,
                -1,
                null,
                null,
                //End
                new List<string>() { "NevernamedsItems/Resources/BeamSprites/carrionsubtendril_end_001" },
                13,
                new Vector2(6, 2),
                new Vector2(0, 1),
                //Beginning
                new List<string>() { "NevernamedsItems/Resources/BeamSprites/carrionsubtendril_start_001" },
                13,
                new Vector2(7, 2),
                new Vector2(0, 1),
                //Other Variables
                0
                );

            subTendrilProj.baseData.damage = 10f;
            subTendrilProj.baseData.force *= 1f;
            subTendrilProj.baseData.range = 4.5f;
            subTendrilComp.ProjectileAndBeamMotionModule = new HelixProjectileMotionModule();
            subTendrilComp.boneType = BasicBeamController.BeamBoneType.Projectile;
            subTendrilComp.penetration = 1;
            subTendrilComp.homingRadius = 10;
            subTendrilComp.homingAngularVelocity = 1000;
            CarrionSubTendrilController subtendril = subTendrilProj.gameObject.AddComponent<CarrionSubTendrilController>();
            #endregion

            //MAIN TENTACLE
            #region maintentacle
            List<string> BeamAnimPaths = new List<string>()
            {
                "NevernamedsItems/Resources/BeamSprites/carrionformtwo_mid_001",
                "NevernamedsItems/Resources/BeamSprites/carrionformtwo_mid_002",
                "NevernamedsItems/Resources/BeamSprites/carrionformtwo_mid_003",
                "NevernamedsItems/Resources/BeamSprites/carrionformtwo_mid_004",
                "NevernamedsItems/Resources/BeamSprites/carrionformtwo_mid_005",
            };
            List<string> BeamEndPaths = new List<string>()
            {
                "NevernamedsItems/Resources/BeamSprites/carrionformtwo_end_001",
                "NevernamedsItems/Resources/BeamSprites/carrionformtwo_end_002",
                "NevernamedsItems/Resources/BeamSprites/carrionformtwo_end_003",
                "NevernamedsItems/Resources/BeamSprites/carrionformtwo_end_004",
                "NevernamedsItems/Resources/BeamSprites/carrionformtwo_end_005",
            };

            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(86) as Gun).DefaultModule.projectiles[0]);

            BasicBeamController beamComp = projectile.GenerateBeamPrefab(
                "NevernamedsItems/Resources/BeamSprites/carrionformtwo_mid_001",
                new Vector2(16, 5),
                new Vector2(0, 6),
                BeamAnimPaths,
                13,
                //Impact
                null,
                -1,
                null,
                null,
                //End
                BeamEndPaths,
                13,
                new Vector2(10, 5),
                new Vector2(0, 6),
                //Beginning
                null,
                -1,
                null,
                null,
                //Other Variables
                0
                );

            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            projectile.baseData.damage = 40f;
            projectile.baseData.force *= 1f;
            projectile.baseData.range = 12;
            projectile.baseData.speed *= 3;

            beamComp.boneType = BasicBeamController.BeamBoneType.Projectile;
            beamComp.startAudioEvent = "Play_WPN_demonhead_shot_01";
            beamComp.endAudioEvent = "Stop_WPN_All";
            beamComp.penetration = 2;
            beamComp.homingRadius = 8;
            beamComp.homingAngularVelocity = 400;

            CarrionMainTendrilController mainTendril = projectile.gameObject.AddComponent<CarrionMainTendrilController>();
            mainTendril.subTendrilPrefab = subTendrilProj.gameObject;

            projectile.gameObject.AddComponent<CarrionMovementTentacles>();

            gun.DefaultModule.projectiles[0] = projectile;
            #endregion

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("Carrion Clip", "NevernamedsItems/Resources/CustomGunAmmoTypes/carrion_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/carrion_clipempty");

            gun.quality = PickupObject.ItemQuality.EXCLUDED; //S
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            Carrion.CarrionForme2ID = gun.PickupObjectId;

        }
        public CarrionFormeTwo()
        {

        }
    }
}