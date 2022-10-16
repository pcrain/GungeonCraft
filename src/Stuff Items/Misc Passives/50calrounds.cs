using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Alexandria.ItemAPI;
using UnityEngine;

namespace NevernamedsItems
{
    public class FiftyCalRounds : PassiveItem
    {
        public static void Init()
        {
            string itemName = "50. Cal Rounds";
            string resourceName = "NevernamedsItems/Resources/50calrounds_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<FiftyCalRounds>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Randy's Favourite";
            string longDesc = "These bullets are nothing special by Gungeon standards, but they do pack a decent punch.\n\n" + "Favoured by a peculiar young Gungeoneer with even more peculiar tastes.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.Damage, 1.16f, StatModifier.ModifyMethod.MULTIPLICATIVE);
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.ProjectileSpeed, 1.25f, StatModifier.ModifyMethod.MULTIPLICATIVE);
            item.quality = PickupObject.ItemQuality.C;
            item.SetTag("bullet_modifier");
            item.AddToSubShop(ItemBuilder.ShopType.Trorc);
            FiftyCalRoundsID = item.PickupObjectId;
        }
        public static int FiftyCalRoundsID;
    }
}
