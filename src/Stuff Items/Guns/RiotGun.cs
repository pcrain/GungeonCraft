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
    public class RiotGun : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Riot Gun", "riotgun");
            Game.Items.Rename("outdated_gun_mods:riot_gun", "nn:riot_gun");
            gun.gameObject.AddComponent<RiotGun>();
            gun.SetShortDescription("Definitely Humane");
            gun.SetLongDescription("Fires elastic rubber bullets."+"\n\nWhile rubber bullets are generally considered non-lethal, a more accurate term would be 'less-lethal'."+"\nThese bullets can still cause damage.");

            gun.SetupSprite(null, "riotgun_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 12);
            gun.SetAnimationFPS(gun.reloadAnimation, 1);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(150) as Gun).gunSwitchGroup;

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Automatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.DefaultModule.cooldownTime = 0.5f;
            gun.DefaultModule.numberOfShotsInClip = 5;
            gun.barrelOffset.transform.localPosition = new Vector3(2.06f, 0.75f, 0f);
            gun.SetBaseMaxAmmo(200);
            gun.gunClass = GunClass.RIFLE;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage = 7f;
            projectile.baseData.force *= 5f;
            projectile.baseData.speed *= 1f;
            projectile.baseData.range *= 1f;
            BounceProjModifier Bouncing = projectile.gameObject.GetOrAddComponent<BounceProjModifier>();
            Bouncing.numberOfBounces = 1;
            projectile.AppliesStun = true;
            projectile.StunApplyChance = 0.2f;
            projectile.AppliedStunDuration = 2f;
            projectile.SetProjectileSpriteRight("riotgun_projectile", 12, 12, true, tk2dBaseSprite.Anchor.MiddleCenter, 10, 10);


            projectile.pierceMinorBreakables = true;
            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("RiotGun Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/riotgun_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/riotgun_clipempty");

            gun.quality = PickupObject.ItemQuality.D;
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            gun.AddToSubShop(ItemBuilder.ShopType.Trorc);

            RiotGunID = gun.PickupObjectId;
            gun.SetupUnlockOnCustomFlag(CustomDungeonFlags.PURCHASED_RIOTGUN, true);
            gun.AddItemToTrorcMetaShop(10);
        }
        public static int RiotGunID;
        public RiotGun()
        {

        }
        public override void PostProcessProjectile(Projectile projectile)
        {
            PlayerController player = projectile.Owner as PlayerController;
            if (player && player.PlayerHasActiveSynergy("Crowd Supression"))
            {
                AreaFearBulletModifier fearAura = projectile.gameObject.GetOrAddComponent<AreaFearBulletModifier>();
                fearAura.effectRadius = 3f;
                fearAura.fearStartDistance = 3f;
                fearAura.fearStopDistance = 7f;
                fearAura.fearLength = 2f;
                fearAura.procChance = 1f;
                fearAura.useSpecialTint = false;
            }
            base.PostProcessProjectile(projectile);
        }
    }
}

