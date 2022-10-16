using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using ItemAPI;

namespace NevernamedsItems
{
    public class PunishmentRay : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Punishment Ray", "punishmentray");
            Game.Items.Rename("outdated_gun_mods:punishment_ray", "nn:punishment_ray");
            gun.gameObject.AddComponent<PunishmentRay>();
            gun.SetShortDescription("STREAK");
            gun.SetLongDescription("Repeatedly landing shots builds up a damage increasing streak, but losing a high enough streak hurts." + "\n\nHave you been a bad Gungeoneer?");

            gun.SetupSprite(null, "punishmentray_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 10);

            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(32) as Gun).gunSwitchGroup;
            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.DefaultModule.cooldownTime = 0.4f;
            gun.muzzleFlashEffects.type = VFXPoolType.None;
            gun.DefaultModule.numberOfShotsInClip = 6;
            gun.DefaultModule.angleFromAim = 0f;
            gun.DefaultModule.angleVariance = 0f;
            gun.barrelOffset.transform.localPosition = new Vector3(1.68f, 0.56f, 0f);
            gun.SetBaseMaxAmmo(200);
            gun.ammo = 200;
            gun.gunClass = GunClass.PISTOL;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage *= 1.4f;
            projectile.baseData.speed *= 1f;
            projectile.baseData.range *= 1f;
            projectile.baseData.force *= 1f;

            projectile.SetProjectileSpriteRight("punishmentray_projectile", 20, 3, false, tk2dBaseSprite.Anchor.MiddleCenter, 10, 3);

            projectile.transform.parent = gun.barrelOffset;
            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("Punishment Ray Lasers", "NevernamedsItems/Resources/CustomGunAmmoTypes/punishmentray_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/punishmentray_clipempty");
            gun.quality = PickupObject.ItemQuality.C;
            ETGMod.Databases.Items.Add(gun, null, "ANY");
        }
        public static int HitStreak = 0;
        protected override void OnPickedUpByPlayer(PlayerController player)
        {
            if (!everPickedUpByPlayer)
            {
                HitStreak = 0;
            }
            base.OnPickedUpByPlayer(player);
        }
        private void OnCollision(CollisionData data)
        {
            if (data.OtherRigidbody != null && data.OtherRigidbody.gameObject != null)
            {
                AIActor actorness = data.OtherRigidbody.gameObject.GetComponent<AIActor>();
                HealthHaver healthness = data.OtherRigidbody.gameObject.GetComponent<HealthHaver>();
                if (actorness != null || healthness != null)
                {
                    if (healthness.IsVulnerable) HitStreak += 1;
                }
                else if (data.OtherRigidbody.name != null && !AcceptableNonEnemyTargets.Contains(data.OtherRigidbody.name))
                {
                    ETGModConsole.Log(data.OtherRigidbody.name);
                    EndStreak(data.MyRigidbody.projectile);
                }
                else if (data.OtherRigidbody.name == null)
                {
                    EndStreak(data.MyRigidbody.projectile);
                }


            }
            else
            {
                EndStreak(data.MyRigidbody.projectile);
            }
            if (data.MyRigidbody.projectile != null)
            {
                PunishmentRayHitOnce hitonce = data.MyRigidbody.projectile.gameObject.AddComponent<PunishmentRayHitOnce>();
            }

        }
        public class PunishmentRayHitOnce : MonoBehaviour
        {
            public PunishmentRayHitOnce()
            {
            }
        }
        public static List<string> AcceptableNonEnemyTargets = new List<string>()
        {
            "Red Barrel",
        };
        private void EndStreak(Projectile projectileResponsible)
        {
            if (projectileResponsible != null)
            {
                PunishmentRayHitOnce baseProj = projectileResponsible.GetComponent<PunishmentRayHitOnce>();
                if (baseProj != null)
                {
                    return;
                }
            }
            if (this.gun.CurrentOwner != null && this.gun.CurrentOwner is PlayerController)
            {
                if (HitStreak > 0)
                {
                    VFXToolbox.DoStringSquirt("STREAK LOST", this.gun.CurrentOwner.transform.position, Color.red);
                }
                int damageThreshold = 10;
                if ((this.gun.CurrentOwner as PlayerController).PlayerHasActiveSynergy("Spare The Rod")) damageThreshold = 20;
                if (HitStreak >= damageThreshold)
                {
                    this.gun.CurrentOwner.healthHaver.ApplyDamage(0.5f, Vector2.zero, "STREAK LOST", CoreDamageTypes.None, DamageCategory.Normal, true, null, false);
                }
            }
            HitStreak = 0;
        }
        public override void PostProcessProjectile(Projectile projectile)
        {
            if (projectile.Owner is PlayerController)
            {
                PlayerController owner = projectile.Owner as PlayerController;
                if (owner.IsInCombat)
                {
                    projectile.specRigidbody.OnCollision += this.OnCollision;
                }
                float amountPerLevel = 0.02f;
                if (owner.PlayerHasActiveSynergy("Cobalt Streak")) amountPerLevel = 0.05f;
                projectile.baseData.damage *= (HitStreak * amountPerLevel) + 1;
            }
            base.PostProcessProjectile(projectile);
        }
        public PunishmentRay()
        {

        }
    }
}