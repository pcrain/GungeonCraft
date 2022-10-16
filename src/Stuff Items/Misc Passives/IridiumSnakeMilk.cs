using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Alexandria.ItemAPI;

namespace NevernamedsItems
{
    public class IridiumSnakeMilk : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Iridium Snake Milk";
            string resourceName = "NevernamedsItems/Resources/NeoItemSprites/iridiumsnakemilk_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<IridiumSnakeMilk>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "All Speed Up";
            string longDesc = "The imbibing of this vile tasting milk increases the speed of everything one does."+"\n\nOriginally given as a reward to those who reached the deepest depths of the Black Powder Mine.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.ProjectileSpeed, 1.25f, StatModifier.ModifyMethod.MULTIPLICATIVE);           
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.ReloadSpeed, 0.75f, StatModifier.ModifyMethod.MULTIPLICATIVE);
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.RateOfFire, 1.25f, StatModifier.ModifyMethod.MULTIPLICATIVE);
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.ChargeAmountMultiplier, 1.25f, StatModifier.ModifyMethod.MULTIPLICATIVE);
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.MovementSpeed, 2f, StatModifier.ModifyMethod.ADDITIVE);

            item.quality = PickupObject.ItemQuality.A;
        }
    }
}
