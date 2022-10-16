using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using ItemAPI;
using UnityEngine;
using System.Reflection;

namespace NevernamedsItems
{

    public class DiscGunSuperDiscForme : GunBehaviour
    {
        public static int DiscGunSuperDiscSynergyFormeID;

        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Super Disc Gun", "discgun_superdiscsynergyforme");
            Game.Items.Rename("outdated_gun_mods:super_disc_gun", "nn:disc_gun+super_disc");
            gun.gameObject.AddComponent<DiscGunSuperDiscForme>();
            gun.SetShortDescription("Badder Choices");
            gun.SetLongDescription("Fires a shit-ton of discs. If you're reading this, you're a hacker.");

            gun.SetupSprite(null, "discgun_superdiscsynergyforme_idle_001", 8);
            gun.SetAnimationFPS(gun.shootAnimation, 14);

            for (int i = 0; i < 5; i++)
            {
                gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);
            }

            foreach (ProjectileModule mod in gun.Volley.projectiles)
            {
                mod.ammoCost = 1;
                mod.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
                mod.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
                mod.cooldownTime = 0.25f;
                mod.numberOfShotsInClip = 10;
                mod.angleVariance = 20f;

                Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(mod.projectiles[0]);
                projectile.gameObject.SetActive(false);
                FakePrefab.MarkAsFakePrefab(projectile.gameObject);
                UnityEngine.Object.DontDestroyOnLoad(projectile);
                mod.projectiles[0] = projectile;
                projectile.baseData.damage *= 4f;
                projectile.baseData.range *= 20f;
                projectile.baseData.speed *= 0.4f;
                projectile.SetProjectileSpriteRight("discgun_projectile", 15, 15, true, tk2dBaseSprite.Anchor.MiddleCenter, 9, 9);
                SelfHarmBulletBehaviour SuicidalTendancies = projectile.gameObject.AddComponent<SelfHarmBulletBehaviour>();

                PierceProjModifier Piercing = projectile.gameObject.GetOrAddComponent<PierceProjModifier>();
                Piercing.penetratesBreakables = true;
                Piercing.penetration += 10;
                BounceProjModifier Bouncing = projectile.gameObject.GetOrAddComponent<BounceProjModifier>();
                Bouncing.numberOfBounces = 10;
                if (mod != gun.DefaultModule) { mod.ammoCost = 0; }
                projectile.transform.parent = gun.barrelOffset;

            }

            gun.reloadTime = 1f;
            gun.SetBaseMaxAmmo(300);
            //gun.DefaultModule.positionOffset = new Vector3(1f, 0f, 0f);
            gun.barrelOffset.transform.localPosition = new Vector3(1.75f, 1.12f, 0f);


            //BULLET STATS

            gun.quality = PickupObject.ItemQuality.EXCLUDED;

            gun.encounterTrackable.EncounterGuid = "this is the Disc Gun Super Disc Synergy Forme";
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            string bleh = "Not a Bot, if you're sniffing around in my code, lookin to steal for the Nuclear Throne Mode, you're a stinker. It's cool, I'm a stinker too, just wanted to let you know";
            if (bleh == null) ETGModConsole.Log("BOT WHAT THE FUCK DID YOU DO");
                
            DiscGunSuperDiscSynergyFormeID = gun.PickupObjectId;
        }
        public DiscGunSuperDiscForme()
        {

        }
    }
}