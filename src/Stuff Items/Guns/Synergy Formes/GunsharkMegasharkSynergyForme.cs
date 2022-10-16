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

    public class GunsharkMegasharkSynergyForme : GunBehaviour
    {
        public static int GunsharkMegasharkSynergyFormeID;


        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Megashark", "gunshark_megasharkforme");
            Game.Items.Rename("outdated_gun_mods:megashark", "nn:gunshark+megashark");
            gun.gameObject.AddComponent<Gunshark>();
            gun.SetShortDescription("Completely Awesomer");
            gun.SetLongDescription("Big shark gun go brr."+"\n\nIf you're reading this, you're a cheatsy haxor.");

            gun.SetupSprite(null, "gunshark_megasharkforme_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 17);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Automatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 2f;
            
            gun.DefaultModule.cooldownTime = 0.04f;
            gun.DefaultModule.numberOfShotsInClip = 200;
            gun.barrelOffset.transform.localPosition = new Vector3(3.12f, 0.68f, 0f);
            gun.SetBaseMaxAmmo(3996);
            gun.ammo = 3996;
            /*AdvancedTransformGunSynergyProcessor MegaSharkSynergyForme = gun.gameObject.AddComponent<AdvancedTransformGunSynergyProcessor>();
            MegaSharkSynergyForme.NonSynergyGunId = gun.PickupObjectId;
            MegaSharkSynergyForme.SynergyGunId = 1;
            MegaSharkSynergyForme.SynergyToCheck = "Megashark";*/

            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage *= 1.25f;
            projectile.ignoreDamageCaps = true;
            projectile.baseData.speed *= 3f;
            projectile.pierceMinorBreakables = true;
            //projectile.shouldRotate = true;
            projectile.SetProjectileSpriteRight("gunshark_projectile", 17, 4, true, tk2dBaseSprite.Anchor.MiddleCenter, 17, 4);

            projectile.transform.parent = gun.barrelOffset;

            // Here we just set the quality of the gun and the "EncounterGuid", which is used by Gungeon to identify the gun.
            gun.quality = PickupObject.ItemQuality.EXCLUDED;
            gun.encounterTrackable.EncounterGuid = "this is the Gunshark Megashark Synergy Form";
            ETGMod.Databases.Items.Add(gun, null, "ANY");
            GunsharkMegasharkSynergyFormeID = gun.PickupObjectId;

        }
        public override void PostProcessProjectile(Projectile projectile)
        {
            base.PostProcessProjectile(projectile);
        }        
        public override void OnPostFired(PlayerController player, Gun gun)
        {

        }


        //All that's left now is sprite stuff. 
        //Your sprites should be organized, like how you see in the mod folder. 
        //Every gun requires that you have a .json to match the sprites or else the gun won't spawn at all
        //.Json determines the hand sprites for your character. You can make a gun two handed by having both "SecondaryHand" and "PrimaryHand" in the .json file, which can be edited through Notepad or Visual Studios
        //By default this gun is a one-handed weapon
        //If you need a basic two handed .json. Just use the jpxfrd2.json.
        //And finally, don't forget to add your Gun to your ETGModule class!

        public GunsharkMegasharkSynergyForme()
        {

        }
    }
}