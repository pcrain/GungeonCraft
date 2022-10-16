using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Alexandria.ItemAPI;

namespace NevernamedsItems
{
    class GlassRounds : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Glass Rounds";
            string resourceName = "NevernamedsItems/Resources/glassrounds_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<GlassRounds>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Right Through You";
            string longDesc = "+5% damage for every Glass Guon Stone the bearer posesses."+"\n\nDisciples of the Lady of Pane are known for using these special bullets in reverence to their goddess.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.C;
            item.SetTag("bullet_modifier");
        }
        private int currentItems, lastItems;
        public override void Update()
        {
            if (Owner)
            {
                CalculateStats(Owner);
            }

            else { return; }
        }

        private void CalculateStats(PlayerController player)
        {
            currentItems = player.passiveItems.Count;
            if (currentItems != lastItems)
            {
                RemoveStat(PlayerStats.StatType.Damage);
                foreach (PassiveItem item in player.passiveItems)
                {
                    if (item.PickupObjectId == 565)
                    {
                        if (Owner.HasPickupID(Gungeon.Game.Items["nn:glass_chamber"].PickupObjectId))
                        {
                            AddStat(PlayerStats.StatType.Damage, 1.1f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                        }
                        else
                        {
                            AddStat(PlayerStats.StatType.Damage, 1.05f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                        }
                    }
                }

                lastItems = currentItems;
                player.stats.RecalculateStats(player, true, false);
            }
        }


        private void AddStat(PlayerStats.StatType statType, float amount, StatModifier.ModifyMethod method = StatModifier.ModifyMethod.ADDITIVE)
        {
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
        public override void Pickup(PlayerController player)
        {
            bool flag = !this.m_pickedUpThisRun;
            if (flag)
            {
                PickupObject byId = PickupObjectDatabase.GetById(565);
                player.AcquirePassiveItemPrefabDirectly(byId as PassiveItem);
            }
            GameManager.Instance.OnNewLevelFullyLoaded += this.OnNewFloor;
            base.Pickup(player);
        }
        public override DebrisObject Drop(PlayerController player)
        {
            GameManager.Instance.OnNewLevelFullyLoaded -= this.OnNewFloor;
            return base.Drop(player);
        }
        public override void OnDestroy()
        {
            GameManager.Instance.OnNewLevelFullyLoaded -= this.OnNewFloor;
            base.OnDestroy();
        }
        private void OnNewFloor()
        {
            if (Owner)
            {
                Owner.AcquirePassiveItemPrefabDirectly(PickupObjectDatabase.GetById(565) as PassiveItem);
            }
        }
    }
}