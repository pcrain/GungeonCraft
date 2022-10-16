using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using UnityEngine;
using Alexandria.ItemAPI;
using Alexandria.Misc;

namespace NevernamedsItems
{
    class HornedHelmet : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Horned Helmet";
            string resourceName = "NevernamedsItems/Resources/hornedhelmet_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<HornedHelmet>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "19 STR 7 INT";
            string longDesc = "You burst into a berserker rage upon the mere sight of fresh prey. What a monster!";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.B;
            item.AddToSubShop(ItemBuilder.ShopType.Trorc);
            HornedHelmetID = item.PickupObjectId;
        }
        public static int HornedHelmetID;
        public override void Pickup(PlayerController player)
        {
            player.OnEnteredCombat += this.OnEnteredCombat;
            base.Pickup(player);
        }
        public override void DisableEffect(PlayerController player)
        {
            player.OnEnteredCombat -= this.OnEnteredCombat;
            base.DisableEffect(player);
        }       
        private void OnEnteredCombat()
        {
            if (Owner && Owner.GetExtComp()) Owner.GetExtComp().Enrage(3, true);
        }
    }
}
