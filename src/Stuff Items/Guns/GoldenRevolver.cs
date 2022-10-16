using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using Alexandria.ItemAPI;

namespace NevernamedsItems
{
    public class GoldenRevolver : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Golden Revolver", "goldenrevolver");
            Game.Items.Rename("outdated_gun_mods:golden_revolver", "nn:golden_revolver");
            gun.gameObject.AddComponent<GoldenRevolver>();
            gun.SetShortDescription("Pop Pop");
            gun.SetLongDescription("A flashy weapon made entirely out of solid gold."+"\n\nNot to be confused with the AU gun, which is meant to be hidden. This gun is meant to be shown off.");

            gun.SetupSprite(null, "goldenrevolver_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 10);

            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(38) as Gun).gunSwitchGroup;
            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(38) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.DefaultModule.cooldownTime = 0.1f;
            gun.DefaultModule.angleVariance = 5;
            gun.DefaultModule.numberOfShotsInClip = 6;
            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(38) as Gun).muzzleFlashEffects;
            gun.barrelOffset.transform.localPosition = new Vector3(1.25f, 0.75f, 0f);
            gun.SetBaseMaxAmmo(140);
            gun.ammo = 140;
            gun.gunClass = GunClass.PISTOL;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            AutoDoShadowChainOnSpawn chain = projectile.gameObject.AddComponent<AutoDoShadowChainOnSpawn>();
            chain.randomiseChainNum = true;
            chain.randomChainMin = 0;
            chain.randomChainMax = 5;
            chain.pauseLength = 0.05f;

            gun.DefaultModule.projectiles[0] = projectile;

            gun.quality = PickupObject.ItemQuality.S;
            ETGMod.Databases.Items.Add(gun, false, "ANY");
            GoldenRevolverID = gun.PickupObjectId;
        }
        public static int GoldenRevolverID;
        public GoldenRevolver()
        {

        }
    }
}
