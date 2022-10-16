using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using ItemAPI;
using Dungeonator;

namespace NevernamedsItems
{
    class Toolbox : PlayerItem
    {
        public static void Init()
        {
            string itemName = "Toolbox";
            string resourceName = "NevernamedsItems/Resources/toolbox_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<Toolbox>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Robust";
            string longDesc = "Makes a random object." + "\n\nA blunt object popular for it's usefulness in bludgeoning other people (or yourself) in the head." + "\n\nAlso holds tools, or whatever.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.Damage, 60);
            item.consumable = false;
            item.quality = ItemQuality.D;
        }
        public static List<GameObject> PossibleObjects = new List<GameObject>()
        {
            //Tables
            EasyPlaceableObjects.TableVertical,
            EasyPlaceableObjects.TableHorizontal,
            EasyPlaceableObjects.TableHorizontalStone,
            EasyPlaceableObjects.TableVerticalStone,
            EasyPlaceableObjects.FoldingTable,
            //Coffins
            EasyPlaceableObjects.CoffinHoriz,
            EasyPlaceableObjects.CoffinVert,
            //Barrels
            EasyPlaceableObjects.ExplosiveBarrel,
            EasyPlaceableObjects.MetalExplosiveBarrel,
            EasyPlaceableObjects.OilBarrel,
            EasyPlaceableObjects.PoisonBarrel,
            EasyPlaceableObjects.WaterBarrel,
            //Misc
            EasyPlaceableObjects.IceBomb,
            //EasyPlaceableObjects.Brazier,
        };
        public override void DoEffect(PlayerController user)
        {
            try
            {
                IntVector2 bestRewardLocation2 = user.CurrentRoom.GetBestRewardLocation(IntVector2.One * 2, RoomHandler.RewardLocationStyle.PlayerCenter, true);
                Vector3 convertedVector = bestRewardLocation2.ToVector3();

                FireplaceController Fireplace = null;
                if (GameManager.Instance.Dungeon.tileIndices.tilesetId == GlobalDungeonData.ValidTilesets.CASTLEGEON) { Fireplace = FindObjectOfType<FireplaceController>(); }
                GameObject thingToSpawn = BraveUtility.RandomElement(PossibleObjects);
                if (Fireplace != null && Fireplace.transform.position.GetAbsoluteRoom() == user.CurrentRoom) { thingToSpawn = EasyPlaceableObjects.WaterBarrel; }

                if (UnityEngine.Random.value <= 0.01f)
                {
                    IntVector2 bestRewardLocation = user.CurrentRoom.GetBestRewardLocation(IntVector2.One * 2, RoomHandler.RewardLocationStyle.PlayerCenter, true);
                    Chest brown_Chest = GameManager.Instance.RewardManager.D_Chest;
                    brown_Chest.IsLocked = false;
                    brown_Chest.ChestType = Chest.GeneralChestType.UNSPECIFIED;
                    Chest spawnedChest = Chest.Spawn(brown_Chest, bestRewardLocation);
                    spawnedChest.lootTable.lootTable = (UnityEngine.Random.value <= 0.5f ? GameManager.Instance.RewardManager.GunsLootTable : GameManager.Instance.RewardManager.ItemsLootTable);
                    spawnedChest.RegisterChestOnMinimap(spawnedChest.GetAbsoluteParentRoom());
                }
                else
                {
                    SpawnObjectManager.SpawnObject(thingToSpawn, convertedVector, EasyVFXDatabase.BloodiedScarfPoofVFX);
                }

                if (user.PlayerHasActiveSynergy("His Grace")) { KillRandomEnemy(user.CurrentRoom); }
                if (user.PlayerHasActiveSynergy("Sharpest Tool In The Shed")) { DoRandomRoomSpawns(user.CurrentRoom); }
            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message);
                ETGModConsole.Log(e.StackTrace);
            }
        }
        private void DoRandomRoomSpawns(RoomHandler room)
        {
            for (int i = 0; i < 2; i++)
            {
                Vector3 positionToSpawn3 = room.GetRandomVisibleClearSpot(2, 2).ToVector3();
                SpawnObjectManager.SpawnObject(BraveUtility.RandomElement(PossibleObjects), positionToSpawn3, EasyVFXDatabase.BloodiedScarfPoofVFX);
            }
        }

        private void KillRandomEnemy(RoomHandler room)
        {

            AIActor randomActiveEnemy = room.GetRandomActiveEnemy(true);
            if (randomActiveEnemy.IsNormalEnemy && randomActiveEnemy.healthHaver && !randomActiveEnemy.healthHaver.IsBoss)
            {
                UnityEngine.Object.Instantiate<GameObject>(EasyVFXDatabase.TeleporterPrototypeTelefragVFX, randomActiveEnemy.CenterPosition.ToVector3ZisY(0f), Quaternion.identity);
                randomActiveEnemy.healthHaver.ApplyDamage(100000f, Vector2.zero, "His Grace", CoreDamageTypes.None, DamageCategory.Normal, true, null, false);
            }
        }
        public override bool CanBeUsed(PlayerController user)
        {
            return true;
        }
    }
}