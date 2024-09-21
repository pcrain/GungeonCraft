namespace CwaffingTheGungy;

using static AllayCompanion.AllayMovementBehavior.State;


/* TODO:
    - fix dropping items over pits
    - add scouting mode

    - add smoother movement
    - add better animations
    - add sounds
    - add vfx
*/

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
        private Vector2 _targetPos = default;
        private State _state = OWNER_FOLLOW;
        private bool _scouting = false;
        private bool _hasValidTarget = false;
        private Vector2 _pitPos = default;
        private int _heldItemId = -1;
        private tk2dSprite _heldItemRenderer = null;
        private GameActor _lastDroppedActor = null;

        private static bool _DebugTrace = false;

        public override void Start()
        {
            base.Start();
            m_companionController = m_gameObject.GetComponent<CompanionController>();
            m_aiActor.FallingProhibited = true;
            m_aiActor.PathableTiles |= CellTypes.PIT;
            m_aiActor.MovementModifiers += SnapToTargetIfClose;

            #if DEBUG
                Commands._OnDebugKeyPressed += ShowState;
            #endif
        }

        #if DEBUG
            private void ShowState()
            {
                Vector2 myPos = m_aiActor.sprite.WorldCenter;
                Vector2 adjustedTarget = this._targetPos + (m_aiActor.transform.position.XY() - myPos);

                ETGModConsole.Log($"allay status");
                ETGModConsole.Log($"  state is {this._state}");
                ETGModConsole.Log($"  held item is {this._heldItemId}");
                ETGModConsole.Log($"  held enemy is {(this._heldActor ? this._heldActor.ActorName : "none")}");
                ETGModConsole.Log($"  reached target? {ReachedTarget()}");
                ETGModConsole.Log($"  done pathing? {m_aiActor.PathComplete}");
                ETGModConsole.Log($"  has path? {m_aiActor.Path != null}");
                ETGModConsole.Log($"  transform pos {m_aiActor.transform.position.XY()}");
                ETGModConsole.Log($"  sprite center {myPos}");
                ETGModConsole.Log($"  target pos {this._targetPos}");
                ETGModConsole.Log($"  target square distance {(this._targetPos - myPos).sqrMagnitude}");

                _DebugTrace = !_DebugTrace;
            }
        #endif

        public override void Destroy()
        {
            Commands._OnDebugKeyPressed -= ShowState;
            base.Destroy();
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
            PlayerController owner = m_companionController.m_owner;
            if (owner.CurrentRoom is not RoomHandler room)
                return null;

            float floorHealthMod = 1f;
            GameLevelDefinition level = GameManager.Instance.GetLastLoadedLevelDefinition();
            if (level != null)
                floorHealthMod *= level.enemyHealthMultiplier;
            float maxHealth = 40f * floorHealthMod;

            Vector2 myPos = m_aiActor.specRigidbody.UnitCenter;
            AIActor nearest = null;
            float nearestDist = 999f;

            foreach (AIActor enemy in room.SafeGetEnemiesInRoom())
            {
                if (!enemy || !enemy.isActiveAndEnabled || enemy == this._lastDroppedActor)
                    continue;
                if (!enemy.IsWorthShootingAt || enemy.IsFlying || enemy.FallingProhibited)
                    continue;
                if (enemy.healthHaver is not HealthHaver hh || hh.maximumHealth >= maxHealth)
                    continue;
                if (hh.IsDead || hh.IsBoss || hh.IsSubboss || !hh.IsVulnerable)
                    continue;
                if (enemy.specRigidbody is not SpeculativeRigidbody body || !body.CanBeCarried)
                    continue;
                if (enemy.behaviorSpeculator is not BehaviorSpeculator bs || !bs.IsInterruptable || bs.ImmuneToStun)
                    continue;

                float sqrDist = (enemy.CenterPosition - myPos).sqrMagnitude;
                if (sqrDist > nearestDist)
                    continue;

                nearest = enemy;
                nearestDist = sqrDist;
            }
            return nearest;
        }

        private PickupObject FindCarriableItem()
        {
            const float _MAX_ITEM_DIST = 5f;
            const float _MAX_ITEM_DIST_SQR = _MAX_ITEM_DIST * _MAX_ITEM_DIST;
            PlayerController owner = m_companionController.m_owner;
            if (owner.CurrentRoom is not RoomHandler room)
                return null;
            if (StaticReferenceManager.AllDebris is not List<DebrisObject> allDebris)
                return null;
            for (int j = 0; j < allDebris.Count; j++)
            {
                if (allDebris[j] is not DebrisObject d || !d.IsPickupObject || d.transform.position.GetAbsoluteRoom() != room)
                    continue;
                GameObject dobj = d.gameObject;
                HealthPickup component     = dobj.GetComponent<HealthPickup>();
                AmmoPickup component2      = dobj.GetComponent<AmmoPickup>();
                KeyBulletPickup component3 = dobj.GetComponent<KeyBulletPickup>();
                SilencerItem component4    = dobj.GetComponent<SilencerItem>();
                if (!(component || component2 || component3 || component4))
                    continue;
                float sqrDist = (owner.CenterPosition - d.transform.position.XY()).sqrMagnitude;
                if (sqrDist > _MAX_ITEM_DIST_SQR)
                    return dobj.GetComponent<PickupObject>();
            }
            return null;
        }

        private bool FindNearestPit(Vector2 pos, out IntVector2 pitPos)
        {
            pitPos = IntVector2.Zero;
            if (this._cachedPitRoom == null)
                return false;

            IntVector2 ipos = pos.ToIntVector2(VectorConversions.Floor);
            float nearest = 9999f;
            bool found = false;
            foreach (IntVector2 cellPos in this._cachedPitRoom.Cells)
            {
                CellData cell = GameManager.Instance.Dungeon.data[cellPos];
                if (cell == null || cell.type != CellType.PIT || cell.fallingPrevented)
                    continue;
                float dist = (cellPos - ipos).sqrMagnitude;
                if (found && dist > nearest)
                    continue;
                found = true;
                nearest = dist;
                pitPos = cellPos;
            }
            return found;
        }

        private RoomHandler _cachedPitRoom = null;
        private bool _cachedRoomHasPits = false;
        private bool RoomContainsPits()
        {
            RoomHandler room = m_companionController.m_owner.CurrentRoom;
            if (room == this._cachedPitRoom)
                return this._cachedRoomHasPits;
            this._cachedPitRoom = room;
            this._cachedRoomHasPits = FindNearestPit(m_aiActor.specRigidbody.UnitCenter, out _);
            return this._cachedRoomHasPits;
        }

        private void DetermineNewTarget()
        {
            PlayerController owner = m_companionController.m_owner;

            // set up a sane default state
            DropItem();
            DropEnemy();
            this._hasValidTarget = true;
            this._scouting = this._scouting && this._heldItemId >= 0;
            this._heldActor = null;
            this._targetItem = null;
            this._targetActor = owner;
            this._state = OWNER_FOLLOW;

            if (this._scouting)
                return; // nothing to do in scout mode unless explicitly finding something after combat

            if (owner.IsInCombat)
            {
                if (!RoomContainsPits())
                    return;
                if (FindCarriableEnemy() is not AIActor target)
                    return; // failed to find a carriable enemy
                Vector2 myPos = m_aiActor.specRigidbody.UnitCenter;
                if (!FindNearestPit(myPos, out IntVector2 pitPos))
                    return;

                this._state = ENEMY_SEEK;
                this._targetActor = target;
                this._pitPos = pitPos.ToVector2();
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

        private void UpdateStateAndTargetPosition()
        {
            if (this._state == OWNER_FOLLOW || !IsTargetValid())
                DetermineNewTarget();
            if (!this._hasValidTarget)
                return;

            switch(this._state)
            {
                case ENEMY_CARRY:
                    this._targetPos = this._pitPos;
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
            const float _PICKUP_RADIUS = 1.0f;
            const float _PICKUP_RADIUS_SQR = _PICKUP_RADIUS * _PICKUP_RADIUS;
            if (!this._hasValidTarget || m_companionController.IsBeingPet)
                return false;

            switch(this._state)
            {
                case ITEM_DANCE:
                    return true;
                case ENEMY_CARRY:
                    return m_aiActor.IsOverPit && (this._heldActor as AIActor).IsOverPit &&
                        (this._heldActor.WillDefinitelyFall() || DeltaToTarget().sqrMagnitude < 0.1f);
                case OWNER_FOLLOW:
                case ITEM_RETRIEVE:
                    return (Vector2.Distance(this._targetPos, m_aiActor.CenterPosition) <= IdealRadius);
                case ENEMY_SEEK:
                case ITEM_SEEK:
                case ITEM_INSPECT:
                case ITEM_LOCATE:
                    return ((this._targetPos - m_aiActor.CenterPosition).sqrMagnitude <= _PICKUP_RADIUS_SQR);
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

        private static void CheckPit(string label, int x, int y)
        {
            ETGModConsole.Log($"pit {label} {x},{y} : {GameManager.Instance.Dungeon.data[new IntVector2(x,y)].type == CellType.PIT}");
        }

        private static void CheckNearbyPits(IntVector2 pitPos)
        {
            int x = pitPos.x;
            int y = pitPos.y;
            CheckPit("here", x,     y     );
            CheckPit("sw",   x - 1, y - 1 );
            CheckPit("s",    x,     y - 1 );
            CheckPit("se",   x + 1, y - 1 );
            CheckPit("w",    x - 1, y     );
            CheckPit("e",    x + 1, y     );
            CheckPit("nw",   x - 1, y + 1 );
            CheckPit("n",    x,     y + 1 );
            CheckPit("ne",   x + 1, y + 1 );
        }

        private void GrabEnemy()
        {
            if (!this._targetActor)
            {
                Lazy.RuntimeWarn("grabbing nonexistent enemy");
                return;
            }

            if (this._targetActor.behaviorSpeculator is BehaviorSpeculator bs)
            {
                // bs.InterruptAndDisable();
                bs.Stun(3600f, false);
            }

            SpeculativeRigidbody body = m_companionController.specRigidbody;
            SpeculativeRigidbody other = this._targetActor.specRigidbody;
            body.RegisterCarriedRigidbody(other);

            Vector2 myPos = m_companionController.aiActor.sprite.WorldCenter;
            Vector2 offset = this._targetActor.transform.position.XY() - other.UnitCenter;
            this._targetActor.transform.position = (myPos + offset).ToVector3ZUp();
            other.Reinitialize();
            other.RegisterSpecificCollisionException(body);
            body.RegisterSpecificCollisionException(other);
            other.CollideWithOthers = false;
            other.CollideWithTileMap = false;
            other.AddCollisionLayerIgnoreOverride(CollisionMask.LayerToMask(CollisionLayer.LowObstacle));
            // other.AddCollisionLayerIgnoreOverride(CollisionMask.LayerToMask(CollisionLayer.PlayerHitBox));
            // other.AddCollisionLayerIgnoreOverride(CollisionMask.LayerToMask(CollisionLayer.EnemyHitBox));
            if (this._targetActor.knockbackDoer is KnockbackDoer kb)
                kb.knockbackMultiplier = 0f;
            this._targetActor.FallingProhibited = true;

            this._heldActor = this._targetActor;
            this._targetActor = null;
            FindNearestPit(m_companionController.transform.position, out IntVector2 pitPos);
            // CheckNearbyPits(pitPos);
            this._pitPos = pitPos.ToVector2() + new Vector2(0.5f, 0.5f); // center of pit cell
            this._targetPos = this._pitPos;
            this._state = ENEMY_CARRY;
            RepathToTarget();
        }

        private void DropEnemyInPit()
        {
            DropEnemy();
            this._state = OWNER_FOLLOW;
            DetermineNewTarget();
        }

        private void DropEnemy()
        {
            if (!this._heldActor)
                return;

            SpeculativeRigidbody body = m_companionController.specRigidbody;
            SpeculativeRigidbody other = this._heldActor.specRigidbody;
            body.DeregisterCarriedRigidbody(other);

            other.DeregisterSpecificCollisionException(body);
            body.DeregisterSpecificCollisionException(other);
            other.CollideWithOthers = true;
            other.CollideWithTileMap = true;
            other.RemoveCollisionLayerIgnoreOverride(CollisionMask.LayerToMask(CollisionLayer.LowObstacle));
            // other.RemoveCollisionLayerIgnoreOverride(CollisionMask.LayerToMask(CollisionLayer.PlayerHitBox));
            // other.RemoveCollisionLayerIgnoreOverride(CollisionMask.LayerToMask(CollisionLayer.EnemyHitBox));
            if (this._heldActor.knockbackDoer is KnockbackDoer kb)
                kb.knockbackMultiplier = 1f;
            this._heldActor.FallingProhibited = false;
            if (this._heldActor.behaviorSpeculator is BehaviorSpeculator bs)
            {
                bs.EndStun();
                bs.enabled = true;
            }
            other.Reinitialize();
            if (DeltaToTarget().sqrMagnitude < 0.1f && !this._heldActor.WillDefinitelyFall())
            {
                // Lazy.DebugWarn("FORCING GAME ACTOR TO FALL UNNATURALLY, HOPE NOBODY NOTICES");
                //NOTE: this does in fact happen a lot, pits are wonky and pathfinding isn't fantastic
                this._heldActor.ForceFall();
            }

            this._lastDroppedActor = this._heldActor;
            this._heldActor = null;
        }

        private void GrabItem()
        {
            if (!this._targetItem)
            {
                Lazy.RuntimeWarn("grabbing nonexistent item");
                return;
            }

            if (this._state == ITEM_INSPECT)
            {
                return; //TODO:
            }

            this._heldItemId = this._targetItem.PickupObjectId;
            this._heldItemRenderer = Lazy.SpriteObject(this._targetItem.sprite.collection, this._targetItem.sprite.spriteId);
            this._heldItemRenderer.HeightOffGround = 2f;
            this._heldItemRenderer.UpdateZDepth();
            this._heldItemRenderer.PlaceAtPositionByAnchor(m_companionController.aiActor.sprite.WorldCenter, Anchor.UpperCenter);
            m_companionController.sprite.AttachRenderer(this._heldItemRenderer);
            this._heldItemRenderer.gameObject.transform.parent = m_companionController.sprite.transform;
            SpriteOutlineManager.AddOutlineToSprite(this._heldItemRenderer, Color.black);
            UnityEngine.Object.Destroy(this._targetItem.gameObject);

            this._targetItem = null;
            this._targetActor = m_companionController.m_owner;
            this._state = ITEM_RETRIEVE;
            RepathToTarget();
        }

        private void DropItemNearPlayer()
        {
            DropItem();
            this._targetActor = null;
            this._state = OWNER_FOLLOW;
            DetermineNewTarget();
        }

        private void DropItem()
        {
            if (this._heldItemId != -1)
                LootEngine.SpawnItem(
                    PickupObjectDatabase.GetById(this._heldItemId).gameObject,
                    m_companionController.aiActor.sprite.WorldBottomCenter.ToVector3ZUp(),
                    UnityEngine.Random.insideUnitCircle.normalized,
                    0.1f);
            this._heldItemId = -1;

            if (this._heldItemRenderer)
            {
                m_companionController.sprite.DetachRenderer(this._heldItemRenderer);
                this._heldItemRenderer.gameObject.transform.parent = null;
                UnityEngine.Object.Destroy(this._heldItemRenderer.gameObject);
                this._heldItemRenderer = null;
            }
        }

        private void DanceAroundItem()
        {
            if (this._state == ITEM_LOCATE)
            {
                //TODO: extra dance setup stuff
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

        const float _SNAP_DIST = 1f;
        const float _SNAP_DIST_SQR = _SNAP_DIST * _SNAP_DIST;
        private void RepathToTarget()
        {
            // adjust relative to the center of our sprite
            Vector2 bottomLeft = m_aiActor.transform.position.XY();
            Vector2 center = m_aiActor.sprite.WorldCenter;
            Vector2 adjustedTarget = this._targetPos + (bottomLeft - center);

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
                m_aiActor.PathfindToPosition(adjustedTarget);

            if (m_aiActor.Path == null)
            {
                m_sequentialPathFails = 0;
                return;
            }

            if (m_aiActor.Path.InaccurateLength > 50f)
            {
                m_aiActor.ClearPath();
                m_sequentialPathFails = 0;
                m_aiActor.CompanionWarp(adjustedTarget);
            }
            else if (!m_aiActor.Path.WillReachFinalGoal && (++m_sequentialPathFails) > 3)
            {
                CellData cellData2 = GameManager.Instance.Dungeon.data[adjustedTarget.ToIntVector2(VectorConversions.Floor)];
                if (cellData2 != null && cellData2.IsPassable)
                {
                    m_sequentialPathFails = 0;
                    m_aiActor.CompanionWarp(adjustedTarget);
                }
            }
            else
                m_sequentialPathFails = 0;
        }

        private Vector2 DeltaToTarget()
        {
            // adjust relative to the center of our sprite
            Vector2 bottomLeft = m_aiActor.transform.position.XY();
            Vector2 adjustedTarget = this._targetPos - m_aiActor.sprite.GetRelativePositionFromAnchor(Anchor.MiddleCenter);
            return adjustedTarget - bottomLeft;
        }

        private void SnapToTargetIfClose(ref Vector2 voluntaryVel, ref Vector2 involuntaryVel)
        {
            Vector2 delta = DeltaToTarget();
            if (delta.sqrMagnitude < _SNAP_DIST_SQR)
                voluntaryVel += 2f * delta.normalized;
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

            CwaffVFX.Spawn(VFX.MiniPickup, this._targetPos, lifetime: 0.1f, fadeOutTime: 0.1f);

            return BehaviorResult.SkipRemainingClassBehaviors;
        }
    }
}
