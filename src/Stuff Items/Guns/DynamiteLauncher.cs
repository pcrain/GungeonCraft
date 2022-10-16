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
    public class DynamiteLauncher : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Dynamite Launcher", "dynamitelauncher");
            Game.Items.Rename("outdated_gun_mods:dynamite_launcher", "nn:dynamite_launcher");
            gun.gameObject.AddComponent<DynamiteLauncher>();
            gun.SetShortDescription("Dynamight makes Dynaright");
            gun.SetLongDescription("Fires a potent stick of nitroglycerin."+"\n\nCreated by mad Nitra Gunsmith, before he realised that he didn't have hands.");

            gun.SetupSprite(null, "dynamitelauncher_idle_001", 8);

            gun.outOfAmmoAnimation = "empty";

            gun.SetAnimationFPS(gun.shootAnimation, 12);
            gun.SetAnimationFPS(gun.reloadAnimation, 1);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(150) as Gun).gunSwitchGroup;

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 2f;
            gun.DefaultModule.cooldownTime = 1f;
            gun.DefaultModule.numberOfShotsInClip = 1;
            gun.barrelOffset.transform.localPosition = new Vector3(1.56f, 0.62f, 0f);
            gun.SetBaseMaxAmmo(40);
            gun.gunClass = GunClass.EXPLOSIVE;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage *= 6f;
            projectile.baseData.speed *= 0.4f;
            projectile.baseData.range *= 1f;
            BounceProjModifier Bouncing = projectile.gameObject.GetOrAddComponent<BounceProjModifier>();
            Bouncing.numberOfBounces = 1;
            ExplosiveModifier exploding = projectile.gameObject.GetOrAddComponent<ExplosiveModifier>();
            exploding.doExplosion = true;
            exploding.explosionData = StaticExplosionDatas.customDynamiteExplosion;

            projectile.pierceMinorBreakables = true;


            projectile.AnimateProjectile(new List<string> {
                "dynamiteproj_1",
                "dynamiteproj_2",
                "dynamiteproj_3",
                "dynamiteproj_4",
            }, 8, true, new List<IntVector2> {
                new IntVector2(14, 15), //1
                new IntVector2(15, 14), //2            
                new IntVector2(14,15), //3
                new IntVector2(15, 14), //4
            }, AnimateBullet.ConstructListOfSameValues(false, 4), AnimateBullet.ConstructListOfSameValues(tk2dBaseSprite.Anchor.MiddleCenter, 4), AnimateBullet.ConstructListOfSameValues(true, 4), AnimateBullet.ConstructListOfSameValues(false, 4),
                        AnimateBullet.ConstructListOfSameValues<Vector3?>(null, 4), AnimateBullet.ConstructListOfSameValues<IntVector2?>(null, 4), AnimateBullet.ConstructListOfSameValues<IntVector2?>(null, 4), AnimateBullet.ConstructListOfSameValues<Projectile>(null, 4));

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("Dynamite Launcher Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/dynamitelauncher_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/dynamitelauncher_clipempty");

            gun.quality = PickupObject.ItemQuality.A;
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            DynamiteLauncherID = gun.PickupObjectId;
            gun.AddToSubShop(ItemBuilder.ShopType.Trorc);

            gun.SetupUnlockOnCustomFlag(CustomDungeonFlags.NITRA_QUEST_REWARDED, true);

        }
        public static int DynamiteLauncherID;
        public DynamiteLauncher()
        {

        }
        public override void PostProcessProjectile(Projectile projectile)
        {
            PlayerController player = projectile.Owner as PlayerController;
            if (player != null && player.PlayerHasActiveSynergy("Nobel Effort"))
            {
                SpawnEnemyOnBulletSpawn nitraSpawning = projectile.gameObject.GetOrAddComponent<SpawnEnemyOnBulletSpawn>();
                nitraSpawning.companioniseEnemy = true;
                nitraSpawning.deleteProjAfterSpawn = false;
                nitraSpawning.enemyBulletDamage = 30f;
                nitraSpawning.guidToSpawn = EnemyGuidDatabase.Entries["dynamite_kin"];
                nitraSpawning.ignoreSpawnedEnemyForGoodMimic = true;
                nitraSpawning.killSpawnedEnemyOnRoomClear = true;
                nitraSpawning.procChance = 1f;
                nitraSpawning.scaleEnemyDamage = true;
                nitraSpawning.scaleEnemyProjSize = true;
                nitraSpawning.scaleEnemyProjSpeed = true;
                nitraSpawning.doPostProcessOnEnemyBullets = false;
                base.PostProcessProjectile(projectile);
            }
        }
    }
}
