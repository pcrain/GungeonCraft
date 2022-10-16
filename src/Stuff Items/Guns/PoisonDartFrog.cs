using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Reflection;
using Gungeon;
using MonoMod;
using UnityEngine;
using Alexandria.ItemAPI;

namespace NevernamedsItems
{

    public class PoisonDartFrog : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Poison Dart Frog", "poisondartfrog");
            Game.Items.Rename("outdated_gun_mods:poison_dart_frog", "nn:poison_dart_frog");
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(599) as Gun).gunSwitchGroup;
            gun.SetShortDescription("Oh yeah, it's Frog Time");
            gun.SetLongDescription("An endangered frog species from inside the Gungeon. Spits poison darts to protect itself."+"\n\nHow do you 'fire' a frog? How do you RELOAD a frog??");
            gun.SetupSprite(null, "poisondartfrog_idle_001", 8);
            gun.SetAnimationFPS(gun.shootAnimation, 16);
            for (int i = 0; i < 3; i++)
            {
                gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);
            }

            //GUN STATS
            foreach (ProjectileModule mod in gun.Volley.projectiles)
            {
                mod.ammoCost = 1;
                mod.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
                mod.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
                mod.cooldownTime = 0.2f;
                mod.angleVariance = 10f;
                mod.numberOfShotsInClip = 3;
                Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(mod.projectiles[0]);
                mod.projectiles[0] = projectile;
                projectile.gameObject.SetActive(false);
                FakePrefab.MarkAsFakePrefab(projectile.gameObject);
                UnityEngine.Object.DontDestroyOnLoad(projectile);
                projectile.baseData.damage *= 1.4f;
                if (mod != gun.DefaultModule) { mod.ammoCost = 0; }
                projectile.transform.parent = gun.barrelOffset;
                projectile.damageTypes |= CoreDamageTypes.Poison;
                ExtremelySimplePoisonBulletBehaviour poisoning = projectile.gameObject.AddComponent<ExtremelySimplePoisonBulletBehaviour>();
                poisoning.procChance = 1;
                poisoning.useSpecialTint = false;
                projectile.SetProjectileSpriteRight("blowgun_projectile", 16, 7, false, tk2dBaseSprite.Anchor.MiddleCenter, 15, 8);
            }

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = "Blowgun Darts";

            gun.reloadTime = 0.5f;
            gun.SetBaseMaxAmmo(200);
            gun.quality = PickupObject.ItemQuality.C;
            gun.gunClass = GunClass.POISON;
            gun.encounterTrackable.EncounterGuid = "this is the Poison Dart Frog";
            ETGMod.Databases.Items.Add(gun, null, "ANY");
            gun.barrelOffset.transform.localPosition = new Vector3(1.37f, 0.37f, 0f);
            PoisonDartFrogID = gun.PickupObjectId;
            gun.SetTag("non_companion_living_item");

        }
        public static int PoisonDartFrogID;
        public PoisonDartFrog()
        {

        }
    }
}