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
    public class Blazer : PassiveItem
    {
        public static string ItemName         = "Blazer";
        public static string SpritePath       = "CwaffingTheGungy/Resources/ItemSprites/blazer_icon";
        public static string ShortDescription = "Get 'em While It's Hot";
        public static string LongDescription  = "(Fire rate, charge time, and reload speed are doubled for 3 seconds upon entering combat.)";

        internal const  float          _BOOST_TIME = 3f;
        internal static StatModifier[] _Boosts     = null;
        internal static StatModifier[] _NoBoosts   = new StatModifier[]{};

        private bool _boostedEntrance = false;

        public static void Init()
        {
            PickupObject item = Lazy.SetupPassive<Blazer>(ItemName, SpritePath, ShortDescription, LongDescription);
            item.quality      = PickupObject.ItemQuality.A;

            _Boosts = new[] {
                new StatModifier(){
                    amount      = 2.00f,
                    modifyType  = StatModifier.ModifyMethod.MULTIPLICATIVE,
                    statToBoost = PlayerStats.StatType.RateOfFire,
                },
                new StatModifier(){
                    amount      = 0.50f,
                    modifyType  = StatModifier.ModifyMethod.MULTIPLICATIVE,
                    statToBoost = PlayerStats.StatType.ReloadSpeed,
                },
                new StatModifier(){
                    amount      = 2.00f,
                    modifyType  = StatModifier.ModifyMethod.MULTIPLICATIVE,
                    statToBoost = PlayerStats.StatType.ChargeAmountMultiplier,
                },
            };

        }

        public override void Pickup(PlayerController player)
        {
            this.passiveStatModifiers = _NoBoosts;
            base.Pickup(player);
            player.OnEnteredCombat += this.OnEnteredCombat;
        }

        public override DebrisObject Drop(PlayerController player)
        {
            player.OnEnteredCombat -= this.OnEnteredCombat;
            RemoveBoosts(player);
            return base.Drop(player);
        }

        private void OnEnteredCombat()
        {
            if (this._boostedEntrance)
                return;
            BoostStats();
        }

        private void BoostStats()
        {
            this._boostedEntrance = true;
            this.passiveStatModifiers = _Boosts;
            this.Owner.stats.RecalculateStats(this.Owner);
            this.Owner.StartCoroutine(RemoveBoosts_CR());
        }

        private IEnumerator RemoveBoosts_CR()
        {
            yield return new WaitForSeconds(_BOOST_TIME);
            if (this.Owner)
                RemoveBoosts(this.Owner);
        }

        private void RemoveBoosts(PlayerController pc)
        {
            if (this.Owner != pc || !this._boostedEntrance)
                return;

            this.passiveStatModifiers = _NoBoosts;
            this.Owner.stats.RecalculateStats(this.Owner);
            this._boostedEntrance = false;
        }
    }
}