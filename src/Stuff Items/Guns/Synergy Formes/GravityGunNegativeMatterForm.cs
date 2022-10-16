using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using ItemAPI;
using Dungeonator;
using System.Reflection;
using System.Collections.ObjectModel;

namespace NevernamedsItems
{
    public class GravityGunNegativeMatterForm : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Dark Matter Grav Gun", "gravitygunnegativematter");
            Game.Items.Rename("outdated_gun_mods:dark_matter_grav_gun", "nn:gravity_gun+negative_matter");
            gun.gameObject.AddComponent<GravityGunNegativeMatterForm>();
            gun.SetShortDescription("ding ding");
            gun.SetLongDescription("");

            gun.SetupSprite(null, "gravitygunnegativematter_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 25);

            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(562) as Gun).gunSwitchGroup;
            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);
            gun.usesContinuousFireAnimation = true;

            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).wrapMode = tk2dSpriteAnimationClip.WrapMode.LoopSection;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).loopStart = 1;

            //GUN STATS
            gun.barrelOffset.transform.localPosition = new Vector3(1.31f, 0.5f, 0f);
            gun.SetBaseMaxAmmo(10000);
            gun.ammo = 10000;

            gun.doesScreenShake = false;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Automatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.DefaultModule.cooldownTime = 0.1f;
            gun.DefaultModule.numberOfShotsInClip = 10000;
            gun.DefaultModule.angleVariance = 0f;
            gun.InfiniteAmmo = true;
            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.BEAM;
            gun.reloadTime = 0f;
            gun.muzzleFlashEffects.type = VFXPoolType.None;
            gun.DefaultModule.ammoCost = 0;
            gun.DefaultModule.projectiles[0] = null;



            gun.quality = PickupObject.ItemQuality.EXCLUDED;
            ETGMod.Databases.Items.Add(gun, null, "ANY");
            GravityGunNegativeMatterID = gun.PickupObjectId;
        }
        public static int GravityGunNegativeMatterID;
        public GravityGunNegativeMatterForm()
        {

        }             
    }
}
