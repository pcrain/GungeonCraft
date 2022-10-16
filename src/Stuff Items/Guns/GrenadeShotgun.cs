using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using ItemAPI;
using SaveAPI;

namespace NevernamedsItems
{

    public class GrenadeShotgun : GunBehaviour
    {


        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Grenade Shotgun", "grenadeshotgun");
            Game.Items.Rename("outdated_gun_mods:grenade_shotgun", "nn:grenade_shotgun");
            gun.gameObject.AddComponent<GrenadeShotgun>();
            gun.SetShortDescription("Sit Down");
            gun.SetLongDescription("The product of combining two of the most entertaining classes of weaponry- Shotguns, and the ones that explode.");

            gun.SetupSprite(null, "grenadeshotgun_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 13);

            for (int i = 0; i < 4; i++)
            {
                gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(19) as Gun, true, false);
            }

            //GUN STATS
            foreach (ProjectileModule mod in gun.Volley.projectiles)
            {

                mod.ammoCost = 1;
                mod.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
                mod.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
                mod.cooldownTime = 1f;
                mod.angleVariance = 15f;
                mod.numberOfShotsInClip = 1;
                Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(mod.projectiles[0]);
                mod.projectiles[0] = projectile;
                projectile.gameObject.SetActive(false);
                FakePrefab.MarkAsFakePrefab(projectile.gameObject);
                UnityEngine.Object.DontDestroyOnLoad(projectile);
                projectile.baseData.range = 7;
                BounceProjModifier Bouncing = projectile.gameObject.GetOrAddComponent<BounceProjModifier>();
                Bouncing.numberOfBounces = 1;
                if (mod != gun.DefaultModule) { mod.ammoCost = 0; }
                projectile.transform.parent = gun.barrelOffset;
            }

            gun.reloadTime = 2f;
            gun.barrelOffset.transform.localPosition = new Vector3(2.18f, 0.68f, 0f);
            gun.SetBaseMaxAmmo(40);
            gun.gunClass = GunClass.EXPLOSIVE;
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(150) as Gun).gunSwitchGroup;
            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(37) as Gun).muzzleFlashEffects;

            gun.quality = PickupObject.ItemQuality.A;
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            gun.Volley.UsesShotgunStyleVelocityRandomizer = true;
            gun.AddToSubShop(ItemBuilder.ShopType.Trorc);

            GrenadeShotgunID = gun.PickupObjectId;
            gun.SetupUnlockOnCustomFlag(CustomDungeonFlags.PURCHASED_GRENADESHOTGUN, true);
            gun.AddItemToTrorcMetaShop(40);
        }
        public override void PostProcessProjectile(Projectile projectile)
        {
            if (projectile)
            {
                projectile.baseData.speed *= UnityEngine.Random.Range(0.5f, 1.5f);
                projectile.baseData.range *= UnityEngine.Random.Range(1f, 1.5f);
            }
            base.PostProcessProjectile(projectile);
        }
        public static int GrenadeShotgunID;        
        public GrenadeShotgun()
        {

        }
    }
}
