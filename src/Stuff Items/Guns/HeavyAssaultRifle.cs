using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Reflection;
using Gungeon;
using MonoMod;
using UnityEngine;
using ItemAPI;

namespace NevernamedsItems
{

    public class HeavyAssaultRifle : GunBehaviour
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Heavy Assault Rifle", "heavyassaultrifle");
            Game.Items.Rename("outdated_gun_mods:heavy_assault_rifle", "nn:heavy_assault_rifle");
            gun.gameObject.AddComponent<HeavyAssaultRifle>();
            gun.SetShortDescription("It's so big...");
            gun.SetLongDescription("An oversized assault rifle from the FUTURE... which by now has already become the past.");
            
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(81) as Gun).gunSwitchGroup;

            gun.SetupSprite(null, "heavyassaultrifle_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 15);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Automatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 2.5f;
            gun.DefaultModule.cooldownTime = 0.11f;
            gun.DefaultModule.numberOfShotsInClip = 40;
            gun.barrelOffset.transform.localPosition = new Vector3(4.18f, 0.87f, 0f);
            gun.SetBaseMaxAmmo(250);
            gun.gunClass = GunClass.FULLAUTO;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);

            projectile.baseData.damage *= 6f;
            projectile.SetProjectileSpriteRight("heavyassaultrifle_projectile", 22, 7, false, tk2dBaseSprite.Anchor.MiddleCenter, 22, 7);

            projectile.transform.parent = gun.barrelOffset;
            PierceProjModifier orAddComponent = projectile.gameObject.GetOrAddComponent<PierceProjModifier>();
            orAddComponent.penetratesBreakables = true;
            orAddComponent.penetration = 5;

            gun.AddCurrentGunStatModifier(PlayerStats.StatType.MovementSpeed, 0.8f, StatModifier.ModifyMethod.MULTIPLICATIVE);
            gun.AddCurrentGunStatModifier(PlayerStats.StatType.DodgeRollSpeedMultiplier, 0.8f, StatModifier.ModifyMethod.MULTIPLICATIVE);

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("HeavyAssaultRifle Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/heavyassaultrifle_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/heavyassaultrifle_clipempty");

            gun.DefaultModule.projectiles[0] = projectile;
            gun.quality = PickupObject.ItemQuality.S;
            gun.encounterTrackable.EncounterGuid = "this is the Heavy Assault Rifle";
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            HeavyAssaultRifleID = gun.PickupObjectId;

            gun.AddToSubShop(ItemBuilder.ShopType.Trorc);

        }
        public static int HeavyAssaultRifleID;
        public HeavyAssaultRifle()
        {

        }
    }
}