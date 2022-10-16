using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dungeonator;
using Gungeon;
using ItemAPI;
using SaveAPI;
using UnityEngine;

namespace NevernamedsItems
{
    public class ShadesEye : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Shade's Eye";
            string resourceName = "NevernamedsItems/Resources/shadeseye_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<ShadesEye>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Everything For A Price";
            string longDesc = "Doubles boss loot, but taking damage in a bossfight causes instant death." + "\nDestroyed upon being discarded." + "\n\nThe wandering eye of a vengeful shade.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.C;
            Game.Items.Rename("nn:shade's_eye", "nn:shades_eye");
            ShadesEyeID = item.PickupObjectId;
            item.SetupUnlockOnCustomFlag(CustomDungeonFlags.CHEATED_DEATH_SHADE, true);
        }
        public override void Pickup(PlayerController player)
        {
            player.OnRoomClearEvent += OnRoomClear;
            player.OnReceivedDamage += OnDamaged;
            base.Pickup(player);
        }
        public override DebrisObject Drop(PlayerController player)
        {
            player.OnReceivedDamage -= OnDamaged;
            player.OnRoomClearEvent -= OnRoomClear;
            DebrisObject obj = base.Drop(player);
            Destroy(obj.gameObject, 1f);
            return obj;
        }
        public override void OnDestroy()
        {
            if (Owner)
            {
                Owner.OnRoomClearEvent -= OnRoomClear;
                Owner.OnReceivedDamage -= OnDamaged;
            }
            base.OnDestroy();
        }
        private void OnDamaged(PlayerController player)
        {
            if (player && player.CurrentRoom != null)
            {
                if (player.CurrentRoom.area.PrototypeRoomCategory == PrototypeDungeonRoom.RoomCategory.BOSS)
                {
                    Owner.healthHaver.Armor = 0;
                    Owner.healthHaver.ForceSetCurrentHealth(0);
                    Owner.healthHaver.Die(Vector2.zero);
                }
            }
        }
        public static List<int> lootIDlist = new List<int>()
        {
            73, //Half Heart
            85, //Heart
            120, //Armor
            67, //Key
        };
        private void GiveConsumables()
        {
            int amt = UnityEngine.Random.Range(2, 4);
            for (int i = 0; i < amt; i++)
            {
                IntVector2 bestRewardLocation2 = Owner.CurrentRoom.GetBestRewardLocation(IntVector2.One * 3, RoomHandler.RewardLocationStyle.PlayerCenter, true);
                Vector3 convertedVector = bestRewardLocation2.ToVector3();
                LootEngine.SpawnItem(PickupObjectDatabase.GetById(BraveUtility.RandomElement(lootIDlist)).gameObject, convertedVector, Vector2.zero, 1f, false, true, false);
            }
        }
        private void OnRoomClear(PlayerController player)
        {
            if (player && player.CurrentRoom != null)
            {
                if (player.CurrentRoom.area.PrototypeRoomCategory == PrototypeDungeonRoom.RoomCategory.BOSS)
                {
                    if (!player.CurrentRoom.PlayerHasTakenDamageInThisRoom && player.CurrentRoom.area.PrototypeRoomBossSubcategory != PrototypeDungeonRoom.RoomBossSubCategory.MINI_BOSS)
                    {
                        GiveMastery();
                    }
                    GiveItem();
                    GiveConsumables();
                }
            }
        }
        private void GiveItem()
        {
            float itemOrGun = UnityEngine.Random.value;
            PickupObject itemToSpawn = null;
            if (itemOrGun > 0.5)
            {
                itemToSpawn = LootEngine.GetItemOfTypeAndQuality<PickupObject>(PickupObject.ItemQuality.A, GameManager.Instance.RewardManager.ItemsLootTable, true);
            }
            else
            {
                itemToSpawn = LootEngine.GetItemOfTypeAndQuality<Gun>(PickupObject.ItemQuality.A, GameManager.Instance.RewardManager.GunsLootTable, true);
            }
            LootEngine.SpawnItem(itemToSpawn.gameObject, Owner.sprite.WorldCenter, Vector2.zero, 0);
        }
        private void GiveMastery()
        {
            if (GameManager.Instance.Dungeon.BossMasteryTokenItemId > 0)
            {
                LootEngine.SpawnItem((PickupObjectDatabase.GetById(GameManager.Instance.Dungeon.BossMasteryTokenItemId).gameObject), Owner.sprite.WorldCenter, Vector2.zero, 0);
            }
        }
        public static int ShadesEyeID;
    }
}
