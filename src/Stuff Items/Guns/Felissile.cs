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

    public class Felissile : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Felissile", "felissile");
            Game.Items.Rename("outdated_gun_mods:felissile", "nn:felissile");
            gun.gameObject.AddComponent<Felissile>();
            gun.SetShortDescription("What's New?");
            gun.SetLongDescription("Fires a rocket on the first shot of it's clip."+"\n\nOnce part of a peculiar XXXS Size mech suit.");

            gun.SetupSprite(null, "felissile_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 15);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(39) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 2.5f;
            gun.DefaultModule.cooldownTime = 0.25f;
            gun.DefaultModule.numberOfShotsInClip = 10;
            gun.barrelOffset.transform.localPosition = new Vector3(1.12f, 0.37f, 0f);
            gun.SetBaseMaxAmmo(500);

            gun.DefaultModule.usesOptionalFinalProjectile = true;
            gun.DefaultModule.numberOfFinalProjectiles = 9;
            gun.gunClass = GunClass.EXPLOSIVE;
            //BULLET STATS
            Projectile missileProjectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            missileProjectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(missileProjectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(missileProjectile);
            gun.DefaultModule.projectiles[0] = missileProjectile;
            missileProjectile.baseData.damage = 15f;
            ExplosiveModifier explosion = missileProjectile.gameObject.GetComponent<ExplosiveModifier>();
            if (explosion) Destroy(explosion);
            ExplosiveModifier explosiveModifier = missileProjectile.gameObject.AddComponent<ExplosiveModifier>();
            explosiveModifier.doExplosion = true;
            explosiveModifier.explosionData = FelissileExplosion;
            missileProjectile.baseData.range *= 0.5f;
            missileProjectile.baseData.speed *= 3f;
            missileProjectile.transform.parent = gun.barrelOffset;
            missileProjectile.SetProjectileSpriteRight("felissile_rocket_projectile", 16, 11, false, tk2dBaseSprite.Anchor.MiddleCenter, 14, 3);

           

            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            projectile.baseData.speed *= 1.1f;
            projectile.baseData.damage = 11f;
            projectile.SetProjectileSpriteRight("felissile_normal_projectile", 10, 9, true, tk2dBaseSprite.Anchor.MiddleCenter, 9, 8);
            projectile.hitEffects.alwaysUseMidair = true;
            projectile.hitEffects.overrideMidairDeathVFX = EasyVFXDatabase.WhiteCircleVFX;

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("Felissile Rockets", "NevernamedsItems/Resources/CustomGunAmmoTypes/felissile_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/felissile_clipempty");

            //gun.DefaultModule.finalAmmoType = GameUIAmmoType.AmmoType.CUSTOM;
            //gun.DefaultModule.finalCustomAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("Felissile Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/minutegun_clipempty", "NevernamedsItems/Resources/CustomGunAmmoTypes/minutegun_clipfull");

            gun.DefaultModule.finalProjectile = projectile;
            gun.gunHandedness = GunHandedness.HiddenOneHanded;

            gun.quality = PickupObject.ItemQuality.B; //A
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            ID = gun.PickupObjectId;
        }
        public static int ID;
        protected override void OnPickedUpByPlayer(PlayerController player)
        {
            base.OnPickedUpByPlayer(player);
            if (everPickedUpByPlayer)
            {
                if (gun)
                {
                    gun.ClipShotsRemaining = gun.ClipCapacity - 1;
                }
            }
        }
        protected override void Update()
        {
            if (gun && gun.CurrentOwner)
            {
                if (gun.DefaultModule.ammoCost < 10 && gun.ClipShotsRemaining > 9)
                {
                    gun.DefaultModule.ammoCost = 10;
                }
                else if (gun.DefaultModule.ammoCost >= 10 && gun.ClipShotsRemaining <= 9)
                {
                    gun.DefaultModule.ammoCost = 1;
                }
            }
            base.Update();
        }
        static ExplosionData bigExplosion = GameManager.Instance.Dungeon.sharedSettingsPrefab.DefaultExplosionData;
        static ExplosionData FelissileExplosion = new ExplosionData()
        {
            effect = bigExplosion.effect,
            ignoreList = bigExplosion.ignoreList,
            ss = bigExplosion.ss,
            damageRadius = 2.5f,
            damageToPlayer = 0f,
            doDamage = true,
            damage = 40,
            doDestroyProjectiles = true,
            doForce = true,
            debrisForce = 30f,
            preventPlayerForce = true,
            explosionDelay = 0.1f,
            usesComprehensiveDelay = false,
            doScreenShake = true,
            playDefaultSFX = true,
        };
        public override void OnPostFired(PlayerController player, Gun gun)
        {

        }
        public Felissile()
        {

        }
    }
}
