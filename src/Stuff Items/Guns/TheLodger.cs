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

    public class TheLodger : GunBehaviour
    {


        public static void Add()
        {
            
            Gun gun = ETGMod.Databases.Items.NewGun("The Lodger", "lodger");
           
            Game.Items.Rename("outdated_gun_mods:the_lodger", "nn:the_lodger");
            gun.gameObject.AddComponent<TheLodger>();
            gun.SetShortDescription("Cherish What You Have");
            gun.SetLongDescription("Many Gungeoneers have a bad habit of turning their noses up at items they deem to be of poor quality, but the Lodger seeks to teach them a lesson in humility.");
        
            gun.SetupSprite(null, "lodger_idle_001", 8);
 
            gun.SetAnimationFPS(gun.shootAnimation, 10);
            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);
            gun.DefaultModule.ammoCost = 3;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1.1f;
            gun.DefaultModule.cooldownTime = 0.3f;
            gun.DefaultModule.numberOfShotsInClip = 10;
            gun.SetBaseMaxAmmo(1924);
            gun.gunClass = GunClass.SHITTY;
            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(26) as Gun).muzzleFlashEffects;
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(477) as Gun).gunSwitchGroup;
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage = 5f;
            projectile.SetProjectileSpriteRight("lodger_projectile", 8, 9, false, tk2dBaseSprite.Anchor.MiddleCenter, 7, 8);
            projectile.hitEffects.alwaysUseMidair = true;
            projectile.hitEffects.overrideMidairDeathVFX = (PickupObjectDatabase.GetById(28) as Gun).DefaultModule.projectiles[0].hitEffects.tileMapVertical.effects[0].effects[0].effect;

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("Lodger Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/lodger_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/lodger_clipempty");

            gun.quality = PickupObject.ItemQuality.D;
            ETGMod.Databases.Items.Add(gun, null, "ANY");
            TheLodgerID = gun.PickupObjectId;
        }
        public static int TheLodgerID;
        public override void PostProcessProjectile(Projectile projectile)
        {
            PlayerController playerController = this.gun.CurrentOwner as PlayerController;
            float badStuffModifier = 1f;
            foreach (PassiveItem item in playerController.passiveItems)
            {
                if (badStuff.Contains(item.PickupObjectId))
                {
                    badStuffModifier += 0.1f;
                }
                else if (reallyBadStuff.Contains(item.PickupObjectId))
                {
                    badStuffModifier += 0.2f;
                }
                else if (item.PickupObjectId == 127)
                {
                    badStuffModifier += 0.05f;
                }
            }
            foreach (PlayerItem item in playerController.activeItems)
            {
                if (badStuff.Contains(item.PickupObjectId))
                {
                    badStuffModifier += 0.1f;
                }
                else if (reallyBadStuff.Contains(item.PickupObjectId))
                {
                    badStuffModifier += 0.2f;
                }
                else if (item.PickupObjectId == 409f)
                {
                    badStuffModifier += 1f;
                }
            }
            foreach (Gun gun in playerController.inventory.AllGuns)
            {
                if (badStuff.Contains(gun.PickupObjectId))
                {
                    badStuffModifier += 0.1f;
                }
                else if (reallyBadStuff.Contains(gun.PickupObjectId))
                {
                    badStuffModifier += 0.2f;
                }
            }
            projectile.baseData.damage *= badStuffModifier;
            base.PostProcessProjectile(projectile);
        }
        public static List<int> badStuff = new List<int>()
        {
            378, //Derringer
            122, //Blunderbuss
            440, //Ruby Bracelet
            63, //Medkit
            104, //Ration
            108, //Bomb
            109, //Ice Bomb
            234, //iBomb Companion App
            403, //Melted Rock
            462, //Smoke Bomb
            216, //Box
            205, //Poison Vial
            201, //Portable Turret
            448, //Boomerang
            447, //Shield of the Maiden
            521, //Chance Bullets
            488, //Ring of Chest Vampirism
            256, //Heavy Boots
            119, //Metronome
            432, //Jar of Bees
            306, //Escape Rope
            106, //Jetpack
            487, //Book of Chest Anatomy
            197, //Pea Shooter
            83, //Unfinished Gun
            79, //Makarov
            9, //Dueling Pistol
            10, //Mega Douser
            510, //JK-47
            383, //Flash Ray
            334, //Wind Up Gun
            3, //Screecher
            196, //Fossilised Gun
            26, //Nail Gun
            292, //Molotov Launcher
            340, //Lower Case R
            150, //T-Shirt Cannon
        };
        public static List<int> reallyBadStuff = new List<int>()
        {
            209, //Sense of Direction  
            460, //Chaff Grenade
            136, //C4
            66, //Proximity Mine
            308, //Cluster Mine
            132, //Ring of Miserly Protection
            31, //Klobbe
            202, //Sawed-Off
        };
        public TheLodger()
        {

        }
    }
}
