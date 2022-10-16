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
using SaveAPI;

namespace NevernamedsItems
{

    public class PhaserSpiderling : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Phaser Spiderling", "phaserspiderling");
            Game.Items.Rename("outdated_gun_mods:phaser_spiderling", "nn:phaser_spiderling");
            var behav = gun.gameObject.AddComponent<PhaserSpiderling>();
            behav.preventNormalReloadAudio = true;
            behav.overrideNormalReloadAudio = "Play_ENM_PhaseSpider_Weave_01";
            behav.preventNormalFireAudio = true;
            behav.overrideNormalFireAudio = "Play_ENM_PhaseSpider_Spray_01";

            gun.SetShortDescription("Arachnikov");
            gun.SetLongDescription("The hatchling spawn of a Phaser Spider."+"\n\nOne of the first organs to fully develop are the spinnerets.");

            gun.SetupSprite(null, "phaserspiderling_idle_001", 8);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(86) as Gun).gunSwitchGroup;
            gun.muzzleFlashEffects.type = VFXPoolType.None;


            gun.SetAnimationFPS(gun.shootAnimation, 15);
            gun.SetAnimationFPS(gun.idleAnimation, 9);

            for (int i = 0; i < 7; i++)
            {
                gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);
            }
            //Easy Variables to Change all the Modules
            float AllModCooldown = 0.1f;
            int AllModClipshots = 500;
            float LesserModCooldown = 0.4f;


            int i2 = 0;
            foreach (ProjectileModule mod in gun.Volley.projectiles)
            {
                mod.numberOfShotsInClip = AllModClipshots;

                Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(mod.projectiles[0]);
                mod.projectiles[0] = projectile;
                projectile.gameObject.SetActive(false);
                FakePrefab.MarkAsFakePrefab(projectile.gameObject);
                UnityEngine.Object.DontDestroyOnLoad(projectile);
                projectile.baseData.damage *= 1f;
                projectile.baseData.speed *= 0.5f;
                projectile.baseData.range *= 2f;

                GoopModifier webbing = projectile.gameObject.AddComponent<GoopModifier>();
                webbing.goopDefinition = EasyGoopDefinitions.PlayerFriendlyWebGoop;
                webbing.SpawnGoopInFlight = true;
                webbing.InFlightSpawnFrequency = 0.05f;
                webbing.InFlightSpawnRadius = 1f;
                webbing.SpawnGoopOnCollision = true;
                webbing.CollisionSpawnRadius = 2f;

                projectile.SetProjectileSpriteRight("yellow_enemystyle_projectile", 10, 10, true, tk2dBaseSprite.Anchor.MiddleCenter, 8, 8);

                if (i2 <= 0) //First Common Proj (-30)
                {
                    mod.ammoCost = 0;
                    mod.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
                    mod.shootStyle = ProjectileModule.ShootStyle.Automatic;
                    mod.cooldownTime = AllModCooldown;
                    mod.angleVariance = 0.01f;
                    mod.angleFromAim = -30f;
                    i2++;
                }
                else if (i2 == 1) //Second Common Proj (0)
                {
                    mod.ammoCost = 1;
                    mod.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
                    mod.shootStyle = ProjectileModule.ShootStyle.Automatic;
                    mod.cooldownTime = AllModCooldown;
                    mod.angleVariance = 0.01f;
                    mod.angleFromAim = 0.01f;

                    i2++;
                }
                else if (i2 == 2) //Third Common Proj (30)
                {
                    mod.ammoCost = 0;
                    mod.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
                    mod.shootStyle = ProjectileModule.ShootStyle.Automatic;
                    mod.cooldownTime = AllModCooldown;
                    mod.angleFromAim = 30f;
                    mod.angleVariance = 0.1f;

                    i2++;
                }
                else if (i2 == 3) //First uncommon Proj
                {
                    mod.ammoCost = 0;
                    mod.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
                    mod.shootStyle = ProjectileModule.ShootStyle.Automatic;
                    mod.cooldownTime = LesserModCooldown;
                    mod.angleFromAim = -10f;
                    mod.angleVariance = 0.1f;
                    webbing.SpawnGoopInFlight = false;

                    i2++;
                }
                else if (i2 == 4) //Second uncommon Proj
                {
                    mod.ammoCost = 0;
                    mod.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
                    mod.shootStyle = ProjectileModule.ShootStyle.Automatic;
                    mod.cooldownTime = LesserModCooldown;
                    mod.angleFromAim = -20f;
                    mod.angleVariance = 0.1f;
                    webbing.SpawnGoopInFlight = false;

                    i2++;
                }
                else if (i2 == 5) //First uncommon Proj
                {
                    mod.ammoCost = 0;
                    mod.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
                    mod.shootStyle = ProjectileModule.ShootStyle.Automatic;
                    mod.cooldownTime = LesserModCooldown;
                    mod.angleFromAim = 10f;
                    mod.angleVariance = 0.1f;
                    webbing.SpawnGoopInFlight = false;

                    i2++;
                }
                else if (i2 >= 6) //First uncommon Proj
                {
                    mod.ammoCost = 0;
                    mod.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
                    mod.shootStyle = ProjectileModule.ShootStyle.Automatic;
                    mod.cooldownTime = LesserModCooldown;
                    mod.angleFromAim = 20f;
                    mod.angleVariance = 0.1f;
                    webbing.SpawnGoopInFlight = false;

                    i2++;
                }
            }
            gun.reloadTime = 1.4f;
            gun.barrelOffset.transform.localPosition = new Vector3(1.25f, 0.62f, 0f);
            gun.SetBaseMaxAmmo(600);

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("PhaserSpiderling Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/phaserspiderling_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/phaserspiderling_clipempty");

            gun.gunClass = GunClass.FULLAUTO;
            gun.quality = PickupObject.ItemQuality.S;
            ETGMod.Databases.Items.Add(gun, null, "ANY");
            gun.SetTag("non_companion_living_item");

            PhaserSpiderlingID = gun.PickupObjectId;
            gun.SetupUnlockOnCustomFlag(CustomDungeonFlags.PHASERSPIDER_QUEST_REWARDED, true);
        }
        public static int PhaserSpiderlingID;
        public PhaserSpiderling()
        {

        }
    }
}
