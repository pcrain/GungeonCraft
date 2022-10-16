using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Reflection;
using Gungeon;
using MonoMod;
using UnityEngine;
using Alexandria.ItemAPI;

namespace NevernamedsItems
{

    public class ARCRifle : GunBehaviour
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("ARC Rifle", "arcrifle");
            Game.Items.Rename("outdated_gun_mods:arc_rifle", "nn:arc_rifle");
            gun.gameObject.AddComponent<ARCRifle>();
            gun.SetShortDescription("Electrotech Assassin");
            gun.SetLongDescription("This slow firing yet powerful electric rifle was patented by the ARC Private Security Company for effective 'ranged situation management'."+"\n\nColloquial nicknames such as 'Thunderstick' are not uncommon among ARC personnel.");

            gun.SetupSprite(null, "arcrifle_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 15);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(ARCPistol.ID) as Gun, true, false);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(153) as Gun).gunSwitchGroup;

            //GUN STATS
            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(41) as Gun).muzzleFlashEffects;
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.DefaultModule.cooldownTime = 1f;
            gun.DefaultModule.numberOfShotsInClip = 8;
            gun.barrelOffset.transform.localPosition = new Vector3(47f / 16f, 8f / 16f, 0f);
            gun.SetBaseMaxAmmo(180);
            gun.gunClass = GunClass.RIFLE;
            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = "ARC Bullets";

            //BULLET STATS
            Projectile projectile = gun.DefaultModule.projectiles[0].gameObject.InstantiateAndFakeprefab().GetComponent<Projectile>();
            projectile.baseData.damage = 26f;
            PierceProjModifier pierce = projectile.gameObject.GetOrAddComponent<PierceProjModifier>();
            pierce.penetration++;
            gun.DefaultModule.projectiles[0] = projectile;

            gun.quality = PickupObject.ItemQuality.B;
            ETGMod.Databases.Items.Add(gun, false, "ANY");

            ID = gun.PickupObjectId;
        }
        public static int ID;
    }
}