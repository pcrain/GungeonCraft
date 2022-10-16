using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Alexandria.ItemAPI;

namespace NevernamedsItems
{
    public class EndlessBullets : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Endless Bullets";
            string resourceName = "NevernamedsItems/Resources/endlessbullets_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<EndlessBullets>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "And They Don't Stop Comin'";
            string longDesc = "Grants nigh infinite range on all weapons."+"\n\nIn the Gungeon, range is your ultimate advantage. Keep your distance from the enemy.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.RangeMultiplier, 100, StatModifier.ModifyMethod.MULTIPLICATIVE);
            item.quality = PickupObject.ItemQuality.C;
            item.SetTag("bullet_modifier");
        }
    }
}

