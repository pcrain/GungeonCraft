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
    public class SickWorm : AdvancedGunBehavior
    {
        public static void Add()
        {

            Gun gun = ETGMod.Databases.Items.NewGun("Sick Worm", "sickworm");
            Game.Items.Rename("outdated_gun_mods:sick_worm", "nn:sick_worm");
            var behav = gun.gameObject.AddComponent<SickWorm>();
            behav.preventNormalFireAudio = true;
            gun.SetShortDescription("Projectile Vomit");
            gun.SetLongDescription("A rare example of Gungeon Gigantism, this worm has developed a remarkable evolutionary defence mechanism; regurgitating high-speed digestive juices at potential predators.");

            gun.SetupSprite(null, "sickworm_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 16);
            gun.isAudioLoop = true;
            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);

            //GUN STATS
            gun.doesScreenShake = false;
            gun.DefaultModule.ammoCost = 10;
            gun.DefaultModule.angleVariance = 0;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Beam;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.muzzleFlashEffects.type = VFXPoolType.None;
            gun.DefaultModule.cooldownTime = 0.001f;
            gun.DefaultModule.numberOfShotsInClip = 2000;
            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.BEAM;
            gun.barrelOffset.transform.localPosition = new Vector3(1.06f, 0.62f, 0f);
            gun.SetBaseMaxAmmo(2000);
            gun.ammo = 2000;
            gun.gunClass = GunClass.SILLY;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).wrapMode = tk2dSpriteAnimationClip.WrapMode.LoopSection;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).loopStart = 1;

            List<string> BeamAnimPaths = new List<string>()
            {
                "NevernamedsItems/Resources/BeamSprites/sickworm_mid_001",
            };
            List<string> BeamEndPaths = new List<string>()
            {
                "NevernamedsItems/Resources/BeamSprites/sickworm_end_001",
            };

            //BULLET STATS
            Projectile projectile = ProjectileUtility.SetupProjectile(86);

            BasicBeamController beamComp = projectile.GenerateBeamPrefab(
                "NevernamedsItems/Resources/BeamSprites/sickworm_mid_001",
                new Vector2(8, 8),
                new Vector2(0, 1),
                BeamAnimPaths,
                9,
                //Impact
                null,
                -1,
                null,
                null,
                //End
                BeamEndPaths,
                9,
                new Vector2(8, 8),
                new Vector2(0, 1),
                //Beginning
                null,
                -1,
                null,
                null
                );

            projectile.baseData.damage = 30f;
            projectile.baseData.force *= 1f;
            projectile.baseData.range = 7f;
            projectile.baseData.speed *= 0.7f;

            beamComp.penetration = 2;
            beamComp.boneType = BasicBeamController.BeamBoneType.Projectile;
            beamComp.interpolateStretchedBones = false;
            beamComp.endAudioEvent = "Stop_WPN_All";
            beamComp.startAudioEvent = "Play_WPN_SeriousCannon_Scream_01";


            Projectile spewProj = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(86) as Gun).DefaultModule.projectiles[0]);
            spewProj.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(spewProj.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(spewProj);
            spewProj.SetProjectileSpriteRight("sickworm_projectile", 5, 5, false, tk2dBaseSprite.Anchor.MiddleCenter, 3, 3);
            spewProj.baseData.damage = 2;
            RandomProjectileStatsComponent randomStats = spewProj.gameObject.AddComponent<RandomProjectileStatsComponent>();
            randomStats.randomScale = true;
            randomStats.randomSpeed = true;

            BeamProjSpewModifier spew = projectile.gameObject.AddComponent<BeamProjSpewModifier>();
            spew.bulletToSpew = spewProj;
            spew.accuracyVariance = 5;


            gun.DefaultModule.projectiles[0] = projectile;

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("SickWorm Clip", "NevernamedsItems/Resources/CustomGunAmmoTypes/sickworm_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/sickworm_clipempty");

            gun.quality = PickupObject.ItemQuality.A; 
            ETGMod.Databases.Items.Add(gun, false, "ANY");

        }
        public SickWorm()
        {

        }
    }
}

