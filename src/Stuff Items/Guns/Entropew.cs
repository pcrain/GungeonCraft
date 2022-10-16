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
using MonoMod.RuntimeDetour;
using System.Reflection;

namespace NevernamedsItems
{
    public class Entropew : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Entropew", "entropew");
            Game.Items.Rename("outdated_gun_mods:entropew", "nn:entropew");
            gun.gameObject.AddComponent<Entropew>();
            gun.SetShortDescription("Controlled Chaos");
            gun.SetLongDescription("An icon of disorder and displacement, given form by the Gungeon's Magickes." + "\n\nStrange things seem to happen when a chest is opened while holding this gun...");

            gun.SetupSprite(null, "entropew_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 16);

            for (int i = 0; i < 2; i++)
            {
                gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);
            }

            //GUN STATS
            foreach (ProjectileModule mod in gun.Volley.projectiles)
            {
                mod.ammoCost = 1;
                mod.shootStyle = ProjectileModule.ShootStyle.Automatic;
                mod.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
                mod.cooldownTime = 0.1f;
                mod.angleVariance = 2f;
                mod.numberOfShotsInClip = 40;
                Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(mod.projectiles[0]);
                mod.projectiles[0] = projectile;
                projectile.gameObject.SetActive(false);
                FakePrefab.MarkAsFakePrefab(projectile.gameObject);
                UnityEngine.Object.DontDestroyOnLoad(projectile);
                projectile.baseData.range *= 10;
                projectile.baseData.damage = 6;
                projectile.SetProjectileSpriteRight("entropew_projectile", 5, 7, true, tk2dBaseSprite.Anchor.MiddleCenter, 4, 6);

                if (mod != gun.DefaultModule)
                {
                    mod.ammoCost = 0;
                    mod.angleVariance = 40;
                }
                projectile.transform.parent = gun.barrelOffset;

            }

            gun.reloadTime = 1.5f;
            gun.barrelOffset.transform.localPosition = new Vector3(2.18f, 0.75f, 0f);
            gun.SetBaseMaxAmmo(300);
            gun.gunClass = GunClass.FULLAUTO;
            gun.quality = PickupObject.ItemQuality.A;
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            chestPreOpenHook = new Hook(
                typeof(Chest).GetMethod("Open", BindingFlags.Instance | BindingFlags.NonPublic),
                typeof(Entropew).GetMethod("ChestPreOpen", BindingFlags.Static | BindingFlags.Public)
            );
            EntropewID = gun.PickupObjectId;
        }
        public static int EntropewID;
        public static Hook chestPreOpenHook;
        public static void ChestPreOpen(Action<Chest, PlayerController> orig, Chest chest, PlayerController opener)
        {
            if (opener.CurrentGun && opener.CurrentGun.PickupObjectId == EntropewID)
            {
                if (opener.CurrentGun.ammo >= 150)
                {
                    List<PickupObject.ItemQuality> Qualities = new List<PickupObject.ItemQuality>()
                    {
                        PickupObject.ItemQuality.D,
                        PickupObject.ItemQuality.C,
                        PickupObject.ItemQuality.B,
                        PickupObject.ItemQuality.A,
                        PickupObject.ItemQuality.S,
                    };

                    chest.PredictContents(opener);
                    List<PickupObject> newCont = new List<PickupObject>();
                    int itemNum = chest.contents.Count;
                    for (int i = 0; i < itemNum; i++)
                    {
                        PickupObject item = LootEngine.GetItemOfTypeAndQuality<PickupObject>(BraveUtility.RandomElement(Qualities), null, false);
                        newCont.Add(item);
                    }
                    chest.contents.Clear();
                    chest.contents.AddRange(newCont);
                    opener.CurrentGun.ammo -= 150;
                }
            }
            orig(chest, opener);
        }
        
        public Entropew()
        {

        }
    }
}
