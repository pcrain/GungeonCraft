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

    public class Icicle : GunBehaviour
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Icicle", "icicle");
            Game.Items.Rename("outdated_gun_mods:icicle", "nn:icicle");
            gun.gameObject.AddComponent<Icicle>();
            gun.SetShortDescription("Begins Anew");
            gun.SetLongDescription("Becomes more powerful the cooler it's owner is." + "\n\nSnapped off of the ceiling of the Hollow's deepest catacomb, and somehow hasn't thawed ever since.");

            gun.SetupSprite(null, "icicle_idle_001", 8);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(199) as Gun).gunSwitchGroup;

            gun.SetAnimationFPS(gun.shootAnimation, 14);
            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(97) as Gun).muzzleFlashEffects;
            gun.AddPassiveStatModifier(PlayerStats.StatType.Coolness, 1, StatModifier.ModifyMethod.ADDITIVE);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.DefaultModule.cooldownTime = 0.5f;
            gun.DefaultModule.numberOfShotsInClip = 10;
            gun.barrelOffset.transform.localPosition = new Vector3(1.62f, 0.56f, 0f);
            gun.SetBaseMaxAmmo(220);
            gun.ammo = 220;
            gun.gunClass = GunClass.ICE;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            projectile.baseData.damage *= 1.6f;
            projectile.baseData.speed *= 1f;
            projectile.damageTypes |= CoreDamageTypes.Ice;

            ScaleProjectileStatOffPlayerStat coolnessScaling = projectile.gameObject.AddComponent<ScaleProjectileStatOffPlayerStat>();
            coolnessScaling.multiplierPerLevelOfStat = 0.2f;
            coolnessScaling.projstat = ScaleProjectileStatOffPlayerStat.ProjectileStatType.DAMAGE;
            coolnessScaling.playerstat = PlayerStats.StatType.Coolness;

            SimpleFreezingBulletBehaviour freezing = projectile.gameObject.AddComponent<SimpleFreezingBulletBehaviour>();
            freezing.freezeAmount = 40;
            freezing.useSpecialTint = false;
            freezing.freezeAmountForBosses = 40;

            GoopModifier watering = projectile.gameObject.AddComponent<GoopModifier>();
            watering.CollisionSpawnRadius = 0.8f;
            watering.SpawnGoopOnCollision = true;
            watering.SpawnGoopInFlight = false;
            watering.goopDefinition = EasyGoopDefinitions.WaterGoop;

            projectile.SetProjectileSpriteRight("icicle_projectile", 13, 5, false, tk2dBaseSprite.Anchor.MiddleCenter, 13, 5);
            gun.DefaultModule.projectiles[0] = projectile;

            gun.quality = PickupObject.ItemQuality.B;
            ETGMod.Databases.Items.Add(gun, null, "ANY");
            IcicleID = gun.PickupObjectId;

        }
        public static int IcicleID;
        public override void PostProcessProjectile(Projectile projectile)
        {
            if (projectile.Owner is PlayerController)
            {
                PlayerController player = projectile.Owner as PlayerController;
                if (player.PlayerHasActiveSynergy("You Need To Chill"))
                {
                    SimpleFreezingBulletBehaviour freezing = projectile.gameObject.GetComponent<SimpleFreezingBulletBehaviour>();
                    if (freezing != null)
                    {
                        freezing.freezeAmount *= 2;
                    }
                }
            }
        }
        public Icicle()
        {

        }
    }
}

