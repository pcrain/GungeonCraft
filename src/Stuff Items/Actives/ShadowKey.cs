using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Alexandria.ItemAPI;
using Dungeonator;
using Alexandria.ChestAPI;
using Alexandria.Misc;

namespace NevernamedsItems
{
    class ShadowKey : PlayerItem
    {
        public static void Init()
        {
            string itemName = "Shadow Key";
            string resourceName = "NevernamedsItems/Resources/NeoActiveSprites/shadowkey_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<ShadowKey>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Dark Realmed";
            string longDesc = "Duplicates a chest. One use."+"\n\nRecovered from a mysterious chest in a far off dungeon, this key is capable of turning a chest's shadow into an exact duplicate of the original.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.None, 5);
            item.consumable = true;
            item.quality = ItemQuality.D;
            item.AddToSubShop(ItemBuilder.ShopType.Flynt);
        }
        public override void DoEffect(PlayerController user)
        {
            IPlayerInteractable nearestInteractable = user.CurrentRoom.GetNearestInteractable(user.CenterPosition, 1f, user);
            if (!(nearestInteractable is Chest)) return;
            Chest dupeChest = nearestInteractable as Chest;

            IntVector2 bestRewardLocation = user.CurrentRoom.GetBestRewardLocation(IntVector2.One * 3, RoomHandler.RewardLocationStyle.PlayerCenter, true);

            ChestUtility.ChestTier tier = dupeChest.GetChestTier();

            if (tier == ChestUtility.ChestTier.RAT)
            {
                tier = ChestUtility.ChestTier.RED;
            }
            else if (tier == ChestUtility.ChestTier.TRUTH)
            {
                tier = ChestUtility.ChestTier.BLUE;
            }

            ThreeStateValue isMimic = ThreeStateValue.UNSPECIFIED;
            if (dupeChest.IsMimic) isMimic = ThreeStateValue.FORCEYES;
            else isMimic = ThreeStateValue.FORCENO;

            ThreeStateValue isFused = ThreeStateValue.UNSPECIFIED;
            if (dupeChest.GetFuse() != null) isFused = ThreeStateValue.FORCEYES;
            else isFused = ThreeStateValue.FORCENO;

            Chest spawnedChest = ChestUtility.SpawnChestEasy(bestRewardLocation, tier, dupeChest.IsLocked, Chest.GeneralChestType.UNSPECIFIED, isMimic, isFused);

            if (dupeChest.GetComponent<JammedChestBehav>()) spawnedChest.gameObject.AddComponent<JammedChestBehav>();
            else if (dupeChest.GetComponent<PassedOverForJammedChest>()) spawnedChest.gameObject.AddComponent<PassedOverForJammedChest>();
        }

        public override bool CanBeUsed(PlayerController user)
        {
            IPlayerInteractable nearestInteractable = user.CurrentRoom.GetNearestInteractable(user.CenterPosition, 1f, user);
            if (nearestInteractable is Chest)
            {
                return true;
            }
            else return false;
        }
    }
}

