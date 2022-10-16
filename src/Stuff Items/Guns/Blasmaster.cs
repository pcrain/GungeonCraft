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

    public class Blasmaster : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Blasmaster", "blasmaster");
            Game.Items.Rename("outdated_gun_mods:blasmaster", "nn:blasmaster");
            gun.gameObject.AddComponent<Blasmaster>();
            gun.SetShortDescription("Platoon Onboard");
            gun.SetLongDescription("Standard issue blasma blaster from the far reaches of space, outfitted with a special ruby from deep within the Black Powder Mines to maximise it's power.");

            gun.SetupSprite(null, "blasmaster_idle_001", 8);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(599) as Gun).gunSwitchGroup;

            gun.SetAnimationFPS(gun.shootAnimation, 14);
            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(809) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 0.8f;
            gun.DefaultModule.cooldownTime = 0.1f;
            gun.DefaultModule.numberOfShotsInClip = 8;
            gun.barrelOffset.transform.localPosition = new Vector3(1.12f, 0.56f, 0f);
            gun.SetBaseMaxAmmo(250);
            gun.gunClass = GunClass.PISTOL;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.transform.parent = gun.barrelOffset;
            projectile.baseData.speed *= 1f;
            projectile.baseData.damage *= 2f;
            projectile.baseData.range *= 0.3f;

            gun.quality = PickupObject.ItemQuality.C;
            gun.encounterTrackable.EncounterGuid = "this is the Blasmaster";
            ETGMod.Databases.Items.Add(gun, null, "ANY");
            gun.SetupUnlockOnCustomFlag(CustomDungeonFlags.PURCHASED_BLASMASTER, true);

            BlasmasterID = gun.PickupObjectId;
        }
        public static int BlasmasterID;
        public override void PostProcessProjectile(Projectile projectile)
        {
            base.PostProcessProjectile(projectile);
            if (hasBonusDamageFromSynergy)
            {
                projectile.baseData.damage *= 2f;
            }
        }
        private void onUsedPlayerItem(PlayerController player, PlayerItem activeItem)
        {
            if (activeItem.PickupObjectId == 250 || activeItem.PickupObjectId == 69)
            {
                hasBonusDamageFromSynergy = true;
                Invoke("ResetDamage", 3f);
            }
        }
        public void ResetDamage()
        {
            hasBonusDamageFromSynergy = false;
        }
        private bool hasBonusDamageFromSynergy;
        protected override void OnPostDroppedByPlayer(PlayerController player)
        {
            base.OnPostDroppedByPlayer(player);
            player.OnUsedPlayerItem -= this.onUsedPlayerItem;
        }
        protected override void OnPickedUpByPlayer(PlayerController player)
        {
            base.OnPickedUpByPlayer(player);
            player.OnUsedPlayerItem += this.onUsedPlayerItem;
        }
        public Blasmaster()
        {

        }
    }
}
