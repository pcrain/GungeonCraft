using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using ItemAPI;
using SaveAPI;

namespace NevernamedsItems
{
    public class OlReliable : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Ol' Reliable", "olreliable");
            Game.Items.Rename("outdated_gun_mods:ol'_reliable", "nn:ol_reliable");
            var behav = gun.gameObject.AddComponent<OlReliable>();

            gun.SetShortDescription("Old School Cool");
            gun.SetLongDescription("This well oiled rifle was brought to the Gungeon by a student of the Gungeon Master, and is older than the Gungeon itself!"+"\n\nPassed down through the generations, Ol' Reliable has been a favourite of Gungeoneers from all walks of life.");

            gun.SetupSprite(null, "olreliable_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 12);
            gun.SetAnimationFPS(gun.reloadAnimation, 0);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(49) as Gun, true, false);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(5) as Gun).gunSwitchGroup;

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1.2f;
            gun.DefaultModule.cooldownTime = 0.3f;
            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(5) as Gun).muzzleFlashEffects;

            gun.DefaultModule.numberOfShotsInClip = 5;
            gun.barrelOffset.transform.localPosition = new Vector3(2.5f, 0.62f, 0f);
            gun.SetBaseMaxAmmo(130);
            gun.gunClass = GunClass.RIFLE;

            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage = 20f;
            projectile.baseData.speed *= 1.1f;
            projectile.baseData.range *= 10f;

            projectile.pierceMinorBreakables = true;
            
            PierceProjModifier orAddComponent = projectile.gameObject.GetOrAddComponent<PierceProjModifier>();
            orAddComponent.penetratesBreakables = true;
            orAddComponent.penetration = 1;


            gun.quality = PickupObject.ItemQuality.B;
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            ID = gun.PickupObjectId;
        }
        public static int ID;
        public OlReliable()
        {

        }
    }
}
