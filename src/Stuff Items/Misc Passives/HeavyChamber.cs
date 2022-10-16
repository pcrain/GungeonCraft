using ItemAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NevernamedsItems
{
    public class HeavyChamber : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Heavy Chamber";
            string resourceName = "NevernamedsItems/Resources/heavychamber_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<HeavyChamber>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Trade-off";
            string longDesc = "Doubles clip size, but also doubles the time it takes to reload." + "\n\nThis chamber is sturdy enough to carry many more bullets, but reloading it is a pain.";
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.ReloadSpeed, 2, StatModifier.ModifyMethod.MULTIPLICATIVE);
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.AdditionalClipCapacityMultiplier, 2, StatModifier.ModifyMethod.MULTIPLICATIVE);

            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            item.CanBeDropped = true;
            item.quality = PickupObject.ItemQuality.B;
            HeavyChamberID = item.PickupObjectId;
        }
        public static int HeavyChamberID;
    }
}
