using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ItemAPI;

namespace NevernamedsItems
{
    class ForsakenHeart : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Forsaken Heart";
            string resourceName = "NevernamedsItems/Resources/NeoItemSprites/forsakenheart_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<ForsakenHeart>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "And lo, it shall embroil within";
            string longDesc = "A cursed heart embiggens the smallest of wretches.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.Curse, 1f, StatModifier.ModifyMethod.ADDITIVE);
            item.quality = PickupObject.ItemQuality.B;
            item.AddToSubShop(ItemBuilder.ShopType.Cursula);
            ForsakenHeartID = item.PickupObjectId;
        }
        public static int ForsakenHeartID;
        private float lastCurse = -1f;
        public override void Update()
        {
            if (Owner != null)
            {
                float curse = Owner.stats.GetStatValue(PlayerStats.StatType.Curse);
                if (curse != lastCurse)
                {
                    this.RemoveStat(PlayerStats.StatType.Health);
                    if (curse > 0)
                    {
                        this.AddStat(PlayerStats.StatType.Health, Mathf.FloorToInt(curse / 2.5f), StatModifier.ModifyMethod.ADDITIVE);
                        base.Owner.stats.RecalculateStats(base.Owner, true, false);
                        this.lastCurse = curse;
                    }
                }
            }
            base.Update();
        }
        private void AddStat(PlayerStats.StatType statType, float amount, StatModifier.ModifyMethod method = StatModifier.ModifyMethod.ADDITIVE)
        {
            StatModifier statModifier = new StatModifier();
            statModifier.amount = amount;
            statModifier.statToBoost = statType;
            statModifier.modifyType = method;
            foreach (StatModifier statModifier2 in this.passiveStatModifiers)
            {
                bool flag = statModifier2.statToBoost == statType;
                if (flag)
                {
                    return;
                }
            }
            bool flag2 = this.passiveStatModifiers == null;
            if (flag2)
            {
                this.passiveStatModifiers = new StatModifier[]
                {
            statModifier
                };
                return;
            }
            this.passiveStatModifiers = this.passiveStatModifiers.Concat(new StatModifier[]
            {
        statModifier
            }).ToArray<StatModifier>();
        }
        private void RemoveStat(PlayerStats.StatType statType)
        {
            List<StatModifier> list = new List<StatModifier>();
            for (int i = 0; i < this.passiveStatModifiers.Length; i++)
            {
                bool flag = this.passiveStatModifiers[i].statToBoost != statType;
                if (flag)
                {
                    list.Add(this.passiveStatModifiers[i]);
                }
            }
            this.passiveStatModifiers = list.ToArray();
        }
    }
}
