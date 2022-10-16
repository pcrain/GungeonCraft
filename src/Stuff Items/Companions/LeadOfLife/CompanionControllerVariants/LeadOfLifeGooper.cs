using Alexandria.Misc;
using Dungeonator;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NevernamedsItems
{
    class LeadOfLifeGooper : LeadOfLifeCompanion
    {
        public static LeadOfLifeGooper AddToPrefab(GameObject prefab,
            int itemID,
            float moveSpeed = 5,
            List<MovementBehaviorBase> movementBehaviors = null
            )
        {
            LeadOfLifeGooper addedInstance = prefab.AddComponent<LeadOfLifeGooper>();
            addedInstance.aiActor.MovementSpeed = moveSpeed;
            addedInstance.tiedItemID = itemID;

            addedInstance.aiAnimator.GetDirectionalAnimation("idle").Prefix = "idle";
            if (addedInstance.aiAnimator.GetDirectionalAnimation("move") != null) addedInstance.aiAnimator.GetDirectionalAnimation("move").Prefix = "run";
            BehaviorSpeculator component = prefab.GetComponent<BehaviorSpeculator>();
            if (movementBehaviors == null) component.MovementBehaviors.AddRange(new List<MovementBehaviorBase>() { new CustomCompanionBehaviours.LeadOfLifeCompanionApproach(), new CompanionFollowPlayerBehavior { IdleAnimations = new string[] { "idle" } } });
            else component.MovementBehaviors.AddRange(movementBehaviors);

            return addedInstance;
        }
        public LeadOfLifeGooper()
        {
            doGoopCircle = false;
            goopRadiusOrWidth = 1;
        }
        public override void Attack()
        {
            if (goopDefToSpawn)
            {
                if (doGoopCircle)
                {
                    DeadlyDeadlyGoopManager.GetGoopManagerForGoopType(goopDefToSpawn).TimedAddGoopCircle(base.specRigidbody.UnitCenter, goopRadiusOrWidth, 0.5f, false);
                }
                else
                {
                    DeadlyDeadlyGoopManager.GetGoopManagerForGoopType(goopDefToSpawn).TimedAddGoopLine(base.specRigidbody.UnitCenter, base.sprite.WorldCenter.GetPositionOfNearestEnemy(ActorCenter.RIGIDBODY), goopRadiusOrWidth, 0.5f);
                }
            }
            timesAttacked++;
            base.Attack();
        }
        public bool doGoopCircle;
        public GoopDefinition goopDefToSpawn;
        public float goopRadiusOrWidth;
    }
}
