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

    public class DartRifle : GunBehaviour
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Dart Rifle", "dartrifle");
            Game.Items.Rename("outdated_gun_mods:dart_rifle", "nn:dart_rifle");
            gun.gameObject.AddComponent<DartRifle>();
            gun.SetShortDescription("Tactical Incapacitation");
            gun.SetLongDescription("Used to incapacitate large animals for transport."+"\n\nLoaded with a powerful sedative.");

            gun.SetupSprite(null, "dartrifle_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 15);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.DefaultModule.cooldownTime = 0.6f;
            gun.DefaultModule.numberOfShotsInClip = 5;
            gun.barrelOffset.transform.localPosition = new Vector3(2.12f, 0.56f, 0f);
            gun.SetBaseMaxAmmo(100);
            gun.gunClass = GunClass.RIFLE;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.transform.parent = gun.barrelOffset;
            projectile.baseData.speed *= 2f;
            projectile.baseData.damage *= 1.4f;
            projectile.AppliesStun = true;
            projectile.AppliedStunDuration += 7f;
            projectile.StunApplyChance = 1f;
            projectile.SetProjectileSpriteRight("dartrifle_projectile", 16, 7, false, tk2dBaseSprite.Anchor.MiddleCenter, 14, 3);

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("Dart Rifle Darts", "NevernamedsItems/Resources/CustomGunAmmoTypes/dartrifle_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/dartrifle_clipempty");

            gun.quality = PickupObject.ItemQuality.B;
            ETGMod.Databases.Items.Add(gun, null, "ANY");
            gun.SetupUnlockOnCustomFlag(CustomDungeonFlags.PURCHASED_DARTRIFLE, true);
            DartRifleID = gun.PickupObjectId;
        }
        public override void PostProcessProjectile(Projectile projectile)
        {
            PlayerController player = projectile.Owner as PlayerController;
            base.PostProcessProjectile(projectile);
            if (player.PlayerHasActiveSynergy("Old and New"))
            {
                projectile.damageTypes |= CoreDamageTypes.Poison;
                ExtremelySimplePoisonBulletBehaviour poisoning = projectile.gameObject.AddComponent<ExtremelySimplePoisonBulletBehaviour>();
                poisoning.procChance = 1;
                poisoning.useSpecialTint = false;
            }
        }
        public static int DartRifleID;
        public DartRifle()
        {

        }
    }
}