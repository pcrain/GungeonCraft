using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Reflection;

using UnityEngine;
using MonoMod.RuntimeDetour;

using ItemAPI;
using Gungeon;
using Dungeonator;

using Alexandria.Misc;

namespace CwaffingTheGungy
{
    public class JohnsWick : PassiveItem
    {
        public static string passiveName      = "John's Wick";
        public static string spritePath       = "CwaffingTheGungy/Resources/ItemSprites/johns_wick_icon";
        public static string shortDescription = "No Dogs Harmed";
        public static string longDescription  = "(Move faster and do triple damage while on fire; take damage from fire more slowly.)";

        private PlayerController owner;
        private float lastFireMeterValue = 0f;
        private const float MAX_FIRE_INCREASE = 0.166f; // base game increases by 0.66f

        private bool flaming = false;
        private StatModifier[] flameOn = null;
        private StatModifier[] flameOff = null;

        public static void Init()
        {
            PickupObject item  = Lazy.SetupItem<JohnsWick>(passiveName, spritePath, shortDescription, longDescription, "cg");
            item.quality       = PickupObject.ItemQuality.C;
        }

        public override void Pickup(PlayerController player)
        {
            if (flameOff == null)
                flameOff = (new StatModifier[] {}).ToArray();
            if (flameOn == null)
            {
                StatModifier s1 = new StatModifier {
                    amount      = 5f,
                    statToBoost = PlayerStats.StatType.MovementSpeed,
                    modifyType  = StatModifier.ModifyMethod.ADDITIVE };
                StatModifier s2 = new StatModifier {
                    amount      = 3f,
                    statToBoost = PlayerStats.StatType.Damage,
                    modifyType  = StatModifier.ModifyMethod.MULTIPLICATIVE };
                flameOn = (new StatModifier[] { s1, s2 }).ToArray();
            }
            this.flaming = false;
            this.passiveStatModifiers = flameOff;
            this.owner = player;
            this.owner.PostProcessProjectile += this.PostProcessProjectile;
            lastFireMeterValue = this.owner.CurrentFireMeterValue;
            base.Pickup(player);
        }

        public override DebrisObject Drop(PlayerController player)
        {
            this.owner = null;
            this.owner.PostProcessProjectile -= this.PostProcessProjectile;
            return base.Drop(player);
        }

        private void PostProcessProjectile(Projectile proj, float effectChanceScalar)
        {
            if (this.owner == null || (!this.flaming))
                return;
            proj.StartCoroutine(GetWickedCR(proj.specRigidbody));
        }

        private IEnumerator GetWickedCR(SpeculativeRigidbody s)
        {
            while (s)
            {
                GetWicked(s);
                yield return null;
            }
        }

        private void GetWicked(SpeculativeRigidbody s)
        {
            int num = 1;
            Vector3 minPosition = s.HitboxPixelCollider.UnitBottomLeft.ToVector3ZisY();
            Vector3 maxPosition = s.HitboxPixelCollider.UnitTopRight.ToVector3ZisY();
            float angleVariance = 15f;
            float baseMagnitude = 2.25f;
            float magnitudeVariance = 1f;
            Color? startColor = Color.blue;
            GlobalSparksDoer.DoRadialParticleBurst(
              num, minPosition, maxPosition, angleVariance, baseMagnitude, magnitudeVariance,
              startColor: startColor,
              startLifetime: 0.5f,
              systemType: GlobalSparksDoer.SparksType.STRAIGHT_UP_GREEN_FIRE/*EMBERS_SWIRLING*/);
        }

        public override void Update()
        {
            base.Update();

            if (!this.owner)
                return;

            float maxCurrentFireMeterValue = lastFireMeterValue + (MAX_FIRE_INCREASE * BraveTime.DeltaTime);
            this.owner.CurrentFireMeterValue = Mathf.Min(this.owner.CurrentFireMeterValue,maxCurrentFireMeterValue);
            lastFireMeterValue = this.owner.CurrentFireMeterValue;

            if (!this.owner.IsOnFire)
            {
                if (this.flaming)
                {
                    this.flaming = false;
                    this.passiveStatModifiers = flameOff;
                    this.owner.stats.RecalculateStats(this.owner, false, false);
                }
                return;
            }
            if (!this.flaming)
            {
                this.flaming = true;
                this.passiveStatModifiers = flameOn;
                this.owner.stats.RecalculateStats(this.owner, false, false);
            }
            GetWicked(this.owner.specRigidbody);
        }
    }
}
