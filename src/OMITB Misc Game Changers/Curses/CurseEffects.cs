using Alexandria.Misc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Alexandria.ItemAPI;

namespace NevernamedsItems
{
    public static class CurseEffects
    {
        public static void Init()
        {
            CustomActions.OnAnyHealthHaverDie += AnyHealthHaverKilled;
        }

        public static void AnyHealthHaverKilled(HealthHaver target)
        {
            if (target && target.aiActor && target.specRigidbody)
            {
                ETGMod.StartGlobalCoroutine(
                    TriggerCurseDeathEffects(
                       target.specRigidbody.UnitCenter,
                       target.aiActor.EnemyGuid,
                       target.healthHaver.GetMaxHealth(),
                       target.aiActor.IsBlackPhantom,
                       target.aiActor.CanTargetEnemies && !target.aiActor.CanTargetPlayers
                       ));
            }
        }
        public static IEnumerator TriggerCurseDeathEffects(Vector2 position, string guid, float maxHP, bool isJammed, bool isCharmed)
        {         
            if (CurseManager.CurseIsActive("Curse of Infestation"))
            {
                if (maxHP > 10)
                {
                    int max;
                    if (maxHP < 30) max = 2;
                    else if (maxHP > 30 && maxHP < 50) max = 3;
                    else max = 4;
                    int amt = UnityEngine.Random.Range(-1, max);
                    if (amt > 0)
                    {
                        for (int i = 0; i < amt; i++)
                        {
                            string batGUID = BraveUtility.RandomElement(AlexandriaTags.GetAllEnemyGuidsWithTag("small_bullat"));
                            if (GameManager.Instance.AnyPlayerHasActiveSynergy("The Last Crusade") || isCharmed)
                            {
                                AIActor targetActor = CompanionisedEnemyUtility.SpawnCompanionisedEnemy(GameManager.Instance.PrimaryPlayer, batGUID, position.ToIntVector2(), false, Color.red, 5, 2, false, false);
                                PhysicsEngine.Instance.RegisterOverlappingGhostCollisionExceptions(targetActor.specRigidbody, null, false);
                            }
                            else
                            {
                                var enemyToSpawn = EnemyDatabase.GetOrLoadByGuid(batGUID);
                                AIActor TargetActor = AIActor.Spawn(enemyToSpawn, position, GameManager.Instance.Dungeon.data.GetAbsoluteRoomFromPosition(position.ToIntVector2()), true, AIActor.AwakenAnimationType.Default, true);
                                PhysicsEngine.Instance.RegisterOverlappingGhostCollisionExceptions(TargetActor.specRigidbody, null, false);
                                TargetActor.PreventBlackPhantom = true;
                            }
                        }
                    }
                }
            }
            if (CurseManager.CurseIsActive("Curse of Sludge"))
            {
                DoCurseGoopCircle(EasyGoopDefinitions.EnemyFriendlyPoisonGoop, EasyGoopDefinitions.PlayerFriendlyPoisonGoop, position, maxHP, isJammed);
            }
            if (CurseManager.CurseIsActive("Curse of The Hive"))
            {
                DoCurseGoopCircle(EasyGoopDefinitions.HoneyGoop, EasyGoopDefinitions.PlayerFriendlyHoneyGoop, position, maxHP, isJammed);
            }
            yield break;
        }
        public static void DoCurseGoopCircle(GoopDefinition def, GoopDefinition crusadeGoop, Vector2 pos, float maxHP, bool isJammed)
        {
            if (maxHP > 0)
            {
                DeadlyDeadlyGoopManager goop = null;
                if (GameManager.Instance.AnyPlayerHasActiveSynergy("The Last Crusade")) goop = DeadlyDeadlyGoopManager.GetGoopManagerForGoopType(crusadeGoop);
                else goop = DeadlyDeadlyGoopManager.GetGoopManagerForGoopType(def);
                float hpMod = maxHP;
                if (isJammed) hpMod /= 3.5f;
                hpMod /= AIActor.BaseLevelHealthModifier;
                float radius = Math.Min((hpMod / 7.5f), 6);
                goop.TimedAddGoopCircle(pos, radius, 0.75f, true);
            }
        }
    }
}
