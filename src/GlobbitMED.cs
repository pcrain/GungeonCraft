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

    public class GlobbitMED : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("GlobbitMedium", "globbitmed");
            Game.Items.Rename("outdated_gun_mods:globbitmedium", "nn:globbit_medium_forme");
            gun.gameObject.AddComponent<GlobbitMED>();
            gun.SetShortDescription("no");
            gun.SetLongDescription("no");
            gun.SetupSprite(null, "globbitmed_idle_001", 8);

            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(404) as Gun).gunSwitchGroup;

            gun.SetAnimationFPS(gun.shootAnimation, 10);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);

            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1.1f; ;
            gun.DefaultModule.cooldownTime = 0.3f;
            gun.DefaultModule.numberOfShotsInClip = 6;
            gun.SetBaseMaxAmmo(250);
            // Here we just set the quality of the gun and the "EncounterGuid", which is used by Gungeon to identify the gun.
            gun.quality = PickupObject.ItemQuality.EXCLUDED; //C

            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage *= 1.6f;
            projectile.baseData.speed *= 0.8f;
            projectile.baseData.range *= 2f;
            GoopModifier blood = projectile.gameObject.AddComponent<GoopModifier>();
            blood.goopDefinition = EasyGoopDefinitions.BlobulonGoopDef;
            blood.SpawnGoopInFlight = true;
            blood.InFlightSpawnFrequency = 0.05f;
            blood.InFlightSpawnRadius = 0.5f;
            blood.SpawnGoopOnCollision = true;
            blood.CollisionSpawnRadius = 1f;


                projectile.SetProjectileSpriteRight("globbitmed_projectile", 7, 7, false, tk2dBaseSprite.Anchor.MiddleCenter, 6, 6);



            projectile.transform.parent = gun.barrelOffset;


            gun.barrelOffset.transform.localPosition = new Vector3(1.31f, 0.81f, 0f);

            // Add the custom component here
            //AmmoBasedFormeChanger formes = gun.gameObject.AddComponent<AmmoBasedFormeChanger>();



            ETGMod.Databases.Items.Add(gun, null, "ANY");
            GlobbitMediumID = gun.PickupObjectId;
        }
        public static int GlobbitMediumID;
        public GlobbitMED   ()
        {

        }
    }
}