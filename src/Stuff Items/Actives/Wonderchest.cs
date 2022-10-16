using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Alexandria.ChestAPI;
using Alexandria.Misc;
using Dungeonator;
using ItemAPI;
using UnityEngine;

namespace NevernamedsItems
{
    class Wonderchest : PlayerItem
    {
        public static void Init()
        {
            string itemName = "Wonderchest";
            string resourceName = "NevernamedsItems/Resources/wonderchest_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<Wonderchest>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "What could be inside?";
            string longDesc = "Extremely rare chests such as this one were particularly favoured by Alben Smallbore for storing his valuables." + "\n\nThe complicated magically encripted lock on this thing causes it to access a different pocket subreality depending on where it is opened.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.None, 1000);
            item.consumable = true;
            item.quality = ItemQuality.B;

            item.AddToSubShop(ItemBuilder.ShopType.Flynt);
        }
        public override void DoEffect(PlayerController user)
        {
            bool hasDeterminedValidFloor = false;
            if (GameManager.Instance.Dungeon.IsGlitchDungeon) //GLITCHED FLOOR BONUS
            {
                IntVector2 bestRewardLocation2 = user.CurrentRoom.GetBestRewardLocation(IntVector2.One * 3, RoomHandler.RewardLocationStyle.PlayerCenter, true);
                ChestUtility.SpawnChestEasy(bestRewardLocation2, ChestUtility.ChestTier.BLACK, true, Chest.GeneralChestType.UNSPECIFIED);
            }
            if (UnityEngine.Random.value <= 0.001f) //RANDOM RARE RAINBOW
            {
                IntVector2 bestRewardLocation = user.CurrentRoom.GetBestRewardLocation(IntVector2.One * 3, RoomHandler.RewardLocationStyle.PlayerCenter, true);
                ChestUtility.SpawnChestEasy(bestRewardLocation, ChestUtility.ChestTier.RAINBOW, false, Chest.GeneralChestType.UNSPECIFIED, ThreeStateValue.FORCENO, ThreeStateValue.FORCENO);
            }
            else
            {
                switch (GameManager.Instance.Dungeon.tileIndices.tilesetId)
                {
                    case GlobalDungeonData.ValidTilesets.CASTLEGEON: //KEEP
                        int pickupID = 224;
                        if (UnityEngine.Random.value <= .50f) pickupID = 120;
                        LootEngine.GivePrefabToPlayer(PickupObjectDatabase.GetById(pickupID).gameObject, user);
                        LootEngine.GivePrefabToPlayer(PickupObjectDatabase.GetById(pickupID).gameObject, user);
                        hasDeterminedValidFloor = true;
                        break;
                    case GlobalDungeonData.ValidTilesets.SEWERGEON: //OUBLIETTE
                        for (int i = 0; i < 3; i++) LootEngine.GivePrefabToPlayer(PickupObjectDatabase.GetById(67).gameObject, user);
                        hasDeterminedValidFloor = true;
                        break;
                    case GlobalDungeonData.ValidTilesets.JUNGLEGEON: //JUNGLE
                        for (int i = 0; i < 3; i++)
                        {
                            IntVector2 bestRewardLocation = user.CurrentRoom.GetRandomVisibleClearSpot(2, 2);
                            ChestUtility.SpawnChestEasy(bestRewardLocation, ChestUtility.ChestTier.BROWN, false, Chest.GeneralChestType.UNSPECIFIED);
                        }
                        hasDeterminedValidFloor = true;
                        break;
                    case GlobalDungeonData.ValidTilesets.GUNGEON: //GUNGEON PROPER
                        for (int i = 0; i < 3; i++) LootEngine.GivePrefabToPlayer(PickupObjectDatabase.GetById(74).gameObject, user);
                        hasDeterminedValidFloor = true;
                        break;
                    case GlobalDungeonData.ValidTilesets.CATHEDRALGEON: //ABBEY
                        ChangeStatPermanent(user, PlayerStats.StatType.Health, 2, StatModifier.ModifyMethod.ADDITIVE);
                        ChangeStatPermanent(user, PlayerStats.StatType.Curse, 2, StatModifier.ModifyMethod.ADDITIVE);
                        if (user.ForceZeroHealthState)
                        {
                            LootEngine.GivePrefabToPlayer(PickupObjectDatabase.GetById(120).gameObject, user);
                            LootEngine.GivePrefabToPlayer(PickupObjectDatabase.GetById(120).gameObject, user);
                        }
                        hasDeterminedValidFloor = true;
                        break;
                    case GlobalDungeonData.ValidTilesets.MINEGEON: //MINES
                        IntVector2 bestRewardLocation2 = user.CurrentRoom.GetBestRewardLocation(IntVector2.One * 3, RoomHandler.RewardLocationStyle.PlayerCenter, true);
                        ChestUtility.SpawnChestEasy(bestRewardLocation2, ChestUtility.ChestTier.RED, false, Chest.GeneralChestType.UNSPECIFIED);
                        hasDeterminedValidFloor = true;
                        break;
                    case GlobalDungeonData.ValidTilesets.RATGEON: //RAT LAIR
                        LootEngine.GivePrefabToPlayer(PickupObjectDatabase.GetById(727).gameObject, user);
                        hasDeterminedValidFloor = true;
                        break;
                    case GlobalDungeonData.ValidTilesets.CATACOMBGEON: // HOLLOW
                        IntVector2 bestRewardLocation3 = user.CurrentRoom.GetBestRewardLocation(IntVector2.One * 3, RoomHandler.RewardLocationStyle.PlayerCenter, true);
                        Chest spawnedChestRandomHollow = GameManager.Instance.RewardManager.SpawnRewardChestAt(bestRewardLocation3);
                        spawnedChestRandomHollow.RegisterChestOnMinimap(spawnedChestRandomHollow.GetAbsoluteParentRoom());
                        ChangeStatPermanent(user, PlayerStats.StatType.GlobalPriceMultiplier, 0.8f, StatModifier.ModifyMethod.MULTIPLICATIVE);
                        hasDeterminedValidFloor = true;
                        break;
                    case GlobalDungeonData.ValidTilesets.OFFICEGEON: //R&G DEPT
                        for (int i = 0; i < 2; i++)
                        {
                            IntVector2 bestRewardLocation4 = user.CurrentRoom.GetRandomVisibleClearSpot(2, 2);
                            Chest spawnedChestRNG = GameManager.Instance.RewardManager.SpawnRewardChestAt(bestRewardLocation4);
                            spawnedChestRNG.RegisterChestOnMinimap(spawnedChestRNG.GetAbsoluteParentRoom());
                        }
                        hasDeterminedValidFloor = true;
                        break;
                    case GlobalDungeonData.ValidTilesets.WESTGEON: //OLD WEST FLOOR (EXPAND)
                        LootEngine.GivePrefabToPlayer(PickupObjectDatabase.GetById(727).gameObject, user);
                        LootEngine.GivePrefabToPlayer(PickupObjectDatabase.GetById(137).gameObject, user);
                        hasDeterminedValidFloor = true;
                        break;
                    case GlobalDungeonData.ValidTilesets.FORGEGEON: //FORGE
                        for (int i = 0; i < 6; i++) LootEngine.GivePrefabToPlayer(PickupObjectDatabase.GetById(120).gameObject, user);
                        PickupObject itemOfTypeAndQuality = LootEngine.GetItemOfTypeAndQuality<PickupObject>(BraveUtility.RandomElement(BToSItemTiers), GameManager.Instance.RewardManager.GunsLootTable, false);
                        LootEngine.SpawnItem(itemOfTypeAndQuality.gameObject, LastOwner.specRigidbody.UnitCenter, Vector2.left, 1f, false, true, false);
                        hasDeterminedValidFloor = true;
                        break;
                    case GlobalDungeonData.ValidTilesets.HELLGEON: //BULLET HELL
                        if (GameManager.IsGunslingerPast)
                        {
                            for (int i = 0; i < 2; i++)
                            {
                                IntVector2 bestRewardLocation5 = user.CurrentRoom.GetRandomVisibleClearSpot(2, 2);
                                ChestUtility.SpawnChestEasy(bestRewardLocation5, ChestUtility.ChestTier.BLACK, true, Chest.GeneralChestType.UNSPECIFIED);
                            }
                            ChangeStatPermanent(user, PlayerStats.StatType.Curse, 5, StatModifier.ModifyMethod.ADDITIVE);
                        }
                        else
                        {
                            for (int i = 0; i < 2; i++)
                            {
                                IntVector2 bestRewardLocation4 = user.CurrentRoom.GetRandomVisibleClearSpot(2, 2);
                                Chest randomHellMagChest = GameManager.Instance.RewardManager.SpawnRewardChestAt(bestRewardLocation4);
                                randomHellMagChest.RegisterChestOnMinimap(randomHellMagChest.GetAbsoluteParentRoom());
                            }
                            for (int i = 0; i < 2; i++)
                            {
                                IntVector2 bestRewardLocation4 = user.CurrentRoom.GetRandomVisibleClearSpot(2, 2);
                                Chest randomHellRandoChest = GameManager.Instance.RewardManager.SpawnTotallyRandomChest(bestRewardLocation4);
                                randomHellRandoChest.RegisterChestOnMinimap(randomHellRandoChest.GetAbsoluteParentRoom());
                            }
                            ChangeStatPermanent(user, PlayerStats.StatType.Curse, 3, StatModifier.ModifyMethod.ADDITIVE);
                        }
                        hasDeterminedValidFloor = true;
                        break;
                    case GlobalDungeonData.ValidTilesets.BELLYGEON: //BELLY
                        ChangeStatPermanent(user, PlayerStats.StatType.Health, 1, StatModifier.ModifyMethod.ADDITIVE);
                        user.healthHaver.ApplyHealing(100f);
                        if (user.ForceZeroHealthState)
                        {
                            for (int i = 0; i < 5; i++) LootEngine.GivePrefabToPlayer(PickupObjectDatabase.GetById(120).gameObject, user);
                        }
                        hasDeterminedValidFloor = true;
                        break;
                }
                //-----------------------------------------DEFAULT CATCH EFFECT
                if (!hasDeterminedValidFloor)
                {
                    LootEngine.SpawnItem(PickupObjectDatabase.GetById(127).gameObject, user.specRigidbody.UnitCenter, Vector2.zero, 1f, false, true, false);
                }
            }
        }
        public static List<PickupObject.ItemQuality> BToSItemTiers = new List<PickupObject.ItemQuality>()
        {
            ItemQuality.B,
            ItemQuality.A,
            ItemQuality.S,
        };    
        private void ChangeStatPermanent(PlayerController target, PlayerStats.StatType statToChance, float amount, StatModifier.ModifyMethod modifyMethod)
        {
            StatModifier statModifier = new StatModifier();
            statModifier.amount = amount;
            statModifier.modifyType = modifyMethod;
            statModifier.statToBoost = statToChance;
            target.ownerlessStatModifiers.Add(statModifier);
            target.stats.RecalculateStats(target, false, false);
        }
    }
}
