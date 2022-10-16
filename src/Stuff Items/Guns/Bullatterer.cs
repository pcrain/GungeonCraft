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

    public class Bullatterer : AdvancedGunBehavior
    {

        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Bullatterer", "bullatterer");
            Game.Items.Rename("outdated_gun_mods:bullatterer", "nn:bullatterer");
            gun.gameObject.AddComponent<Bullatterer>();
            gun.SetShortDescription("Fly, my pretties!");
            gun.SetLongDescription("Releases angry Bullats to fight for you."+"\n\nCreated by an ancient vampire in order to fight the most intimidating monster of all... his own loneliness.");
            gun.SetupSprite(null, "bullatterer_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 13);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);

            gun.muzzleFlashEffects.type = VFXPoolType.None;

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Automatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1.5f;
            gun.DefaultModule.cooldownTime = 0.35f;
            gun.DefaultModule.numberOfShotsInClip = 30;
            gun.barrelOffset.transform.localPosition = new Vector3(1.75f, 1.12f, 0f);
            gun.SetBaseMaxAmmo(400);
            gun.ammo = 400;
            gun.gunClass = GunClass.CHARM;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage *= 0f;
            projectile.sprite.renderer.enabled = false;
            projectile.pierceMinorBreakables = true;

            SpawnEnemyOnBulletSpawn orAddComponent = projectile.gameObject.GetOrAddComponent<SpawnEnemyOnBulletSpawn>();
            orAddComponent.companioniseEnemy = true;
            orAddComponent.deleteProjAfterSpawn = true;
            orAddComponent.enemyBulletDamage = 15f;
            orAddComponent.guidToSpawn = EnemyGuidDatabase.Entries["bullat"];
            orAddComponent.ignoreSpawnedEnemyForGoodMimic = true;
            orAddComponent.killSpawnedEnemyOnRoomClear = true;
            orAddComponent.procChance = 1f;
            orAddComponent.scaleEnemyDamage = true;
            orAddComponent.knockbackAmountAwayFromOwner = 20;
            orAddComponent.scaleEnemyProjSize = true;
            orAddComponent.scaleEnemyProjSpeed = true;
            orAddComponent.doPostProcessOnEnemyBullets = false;


            gun.quality = PickupObject.ItemQuality.C;
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            BullattererID = gun.PickupObjectId;
        }
        public static int BullattererID;
        protected override void Update()
        {
            /*if (this.gun.CurrentOwner != null && this.gun.CurrentOwner is PlayerController && !(this.gun.CurrentOwner as PlayerController).IsInCombat)
            {
                if (!this.gun.RuntimeModuleData[this.gun.DefaultModule].onCooldown)
                {
                    this.gun.RuntimeModuleData[this.gun.DefaultModule].onCooldown = true;
                }
            }*/
            base.Update();
        }
        public Bullatterer()
        {

        }
        public override void PostProcessProjectile(Projectile projectile)
        {
            PlayerController player = projectile.Owner as PlayerController;
            if (player != null && projectile.gameObject.GetComponent<SpawnEnemyOnBulletSpawn>())
            {
                int bullatType = UnityEngine.Random.Range(1, 5);
                if (bullatType == 1 && player.PlayerHasActiveSynergy("Shotgatterer"))
                {
                    SpawnEnemyOnBulletSpawn orAddComponent = projectile.gameObject.GetOrAddComponent<SpawnEnemyOnBulletSpawn>();
                    orAddComponent.companioniseEnemy = true;
                    orAddComponent.deleteProjAfterSpawn = true;
                    orAddComponent.enemyBulletDamage = 30f;
                    orAddComponent.guidToSpawn = EnemyGuidDatabase.Entries["shotgat"];
                    orAddComponent.ignoreSpawnedEnemyForGoodMimic = true;
                    orAddComponent.killSpawnedEnemyOnRoomClear = true;
                    orAddComponent.procChance = 1f;
                    orAddComponent.scaleEnemyDamage = true;
                    orAddComponent.scaleEnemyProjSize = true;
                    orAddComponent.scaleEnemyProjSpeed = true;
                    orAddComponent.doPostProcessOnEnemyBullets = false;
                }
                else if (bullatType == 2 && player.PlayerHasActiveSynergy("Grenatterer"))
                {
                    SpawnEnemyOnBulletSpawn orAddComponent = projectile.gameObject.GetOrAddComponent<SpawnEnemyOnBulletSpawn>();
                    orAddComponent.companioniseEnemy = true;
                    orAddComponent.deleteProjAfterSpawn = true;
                    orAddComponent.enemyBulletDamage = 10f;
                    orAddComponent.guidToSpawn = EnemyGuidDatabase.Entries["grenat"];
                    orAddComponent.ignoreSpawnedEnemyForGoodMimic = true;
                    orAddComponent.killSpawnedEnemyOnRoomClear = true;
                    orAddComponent.procChance = 1f;
                    orAddComponent.scaleEnemyDamage = true;
                    orAddComponent.scaleEnemyProjSize = true;
                    orAddComponent.scaleEnemyProjSpeed = true;
                    orAddComponent.doPostProcessOnEnemyBullets = false;
                }
                else if (bullatType == 3 && player.PlayerHasActiveSynergy("Spiratterer"))
                {
                    SpawnEnemyOnBulletSpawn orAddComponent = projectile.gameObject.GetOrAddComponent<SpawnEnemyOnBulletSpawn>();
                    orAddComponent.companioniseEnemy = true;
                    orAddComponent.deleteProjAfterSpawn = true;
                    orAddComponent.enemyBulletDamage = 10f;
                    orAddComponent.guidToSpawn = EnemyGuidDatabase.Entries["spirat"];
                    orAddComponent.ignoreSpawnedEnemyForGoodMimic = true;
                    orAddComponent.killSpawnedEnemyOnRoomClear = true;
                    orAddComponent.procChance = 1f;
                    orAddComponent.scaleEnemyDamage = true;
                    orAddComponent.scaleEnemyProjSize = true;
                    orAddComponent.scaleEnemyProjSpeed = true;
                    orAddComponent.doPostProcessOnEnemyBullets = false;
                }
            }
            base.PostProcessProjectile(projectile);
        }

    }
}