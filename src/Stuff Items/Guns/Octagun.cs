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

    public class Octagun : GunBehaviour
    {


        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Octagun", "octagun");
            Game.Items.Rename("outdated_gun_mods:octagun", "nn:octagun");
            gun.gameObject.AddComponent<Octagun>();
            gun.SetShortDescription("Welcome To The 2nd Dimension");
            gun.SetLongDescription("A simple shape, with a name that kinda sounds like it has 'gun' in it."+"\n\nOften confused by preschoolers for it's much more fashionable cousin, the Pentagon.");

            gun.SetupSprite(null, "octagun_idle_001", 8);
            //ItemBuilder.AddPassiveStatModifier(gun, PlayerStats.StatType.GlobalPriceMultiplier, 0.925f, StatModifier.ModifyMethod.MULTIPLICATIVE);

            gun.SetAnimationFPS(gun.shootAnimation, 8);
            gun.SetAnimationFPS(gun.idleAnimation, 5);

            for (int i = 0; i < 8; i++)
            {
                gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);
            }

            //GUN STATS
            foreach (ProjectileModule mod in gun.Volley.projectiles)
            {
                mod.ammoCost = 8;
                mod.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
                mod.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
                mod.cooldownTime = 0.8f;
                mod.angleVariance = 35f;
                mod.numberOfShotsInClip = 8;
                Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(mod.projectiles[0]);
                mod.projectiles[0] = projectile;
                projectile.gameObject.SetActive(false);
                FakePrefab.MarkAsFakePrefab(projectile.gameObject);
                UnityEngine.Object.DontDestroyOnLoad(projectile);
                projectile.baseData.damage *= 1.6f;
                projectile.baseData.speed *= 0.1f;
                projectile.SetProjectileSpriteRight("octagun_projectile", 10, 10, true, tk2dBaseSprite.Anchor.MiddleCenter, 8, 8);
                if (mod != gun.DefaultModule) { mod.ammoCost = 0; }
                projectile.transform.parent = gun.barrelOffset;
            }

            gun.reloadTime = 1.5f;
            gun.barrelOffset.transform.localPosition = new Vector3(1.0f, 0.43f, 0f);
            gun.SetBaseMaxAmmo(888);
            gun.gunClass = GunClass.SHOTGUN;
            //BULLET STATS
            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("Octagun Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/octagun_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/octagun_clipempty");

            // Here we just set the quality of the gun and the "EncounterGuid", which is used by Gungeon to identify the gun.
            gun.quality = PickupObject.ItemQuality.C;
            gun.encounterTrackable.EncounterGuid = "this is the Octagun";
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            OctagunID = gun.PickupObjectId;
        }
        public static int OctagunID;
        public override void PostProcessProjectile(Projectile projectile)
        {
            base.PostProcessProjectile(projectile);
            PlayerController player = projectile.Owner as PlayerController;
            if (player.PlayerHasActiveSynergy("Shapes N' Beats"))
            {
                projectile.baseData.speed *= 3f;
            }
        }
        
        public override void Update()
        {
            PlayerController player = gun.CurrentOwner as PlayerController;
            if (player.PlayerHasActiveSynergy("Shapes N' Beats"))
            {
                if (gun.DefaultModule.cooldownTime == 0.8f)
                {
                    foreach (ProjectileModule mod in gun.Volley.projectiles)
                    {
                        mod.cooldownTime = 0.2f;       
                    }
                }
            }
            else
            {
                if (gun.DefaultModule.cooldownTime == 0.2f)
                {
                    foreach (ProjectileModule mod in gun.Volley.projectiles)
                    {
                        mod.cooldownTime = 0.8f;                     
                    }
                }
            }
        }
        public Octagun()
        {

        }
    }
   
}
