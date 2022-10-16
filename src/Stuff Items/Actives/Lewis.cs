using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Alexandria.ItemAPI;
using UnityEngine;

namespace NevernamedsItems
{
    class Lewis : PlayerItem
    {
        public static void Init()
        {
            string itemName = "Lewis";
            string resourceName = "NevernamedsItems/Resources/lewis_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<Lewis>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "With Friends Like These...";
            string longDesc = "This absolute freeloader just sits in your active item storage doing nothing.\n\n" + "At least he kinda pays rent through providing you some stats";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.Damage, 1000);
            item.consumable = false;
            item.quality = ItemQuality.B;         
            item.SetTag("non_companion_living_item");
            LewisID = item.PickupObjectId;
        }
        public static int LewisID;

        public override void DoEffect(PlayerController user)
        {
            AkSoundEngine.PostEvent("Play_ENM_blobulord_reform_01", base.gameObject);
        }

        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            needRestat = true;
            player.stats.RecalculateStats(player, true, false);
        }

        public DebrisObject Drop(PlayerController player)
        {
            DebrisObject debrisObject = base.Drop(player);
            hasSynergy = false;
            needRestat = false;
            player.stats.RecalculateStats(player, true, false);
            return debrisObject;
        }
        public override void OnDestroy()
        {
            if (LastOwner != null)
            {
                hasSynergy = false;
                needRestat = false;
                LastOwner.stats.RecalculateStats(LastOwner, true, false);
            }
            base.OnDestroy();
        }


        
       
        //PSEUDOSYNERGY
        private void AddStat(PlayerStats.StatType statType, float amount, StatModifier.ModifyMethod method = StatModifier.ModifyMethod.ADDITIVE)
        {
            foreach (var m in passiveStatModifiers)
            {
                if (m.statToBoost == statType) return; //don't add duplicates
            }

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

        private bool hasSynergy, needRestat;

        public override void Update()
        {
            PlayerController player = this.LastOwner;
            if (player && player.HasActiveItem(this.PickupObjectId))
            {
                if (player.PlayerHasActiveSynergy("Rally The Slacker") && hasSynergy == false)
                {
                    hasSynergy = true;
                    needRestat = true;
                    RemoveStat(PlayerStats.StatType.Damage);
                    RemoveStat(PlayerStats.StatType.RateOfFire);
                    RemoveStat(PlayerStats.StatType.ReloadSpeed);
                    RemoveStat(PlayerStats.StatType.MovementSpeed);
                    RemoveStat(PlayerStats.StatType.Health);

                    //STATS WITH BANNER
                    AddStat(PlayerStats.StatType.Damage, 1.5f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                    AddStat(PlayerStats.StatType.RateOfFire, 1.5f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                    AddStat(PlayerStats.StatType.ReloadSpeed, 0.5f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                    AddStat(PlayerStats.StatType.MovementSpeed, 1.5f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                    AddStat(PlayerStats.StatType.Health, 2f, StatModifier.ModifyMethod.ADDITIVE);
                    player.stats.RecalculateStats(player, true, false);
                }

                else if (!player.PlayerHasActiveSynergy("Rally The Slacker") && needRestat == true)
                {
                    needRestat = false;
                    hasSynergy = false;
                    RemoveStat(PlayerStats.StatType.Damage);
                    RemoveStat(PlayerStats.StatType.RateOfFire);
                    RemoveStat(PlayerStats.StatType.ReloadSpeed);
                    RemoveStat(PlayerStats.StatType.MovementSpeed);
                    RemoveStat(PlayerStats.StatType.Health);

                    //STATS WITHOUT BANNER
                    AddStat(PlayerStats.StatType.Damage, 1.2f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                    AddStat(PlayerStats.StatType.RateOfFire, 1.2f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                    AddStat(PlayerStats.StatType.ReloadSpeed, 0.8f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                    AddStat(PlayerStats.StatType.MovementSpeed, 1.2f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                    AddStat(PlayerStats.StatType.Health, 1f, StatModifier.ModifyMethod.ADDITIVE);
                    player.stats.RecalculateStats(player, true, false);
                }
            }
            else { return; }
        }
    }
}

