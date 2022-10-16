using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Reflection;
using Gungeon;
using MonoMod;
using UnityEngine;
using ItemAPI;
using SaveAPI;

namespace NevernamedsItems
{
    public class Blizzkrieg : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Blizzkrieg", "blizzkrieg");
            Game.Items.Rename("outdated_gun_mods:blizzkrieg", "nn:blizzkrieg");
            var behav = gun.gameObject.AddComponent<Blizzkrieg>();

            gun.SetShortDescription("Kalt Action");
            gun.SetLongDescription("Fires chunks of hypercold H2O."+"\n\nSecret Blobulonian technology, developed during their ill-fated winter campaign.");

            gun.SetupSprite(null, "blizzkrieg_idle_001", 8);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(38) as Gun).gunSwitchGroup;


            gun.SetAnimationFPS(gun.shootAnimation, 12);

            for (int i = 0; i < 2; i++)
            {
                gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(402) as Gun, true, false);
            }

            foreach (ProjectileModule mod in gun.Volley.projectiles)
            {
                Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(mod.projectiles[0]);
                projectile.gameObject.SetActive(false);
                FakePrefab.MarkAsFakePrefab(projectile.gameObject);
                UnityEngine.Object.DontDestroyOnLoad(projectile);
                mod.projectiles[0] = (projectile);

                Projectile projectile2 = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
                projectile2.gameObject.SetActive(false);
                FakePrefab.MarkAsFakePrefab(projectile2.gameObject);
                UnityEngine.Object.DontDestroyOnLoad(projectile2);
                projectile2.baseData.damage *= 1.6f;
                projectile2.baseData.speed *= 1f;
                projectile2.damageTypes |= CoreDamageTypes.Ice;

                ScaleProjectileStatOffPlayerStat coolnessScaling = projectile2.gameObject.AddComponent<ScaleProjectileStatOffPlayerStat>();
                coolnessScaling.multiplierPerLevelOfStat = 0.2f;
                coolnessScaling.projstat = ScaleProjectileStatOffPlayerStat.ProjectileStatType.DAMAGE;
                coolnessScaling.playerstat = PlayerStats.StatType.Coolness;

                SimpleFreezingBulletBehaviour freezing = projectile2.gameObject.AddComponent<SimpleFreezingBulletBehaviour>();
                freezing.freezeAmount = 40;
                freezing.useSpecialTint = false;
                freezing.freezeAmountForBosses = 40;

                GoopModifier watering = projectile2.gameObject.AddComponent<GoopModifier>();
                watering.CollisionSpawnRadius = 0.8f;
                watering.SpawnGoopOnCollision = true;
                watering.SpawnGoopInFlight = false;
                watering.goopDefinition = EasyGoopDefinitions.WaterGoop;
                projectile2.SetProjectileSpriteRight("icicle_projectile", 13, 5, false, tk2dBaseSprite.Anchor.MiddleCenter, 13, 5);
                mod.projectiles.Add(projectile2);


                mod.ammoCost = 1;
                mod.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
                mod.shootStyle = ProjectileModule.ShootStyle.Automatic;
                mod.cooldownTime = 0.1f;
                mod.angleVariance = 34f;
                mod.numberOfShotsInClip = 30;
                if (mod != gun.DefaultModule) mod.ammoCost = 0;
            }
            gun.reloadTime = 1.5f;
            gun.barrelOffset.transform.localPosition = new Vector3(2.56f, 0.81f, 0f);
            gun.SetBaseMaxAmmo(400);
            gun.gunClass = GunClass.ICE;
            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("Blizzkrieg Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/blizzkreig_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/blizzkreig_clipempty");


            gun.quality = PickupObject.ItemQuality.S;
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            BlizzkriegID = gun.PickupObjectId;
            gun.SetupUnlockOnCustomFlag(CustomDungeonFlags.CHALLENGE_KEEPITCOOL_BEATEN, true);
        }
        public static int BlizzkriegID;       
        public Blizzkrieg()
        {

        }
    }
}

