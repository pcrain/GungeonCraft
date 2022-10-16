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
    public class Autogun : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Autogun", "autogun");
            Game.Items.Rename("outdated_gun_mods:autogun", "nn:autogun");
            var behav = gun.gameObject.AddComponent<Autogun>();
            behav.preventNormalFireAudio = true;
            gun.SetShortDescription("Fully Fully Automatic");
            gun.SetLongDescription("Fires weak energy bolts programmed to seek and destroy."+"\n\nBrought to the Gungeon by an incompetent spacefarer who couldn't hit the broad side of a barn... from inside the barn.");

            gun.doesScreenShake = false;
            gun.SetupSprite(null, "autogun_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 12);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(89) as Gun).gunSwitchGroup;
            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);
            gun.gunHandedness = GunHandedness.TwoHanded;
            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(36) as Gun).muzzleFlashEffects;
            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Automatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 2f;
            gun.DefaultModule.cooldownTime = 0.02f;
            gun.DefaultModule.numberOfShotsInClip = -1;
            gun.barrelOffset.transform.localPosition = new Vector3(1.43f, 0.31f, 0f);
            gun.SetBaseMaxAmmo(2500);
            gun.ammo = 2500;
            gun.gunClass = GunClass.FULLAUTO;
            
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).frames[0].eventAudio = "Play_MouthPopSound";
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).frames[0].triggerEvent = true;

            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(445) as Gun).DefaultModule.projectiles[0].GetComponent<SpawnProjModifier>().projectileToSpawnOnCollision);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage = 1f;
            projectile.baseData.range *= 10f;
            projectile.baseData.force *= 0.05f;
            BounceProjModifier bounce = projectile.gameObject.GetOrAddComponent<BounceProjModifier>();
            bounce.numberOfBounces = 1;

            projectile.SetProjectileSpriteRight("autogun_proj", 2, 2, true, tk2dBaseSprite.Anchor.MiddleCenter, 2, 2);
            EasyTrailBullet trail4 = projectile.gameObject.AddComponent<EasyTrailBullet>();
            trail4.TrailPos = projectile.transform.position;
            trail4.StartWidth = 0.2f;
            trail4.EndWidth = 0f;
            trail4.LifeTime = 0.1f;
            trail4.BaseColor = ExtendedColours.freezeBlue;
            trail4.EndColor = ExtendedColours.freezeBlue;

            projectile.hitEffects.alwaysUseMidair = true;
            projectile.hitEffects.overrideMidairDeathVFX = (PickupObjectDatabase.GetById(18) as Gun).DefaultModule.projectiles[0].hitEffects.overrideMidairDeathVFX;

            projectile.transform.parent = gun.barrelOffset;
            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.BEAM;

            gun.quality = PickupObject.ItemQuality.C;
            ETGMod.Databases.Items.Add(gun, null, "ANY");
            gun.SetupUnlockOnCustomFlag(CustomDungeonFlags.PURCHASED_AUTOGUN, true);

            gun.AddToSubShop(ItemBuilder.ShopType.Trorc);
            AutogunID = gun.PickupObjectId;
        }
        public static int AutogunID;
    }
}

