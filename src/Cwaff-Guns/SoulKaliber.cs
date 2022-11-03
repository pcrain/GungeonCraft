using System;
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
using Dungeonator;

namespace CwaffingTheGungy
{
    public class SoulKaliber : AdvancedGunBehavior
    {
        public static string gunName          = "Soul Kaliber";
        public static string spriteName       = "ringer";
        public static string projectileName   = "ak-47";
        public static string shortDescription = "Gundead or Alive";
        public static string longDescription  = "(hitting an enemy gives them the soul link status effect, making all soul linked enemies take damage when any enemy is hit)";

        public static Projectile gunprojectile;
        public static Projectile fakeprojectile;

        private int oldammo = 1;

        public static void Add()
        {
            Gun gun = Lazy.InitGunFromStrings(gunName, spriteName, projectileName, shortDescription, longDescription);

            var comp = gun.gameObject.AddComponent<SoulKaliber>();
            comp.preventNormalFireAudio = true;

            gun.muzzleFlashEffects.type           = VFXPoolType.None;
            gun.gunSwitchGroup                    = (PickupObjectDatabase.GetById(198) as Gun).gunSwitchGroup;
            gun.DefaultModule.ammoCost            = 1;
            gun.DefaultModule.shootStyle          = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle       = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime                        = 1.1f;
            gun.DefaultModule.cooldownTime        = 0.1f;
            gun.DefaultModule.numberOfShotsInClip = 10;
            gun.quality                           = PickupObject.ItemQuality.D;
            gun.SetBaseMaxAmmo(250);
            gun.SetAnimationFPS(gun.shootAnimation, 24);

            Projectile projectile = Lazy.PrefabProjectileFromGun(gun);
            projectile.baseData.speed   = 30.0f;
            projectile.baseData.damage  = 1f;
            projectile.gameObject.AddComponent<SoulLinkProjectile>();
        }

    }

    public class SoulLinkProjectile : MonoBehaviour
    {
        private Projectile m_projectile;

        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            this.m_projectile.OnHitEnemy += this.OnHitEnemy;
        }

        private void OnHitEnemy(Projectile bullet, SpeculativeRigidbody enemy, bool what)
        {
            enemy.aiActor.ApplyEffect(SoulLinkStatusEffectSetup.StandardSoulLinkEffect);
            var comp = enemy.aiActor.gameObject.AddComponent<UnderEffectsOfSoulLink>();
            enemy.healthHaver.ModifyDamage += this.OnTakeDamage;
        }

        private void OnTakeDamage(HealthHaver enemy, HealthHaver.ModifyDamageEventArgs data)
        {
            List<AIActor> activeEnemies = enemy.aiActor.GetAbsoluteParentRoom().GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
            if (activeEnemies == null)
                return;
            for (int i = 0; i < activeEnemies.Count; i++)
            {
                AIActor otherEnemy = activeEnemies[i];
                if (!(otherEnemy && otherEnemy.specRigidbody && !otherEnemy.IsGone && otherEnemy.healthHaver))
                    continue;
                var comp = otherEnemy.gameObject.GetComponent<UnderEffectsOfSoulLink>();
                if (!comp)
                    continue;
                comp.TryApplyDamage(otherEnemy.healthHaver);
            }
        }
    }

    public class UnderEffectsOfSoulLink : MonoBehaviour
    {
        private AIActor m_enemy;
        private float m_cooldown;
        private static VFXPool vfx = null;

        private void Start()
        {
            if (vfx == null)
                vfx = VFX.CreatePoolFromVFXGameObject((PickupObjectDatabase.GetById(0) as Gun).DefaultModule.projectiles[0].hitEffects.overrideMidairDeathVFX);
            this.m_enemy = base.GetComponent<AIActor>();
            this.m_cooldown = 0;
            // dummy class
        }

        public void TryApplyDamage(HealthHaver enemyHH)
        {
            if (this.m_cooldown <= 0)
            {
                this.m_cooldown = 0.1f;
                // Vector2 directionToPlayer = bouncer.projectile.Owner.specRigidbody.UnitCenter - bouncer.specRigidbody.UnitCenter;
                enemyHH.ApplyDamage(1f, new Vector2(10f,0f), "Soul Link",
                    CoreDamageTypes.Magic, DamageCategory.Collision,
                    false, null, false);

                Vector2 ppos = this.m_enemy.sprite.WorldCenter;
                for (int i = 0; i < 3; ++i)
                {
                    Vector2 finalpos = ppos + Lazy.AngleToVector(120*i,1);
                    vfx.SpawnAtPosition(
                        finalpos.ToVector3ZisY(-1f), /* -1 = above player sprite */
                        120*i,
                        null, null, null, -0.05f);
                }
            }
        }

        private void Update()
        {
            if (this.m_cooldown > 0f)
            {
                this.m_cooldown -= BraveTime.DeltaTime;
            }
        }
    }
}
