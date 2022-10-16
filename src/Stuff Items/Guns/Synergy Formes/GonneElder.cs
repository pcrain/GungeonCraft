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

    public class GonneElder : GunBehaviour
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Elder Gonne", "eldergonne");
            Game.Items.Rename("outdated_gun_mods:elder_gonne", "nn:gonne+discworld");
            gun.gameObject.AddComponent<GonneElder>();
            gun.SetShortDescription("It Whispers To Me");
            gun.SetLongDescription("This peculiar old-fashioned firearm whispers offers of power and domination to it's bearer." + "\n\nFor the rest of the Galaxy's safety, it was cast away to the depths of the Gungeon.");
            gun.SetupSprite(null, "eldergonne_idle_001", 8);
            gun.SetAnimationFPS(gun.shootAnimation, 15);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(519) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Burst;
            gun.DefaultModule.burstShotCount = 6;
            gun.DefaultModule.burstCooldownTime = 0.1f;
            gun.DefaultModule.angleVariance = 5f;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 0.7f;
            gun.DefaultModule.cooldownTime = 0.5f;
            gun.DefaultModule.numberOfShotsInClip = 6;
            gun.barrelOffset.transform.localPosition = new Vector3(2.5f, 1.12f, 0f);
            gun.SetBaseMaxAmmo(300);

            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.speed *= 1.1f;
            projectile.baseData.damage *= 2.4f;
            projectile.transform.parent = gun.barrelOffset;

            gun.quality = PickupObject.ItemQuality.EXCLUDED;
            ETGMod.Databases.Items.Add(gun, null, "ANY");
            ElderGonneId = gun.PickupObjectId;
        }
        public static int ElderGonneId;
        public GonneElder()
        {

        }
    }
}