using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using System.Collections;
using Alexandria.ItemAPI;

namespace NevernamedsItems
{
    public class MagicQuiver : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Magic Quiver";
            string resourceName = "NevernamedsItems/Resources/magicquiver_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<MagicQuiver>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Stranger Ranger";
            string longDesc = "Increases the damage of arrow-based weapons." + "\n\nAlben Smallbore was commissioned to create this artefact by a disappointed Bowman, disatisfied with the Gungeon's nigh dismissal of his craft.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.C;
        }
        private Gun currentHeldGun, lastHeldGun;
        public override void Update()
        {
            if (Owner && Owner.CurrentGun)
            {
                currentHeldGun = Owner.CurrentGun;
                if (currentHeldGun != lastHeldGun)
                {
                    RemoveStat(PlayerStats.StatType.Damage);
                    if (Owner.CurrentGun.HasTag("arrow_bolt_weapon")) 
                    {
                        AddStat(PlayerStats.StatType.Damage, 1.5f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                    }
                    Owner.stats.RecalculateStats(Owner, true, false);
                    lastHeldGun = currentHeldGun;
                }
            }
            base.Update();
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
