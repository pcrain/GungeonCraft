using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using Alexandria.ItemAPI;
using SaveAPI;

namespace NevernamedsItems
{
    public class Welgun : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Welgun", "welgun");
            Game.Items.Rename("outdated_gun_mods:welgun", "nn:welgun");
            var behav = gun.gameObject.AddComponent<Welgun>();
            gun.SetShortDescription("Yoink");
            gun.SetLongDescription("This gun has been specially manufactured to be compatible with the same shells used by the Gundead."+"\n\nAllows for the stealing of ammo from fallen foes.");

            gun.SetupSprite(null, "welgun_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 17);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Automatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1.3f;
            gun.DefaultModule.cooldownTime = 0.21f;
            //gun.muzzleFlashEffects.type = VFXPoolType.None;
            gun.DefaultModule.numberOfShotsInClip = 15;
            gun.barrelOffset.transform.localPosition = new Vector3(1.37f, 0.81f, 0f);
            gun.SetBaseMaxAmmo(300);
            gun.gunClass = GunClass.PISTOL;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage = 5f;
            projectile.baseData.speed *= 1.5f;
            projectile.baseData.range *= 10f;


            projectile.SetProjectileSpriteRight("welgun_projectile", 4, 4, true, tk2dBaseSprite.Anchor.MiddleCenter, 4, 4);

            projectile.transform.parent = gun.barrelOffset;


            gun.quality = PickupObject.ItemQuality.C;
            ETGMod.Databases.Items.Add(gun, false, "ANY");

            WelgunID = gun.PickupObjectId;
        }
        public static int WelgunID;
        public override void PostProcessProjectile(Projectile projectile)
        {
            projectile.OnHitEnemy += this.OnHitEnemy;
            base.PostProcessProjectile(projectile);
        }
        private void OnHitEnemy(Projectile proj, SpeculativeRigidbody enemy, bool fatal)
        {
            if (enemy && enemy.healthHaver && fatal)
            {
                if (gun && gun.GunPlayerOwner())
                {
                    if (UnityEngine.Random.value <= 0.1f)
                    {
                        gun.GainAmmo(UnityEngine.Random.Range(10, 16));
                    }
                }
            }
        }
        public Welgun()
        {

        }
    }
}
