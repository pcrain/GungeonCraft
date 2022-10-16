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
    public class UpNUp : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Up-N-Up", "upnup");
            Game.Items.Rename("outdated_gun_mods:upnup", "nn:up_n_up");
            gun.gameObject.AddComponent<UpNUp>();
            gun.SetShortDescription("Great Potential");
            gun.SetLongDescription("Though this pistol on it's own is unremarkable, the damage of it's bullets is affected TWICE by any bullet stat modifiers you recieve.");

            gun.SetupSprite(null, "upnup_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 12);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 0.5f;
            gun.DefaultModule.cooldownTime = 0.2f;
            gun.DefaultModule.numberOfShotsInClip = 7;
            gun.barrelOffset.transform.localPosition = new Vector3(1.31f, 0.81f, 0f);
            gun.SetBaseMaxAmmo(500);
            gun.gunClass = GunClass.PISTOL;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage *= 1f;
            projectile.baseData.speed *= 1f;
            projectile.baseData.range *= 1f;
            projectile.transform.parent = gun.barrelOffset;
            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("UpNUp Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/upnup_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/upnup_clipempty");
            gun.quality = PickupObject.ItemQuality.D;
            gun.encounterTrackable.EncounterGuid = "this is the Up-N-Up";
            ETGMod.Databases.Items.Add(gun, null, "ANY");
        }
        public override void PostProcessProjectile(Projectile projectile)
        {
            base.PostProcessProjectile(projectile);
            PlayerController player = projectile.Owner as PlayerController;
            projectile.baseData.damage *= player.stats.GetStatValue(PlayerStats.StatType.Damage);
            projectile.baseData.speed *= player.stats.GetStatValue(PlayerStats.StatType.ProjectileSpeed);
            projectile.baseData.force *= player.stats.GetStatValue(PlayerStats.StatType.KnockbackMultiplier);
            projectile.BossDamageMultiplier *= player.stats.GetStatValue(PlayerStats.StatType.DamageToBosses);
            projectile.UpdateSpeed();
        }
        public UpNUp()
        {

        }
    }
}
