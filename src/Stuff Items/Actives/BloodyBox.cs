using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dungeonator;
using UnityEngine;
using ItemAPI;
using SaveAPI;

namespace NevernamedsItems
{
    class BloodyBox : PlayerItem
    {
        public static void Init()
        {
            string itemName = "Bloody Box";
            string resourceName = "NevernamedsItems/Resources/bloodybox_icon2";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<BloodyBox>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Oozing and Gurgling";
            string longDesc = "Sacrifice blood to the gods of infinity, in return for a chest." + "\n\nA neatly layered series of chests, emerging upwards infinitely from the gaping maw of the eternal void. The screaming voices from the abyss rattle up through the tower, and they demand sustenance.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.Timed, 5);
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.Curse, 1f, StatModifier.ModifyMethod.ADDITIVE);
            item.consumable = false;
            item.quality = ItemQuality.B;
            item.AddToSubShop(ItemBuilder.ShopType.Flynt);
            item.SetupUnlockOnCustomFlag(CustomDungeonFlags.KILLEDJAMMEDMIMIC, true);
        }
        public override void DoEffect(PlayerController user)
        {
            var locationToSpawn = user.CurrentRoom.GetBestRewardLocation(IntVector2.One * 3, RoomHandler.RewardLocationStyle.PlayerCenter, true);

            AkSoundEngine.PostEvent("Play_VO_lichA_cackle_01", base.gameObject);
            Chest spawnedChest = GameManager.Instance.RewardManager.SpawnTotallyRandomChest(locationToSpawn);
            spawnedChest.RegisterChestOnMinimap(spawnedChest.GetAbsoluteParentRoom());

            if (user.ForceZeroHealthState) user.healthHaver.Armor = user.healthHaver.Armor - 2;
            else { user.healthHaver.ApplyHealing(-2.5f); }
        }

        public override bool CanBeUsed(PlayerController user)
        {
            PlayableCharacters characterIdentity = user.characterIdentity;
            if (user.ForceZeroHealthState) { return user.healthHaver.Armor > 2; }
            else { return user.healthHaver.GetCurrentHealth() > 2.5f; }
        }
    }
}
