using Dungeonator;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NevernamedsItems
{
    class LeadOfLifeExploder : LeadOfLifeCompanion
    {
        public static LeadOfLifeExploder AddToPrefab(GameObject prefab,
            int itemID,
            float moveSpeed = 5,
            List<MovementBehaviorBase> movementBehaviors = null
            )
        {
            LeadOfLifeExploder addedInstance = prefab.AddComponent<LeadOfLifeExploder>();
            addedInstance.aiActor.MovementSpeed = moveSpeed;
            addedInstance.tiedItemID = itemID;

            addedInstance.aiAnimator.GetDirectionalAnimation("idle").Prefix = "idle";
            if (addedInstance.aiAnimator.GetDirectionalAnimation("move") != null) addedInstance.aiAnimator.GetDirectionalAnimation("move").Prefix = "run";
            BehaviorSpeculator component = prefab.GetComponent<BehaviorSpeculator>();
            if (movementBehaviors == null) component.MovementBehaviors.AddRange(new List<MovementBehaviorBase>() { new CustomCompanionBehaviours.LeadOfLifeCompanionApproach(), new CompanionFollowPlayerBehavior { IdleAnimations = new string[] { "idle" } } });
            else component.MovementBehaviors.AddRange(movementBehaviors);

            return addedInstance;
        }
        public LeadOfLifeExploder()
        {

        }
        public override void Attack()
        {
            if (doChainExplosion)
            {
                GameManager.Instance.Dungeon.StartCoroutine(this.DoKatanaBulletsChain(base.specRigidbody.UnitCenter, GetAngleToTarget()));
            }
            else
            {
                Exploder.Explode(base.specRigidbody.UnitCenter, explosion, Vector2.zero, null, false, CoreDamageTypes.None, false);
            }
            timesAttacked++;
            base.Attack();
        }
        private IEnumerator DoKatanaBulletsChain(Vector2 startPosition, Vector2 direction)
        {
            float perExplosionTime = chainExplosionDuration / (float)chainExplosionAmount;
            float[] explosionTimes = new float[chainExplosionAmount];
            explosionTimes[0] = 0f;
            explosionTimes[1] = perExplosionTime;
            for (int i = 2; i < chainExplosionAmount; i++) explosionTimes[i] = explosionTimes[i - 1] + perExplosionTime;

            Vector2 lastValidPosition = startPosition;
            bool hitWall = false;
            int index = 0;
            float elapsed = 0f;
            Vector2 currentDirection = direction;
            RoomHandler currentRoom = startPosition.GetAbsoluteRoom();
            while (elapsed < chainExplosionDuration)
            {
                elapsed += BraveTime.DeltaTime;
                while (index < chainExplosionAmount && elapsed >= explosionTimes[index])
                {
                    Vector2 vector = startPosition + currentDirection.normalized * chainExplosionDistance;
                    Vector2 vector2 = Vector2.Lerp(startPosition, vector, ((float)index + 1f) / (float)chainExplosionAmount);
                    if (!this.ValidPositionForKatanaSlash(vector2))
                    {
                        hitWall = true;
                    }
                    if (!hitWall)
                    {
                        lastValidPosition = vector2;
                    }
                    Exploder.Explode(lastValidPosition, explosion, currentDirection, null, false, CoreDamageTypes.None, false);
                    index++;
                }
                yield return null;
            }
            yield break;
        }
        private bool ValidPositionForKatanaSlash(Vector2 pos)
        {
            IntVector2 intVector = pos.ToIntVector2(VectorConversions.Floor);
            return GameManager.Instance.Dungeon.data.CheckInBoundsAndValid(intVector) && GameManager.Instance.Dungeon.data[intVector].type != CellType.WALL;
        }

        public ExplosionData explosion;
        public bool doChainExplosion;
        public float chainExplosionDistance;
        public float chainExplosionDuration;
        public int chainExplosionAmount;
    }
}
