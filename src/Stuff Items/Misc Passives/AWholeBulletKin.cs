using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Alexandria.ItemAPI;
using UnityEngine;

namespace NevernamedsItems
{
    public class AWholeBulletKin : PassiveItem
    {
        public static void Init()
        {
            string itemName = "A Whole Bullet Kin";
            string resourceName = "NevernamedsItems/Resources/awholebulletkin_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<AWholeBulletKin>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "What.";
            string longDesc = "This is just... a bullet kin...\n\n" + "Is he moving?... Is he alive? Comatose? Also WHAT?";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.ReloadSpeed, 0.8f, StatModifier.ModifyMethod.MULTIPLICATIVE);
            item.quality = PickupObject.ItemQuality.D;
            WholeBulletKinID = item.PickupObjectId;
            item.SetTag("non_companion_living_item");
        }
        public static int WholeBulletKinID;
    }
}
