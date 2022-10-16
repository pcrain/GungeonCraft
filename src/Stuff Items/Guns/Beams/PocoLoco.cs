using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using Alexandria.ItemAPI;

namespace NevernamedsItems
{
    public class PocoLoco : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Poco Loco", "pocoloco");
            Game.Items.Rename("outdated_gun_mods:poco_loco", "nn:poco_loco");
            var behav = gun.gameObject.AddComponent<PocoLoco>();
            behav.preventNormalFireAudio = true;
            gun.SetShortDescription("Crazy");
            gun.SetLongDescription("This energy beam emitter has been heavily modified. At a cursory glance, it's there's no way it could possibly be functional- but what's the harm in trying?");

            gun.SetupSprite(null, "pocoloco_idle_001", 8);

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
            gun.DefaultModule.numberOfShotsInClip = 500;
            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.BEAM;
            gun.barrelOffset.transform.localPosition = new Vector3(18f / 16f, 8f / 16f, 0f);
            gun.SetBaseMaxAmmo(500);
            gun.ammo = 500;
            gun.gunClass = GunClass.BEAM;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).wrapMode = tk2dSpriteAnimationClip.WrapMode.LoopSection;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).loopStart = 1;

            List<string> BeamAnimPaths = new List<string>()
            {
                "NevernamedsItems/Resources/BeamSprites/pocolocobeam_mid_001",
                "NevernamedsItems/Resources/BeamSprites/pocolocobeam_mid_002",
                "NevernamedsItems/Resources/BeamSprites/pocolocobeam_mid_003",
                "NevernamedsItems/Resources/BeamSprites/pocolocobeam_mid_004",
                "NevernamedsItems/Resources/BeamSprites/pocolocobeam_mid_005",
                "NevernamedsItems/Resources/BeamSprites/pocolocobeam_mid_006",
                "NevernamedsItems/Resources/BeamSprites/pocolocobeam_mid_007",
                "NevernamedsItems/Resources/BeamSprites/pocolocobeam_mid_008",
                "NevernamedsItems/Resources/BeamSprites/pocolocobeam_mid_009",
                "NevernamedsItems/Resources/BeamSprites/pocolocobeam_mid_010",
                "NevernamedsItems/Resources/BeamSprites/pocolocobeam_mid_011",
                "NevernamedsItems/Resources/BeamSprites/pocolocobeam_mid_012",               
            };
            
            List<string> BeamImpactPaths = new List<string>()
            {
                "NevernamedsItems/Resources/BeamSprites/yellowbeam_impact_001",
                "NevernamedsItems/Resources/BeamSprites/yellowbeam_impact_002",
                "NevernamedsItems/Resources/BeamSprites/yellowbeam_impact_003",
                "NevernamedsItems/Resources/BeamSprites/yellowbeam_impact_004",
            };

            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(86) as Gun).DefaultModule.projectiles[0]);

            BasicBeamController beamComp = projectile.GenerateBeamPrefab(
                "NevernamedsItems/Resources/BeamSprites/pocolocobeam_mid_001",
                new Vector2(4, 4),
                new Vector2(0, 0),
                BeamAnimPaths,
                13,
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
                null,
                //Other Variables
                10
                );

            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            projectile.baseData.damage = 5f;
            projectile.baseData.force *= 1f;
            projectile.baseData.range *= 200;
            projectile.baseData.speed *= 5;
            BeamSplittingModifier split = projectile.gameObject.AddComponent<BeamSplittingModifier>();
            split.dmgMultOnSplit = 1;
            split.distanceTilSplit = 4;
            split.amtToSplitTo = 3;
            split.splitAngles = 45;

            beamComp.penetration = 3;
            beamComp.homingAngularVelocity = 200;
            beamComp.homingRadius = 30;
            beamComp.reflections = 3;

            beamComp.boneType = BasicBeamController.BeamBoneType.Projectile;
            beamComp.startAudioEvent = "Play_WPN_radiationlaser_shot_01";
            beamComp.endAudioEvent = "Stop_WPN_All";
            //beamComp.interpolateStretchedBones = false;

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("Y-Beam Laser", "NevernamedsItems/Resources/CustomGunAmmoTypes/ybeam_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/genericbeam_clipempty");

            gun.DefaultModule.projectiles[0] = projectile;

            gun.quality = PickupObject.ItemQuality.C;
            ETGMod.Databases.Items.Add(gun, false, "ANY");

        }
    }
}