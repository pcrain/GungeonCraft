using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using ItemAPI;
using Alexandria.Misc;

namespace NevernamedsItems
{

    public class TheBride : GunBehaviour
    {


        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("The Bride", "thebride");
            Game.Items.Rename("outdated_gun_mods:the_bride", "nn:the_bride");
            gun.gameObject.AddComponent<TheBride>();
            gun.SetShortDescription("Here Comes The Bride");
            gun.SetLongDescription("A lonesome gun, stood up on it's wedding day and forever hoping to be reunited with it's partner.");

            gun.SetupSprite(null, "thebride_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 13);
            gun.SetAnimationFPS(gun.idleAnimation, 5);

            for (int i = 0; i < 7; i++)
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
                projectile.baseData.range = 15;
                projectile.baseData.damage = 12f;
                projectile.hitEffects.alwaysUseMidair = true;
                projectile.hitEffects.overrideMidairDeathVFX = EasyVFXDatabase.WhiteCircleVFX;
                projectile.SetProjectileSpriteRight("bride_projectile", 12, 12, true, tk2dBaseSprite.Anchor.MiddleCenter, 10, 10);
                if (iterator == 1 || iterator == 2 || iterator == 3)
                {
                    mod.angleFromAim = 45;
                }
                else if (iterator == 4 || iterator == 5 || iterator == 6)
                {
                    mod.angleFromAim = -45;
                }
                iterator++;
                if (mod != gun.DefaultModule) { mod.ammoCost = 0; }
                projectile.transform.parent = gun.barrelOffset;
            }
            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("Bride Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/bride_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/groom_clipempty");
            gun.reloadTime = 1.5f;
            gun.gunHandedness = GunHandedness.TwoHanded;
            gun.barrelOffset.transform.localPosition = new Vector3(2.62f, 0.81f, 0f);
            gun.SetBaseMaxAmmo(100);
            gun.gunClass = GunClass.SHOTGUN;
            //BULLET STATS
            gun.quality = PickupObject.ItemQuality.B;
            ETGMod.Databases.Items.Add(gun, false, "ANY");

            gun.Volley.UsesShotgunStyleVelocityRandomizer = true;

            TheBrideID = gun.PickupObjectId;
        }
        public override void PostProcessProjectile(Projectile projectile)
        {
            if (projectile && projectile.ProjectilePlayerOwner() && projectile.ProjectilePlayerOwner().PlayerHasActiveSynergy("Wuv... twue wuv..."))
            {
                if (UnityEngine.Random.value <= 0.1f)
                {
                    projectile.AdjustPlayerProjectileTint(ExtendedColours.charmPink, 2);
                    projectile.statusEffectsToApply.Add(StaticStatusEffects.charmingRoundsEffect);
                }
            }
            base.PostProcessProjectile(projectile);
        }
        public static int TheBrideID;        
        public TheBride()
        {

        }
    }
}
