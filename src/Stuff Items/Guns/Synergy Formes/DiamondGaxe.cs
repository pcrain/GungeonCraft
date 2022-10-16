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

    public class DiamondGaxe : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Diamond Gaxe", "diamondgaxe");
            Game.Items.Rename("outdated_gun_mods:diamond_gaxe", "nn:gaxe+diamond_gaxe");
            gun.gameObject.AddComponent<Gaxe>();
            gun.SetShortDescription("Diggy Diggy+");
            gun.SetLongDescription("Never spend your diamonds on a hoe");

            gun.SetupSprite(null, "diamondgaxe_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 16);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(335) as Gun).gunSwitchGroup;

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);
            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);
            //GUN STATS

            gun.reloadTime = 1f;
            gun.barrelOffset.transform.localPosition = new Vector3(2.62f, 1.12f, 0f);
            gun.SetBaseMaxAmmo(150);
            gun.gunClass = GunClass.PISTOL;
            //BULLET STATS           

            bool iterator = false;
            foreach (ProjectileModule mod in gun.Volley.projectiles)
            {
                if (iterator == false)
                {
                    mod.ammoCost = 1;

                    Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(mod.projectiles[0]);
                    mod.projectiles[0] = projectile;
                    projectile.gameObject.SetActive(false);
                    FakePrefab.MarkAsFakePrefab(projectile.gameObject);
                    UnityEngine.Object.DontDestroyOnLoad(projectile);
                    projectile.transform.parent = gun.barrelOffset;
                    projectile.baseData.range *= 2;
                    projectile.baseData.speed *= 1.5f;
                    projectile.baseData.damage = 14;
                    projectile.hitEffects.overrideMidairDeathVFX = (PickupObjectDatabase.GetById(506) as Gun).DefaultModule.projectiles[0].hitEffects.overrideMidairDeathVFX;
                    projectile.hitEffects.alwaysUseMidair = true;
                    projectile.SetProjectileSpriteRight("diamond_projectile", 11, 11, false, tk2dBaseSprite.Anchor.MiddleCenter, 10, 10);
                    iterator = true;
                }
                else
                {
                    mod.ammoCost = 0;
                    Projectile projectile = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
                    mod.projectiles[0] = projectile;
                    projectile.gameObject.SetActive(false);
                    FakePrefab.MarkAsFakePrefab(projectile.gameObject);
                    projectile.transform.parent = gun.barrelOffset;
                    UnityEngine.Object.DontDestroyOnLoad(projectile);
                    projectile.baseData.range *= 0.1f;
                    projectile.baseData.damage = 30;
                    projectile.specRigidbody.CollideWithTileMap = false;
                    projectile.pierceMinorBreakables = true;
                    PierceProjModifier piercing = projectile.gameObject.GetOrAddComponent<PierceProjModifier>();
                    piercing.penetration = 100;
                    MaintainDamageOnPierce maintenance = projectile.gameObject.GetOrAddComponent<MaintainDamageOnPierce>();
                    projectile.SetProjectileSpriteRight("gaxe_projectile", 12, 40, true, tk2dBaseSprite.Anchor.MiddleCenter, 8, 30);
                    DamageSecretWalls damageWalls = projectile.gameObject.GetOrAddComponent<DamageSecretWalls>();
                    //damageWalls.damageToDeal = 10f;
                }
                mod.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
                mod.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
                mod.cooldownTime = 0.35f;
                mod.angleVariance = 5f;
                mod.numberOfShotsInClip = 5;
            }

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("Diamond Gaxe Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/diamondgaxe_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/gaxe_clipempty");

            gun.quality = PickupObject.ItemQuality.EXCLUDED;
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            DiamondGaxeID = gun.PickupObjectId;
        }
        public static int DiamondGaxeID;
        public DiamondGaxe()
        {

        }
    }
}