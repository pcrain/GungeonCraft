using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NevernamedsItems
{
    class LeadOfLifeProximityShooter : LeadOfLifeBasicShooter
    {
        public static LeadOfLifeProximityShooter AddToPrefab(GameObject prefab,
           int itemID,
           float moveSpeed = 5,
           List<MovementBehaviorBase> movementBehaviors = null
           )
        {
            LeadOfLifeProximityShooter addedInstance = prefab.AddComponent<LeadOfLifeProximityShooter>();
            addedInstance.aiActor.MovementSpeed = moveSpeed;
            addedInstance.tiedItemID = itemID;

            addedInstance.aiAnimator.GetDirectionalAnimation("idle").Prefix = "idle";
            if (addedInstance.aiAnimator.GetDirectionalAnimation("move") != null) addedInstance.aiAnimator.GetDirectionalAnimation("move").Prefix = "run";
            BehaviorSpeculator component = prefab.GetComponent<BehaviorSpeculator>();
            if (movementBehaviors == null) component.MovementBehaviors.AddRange(new List<MovementBehaviorBase>() { new CustomCompanionBehaviours.LeadOfLifeCompanionApproach(), new CompanionFollowPlayerBehavior { IdleAnimations = new string[] { "idle" } } });
            else component.MovementBehaviors.AddRange(movementBehaviors);

            return addedInstance;
        }

        public bool scaleProxDistant;
        public bool scaleProxClose;
        public override void PostProcessProjectile(Projectile projectile)
        {
            float distance = Vector2.Distance(base.specRigidbody.UnitCenter, Owner.CenterPosition);
            if (scaleProxDistant)
            {
                float multiplier = (distance * 0.025f) + 1;
                projectile.baseData.damage *= multiplier;
                projectile.AdditionalScaleMultiplier *= multiplier;
            }
            if (scaleProxClose)
            {
                float distReduc = ((distance * 4) * 0.01f);
                float multiplier = 1 - distReduc;
                if (multiplier <= 0) multiplier = 0.001f;
                projectile.baseData.damage *= multiplier;
                projectile.AdditionalScaleMultiplier *= multiplier;
            }
            base.PostProcessProjectile(projectile);
        }
    }
}
