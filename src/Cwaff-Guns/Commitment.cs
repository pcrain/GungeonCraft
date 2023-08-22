using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

using UnityEngine;

using Gungeon;
using Alexandria.ItemAPI;

/*
    Need to prevent:
        - dodge rolling
        - stopping firing

        m_player.inventory.GunLocked.SetOverride("spren gun", true);
*/

namespace CwaffingTheGungy
{
    public class Commitment : AdvancedGunBehavior
    {
        public static string GunName          = "Commitment";
        public static string SpriteName       = "g20";
        public static string ProjectileName   = "86"; //marine sidearm
        public static string ShortDescription = "Going Until It's Gone";
        public static string LongDescription  = "(cannot switch weapons or stop firing until out of ammo)";

        private bool committed = false;

        public static void Add()
        {
            Gun gun = Lazy.InitGunFromStrings(GunName, SpriteName, ProjectileName, ShortDescription, LongDescription);
            var comp = gun.gameObject.AddComponent<Commitment>();
            // comp.preventNormalFireAudio = true;

            gun.gunSwitchGroup                    = (PickupObjectDatabase.GetById(198) as Gun).gunSwitchGroup;
            gun.DefaultModule.ammoCost            = 1;
            gun.DefaultModule.shootStyle          = ProjectileModule.ShootStyle.Automatic;
            gun.DefaultModule.sequenceStyle       = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime                        = 1.1f;
            gun.DefaultModule.angleVariance       = 15.0f;
            gun.DefaultModule.cooldownTime        = 0.15f;
            gun.DefaultModule.numberOfShotsInClip = 10000;
            gun.CanBeDropped                      = false;
            gun.CanGainAmmo                       = false;
            gun.quality                           = PickupObject.ItemQuality.C;
            gun.SetBaseMaxAmmo(500);
            gun.SetAnimationFPS(gun.shootAnimation, 24);

            Projectile projectile       = Lazy.PrefabProjectileFromGun(gun);
            projectile.baseData.damage  = 16f;
            projectile.baseData.speed   = 24.0f;
            projectile.transform.parent = gun.barrelOffset;
        }

        public override void OnPostFired(PlayerController player, Gun gun)
        {
            base.OnPostFired(player, gun);
            // if (gun.ammo <= 1)
            if (gun.ammo <= 0)
            {
                if (this.committed)
                    ETGModConsole.Log("Uncommitted!");
                // player.inventory.DestroyCurrentGun();
                this.committed                = false;
                // player.m_preventItemSwitching = false;
                player.forceFireDown          = false;
                player.forceFireUp            = true;
            }
            else
            {
                if (!this.committed)
                    ETGModConsole.Log("Committed!");
                this.committed                = true;
                // player.m_preventItemSwitching = true;
                player.forceFireUp            = false;
                player.forceFireDown          = true;
            }
        }

        protected override void Update()
        {
            base.Update();
            if (!this.Player)
                return;
            bool forceOn             = this.committed && (this.gun.CurrentAmmo > 0);
            this.Player.m_preventItemSwitching = forceOn;
            this.Player.forceFireDown          = forceOn;

            // p.forceFire = true;
            // p.m_handleDodgeRollStartThisFrame = false;
            // p.m_disableInput = new OverridableBool(true);
            // p.m_inputState   = PlayerInputState.OnlyMovement;
            // p.m_dodgeRollTimer = 100f;
            // if (p.IsDodgeRolling)
            // {
            //     p.ForceStopDodgeRoll();
            //     // p.ClearDodgeRollState();
            // }
        }

        protected override void OnPostDroppedByPlayer(PlayerController player)
        {
            if (this.committed)
            {
                this.committed = false;
                player.forceFireDown = false;
            }
            base.OnPostDroppedByPlayer(player);
            ETGModConsole.Log("Dropped gun");
        }

        public override void OnSwitchedAwayFromThisGun()
        {
            if (this.committed && this.Player)
            {
                while (this.Player.inventory.CurrentGun.PickupObjectId != this.gun.PickupObjectId)
                    this.Player.inventory.ChangeGun(1, false, false);
                ETGModConsole.Log("Forcing gun back to Commitment");
            }
            base.OnSwitchedAwayFromThisGun();
        }
    }
}
