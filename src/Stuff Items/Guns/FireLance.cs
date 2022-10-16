using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using Alexandria.ItemAPI;
using Alexandria.Misc;

namespace NevernamedsItems
{

    public class FireLance : GunBehaviour
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Fire Lance", "firelance");
            Game.Items.Rename("outdated_gun_mods:fire_lance", "nn:fire_lance");
            gun.gameObject.AddComponent<FireLance>();
            gun.SetShortDescription("Where it all started");
            gun.SetLongDescription("Long explosive lances such as these are recorded to be some of the first gunpowder-based weapons in human history, beaten out only by crude bombs."+"\n\nWithout this, none of this would be possible.");

            gun.SetupSprite(null, "firelance_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 15);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.DefaultModule.cooldownTime = 0.5f;
            gun.muzzleFlashEffects.type = VFXPoolType.None;
            gun.DefaultModule.numberOfShotsInClip = 5;
            gun.barrelOffset.transform.localPosition = new Vector3(4.56f, 1.18f, 0f);
            gun.SetBaseMaxAmmo(100);
            gun.gunClass = GunClass.EXPLOSIVE;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage *= 3f;
            projectile.sprite.renderer.enabled = false;
            projectile.baseData.range /= 35f;
            projectile.transform.parent = gun.barrelOffset;
            ExplosiveModifier explosiveModifier = projectile.gameObject.AddComponent<ExplosiveModifier>();
            explosiveModifier.doExplosion = true;
            explosiveModifier.explosionData = FireLanceExplosion;

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("Fire Lance Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/firelance_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/firelance_clipempty");

            gun.quality = PickupObject.ItemQuality.D;
            ETGMod.Databases.Items.Add(gun, false, "ANY");
            FireLanceID = gun.PickupObjectId;
        }
        public static int FireLanceID;
        static ExplosionData smallExplosion = GameManager.Instance.Dungeon.sharedSettingsPrefab.DefaultSmallExplosionData;
        static ExplosionData FireLanceExplosion = new ExplosionData()
        {
            effect = smallExplosion.effect,
            ignoreList = smallExplosion.ignoreList,
            ss = smallExplosion.ss,
            damageRadius = 2.5f,
            damageToPlayer = 0f,
            doDamage = true,
            damage = 25,
            doDestroyProjectiles = true,
            doForce = true,
            debrisForce = 30f,
            preventPlayerForce = true,
            explosionDelay = 0.1f,
            usesComprehensiveDelay = false,
            doScreenShake = true,
            playDefaultSFX = true,
        };
        public override void PostProcessProjectile(Projectile projectile)
        {
            if (projectile && projectile.ProjectilePlayerOwner() && projectile.ProjectilePlayerOwner().PlayerHasActiveSynergy("There are some who call me..."))
            {
                projectile.baseData.range = 1000;
                projectile.baseData.speed *= 20;
            }
            base.PostProcessProjectile(projectile);
        }
        public override void OnPostFired(PlayerController player, Gun gun)
        {
            gun.PreventNormalFireAudio = true;
        }
        public FireLance()
        {

        }
    }
}