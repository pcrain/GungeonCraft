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
    public class Tranquilizer : AdvancedGunBehavior
    {
        public static string gunName          = "Tranquilizer";
        public static string spriteName       = "biotranstater2100";
        public static string projectileName   = "ak-47";
        public static string shortDescription = "Zzzzzz";
        public static string longDescription  = "(10 seconds after being hit, enemy is permastunned)";

        public static void Add()
        {
            Gun gun = Lazy.InitGunFromStrings(gunName, spriteName, projectileName, shortDescription, longDescription);
            var comp = gun.gameObject.AddComponent<Tranquilizer>();

            gun.gunSwitchGroup                    = (PickupObjectDatabase.GetById(198) as Gun).gunSwitchGroup;
            gun.DefaultModule.ammoCost            = 1;
            gun.DefaultModule.shootStyle          = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle       = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime                        = 1.1f;
            gun.DefaultModule.cooldownTime        = 0.1f;
            gun.DefaultModule.numberOfShotsInClip = 20;
            gun.quality                           = PickupObject.ItemQuality.D;
            gun.SetBaseMaxAmmo(250);
            gun.SetAnimationFPS(gun.shootAnimation, 24);

            Projectile projectile       = Lazy.PrefabProjectileFromGun(gun);
            projectile.transform.parent = gun.barrelOffset;

            TranquilizerBehavior tranq = projectile.gameObject.AddComponent<TranquilizerBehavior>();
            tranq.stundelay            = 10;
            tranq.stuntime             = 3600;
        }

    }

    public class TranquilizerBehavior : MonoBehaviour
    {
        public int stundelay = 1;
        public int stuntime  = 1;

        private void Start()
        {
            base.GetComponent<Projectile>().OnHitEnemy += OnHitEnemy;
        }

        private void OnHitEnemy(Projectile bullet, SpeculativeRigidbody enemy, bool what)
        {
            var t = enemy.aiActor.gameObject.AddComponent<EnemyTranquilizedBehavior>();
            t.stuntime = this.stuntime;
            t.stundelay = this.stundelay;
        }

        private class EnemyTranquilizedBehavior : MonoBehaviour
        {
            public float stuntime  = 1;
            public float stundelay = 1;
            private AIActor m_enemy;
            private void Start()
            {
                this.m_enemy = base.GetComponent<AIActor>();
                Invoke("Tranquilize",stundelay);
            }
            private void Tranquilize()
            {
                this.m_enemy.behaviorSpeculator.Stun(this.stuntime); //an hour should be long enough
            }
        }
    }

}