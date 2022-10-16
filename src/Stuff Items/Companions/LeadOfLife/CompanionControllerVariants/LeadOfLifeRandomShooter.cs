using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Alexandria.Misc;
using Alexandria.ItemAPI;

namespace NevernamedsItems
{
    class LeadOfLifeRandomShooter : LeadOfLifeCompanion
    {
        public static LeadOfLifeRandomShooter AddToPrefab(GameObject prefab,
            int itemID,
            float moveSpeed = 5,
            List<MovementBehaviorBase> movementBehaviors = null
            )
        {
            LeadOfLifeRandomShooter addedInstance = prefab.AddComponent<LeadOfLifeRandomShooter>();
            addedInstance.aiActor.MovementSpeed = moveSpeed;
            addedInstance.tiedItemID = itemID;

            addedInstance.aiAnimator.GetDirectionalAnimation("idle").Prefix = "idle";
            if (addedInstance.aiAnimator.GetDirectionalAnimation("move") != null) addedInstance.aiAnimator.GetDirectionalAnimation("move").Prefix = "run";
            BehaviorSpeculator component = prefab.GetComponent<BehaviorSpeculator>();
            if (movementBehaviors == null) component.MovementBehaviors.AddRange(new List<MovementBehaviorBase>() { new CustomCompanionBehaviours.LeadOfLifeCompanionApproach(), new CompanionFollowPlayerBehavior { IdleAnimations = new string[] { "idle" } } });
            else component.MovementBehaviors.AddRange(movementBehaviors);

            return addedInstance;
        }
        public LeadOfLifeRandomShooter()
        {
            angleVariance = 15f;
        }
        public override void Attack()
        {
            DoChanceBulletsSpawn();
            timesAttacked++;
            base.Attack();
        }
        private void DoChanceBulletsSpawn()
        {
            List<Projectile> ValidBullets = new List<Projectile>();
            List<Projectile> ValidBeams = new List<Projectile>();
            if (Owner && Owner.inventory != null)
            {
                for (int j = 0; j < Owner.inventory.AllGuns.Count; j++)
                {
                    if (Owner.inventory.AllGuns[j] && !Owner.inventory.AllGuns[j].InfiniteAmmo)
                    {
                        ProjectileModule defaultModule = Owner.inventory.AllGuns[j].DefaultModule;
                        if (defaultModule.shootStyle == ProjectileModule.ShootStyle.Beam)
                        {
                            ValidBeams.Add(defaultModule.GetCurrentProjectile());
                        }
                        else if (defaultModule.shootStyle == ProjectileModule.ShootStyle.Charged)
                        {
                            Projectile projectile = null;
                            for (int k = 0; k < 15; k++)
                            {
                                ProjectileModule.ChargeProjectile chargeProjectile = defaultModule.chargeProjectiles[UnityEngine.Random.Range(0, defaultModule.chargeProjectiles.Count)];
                                if (chargeProjectile != null) projectile = chargeProjectile.Projectile;
                                if (projectile) break;
                            }
                            ValidBullets.Add(projectile);
                        }
                        else
                        {
                            ValidBullets.Add(defaultModule.GetCurrentProjectile());
                        }
                    }
                }

                int listsCombined = ValidBullets.Count + ValidBeams.Count;
                if (listsCombined > 0)
                {
                    int randomSelection = UnityEngine.Random.Range(0, listsCombined);
                    if (randomSelection > ValidBullets.Count) //Beams
                    {
                        FireBeams(BraveUtility.RandomElement(ValidBeams));
                    }
                    else //Projectiles
                    {
                        FireBullets(BraveUtility.RandomElement(ValidBullets));
                    }
                }
                else
                {
                    FireBullets((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
                }
            }
        }
        private void FireBullets(Projectile projectileToFire)
        {
            AIActor nearestenemy = base.sprite.WorldCenter.GetNearestEnemyToPosition();
            if (nearestenemy)
            {
                GameObject gameObject = ProjSpawnHelper.SpawnProjectileTowardsPoint(projectileToFire.gameObject, base.sprite.WorldCenter, nearestenemy.Position, 0, angleVariance);
                Projectile component = gameObject.GetComponent<Projectile>();
                if (component != null)
                {
                    component.Owner = Owner;
                    component.TreatedAsNonProjectileForChallenge = true;
                    component.Shooter = base.specRigidbody;
                    component.collidesWithPlayer = false;
                    PostProcessProjectile(component);
                }
            }
        }
        private void FireBeams(Projectile beam)
        {
            BeamController createdbeam = BeamAPI.FreeFireBeamFromAnywhere(beam, Owner, base.gameObject, Vector2.zero,  GetAngleToFire().ToAngle(), 1, true);
            PostProcessBeam(createdbeam);
        }
        public Vector2 GetAngleToFire()
        {
            return (base.specRigidbody.UnitCenter.GetPositionOfNearestEnemy(ActorCenter.SPRITE) - base.specRigidbody.UnitCenter).normalized;
        }
        public virtual void PostProcessProjectile(Projectile projectile)
        {

        }
        public virtual void PostProcessBeam(BeamController beam)
        {

        }
        public List<Projectile> bulletsToFire;

        //Bullet Stats
        public float angleVariance;
    }
}
