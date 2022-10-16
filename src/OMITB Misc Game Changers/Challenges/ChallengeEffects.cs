using Alexandria.Misc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NevernamedsItems
{
    public static class ChallengeEffects
    {

    }
    /*
      if (enemy && enemy.healthHaver)
            {
                if (enemy.GetComponent<SteedBlobController>() == null)
                {
                    var steedPrefab = EnemyDatabase.GetOrLoadByGuid(EnemyGuidDatabase.Entries["blobulon"]);
                    if (enemy.healthHaver.GetMaxHealth() <= 12) steedPrefab = EnemyDatabase.GetOrLoadByGuid(EnemyGuidDatabase.Entries["blobuloid"]);
                    AIActor steed = AIActor.Spawn(steedPrefab.aiActor, enemy.Position, GameManager.Instance.Dungeon.data.GetAbsoluteRoomFromPosition(((Vector2)enemy.Position).ToIntVector2()), true, AIActor.AwakenAnimationType.Default, true);

                    PhysicsEngine.Instance.RegisterOverlappingGhostCollisionExceptions(steed.specRigidbody, null, false);
                    steed.gameObject.AddComponent<KillOnRoomClear>();
                    SteedBlobController steedController = steed.gameObject.AddComponent<SteedBlobController>();
                    if (steed.gameObject.GetComponent<SpawnEnemyOnDeath>()) UnityEngine.Object.Destroy(steed.gameObject.GetComponent<SpawnEnemyOnDeath>());

                    if (enemy.IsFlying) steed.SetIsFlying(true, "flyingSteed", true, true);

                    if (enemy.GetComponent<KillOnRoomClear>()) steed.gameObject.GetOrAddComponent<KillOnRoomClear>();
                    if (enemy.IgnoreForRoomClear) steed.IgnoreForRoomClear = true;

                    steed.healthHaver.SetHealthMaximum((steed.healthHaver.GetMaxHealth() * 0.8f), null, true);
                    steed.MovementSpeed *= 1.2f;

                    steedController.riderSpeedToRestore = enemy.MovementSpeed;
                    steedController.riderKnockbackToRestore = enemy.knockbackDoer.knockbackMultiplier;
                    enemy.MovementSpeed = 0;
                    enemy.knockbackDoer.knockbackMultiplier = 0;

                    steedController.rider = enemy;
                }
            }*/
}
