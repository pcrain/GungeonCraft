using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using ItemAPI;
using SaveAPI;

namespace NevernamedsItems
{
    public class ChaosRuby : PassiveItem
    {
        public static void Init()
        {
            //The name of the item
            string itemName = "Chaos Ruby";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it
            string resourceName = "NevernamedsItems/Resources/chaosruby_icon";
            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a PassiveItem component to the object
            var item = obj.AddComponent<ChaosRuby>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "(Un)Control";
            string longDesc = "Increases one random stat by 20%, at least in most cases."+"\n\nRumour has it that these gemstones are what drew Tonic the Sledge Dog to The Gungeon in the first place.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //Do this after ItemBuilder.AddSpriteToObject!
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Adds the actual passive effect to the item

            //Set the rarity of the item
            item.quality = PickupObject.ItemQuality.D;
            item.SetupUnlockOnCustomFlag(CustomDungeonFlags.BEATEN_KEEP_TURBO_MODE, true);

        }
        bool hasPicked = false;
        public int randomNumber;
        public string randomNumberToString;
        private void PickStats()
        {
            if (hasPicked) return;
            else
            {
                PlayableCharacters characterIdentity = Owner.characterIdentity;
                Effect effect = BraveUtility.RandomElement(statEffects);
                statEffects.Remove(effect);
                if (effect.modifyMethod == StatModifier.ModifyMethod.MULTIPLICATIVE)
                {
                    AddStat(effect.statToEffect, effect.amount, StatModifier.ModifyMethod.MULTIPLICATIVE);
                }
                else
                {
                    if (effect.statToEffect == PlayerStats.StatType.Health && Owner.ForceZeroHealthState)
                    {
                        LootEngine.GivePrefabToPlayer(PickupObjectDatabase.GetById(120).gameObject, Owner);
                    }
                    AddStat(effect.statToEffect, effect.amount, StatModifier.ModifyMethod.ADDITIVE);
                }
                Owner.stats.RecalculateStats(Owner, true, false);
                hasPicked = true;
            }
        }
        public List<Effect> statEffects = new List<Effect>();
        public struct Effect
        {
            public PlayerStats.StatType statToEffect;
            public float amount;
            public StatModifier.ModifyMethod modifyMethod;
            public Action<Effect, PlayerController> action;
        }
        public void DefineEffects()
        {
            statEffects.Add(new Effect()
            {
                statToEffect = PlayerStats.StatType.Accuracy,
                modifyMethod = StatModifier.ModifyMethod.MULTIPLICATIVE,
                amount = 0.80f,
            });
            statEffects.Add(new Effect()
            {
                statToEffect = PlayerStats.StatType.ReloadSpeed,
                modifyMethod = StatModifier.ModifyMethod.MULTIPLICATIVE,
                amount = 0.80f,
            });
            statEffects.Add(new Effect()
            {
                statToEffect = PlayerStats.StatType.Damage,
                modifyMethod = StatModifier.ModifyMethod.MULTIPLICATIVE,
                amount = 1.20f,
            });
            statEffects.Add(new Effect()
            {
                statToEffect = PlayerStats.StatType.PlayerBulletScale,
                modifyMethod = StatModifier.ModifyMethod.MULTIPLICATIVE,
                amount = 1.20f,
            });
            statEffects.Add(new Effect()
            {
                statToEffect = PlayerStats.StatType.ProjectileSpeed,
                modifyMethod = StatModifier.ModifyMethod.MULTIPLICATIVE,
                amount = 1.20f,
            });
            statEffects.Add(new Effect()
            {
                statToEffect = PlayerStats.StatType.RateOfFire,
                modifyMethod = StatModifier.ModifyMethod.MULTIPLICATIVE,
                amount = 1.20f,
            });
            statEffects.Add(new Effect()
            {
                statToEffect = PlayerStats.StatType.DamageToBosses,
                modifyMethod = StatModifier.ModifyMethod.MULTIPLICATIVE,
                amount = 1.20f,
            });
            statEffects.Add(new Effect()
            {
                statToEffect = PlayerStats.StatType.DodgeRollDamage,
                modifyMethod = StatModifier.ModifyMethod.MULTIPLICATIVE,
                amount = 1.20f,
            });
            statEffects.Add(new Effect()
            {
                statToEffect = PlayerStats.StatType.DodgeRollDistanceMultiplier,
                modifyMethod = StatModifier.ModifyMethod.MULTIPLICATIVE,
                amount = 1.20f,
            });
            statEffects.Add(new Effect()
            {
                statToEffect = PlayerStats.StatType.DodgeRollSpeedMultiplier,
                modifyMethod = StatModifier.ModifyMethod.MULTIPLICATIVE,
                amount = 1.20f,
            });
            statEffects.Add(new Effect()
            {
                statToEffect = PlayerStats.StatType.GlobalPriceMultiplier,
                modifyMethod = StatModifier.ModifyMethod.MULTIPLICATIVE,
                amount = 0.8f,
            });
            statEffects.Add(new Effect()
            {
                statToEffect = PlayerStats.StatType.KnockbackMultiplier,
                modifyMethod = StatModifier.ModifyMethod.MULTIPLICATIVE,
                amount = 1.20f,
            });
            statEffects.Add(new Effect()
            {
                statToEffect = PlayerStats.StatType.MoneyMultiplierFromEnemies,
                modifyMethod = StatModifier.ModifyMethod.MULTIPLICATIVE,
                amount = 1.20f,
            });
            statEffects.Add(new Effect()
            {
                statToEffect = PlayerStats.StatType.ThrownGunDamage,
                modifyMethod = StatModifier.ModifyMethod.MULTIPLICATIVE,
                amount = 1.20f,
            });
            statEffects.Add(new Effect()
            {
                statToEffect = PlayerStats.StatType.MovementSpeed,
                modifyMethod = StatModifier.ModifyMethod.MULTIPLICATIVE,
                amount = 1.20f,
            });
            statEffects.Add(new Effect()
            {
                statToEffect = PlayerStats.StatType.Health,
                modifyMethod = StatModifier.ModifyMethod.ADDITIVE,
                amount = 1f,
            });
            statEffects.Add(new Effect()
            {
                statToEffect = PlayerStats.StatType.Coolness,
                modifyMethod = StatModifier.ModifyMethod.ADDITIVE,
                amount = 1f,
            });
        }
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            DefineEffects();
            PickStats();
        }
        private void AddStat(PlayerStats.StatType statType, float amount, StatModifier.ModifyMethod method = StatModifier.ModifyMethod.ADDITIVE)
        {
            /*foreach (var m in passiveStatModifiers)
            {
                if (m.statToBoost == statType) return; //don't add duplicates
            }*/

            StatModifier modifier = new StatModifier
            {
                amount = amount,
                statToBoost = statType,
                modifyType = method
            };

            if (this.passiveStatModifiers == null)
                this.passiveStatModifiers = new StatModifier[] { modifier };
            else
                this.passiveStatModifiers = this.passiveStatModifiers.Concat(new StatModifier[] { modifier }).ToArray();
        }

        private void RemoveStat(PlayerStats.StatType statType)
        {
            var newModifiers = new List<StatModifier>();
            for (int i = 0; i < passiveStatModifiers.Length; i++)
            {
                if (passiveStatModifiers[i].statToBoost != statType)
                    newModifiers.Add(passiveStatModifiers[i]);
            }
            this.passiveStatModifiers = newModifiers.ToArray();
        }
    }
}
