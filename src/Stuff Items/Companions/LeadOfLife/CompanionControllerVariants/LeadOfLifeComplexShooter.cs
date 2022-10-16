using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NevernamedsItems
{
    class LeadOfLifeComplexShooter : LeadOfLifeBasicShooter
    {
        public static LeadOfLifeComplexShooter AddToPrefab(GameObject prefab,
           int itemID,
           float moveSpeed = 5,
           List<MovementBehaviorBase> movementBehaviors = null
           )
        {
            LeadOfLifeComplexShooter addedInstance = prefab.AddComponent<LeadOfLifeComplexShooter>();
            addedInstance.aiActor.MovementSpeed = moveSpeed;
            addedInstance.tiedItemID = itemID;

            addedInstance.aiAnimator.GetDirectionalAnimation("idle").Prefix = "idle";
            if (addedInstance.aiAnimator.GetDirectionalAnimation("move") != null) addedInstance.aiAnimator.GetDirectionalAnimation("move").Prefix = "run";
            BehaviorSpeculator component = prefab.GetComponent<BehaviorSpeculator>();
            if (movementBehaviors == null) component.MovementBehaviors.AddRange(new List<MovementBehaviorBase>() { new CustomCompanionBehaviours.LeadOfLifeCompanionApproach(), new CompanionFollowPlayerBehavior { IdleAnimations = new string[] { "idle" } } });
            else component.MovementBehaviors.AddRange(movementBehaviors);

            return addedInstance;
        }
        public LeadOfLifeComplexShooter()
        {
            orbital = false;
            platinum = false;
        }

        public override void PostProcessProjectile(Projectile projectile)
        {
            if (orbital && base.specRigidbody)
            {
                OrbitalBulletsBehaviour orbital = projectile.gameObject.GetOrAddComponent<OrbitalBulletsBehaviour>();
                orbital.usesOverrideCenter = true;
                orbital.overrideCenter = base.specRigidbody;
                orbital.orbitalGroup = 2;
            }
            if (platinum && correspondingItem != null && correspondingItem.GetComponent<PlatinumBulletsItem>() != null)
            {
                PlatinumBulletsItem platBulletsItem = correspondingItem.GetComponent<PlatinumBulletsItem>();
                projectile.baseData.damage *= Mathf.Min(platBulletsItem.MaximumDamageMultiplier, 1f + platBulletsItem.m_totalBulletsFiredNormalizedByFireRate / platBulletsItem.ShootSecondsPerDamageDouble);
            }
            base.PostProcessProjectile(projectile);
        }
        public override float ModifyCooldown(float originalCooldown)
        {
            if (platinum && correspondingItem != null && correspondingItem.GetComponent<PlatinumBulletsItem>() != null)
            {
                PlatinumBulletsItem platBulletsItem = correspondingItem.GetComponent<PlatinumBulletsItem>();
                return originalCooldown / Mathf.Min(platBulletsItem.MaximumRateOfFireMultiplier, 1f + platBulletsItem.m_totalBulletsFiredNormalizedByFireRate / platBulletsItem.ShootSecondsPerRateOfFireDouble);
            }
            return base.ModifyCooldown(originalCooldown);
        }
        public bool orbital;
        public bool platinum;
    }
}
