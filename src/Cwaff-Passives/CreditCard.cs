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
using ItemAPI;
using Alexandria.Misc;

namespace CwaffingTheGungy
{
    public class CreditCard : PassiveItem
    {
        public static string passiveName      = "Credit Card";
        public static string spritePath       = "CwaffingTheGungy/Resources/ItemSprites/credit_card_icon";
        public static string shortDescription = "Shop 'til You Drop";
        public static string longDescription  = "(Grants 500 shells. Grants 1 curse for every 50 shells below 500. Grants 1 coolness for every 50 shells above 500.)";

        private const int BASE_CREDIT = 500;
        private const int CREDIT_DELTA = 50;

        private int oldCurrency = 0;
        private StatModifier curseMod = null;
        private StatModifier coolMod = null;

        public static void Init()
        {
            PickupObject item  = Lazy.SetupItem<CreditCard>(passiveName, spritePath, shortDescription, longDescription, "cg");
            item.quality       = PickupObject.ItemQuality.A;
        }

        public override void Pickup(PlayerController player)
        {
            if (!this.m_pickedUpThisRun)
            {
                this.curseMod = new StatModifier();
                    curseMod.amount = 0f;
                    curseMod.modifyType = StatModifier.ModifyMethod.ADDITIVE;
                    curseMod.statToBoost = PlayerStats.StatType.Curse;
                this.coolMod = new StatModifier();
                    coolMod.amount = 0f;
                    coolMod.modifyType = StatModifier.ModifyMethod.ADDITIVE;
                    coolMod.statToBoost = PlayerStats.StatType.Coolness;
                this.passiveStatModifiers = new []{curseMod, coolMod};
            }

            base.Pickup(player);
            oldCurrency = BASE_CREDIT;
            GameManager.Instance.PrimaryPlayer.carriedConsumables.Currency += BASE_CREDIT;
            UpdateCreditScore();
        }

        public override DebrisObject Drop(PlayerController player)
        {
            GameManager.Instance.PrimaryPlayer.carriedConsumables.Currency -= BASE_CREDIT;
            return base.Drop(player);
        }

        public override void Update()
        {
            base.Update();
            if (!this.Owner)
                return;
            UpdateCreditScore();
        }

        private void UpdateCreditScore()
        {
            int newCurrency = GameManager.Instance.PrimaryPlayer.carriedConsumables.Currency;
            if (oldCurrency == newCurrency)
                return;

            int newCurse = (newCurrency > BASE_CREDIT) ? 0 : ((BASE_CREDIT - newCurrency) / CREDIT_DELTA);
            int newCool  = (newCurrency < BASE_CREDIT) ? 0 : ((newCurrency - BASE_CREDIT) / CREDIT_DELTA);
            curseMod.amount = newCurse;
            coolMod.amount = newCool;
            this.Owner.stats.RecalculateStats(this.Owner);
            this.CanBeDropped = (newCurrency >= BASE_CREDIT);
        }
    }
}
