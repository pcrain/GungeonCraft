using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using Alexandria.ItemAPI;
using MonoMod.RuntimeDetour;
using System.Reflection;
using Alexandria.Misc;
using SaveAPI;

namespace NevernamedsItems
{
    public class Creditor : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Creditor", "creditor");
            Game.Items.Rename("outdated_gun_mods:creditor", "nn:creditor");
            gun.gameObject.AddComponent<Creditor>();
            gun.SetShortDescription("Credit To The Team");
            gun.SetLongDescription("Converts the sheer economic potential of the Hegemony Credit into a powerful blast."+"\n\nDraws from the bearer's stored funds.");

            gun.SetupSprite(null, "creditor_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 10);

            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(86) as Gun).gunSwitchGroup;

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 0;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 5f;
            gun.DefaultModule.cooldownTime = 0.1f;
            gun.DefaultModule.angleVariance = 3;
            gun.DefaultModule.numberOfShotsInClip = 5;
            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(89) as Gun).muzzleFlashEffects;
            gun.barrelOffset.transform.localPosition = new Vector3(1.37f, 0.81f, 0f);
            gun.SetBaseMaxAmmo(0);
            gun.gunClass = GunClass.PISTOL;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            projectile.baseData.damage = 35f;
            projectile.baseData.force *= 2f;
            projectile.baseData.speed *= 1f;
            projectile.pierceMinorBreakables = true;
            projectile.hitEffects.alwaysUseMidair = true;
            projectile.hitEffects.overrideMidairDeathVFX = EasyVFXDatabase.GreenLaserCircleVFX;

            projectile.AnimateProjectile(new List<string> {
                "creditorproj_001",
                "creditorproj_002",
                "creditorproj_003",
                "creditorproj_004",
                "creditorproj_005",
                "creditorproj_006",
                "creditorproj_007",
                "creditorproj_008",
                "creditorproj_009",
                "creditorproj_010",
            }, 
            10, //FPS
            true, //Loops
            AnimateBullet.ConstructListOfSameValues<IntVector2>(new IntVector2(15, 11), 10), //Pixel Sizes
            AnimateBullet.ConstructListOfSameValues(true, 10), //Lightened
            AnimateBullet.ConstructListOfSameValues(tk2dBaseSprite.Anchor.MiddleCenter, 10), //Anchors
            AnimateBullet.ConstructListOfSameValues(true, 10), //Anchors Change Colliders
            AnimateBullet.ConstructListOfSameValues(false, 10),
            AnimateBullet.ConstructListOfSameValues<Vector3?>(null, 10), 
            AnimateBullet.ConstructListOfSameValues<IntVector2?>(new IntVector2(13, 7), 10), //Override Colliders
            AnimateBullet.ConstructListOfSameValues<IntVector2?>(null, 10), //Override Collider Offsets
            AnimateBullet.ConstructListOfSameValues<Projectile>(null, 10)); //Override Proj to Copy From


            gun.DefaultModule.projectiles[0] = projectile;

            gun.quality = PickupObject.ItemQuality.A;
            ETGMod.Databases.Items.Add(gun, false, "ANY");
            gun.SetupUnlockOnCustomFlag(CustomDungeonFlags.PURCHASED_CREDITOR, true);

            gun.SetTag("override_cangainammo_check");
            CreditorID = gun.PickupObjectId;
        }
        public static int CreditorID;
        public override bool CollectedAmmoPickup(PlayerController player, Gun self, AmmoPickup pickup)
        {
            LootEngine.SpawnCurrency(player.sprite.WorldCenter, 10, true);
            pickup.ForcePickupWithoutGainingAmmo(player);
            return false;
        }
        public override void PostProcessProjectile(Projectile projectile)
        {
            if (UnityEngine.Random.value <= 0.25)
            {
                projectile.ignoreDamageCaps = true;
            }
            base.PostProcessProjectile(projectile);
        }
        public override void OnPostFired(PlayerController player, Gun gun)
        {
            if (gun.GunPlayerOwner() && !gun.InfiniteAmmo)
            {
                int MetaCost = -1;
                if (player.HasPickupID(TitaniumClip.TitaniumClipID)) MetaCost = -2;
                GameStatsManager.Instance.RegisterStatChange(TrackedStats.META_CURRENCY, MetaCost);
            }
            base.OnPostFired(player, gun);
        }
        protected override void Update()
        {
            if (gun.GunPlayerOwner())
            {
                if (gun.CurrentAmmo != GameStatsManager.Instance.GetPlayerStatValue(TrackedStats.META_CURRENCY))
                {
                    this.RecalculateClip(gun.GunPlayerOwner());
                }
            }
            base.Update();
        }
        private void RecalculateClip(PlayerController gunowner)
        {
            int total = (int)GameStatsManager.Instance.GetPlayerStatValue(TrackedStats.META_CURRENCY);
            gun.CurrentAmmo = total;
            gun.DefaultModule.numberOfShotsInClip = total;
            gun.ClipShotsRemaining = total;
            gunowner.stats.RecalculateStats(gunowner, false, false);
        }
        private void OnKilledEnemy(PlayerController player, HealthHaver enemy)
        {
            if (player.PlayerHasActiveSynergy("Fully Funded"))
            {
                if (player.CurrentGun.PickupObjectId == Creditor.CreditorID && UnityEngine.Random.value <= 0.25 && enemy.GetMaxHealth() >= 15)
                {
                    LootEngine.SpawnCurrency(enemy.specRigidbody.UnitCenter, 1, true);
                }                
            }
        }
        protected override void OnPostDroppedByPlayer(PlayerController player)
        {
            player.OnKilledEnemyContext -= this.OnKilledEnemy;
            base.OnPostDroppedByPlayer(player);
        }
        public override void OnDestroy()
        {
            if (gun && gun.CurrentOwner && gun.GunPlayerOwner())
            {
                gun.GunPlayerOwner().OnKilledEnemyContext -= this.OnKilledEnemy;
            }
            base.OnDestroy();
        }
        protected override void OnPickedUpByPlayer(PlayerController player)
        {
            player.OnKilledEnemyContext += this.OnKilledEnemy;

            if (!everPickedUpByPlayer)
            {
                LootEngine.SpawnCurrency(player.sprite.WorldCenter, 10, true);
            }
            base.OnPickedUpByPlayer(player);
        }
            public Creditor()
        {

        }
    }
}

