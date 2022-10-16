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
using Dungeonator;
using Alexandria.Misc;

namespace NevernamedsItems
{

    public class Pista : GunBehaviour
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Pista", "pista");
            Game.Items.Rename("outdated_gun_mods:pista", "nn:pista");
            gun.gameObject.AddComponent<Pista>();
            gun.SetShortDescription("Yeeeeehaw!");
            gun.SetLongDescription("Six tiny spirits inhabit this gun, gleefully riding it's bullets into battle, and re-aiming them towards the nearest target when the owner signals them via reloading."+"\n\nThis gun smells vaguely Italian.");

            gun.SetupSprite(null, "pista_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 15);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.DefaultModule.cooldownTime = 0.15f;
            gun.DefaultModule.numberOfShotsInClip = 6;
            gun.barrelOffset.transform.localPosition = new Vector3(0.81f, 0.62f, 0f);
            gun.SetBaseMaxAmmo(200);
            gun.gunClass = GunClass.PISTOL;
            //BULLET STATS
            Projectile projectile = gun.DefaultModule.projectiles[0].InstantiateAndFakeprefab();
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.transform.parent = gun.barrelOffset;
            projectile.baseData.speed *= 0.65f;
            projectile.baseData.range *= 2f;
            projectile.baseData.damage *= 1.6f;
            SelfReAimBehaviour reaim = projectile.gameObject.GetOrAddComponent<SelfReAimBehaviour>();
            reaim.maxReloadReAims = 1;
            reaim.trigger = SelfReAimBehaviour.ReAimTrigger.RELOAD;

            gun.quality = PickupObject.ItemQuality.C;
            ETGMod.Databases.Items.Add(gun, false, "ANY");

            PistaID = gun.PickupObjectId;
        }
        public static int PistaID;
        public override void PostProcessProjectile(Projectile projectile)
        {
            if (projectile.gameObject.GetComponent<SelfReAimBehaviour>() && projectile.ProjectilePlayerOwner() && projectile.ProjectilePlayerOwner().PlayerHasActiveSynergy("Pistols Requiem"))
            {
                projectile.gameObject.GetComponent<SelfReAimBehaviour>().maxReloadReAims = 100;
            }
            base.PostProcessProjectile(projectile);
        }        
    }
}