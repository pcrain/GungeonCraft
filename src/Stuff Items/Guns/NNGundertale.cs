using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using Alexandria.ItemAPI;
using System.Reflection;
using Alexandria.EnemyAPI;

namespace NevernamedsItems
{
    public class NNGundertale : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Gundertale", "gundertale");
            Game.Items.Rename("outdated_gun_mods:gundertale", "nn:gundertale");
            var behav = gun.gameObject.AddComponent<NNGundertale>();
            behav.preventNormalFireAudio = true;
            behav.preventNormalReloadAudio = true;
            gun.SetShortDescription("Filled With Determination");
            gun.SetLongDescription("Doesnt shoot. Befriends your enemies with your masterful dodges." + "\n\nAn antique Revolver. On days like these...");

            gun.SetupSprite(null, "gundertale_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 12);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 0;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.DefaultModule.cooldownTime = 10000000000f;
            gun.muzzleFlashEffects.type = VFXPoolType.None;
            gun.DefaultModule.numberOfShotsInClip = 10;
            gun.barrelOffset.transform.localPosition = new Vector3(2.5f, 0.68f, 0f);
            gun.SetBaseMaxAmmo(50);
            gun.gunClass = GunClass.PISTOL;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage *= 0f;
            projectile.baseData.speed *= 0f;
            projectile.sprite.renderer.enabled = false;


            gun.quality = PickupObject.ItemQuality.A;
            ETGMod.Databases.Items.Add(gun, false, "ANY");

        }

        protected override void Update()
        {
            if (this.gun != null && this.gun.DefaultModule != null && this.gun.RuntimeModuleData[this.gun.DefaultModule] != null)
            {
                if (!this.gun.RuntimeModuleData[this.gun.DefaultModule].onCooldown)
                {
                    this.gun.RuntimeModuleData[this.gun.DefaultModule].onCooldown = true;
                }
            }
            base.Update();
        }
        private bool DetermineCanMakeNPC(AIActor target)
        {
            CustomEnemyTagsSystem tags = target.gameObject.GetComponent<CustomEnemyTagsSystem>();
            if (tags != null && tags.isGundertaleFriendly == true) { return false; }

            if (!target.healthHaver.IsBoss)
            {
                Gun gundertale = this.gun.CurrentOwner.CurrentGun;
                if (gundertale && gundertale == this.gun && gundertale.CurrentAmmo > 0 && gundertale.ClipShotsRemaining > 0)
                {
                    gundertale.CurrentAmmo -= 1;
                    gundertale.ClipShotsRemaining -= 1;
                    return true;
                }
                else { return false; }
            }
            else
            {
                return false;
            }
        }
        private void onDodgeRolledOverBullet(Projectile bullet)
        {
            if (this.gun.CurrentOwner.CurrentGun == this.gun && bullet.Owner && bullet.Owner is AIActor)
            {

                bool canNPCify = DetermineCanMakeNPC(bullet.Owner as AIActor);
                if (canNPCify) { MakeEnemyNPC(bullet.Owner.aiActor); }
            }
        }
        private void onDodgeRolledIntoEnemy(PlayerController player, AIActor enemy)
        {
            if (this.gun.CurrentOwner.CurrentGun == this.gun && enemy && enemy is AIActor)
            {
                bool canNPCify = DetermineCanMakeNPC(enemy);
                if (canNPCify)
                {
                    enemy.healthHaver.flashesOnDamage = false;
                    enemy.healthHaver.RegenerateCache();
                    MakeEnemyNPC(enemy.aiActor);
                }
            }
        }
        private void HandleSpawnLoot(AIActor enemy)
        {
            var type = typeof(AIActor);
            var func = type.GetMethod("HandleRewards", BindingFlags.Instance | BindingFlags.NonPublic);
            var ret = func.Invoke(enemy, null);
        }
        private void MakeEnemyNPC(AIActor enemy)
        {
            HandleSpawnLoot(enemy);
            var CurrentRoom = enemy.transform.position.GetAbsoluteRoom();
            UnityEngine.Object.Instantiate<GameObject>(EasyVFXDatabase.GundetaleSpareVFX, (enemy.sprite.WorldTopCenter + new Vector2(0, 0.25f)), Quaternion.identity);
            if (enemy.GetComponent<KillOnRoomUnseal>())
            {
                UnityEngine.Object.Destroy(enemy.GetComponent<KillOnRoomUnseal>());
            }
            CurrentRoom.DeregisterEnemy(enemy);
            CustomEnemyTagsSystem tags = enemy.gameObject.GetOrAddComponent<CustomEnemyTagsSystem>();
            enemy.gameActor.DeleteOwnedBullets();
            if (tags != null)
            {
                tags.isGundertaleFriendly = true;
            }
            if (enemy.specRigidbody)
            {
                UnityEngine.Object.Destroy(enemy.specRigidbody);
            }
            if (enemy.behaviorSpeculator)
            {
                UnityEngine.Object.Destroy(enemy.behaviorSpeculator);
            }
            if (enemy.healthHaver)
            {
                enemy.healthHaver.IsVulnerable = false;
                enemy.healthHaver.bossHealthBar = HealthHaver.BossBarType.None;
                enemy.healthHaver.EndBossState(false);
            }
            if (enemy.aiAnimator)
            {
                enemy.aiAnimator.PlayUntilCancelled("idle", false, null, -1f, false);
            }
            //GameUIBossHealthController bossUI = enemy.GetComponent<GameUIBossHealthController>()
        }
        protected override void OnPickedUpByPlayer(PlayerController player)
        {
            player.OnDodgedProjectile += this.onDodgeRolledOverBullet;
            player.OnRolledIntoEnemy += this.onDodgeRolledIntoEnemy;
            base.OnPickedUpByPlayer(player);
        }
        protected override void OnPostDroppedByPlayer(PlayerController player)
        {
            player.OnDodgedProjectile -= this.onDodgeRolledOverBullet;
            player.OnRolledIntoEnemy -= this.onDodgeRolledIntoEnemy;
            base.OnPostDroppedByPlayer(player);
        }
        public override void OnDestroy()
        {
            if (gun.GunPlayerOwner())
            {
                gun.GunPlayerOwner().OnDodgedProjectile -= this.onDodgeRolledOverBullet;
                gun.GunPlayerOwner().OnRolledIntoEnemy -= this.onDodgeRolledIntoEnemy;
            }
            base.OnDestroy();
        }
        public static List<int> lootIDlist = new List<int>()
        {
            78, //Ammo
            600, //Spread Ammo
            565, //Glass Guon Stone
            73, //Half Heart
            85, //Heart
            120, //Armor
        };
    }
}