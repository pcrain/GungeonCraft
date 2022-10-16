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
    public class LightningRod : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Lightning Rod", "lightningrod");
            Game.Items.Rename("outdated_gun_mods:lightning_rod", "nn:lightning_rod");
            var behav = gun.gameObject.AddComponent<LightningRod>();
            behav.preventNormalFireAudio = true;
            gun.SetShortDescription("By Jove!");
            gun.SetLongDescription("An ancient magical staff that harnesses the electric power of the sky."+"\n\nUsed for forestry, battery charging, and sometimes for dungeon crawling.");

            gun.SetupSprite(null, "lightningrod_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 9);
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
            gun.DefaultModule.numberOfShotsInClip = -1;
            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = "Y-Beam Laser";
            gun.barrelOffset.transform.localPosition = new Vector3(0.75f, 0.81f, 0f);
            gun.SetBaseMaxAmmo(1000);
            gun.ammo = 1000;
            gun.gunClass = GunClass.BEAM;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).wrapMode = tk2dSpriteAnimationClip.WrapMode.LoopSection;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).loopStart = 1;

            List<string> StartAnimPaths = new List<string>()
            {
                "NevernamedsItems/Resources/BeamSprites/stormrodbeam_start_001",
                "NevernamedsItems/Resources/BeamSprites/stormrodbeam_start_002",
                "NevernamedsItems/Resources/BeamSprites/stormrodbeam_start_003",
                "NevernamedsItems/Resources/BeamSprites/stormrodbeam_start_004",
            };
            List<string> BeamAnimPaths = new List<string>()
            {
                "NevernamedsItems/Resources/BeamSprites/stormrodbeam_mid_001",
                "NevernamedsItems/Resources/BeamSprites/stormrodbeam_mid_002",
                "NevernamedsItems/Resources/BeamSprites/stormrodbeam_mid_003",
                "NevernamedsItems/Resources/BeamSprites/stormrodbeam_mid_004",
            };
            List<string> BeamImpactPaths = new List<string>()
            {
                "NevernamedsItems/Resources/BeamSprites/stormrodbeam_impact_001",
                "NevernamedsItems/Resources/BeamSprites/stormrodbeam_impact_002",
                "NevernamedsItems/Resources/BeamSprites/stormrodbeam_impact_003",
                "NevernamedsItems/Resources/BeamSprites/stormrodbeam_impact_004",
            };

            //BULLET STATS
            Projectile projectile = ProjectileUtility.SetupProjectile(86);

            BasicBeamController beamComp = projectile.GenerateBeamPrefab(
                "NevernamedsItems/Resources/BeamSprites/stormrodbeam_mid_001",
                new Vector2(17, 7),
                new Vector2(0, 5),
                BeamAnimPaths,
                10,
                //Impact
                BeamImpactPaths,
                10,
                new Vector2(4, 4),
                new Vector2(7, 7),
                //End
                null,
                -1,
                null,
                null,
                //Beginning
                StartAnimPaths,
                10,
                new Vector2(4, 4),
                new Vector2(7, 7),
                10,
                10f
                );

            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            projectile.baseData.damage = 30f;
            projectile.baseData.force *= 1f;
            projectile.damageTypes |= CoreDamageTypes.Electric;
            projectile.baseData.range = 1000;
            projectile.baseData.speed *= 10f;
            beamComp.boneType = BasicBeamController.BeamBoneType.Straight;
            beamComp.startAudioEvent = "Play_ElectricSoundLoop";
            beamComp.endAudioEvent = "Stop_ElectricSoundLoop";
            

            gun.DefaultModule.projectiles[0] = projectile;

            gun.quality = PickupObject.ItemQuality.C;
            ETGMod.Databases.Items.Add(gun, false, "ANY");

            LightningRodID = gun.PickupObjectId;
        }
        public static int LightningRodID;
    }
}
