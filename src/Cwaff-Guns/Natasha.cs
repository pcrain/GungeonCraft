using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using UnityEngine;

using Gungeon;
using Alexandria.ItemAPI;

namespace CwaffingTheGungy
{
    public class Natasha : AdvancedGunBehavior
    {
        public static string GunName          = "Natasha";
        public static string SpriteName       = "accelerator";
        public static string ProjectileName   = "ak-47";
        public static string ShortDescription = "Fear no Man";
        public static string LongDescription  = "(Gets more powerful the longer you fire, but you slow down as well.)";

        private static float baseCooldownTime = 0.4f;
        private float speedmult               = 1.0f;

        public static void Add()
        {
            Gun gun = Lazy.InitGunFromStrings(GunName, SpriteName, ProjectileName, ShortDescription, LongDescription);
            var comp = gun.gameObject.AddComponent<Natasha>();

            gun.gunSwitchGroup                    = (PickupObjectDatabase.GetById(198) as Gun).gunSwitchGroup;
            gun.DefaultModule.ammoCost            = 1;
            gun.DefaultModule.shootStyle          = ProjectileModule.ShootStyle.Automatic;
            gun.DefaultModule.sequenceStyle       = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime                        = 1.1f;
            gun.DefaultModule.angleVariance       = 15.0f;
            gun.DefaultModule.cooldownTime        = baseCooldownTime;
            gun.DefaultModule.numberOfShotsInClip = 1000;
            gun.quality                           = PickupObject.ItemQuality.D;
            gun.SetBaseMaxAmmo(1000);
            gun.SetAnimationFPS(gun.shootAnimation, 24);

            Projectile projectile       = Lazy.PrefabProjectileFromGun(gun);
            projectile.baseData.damage  = 3f;
            projectile.baseData.speed   = 20.0f;
            projectile.transform.parent = gun.barrelOffset;

            projectile.gameObject.AddComponent<NatashaBullets>();
        }

        public override void OnPostFired(PlayerController player, Gun gun)
        {
            base.OnPostFired(player, gun);
            if (this.speedmult > 0.15f)
            {
                this.speedmult *= 0.85f;
                this.RecalculateGunStats();
            }
        }

        public override void OnFinishAttack(PlayerController player, Gun gun)
        {
            this.speedmult = 1.0f;
            this.RecalculateGunStats();
        }

        protected override void OnPickedUpByPlayer(PlayerController player)
        {
            base.OnPickedUpByPlayer(player);
            player.OnRollStarted += this.OnDodgeRoll;
        }
        protected override void OnPostDroppedByPlayer(PlayerController player)
        {
            base.OnPostDroppedByPlayer(player);
            player.OnRollStarted -= this.OnDodgeRoll;
        }

        private void OnDodgeRoll(PlayerController player, Vector2 dirVec)
        {
            this.speedmult = 1.0f;
            this.RecalculateGunStats();
        }

        private void RecalculateGunStats()
        {
            if (!this.Player)
                return;
            this.gun.RemoveStatFromGun(PlayerStats.StatType.MovementSpeed);
            this.gun.AddStatToGun(PlayerStats.StatType.MovementSpeed, (float)Math.Sqrt(this.speedmult), StatModifier.ModifyMethod.MULTIPLICATIVE);
            this.Player.stats.RecalculateStats(this.Player); // TODO: this resets the gun's cooldown time??? need it first for now
            this.gun.DefaultModule.cooldownTime = this.speedmult * baseCooldownTime;
        }
    }

    public class NatashaBullets : MonoBehaviour
    {
        private const float NATASHA_PROJECTILE_SCALE = 0.5f;
        private void Start()
        {
            Projectile self = base.GetComponent<Projectile>();
            if (self?.Owner is PlayerController)
            {
                PlayerController owner = self.Owner as PlayerController;
                self.RuntimeUpdateScale(NATASHA_PROJECTILE_SCALE * owner.stats.GetStatValue(PlayerStats.StatType.PlayerBulletScale));
            }
        }
    }
}
