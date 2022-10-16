using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Alexandria.ItemAPI;
using UnityEngine;

namespace NevernamedsItems
{
    public class HardReloadBullets : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Hard Reload Bullets";
            string resourceName = "NevernamedsItems/Resources/hardreloadbullets_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<HardReloadBullets>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Slippery Buggers";
            string longDesc = "These bullets were made by a narcissistic gunslinger purely to flex his highly trained fine motor skills on others.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.ReloadSpeed, 1.5f, StatModifier.ModifyMethod.MULTIPLICATIVE);
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.Damage, 1.30f, StatModifier.ModifyMethod.MULTIPLICATIVE);
            item.quality = PickupObject.ItemQuality.C;
            item.SetTag("bullet_modifier");
            item.AddToSubShop(ItemBuilder.ShopType.Trorc);
        }
    }
}
