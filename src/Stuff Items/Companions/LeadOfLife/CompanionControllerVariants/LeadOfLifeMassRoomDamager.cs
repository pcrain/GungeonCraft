using Alexandria.Misc;
using Dungeonator;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NevernamedsItems
{
    class LeadOfLifeMassRoomDamager : LeadOfLifeCompanion
    {
        public static LeadOfLifeMassRoomDamager AddToPrefab(GameObject prefab,
            int itemID,
            float moveSpeed = 5,
            List<MovementBehaviorBase> movementBehaviors = null
            )
        {
            LeadOfLifeMassRoomDamager addedInstance = prefab.AddComponent<LeadOfLifeMassRoomDamager>();
            addedInstance.aiActor.MovementSpeed = moveSpeed;
            addedInstance.tiedItemID = itemID;

            addedInstance.aiAnimator.GetDirectionalAnimation("idle").Prefix = "idle";
            if (addedInstance.aiAnimator.GetDirectionalAnimation("move") != null) addedInstance.aiAnimator.GetDirectionalAnimation("move").Prefix = "run";
            BehaviorSpeculator component = prefab.GetComponent<BehaviorSpeculator>();
            if (movementBehaviors == null) component.MovementBehaviors.AddRange(new List<MovementBehaviorBase>() { new CustomCompanionBehaviours.LeadOfLifeCompanionApproach(), new CompanionFollowPlayerBehavior { IdleAnimations = new string[] { "idle" } } });
            else component.MovementBehaviors.AddRange(movementBehaviors);

            return addedInstance;
        }
        public LeadOfLifeMassRoomDamager()
        {
            targetType = RoomHandler.ActiveEnemyType.All;
            roomDamageAmount = 5f;

            //Blank Variables
            doesBlank = false;
            blankType = EasyBlankType.MINI;
            randomChanceToAlternateBlankType = 0;
            chanceToFailBlank = 0f;
        }
        public override void Attack()
        {
            if (base.aiActor.CenterPosition.GetAbsoluteRoom()!= null)
            {
                List<AIActor> enemies = base.aiActor.CenterPosition.GetAbsoluteRoom().GetActiveEnemies(targetType);
                if (enemies != null && enemies.Count > 0)
                {
                    foreach (AIActor actor in enemies)
                    {
                        if (actor && actor.healthHaver)
                        {
                            actor.healthHaver.ApplyDamage(roomDamageAmount, Vector2.zero, "Lead of Life Buddy");
                            OnMassDamagedEnemy();
                        }
                    }
                }
            }
            if (doesBlank)
            {
                if (UnityEngine.Random.value <= chanceToFailBlank)
                {
                    base.Attack();
                }
                else
                {
                    EasyBlankType type = blankType;
                    if (UnityEngine.Random.value <= randomChanceToAlternateBlankType)
                    {
                        if (type == EasyBlankType.FULL) { type = EasyBlankType.MINI; }
                        else type = EasyBlankType.FULL;
                    }
                    Owner.DoEasyBlank(base.specRigidbody.UnitCenter, type);

                    timesAttacked++;
                }
            }
            timesAttacked++;
            base.Attack();
        }
        public virtual void OnMassDamagedEnemy() { }

        public RoomHandler.ActiveEnemyType targetType;
        public float roomDamageAmount;

        //Blank Variables
        public bool doesBlank;
        public float chanceToFailBlank;
        public float randomChanceToAlternateBlankType;
        public EasyBlankType blankType;
    }
}
