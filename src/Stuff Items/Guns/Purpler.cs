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

namespace NevernamedsItems
{

    public class Purpler : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Purpler", "purpler");
            Game.Items.Rename("outdated_gun_mods:purpler", "nn:purpler");
            gun.gameObject.AddComponent<Purpler>();
            gun.SetShortDescription("Burning Bills");
            gun.SetLongDescription("Fires purple in it's rawest form." + "\n\nThis inconveniently small blaster was made for much more diminutive beings with no fingers.");

            gun.SetupSprite(null, "purpler_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 15);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(89) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Burst;
            gun.DefaultModule.burstShotCount = 3;
            gun.DefaultModule.burstCooldownTime = 0.2f;
            gun.DefaultModule.angleVariance = 5f;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.DefaultModule.cooldownTime = 0.7f;
            gun.DefaultModule.numberOfShotsInClip = 12;
            gun.barrelOffset.transform.localPosition = new Vector3(1.06f, 0.5f, 0f);
            gun.SetBaseMaxAmmo(300);
            gun.gunClass = GunClass.SHITTY;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage *= 1.6f;

            projectile.baseData.range *= 0.7f;
            projectile.transform.parent = gun.barrelOffset;
            projectile.SetProjectileSpriteRight("purpler_projectile", 8, 6, true, tk2dBaseSprite.Anchor.MiddleCenter, 6, 6);
            projectile.hitEffects.alwaysUseMidair = true;
            projectile.hitEffects.overrideMidairDeathVFX = EasyVFXDatabase.RedLaserCircleVFX;

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = "Thinline Bullets";

            gun.quality = PickupObject.ItemQuality.C;
            gun.encounterTrackable.EncounterGuid = "this is the Purpler";
            ETGMod.Databases.Items.Add(gun, null, "ANY");
            PurplerID = gun.PickupObjectId;
        }
        public static int PurplerID;
        private void changedGun(Gun oldGun, Gun newGun, bool what)
        {
            //ETGModConsole.Log("Guns changed");
            flightCheck(newGun);
        }
        private void flightCheck(Gun currentGun)
        {
            //ETGModConsole.Log("Flightchecked");
            PlayerController playerController = currentGun.CurrentOwner as PlayerController;
            if (currentGun == this.gun && !this.GaveFlight && playerController.PlayerHasActiveSynergy("Birdie!"))
            {
                base.Player.SetIsFlying(true, "PurpBirdie", false, false);
                base.Player.AdditionalCanDodgeRollWhileFlying.AddOverride("PurpBirdie", null);
                this.GaveFlight = true;
            }
            else
            {
                if ((currentGun != this.gun || !playerController.PlayerHasActiveSynergy("Birdie!")) && this.GaveFlight)
                {
                    base.Player.SetIsFlying(false, "PurpBirdie", false, false);
                    base.Player.AdditionalCanDodgeRollWhileFlying.RemoveOverride("PurpBirdie");
                    this.GaveFlight = false;

                }
            }
        }
        private bool GaveFlight;
        private bool hasBirdieNow;
        private bool hadBirdieLastWeChecked;
        public override void PostProcessProjectile(Projectile projectile)
        {
            base.PostProcessProjectile(projectile);
            PlayerController player = projectile.Owner as PlayerController;
            if (player)
            {
                if (player.PlayerHasActiveSynergy("Purplest"))
                {
                    projectile.baseData.range *= 3f;
                    projectile.baseData.damage *= 2f;
                }
            }
        }

        protected override void Update()
        {
            base.Update();
            PlayerController player = gun.CurrentOwner as PlayerController;
            hasBirdieNow = player.PlayerHasActiveSynergy("Birdie!");
            if (hasBirdieNow != hadBirdieLastWeChecked)
            {
                flightCheck(player.CurrentGun);
                hadBirdieLastWeChecked = hasBirdieNow;
            }
        }
        public override void OnReload(PlayerController player, Gun gun)
        {
            base.OnReload(player, gun);
            flightCheck(gun);
        }
        protected override void OnPickedUpByPlayer(PlayerController player)
        {
            //ETGModConsole.Log("OnPickup Was Triggered");
            base.OnPickedUpByPlayer(player);
            player.GunChanged += this.changedGun;
            flightCheck(player.CurrentGun);
        }
        protected override void OnPostDroppedByPlayer(PlayerController player)
        {
            player.GunChanged -= this.changedGun;
            flightCheck(player.CurrentGun);
            player.stats.RecalculateStats(player, true, false);
            base.OnPostDroppedByPlayer(player);
        }
        public Purpler()
        {

        }
    }
}
