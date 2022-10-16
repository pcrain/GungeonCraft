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

    public class KingBullatterer : AdvancedGunBehavior
    {

        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("King Bullatterer", "kingbullatterer");
            Game.Items.Rename("outdated_gun_mods:king_bullatterer", "nn:bullatterer+king_bullatterer");
            gun.gameObject.AddComponent<Bullatterer>();
            gun.SetShortDescription("djahksdhkssdfdfssdf");
            gun.SetLongDescription("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
            gun.SetupSprite(null, "kingbullatterer_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 13);
            gun.SetAnimationFPS(gun.idleAnimation, 13);
            for (int i = 0; i < 13; i++)
            {
                gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);
            }
            gun.muzzleFlashEffects.type = VFXPoolType.None;

            //GUN STATS
            gun.reloadTime = 1.5f;
            gun.barrelOffset.transform.localPosition = new Vector3(1.25f, 0.43f, 0f);
            gun.SetBaseMaxAmmo(400);
            gun.ammo = 400;

            Projectile bouncerProj = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(86) as Gun).DefaultModule.projectiles[0]);
            bouncerProj.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(bouncerProj.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(bouncerProj);
            bouncerProj.baseData.damage *= 1.2f;
            bouncerProj.baseData.speed *= 0.65f;
            bouncerProj.SetProjectileSpriteRight("yellow_enemystyle_projectile", 10, 10, true, tk2dBaseSprite.Anchor.MiddleCenter, 8, 8);

            BounceProjModifier bouncing = bouncerProj.gameObject.GetOrAddComponent<BounceProjModifier>();
            bouncing.numberOfBounces = 1;

            Projectile bullatSpawnerProj = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(86) as Gun).DefaultModule.projectiles[0]);
            bullatSpawnerProj.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(bullatSpawnerProj.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(bullatSpawnerProj);
            bullatSpawnerProj.baseData.damage *= 0f;
            bullatSpawnerProj.sprite.renderer.enabled = false;
            bullatSpawnerProj.pierceMinorBreakables = true;
            SpawnEnemyOnBulletSpawn orAddComponent = bullatSpawnerProj.gameObject.GetOrAddComponent<SpawnEnemyOnBulletSpawn>();
            orAddComponent.companioniseEnemy = true;
            orAddComponent.deleteProjAfterSpawn = true;
            orAddComponent.enemyBulletDamage = 15f;
            orAddComponent.guidToSpawn = EnemyGuidDatabase.Entries["bullat"];
            orAddComponent.ignoreSpawnedEnemyForGoodMimic = true;
            orAddComponent.killSpawnedEnemyOnRoomClear = true;
            orAddComponent.procChance = 1f;
            orAddComponent.scaleEnemyDamage = true;
            orAddComponent.scaleEnemyProjSize = true;
            orAddComponent.scaleEnemyProjSpeed = true;
            orAddComponent.doPostProcessOnEnemyBullets = false;

            int i2 = 0;
            foreach (ProjectileModule mod in gun.Volley.projectiles)
            {
                mod.numberOfShotsInClip = 15;
                mod.cooldownTime = 0.55f;
                mod.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
                mod.shootStyle = ProjectileModule.ShootStyle.Automatic;


                if (i2 <= 0) //bullatProj
                {
                    mod.ammoCost = 1;
                    mod.angleVariance = 0.01f;
                    mod.angleFromAim = 0f;
                    mod.projectiles[0] = bullatSpawnerProj;
                    i2++;
                }
                else if (i2 == 1) //0 degrees
                {
                    mod.ammoCost = 0;
                    mod.angleVariance = 7f;
                    mod.angleFromAim = 0f;
                    mod.projectiles[0] = bouncerProj;
                    i2++;
                }
                else if (i2 == 2) //30 degrees
                {
                    mod.ammoCost = 0;
                    mod.angleVariance = 7f;
                    mod.angleFromAim = 30f;
                    mod.projectiles[0] = bouncerProj;
                    i2++;
                }
                else if (i2 == 3) //60 degrees
                {
                    mod.ammoCost = 0;
                    mod.angleVariance = 7f;
                    mod.angleFromAim = 60f;
                    mod.projectiles[0] = bouncerProj;
                    i2++;
                }
                else if (i2 == 4) //90 degrees
                {
                    mod.ammoCost = 0;
                    mod.angleVariance = 7f;
                    mod.angleFromAim = 90f;
                    mod.projectiles[0] = bouncerProj;
                    i2++;
                }
                else if (i2 == 5) //120 degrees
                {
                    mod.ammoCost = 0;
                    mod.angleVariance = 7f;
                    mod.angleFromAim = 120f;
                    mod.projectiles[0] = bouncerProj;
                    i2++;
                }
                else if (i2 == 6) //150 degrees
                {
                    mod.ammoCost = 0;
                    mod.angleVariance = 7f;
                    mod.angleFromAim = 150f;
                    mod.projectiles[0] = bouncerProj;
                    i2++;
                }
                else if (i2 == 7) //180 degrees
                {
                    mod.ammoCost = 0;
                    mod.angleVariance = 7f;
                    mod.angleFromAim = 180f;
                    mod.projectiles[0] = bouncerProj;
                    i2++;
                }
                else if (i2 == 8) //-30 degrees
                {
                    mod.ammoCost = 0;
                    mod.angleVariance = 7f;
                    mod.angleFromAim = -30f;
                    mod.projectiles[0] = bouncerProj;
                    i2++;
                }
                else if (i2 == 9) //-60 degrees
                {
                    mod.ammoCost = 0;
                    mod.angleVariance = 7f;
                    mod.angleFromAim = -60f;
                    mod.projectiles[0] = bouncerProj;
                    i2++;
                }
                else if (i2 == 10) //-90 degrees
                {
                    mod.ammoCost = 0;
                    mod.angleVariance = 7f;
                    mod.angleFromAim = -90f;
                    mod.projectiles[0] = bouncerProj;
                    i2++;
                }
                else if (i2 == 11) //-120 degrees
                {
                    mod.ammoCost = 0;
                    mod.angleVariance = 7f;
                    mod.angleFromAim = -120f;
                    mod.projectiles[0] = bouncerProj;
                    i2++;
                }
                else if (i2 >= 12) //-150 degrees
                {
                    mod.ammoCost = 0;
                    mod.angleVariance = 7f;
                    mod.angleFromAim = -150f;
                    mod.projectiles[0] = bouncerProj;
                    i2++;
                }
            }

            gun.quality = PickupObject.ItemQuality.EXCLUDED;
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            KingBullattererID = gun.PickupObjectId;
        }
        public static int KingBullattererID;
        public KingBullatterer()
        {

        }       
    }
}
