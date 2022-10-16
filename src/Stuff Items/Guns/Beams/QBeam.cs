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
    public class QBeam : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Q-Beam", "qbeam");
            Game.Items.Rename("outdated_gun_mods:qbeam", "nn:q_beam");
            var behav = gun.gameObject.AddComponent<QBeam>();
            behav.preventNormalFireAudio = true;
            gun.SetShortDescription("");
            gun.SetLongDescription("");

            gun.SetupSprite(null, "qbeam_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 9);
            gun.isAudioLoop = true;
            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);
            gun.gunHandedness = GunHandedness.TwoHanded;
            //GUN STATS
            gun.doesScreenShake = false;
            gun.DefaultModule.ammoCost = 10;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Beam;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.muzzleFlashEffects.type = VFXPoolType.None;
            gun.DefaultModule.cooldownTime = 0.001f;
            gun.DefaultModule.numberOfShotsInClip = 2000;
            gun.DefaultModule.angleVariance = 0;
            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = "Rico Laser";
            gun.barrelOffset.transform.localPosition = new Vector3(38f/16f, 6f/16f, 0f);
            gun.SetBaseMaxAmmo(2000);
            gun.ammo = 2000;
            gun.gunClass = GunClass.BEAM;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).wrapMode = tk2dSpriteAnimationClip.WrapMode.LoopSection;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).loopStart = 0;

            List<string> BeamAnimPaths = new List<string>()
            {
                "NevernamedsItems/Resources/BeamSprites/limebeam_mid_001",
                "NevernamedsItems/Resources/BeamSprites/limebeam_mid_002",
                "NevernamedsItems/Resources/BeamSprites/limebeam_mid_003",
                "NevernamedsItems/Resources/BeamSprites/limebeam_mid_004",
            };
            List<string> BeamImpactPaths = new List<string>()
            {
                "NevernamedsItems/Resources/BeamSprites/limebeam_impact_001",
                "NevernamedsItems/Resources/BeamSprites/limebeam_impact_002",
                "NevernamedsItems/Resources/BeamSprites/limebeam_impact_003",
                "NevernamedsItems/Resources/BeamSprites/limebeam_impact_004",
            };

            //BULLET STATS
            Projectile projectile = ProjectileUtility.SetupProjectile(86);

            BasicBeamController beamComp = projectile.GenerateBeamPrefab(
                "NevernamedsItems/Resources/BeamSprites/limebeam_mid_001",
                new Vector2(6, 4),
                new Vector2(0, 1),
                BeamAnimPaths,
                9,
                //Impact
                BeamImpactPaths,
                13,
                new Vector2(4, 4),
                new Vector2(7, 7),
                //End
                null,
                -1,
                null,
                null,
                //Beginning
                null,
                -1,
                null,
                null
                );
            projectile.baseData.damage = 5f;
            projectile.baseData.force *= 1f;
            projectile.baseData.range *= 200;

            projectile.baseData.speed *= 10f;
            beamComp.boneType = BasicBeamController.BeamBoneType.Straight;
            beamComp.startAudioEvent = "Play_WPN_radiationlaser_shot_01";
            beamComp.endAudioEvent = "Stop_WPN_All";

            gun.DefaultModule.projectiles[0] = projectile;

            gun.quality = PickupObject.ItemQuality.EXCLUDED; //A
            ETGMod.Databases.Items.Add(gun, false, "ANY");
            ID = gun.PickupObjectId;
        }
        public static int ID;
    }
}
