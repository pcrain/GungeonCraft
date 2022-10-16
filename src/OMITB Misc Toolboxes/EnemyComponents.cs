using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Gungeon;

namespace NevernamedsItems
{
    public class SpecialSizeStatModification : MonoBehaviour
    {
        public SpecialSizeStatModification()
        {
            adjustsSpeed = true;
        }
        private void Start()
        {
            this.baseEnemy = base.GetComponent<AIActor>();
            if (baseEnemy != null)
            {
                if (baseEnemy.specRigidbody)
                {
                    baseEnemy.specRigidbody.OnPreRigidbodyCollision += preCollide;
                }
            }
        }
        private void preCollide(SpeculativeRigidbody myRigidbody, PixelCollider myCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherCollider)
        {
            if (baseEnemy && baseEnemy.EnemyScale.x < 0.55f)
            {
                if (otherRigidbody.GetComponent<PlayerController>())
                {
                    baseEnemy.EraseFromExistenceWithRewards(false);
                    GlobalSparksDoer.DoRandomParticleBurst(5, baseEnemy.specRigidbody.UnitBottomLeft, baseEnemy.specRigidbody.UnitTopRight, new Vector3(1, 1, 1), 360, 4, null, null, null, GlobalSparksDoer.SparksType.BLOODY_BLOOD);
                }
            }
        }
        private void Update()
        {
            if (baseEnemy)
            {
                if (adjustsSpeed)
                {
                    if (baseEnemy.MovementSpeed != baseEnemy.BaseMovementSpeed * (1 / baseEnemy.EnemyScale.x))
                    {
                        baseEnemy.MovementSpeed = baseEnemy.BaseMovementSpeed * (1 / baseEnemy.EnemyScale.x);
                    }
                }
                if (baseEnemy.EnemyScale.x < 0.55f && baseEnemy.CollisionDamage > 0)
                {
                    cachedCollisionDamage = baseEnemy.CollisionDamage;
                    baseEnemy.CollisionDamage = 0;
                }
                if (baseEnemy.EnemyScale.x > 0.55f && baseEnemy.CollisionDamage <= 0 && cachedCollisionDamage > 0)
                {
                    baseEnemy.CollisionDamage = cachedCollisionDamage;
                }

            }
        }
        private AIActor baseEnemy;
        private float cachedCollisionDamage;
        public bool canBeSteppedOn;
        public bool adjustsSpeed;
    } //For small and large enemies to make them faster or slower or step-on-able
}
