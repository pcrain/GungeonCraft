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

    public class GlobbitMEGA : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Globbit", "globbitlarge");
            Game.Items.Rename("outdated_gun_mods:globbit", "nn:globbit");
            gun.gameObject.AddComponent<GlobbitMEGA>();
            gun.SetShortDescription("Oh!");
            gun.SetLongDescription("Shrinks slowly as it's ammo depletes."+"\n\nA former Blobulonian soldier, genetically modified beyond the point of sapience until only a gun remained.");
            gun.SetupSprite(null, "globbitlarge_idle_001", 8);

            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(404) as Gun).gunSwitchGroup;

            gun.SetAnimationFPS(gun.shootAnimation, 10);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);

            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1.1f;;
            gun.DefaultModule.cooldownTime = 0.5f;
            gun.DefaultModule.numberOfShotsInClip = 3;
            gun.SetBaseMaxAmmo(250);
            // Here we just set the quality of the gun and the "EncounterGuid", which is used by Gungeon to identify the gun.
            gun.quality = PickupObject.ItemQuality.EXCLUDED; //B

            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage *= 3f;
            projectile.baseData.speed *= 0.5f;
            projectile.pierceMinorBreakables = true;
            projectile.baseData.range *= 2f;
            GoopModifier blood = projectile.gameObject.AddComponent<GoopModifier>();
            blood.goopDefinition = EasyGoopDefinitions.BlobulonGoopDef;
            blood.SpawnGoopInFlight = true;
            blood.InFlightSpawnFrequency = 0.05f;
            blood.InFlightSpawnRadius = 1f;
            blood.SpawnGoopOnCollision = true;
            blood.CollisionSpawnRadius = 2f;


            //ANIMATE BULLETS
            projectile.AnimateProjectile(new List<string> {
                "globbitlargeproj_1",
                "globbitlargeproj_2",
                "globbitlargeproj_1",
                "globbitlargeproj_3",
            }, 8, true, new List<IntVector2> {
                new IntVector2(15, 15), //1
                new IntVector2(13, 17), //2            
                new IntVector2(15, 15), //3
                new IntVector2(17, 13), //4
            }, AnimateBullet.ConstructListOfSameValues(false, 4), AnimateBullet.ConstructListOfSameValues(tk2dBaseSprite.Anchor.MiddleCenter, 4), AnimateBullet.ConstructListOfSameValues(true, 4), AnimateBullet.ConstructListOfSameValues(false, 4),
            AnimateBullet.ConstructListOfSameValues<Vector3?>(null, 4), AnimateBullet.ConstructListOfSameValues<IntVector2?>(null, 4), AnimateBullet.ConstructListOfSameValues<IntVector2?>(null, 4), AnimateBullet.ConstructListOfSameValues<Projectile>(null, 4));


            projectile.transform.parent = gun.barrelOffset;


            gun.barrelOffset.transform.localPosition = new Vector3(1.93f, 1.12f, 0f);

            // Add the custom component here
            AmmoBasedFormeChanger formes = gun.gameObject.AddComponent<AmmoBasedFormeChanger>();
            formes.baseGunID = gun.PickupObjectId;
            formes.lowerAmmoGunID = GlobbitMED.GlobbitMediumID;
            formes.lowerAmmoAmount = 200;
            formes.lowestAmmoGunID = GlobbitSMALL.GlobbitSmallID;
            formes.lowestAmmoAmount = 100;


            GlobbitLargeID = gun.PickupObjectId;
            ETGMod.Databases.Items.Add(gun, null, "ANY");
        }
        public static int GlobbitLargeID;
        public GlobbitMEGA()
        {

        }
    }
}