using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using UnityEngine;
using MonoMod.RuntimeDetour;

using Gungeon;
using Dungeonator;
using Alexandria.ItemAPI;
using Alexandria.Misc;

namespace CwaffingTheGungy
{
    public class WeddingRing : PassiveItem
    {
        public static string ItemName         = "Wedding Ring";
        public static string SpritePath       = "CwaffingTheGungy/Resources/ItemSprites/wedding_ring_icon";
        public static string ShortDescription = "Commitment";
        public static string LongDescription  = "(Every enemy killed without switching guns grants 1% boosts to damage, reload speed, and chance not to consume ammo, up to a maximum of 50% each; stats reset upon firing another gun)";

        private const float _BONUS_PER_KILL = 0.01f;

        private Gun            _committedGun    = null;
        private StatModifier[] _commitmentBuffs = null;
        private float          _commitmentMult  = 1.00f;
        private int            _lastKnownAmmo   = 0;
        private bool           _refundAmmo      = false;

        public static void Init()
        {
            PickupObject item  = Lazy.SetupPassive<WeddingRing>(ItemName, SpritePath, ShortDescription, LongDescription);
            item.quality       = PickupObject.ItemQuality.C;
        }

        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.OnPreFireProjectileModifier += this.ChanceToRefundAmmo;
            player.PostProcessProjectile += this.PostProcessProjectile;
            player.OnKilledEnemy += this.OnKilledEnemy;
            if (m_pickedUpThisRun)
                return;

            this.passiveStatModifiers = new StatModifier[] {
                new StatModifier {
                    amount      = 1.00f,
                    statToBoost = PlayerStats.StatType.ReloadSpeed,
                    modifyType  = StatModifier.ModifyMethod.MULTIPLICATIVE},
                new StatModifier {
                    amount      = 1.00f,
                    statToBoost = PlayerStats.StatType.Damage,
                    modifyType  = StatModifier.ModifyMethod.MULTIPLICATIVE},
                new StatModifier {
                    amount      = 1.00f,
                    statToBoost = PlayerStats.StatType.DamageToBosses,
                    modifyType  = StatModifier.ModifyMethod.MULTIPLICATIVE},
            };
        }

        public override DebrisObject Drop(PlayerController player)
        {
            player.OnKilledEnemy -= this.OnKilledEnemy;
            player.PostProcessProjectile -= this.PostProcessProjectile;
            player.OnPreFireProjectileModifier -= this.ChanceToRefundAmmo;
            UpdateCommitmentStats(player, reset: true);
            return base.Drop(player);
        }

        private void UpdateCommitmentStats(PlayerController player, bool reset = false)
        {
            this._commitmentMult = reset ? 1.00f : (this._commitmentMult + _BONUS_PER_KILL);
            foreach (StatModifier stat in this.passiveStatModifiers)
                stat.amount = this._commitmentMult;
            player.stats.RecalculateStats(player);
        }

        private void OnKilledEnemy(PlayerController player)
        {
            UpdateCommitmentStats(player);
        }

        private Projectile ChanceToRefundAmmo(Gun gun, Projectile projectile)
        {
            this._refundAmmo    = UnityEngine.Random.value < (this._commitmentMult - 1.00f);
            this._lastKnownAmmo = (this.Owner as PlayerController).CurrentGun.CurrentAmmo;
            return projectile;
        }

        private void PostProcessProjectile(Projectile proj, float effectChanceScalar)
        {
            if (this.Owner is not PlayerController player)
                return;
            if (player.CurrentGun == this._committedGun)
                return;

            UpdateCommitmentStats(player, reset: true);
            this._committedGun = player.CurrentGun;
            this._refundAmmo = false;
        }

        private void LateUpdate()
        {
            if (!this._refundAmmo)
                return;

            this._refundAmmo = false;
            this._committedGun.CurrentAmmo = this._lastKnownAmmo;
        }
    }
}