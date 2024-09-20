namespace CwaffingTheGungy;

using System;
using static AllayCompanion.AllayMovementBehavior.State;

public class Allay : CwaffCompanion
{
    public static string ItemName         = "Allay";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    public static void Init()
    {
        PassiveItem item  = Lazy.SetupPassive<Allay>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.B;

        AllayCompanion friend = item.InitCompanion<AllayCompanion>(baseFps: 12);
        friend.MakeIntangible();
        friend.aiActor.MovementSpeed = 7f;
        friend.aiActor.HasShadow = false;

        string companionName = ItemName.ToID();
        BehaviorSpeculator bs = friend.gameObject.GetComponent<BehaviorSpeculator>();
        bs.MovementBehaviors.Add(new AllayCompanion.AllayMovementBehavior {
            IdleAnimations = [$"{companionName}_idle"],
            });
    }
}

public class AllayCompanion : CwaffCompanionController
{
    public class AllayMovementBehavior : MovementBehaviorBase
    {
        /* Behavior sketch:
          - spinning near a grounded item while allay is in follow mode makes allay pick up the nearest item and switch to scout mode
          - petting an allay in scout mode makes it drop its item and switch to follow mode
          - in scout mode, allay has a 10% chance to find a duplicate of its current item on room clear
            - each item found decreases successive item find chance to 9%, 8%, 7%, etc. down to 1%
          - allays cannot pick up enemies that are too large or are immune to pits
        */

        internal enum State {
            OWNER_FOLLOW,  // (default state) following the owner when no items or enemies are available
            ENEMY_SEEK,    // (follow mode, in combat) seeking an enemy to carry to a pit, if possible
            ENEMY_CARRY,   // (follow mode, in combat) carrying an enemy to the nearest pit
            ITEM_SEEK,     // (follow mode, out of combat) retrieving a grounded item for the player
            ITEM_RETRIEVE, // (follow mode, out of combat) bringing an item to the player
            ITEM_INSPECT,  // (follow mode, out of combat) moving towards an item to transition to scout mode
            ITEM_LOCATE,   // (scout mode) moving towards successfully located an item after combat ended
            ITEM_DANCE,    // (scout mode) circle around located item once near enough
        }

        public float PathInterval = 0.25f;
        public float IdealRadius = 3f;
        public string[] IdleAnimations;
        public string RollAnimation = "roll";

        [NonSerialized]
        public bool TemporarilyDisabled;

        private int m_sequentialPathFails;
        private float m_idleTimer = 2f;
        private float m_repathTimer;
        private CompanionController m_companionController;

        private GameActor _targetActor = null;
        private GameActor _heldActor = null;
        private PickupObject _targetItem = null;
        private PickupObject _heldItem = null;
        private Vector2 _targetPos = default;
        private State _state = OWNER_FOLLOW;
        private bool _scouting = false;
        private bool _hasValidTarget = false;

        public override void Start()
        {
            base.Start();
            m_companionController = m_gameObject.GetComponent<CompanionController>();
            m_aiActor.FallingProhibited = true;
            m_aiActor.PathableTiles |= CellTypes.PIT;
        }

        public override void Upkeep()
        {
            base.Upkeep();
            DecrementTimer(ref m_repathTimer);
        }

        private void UnsetTargets()
        {
            this._targetActor = null;
            this._targetItem = null;
            this._hasValidTarget = false;
        }

        private AIActor FindCarriableEnemy()
        {
            return null;
        }

        private PickupObject FindCarriableItem()
        {
            return null;
        }

        private void DetermineNewTarget()
        {
            PlayerController owner = m_companionController.m_owner;

            // set up a sane default state
            this._hasValidTarget = true;
            this._scouting = this._scouting && this._heldItem;
            this._heldActor = null;
            this._targetItem = null;
            this._targetActor = owner;
            this._state = OWNER_FOLLOW;

            if (this._scouting)
                return; // nothing to do in scout mode unless explicitly finding something after combat

            if (owner.IsInCombat)
            {
                if (FindCarriableEnemy() is not AIActor target)
                    return; // failed to find a carriable enemy

                this._state = ENEMY_SEEK;
                this._targetActor = target;
                return;
            }

            if (FindCarriableItem() is PickupObject item)
            {
                this._state = ITEM_SEEK;
                this._targetItem = item;
                return;
            }
        }

