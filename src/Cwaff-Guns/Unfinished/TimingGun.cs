﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using UnityEngine;
using MonoMod.RuntimeDetour;

using Gungeon;
using Dungeonator;
using Alexandria.ItemAPI;
using Alexandria.Misc;

namespace CwaffingTheGungy
{
    public class TimingGun : AdvancedGunBehavior
    {
        public static string ItemName         = "Timing Gun";
        public static string SpriteName       = "agargun";
        public static string ProjectileName   = "ak-47";
        public static string ShortDescription = "One You Can Count On";
        public static string LongDescription  = "(charge 1-10, different effects depending on charge)";

        public static List<string> timingLevelSprites;
        public static List<Projectile> timingProjectiles;
        public PlayerController owner;

        private static int maxCharge = 10;
        private static int spinSpeed = 6;
        private int curCharge = 0;
        private int curSpin = 0;
        private GameObject theCounter = null;

        public static void Add()
        {
            Gun gun = Lazy.SetupGun(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription);
            var comp = gun.gameObject.AddComponent<TimingGun>();

            gun.gunSwitchGroup                    = (ItemHelper.Get(Items.GunslingersAshes) as Gun).gunSwitchGroup;
            gun.DefaultModule.shootStyle          = ProjectileModule.ShootStyle.Automatic;
            gun.DefaultModule.sequenceStyle       = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.quality                           = PickupObject.ItemQuality.C;
            gun.DefaultModule.ammoCost            = 1;
            gun.SetBaseMaxAmmo(300);
            gun.SetAnimationFPS(gun.shootAnimation, 24);

            gun.reloadTime                        = 2.0f;
            gun.DefaultModule.cooldownTime        = 0.2f;
            gun.DefaultModule.numberOfShotsInClip = 12;

            timingProjectiles = new List<Projectile>();
            timingLevelSprites = new List<string>();

            Projectile projectile       = Lazy.PrefabProjectileFromGun(gun);
            projectile.baseData.damage  = 0f;  //dummy value to check when stats need to be recalculated
            projectile.baseData.speed   = 5.0f;
            projectile.baseData.range   = 5.0f;
            projectile.transform.parent = gun.barrelOffset;

            // No guns without ammo (base stats)
            Projectile p0 = Lazy.PrefabProjectileFromExistingProjectile(projectile);
            p0.baseData.damage = 1f;
            p0.baseData.speed  = 2f;
            p0.baseData.range  = 2f;
            timingProjectiles.Add(p0);

            // 1+ guns without ammo (scale stats from last projectile)
            for(int i = 1; i < maxCharge; ++i)
            {
                Projectile po      = timingProjectiles[i-1];
                Projectile pi      = Lazy.PrefabProjectileFromExistingProjectile(po);
                pi.baseData.damage = po.baseData.damage * 1.4f;
                pi.baseData.speed  = po.baseData.speed * 1.4f;
                pi.baseData.range  = po.baseData.range * 1.4f;
                timingProjectiles.Add(pi);
                timingLevelSprites.Add(i.ToString());
            }
        }

        public override Projectile OnPreFireProjectileModifier(Gun gun, Projectile projectile, ProjectileModule mod)
        {
            return timingProjectiles[this.curCharge];
        }

        protected override void Update()
        {
            base.Update();
            if (!this.Player)
                return;

            if (++this.curSpin != spinSpeed)
                return;

            this.curSpin = 0;
            this.curCharge = (this.curCharge+1) % maxCharge;
            if (theCounter != null)
                UnityEngine.Object.Destroy(theCounter);
            theCounter = Instantiate<GameObject>(
                            VFX.animations[curCharge.ToString()],
                            this.Player.specRigidbody.sprite.WorldTopCenter + new Vector2(0f,0.5f),
                            Quaternion.identity,
                            this.Player.specRigidbody.transform);
            theCounter.transform.localScale = new Vector3(0.2f,0.2f,0.2f);
        }
    }
}