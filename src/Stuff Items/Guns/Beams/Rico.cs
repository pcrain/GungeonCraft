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
    public class Rico : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Rico", "rico");
            Game.Items.Rename("outdated_gun_mods:rico", "nn:rico");
            var behav = gun.gameObject.AddComponent<Rico>();
            behav.preventNormalFireAudio = true;
            gun.SetShortDescription("Cosmic Ray Spallation");
            gun.SetLongDescription("The lack of any radiation filtering on this homemade energy blaster causes it's playload to ricochet uncontrollably off of surfaces!");

            gun.SetupSprite(null, "rico_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 9);
            gun.isAudioLoop = true;
            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);
            gun.gunHandedness = GunHandedness.OneHanded;
            //GUN STATS
            gun.doesScreenShake = false;
            gun.DefaultModule.ammoCost = 5;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Beam;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.muzzleFlashEffects.type = VFXPoolType.None;
            gun.DefaultModule.cooldownTime = 0.001f;
            gun.DefaultModule.numberOfShotsInClip = 1000;
            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("Rico Laser", "NevernamedsItems/Resources/CustomGunAmmoTypes/rico_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/genericbeam_clipempty");
            gun.barrelOffset.transform.localPosition = new Vector3(1.31f, 0.49f, 0f);
            gun.SetBaseMaxAmmo(1000);
            gun.ammo = 1000;
            gun.gunClass = GunClass.BEAM;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).wrapMode = tk2dSpriteAnimationClip.WrapMode.LoopSection;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).loopStart = 1;

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

            projectile.baseData.damage = 7f;
            projectile.baseData.force *= 1f;
            projectile.baseData.range *= 200;
            
            projectile.baseData.speed *= 10f;
            beamComp.boneType = BasicBeamController.BeamBoneType.Straight;
            beamComp.reflections = 7;
            beamComp.startAudioEvent = "Play_WPN_radiationlaser_shot_01";
            beamComp.endAudioEvent = "Stop_WPN_All";
            //beamComp.interpolateStretchedBones = false;

            gun.DefaultModule.projectiles[0] = projectile;

            gun.quality = PickupObject.ItemQuality.C; 
            ETGMod.Databases.Items.Add(gun, false, "ANY");
            RicoID = gun.PickupObjectId;
        }
        public static int RicoID;
        public Rico()
        {

        }
    }
}
