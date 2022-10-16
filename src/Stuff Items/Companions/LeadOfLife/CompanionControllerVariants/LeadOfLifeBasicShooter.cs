using Alexandria.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NevernamedsItems
{
    class LeadOfLifeBasicShooter : LeadOfLifeCompanion
    {
        public static LeadOfLifeBasicShooter AddToPrefab(GameObject prefab,
            int itemID,
            float moveSpeed = 5,
            List<MovementBehaviorBase> movementBehaviors = null
            )
        {
            LeadOfLifeBasicShooter addedInstance = prefab.AddComponent<LeadOfLifeBasicShooter>();
            addedInstance.aiActor.MovementSpeed = moveSpeed;
            addedInstance.tiedItemID = itemID;

            addedInstance.aiAnimator.GetDirectionalAnimation("idle").Prefix = "idle";
            if (addedInstance.aiAnimator.GetDirectionalAnimation("move") != null) addedInstance.aiAnimator.GetDirectionalAnimation("move").Prefix = "run";
            BehaviorSpeculator component = prefab.GetComponent<BehaviorSpeculator>();
            if (movementBehaviors == null) component.MovementBehaviors.AddRange(new List<MovementBehaviorBase>() { new CustomCompanionBehaviours.LeadOfLifeCompanionApproach(), new CompanionFollowPlayerBehavior { IdleAnimations = new string[] { "idle" } } });
            else component.MovementBehaviors.AddRange(movementBehaviors);

            return addedInstance;
        }
        public LeadOfLifeBasicShooter()
        {
            bulletsToFire = new List<Projectile>();
            angleVariance = 15f;
            multiShotsSequential = false;
            lastFiredSequential = 0;
        }
        public override void Attack()
        {
            if (multiShotsSequential)
            {
                FireBullets(bulletsToFire[lastFiredSequential]);
                lastFiredSequential++;
                if (lastFiredSequential > bulletsToFire.Count() - 1) lastFiredSequential = 0;
            }
            else { foreach (Projectile bulletPrefab in bulletsToFire) FireBullets(bulletPrefab); }
            timesAttacked++;
            base.Attack();
        }

        private void FireBullets(Projectile projectileToFire)
        {               
            if (base.aiActor.OverrideTarget != null)
            {
                Vector2 nearestEnemyPosition = base.aiActor.OverrideTarget.sprite != null ? base.aiActor.OverrideTarget.sprite.WorldCenter : base.aiActor.OverrideTarget.UnitCenter;

                GameObject gameObject = ProjSpawnHelper.SpawnProjectileTowardsPoint(projectileToFire.gameObject, base.sprite.WorldCenter, nearestEnemyPosition, 0, angleVariance);
                Projectile component = gameObject.GetComponent<Projectile>();
                if (component != null)
                {
                    component.Owner = Owner;
                    component.TreatedAsNonProjectileForChallenge = true;
                    component.Shooter = base.specRigidbody;
                    component.collidesWithPlayer = false;

                    component.OnHitEnemy += this.OnHitEnemy;

                    component.ApplyCompanionModifierToBullet(Owner);
                    PostProcessProjectile(component);
                }
            }
        }
        private void OnHitEnemy(Projectile bullet, SpeculativeRigidbody enemy, bool fatal)
        {
            LandedShotOnEnemy(enemy, bullet, fatal);
        }

        //Virtual voids for override
        public virtual void PostProcessProjectile(Projectile projectile)
        {

        }
        public virtual void LandedShotOnEnemy(SpeculativeRigidbody enemy, Projectile projectile, bool fatal)
        {

        }

        //Variables
        public List<Projectile> bulletsToFire;

        public float angleVariance;

        public bool multiShotsSequential;
        private int lastFiredSequential;
    }
}
