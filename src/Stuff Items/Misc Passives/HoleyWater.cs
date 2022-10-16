using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ItemAPI;
using SaveAPI;
using UnityEngine;

namespace NevernamedsItems
{
    public class HoleyWater : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Holey Water";
            string resourceName = "NevernamedsItems/Resources/NeoItemSprites/holeywater_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<HoleyWater>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Pure";
            string longDesc = "Prevents the more esoteric effects of curse."+"\n\nDistilled with water taken from the elusive Shrine of Cleansing.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            item.quality = PickupObject.ItemQuality.D;
            HoleyWaterID = item.PickupObjectId;
            item.SetupUnlockOnCustomFlag(CustomDungeonFlags.FLOOR_CLEARED_WITH_CURSE, true);
        }
        public static int HoleyWaterID;
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
        }
        public override DebrisObject Drop(PlayerController player)
        {
            return base.Drop(player);
        }
        public override void OnDestroy()
        {
            base.OnDestroy();
        }
    }
}
