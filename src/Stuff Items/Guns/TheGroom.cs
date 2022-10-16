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

    public class TheGroom : GunBehaviour
    {


        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("The Groom", "thegroom");
            Game.Items.Rename("outdated_gun_mods:the_groom", "nn:the_groom");
            gun.gameObject.AddComponent<TheGroom>();
            gun.SetShortDescription("Thin As A Broom");
            gun.SetLongDescription("Despite it's heart's longing, this gun missed it's wedding day. Now it roams the halls of the Gungeon, searching for it's bride.");

            gun.SetupSprite(null, "thegroom_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 13);
            gun.SetAnimationFPS(gun.idleAnimation, 5);

            for (int i = 0; i < 6; i++)
            {
                gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);
            }

            int iterator = 0;
            //GUN STATS
            foreach (ProjectileModule mod in gun.Volley.projectiles)
            {
                mod.ammoCost = 1;
                mod.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
                mod.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
                mod.cooldownTime = 0.5f;
                mod.angleVariance = 11.25f;
                mod.numberOfShotsInClip = 7;
                Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(mod.projectiles[0]);
                mod.projectiles[0] = projectile;
                projectile.gameObject.SetActive(false);
                FakePrefab.MarkAsFakePrefab(projectile.gameObject);
                UnityEngine.Object.DontDestroyOnLoad(projectile);
                projectile.baseData.range = 25;
                projectile.baseData.damage = 8f;
                projectile.hitEffects.alwaysUseMidair = true;
                projectile.hitEffects.overrideMidairDeathVFX = RainbowGuonStone.GreyGuonTransitionVFX;
                projectile.SetProjectileSpriteRight("groom_projectile", 12, 12, true, tk2dBaseSprite.Anchor.MiddleCenter, 10, 10);
                if (iterator == 0 || iterator == 1 || iterator == 2)
                {
                    mod.angleFromAim = 22.5f;
                }
                else if (iterator == 3 || iterator == 4 || iterator == 5)
                {
                    mod.angleFromAim = -22.5f;
                }
                iterator++;
                if (mod != gun.DefaultModule) { mod.ammoCost = 0; }
                projectile.transform.parent = gun.barrelOffset;
            }
            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("Groom Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/groom_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/groom_clipempty");
            gun.reloadTime = 1.5f;
            gun.gunHandedness = GunHandedness.TwoHanded;
            gun.barrelOffset.transform.localPosition = new Vector3(2.56f, 0.68f, 0f);
            gun.SetBaseMaxAmmo(100);
            gun.gunClass = GunClass.SHOTGUN;
            //BULLET STATS
            gun.quality = PickupObject.ItemQuality.B;
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            gun.Volley.UsesShotgunStyleVelocityRandomizer = true;

            TheGroomID = gun.PickupObjectId;
        }
        public static int TheGroomID;
        public TheGroom()
        {

        }
    }
}