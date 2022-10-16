using Dungeonator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NevernamedsItems
{
    public class EnemyApproachingHopper : MovementBehaviorBase
    {
        public override void Init(GameObject gameObject, AIActor aiActor, AIShooter aiShooter)
        {
            isHopping = false;
            waitTimer = 0.5f;
            storedMovement = aiActor.MovementSpeed;
            base.Init(gameObject, aiActor, aiShooter);
        }
        public override void Upkeep()
        {
            base.Upkeep();
            if (isHopping) base.DecrementTimer(ref this.hopTimer, false);
            if (!isHopping) base.DecrementTimer(ref this.waitTimer, false);
        }

        public override BehaviorResult Update()
        {
            if (Owner == null)
            {
                if (this.m_aiActor && this.m_aiActor.CompanionOwner) Owner = this.m_aiActor.CompanionOwner;
                else Owner = GameManager.Instance.BestActivePlayer;
            }
            SpeculativeRigidbody overrideTarget = this.m_aiActor.OverrideTarget;


            if (isHopping)
            {

                if (hopTimer <= 0)
                {
                    Land();
                    return BehaviorResult.Continue;
                }
            }
            if (!isHopping && waitTimer <= 0)
            {
                return Hop();

            }
            return BehaviorResult.Continue;
        }

        private BehaviorResult Hop()
        {
            if (this.m_aiActor.OverrideTarget == null || (this.m_aiActor.OverrideTarget.healthHaver && this.m_aiActor.OverrideTarget.healthHaver.IsDead)) { this.PickNewTarget(); }
            if (isValid != null && this.m_aiActor.OverrideTarget && this.m_aiActor.OverrideTarget.aiActor != null && !isValid(this.m_aiActor.OverrideTarget.aiActor)) { this.PickNewTarget(); }

            if (Owner && Owner.IsInCombat && this.m_aiActor.OverrideTarget && this.m_aiActor.OverrideTarget.healthHaver && this.m_aiActor.OverrideTarget.healthHaver.IsAlive)
            {
                isHopping = true;
                this.m_aiActor.MovementSpeed = storedMovement;

                if (!string.IsNullOrEmpty(hopAnim) && base.m_aiAnimator) base.m_aiAnimator.PlayUntilFinished(hopAnim);

                if (onHopped != null) { onHopped(base.m_aiActor, base.m_aiActor.specRigidbody.UnitCenter); }

                this.m_aiActor.PathfindToPosition(this.m_aiActor.OverrideTarget.UnitCenter, null, true, null, null, null, false);
                hopTimer = airtime;
                return BehaviorResult.SkipRemainingClassBehaviors;
            }

            else if (Vector2.Distance(this.m_aiActor.CenterPosition, Owner.CenterPosition) > 4)
            {
                isHopping = true;
                this.m_aiActor.MovementSpeed = storedMovement;

                if (!string.IsNullOrEmpty(hopAnim) && base.m_aiAnimator) base.m_aiAnimator.PlayUntilFinished(hopAnim);
                if (onHopped != null) { onHopped(base.m_aiActor, base.m_aiActor.specRigidbody.UnitCenter); }


                this.m_aiActor.PathfindToPosition(Owner.CenterPosition, null, true, null, null, null, false);
                hopTimer = airtime;
                return BehaviorResult.SkipRemainingClassBehaviors;
            }
            return BehaviorResult.Continue;
        }
        private void Land()
        {
            isHopping = false;
            this.m_aiActor.MovementSpeed = 0;
            this.m_aiActor.ClearPath();
            if (onLanded != null) { onLanded(base.m_aiActor, base.m_aiActor.specRigidbody.UnitCenter); }
             if (Owner && Owner.IsInCombat && this.m_aiActor.OverrideTarget && this.m_aiActor.OverrideTarget.healthHaver && this.m_aiActor.OverrideTarget.healthHaver.IsAlive)
            {
                if (this.m_aiActor.specRigidbody.UnitCenter.GetAbsoluteRoom() == null || this.m_aiActor.OverrideTarget.UnitCenter.GetAbsoluteRoom() != this.m_aiActor.specRigidbody.UnitCenter.GetAbsoluteRoom()) waitTimer = outOfCombatWaitTime;
                else waitTimer = waitTime;
            }
            else waitTimer = outOfCombatWaitTime;
        }
        private void PickNewTarget()
        {
            if (this.m_aiActor != null)
            {
                if (this.Owner == null)
                {
                    if (this.m_aiActor && this.m_aiActor.CompanionOwner)
                    {
                        Owner = this.m_aiActor.CompanionOwner;
                    }
                    else
                    {
                        Owner = GameManager.Instance.BestActivePlayer;
                    }
                }
                this.Owner.CurrentRoom.GetActiveEnemies(RoomHandler.ActiveEnemyType.RoomClear, ref this.roomEnemies);
                for (int i = 0; i < this.roomEnemies.Count; i++)
                {
                    AIActor aiactor = this.roomEnemies[i];
                    if (aiactor.IsHarmlessEnemy || !aiactor.IsNormalEnemy || aiactor.healthHaver.IsDead || aiactor == this.m_aiActor || aiactor.EnemyGuid == "ba928393c8ed47819c2c5f593100a5bc") { this.roomEnemies.Remove(aiactor); }
                    if (isValid != null && !isValid(aiactor)) { this.roomEnemies.Remove(aiactor); }
                }
                if (this.roomEnemies.Count == 0) { this.m_aiActor.OverrideTarget = null; }
                else
                {
                    AIActor aiActor = this.m_aiActor;
                    AIActor aiactor2 = this.roomEnemies[UnityEngine.Random.Range(0, this.roomEnemies.Count)];
                    aiActor.OverrideTarget = ((aiactor2 != null) ? aiactor2.specRigidbody : null);
                }
            }
        }

        public Func<AIActor, bool> isValid;
        public Action<AIActor, Vector2> onLanded;
        public Action<AIActor, Vector2> onHopped;

        private float hopTimer;
        private float waitTimer;
        public float airtime = 0.5f;
        public float waitTime = 0.5f;
        public float outOfCombatWaitTime = 0.1f;
        private bool isHopping;
        private float storedMovement;
        public string hopAnim;
        private List<AIActor> roomEnemies = new List<AIActor>();
        private PlayerController Owner;
    }
}
