using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using SaveAPI;
using Alexandria.ItemAPI;
using Alexandria.Misc;

namespace NevernamedsItems
{
    public class Converter : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Converter", "converter");
            Game.Items.Rename("outdated_gun_mods:converter", "nn:converter");
            var behav = gun.gameObject.AddComponent<Converter>();
            behav.preventNormalFireAudio = true;
            gun.SetShortDescription("My Bullets!");
            gun.SetLongDescription("Converts enemy bullets to your side."+"\n\nPart of a dismantled mind control device. Not strong enough to convert the Gundead themselves, but enemy shots have far weaker wills.");

            gun.SetupSprite(null, "converter_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 9);
            gun.isAudioLoop = true;
            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);

            //GUN STATS
            gun.doesScreenShake = false;
            gun.DefaultModule.ammoCost = 20;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Beam;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.muzzleFlashEffects.type = VFXPoolType.None;        
            gun.DefaultModule.cooldownTime = 0.001f;
            gun.DefaultModule.numberOfShotsInClip = -1;
            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = "Y-Beam Laser";
            gun.barrelOffset.transform.localPosition = new Vector3(0.93f, 0.5f, 0f);
            gun.SetBaseMaxAmmo(2000);
            gun.ammo = 2000;
            gun.gunClass = GunClass.BEAM;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).wrapMode = tk2dSpriteAnimationClip.WrapMode.LoopSection;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).loopStart = 1;

            List<string> BeamAnimPaths = new List<string>()
            {
                "NevernamedsItems/Resources/BeamSprites/yellowbeam_mid_001",
                "NevernamedsItems/Resources/BeamSprites/yellowbeam_mid_002",
                "NevernamedsItems/Resources/BeamSprites/yellowbeam_mid_003",
                "NevernamedsItems/Resources/BeamSprites/yellowbeam_mid_004",
            };
            List<string> BeamImpactPaths = new List<string>()
            {
                "NevernamedsItems/Resources/BeamSprites/yellowbeam_impact_001",
                "NevernamedsItems/Resources/BeamSprites/yellowbeam_impact_002",
                "NevernamedsItems/Resources/BeamSprites/yellowbeam_impact_003",
                "NevernamedsItems/Resources/BeamSprites/yellowbeam_impact_004",
            };

            //BULLET STATS
            Projectile projectile = ProjectileUtility.SetupProjectile(86);

            BasicBeamController beamComp = projectile.GenerateBeamPrefab(
                "NevernamedsItems/Resources/BeamSprites/yellowbeam_mid_001",
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
                null,
                50
                );
            
            projectile.baseData.damage = 7f;
            projectile.baseData.force *= 1f;
            projectile.baseData.range = 12;
            projectile.gameObject.AddComponent<EnemyBulletConverterBeam>();
            projectile.baseData.speed *= 10f;
            beamComp.boneType = BasicBeamController.BeamBoneType.Straight;
            beamComp.startAudioEvent = "Play_WPN_radiationlaser_shot_01";
            beamComp.endAudioEvent = "Stop_WPN_All";
            beamComp.penetration = 1;
            //beamComp.interpolateStretchedBones = false;

            gun.DefaultModule.projectiles[0] = projectile;

            gun.quality = PickupObject.ItemQuality.A; 
            ETGMod.Databases.Items.Add(gun, false, "ANY");

            gun.SetupUnlockOnCustomFlag(CustomDungeonFlags.PURCHASED_CONVERTER, true);
            ConverterID = gun.PickupObjectId;
        }
        public static int ConverterID;
    }
}
