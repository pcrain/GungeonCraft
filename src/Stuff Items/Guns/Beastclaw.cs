using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using ItemAPI;
using SaveAPI;

namespace NevernamedsItems
{
    public class Beastclaw : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Beastclaw", "beastclaw");
            Game.Items.Rename("outdated_gun_mods:beastclaw", "nn:beastclaw");
            gun.gameObject.AddComponent<Beastclaw>();
            gun.SetShortDescription("Out of Place");
            gun.SetLongDescription("The weaponised claw of a mighty Misfire Beast." + "\n\nRumour has it that infamous big game hunter Emmitt Calx had one of the beasts as a tame pet and lap cat.");

            gun.SetupSprite(null, "beastclaw_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 12);
            gun.SetAnimationFPS(gun.reloadAnimation, 1);

            gun.muzzleFlashEffects.type = VFXPoolType.None;

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Automatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.DefaultModule.cooldownTime = 0.05f;
            gun.DefaultModule.angleVariance = 20f;
            gun.DefaultModule.numberOfShotsInClip = 100;
            gun.barrelOffset.transform.localPosition = new Vector3(0.93f, 0.37f, 0f);
            gun.SetBaseMaxAmmo(1500);
            gun.ammo = 1500;
            gun.gunClass = GunClass.SILLY;


            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage *= 0.8f;
            projectile.baseData.speed *= 1f;
            projectile.baseData.range *= 1f;
            projectile.SetProjectileSpriteRight("enemystyle_projectile", 10, 10, true, tk2dBaseSprite.Anchor.MiddleCenter, 8, 8);
            InstantTeleportToPlayerCursorBehav tp = projectile.gameObject.GetOrAddComponent<InstantTeleportToPlayerCursorBehav>();
            

            gun.quality = PickupObject.ItemQuality.A;
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            gun.AddToSubShop(ItemBuilder.ShopType.Cursula);

            BeastclawID = gun.PickupObjectId;
            gun.SetupUnlockOnCustomFlag(CustomDungeonFlags.MISFIREBEAST_QUEST_REWARDED, true);
        }
        public static int BeastclawID;
        
        public Beastclaw()
        {

        }
    }
}