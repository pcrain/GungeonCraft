/*using UnityEngine;
using ItemAPI;
using Dungeonator;
using System.Collections;
using System;
using MonoMod.RuntimeDetour;
using System.Reflection;
using Gungeon;
using System.Collections.Generic;

namespace NevernamedsItems
{
    public class LoopingMode : ETGModule
    {
        public static int timesLooped = 0;
        public static bool loopingActive;
        public override void Exit()
        { 
        }
        public override void Init()
        { 
        }
        public override void Start()
        {
            ETGModConsole.Log("[NN] Looping mode at least fucking tried.");
            try
            {
                timesLooped = 0;
                loopingActive = false;
                GameManager.Instance.OnNewLevelFullyLoaded += this.OnNewFloor;
                ETGModConsole.Log("[NN] Looping mode correctly initialised.");
            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message);
            }
        }
        Hook RestartHook = new Hook(
    typeof(GameManager).GetMethod("QuickRestart", BindingFlags.Instance | BindingFlags.Public),
    typeof(LoopingMode).GetMethod("TurnOffEffects", BindingFlags.Static | BindingFlags.Public),
    typeof(GameManager)
            );
        public static void TurnOffEffects(QuickRestartOptions options = default(QuickRestartOptions))
        {

            timesLooped = 0;
            ETGModConsole.Log("[NN] Looping mode correctly reset itself");
        }
        private void OnNewFloor()
        {
            ETGModConsole.Log("[NN] New floor entered.");
            PlayerController player = GameManager.Instance.PrimaryPlayer;
            if (player != null)
            {
                player.OnAnyEnemyReceivedDamage += this.HandleLichLoop;
            }
            else
            {
                ETGModConsole.Log("[NN] Player was somehow... Null?");
            }
        }
        private void HandleLichLoop(float damage, bool fatal, HealthHaver enemy)
        {
            try
            {
                if (enemy.aiActor.EnemyGuid == "7c5d5f09911e49b78ae644d2b50ff3bf" && loopingActive == true)
                {
                    ETGModConsole.Log("[NN] Lich took damage.");
                    PlayerController player = GameManager.Instance.PrimaryPlayer;
                    //float currentCurse = player.stats.GetBaseStatValue(PlayerStats.StatType.Curse);
                    //player.stats.SetBaseStatValue(PlayerStats.StatType.Curse, currentCurse + 1f, player);
                    float currentBulletSpeed = player.stats.GetBaseStatValue(PlayerStats.StatType.EnemyProjectileSpeedMultiplier);
                    player.stats.SetBaseStatValue(PlayerStats.StatType.EnemyProjectileSpeedMultiplier, currentBulletSpeed * 1.05f, player);
                    ETGMod.AIActor.OnPreStart += HandleLoopMods;
                    player.OnEnteredCombat += this.HandleBonusEnemySpawns;
                    timesLooped += 1;
                    GameManager.Instance.LoadCustomLevel("tt_castle");
                    ETGModConsole.Log("[NN] Keep Loaded.");
                }
                else if (loopingActive)
                {
                    ETGModConsole.Log("[NN] That wasn't the lich.");
                }
            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message);
            }
        }

        private void HandleBonusEnemySpawns()
        {
            if (timesLooped > 0)
            {
                PlayerController player = GameManager.Instance.PrimaryPlayer;
                float chanceForBonusEnemy = 0.05f;
                int maxAmountOfBonusEnemies = 1;
                chanceForBonusEnemy *= timesLooped;
                maxAmountOfBonusEnemies *= timesLooped;

                int actualAmountOfBonusEnemies = UnityEngine.Random.Range(1, maxAmountOfBonusEnemies + 1);

                for (int i = 0; i < actualAmountOfBonusEnemies; i++)
                {
                    if (chanceForBonusEnemy > UnityEngine.Random.value)
                    {
                        var guid = BraveUtility.RandomElement(bonusEnemyPool);
                        IntVector2? spawnPos = player.CurrentRoom.GetRandomVisibleClearSpot(1, 1);
                        var BonusEnemyGuid = EnemyDatabase.GetOrLoadByGuid(guid);
                        AIActor BonusEnemy = AIActor.Spawn(BonusEnemyGuid.aiActor, spawnPos.Value, GameManager.Instance.Dungeon.data.GetAbsoluteRoomFromPosition(spawnPos.Value), true, AIActor.AwakenAnimationType.Default, true);
                        BonusEnemy.HandleReinforcementFallIntoRoom(0f);
                    }
                }
            }
        }
        private void HandleLoopMods(AIActor enemy)
        {
            if (timesLooped > 0)
            {
                if (enemy.CompanionOwner != null)
                {
                    ETGModConsole.Log("A companion spawned");
                    return;
                }
                //Modify Enemy Health Scaling
                float enemyHealthMod = 0.3f;
                enemyHealthMod *= timesLooped;
                enemyHealthMod += 1f;
                float currentEnemyHealth = enemy.healthHaver.GetMaxHealth();
                currentEnemyHealth *= enemyHealthMod;
                enemy.healthHaver.SetHealthMaximum(currentEnemyHealth);

                //Handle Clone Enemy Spawning
                float enemyCloneChance = 0.05f;
                enemyCloneChance *= timesLooped;
                if (!enemy.healthHaver.IsBoss && !enemy.IsMimicEnemy && enemyCloneChance > UnityEngine.Random.value)
                {
                    if (!string.IsNullOrEmpty(enemy.aiActor.EnemyGuid))
                    {
                        if (enemyCloneBlacklist.Contains(enemy.aiActor.EnemyGuid))
                        {
                            return;
                        }
                        else
                        {
                            var enemyParentRoom = enemy.GetAbsoluteParentRoom();
                            IntVector2? spawnPos = enemyParentRoom.GetRandomVisibleClearSpot(1, 1);
                            AIActor EnemyClone = AIActor.Spawn(enemy.aiActor, spawnPos.Value, GameManager.Instance.Dungeon.data.GetAbsoluteRoomFromPosition(spawnPos.Value), true, AIActor.AwakenAnimationType.Default, true);
                            EnemyClone.HandleReinforcementFallIntoRoom(0f);
                        }
                    }
                }
            }
        }
        public static List<string> enemyCloneBlacklist = new List<string>()
        {
            EnemyGuidDatabase.Entries["spent"],
            EnemyGuidDatabase.Entries["gummy_spent"],
        };
        public static List<string> bonusEnemyPool = new List<string>()
        {
            EnemyGuidDatabase.Entries["gun_nut"],
            EnemyGuidDatabase.Entries["chain_gunner"],
            EnemyGuidDatabase.Entries["spectral_gun_nut"],
            EnemyGuidDatabase.Entries["lead_maiden"],
            EnemyGuidDatabase.Entries["shotgrub"],
            EnemyGuidDatabase.Entries["agonizer"],
            EnemyGuidDatabase.Entries["shambling_round"],
            EnemyGuidDatabase.Entries["great_bullet_shark"],
            EnemyGuidDatabase.Entries["spogre"],
            EnemyGuidDatabase.Entries["titan_bullet_kin"],
            EnemyGuidDatabase.Entries["killithid"],
            EnemyGuidDatabase.Entries["spent"],
            EnemyGuidDatabase.Entries["misfire_beast"],
            EnemyGuidDatabase.Entries["phaser_spider"],
            EnemyGuidDatabase.Entries["jamerlengo"],
            EnemyGuidDatabase.Entries["chancebulon"],
        };


    }
}*/
