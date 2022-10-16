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

namespace NevernamedsItems
{

    public class OrbOfTheGun : AdvancedGunBehavior
    {


        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Orb of the Gun", "orbofthegun");
            Game.Items.Rename("outdated_gun_mods:orb_of_the_gun", "nn:orb_of_the_gun");
            var behav = gun.gameObject.AddComponent<OrbOfTheGun>();
            behav.preventNormalFireAudio = true;
            behav.overrideNormalFireAudio = "Play_BOSS_agunim_orb_01";
            gun.SetShortDescription("Hypershot");
            gun.SetLongDescription("This gun is from a strange non-euclidian plane of reality, where it used to be able to only point in one direction." + "\n\nReloading when the clip is more than 50% empty sends bullets close to you to another dimension.");

            gun.SetupSprite(null, "orbofthegun_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 15);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1.2f;
            gun.DefaultModule.cooldownTime = 0.7f;
            gun.muzzleFlashEffects.type = VFXPoolType.None;
            gun.DefaultModule.numberOfShotsInClip = 15;
            gun.gunHandedness = GunHandedness.HiddenOneHanded;
            gun.barrelOffset.transform.localPosition = new Vector3(1.25f, 0.62f, 0f);
            gun.SetBaseMaxAmmo(125);
            gun.gunClass = GunClass.RIFLE;

            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage *= 4f;
            projectile.baseData.speed *= 2f;
            projectile.pierceMinorBreakables = true;

            projectile.SetProjectileSpriteRight("orbofthegun_projectile", 20, 12, true, tk2dBaseSprite.Anchor.MiddleCenter, 20, 12);

            gun.quality = PickupObject.ItemQuality.A;
            ETGMod.Databases.Items.Add(gun, false, "ANY");

        }
        public override void OnReload(PlayerController player, Gun gun)
        {
            if (player != null && gun != null)
            {
                if (gun.ClipShotsRemaining < gun.ClipCapacity / 2)
                {
                    player.DoEasyBlank(player.specRigidbody.UnitCenter, EasyBlankType.MINI);
                }
            }
            base.OnReload(player, gun);
        }



        public OrbOfTheGun()
        {

        }
    }
}

