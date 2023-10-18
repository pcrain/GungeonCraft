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
    public class JohnsWick : PassiveItem
    {
        public static string ItemName         = "John's Wick";
        public static string SpritePath       = "johns_wick_icon";
        public static string ShortDescription = "No Dogs Harmed";
        public static string LongDescription  = "Move faster and deal double damage while on fire; take damage from fire more slowly.\n\nAccording to Bello, the wick inside this lantern was once possessed by a man who survived dozens of assassination attempts en route to grabbing breakfast at a hotel. This raises far more questions than it answers, and Bello refuses to elaborate further.";

        private const float _FIRE_TIMER_MULT = 0.25f;
        private const float _MOVEMENT_BOOST  = 5f;
        private const float _DAMAGE_BOOST    = 2f;

        private bool                 _wasOnFire          = false;
        private StatModifier[]       _flameOn            = null;
        private StatModifier[]       _flameOff           = null;
        private DamageTypeModifier   _fireResistance     = null;

        public static void Init()
        {
            PickupObject item  = Lazy.SetupPassive<JohnsWick>(ItemName, SpritePath, ShortDescription, LongDescription);
            item.quality       = PickupObject.ItemQuality.C;
        }

        private void OnFirstPickup()
        {
            this._flameOff = new StatModifier[]{};
            StatModifier s1 = new StatModifier {
                amount      = _MOVEMENT_BOOST,
                statToBoost = PlayerStats.StatType.MovementSpeed,
                modifyType  = StatModifier.ModifyMethod.ADDITIVE };
            StatModifier s2 = new StatModifier {
                amount      = _DAMAGE_BOOST,
                statToBoost = PlayerStats.StatType.Damage,
                modifyType  = StatModifier.ModifyMethod.MULTIPLICATIVE };
            this._flameOn = (new StatModifier[] { s1, s2 }).ToArray();
            this._wasOnFire = false;
            this._fireResistance = new DamageTypeModifier {
                damageType = CoreDamageTypes.Fire,
                damageMultiplier = _FIRE_TIMER_MULT,
            };
        }

        public override void Pickup(PlayerController player)
        {
            if (!this.m_pickedUpThisRun)
                OnFirstPickup();
            base.Pickup(player);

            this.passiveStatModifiers = _flameOff;
            player.PostProcessProjectile += this.PostProcessProjectile;
            if (!player.healthHaver.damageTypeModifiers.Contains(this._fireResistance))
                player.healthHaver.damageTypeModifiers.Add(this._fireResistance);
        }

        public override DebrisObject Drop(PlayerController player)
        {
            player.PostProcessProjectile -= this.PostProcessProjectile;
            if (player.healthHaver.damageTypeModifiers.Contains(this._fireResistance))
                player.healthHaver.damageTypeModifiers.Remove(this._fireResistance);
            return base.Drop(player);
        }

        private void PostProcessProjectile(Projectile proj, float effectChanceScalar)
        {
            if (!(this.Owner && this._wasOnFire))
                return;
            proj.StartCoroutine(GetWicked(proj.specRigidbody));
        }

        private IEnumerator GetWicked(SpeculativeRigidbody s, bool once = false)
        {
            const int   NUM                = 1;
            const float ANGLE_VARIANCE     = 15f;
            const float BASE_MAGNITUDE     = 2.25f;
            const float MAGNITUDE_VARIANCE = 1f;
            Color? startColor              = Color.blue;
            while (s)
            {
                Vector3 minPosition = s.HitboxPixelCollider.UnitBottomLeft.ToVector3ZisY();
                Vector3 maxPosition = s.HitboxPixelCollider.UnitTopRight.ToVector3ZisY();
                GlobalSparksDoer.DoRadialParticleBurst(
                  NUM, minPosition, maxPosition, ANGLE_VARIANCE, BASE_MAGNITUDE, MAGNITUDE_VARIANCE,
                  startColor: startColor,
                  startLifetime: 0.5f,
                  systemType: GlobalSparksDoer.SparksType.STRAIGHT_UP_GREEN_FIRE/*EMBERS_SWIRLING*/
                  );
                if (once)
                    yield break;
                yield return null;
            }
        }

        public override void Update()
        {
            base.Update();

            if (!this.Owner)
                return;

            if (this._wasOnFire != this.Owner.IsOnFire)
            {
                this._wasOnFire           = this.Owner.IsOnFire;
                this.passiveStatModifiers = this.Owner.IsOnFire ? _flameOn : _flameOff;
                this.Owner.stats.RecalculateStats(this.Owner, false, false);
            }
            if (this.Owner.IsOnFire)
                this.Owner.StartCoroutine(GetWicked(this.Owner.specRigidbody, once: true));
        }
    }
}
