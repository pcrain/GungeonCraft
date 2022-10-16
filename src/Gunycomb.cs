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

    public class Gunycomb : GunBehaviour
    {


        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Gunycomb", "gunycomb");
            Game.Items.Rename("outdated_gun_mods:gunycomb", "nn:gunycomb");
            gun.gameObject.AddComponent<Gunycomb>();
            gun.SetShortDescription("Shreddin' Vapours");
            gun.SetLongDescription("Used in rebel attacks on remote Hegemony of Man outposts, this high-tech tool of destruction is geared to take out heavy targets." + "\n\nIgnores boss DPS cap.");

            gun.SetupSprite(null, "antimaterielrifle_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 15);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(10) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Beam;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 0.0001f;
            gun.DefaultModule.cooldownTime = 0.10f;
            gun.muzzleFlashEffects.type = VFXPoolType.None;
            gun.DefaultModule.numberOfShotsInClip = 900;
            gun.barrelOffset.transform.localPosition = new Vector3(2.75f, 0.56f, 0f);
            gun.SetBaseMaxAmmo(900);

            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage *= 1f;
            projectile.baseData.speed *= 1f;
            projectile.DefaultTintColor = ExtendedColours.honeyYellow;
            GoopModifier goopmod = projectile.GetComponent<GoopModifier>();
            goopmod.goopDefinition = EasyGoopDefinitions.HoneyGoop;


            projectile.transform.parent = gun.barrelOffset;

            // Here we just set the quality of the gun and the "EncounterGuid", which is used by Gungeon to identify the gun.
            gun.quality = PickupObject.ItemQuality.EXCLUDED;
            gun.encounterTrackable.EncounterGuid = "this is the Antimateriel Rifle";
            ETGMod.Databases.Items.Add(gun, null, "ANY");

        }

        public Gunycomb()
        {

        }
    }
}