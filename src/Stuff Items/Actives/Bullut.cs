using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dungeonator;
using ItemAPI;
using UnityEngine;

namespace NevernamedsItems
{
    class Bullut : PlayerItem
    {
        //Call this method from the Start() method of your ETGModule extension class
        public static void Init()
        {
            //The name of the item
            string itemName = "Bullut";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it.
            string resourceName = "NevernamedsItems/Resources/bullut_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a ActiveItem component to the object
            var item = obj.AddComponent<Bullut>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Supposed Delicacy";
            string longDesc = "Bullet Embryos, boiled inside their shells. Apparently this is supposed to be food." + "\n\nEating this makes the Gundead, Kaliber, and pretty much everyone else mad at you, though the Gunsling King describes it as a... rewarding experience.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //"kts" here is the item pool. In the console you'd type kts:sweating_bullets
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Set the cooldown type and duration of the cooldown
            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.None, 1000);

            //Adds a passive modifier, like curse, coolness, damage, etc. to the item. Works for passives and actives.

            //Set some other fields
            item.consumable = true;
            item.quality = ItemQuality.D;


        }
        public override void DoEffect(PlayerController user)
        {
            ChangeStatPermanent(user, PlayerStats.StatType.Curse, 2, StatModifier.ModifyMethod.ADDITIVE);
            ChangeStatPermanent(user, PlayerStats.StatType.EnemyProjectileSpeedMultiplier, 1.5f, StatModifier.ModifyMethod.MULTIPLICATIVE);
            for (int i = 0; i < 3; i++)
            {
                var locationToSpawn = user.CurrentRoom.GetRandomVisibleClearSpot(2, 2);
                Chest spawnedChest = GameManager.Instance.RewardManager.SpawnRewardChestAt(locationToSpawn);
                spawnedChest.RegisterChestOnMinimap(spawnedChest.GetAbsoluteParentRoom());
            }
            //SYNERGY CHEST
            Chest Synergy_Chest = GameManager.Instance.RewardManager.Synergy_Chest;
            Synergy_Chest.IsLocked = true;           
            Chest SpawnedSynergy = Chest.Spawn(Synergy_Chest, user.CurrentRoom.GetRandomVisibleClearSpot(2, 2));
            SpawnedSynergy.RegisterChestOnMinimap(SpawnedSynergy.GetAbsoluteParentRoom());
            //RED CHEST
            Chest Red_Chest = GameManager.Instance.RewardManager.A_Chest;
            Red_Chest.IsLocked = true;
            Red_Chest.ChestType = Chest.GeneralChestType.UNSPECIFIED;
            Chest SpawnedRed = Chest.Spawn(Red_Chest, user.CurrentRoom.GetRandomVisibleClearSpot(2, 2));
            SpawnedRed.lootTable.lootTable = (UnityEngine.Random.value <= 0.5f ? GameManager.Instance.RewardManager.GunsLootTable : GameManager.Instance.RewardManager.ItemsLootTable);
            SpawnedRed.RegisterChestOnMinimap(SpawnedRed.GetAbsoluteParentRoom());
            //BLACK CHEST
            Chest Black_Chest = GameManager.Instance.RewardManager.S_Chest;
            Black_Chest.IsLocked = true;
            Chest SpawnedBlack = Chest.Spawn(Black_Chest, user.CurrentRoom.GetRandomVisibleClearSpot(2, 2));
            SpawnedBlack.lootTable.lootTable = (UnityEngine.Random.value <= 0.5f ? GameManager.Instance.RewardManager.GunsLootTable : GameManager.Instance.RewardManager.ItemsLootTable);
            SpawnedBlack.RegisterChestOnMinimap(SpawnedBlack.GetAbsoluteParentRoom());
            //GIVE KEYS
            for (int i = 0; i < 10; i++) LootEngine.GivePrefabToPlayer(PickupObjectDatabase.GetById(67).gameObject, user);
        }
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
