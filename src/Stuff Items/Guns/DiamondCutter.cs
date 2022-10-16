using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using ItemAPI;

namespace NevernamedsItems
{

    public class DiamondCutter : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Diamond Cutter", "diamondcutter");
            Game.Items.Rename("outdated_gun_mods:diamond_cutter", "nn:diamond_cutter");
            var behav = gun.gameObject.AddComponent<DiamondCutter>();
            behav.preventNormalReloadAudio = true;
            behav.preventNormalFireAudio = true;
            behav.overrideNormalFireAudio = "Play_WPN_blasphemy_shot_01";
            gun.SetShortDescription("Face It!");
            gun.SetLongDescription("Fires piercing gemstones."+"\n\nLeft in a chest by a powerful warrior of shimmering crystal... who didn't show up to work today.");

            gun.SetupSprite(null, "diamondcutter_idle_001", 8);
            gun.SetAnimationFPS(gun.reloadAnimation, 1);

            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(97) as Gun).muzzleFlashEffects;

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 0.8f;
            gun.DefaultModule.cooldownTime = 0.2f;
            gun.DefaultModule.numberOfShotsInClip = 3;
            gun.barrelOffset.transform.localPosition = new Vector3(1.81f, 1.43f, 0f);
            gun.SetBaseMaxAmmo(100);
            gun.ammo = 100;
            gun.gunClass = GunClass.SILLY;

            gun.AddPassiveStatModifier(PlayerStats.StatType.Curse, 1f, StatModifier.ModifyMethod.ADDITIVE);

            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            projectile.baseData.damage = 10f;
            projectile.baseData.speed *= 1.5f;
            projectile.hitEffects.alwaysUseMidair = true;
            projectile.hitEffects.overrideMidairDeathVFX = (PickupObjectDatabase.GetById(506) as Gun).DefaultModule.projectiles[0].hitEffects.overrideMidairDeathVFX;

            PierceProjModifier pierce = projectile.gameObject.AddComponent<PierceProjModifier>();
            pierce.penetration = 10;
            MaintainDamageOnPierce maintain = projectile.gameObject.AddComponent<MaintainDamageOnPierce>();
            maintain.damageMultOnPierce = 2f;
            projectile.pierceMinorBreakables = true;

            projectile.SetProjectileSpriteRight("diamondcutter_proj", 23, 13, false, tk2dBaseSprite.Anchor.MiddleCenter, 17, 5);
            gun.DefaultModule.projectiles[0] = projectile;

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("DiamondCutter Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/diamondcutter_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/diamondcutter_clipempty");

            gun.quality = PickupObject.ItemQuality.B;
            ETGMod.Databases.Items.Add(gun, null, "ANY");
            DiamondCutterID = gun.PickupObjectId;

        }
        public static int DiamondCutterID;
    }
}
