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
    public class BoomBeam : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Boom Beam", "boombeam");
            Game.Items.Rename("outdated_gun_mods:boom_beam", "nn:boom_beam");
            var behav = gun.gameObject.AddComponent<BoomBeam>();
            behav.preventNormalFireAudio = true;
            gun.SetShortDescription("Explosive Transmission");
            gun.SetLongDescription("A high-tech military laser packed with so much energy that it causes violent explosions when impeded."+"\n\nThe laser went through five stages of R&D until it's beam was glowy enough.");

            gun.SetupSprite(null, "boombeam_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 20);
            gun.isAudioLoop = true;
            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);

            //GUN STATS
            gun.doesScreenShake = false;
            gun.DefaultModule.ammoCost = 15;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Beam;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.muzzleFlashEffects.type = VFXPoolType.None;
            gun.DefaultModule.cooldownTime = 0.001f;
            gun.DefaultModule.numberOfShotsInClip = 600;
            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.BEAM;
            gun.barrelOffset.transform.localPosition = new Vector3(2.62f, 0.53f, 0f);
            gun.SetBaseMaxAmmo(600);
            gun.ammo = 600;
            gun.gunClass = GunClass.EXPLOSIVE;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).wrapMode = tk2dSpriteAnimationClip.WrapMode.LoopSection;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).loopStart = 1;

            List<string> BeamAnimPaths = new List<string>()
            {
                "NevernamedsItems/Resources/BeamSprites/largeredbeam_mid_001",
                "NevernamedsItems/Resources/BeamSprites/largeredbeam_mid_002",
                "NevernamedsItems/Resources/BeamSprites/largeredbeam_mid_003",
                "NevernamedsItems/Resources/BeamSprites/largeredbeam_mid_004",
            };
            List<string> BeamStartPaths = new List<string>()
            {
                "NevernamedsItems/Resources/BeamSprites/largeredbeam_start_001",
                "NevernamedsItems/Resources/BeamSprites/largeredbeam_start_002",
                "NevernamedsItems/Resources/BeamSprites/largeredbeam_start_003",
                "NevernamedsItems/Resources/BeamSprites/largeredbeam_start_004",
            };
            List<string> BeamImpactPaths = new List<string>()
            {
                "NevernamedsItems/Resources/BeamSprites/largeredbeam_impact_001",
                "NevernamedsItems/Resources/BeamSprites/largeredbeam_impact_002",
                "NevernamedsItems/Resources/BeamSprites/largeredbeam_impact_003",
                "NevernamedsItems/Resources/BeamSprites/largeredbeam_impact_004",
            };

            //BULLET STATS
            Projectile projectile = ProjectileUtility.SetupProjectile(86);

            BasicBeamController beamComp = projectile.GenerateBeamPrefab(
                "NevernamedsItems/Resources/BeamSprites/largeredbeam_mid_001",
                new Vector2(16, 5),
                new Vector2(0, 6),
                BeamAnimPaths,
                13,
                //Impact
                BeamImpactPaths,
                13,
                new Vector2(6, 6),
                new Vector2(8, 8),
                //End
                null,
                -1,
                null,
                null,
                //Beginning
                BeamStartPaths,
                13,
                new Vector2(16, 5),
                new Vector2(0, 6),
                //Other Variables
                100
                );

            projectile.baseData.damage = 30f;
            projectile.baseData.force *= 1f;
            projectile.baseData.range *= 200;
            projectile.baseData.speed *= 6;
            BeamExplosiveModifier booms = projectile.gameObject.AddComponent<BeamExplosiveModifier>();
            booms.canHarmOwner = false;
            booms.chancePerTick = 1;
            booms.explosionData = GameManager.Instance.Dungeon.sharedSettingsPrefab.DefaultExplosionData;
            booms.ignoreQueues = true;
            booms.tickDelay = 0.5f;

            beamComp.boneType = BasicBeamController.BeamBoneType.Projectile;
            beamComp.startAudioEvent = "Play_WPN_radiationlaser_shot_01";
            beamComp.endAudioEvent = "Stop_WPN_All";
            //beamComp.interpolateStretchedBones = false;

            gun.DefaultModule.projectiles[0] = projectile;

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("BoomBeam Laser", "NevernamedsItems/Resources/CustomGunAmmoTypes/boombeam_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/boombeam_clipempty");

            gun.quality = PickupObject.ItemQuality.S; //S
            ETGMod.Databases.Items.Add(gun, false, "ANY");

            ID = gun.PickupObjectId;
        }
        public static int ID;
        public BoomBeam()
        {

        }
    }
}
