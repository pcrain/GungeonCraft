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

    public class StickGunQuickDraw : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Full Auto Stick Gun", "stickgunquickdraw");
            Game.Items.Rename("outdated_gun_mods:full_auto_stick_gun", "nn:stick_gun+quick_draw");
            var behav = gun.gameObject.AddComponent<StickGunQuickDraw>();
            behav.preventNormalReloadAudio = true;
            behav.preventNormalFireAudio = true;
            behav.overrideNormalFireAudio = "Play_PencilScratch";
            gun.SetShortDescription("Scribble");
            gun.SetLongDescription("Carried by a brave stickman as he ventured through the pages of a bored child's homework." + "\n\nHe may be long erased, but his legacy lives on.");

            gun.SetupSprite(null, "fullautostickgun_idle_001", 8);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(StickGun.StickGunID) as Gun, true, false);

            gun.gunHandedness = GunHandedness.TwoHanded;
            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Automatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 0.7f;
            gun.DefaultModule.cooldownTime = 0.1f;
            gun.DefaultModule.numberOfShotsInClip = 33;
            gun.barrelOffset.transform.localPosition = new Vector3(1.25f, 0.5f, 0f);
            gun.SetBaseMaxAmmo(333);
            gun.ammo = 333;
            gun.gunClass = GunClass.SHITTY;
            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(StickGun.StickGunID) as Gun).muzzleFlashEffects;

            gun.quality = PickupObject.ItemQuality.EXCLUDED;
            ETGMod.Databases.Items.Add(gun, null, "ANY");
            FullAutoStickGunID = gun.PickupObjectId;

        }
        public static int FullAutoStickGunID;
    }
}
