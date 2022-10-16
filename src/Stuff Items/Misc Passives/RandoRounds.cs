using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Alexandria.ItemAPI;
using SaveAPI;

namespace NevernamedsItems
{
    public class RandoRounds : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Rando Rounds";
            string resourceName = "NevernamedsItems/Resources/rando6_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<RandoRounds>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Something Up";
            string longDesc = "Increases two random bullet related stats by 15%."+"\n\nThese shells were hand-crafted by Chancelot, the disgraced Ex-Knight of the Octagonal Table."+"\n\nOne of the order's most popular members, he was cast out after being caught in Princess Gunivere's chambers.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.C;
            item.SetTag("bullet_modifier");
            item.SetupUnlockOnCustomFlag(CustomDungeonFlags.PURCHASED_RANDOROUNDS, true);
            item.AddItemToDougMetaShop(19);
        }
        bool hasPicked = false;
        public int randomNumber;
        public string randomNumberToString;
        private void PickStats()
        {
            if (hasPicked) return;
            else
            {
                for (int i = 0; i < 2; i++)
                {
                    Effect effect = BraveUtility.RandomElement(statEffects);
                    statEffects.Remove(effect);
                    if (effect.modifyMethod == StatModifier.ModifyMethod.MULTIPLICATIVE)
                    {
                        AddStat(effect.statToEffect, effect.amount, StatModifier.ModifyMethod.MULTIPLICATIVE);
                    }
                    else
                    {
                        AddStat(effect.statToEffect, effect.amount, StatModifier.ModifyMethod.ADDITIVE);
                    }
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
                amount = .85f,
            });
            statEffects.Add(new Effect()
            {
                statToEffect = PlayerStats.StatType.ReloadSpeed,
                modifyMethod = StatModifier.ModifyMethod.MULTIPLICATIVE,
                amount = 0.85f,
            });
            statEffects.Add(new Effect()
            {
                statToEffect = PlayerStats.StatType.Damage,
                modifyMethod = StatModifier.ModifyMethod.MULTIPLICATIVE,
                amount = 1.15f,
            });
            statEffects.Add(new Effect()
            {
                statToEffect = PlayerStats.StatType.PlayerBulletScale,
                modifyMethod = StatModifier.ModifyMethod.MULTIPLICATIVE,
                amount = 1.15f,
            });
            statEffects.Add(new Effect()
            {
                statToEffect = PlayerStats.StatType.ProjectileSpeed,
                modifyMethod = StatModifier.ModifyMethod.MULTIPLICATIVE,
                amount = 1.15f,
            });
            statEffects.Add(new Effect()
            {
                statToEffect = PlayerStats.StatType.RateOfFire,
                modifyMethod = StatModifier.ModifyMethod.MULTIPLICATIVE,
                amount = 1.15f,
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