        private bool IsTargetValid()
        {
            switch(this._state)
            {
                case ENEMY_CARRY:
                    return this._heldActor && this._heldActor.healthHaver && this._heldActor.healthHaver.IsAlive;
                case OWNER_FOLLOW:
                case ENEMY_SEEK:
                case ITEM_RETRIEVE:
                    return this._targetActor && this._targetActor.healthHaver && this._targetActor.healthHaver.IsAlive;
                case ITEM_DANCE:
                case ITEM_SEEK:
                case ITEM_LOCATE:
                case ITEM_INSPECT:
                    return this._targetItem && this._targetItem.debris && this._targetItem.debris.onGround;
            }
            return false;
        }

        private Vector2 FindNearestPit()
        {
            return this._targetPos; //TODO: not implemented
        }

        private void UpdateStateAndTargetPosition()
        {
            if (!IsTargetValid())
                DetermineNewTarget();
            if (!this._hasValidTarget)
                return;

            switch(this._state)
            {
                case ENEMY_CARRY:
                    this._targetPos = FindNearestPit();
                    return;
                case OWNER_FOLLOW:
                case ENEMY_SEEK:
                case ITEM_RETRIEVE:
                    this._targetPos = this._targetActor.CenterPosition;
                    return;
                case ITEM_DANCE:
                case ITEM_SEEK:
                case ITEM_LOCATE:
                case ITEM_INSPECT:
                    this._targetPos = this._targetItem.transform.position;
                    return;
            }
        }

        private bool ReachedTarget()
        {
            if (!this._hasValidTarget || m_companionController.IsBeingPet)
                return false;

            switch(this._state)
            {
                case ITEM_DANCE:
                    return true;
                case ENEMY_CARRY:
                    return m_aiActor.IsOverPit;
                case OWNER_FOLLOW:
                case ITEM_RETRIEVE:
                    return (Vector2.Distance(this._targetPos, m_aiActor.CenterPosition) <= IdealRadius);
                case ENEMY_SEEK:
                case ITEM_SEEK:
                case ITEM_INSPECT:
                case ITEM_LOCATE:
                    return (Vector2.Distance(this._targetPos, m_aiActor.CenterPosition) <= 2f);
            }
            return false;
        }

        private void OnReachedTarget()
        {
            switch(this._state)
            {
                case OWNER_FOLLOW:
                    BecomeIdle(); break;
                case ENEMY_SEEK:
                    GrabEnemy(); break;
                case ENEMY_CARRY:
                    DropEnemyInPit(); break;
                case ITEM_RETRIEVE:
                    DropItemNearPlayer(); break;
                case ITEM_SEEK:
                case ITEM_INSPECT:
                    GrabItem(); break;
                case ITEM_LOCATE:
                case ITEM_DANCE:
                    DanceAroundItem(); break;
            }
        }

        private void BecomeIdle()
        {
            m_aiActor.ClearPath();
            if (m_idleTimer <= 0f && IdleAnimations != null && IdleAnimations.Length > 0)
            {
                m_aiAnimator.PlayUntilFinished(IdleAnimations[UnityEngine.Random.Range(0, IdleAnimations.Length)]);
                m_idleTimer = UnityEngine.Random.Range(3, 10);
            }
        }

        private void GrabEnemy()
        {

        }

        private void DropEnemyInPit()
        {
            // if (m_aiActor.IsOverPitAtAll)
        }

        private void GrabItem()
        {
            if (this._state == ITEM_INSPECT)
            {

            }
            else
            {

            }
        }

