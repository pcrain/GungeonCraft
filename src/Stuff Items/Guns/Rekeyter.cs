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

    public class Rekeyter : GunBehaviour
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Rekeyter", "rekeyter");
            Game.Items.Rename("outdated_gun_mods:rekeyter", "nn:rekeyter");
            gun.gameObject.AddComponent<Rekeyter>();
            gun.SetShortDescription("Click");
            gun.SetLongDescription("A key clumsily fused to a grip and trigger. Low chance to open chests." + "\n\nHidden sidearm of the infamous criminal Locke Smith.");

            gun.SetupSprite(null, "rekeyter_idle_001", 8);

            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(95) as Gun).gunSwitchGroup;

            gun.SetAnimationFPS(gun.shootAnimation, 15);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Burst;
            gun.DefaultModule.burstShotCount = 2;
            gun.DefaultModule.burstCooldownTime = 0.11f;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.DefaultModule.cooldownTime = 0.5f;
            gun.DefaultModule.numberOfShotsInClip = 10;
            gun.barrelOffset.transform.localPosition = new Vector3(1.93f, 0.81f, 0f);
            gun.SetBaseMaxAmmo(200);
            gun.gunClass = GunClass.PISTOL;

            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage *= 1.6f;
            projectile.baseData.range *= 1f;
            projectile.transform.parent = gun.barrelOffset;

            KeyBulletBehaviour unlocking = projectile.gameObject.GetOrAddComponent<KeyBulletBehaviour>();
            unlocking.useSpecialTint = false;
            unlocking.procChance = 0.1f;
            ScaleProjectileStatOffConsumableCount keyDamage = projectile.gameObject.GetOrAddComponent<ScaleProjectileStatOffConsumableCount>();
            keyDamage.multiplierPerLevelOfStat = 0.1f;
            keyDamage.projstat = ScaleProjectileStatOffConsumableCount.ProjectileStatType.DAMAGE;
            keyDamage.consumableType = ScaleProjectileStatOffConsumableCount.ConsumableType.KEYS;

            projectile.SetProjectileSpriteRight("rekeyter_projectile", 17, 7, false, tk2dBaseSprite.Anchor.MiddleCenter, 17, 6);
            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("Rekeyter Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/rekeyter_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/rekeyter_clipempty");
            gun.quality = PickupObject.ItemQuality.B;
            gun.encounterTrackable.EncounterGuid = "this is the Rekeyter";
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            gun.AddToSubShop(ItemBuilder.ShopType.Flynt);
            RekeyterID = gun.PickupObjectId;
        }
        public static int RekeyterID;
        public Rekeyter()
        {

        }
        public override void Update()
        {
            if (gun.CurrentOwner is PlayerController)
            {
            PlayerController player = gun.CurrentOwner as PlayerController;
                if (player.PlayerHasActiveSynergy("ReShelletonKeyter") && !gun.InfiniteAmmo)
                {
                    gun.InfiniteAmmo = true;
                }
                else if (!player.PlayerHasActiveSynergy("ReShelletonKeyter") && gun.InfiniteAmmo)
                {
                    gun.InfiniteAmmo = false;
                }
            }
        }
    }
}
