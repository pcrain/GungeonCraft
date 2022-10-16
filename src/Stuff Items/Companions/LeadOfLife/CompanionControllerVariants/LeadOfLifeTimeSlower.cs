using Dungeonator;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NevernamedsItems
{
    class LeadOfLifeTimeSlower : LeadOfLifeCompanion
    {
        public static LeadOfLifeTimeSlower AddToPrefab(GameObject prefab,
            int itemID,
            float moveSpeed = 5,
            List<MovementBehaviorBase> movementBehaviors = null
            )
        {
            LeadOfLifeTimeSlower addedInstance = prefab.AddComponent<LeadOfLifeTimeSlower>();
            addedInstance.aiActor.MovementSpeed = moveSpeed;
            addedInstance.tiedItemID = itemID;

            addedInstance.aiAnimator.GetDirectionalAnimation("idle").Prefix = "idle";
            if (addedInstance.aiAnimator.GetDirectionalAnimation("move") != null) addedInstance.aiAnimator.GetDirectionalAnimation("move").Prefix = "run";
            BehaviorSpeculator component = prefab.GetComponent<BehaviorSpeculator>();
            if (movementBehaviors == null) component.MovementBehaviors.AddRange(new List<MovementBehaviorBase>() { new CustomCompanionBehaviours.LeadOfLifeCompanionApproach(), new CompanionFollowPlayerBehavior { IdleAnimations = new string[] { "idle" } } });
            else component.MovementBehaviors.AddRange(movementBehaviors);

            return addedInstance;
        }
        public LeadOfLifeTimeSlower()
        {
            doesSepia = false;
            holdTime = 5f;
            timeModifier = 0.25f;
        }
        public override void Attack()
        {
            var timeSlow = new RadialSlowInterface();
            timeSlow.DoesSepia = doesSepia;
            timeSlow.RadialSlowHoldTime = holdTime;
            timeSlow.RadialSlowTimeModifier = timeModifier;
            timeSlow.DoRadialSlow(base.specRigidbody.UnitCenter, base.aiActor.Position.GetAbsoluteRoom());
            timesAttacked++;
            base.Attack();
        }
        public bool doesSepia;
        public float holdTime;
        public float timeModifier;
    }
}
