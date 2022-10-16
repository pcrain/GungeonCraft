using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using Alexandria.ItemAPI;
using SaveAPI;
using Alexandria.Misc;

namespace NevernamedsItems
{

    public class Spiral : AdvancedGunBehavior
    {


        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Spiral", "spiral");
            Game.Items.Rename("outdated_gun_mods:spiral", "nn:spiral");
            gun.gameObject.AddComponent<Spiral>();
            gun.SetShortDescription("Come Join Me...");
            gun.SetLongDescription("Forever. Inescapable. Beautiful."+"\n\nAll will become a part of the Spiral.");

            gun.SetupSprite(null, "spiral_idle_001", 16);

            gun.SetAnimationFPS(gun.shootAnimation, 30);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 2f;
            gun.DefaultModule.cooldownTime = 1f;
            gun.DefaultModule.numberOfShotsInClip = 1;
            gun.barrelOffset.transform.localPosition = new Vector3(1.12f, 0.62f, 0f);
            gun.SetBaseMaxAmmo(150);
            gun.gunClass = GunClass.SILLY;
            //SUBBULLET
            Projectile swirly = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            swirly.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(swirly.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(swirly);
            swirlyProj = swirly;

            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage *= 5f;
            projectile.baseData.speed *= 0.1f;
            projectile.pierceMinorBreakables = true;
            SpiralHandler spiralBehaviour = projectile.gameObject.AddComponent<SpiralHandler>();
            spiralBehaviour.projectileToSpawn = swirlyProj;
            projectile.SetProjectileSpriteRight("spiral_projectile", 15, 15, true, tk2dBaseSprite.Anchor.MiddleCenter, 14, 14);

            projectile.transform.parent = gun.barrelOffset;

            gun.quality = PickupObject.ItemQuality.A; 
            ETGMod.Databases.Items.Add(gun, false, "ANY");

            gun.AddToSubShop(ItemBuilder.ShopType.Cursula);
            SpiralID = gun.PickupObjectId;
            gun.SetupUnlockOnCustomFlag(CustomDungeonFlags.PURCHASED_SPIRAL, true);
        }
        public static int SpiralID;
        public static Projectile swirlyProj;
        public Spiral()
        {

        }
    }
    public class SpiralHandler : MonoBehaviour
    {
        public SpiralHandler()
        {
            this.projectileToSpawn = null;
        }
        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            this.speculativeRigidBoy = base.GetComponent<SpeculativeRigidbody>();
        }

        private float spawnAngle = 90f;
        private void Update()
        {
            if (this.m_projectile == null)
            {
                if (base.GetComponent<Projectile>() != null) this.m_projectile = base.GetComponent<Projectile>();
                else return;
            }
            if (this.speculativeRigidBoy == null)
            {
                if (base.GetComponent<SpeculativeRigidbody>() != null) this.speculativeRigidBoy = base.GetComponent<SpeculativeRigidbody>();
                else return;
            }
            this.elapsed += BraveTime.DeltaTime;
            if (this.elapsed > 0.02f)
            {
                if (projectileToSpawn == null) { Debug.LogError("SpiralHandler tried to spawn a bullet that doesn't exist."); return; }
                this.SpawnProjectile(this.projectileToSpawn, m_projectile.sprite.WorldCenter, spawnAngle, null);
                spawnAngle += 10;
                this.elapsed = 0;
            }
        }
        private void SpawnProjectile(Projectile proj, Vector3 spawnPosition, float zRotation, SpeculativeRigidbody collidedRigidbody = null)
        {
            GameObject gameObject = SpawnManager.SpawnProjectile(proj.gameObject, spawnPosition, Quaternion.Euler(0f, 0f, zRotation), true);
            Projectile component = gameObject.GetComponent<Projectile>();
            if (component && m_projectile.ProjectilePlayerOwner())
            {
                PlayerController playerController = m_projectile.ProjectilePlayerOwner();
                component.Owner = playerController;
                component.Shooter = playerController.specRigidbody;
                component.SpawnedFromOtherPlayerProjectile = true;
                component.TreatedAsNonProjectileForChallenge = true;
                component.baseData.damage *= playerController.stats.GetStatValue(PlayerStats.StatType.Damage);
                component.baseData.force *= playerController.stats.GetStatValue(PlayerStats.StatType.KnockbackMultiplier);
                component.baseData.speed *= playerController.stats.GetStatValue(PlayerStats.StatType.ProjectileSpeed);
                playerController.DoPostProcessProjectile(component);
            }
        }

        private Projectile m_projectile;
        private SpeculativeRigidbody speculativeRigidBoy;
        public Projectile projectileToSpawn;
        private float elapsed;
    }
}