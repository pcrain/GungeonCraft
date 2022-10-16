using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using Alexandria.ItemAPI;
using Alexandria.Misc;

namespace NevernamedsItems
{

    public class SaltGun : AdvancedGunBehavior
    {


        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Salt Gun", "saltgun");
            Game.Items.Rename("outdated_gun_mods:salt_gun", "nn:salt_gun");
            gun.gameObject.AddComponent<SaltGun>();
            gun.SetShortDescription("Rub It In");
            gun.SetLongDescription("A way over-the-top method of pest control, used in the Gungeon to avoid Kaliber's disdain for flyswatters.");

            gun.SetupSprite(null, "saltgun_idle_001", 8);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(150) as Gun).gunSwitchGroup;
            gun.SetAnimationFPS(gun.shootAnimation, 13);
            gun.SetAnimationFPS(gun.idleAnimation, 5);

            for (int i = 0; i < 5; i++)
            {
                gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);
            }

            //GUN STATS
            foreach (ProjectileModule mod in gun.Volley.projectiles)
            {

                mod.ammoCost = 1;
                mod.shootStyle = ProjectileModule.ShootStyle.Automatic;
                mod.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
                mod.cooldownTime = 0.25f;
                mod.angleVariance = 7f;
                mod.numberOfShotsInClip = 10;
                Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(mod.projectiles[0]);
                mod.projectiles[0] = projectile;
                projectile.gameObject.SetActive(false);
                FakePrefab.MarkAsFakePrefab(projectile.gameObject);
                UnityEngine.Object.DontDestroyOnLoad(projectile);
                projectile.SetProjectileSpriteRight("salt_projectile", 2, 2, false, tk2dBaseSprite.Anchor.MiddleCenter, 2, 2);
                projectile.baseData.range = 6;
                projectile.baseData.damage = 2;
                projectile.baseData.force *= 0.3f;
                if (mod != gun.DefaultModule) { mod.ammoCost = 0; }
                projectile.transform.parent = gun.barrelOffset;
            }
            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("SaltGun Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/saltgun_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/saltgun_clipempty");
            gun.reloadTime = 0.8f;
            gun.barrelOffset.transform.localPosition = new Vector3(1.43f, 0.37f, 0f);
            gun.SetBaseMaxAmmo(300);
            gun.gunClass = GunClass.SHOTGUN;
            gun.quality = PickupObject.ItemQuality.D;
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            gun.Volley.UsesShotgunStyleVelocityRandomizer = true;

            SaltGunID = gun.PickupObjectId;
        }
        protected override void OnPickedUpByPlayer(PlayerController player)
        {
            player.OnTableFlipped += this.Flip;
            base.OnPickedUpByPlayer(player);
        }
        protected override void OnPostDroppedByPlayer(PlayerController player)
        {
            player.OnTableFlipped -= this.Flip;
            base.OnPostDroppedByPlayer(player);
        }
        public override void OnDestroy()
        {
            if (gun && gun.GunPlayerOwner())
            {
                gun.GunPlayerOwner().OnTableFlipped -= this.Flip;

            }
            base.OnDestroy();
        }
        float tableDamageBonusDuration = 0;
        protected override void Update()
        {
            if (tableDamageBonusDuration >= 0)
            {
                tableDamageBonusDuration -= BraveTime.DeltaTime;
            }
            base.Update();
        }
        private void Flip(FlippableCover table)
        {
            if (gun && gun.GunPlayerOwner() && gun.GunPlayerOwner().PlayerHasActiveSynergy("Table Salt"))
            {
                tableDamageBonusDuration += 7;
            }
        }
        public override void PostProcessProjectile(Projectile projectile)
        {
            if (tableDamageBonusDuration > 0)
            {
                projectile.baseData.damage *= 2;
            }
            if (projectile.ProjectilePlayerOwner() && projectile.ProjectilePlayerOwner().PlayerHasActiveSynergy("Pillars Of Salt"))
            {
                projectile.baseData.range *= 3;
            }
            base.PostProcessProjectile(projectile);
        }

        public static int SaltGunID;       
        public SaltGun()
        {

        }
    }
}
