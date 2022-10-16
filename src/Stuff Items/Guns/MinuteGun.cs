using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using Alexandria.ItemAPI;

namespace NevernamedsItems
{
    public class MinuteGun : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Minute Gun", "minutegun");
            Game.Items.Rename("outdated_gun_mods:minute_gun", "nn:minute_gun");
            gun.gameObject.AddComponent<MinuteGun>();
            gun.SetShortDescription("You Have 60 Seconds");
            gun.SetLongDescription("Usable for only 60 seconds each floor." + "\n\nA tiny adventurer managed to bring this all the way from his home in the swamps to the Gungeon's Entrance before his time was up.");

            gun.SetupSprite(null, "minutegun_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 16);

            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(86) as Gun).gunSwitchGroup;
            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 0;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.DefaultModule.cooldownTime = 0.1f;
            gun.DefaultModule.angleVariance = 5;
            gun.DefaultModule.numberOfShotsInClip = 7;
            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(84) as Gun).muzzleFlashEffects;
            gun.barrelOffset.transform.localPosition = new Vector3(1.75f, 0.87f, 0f);
            gun.SetBaseMaxAmmo(0);
            gun.ammo = 60;
            gun.gunClass = GunClass.PISTOL;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            projectile.SetProjectileSpriteRight("minutegun_projectile", 16, 9, true, tk2dBaseSprite.Anchor.MiddleCenter, 15, 8);
            projectile.hitEffects.overrideMidairDeathVFX = EasyVFXDatabase.WhiteCircleVFX;
            projectile.hitEffects.alwaysUseMidair = true;
            projectile.baseData.damage = 20;
            gun.DefaultModule.projectiles[0] = projectile;
            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("Minute Gun Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/minutegun_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/minutegun_clipempty");
            gun.quality = PickupObject.ItemQuality.C;
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            MinuteGunID = gun.PickupObjectId;
        }
        public static int MinuteGunID;
        private float timer;
        public override void OnSwitchedToThisGun()
        {
            if (timer > 0)
            {
                gun.MoveBulletsIntoClip(Mathf.CeilToInt(timer));
            }
            base.OnSwitchedToThisGun();
        }
        public override void PostProcessProjectile(Projectile projectile)
        {
            if (gun.GunPlayerOwner() && gun.GunPlayerOwner().PlayerHasActiveSynergy("Nick Of Time")) projectile.baseData.damage *= (3 - (0.05f * timer));
            base.PostProcessProjectile(projectile);
        }
        protected override void OnPickedUpByPlayer(PlayerController player)
        {
            if (!everPickedUpByPlayer)
            {
                timer = (60 * player.stats.GetStatValue(PlayerStats.StatType.AmmoCapacityMultiplier));
            }
            else
            {
                gun.CurrentAmmo = Mathf.CeilToInt(timer);
            }
            GameManager.Instance.OnNewLevelFullyLoaded += this.OnNewLevel;
            base.OnPickedUpByPlayer(player);
        }
        protected override void OnPostDroppedByPlayer(PlayerController player)
        {
            GameManager.Instance.OnNewLevelFullyLoaded -= this.OnNewLevel;
            base.OnPostDroppedByPlayer(player);
        }
        public override void OnDestroy()
        {
            GameManager.Instance.OnNewLevelFullyLoaded -= this.OnNewLevel;
            base.OnDestroy();
        }
        private void OnNewLevel()
        {
            if (Owner && Owner is PlayerController)
            {
                timer = (60 * (Owner as PlayerController).stats.GetStatValue(PlayerStats.StatType.AmmoCapacityMultiplier));
            }
        }
        protected override void Update()
        {
            if (timer >= 0)
            {
                if (gun && gun.CurrentOwner && gun.CurrentOwner.CurrentGun.PickupObjectId == MinuteGunID)
                {
                    timer -= BraveTime.DeltaTime;
                }
            }
            gun.CurrentAmmo = Mathf.CeilToInt(timer);
            base.Update();
        }
        public MinuteGun()
        {

        }
    }
}
