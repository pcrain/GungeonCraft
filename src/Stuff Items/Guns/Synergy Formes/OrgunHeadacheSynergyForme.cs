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

    public class OrgunHeadacheSynergyForme : GunBehaviour
    {


        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Bigass Brain Thing", "orgun_headache");
            Game.Items.Rename("outdated_gun_mods:bigass_brain_thing", "nn:orgun+headache");
            gun.gameObject.AddComponent<OrgunHeadacheSynergyForme>();
            gun.SetShortDescription("Activates those almonds");
            gun.SetLongDescription("Aww yeah, it's big brain time." + "\n\nAlso, if you're reading this, you're a cheaty haxxor.");

            gun.SetupSprite(null, "orgun_headache_idle_001", 8);
            //ItemBuilder.AddPassiveStatModifier(gun, PlayerStats.StatType.GlobalPriceMultiplier, 0.925f, StatModifier.ModifyMethod.MULTIPLICATIVE);

            gun.SetAnimationFPS(gun.shootAnimation, 8);
            gun.SetAnimationFPS(gun.idleAnimation, 5);

            for (int i = 0; i < 10; i++)
            {
                gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);
            }

            //GUN STATS
            foreach (ProjectileModule mod in gun.Volley.projectiles)
            {
                mod.ammoCost = 1;
                mod.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
                mod.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
                mod.cooldownTime = 0.9f;
                mod.angleVariance = 50f;
                mod.numberOfShotsInClip = 6;
                Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(mod.projectiles[0]);
                mod.projectiles[0] = projectile;
                projectile.gameObject.SetActive(false);
                FakePrefab.MarkAsFakePrefab(projectile.gameObject);
                UnityEngine.Object.DontDestroyOnLoad(projectile);
                projectile.baseData.damage *= 4f;
                projectile.baseData.speed *= 0.3f;
                projectile.SetProjectileSpriteRight("orgun_headache_projectile", 11, 14, true, tk2dBaseSprite.Anchor.MiddleCenter, 10, 13);
                if (mod != gun.DefaultModule) { mod.ammoCost = 0; }
                projectile.transform.parent = gun.barrelOffset;
            }

            gun.reloadTime = 1.5f;
            gun.barrelOffset.transform.localPosition = new Vector3(1.62f, 0.87f, 0f);
            gun.SetBaseMaxAmmo(120);

            //BULLET STATS

            // Here we just set the quality of the gun and the "EncounterGuid", which is used by Gungeon to identify the gun.
            gun.quality = PickupObject.ItemQuality.EXCLUDED;
            gun.encounterTrackable.EncounterGuid = "this is the Orgun Headache Synergy Forme";
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            OrgunHeadacheSynergyFormeID = gun.PickupObjectId;
        }
        public static int OrgunHeadacheSynergyFormeID;
        public OrgunHeadacheSynergyForme()
        {

        }
    }
}