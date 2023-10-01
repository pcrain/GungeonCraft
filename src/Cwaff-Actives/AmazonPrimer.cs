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
    class AmazonPrimer : PlayerItem
    {
        public static string ItemName         = "Amazon Primer";
        public static string SpritePath       = "CwaffingTheGungy/Resources/ItemSprites/amazon_primer_icon";
        public static string ShortDescription = "Cancel Any* Time!";
        public static string LongDescription  = "Begins a Primer subscription when consumed. Subscription drains 5 casings per combat encounter, but doubles fire rate and projectile speed and slightly boosts damage. Per-room subscription cost increases by 5 casings each floor.\n\nOnce upon a time, money couldn't buy firearm proficiency, and gun-toting peasants needed to practice things often described using buzzwords such as \"aiming,\" \"timing,\" and \"strategy\". Fortunately, the stone ages are behind us, and for a low** fee, you too can join the dozens of happy*** Primers and shoot with the best of them!\n\n*(any time you run out of money, your subscription will be automatically cancelled for your convenience)\n\n**(low fee increases as you descend lower into the Gungeon)\n\n***(happiness is subjective and relative)";

        public static void Init()
        {
            PlayerItem item = Lazy.SetupActive<AmazonPrimer>(ItemName, SpritePath, ShortDescription, LongDescription);
            item.quality      = PickupObject.ItemQuality.B;
            item.consumable   = true;
            item.CanBeDropped = true;
        }

        public override void DoEffect(PlayerController user)
        {
            user.gameObject.GetOrAddComponent<PrimerSubscription>();
        }
    }

    public class PrimerSubscription : MonoBehaviour
    {
        internal const int _PRIME_SUB_COST  = 5;
        internal const int _FLOOR_INFLATION = 5;

        private PlayerController _primer        = null;
        private StatModifier[]   _primeBenefits = null;
        private int              _currentCost   = _PRIME_SUB_COST;

        private void Start()
        {
            this._primer = base.gameObject.GetComponent<PlayerController>();
            this._primer.OnEnteredCombat += AnyPrimers;
            this._primer.OnRoomClearEvent += ThanksForPriming;
            GameManager.Instance.OnNewLevelFullyLoaded += Inflation;
            this._primeBenefits = new[] {
                new StatModifier(){
                    amount      = 2.00f,
                    modifyType  = StatModifier.ModifyMethod.MULTIPLICATIVE,
                    statToBoost = PlayerStats.StatType.RateOfFire,
                },
                new StatModifier(){
                    amount      = 2.00f,
                    modifyType  = StatModifier.ModifyMethod.MULTIPLICATIVE,
                    statToBoost = PlayerStats.StatType.ProjectileSpeed,
                },
                new StatModifier(){
                    amount      = 1.25f,
                    modifyType  = StatModifier.ModifyMethod.MULTIPLICATIVE,
                    statToBoost = PlayerStats.StatType.Damage,
                },
                new StatModifier(){
                    amount      = 1.25f,
                    modifyType  = StatModifier.ModifyMethod.MULTIPLICATIVE,
                    statToBoost = PlayerStats.StatType.DamageToBosses,
                },
            };
            DoPrimeVFX();
        }

        private void Inflation()
        {
            this._currentCost += _FLOOR_INFLATION;
        }

        private void AnyPrimers()
        {
            if (GameManager.Instance.PrimaryPlayer.carriedConsumables.Currency < this._currentCost)
            {
                this._primer.OnEnteredCombat -= AnyPrimers;
                this._primer.OnRoomClearEvent -= ThanksForPriming;
                GameManager.Instance.OnNewLevelFullyLoaded -= Inflation;
                AkSoundEngine.PostEvent("prime_ran_out", this._primer.gameObject);
                Lazy.CustomNotification("Primer Expired", "Thanks for Trying Amazon Primer", ItemHelper.Get((Items)IDs.Pickups["amazon_primer"]).sprite);
                UnityEngine.Object.Destroy(this);
                return;
            }

            GameManager.Instance.PrimaryPlayer.carriedConsumables.Currency -= this._currentCost;
            foreach (StatModifier stat in this._primeBenefits)
                this._primer.ownerlessStatModifiers.Add(stat);
            this._primer.stats.RecalculateStats(this._primer);
            DoPrimeVFX();
        }

        private void ThanksForPriming(PlayerController player)
        {
            if (player != this._primer)
                return;
            foreach (StatModifier stat in this._primeBenefits)
                this._primer.ownerlessStatModifiers.Remove(stat);
            this._primer.stats.RecalculateStats(this._primer);
        }

        private void DoPrimeVFX()
        {
            AkSoundEngine.PostEvent("prime_sound", this._primer.gameObject);
            GameObject v = SpawnManager.SpawnVFX(VFX.animations["PrimeLogo"], this._primer.sprite.WorldTopCenter + new Vector2(0f, 0.5f), Quaternion.identity);
                v.transform.parent = this._primer.transform;
                v.ExpireIn(1f);
        }
    }
}
