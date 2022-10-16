using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using Alexandria.ItemAPI;

namespace NevernamedsItems
{

    public class Hwacha : GunBehaviour
    {


        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Hwacha", "hwacha");
            Game.Items.Rename("outdated_gun_mods:hwacha", "nn:hwacha");
            gun.gameObject.AddComponent<Hwacha>();
            gun.SetShortDescription("15th Century");
            gun.SetLongDescription("An ancient machine designed to spew forth rocket-powered arrows. Some consider it the great grandfather of the modern minigun."+"\n\nOnce it has begun firing, it is difficult to stop it.");

            gun.SetupSprite(null, "hwacha_idle_001", 8);
            //ItemBuilder.AddPassiveStatModifier(gun, PlayerStats.StatType.GlobalPriceMultiplier, 0.925f, StatModifier.ModifyMethod.MULTIPLICATIVE);

            gun.SetAnimationFPS(gun.shootAnimation, 8);
            gun.SetAnimationFPS(gun.idleAnimation, 5);
            gun.muzzleFlashEffects.type = VFXPoolType.None;


            for (int i = 0; i < 3; i++)
            {
                gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(12) as Gun, true, false);
            }

            //GUN STATS
            foreach (ProjectileModule mod in gun.Volley.projectiles)
            {
                mod.ammoCost = 1;
                mod.shootStyle = ProjectileModule.ShootStyle.Burst;
                mod.burstShotCount = 200;
                mod.burstCooldownTime = 0.05f;
                mod.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
                mod.cooldownTime = 0.8f;
                mod.angleVariance = 40f;
                mod.numberOfShotsInClip = 200;
                Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(mod.projectiles[0]);
                mod.projectiles[0] = projectile;
                projectile.gameObject.SetActive(false);
                FakePrefab.MarkAsFakePrefab(projectile.gameObject);
                UnityEngine.Object.DontDestroyOnLoad(projectile);
                projectile.baseData.damage *= 0.5f;
                projectile.baseData.speed *= 1.5f;
                if (mod != gun.DefaultModule) { mod.ammoCost = 0; }
                projectile.transform.parent = gun.barrelOffset;
            }

            gun.reloadTime = 5f;
            gun.barrelOffset.transform.localPosition = new Vector3(2.87f, 0.68f, 0f);
            gun.SetBaseMaxAmmo(600);
            gun.PreventNormalFireAudio = true;
            gun.gunClass = GunClass.FULLAUTO;
            //BULLET STATS

            // Here we just set the quality of the gun and the "EncounterGuid", which is used by Gungeon to identify the gun.
            gun.quality = PickupObject.ItemQuality.A;
            gun.encounterTrackable.EncounterGuid = "this is the Hwacha";
            ETGMod.Databases.Items.Add(gun, null, "ANY");
            gun.SetTag("arrow_bolt_weapon");

            HwachaID = gun.PickupObjectId;
        }
        public static int HwachaID;
        public override void PostProcessProjectile(Projectile projectile)
        {
            base.PostProcessProjectile(projectile);
            PlayerController player = projectile.Owner as PlayerController;
            if (player.PlayerHasActiveSynergy("Ancient Traditions"))
            {
                projectile.baseData.damage *= 2f;
            }
        }
        public override void OnPostFired(PlayerController player, Gun gun)
        {
            gun.PreventNormalFireAudio = true;
            AkSoundEngine.PostEvent("Play_WPN_crossbow_shot_01", gameObject);
        }
        public override void OnReloadPressed(PlayerController player, Gun gun, bool bSOMETHING)
        {
            base.OnReloadPressed(player, gun, bSOMETHING);
            AkSoundEngine.PostEvent("Play_WPN_crossbow_reload_01", gameObject);
        }
        public override void Update()
        {
            if (gun && gun.CurrentOwner && gun.GunPlayerOwner())
            {


                PlayerController player = gun.CurrentOwner as PlayerController;
                if (player.PlayerHasActiveSynergy("Ancient Traditions"))
                {
                    if (gun.DefaultModule.burstShotCount == 200)
                    {
                        foreach (ProjectileModule mod in gun.Volley.projectiles)
                        {
                            mod.burstShotCount = 100;
                        }
                    }
                }
                else
                {
                    if (gun.DefaultModule.burstShotCount == 100)
                    {
                        foreach (ProjectileModule mod in gun.Volley.projectiles)
                        {
                            mod.burstShotCount = 200;
                        }
                    }
                }
            }
        }
        public Hwacha()
        {

        }
    }
}
