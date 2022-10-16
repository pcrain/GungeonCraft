using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NevernamedsItems
{
    class LeadOfLifeCompanionCountReactiveShooter : LeadOfLifeBasicShooter
    {
        public static LeadOfLifeCompanionCountReactiveShooter AddToPrefab(GameObject prefab,
           int itemID,
           float moveSpeed = 5,
           List<MovementBehaviorBase> movementBehaviors = null
           )
        {
            LeadOfLifeCompanionCountReactiveShooter addedInstance = prefab.AddComponent<LeadOfLifeCompanionCountReactiveShooter>();
            addedInstance.aiActor.MovementSpeed = moveSpeed;
            addedInstance.tiedItemID = itemID;

            addedInstance.aiAnimator.GetDirectionalAnimation("idle").Prefix = "idle";
            if (addedInstance.aiAnimator.GetDirectionalAnimation("move") != null) addedInstance.aiAnimator.GetDirectionalAnimation("move").Prefix = "run";
            BehaviorSpeculator component = prefab.GetComponent<BehaviorSpeculator>();
            if (movementBehaviors == null) component.MovementBehaviors.AddRange(new List<MovementBehaviorBase>() { new CustomCompanionBehaviours.LeadOfLifeCompanionApproach(), new CompanionFollowPlayerBehavior { IdleAnimations = new string[] { "idle" } } });
            else component.MovementBehaviors.AddRange(movementBehaviors);

            return addedInstance;
        }
        public override void PostProcessProjectile(Projectile projectile)
        {
            if (baseLeadOfLife)
            {
                int numberOfOtherCompanions = (baseLeadOfLife.extantCompanions.Count - 1);
                if (bashfulShot)
                {
                    float multiplier = (-0.03f * numberOfOtherCompanions) + 2;
                    multiplier = Mathf.Max(1, multiplier);
                    projectile.baseData.damage *= multiplier;
                }
                if (crowdedClip)
                {
                    float multiplier = (0.01f * numberOfOtherCompanions) + 1;
                    projectile.baseData.damage *= multiplier;
                }
            }
            base.PostProcessProjectile(projectile);
        }
        public override float ModifyCooldown(float originalCooldown)
        {
            if (baseLeadOfLife)
            {
                int numberOfOtherCompanions = (baseLeadOfLife.extantCompanions.Count - 1);
                if (bashfulShot)
                {
                    float multiplier = (-0.03f * numberOfOtherCompanions) + 2;
                    multiplier = Mathf.Max(1, multiplier);
                    return originalCooldown / multiplier;
                }
            }
            return base.ModifyCooldown(originalCooldown);
        }
        public bool crowdedClip;
        public bool bashfulShot;
    }
}
