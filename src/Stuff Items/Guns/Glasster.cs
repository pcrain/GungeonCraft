using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using ItemAPI;
using Alexandria.Misc;

namespace NevernamedsItems
{
    public class Glasster : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Glasster", "glasster");
            Game.Items.Rename("outdated_gun_mods:glasster", "nn:glasster");
            gun.gameObject.AddComponent<Glasster>();
            gun.SetShortDescription("Glass Blower");
            gun.SetLongDescription("Increases in damage the more Glass Guon Stones are held." + "\n\nGifted unto a spacefaring rogue by the Lady of Pane, it's original bearer died from infection after cutting himself on a shard of glass.");

            gun.SetupSprite(null, "glasster_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 14);

            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(32) as Gun).gunSwitchGroup;
            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.muzzleFlashEffects.type = VFXPoolType.None;
            gun.DefaultModule.cooldownTime = 0.15f;
            gun.DefaultModule.numberOfShotsInClip = 6;
            gun.barrelOffset.transform.localPosition = new Vector3(1.68f, 0.56f, 0f);
            gun.SetBaseMaxAmmo(200);
            gun.ammo = 200;
            gun.gunClass = GunClass.PISTOL;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            projectile.baseData.damage *= 1f;
            projectile.SetProjectileSpriteRight("glasster_projectile", 4, 4, true, tk2dBaseSprite.Anchor.MiddleCenter, 4, 4);
            gun.DefaultModule.projectiles[0] = projectile;

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("Glasster Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/glasster_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/glasster_clipempty");

            gun.quality = PickupObject.ItemQuality.C;
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            GlassterID = gun.PickupObjectId;
        }
        public static int GlassterID;
        public override void OnPostFired(PlayerController player, Gun gun)
        {
            if (player.PlayerHasActiveSynergy("Masterglass"))
            {
                foreach (var orbital in player.orbitals)
                {
                    var o = (PlayerOrbital)orbital;
                    if (o.name == "IounStone_Glass(Clone)")
                    {
                        Vector2 orbitalPosition = orbital.GetTransform().position;
                        Vector2 direction = player.unadjustedAimPoint.XY();
                        if (player.PlayerHasActiveSynergy("Shattershot"))
                        {
                            FireBullet(player, orbitalPosition, direction, 0, 10);
                            FireBullet(player, orbitalPosition, direction, 14, 10);
                            FireBullet(player, orbitalPosition, direction, -14, 10);
                        }
                        else
                        {
                            FireBullet(player, orbitalPosition, direction, 0, 5);
                        }
                    }
                }
            }
            base.OnPostFired(player, gun);
        }
        private void FireBullet(PlayerController Owner, Vector2 startPos, Vector2 targetPos, float angleOffset, float anglevariance)
        {
            GameObject gameObject = ProjSpawnHelper.SpawnProjectileTowardsPoint(GlassShard.GlassShardProjectile.gameObject, startPos, targetPos, angleOffset, anglevariance);
            Projectile component = gameObject.GetComponent<Projectile>();
            if (component != null)
            {
                component.Owner = Owner;
                component.Shooter = Owner.specRigidbody;
                component.TreatedAsNonProjectileForChallenge = true;
                component.baseData.damage *= Owner.stats.GetStatValue(PlayerStats.StatType.Damage);
                component.baseData.speed *= Owner.stats.GetStatValue(PlayerStats.StatType.ProjectileSpeed);
                component.baseData.force *= Owner.stats.GetStatValue(PlayerStats.StatType.KnockbackMultiplier);
                component.AdditionalScaleMultiplier *= Owner.stats.GetStatValue(PlayerStats.StatType.PlayerBulletScale);
                component.UpdateSpeed();
                Owner.DoPostProcessProjectile(component);
            }

        }
        public override void PostProcessProjectile(Projectile projectile)
        {
            if (projectile.ProjectilePlayerOwner())
            {
                float DamageMultiplier = 1;
                foreach (PassiveItem item in projectile.ProjectilePlayerOwner().passiveItems)
                {
                    if (item.PickupObjectId == 565)
                    {
                        DamageMultiplier += 0.35f;
                    }
                }
                projectile.baseData.damage *= DamageMultiplier;
                if (projectile.ProjectilePlayerOwner().PlayerHasActiveSynergy("No Pane, No Gain"))
                {
                    projectile.baseData.damage *= 2;
                }
            }
            base.PostProcessProjectile(projectile);
        }
        private void OnHit(PlayerController player)
        {
            if (player == gun.CurrentOwner)
            {
                if (player.CurrentGun.PickupObjectId == GlassterID &&  player.PlayerHasActiveSynergy("No Pane, No Gain"))
                {
                    gun.CurrentAmmo = 0;
                }
            }
        }
        protected override void OnPickedUpByPlayer(PlayerController player)
        {
            if (!everPickedUpByPlayer)
            {
                player.AcquirePassiveItemPrefabDirectly(PickupObjectDatabase.GetById(565) as PassiveItem);
            }
            player.OnReceivedDamage += this.OnHit;
            base.OnPickedUpByPlayer(player);
        }
        protected override void OnPostDroppedByPlayer(PlayerController player)
        {
            player.OnReceivedDamage -= this.OnHit;

            base.OnPostDroppedByPlayer(player);
        }
        public override void OnDestroy()
        {
            if (gun.CurrentOwner != null && gun.CurrentOwner is PlayerController)
            {
                (gun.CurrentOwner as PlayerController).OnReceivedDamage -= this.OnHit;
            }
            base.OnDestroy();
        }
        public Glasster()
        {

        }
    }
}