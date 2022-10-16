using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Reflection;
using Gungeon;
using MonoMod;
using UnityEngine;
using ItemAPI;
using SaveAPI;

namespace NevernamedsItems
{

    public class StunGun : GunBehaviour
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Stun Gun", "stungun");
            Game.Items.Rename("outdated_gun_mods:stun_gun", "nn:stun_gun");
            gun.gameObject.AddComponent<StunGun>();
            gun.SetShortDescription("Bro!");
            gun.SetLongDescription("Delivers a potent electric shock to it's target."+"\n\nPopular amongst law enforcement, and as a personal protection sidearm.");

            gun.SetupSprite(null, "stungun_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 15);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 0.8f;
            gun.DefaultModule.cooldownTime = 0.2f;
            gun.DefaultModule.numberOfShotsInClip = 5;
            gun.barrelOffset.transform.localPosition = new Vector3(1.0f, 0.56f, 0f);
            gun.SetBaseMaxAmmo(300);
            gun.gunClass = GunClass.PISTOL;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage *= 0.8f;
            projectile.transform.parent = gun.barrelOffset;
            projectile.damageTypes |= CoreDamageTypes.Electric;
            projectile.AppliesStun = true;
            projectile.StunApplyChance = 0.75f;
            projectile.AppliedStunDuration = 5f;
            projectile.SetProjectileSpriteRight("stungun_projectile", 8, 4, false, tk2dBaseSprite.Anchor.MiddleCenter, 8, 4);

            gun.quality = PickupObject.ItemQuality.C;
            gun.encounterTrackable.EncounterGuid = "this is the Stun Gun";
            ETGMod.Databases.Items.Add(gun, null, "ANY");
            gun.SetupUnlockOnCustomFlag(CustomDungeonFlags.PURCHASED_STUNGUN, true);
            StunGunID = gun.PickupObjectId;
        }
        public static int StunGunID;
        public StunGun()
        {

        }
    }    
}

