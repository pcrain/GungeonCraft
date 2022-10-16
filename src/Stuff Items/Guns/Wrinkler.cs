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

namespace NevernamedsItems
{

    public class Wrinkler : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Wrinkler", "wrinkler");
            Game.Items.Rename("outdated_gun_mods:wrinkler", "nn:wrinkler");
            var behav = gun.gameObject.AddComponent<Wrinkler>();
            behav.overrideNormalReloadAudio = "Play_ENM_Tarnisher_Bite_01";
            behav.overrideNormalFireAudio = "Play_ENM_Tarnisher_Spit_01";
            behav.preventNormalFireAudio = true;
            behav.preventNormalReloadAudio = true;
            gun.SetShortDescription("Bite The Bullet");
            gun.SetLongDescription("Eats bullets on reload, resulting in a net ammo profit overall." + "\n\nAn elder-ich being whose gluttony knows no grounds. \nIt's odd fractal digestive tract seems to allow it to regurgitate more material than it ingests.");

            gun.SetupSprite(null, "wrinkler_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 12);
            gun.SetAnimationFPS(gun.reloadAnimation, 15);


            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.muzzleFlashEffects.type = VFXPoolType.None;
            gun.DefaultModule.cooldownTime = 0.3f;
            gun.DefaultModule.numberOfShotsInClip = 10;
            gun.barrelOffset.transform.localPosition = new Vector3(3.0f, 0.59f, 0f);
            gun.SetBaseMaxAmmo(100);
            gun.ammo = 100;
            gun.gunClass = GunClass.SILLY;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            projectile.baseData.damage = 8.5f;
            projectile.AnimateProjectile(new List<string> {
                "wrinklerproj_001",
                "wrinklerproj_002",
                "wrinklerproj_003",
                "wrinklerproj_004",
                "wrinklerproj_005",
            },
            14, //FPS
            true, //Loops
            AnimateBullet.ConstructListOfSameValues(new IntVector2(18, 10), 5), //Sprite Sizes
            AnimateBullet.ConstructListOfSameValues(false, 5), //Lightened
            AnimateBullet.ConstructListOfSameValues(tk2dBaseSprite.Anchor.MiddleCenter, 5), //Anchors
            AnimateBullet.ConstructListOfSameValues(true, 5), //Anchors Change Colliders
            AnimateBullet.ConstructListOfSameValues(false, 5), //Fixes Scales
            AnimateBullet.ConstructListOfSameValues<Vector3?>(null, 5),  //Manual Offsets
            AnimateBullet.ConstructListOfSameValues<IntVector2?>(new IntVector2(14, 8), 5), //Collider Pixel Sizes
            AnimateBullet.ConstructListOfSameValues<IntVector2?>(null, 5), //Override Collider Offsets
            AnimateBullet.ConstructListOfSameValues<Projectile>(null, 5));

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("Wrinkler Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/wrinkler_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/wrinkler_clipempty");

            gun.DefaultModule.projectiles[0] = projectile;
            gun.quality = PickupObject.ItemQuality.A;
            ETGMod.Databases.Items.Add(gun, false, "ANY");

            WrinklerID = gun.PickupObjectId;
        }
        public static int WrinklerID;
        private Vector2 EatPosition()
        {
            if (gun && gun.GunPlayerOwner() && gun.sprite)
            {
                PlayerController player = gun.GunPlayerOwner();
                Vector2 vector = player.CenterPosition;
                Vector2 normalized = (player.unadjustedAimPoint.XY() - vector).normalized;
                Vector2 pos = (gun.sprite.WorldCenter + normalized * 1f);
                return pos;
            }
            else return Vector2.zero;
        }
        protected override void Update()
        {
            if (gun && gun.GunPlayerOwner() && gun.GunPlayerOwner().CurrentGun != null && gun.GunPlayerOwner().CurrentGun.PickupObjectId == WrinklerID)
            {
                if (gun.IsReloading)
                {
                    if (StaticReferenceManager.AllProjectiles != null && StaticReferenceManager.AllProjectiles.Count > 0)
                    {
                        for (int i = (StaticReferenceManager.AllProjectiles.Count - 1); i >= 0; i--)
                        {
                            Projectile bullet = StaticReferenceManager.AllProjectiles[i];
                            if (bullet && (bullet.Owner == null || !(bullet.Owner is PlayerController)))
                            {
                                if (!bullet.ImmuneToBlanks && bullet.specRigidbody != null)
                                {
                                    if (Vector2.Distance(EatPosition(), bullet.specRigidbody.UnitCenter) < 1.5f)
                                    {
                                        bullet.DieInAir();
                                        if (UnityEngine.Random.value <= 0.2) gun.GainAmmo(2);
                                        else gun.GainAmmo(1);

                                    }
                                }
                            }
                        }
                    }
                }
            }
            base.Update();
        }      
        public Wrinkler()
        {

        }
    }

}