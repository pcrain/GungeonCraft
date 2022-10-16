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
using Alexandria.Misc;

namespace NevernamedsItems
{

    public class SporeLauncher : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Spore Launcher", "sporelauncher");
            Game.Items.Rename("outdated_gun_mods:spore_launcher", "nn:spore_launcher");
            gun.gameObject.AddComponent<SporeLauncher>();
            gun.SetShortDescription("Ain't He Cute?");
            gun.SetLongDescription("An infant alien from another dimension. Capable of storing potent spores in it's digestive tract in order to regurgitate them for self defense." + "\n\nEnjoys headpats, belly rubs, and those little fish-shaped cracker things.");

            gun.SetupSprite(null, "sporelauncher_idle_001", 8);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(599) as Gun).gunSwitchGroup;

            gun.SetAnimationFPS(gun.shootAnimation, 14);
            gun.SetAnimationFPS(gun.idleAnimation, 5);
            gun.SetAnimationFPS(gun.reloadAnimation, 10);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 2.5f;
            gun.DefaultModule.cooldownTime = 0.6f;
            gun.DefaultModule.numberOfShotsInClip = 10;
            gun.barrelOffset.transform.localPosition = new Vector3(2.31f, 0.75f, 0f);
            gun.SetBaseMaxAmmo(200);
            gun.gunClass = GunClass.RIFLE;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.transform.parent = gun.barrelOffset;
            projectile.baseData.speed *= 1f;
            projectile.baseData.damage *= 4f;
            projectile.baseData.range *= 10f;

            BounceProjModifier Bouncing = projectile.gameObject.GetOrAddComponent<BounceProjModifier>();
            Bouncing.numberOfBounces = 1;

            HomingModifier homing = projectile.gameObject.AddComponent<HomingModifier>();
            homing.AngularVelocity = 70f;
            homing.HomingRadius = 100f;

            projectile.SetProjectileSpriteRight("sporelauncher_projectile", 10, 10, false, tk2dBaseSprite.Anchor.MiddleCenter, 9, 9);

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("SporeLauncher Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/sporelauncher_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/sporelauncher_clipempty");

            gun.quality = PickupObject.ItemQuality.B;
            gun.encounterTrackable.EncounterGuid = "this is the Spore Launcher";
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            SporeLauncherID = gun.PickupObjectId;
            gun.SetupUnlockOnCustomFlag(CustomDungeonFlags.PURCHASED_SPORELAUNCHER, true);
            gun.AddItemToGooptonMetaShop(20);
            gun.SetTag("non_companion_living_item");
        }
        public static int SporeLauncherID;
        public override void OnPostFired(PlayerController player, Gun gun)
        {
            if (player.PlayerHasActiveSynergy("Enspore!"))
            {
                int id = Gungeon.Game.Items["nn:fungo_cannon"].PickupObjectId;
                if (player.HasPickupID(id))
                {
                    if (UnityEngine.Random.value <= 0.45) { player.GiveAmmoToGunNotInHand(id, 1); }
                }
            }
            base.OnPostFired(player, gun);
        }
        public SporeLauncher()
        {

        }
    }
}