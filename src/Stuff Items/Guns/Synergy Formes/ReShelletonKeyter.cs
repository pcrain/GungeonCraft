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

namespace NevernamedsItems
{

    public class ReShelletonKeyter : GunBehaviour
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("ReShelletonKeyter", "rekeytershelletonforme");
            Game.Items.Rename("outdated_gun_mods:reshelletonkeyter", "nn:reshelletonkeyter");
            gun.gameObject.AddComponent<ReShelletonKeyter>();
            gun.SetShortDescription("Click");
            gun.SetLongDescription("Skull lookin ass");

            gun.SetupSprite(null, "rekeytershelletonforme_idle_001", 8);

            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(95) as Gun).gunSwitchGroup;

            gun.SetAnimationFPS(gun.shootAnimation, 15);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Burst;
            gun.DefaultModule.burstShotCount = 3;
            gun.DefaultModule.burstCooldownTime = 0.11f;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.InfiniteAmmo = true;
            gun.DefaultModule.cooldownTime = 0.5f;
            gun.DefaultModule.numberOfShotsInClip = 15;
            gun.barrelOffset.transform.localPosition = new Vector3(1.93f, 0.81f, 0f);
            gun.SetBaseMaxAmmo(200);


            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage *= 1.6f;
            projectile.baseData.range *= 1f;
            projectile.transform.parent = gun.barrelOffset;

            KeyBulletBehaviour unlocking = projectile.gameObject.GetOrAddComponent<KeyBulletBehaviour>();
            unlocking.useSpecialTint = false;
            unlocking.procChance = 0.1f;

            ScaleProjectileStatOffConsumableCount keyDamage = projectile.gameObject.GetOrAddComponent<ScaleProjectileStatOffConsumableCount>();
            keyDamage.multiplierPerLevelOfStat = 0.1f;
            keyDamage.projstat = ScaleProjectileStatOffConsumableCount.ProjectileStatType.DAMAGE;
            keyDamage.consumableType = ScaleProjectileStatOffConsumableCount.ConsumableType.KEYS;

            projectile.SetProjectileSpriteRight("rekeytershelletonforme_projectile", 17, 7, false, tk2dBaseSprite.Anchor.MiddleCenter, 17, 6);

            gun.quality = PickupObject.ItemQuality.EXCLUDED;
            gun.encounterTrackable.EncounterGuid = "this is the ReShelletonKeyter";
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            ReShelletonKeyterID = gun.PickupObjectId;
        }
        public static int ReShelletonKeyterID;
        public ReShelletonKeyter()
        {

        }
    }
}

