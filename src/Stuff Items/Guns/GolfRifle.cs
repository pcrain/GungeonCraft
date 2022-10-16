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

    public class GolfRifle : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Golf Rifle", "golfrifle");
            Game.Items.Rename("outdated_gun_mods:golf_rifle", "nn:golf_rifle");
            gun.gameObject.AddComponent<GolfRifle>();
            gun.SetShortDescription("Bullet Hole In 1");
            gun.SetLongDescription("Golf is a popular game among the Gundead of the Keep, though it's rules are very peculiar to outsiders." + "\n\nThere's a lot more violence involved.");

            gun.SetupSprite(null, "golfrifle_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 15);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.DefaultModule.cooldownTime = 0.3f;
            gun.DefaultModule.numberOfShotsInClip = 8;
            gun.barrelOffset.transform.localPosition = new Vector3(3.0f, 0.43f, 0f);
            gun.SetBaseMaxAmmo(200);
            gun.gunClass = GunClass.RIFLE;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage *= 1.2f;
            PierceProjModifier Piercing = projectile.gameObject.GetOrAddComponent<PierceProjModifier>();
            Piercing.penetratesBreakables = true;
            Piercing.penetration += 30;
            BounceProjModifier Bouncing = projectile.gameObject.GetOrAddComponent<BounceProjModifier>();
            Bouncing.numberOfBounces = 30;
            projectile.baseData.range *= 60f;
            projectile.transform.parent = gun.barrelOffset;
            projectile.SetProjectileSpriteRight("golfrifle_projectile", 7, 7, false, tk2dBaseSprite.Anchor.MiddleCenter, 6, 6);

            gun.quality = PickupObject.ItemQuality.C;
            gun.encounterTrackable.EncounterGuid = "this is the Golf Rifle";
            ETGMod.Databases.Items.Add(gun, null, "ANY");
            GolfRifleID = gun.PickupObjectId;
        }
        public static int GolfRifleID;

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
                base.Player.SetIsFlying(true, "GolfBirdie", false, false);
                base.Player.AdditionalCanDodgeRollWhileFlying.AddOverride("GolfBirdie", null);
                this.GaveFlight = true;
            }
            else
            {
                if ((currentGun != this.gun || !playerController.PlayerHasActiveSynergy("Birdie!")) && this.GaveFlight)
                {
                    base.Player.SetIsFlying(false, "GolfBirdie", false, false);
                    base.Player.AdditionalCanDodgeRollWhileFlying.RemoveOverride("GolfBirdie");
                    this.GaveFlight = false;
                    
                }
            }
        }
        private bool GaveFlight;
        private bool hasBirdieNow;
        private bool hadBirdieLastWeChecked;

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
        public GolfRifle()
        {

        }
    }
}
