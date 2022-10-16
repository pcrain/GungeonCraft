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
    public class BeamBlade : AdvancedGunBehavior
    {
        public static void Add()
        {

            Gun gun = ETGMod.Databases.Items.NewGun("Beamblade", "beamblade");
            Game.Items.Rename("outdated_gun_mods:beamblade", "nn:beamblade");
            var behav = gun.gameObject.AddComponent<BeamBlade>();
            behav.preventNormalFireAudio = true;
            gun.SetShortDescription("Elegant");
            gun.SetLongDescription("An artful weapon with a blade made of pure beam."+"\n\nRelic of an ancient order of Gun Knights, whose arcane religions have since fallen into obscurity.");

            gun.SetupSprite(null, "beamblade_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 8);
            gun.isAudioLoop = true;
            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);
            gun.AddPassiveStatModifier(PlayerStats.StatType.Curse, 2f, StatModifier.ModifyMethod.ADDITIVE);

            //GUN STATS
            gun.doesScreenShake = false;
            gun.DefaultModule.ammoCost = 10;
            gun.DefaultModule.angleVariance = 0;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Beam;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.muzzleFlashEffects.type = VFXPoolType.None;
            gun.DefaultModule.cooldownTime = 0.001f;
            gun.DefaultModule.numberOfShotsInClip = 3000;
            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.BEAM;
            gun.barrelOffset.transform.localPosition = new Vector3(0.93f, 0.18f, 0f);
            gun.SetBaseMaxAmmo(3000);
            gun.ammo = 3000;
            gun.gunClass = GunClass.BEAM;

            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).wrapMode = tk2dSpriteAnimationClip.WrapMode.LoopSection;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).loopStart = 1;

            List<string> BeamAnimPaths = new List<string>()
            {
                "NevernamedsItems/Resources/BeamSprites/beamblade_mid_001",
                "NevernamedsItems/Resources/BeamSprites/beamblade_mid_002",
                "NevernamedsItems/Resources/BeamSprites/beamblade_mid_003",
                "NevernamedsItems/Resources/BeamSprites/beamblade_mid_004",
                "NevernamedsItems/Resources/BeamSprites/beamblade_mid_005",
                "NevernamedsItems/Resources/BeamSprites/beamblade_mid_006",
            };
            List<string> BeamEndPaths = new List<string>()
            {
                "NevernamedsItems/Resources/BeamSprites/beamblade_end_001",
                "NevernamedsItems/Resources/BeamSprites/beamblade_end_002",
                "NevernamedsItems/Resources/BeamSprites/beamblade_end_003",
                "NevernamedsItems/Resources/BeamSprites/beamblade_end_004",
                "NevernamedsItems/Resources/BeamSprites/beamblade_end_005",
                "NevernamedsItems/Resources/BeamSprites/beamblade_end_006",
            };

            //BULLET STATS
            Projectile projectile = ProjectileUtility.SetupProjectile(86);

            BasicBeamController beamComp = projectile.GenerateBeamPrefab(
                "NevernamedsItems/Resources/BeamSprites/beamblade_mid_001",
                new Vector2(5, 3),
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
                new Vector2(5, 3),
                new Vector2(0, 1),
                //Beginning
                null,
                -1,
                null,
                null
                );

            projectile.baseData.damage = 70f;
            projectile.baseData.force *= 0.1f;
            projectile.baseData.range = 3.5f;
            projectile.baseData.speed *= 1f;

            beamComp.penetration = 100;
            beamComp.boneType = BasicBeamController.BeamBoneType.Straight;
            beamComp.interpolateStretchedBones = false;
            beamComp.endAudioEvent = "Stop_WPN_All";
            beamComp.startAudioEvent = "Play_WPN_moonscraperLaser_shot_01";

            gun.DefaultModule.projectiles[0] = projectile;

            gun.quality = PickupObject.ItemQuality.C; 
            ETGMod.Databases.Items.Add(gun, false, "ANY");

        }
        public BeamBlade()
        {

        }
    }
}
