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

    public class JusticeGun : GunBehaviour
    {


        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Justice", "justice");
            Game.Items.Rename("outdated_gun_mods:justice", "nn:justice");
            gun.gameObject.AddComponent<JusticeGun>();
            gun.SetShortDescription("Served");
            gun.SetLongDescription("Bello's trusty shotgun, custom made for his big, burly hands.");

            gun.SetupSprite(null, "justice_idle_001", 8);
            //ItemBuilder.AddPassiveStatModifier(gun, PlayerStats.StatType.GlobalPriceMultiplier, 0.925f, StatModifier.ModifyMethod.MULTIPLICATIVE);

            gun.SetAnimationFPS(gun.shootAnimation, 8);
            gun.SetAnimationFPS(gun.reloadAnimation, 8);
            gun.AddPassiveStatModifier(PlayerStats.StatType.GlobalPriceMultiplier, 0.8f, StatModifier.ModifyMethod.MULTIPLICATIVE);

            for (int i = 0; i < 6; i++)
            {
                gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);
            }

            //GUN STATS
            foreach (ProjectileModule mod in gun.Volley.projectiles)
            {
                mod.ammoCost = 1;
                mod.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
                mod.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
                mod.cooldownTime = 0.6f;
                mod.angleVariance = 45f;
                mod.numberOfShotsInClip = 5;
                Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(mod.projectiles[0]);
                mod.projectiles[0] = projectile;
                projectile.gameObject.SetActive(false);
                FakePrefab.MarkAsFakePrefab(projectile.gameObject);
                UnityEngine.Object.DontDestroyOnLoad(projectile);
                projectile.baseData.damage *= 3f;
                projectile.baseData.speed *= 1.2f;
                projectile.pierceMinorBreakables = true;
                JusticeBurstHandler BurstPop = projectile.gameObject.AddComponent<JusticeBurstHandler>();

                projectile.SetProjectileSpriteRight("justice_projectile", 12, 12, true, tk2dBaseSprite.Anchor.MiddleCenter, 10, 10);
                if (mod != gun.DefaultModule) { mod.ammoCost = 0; }
                projectile.transform.parent = gun.barrelOffset;
            }

            gun.reloadTime = 2f;
            gun.muzzleFlashEffects.type = VFXPoolType.None;
            gun.barrelOffset.transform.localPosition = new Vector3(3.12f, 0.93f, 0f);
            gun.SetBaseMaxAmmo(80);
            gun.gunClass = GunClass.SHOTGUN;
            //BULLET STATS
            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("Justice Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/justice_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/justice_clipempty");

            // Here we just set the quality of the gun and the "EncounterGuid", which is used by Gungeon to identify the gun.
            gun.quality = PickupObject.ItemQuality.A;
            gun.encounterTrackable.EncounterGuid = "this is Justice";
            ETGMod.Databases.Items.Add(gun, null, "ANY");
            JusticeID = gun.PickupObjectId;
            gun.SetupUnlockOnCustomFlag(CustomDungeonFlags.ANGERED_BELLO, true);
        }
        public static int JusticeID;
        public override void Update()
        {
            if (gun && gun.CurrentOwner && gun.CurrentOwner is PlayerController)
            {
                PlayerController player = gun.CurrentOwner as PlayerController;
                if (player.PlayerHasActiveSynergy("Shotkeeper"))
                {
                    if (gun.DefaultModule.numberOfShotsInClip == 5)
                    {
                        gun.SetBaseMaxAmmo(600);
                        foreach (ProjectileModule mod in gun.Volley.projectiles)
                        {
                            mod.shootStyle = ProjectileModule.ShootStyle.Burst;
                            mod.burstShotCount = 3;
                            mod.burstCooldownTime = 0.2f;
                            mod.numberOfShotsInClip = 12;
                            //hasShotkeeperSynergyAlready = true;
                        }
                    }
                }
                else
                {
                    if (gun.DefaultModule.numberOfShotsInClip == 12)
                    {
                        gun.SetBaseMaxAmmo(200);
                        foreach (ProjectileModule mod in gun.Volley.projectiles)
                        {
                            mod.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
                            mod.numberOfShotsInClip = 5;
                            //hasShotkeeperSynergyAlready = false;
                        }
                    }
                }
            }
        }
        //private bool hasShotkeeperSynergyAlready = false;
        public override void OnPostFired(PlayerController player, Gun gun)
        {
            gun.PreventNormalFireAudio = true;
            AkSoundEngine.PostEvent("Play_WPN_shotgun_shot_01", gameObject);
        }
        public JusticeGun()
        {

        }
    }
    public class JusticeBurstHandler : MonoBehaviour
    {
        public JusticeBurstHandler()
        {
            this.projectileToSpawn = null;
        }

        // Token: 0x06007294 RID: 29332 RVA: 0x002CA328 File Offset: 0x002C8528
        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            this.speculativeRigidBoy = base.GetComponent<SpeculativeRigidbody>();
            if (UnityEngine.Random.value <= 0.45f)
            {
                this.m_projectile.OnDestruction += this.DoBurstProjectiles;
            }
        }

        private float spawnAngle = 90f;
        // Token: 0x06007295 RID: 29333 RVA: 0x002CA3A0 File Offset: 0x002C85A0
        private void DoBurstProjectiles(Projectile bullet)
        {
            Projectile proj = ((Gun)PickupObjectDatabase.GetById(378)).DefaultModule.projectiles[0];
            for (int i = 0; i < 8; i++)
            {
                this.SpawnProjectile(proj, m_projectile.sprite.WorldCenter, m_projectile.transform.eulerAngles.z + spawnAngle, null);
                spawnAngle += 45;
            }
        }
        private void SpawnProjectile(Projectile proj, Vector3 spawnPosition, float zRotation, SpeculativeRigidbody collidedRigidbody = null)
        {
            GameObject gameObject = SpawnManager.SpawnProjectile(proj.gameObject, spawnPosition, Quaternion.Euler(0f, 0f, zRotation), true);
            Projectile component = gameObject.GetComponent<Projectile>();
            if (component)
            {
                component.SpawnedFromOtherPlayerProjectile = true;
                PlayerController playerController = this.m_projectile.Owner as PlayerController;
                component.Owner = this.m_projectile.Owner;
                component.baseData.damage *= 1.2f;
                //component.baseData.damage *= playerController.stats.GetStatValue(PlayerStats.StatType.Damage);
                //component.baseData.speed *= playerController.stats.GetStatValue(PlayerStats.StatType.ProjectileSpeed);
                //playerController.DoPostProcessProjectile(component);
            }
        }

        private Projectile m_projectile;
        private SpeculativeRigidbody speculativeRigidBoy;
        public Projectile projectileToSpawn;
    }
}