using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using ItemAPI;
using MonoMod.RuntimeDetour;
using System.Reflection;
using SaveAPI;

namespace NevernamedsItems
{
    public class Rebondir : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Rebondir", "rebondir");
            Game.Items.Rename("outdated_gun_mods:rebondir", "nn:rebondir");
            gun.gameObject.AddComponent<Rebondir>();
            gun.SetShortDescription("Mort Indirecte");
            gun.SetLongDescription("Fires bouncy bolts of energy that only get stronger with each ricochet."+"\n\nUsed by Hegemony of Man soldiers to clear hostile structures without setting foot inside.");

            gun.SetupSprite(null, "rebondir_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 12);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(89) as Gun).gunSwitchGroup;
            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);
            gun.gunHandedness = GunHandedness.TwoHanded;
            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(89) as Gun).muzzleFlashEffects;
            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Automatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1.1f;
            gun.DefaultModule.cooldownTime = 0.11f;
            gun.DefaultModule.numberOfShotsInClip = 20;
            gun.barrelOffset.transform.localPosition = new Vector3(1.56f, 0.31f, 0f);
            gun.SetBaseMaxAmmo(450);
            gun.ammo = 450;
            gun.gunClass = GunClass.FULLAUTO;

            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage = 5.5f;
            projectile.baseData.speed *= 3f;
            projectile.baseData.range *= 10f;
            BounceProjModifier bounce = projectile.gameObject.GetOrAddComponent<BounceProjModifier>();
            bounce.numberOfBounces = 3;
            bounce.damageMultiplierOnBounce = 1.3f;
            projectile.SetProjectileSpriteRight("initiatewand_bouncer", 4, 4, true, tk2dBaseSprite.Anchor.MiddleCenter, 4, 4);
            EasyTrailBullet trail4 = projectile.gameObject.AddComponent<EasyTrailBullet>();
            trail4.TrailPos = projectile.transform.position;
            trail4.StartWidth = 0.125f;
            trail4.EndWidth = 0f;
            trail4.LifeTime = 0.5f;
            trail4.BaseColor = ExtendedColours.poisonGreen;
            trail4.EndColor = ExtendedColours.poisonGreen;
            projectile.hitEffects.alwaysUseMidair = true;
            projectile.hitEffects.overrideMidairDeathVFX = (PickupObjectDatabase.GetById(89) as Gun).DefaultModule.projectiles[0].hitEffects.overrideMidairDeathVFX;
            projectile.transform.parent = gun.barrelOffset;
            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = "green_small";
            gun.quality = PickupObject.ItemQuality.B;
            ETGMod.Databases.Items.Add(gun, null, "ANY");
            gun.SetupUnlockOnCustomFlag(CustomDungeonFlags.PURCHASED_REBONDIR, true);

            gun.AddToSubShop(ItemBuilder.ShopType.Trorc);
            RebondirID = gun.PickupObjectId;
        }
        public static int RebondirID;
    }
}
