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

    public class Gonne : GunBehaviour
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Gonne", "gonne");
            Game.Items.Rename("outdated_gun_mods:gonne", "nn:gonne");
            gun.gameObject.AddComponent<Gonne>();
            gun.SetShortDescription("It Whispers To Me");
            gun.SetLongDescription("This peculiar old-fashioned firearm whispers offers of power and domination to it's bearer."+"\n\nFor the rest of the Galaxy's safety, it was cast away to the depths of the Gungeon.");
            gun.SetupSprite(null, "gonne_idle_001", 8);
            gun.SetAnimationFPS(gun.shootAnimation, 15);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(519) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Burst;
            gun.DefaultModule.burstShotCount = 3;
            gun.DefaultModule.burstCooldownTime = 0.1f;
            gun.DefaultModule.angleVariance = 5f;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 0.7f;
            gun.DefaultModule.cooldownTime = 0.5f;
            gun.DefaultModule.numberOfShotsInClip = 3;
            gun.barrelOffset.transform.localPosition = new Vector3(2.18f, 0.62f, 0f);
            gun.SetBaseMaxAmmo(300);
            gun.gunClass = GunClass.RIFLE;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.speed *= 1.1f;
            projectile.baseData.damage *= 2.4f;
            projectile.transform.parent = gun.barrelOffset;

            gun.quality = PickupObject.ItemQuality.B;
            gun.encounterTrackable.EncounterGuid = "this is the Gonne";
            ETGMod.Databases.Items.Add(gun, null, "ANY");
            GonneID = gun.PickupObjectId;
        }
        public static int GonneID;
        public override void PostProcessProjectile(Projectile projectile)
        {
            base.PostProcessProjectile(projectile);
            PlayerController player = gun.CurrentOwner as PlayerController;
            if (player.PlayerHasActiveSynergy("Discworld"))
            {
                HomingModifier homing = projectile.gameObject.AddComponent<HomingModifier>();
                homing.AngularVelocity = 250f;
                    homing.HomingRadius = 250f;
            }
        }
       // private bool hasDiscworldSynergyAlready = false;
        public Gonne()
        {

        }
    }
}