        private void DropItemNearPlayer()
        {

        }

        private void DanceAroundItem()
        {
            if (this._state == ITEM_LOCATE)
            {
                //extra stuff
                this._state = ITEM_DANCE;
            }
        }

        private bool CorrectForInaccessibleCell()
        {
            IntVector2 pos = m_aiActor.specRigidbody.UnitCenter.ToIntVector2(VectorConversions.Floor);
            CellData currentCell = GameManager.Instance.Dungeon.data[pos];
            if (currentCell == null || !currentCell.IsPlayerInaccessible)
                return false;

            if (m_repathTimer > 0f)
                return true;

            m_repathTimer = PathInterval;
            RoomHandler currentRoom = GameManager.Instance.Dungeon.data.GetAbsoluteRoomFromPosition(pos);
            if (currentRoom == null)
                return true;

            IntVector2? nearestAvailableCell = currentRoom.GetNearestAvailableCell(
                pos.ToCenterVector2(), m_aiActor.Clearance, m_aiActor.PathableTiles, false,
                (IntVector2 pos) => !GameManager.Instance.Dungeon.data[pos].IsPlayerInaccessible);
            if (nearestAvailableCell.HasValue)
                m_aiActor.PathfindToPosition(nearestAvailableCell.Value.ToCenterVector2());
            return true;
        }

        private void RepathToTarget()
        {
            m_idleTimer = Mathf.Max(m_idleTimer, 2f);
            if (m_repathTimer > 0f)
                return;

            m_repathTimer = PathInterval;
            if (m_companionController && m_companionController.IsBeingPet)
            {
                Vector2 petterPos = m_companionController.m_pettingDoer.specRigidbody.UnitCenter + m_companionController.m_petOffset;
                if (Vector2.Distance(petterPos, m_aiActor.specRigidbody.UnitCenter) < 0.08f)
                    m_aiActor.ClearPath();
                else
                    m_aiActor.PathfindToPosition(petterPos, petterPos);
            }
            else
                m_aiActor.PathfindToPosition(this._targetPos);

            if (m_aiActor.Path == null)
            {
                m_sequentialPathFails = 0;
                return;
            }

            if (m_aiActor.Path.InaccurateLength > 50f)
            {
                m_aiActor.ClearPath();
                m_sequentialPathFails = 0;
                m_aiActor.CompanionWarp(this._targetPos);
            }
            else if (!m_aiActor.Path.WillReachFinalGoal && (++m_sequentialPathFails) > 3)
            {
                CellData cellData2 = GameManager.Instance.Dungeon.data[this._targetPos.ToIntVector2(VectorConversions.Floor)];
                if (cellData2 != null && cellData2.IsPassable)
                {
                    m_sequentialPathFails = 0;
                    m_aiActor.CompanionWarp(this._targetPos);
                }
            }
            else
                m_sequentialPathFails = 0;
        }

        public override BehaviorResult Update()
        {
            if (!GameManager.HasInstance || GameManager.Instance.IsLoadingLevel || !m_companionController || !m_aiActor.CompanionOwner)
                return BehaviorResult.SkipAllRemainingBehaviors;

            if (GameManager.Instance.CurrentLevelOverrideState == GameManager.LevelOverrideState.END_TIMES)
            {
                m_aiActor.ClearPath();
                return BehaviorResult.SkipAllRemainingBehaviors;
            }

            if (TemporarilyDisabled)
                return BehaviorResult.Continue;

            UpdateStateAndTargetPosition();
            if (!this._hasValidTarget)
            {
                m_aiActor.ClearPath();
                return BehaviorResult.SkipAllRemainingBehaviors;
            }

            DecrementTimer(ref m_idleTimer);

            if (CorrectForInaccessibleCell())
                {} // current cell is inaccessible, do nothing else
            else if (ReachedTarget())
                OnReachedTarget();
            else
                RepathToTarget();
            return BehaviorResult.SkipRemainingClassBehaviors;
        }
    }
}
