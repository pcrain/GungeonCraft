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

    public class GlobbitSMALL : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("GlobbitSmall", "globbitsmall");
            Game.Items.Rename("outdated_gun_mods:globbitsmall", "nn:globbit_small_forme");
            gun.gameObject.AddComponent<GlobbitSMALL>();
            gun.SetShortDescription("no");
            gun.SetLongDescription("life is suffering");
            gun.SetupSprite(null, "globbitsmall_idle_001", 8);

            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(404) as Gun).gunSwitchGroup;

            gun.SetAnimationFPS(gun.shootAnimation, 10);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);

            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1.1f; ;
            gun.DefaultModule.cooldownTime = 0.1f;
            gun.DefaultModule.numberOfShotsInClip = 9;
            gun.SetBaseMaxAmmo(250);
            // Here we just set the quality of the gun and the "EncounterGuid", which is used by Gungeon to identify the gun.
            gun.quality = PickupObject.ItemQuality.EXCLUDED; //C

            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage *= 0.8f;
            projectile.baseData.speed *= 1f;
            projectile.baseData.range *= 2f;
            GoopModifier blood = projectile.gameObject.AddComponent<GoopModifier>();
            blood.goopDefinition = EasyGoopDefinitions.BlobulonGoopDef;
            blood.SpawnGoopInFlight = true;
            blood.InFlightSpawnFrequency = 0.05f;
            blood.InFlightSpawnRadius = 0.5f;
            blood.SpawnGoopOnCollision = true;
            blood.CollisionSpawnRadius = 1f;


            projectile.SetProjectileSpriteRight("globbitsmall_projectile", 4, 4, false, tk2dBaseSprite.Anchor.MiddleCenter, 3, 3);



            projectile.transform.parent = gun.barrelOffset;


            gun.barrelOffset.transform.localPosition = new Vector3(0.93f, 0.75f, 0f);

            // Add the custom component here
            //AmmoBasedFormeChanger formes = gun.gameObject.AddComponent<AmmoBasedFormeChanger>();



            ETGMod.Databases.Items.Add(gun, null, "ANY");
            GlobbitSmallID = gun.PickupObjectId;
        }
        public static int GlobbitSmallID;
        public GlobbitSMALL()
        {

        }
    }
}