using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using ItemAPI;

namespace NevernamedsItems
{
    class HeartPadlock : PlayerItem
    {
        public static void Init()
        {
            string itemName = "Heart Padlock";
            string resourceName = "NevernamedsItems/Resources/heartpadlock_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<HeartPadlock>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Locked Life";
            string longDesc = "Spend keys to heal." + "\n\nLocks such as these are commonly used by powerful Gunjurers to secure their souls to their bodies in case of catastrophic injury.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.Timed, 2);
            item.consumable = false;
            item.quality = ItemQuality.D;

            item.AddToSubShop(ItemBuilder.ShopType.Flynt);
            HeartPadlockID = item.PickupObjectId;
        }

        public static int HeartPadlockID;
        public override void DoEffect(PlayerController user)
        {
            AkSoundEngine.PostEvent("Play_OBJ_goldkey_pickup_01", base.gameObject);
            user.PlayEffectOnActor(ResourceCache.Acquire("Global VFX/vfx_healing_sparkles_001") as GameObject, Vector3.zero, true, false, false);

            if (user.ForceZeroHealthState) user.healthHaver.Armor += user.PlayerHasActiveSynergy("All Locked Up") ? 2 : 1;
            else user.healthHaver.ApplyHealing(user.PlayerHasActiveSynergy("All Locked Up") ? 2 : 1);

            user.carriedConsumables.KeyBullets -= (user.PlayerHasActiveSynergy("Key Death") && UnityEngine.Random.value < .5f) ? 0 : 1;
        }

        public override bool CanBeUsed(PlayerController user)
        {
            if (user.carriedConsumables.KeyBullets >= 1) return true;
            else return false;
        }
    }
}
