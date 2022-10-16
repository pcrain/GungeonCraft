using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dungeonator;
using ItemAPI;
using UnityEngine;

namespace NevernamedsItems
{
    class ClipOnAmmoPouch : PlayerItem
    {
        public static void Init()
        {
            string itemName = "Clip-On Ammo Pouch";
            string resourceName = "NevernamedsItems/Resources/cliponammopouch_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<ClipOnAmmoPouch>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Ammo Up";
            string longDesc = "Increases the ammo capacity of the held gun by 50%."+"\n\nClips on easy, and doesn't leave a mark!";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.None, 1000);
            item.consumable = true;
            item.quality = ItemQuality.D;

            item.AddToSubShop(ItemBuilder.ShopType.Trorc);

        }
        public override void DoEffect(PlayerController user)
        {
            Gun instanceGun = user.CurrentGun;
            instanceGun.SetBaseMaxAmmo(Mathf.CeilToInt(instanceGun.GetBaseMaxAmmo() * 1.5f));
            user.stats.RecalculateStats(user);
        }
        public override bool CanBeUsed(PlayerController user)
        {
            if (user && user.CurrentGun != null && !user.CurrentGun.InfiniteAmmo)
            {
                return true;
            }
            else return false;
        }
    }
}
