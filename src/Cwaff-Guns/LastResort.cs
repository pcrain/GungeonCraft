﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using UnityEngine;
using MonoMod;
using MonoMod.RuntimeDetour;
using Gungeon;
using Alexandria.Misc;
using Alexandria.ItemAPI;

namespace CwaffingTheGungy
{
    public class LastResort : AdvancedGunBehavior
    {
        public static string gunName          = "Last Resort";
        public static string spriteName       = "converter";
        public static string projectileName   = "ak-47";
        public static string shortDescription = "Way Past Plan B";
        public static string longDescription  = "(Gains stats for every ammo-less gun you have in your inventory.)";

        public static List<string> lastResortLevelSprites;
        public static List<Projectile> lastResortProjectiles;
        public static Projectile lastResortBaseProjectile;
        public PlayerController owner;

        public static void Add()
        {
            Gun gun = Lazy.InitGunFromStrings(gunName, spriteName, projectileName, shortDescription, longDescription);
            var comp = gun.gameObject.AddComponent<LastResort>();
            comp.preventNormalFireAudio = true;
            comp.preventNormalReloadAudio = true;

            gun.gunSwitchGroup                    = (PickupObjectDatabase.GetById(198) as Gun).gunSwitchGroup;
            gun.DefaultModule.shootStyle          = ProjectileModule.ShootStyle.Automatic;
            gun.DefaultModule.sequenceStyle       = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.quality                           = PickupObject.ItemQuality.C;
            gun.DefaultModule.ammoCost            = 1;
            gun.SetBaseMaxAmmo(1000);
            gun.SetAnimationFPS(gun.shootAnimation, 24);

            gun.reloadTime                        = 2.0f;
            gun.DefaultModule.cooldownTime        = 0.4f;
            gun.DefaultModule.numberOfShotsInClip = 10;

            lastResortProjectiles = new List<Projectile>();
            lastResortLevelSprites = new List<string>();

            Projectile projectile       = Lazy.PrefabProjectileFromGun(gun);
            projectile.baseData.damage  = 0f;  //dummy value to check when stats need to be recalculated
            projectile.baseData.speed   = 5.0f;
            projectile.baseData.range   = 5.0f;
            projectile.transform.parent = gun.barrelOffset;

            // No guns without ammo (base stats)
            Projectile p0 = Lazy.PrefabProjectileFromExistingProjectile(projectile);
            p0.baseData.damage = 2f;
            lastResortProjectiles.Add(p0);

            // 1+ guns without ammo (scale stats from last projectile)
            for(int i = 1; i < 5; ++i)
            {
                Projectile po      = lastResortProjectiles[i-1];
                Projectile pi      = Lazy.PrefabProjectileFromExistingProjectile(po);
                pi.baseData.damage = po.baseData.damage * 2;
                pi.baseData.speed  = po.baseData.speed * 2;
                pi.baseData.range  = po.baseData.range * 2;
                lastResortProjectiles.Add(pi);
                lastResortLevelSprites.Add("PumpChargeMeter"+i);
            }

            lastResortBaseProjectile = projectile;
        }

        public override bool CollectedAmmoPickup(PlayerController player, Gun self, AmmoPickup pickup)
        {
            pickup.ForcePickupWithoutGainingAmmo(player);
            return false;
        }

        protected override void Update()
        {
            base.Update();
            if (!(this.gun && this.gun.GunPlayerOwner()))
                return;
            this.owner = this.gun.GunPlayerOwner();
            // TODO: hack to detect reset gun stat changes, find a better way later
            if (this.gun.DefaultModule.projectiles[0].baseData.damage == 0f)
                ComputeLastResortStats();
        }

        public override void OnSwitchedToThisGun()
        {
            base.OnSwitchedToThisGun();
            ComputeLastResortStats();
            AkSoundEngine.PostEvent("Play_OBJ_silenceblank_small_01", this.gameObject);
        }

        protected override void OnPickedUpByPlayer(PlayerController player)
        {
            base.OnPickedUpByPlayer(player);
            this.owner = player;
            ComputeLastResortStats();
            AkSoundEngine.PostEvent("Play_OBJ_silenceblank_small_01", this.gameObject);
            // owner.ShowOverheadAnimatedVFX("PumpChargeAnimated", 2);
        }

        protected override void OnPickup(GameActor owner)
        {
            base.OnPickup(owner);
        }

        private void ComputeLastResortStats()
        {
            if (!(this.gun && this.gun.GunPlayerOwner()))
                return;
            int ammoless = 0;
            foreach (Gun gun in this.owner.inventory.AllGuns)
            {
                if (!(gun == this.gun || gun.InfiniteAmmo || gun.ammo > 0))
                    ++ammoless;
            }
            // ETGModConsole.Log("Num guns with 0 ammo: "+ammoless);
            this.gun.DefaultModule.projectiles[0] =
                lastResortProjectiles[Math.Min(ammoless,lastResortProjectiles.Count)];
            this.gun.reloadTime                        = 2.0f / (float)Math.Pow(1.5,ammoless);
            this.gun.DefaultModule.cooldownTime        = 0.4f / (float)Math.Pow(1.5,ammoless);
            this.gun.DefaultModule.numberOfShotsInClip = 4 * (1+ammoless);
            this.overrideNormalFireAudio = "Play_WPN_blasphemy_shot_01";
            if (ammoless > 0)
                this.owner.ShowOverheadVFX(lastResortLevelSprites[ammoless-1], 1);
        }
    }
}