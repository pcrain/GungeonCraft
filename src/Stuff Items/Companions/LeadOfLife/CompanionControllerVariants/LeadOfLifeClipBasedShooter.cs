using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NevernamedsItems
{
    class LeadOfLifeClipBasedShooter : LeadOfLifeBasicShooter
    {
        public static LeadOfLifeClipBasedShooter AddToPrefab(GameObject prefab,
           int itemID,
           float moveSpeed = 5,
           List<MovementBehaviorBase> movementBehaviors = null
           )
        {
            LeadOfLifeClipBasedShooter addedInstance = prefab.AddComponent<LeadOfLifeClipBasedShooter>();
            addedInstance.aiActor.MovementSpeed = moveSpeed;
            addedInstance.tiedItemID = itemID;

            addedInstance.aiAnimator.GetDirectionalAnimation("idle").Prefix = "idle";
            if (addedInstance.aiAnimator.GetDirectionalAnimation("move") != null) addedInstance.aiAnimator.GetDirectionalAnimation("move").Prefix = "run";
            BehaviorSpeculator component = prefab.GetComponent<BehaviorSpeculator>();
            if (movementBehaviors == null) component.MovementBehaviors.AddRange(new List<MovementBehaviorBase>() { new CustomCompanionBehaviours.LeadOfLifeCompanionApproach(), new CompanionFollowPlayerBehavior { IdleAnimations = new string[] { "idle" } } });
            else component.MovementBehaviors.AddRange(movementBehaviors);

            return addedInstance;
        }

        public bool isAlpha;
        public bool isOmega;
        public override void PostProcessProjectile(Projectile projectile)
        {
            if (isAlpha)
            {
                if (Owner.CurrentGun && (Owner.CurrentGun.ClipShotsRemaining == Owner.CurrentGun.ClipCapacity))
                {
                    projectile.baseData.damage *= 2;
                    projectile.AdditionalScaleMultiplier *= 1.25f;
                }
            }
            else if (isOmega)
            {
                if (Owner.CurrentGun && Owner.CurrentGun.ClipShotsRemaining == 1)
                {

                    projectile.baseData.damage *= 2;
                    projectile.AdditionalScaleMultiplier *= 1.25f;
                }
            }
            base.PostProcessProjectile(projectile);
        }
    }
}
