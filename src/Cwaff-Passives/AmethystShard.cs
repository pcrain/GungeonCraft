namespace CwaffingTheGungy;

using static AllayCompanion.AllayMovementBehavior.State;

public class AmethystShard : CwaffCompanion
{
    public static string ItemName         = "Amethyst Shard";
    public static string ShortDescription = "Allay Your Worries";
    public static string LongDescription  = "Spawns a friendly Allay. While in combat, the Allay will attempt to grab small enemies and drop them into pits. Out of combat, the Allay will bring nearby minor collectibles to the player. Spinning around near a minor collectible will cause the Allay to pick it up and enter scouting mode, providing a small chance to find items of the same type when clearing a room. Interacting with the Allay will cause it to drop any held items.";
    public static string Lore             = "A shiny purple gemstone normally found in geodes deep underground. It's not particularly useful in its own right, but folk tales claim these gemstones attract certain playful creatures with a love for relocating trinkets they find on the ground.";

    public static string CompanionName    = "Allay";

    public int itemsFound = 0;
    public int scoutItemId = -1;

    public static void Init()
    {
        PassiveItem item  = Lazy.SetupPassive<AmethystShard>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.B;

        AllayCompanion friend = item.InitCompanion<AllayCompanion>(friendName: CompanionName.ToID(), baseFps: 12);
        friend.MakeIntangible();
        friend.aiActor.specRigidbody.CollideWithTileMap = true;

        BehaviorSpeculator bs = friend.gameObject.GetComponent<BehaviorSpeculator>();
        bs.MovementBehaviors.Add(new AllayCompanion.AllayMovementBehavior());

        AllayCompanion._AllaySparkles = VFX.Create("allay_sparkles", fps: 10, loops: false);
    }

    public override void DisableEffect(PlayerController player)
    {
        this.scoutItemId = -1;
        base.DisableEffect(player);
    }

    public override void MidGameSerialize(List<object> data)
    {
        base.MidGameSerialize(data);
        data.Add(itemsFound);
        data.Add(scoutItemId);
    }

    public override void MidGameDeserialize(List<object> data)
    {
        base.MidGameDeserialize(data);
        itemsFound = (int)data[0];
        scoutItemId = (int)data[1];
    }
}

public class AllayCompanion : CwaffCompanionController
{
    internal static GameObject _AllaySparkles;

    public class AllayMovementBehavior : MovementBehaviorBase
    {
        /* Behavior sketch:
          - spinning near a grounded item while allay is in follow mode makes allay pick up the nearest item and switch to scout mode
          - petting an allay in scout mode makes it drop its item and switch to follow mode
          - in scout mode, allay has a 10% chance to find a copy of its held item on room clear
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

        private const float _ROOM_CLEAR_ITEM_CHANCE = 0.075f;

        public float PathInterval = 0.25f;
        public float IdealRadius = 3f;

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
        private Vector2 _pitPos = default;
        private tk2dSprite _heldItemRenderer = null;
        private GameActor _lastDroppedActor = null;
        private float _cumulativeGunRotation = 0f;
        private float _zeroRotationTime = 0f;
        private float _lastGunAngle = 0f;
        private Vector2 _lastVel = default;
        private float _bobAmount = 0f;
        private bool _wasBeingPet = false;
        private AmethystShard _allayItem = null;
        private int _heldItemId = -1;

        public override void Start()
        {
            base.Start();
            m_companionController = m_gameObject.GetComponent<CompanionController>();
            m_aiActor.FallingProhibited = true;
            m_aiActor.PathableTiles |= CellTypes.PIT;
            m_aiActor.MovementModifiers += AdjustMovement;
            m_aiActor.sprite.HeightOffGround = 2f;
            m_aiActor.sprite.UpdateZDepth();
            m_companionController.m_owner.OnRoomClearEvent += PossiblyFindCopyOfHeldItem;
            m_aiActor.specRigidbody.OnTileCollision += OnTileCollision;

            if (!GameManager.Instance.IsLoadingLevel)
                m_aiActor.gameObject.Play("allay_spawn_sound");

            // find our corresponding companion item and read data from it
            if (m_companionController.m_owner is PlayerController owner)
            {
                foreach(PassiveItem passive in owner.passiveItems)
                {
                    if (passive is not AmethystShard allayItem || allayItem.ExtantCompanion != m_gameObject)
                        continue;
                    this._allayItem = allayItem;
                    break;
                }
            }

            if (this._allayItem && this._allayItem.scoutItemId != -1)
            {
                this._heldItemId = this._allayItem.scoutItemId;
                SetupHeldItemRenderer();
                this._scouting = true;
            }

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
        }
#endif

        public override void Destroy()
        {
            DropItem();
            DropEnemy(droppedEarly: true);
            if (m_aiActor)
                m_aiActor.MovementModifiers -= AdjustMovement;
            if (m_companionController && m_companionController.m_owner)
                m_companionController.m_owner.OnRoomClearEvent -= PossiblyFindCopyOfHeldItem;
            #if DEBUG
                Commands._OnDebugKeyPressed -= ShowState;
            #endif
            base.Destroy();
        }

        public override void Upkeep()
        {
            base.Upkeep();
            DecrementTimer(ref m_repathTimer);
        }

        private void OnTileCollision(CollisionData tileCollision)
        {
            if (tileCollision.CollidedX)
                this._lastVel = this._lastVel.WithX(0);
            if (tileCollision.CollidedY)
                this._lastVel = this._lastVel.WithY(0);
        }

        private void PossiblyFindCopyOfHeldItem(PlayerController controller)
        {
            if (!this._scouting || this._heldItemId == -1 || controller.CurrentRoom == null || !this._allayItem)
                return;
            float baseItemFindChance = _ROOM_CLEAR_ITEM_CHANCE * (controller.HasSynergy(Synergy.SPAWNPROOFING) ? 2f : 1f);
            if (controller.HasSynergy(Synergy.SPAWNPROOFING))
                if (StackOfTorches._TorchesInRoom.TryGetValue(controller.CurrentRoom, out int torches) && torches > 0)
                    baseItemFindChance *= 2f;
            if (UnityEngine.Random.value > Mathf.Max(baseItemFindChance - 0.01f * this._allayItem.itemsFound, 0.025f))
                return;

            ++this._allayItem.itemsFound;
            this._targetItem = LootEngine.SpawnItem(
                PickupObjectDatabase.GetById(this._heldItemId).gameObject,
                controller.CurrentRoom.GetBestRewardLocation(IntVector2.One).ToVector3(),
                UnityEngine.Random.insideUnitCircle.normalized,
                0.1f,
                doDefaultItemPoof: true).gameObject.GetComponent<PickupObject>();
            m_aiActor.gameObject.Play("allay_find_sound");
            this._state = ITEM_LOCATE;
        }

        private AIActor FindCarriableEnemy()
        {
            const float _MAX_BASE_HEALTH = 40f;
            PlayerController owner = m_companionController.m_owner;
            if (owner.CurrentRoom is not RoomHandler room)
                return null;

            float floorHealthMod = 1f;
            GameLevelDefinition level = GameManager.Instance.GetLastLoadedLevelDefinition();
            if (level != null)
                floorHealthMod *= level.enemyHealthMultiplier;
            float maxHealth = _MAX_BASE_HEALTH * floorHealthMod;

            Vector2 myPos = m_aiActor.specRigidbody.UnitCenter;
            AIActor nearest = null;
            float nearestDist = 999f;

            foreach (AIActor enemy in room.SafeGetEnemiesInRoom())
            {
                if (!enemy || !enemy.isActiveAndEnabled || enemy == this._lastDroppedActor)
                    continue;
                if (!enemy.IsWorthShootingAt || enemy.IsFlying || enemy.FallingProhibited || enemy.IsFalling)
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

        private static readonly List<int> _PickupWhitelist = [(int)Items.GlassGuonStone];
        private PickupObject FindCarriableItem(bool nearby = false)
        {
            const float _MAX_ITEM_DIST = 5f;
            const float _MAX_ITEM_DIST_SQR = _MAX_ITEM_DIST * _MAX_ITEM_DIST;
            PlayerController owner = m_companionController.m_owner;
            if (owner.CurrentRoom is not RoomHandler room)
                return null;
            int n = CwaffEvents._DebrisPickups.Count;
            for (int i = 0; i < n; ++i)
            {
                PickupObject pickup = CwaffEvents._DebrisPickups[i];
                if (!pickup || pickup.IsBeingSold)
                    continue;
                bool health      = pickup is HealthPickup healthPickup && !healthPickup.m_pickedUp;
                bool ammo        = pickup is AmmoPickup ammoPickup && !ammoPickup.m_pickedUp;
                bool key         = pickup is KeyBulletPickup keyPickup;
                bool blank       = pickup is SilencerItem blankPickup && !blankPickup.m_pickedUp;
                bool whitelisted = _PickupWhitelist.Contains(pickup.PickupObjectId);
                if (!(health || ammo || key || blank || whitelisted))
                    continue;
                if (pickup.transform.position.GetAbsoluteRoom() != room) // save a more expensive check for later
                    continue;
                float sqrDist = (owner.CenterPosition - pickup.transform.position.XY()).sqrMagnitude;
                if (nearby == (sqrDist < _MAX_ITEM_DIST_SQR))
                    return pickup.GetComponent<PickupObject>();
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
                if (dist > nearest)
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
            DropEnemy(droppedEarly: true);
            this._scouting = this._scouting && this._heldItemId >= 0 && !m_companionController.IsBeingPet;
            if (!this._scouting)
                DropItem();
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
                this._pitPos = pitPos.ToVector2() + new Vector2(0.5f, 0.5f); // center of pit cell
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
                    return this._targetItem && this._targetItem.debris &&
                        (this._state == ITEM_LOCATE || this._state == ITEM_DANCE || this._targetItem.debris.onGround) &&
                        this._targetItem.debris.sprite &&
                        this._targetItem.debris.sprite.WorldCenter.GetAbsoluteRoom() is RoomHandler room &&
                        room == m_companionController.m_owner.CurrentRoom;
            }
            return false;
        }

        private void UpdateStateAndTargetPosition()
        {
            if (this._state == OWNER_FOLLOW || m_companionController.IsBeingPet || !IsTargetValid())
                DetermineNewTarget();
            if (this._state != OWNER_FOLLOW)
                this._cumulativeGunRotation = 0.0f; // reset spinning checks

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
                    this._targetPos = this._targetItem.debris.sprite.WorldCenter;
                    return;
            }
        }

        private const float _DANCE_RADIUS = 3.0f;
        private const float _DANCE_RADIUS_SQR = _DANCE_RADIUS * _DANCE_RADIUS;
        private bool ReachedTarget()
        {
            const float _PICKUP_RADIUS = 1.0f;
            const float _PICKUP_RADIUS_SQR = _PICKUP_RADIUS * _PICKUP_RADIUS;
            if (m_companionController.IsBeingPet)
                return false;

            switch(this._state)
            {
                case ITEM_DANCE:
                    return true;
                case ENEMY_CARRY:
                    return m_aiActor.IsOverPit && (this._heldActor.WillDefinitelyFall() || DeltaToTarget().sqrMagnitude < 0.1f);
                case OWNER_FOLLOW:
                    return (Vector2.Distance(this._targetPos, m_aiActor.CenterPosition) <= IdealRadius) && m_aiActor.CenterPosition.InBounds();
                case ITEM_RETRIEVE:
                    return (Vector2.Distance(this._targetPos, m_aiActor.CenterPosition) <= IdealRadius) && !m_aiActor.CenterPosition.NearPit();
                case ENEMY_SEEK:
                case ITEM_SEEK:
                case ITEM_INSPECT:
                    return ((this._targetPos - m_aiActor.CenterPosition).sqrMagnitude <= _PICKUP_RADIUS_SQR);
                case ITEM_LOCATE:
                    return ((this._targetPos - m_aiActor.CenterPosition).sqrMagnitude <= _DANCE_RADIUS_SQR);
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

        //NOTE: adapted from MachoBraceSynergyProcessor::Update()
        private void DoSpinningChecks()
        {
            if (!GameManager.HasInstance || GameManager.Instance.IsLoadingLevel || GameManager.Instance.IsPaused || BraveTime.DeltaTime == 0.0f)
                return;

            if (this._scouting || this._state != OWNER_FOLLOW || !ReachedTarget())
            {
                _cumulativeGunRotation = 0f;
                return;
            }

            PlayerController owner = m_companionController.m_owner;

            float gunAngle = (owner.unadjustedAimPoint.XY() - owner.CenterPosition).ToAngle();
            float angleDelta = Vector2.SignedAngle(
                BraveMathCollege.DegreesToVector(gunAngle),
                BraveMathCollege.DegreesToVector(_lastGunAngle));
            angleDelta = Mathf.Clamp(angleDelta, -90f, 90f);
            if (Mathf.Abs(angleDelta) < 120f * BraveTime.DeltaTime)
            {
                if ((_zeroRotationTime += Time.deltaTime) < 0.0333f)
                    return;
                angleDelta = 0f;
                _cumulativeGunRotation = 0f;
            }
            else
                _zeroRotationTime = 0f;
            _lastGunAngle = gunAngle;
            _cumulativeGunRotation += angleDelta;
            if (Mathf.Abs(_cumulativeGunRotation) < 720f)
                return;
            _cumulativeGunRotation = 0f;
            if (FindCarriableItem(nearby: true) is not PickupObject item)
                return;

            m_aiActor.gameObject.Play("allay_find_sound");
            this._state = ITEM_INSPECT;
            this._targetItem = item;
            this._targetActor = null;
        }

        private void BecomeIdle()
        {
            m_aiActor.ClearPath();
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
                bs.InterruptAndDisable();
                bs.Stun(3600f, false);
            }

            SpeculativeRigidbody body = m_companionController.specRigidbody;
            SpeculativeRigidbody other = this._targetActor.specRigidbody;
            body.RegisterCarriedRigidbody(other);
            body.CollideWithTileMap = false;

            Vector2 myPos = m_companionController.aiActor.sprite.WorldCenter;
            Vector2 offset = this._targetActor.transform.position.XY() - other.UnitCenter;
            this._targetActor.transform.position = (myPos + offset).ToVector3ZUp();
            other.Reinitialize();
            other.RegisterSpecificCollisionException(body);
            body.RegisterSpecificCollisionException(other);
            other.CollideWithOthers = false;
            other.CollideWithTileMap = false;
            other.AddCollisionLayerIgnoreOverride(CollisionMask.LayerToMask(CollisionLayer.LowObstacle));
            if (this._targetActor.knockbackDoer is KnockbackDoer kb)
                kb.knockbackMultiplier = 0f;
            this._targetActor.FallingProhibited = true;

            this._heldActor = this._targetActor;
            this._targetActor = null;
            FindNearestPit(m_companionController.transform.position, out IntVector2 pitPos);
            this._pitPos = pitPos.ToVector2() + new Vector2(0.5f, 0.5f); // center of pit cell
            this._targetPos = this._pitPos;
            this._state = ENEMY_CARRY;
            RepathToTarget();
        }

        private void DropEnemyInPit()
        {
            DropEnemy(droppedEarly: false);
            m_aiActor.gameObject.Play("allay_drop_sound");
            this._state = OWNER_FOLLOW;
            DetermineNewTarget();
        }

        private void DropEnemy(bool droppedEarly = false)
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
            if (this._heldActor.knockbackDoer is KnockbackDoer kb)
                kb.knockbackMultiplier = 1f;
            this._heldActor.FallingProhibited = false;
            if (this._heldActor.behaviorSpeculator is BehaviorSpeculator bs)
            {
                bs.EndStun();
                bs.enabled = true;
            }
            other.Reinitialize();
            other.CorrectForWalls();
            if (!droppedEarly && DeltaToTarget().sqrMagnitude < 0.1f && !this._heldActor.WillDefinitelyFall())
            {
                // Lazy.DebugWarn("FORCING GAME ACTOR TO FALL UNNATURALLY, HOPE NOBODY NOTICES");
                this._heldActor.ForceFall(); //NOTE: this does in fact happen a lot, pits are wonky and pathfinding isn't fantastic
            }

            this._lastDroppedActor = this._heldActor;
            this._heldActor = null;
        }

        private void SetupHeldItemRenderer()
        {
            PickupObject pickup = PickupObjectDatabase.GetById(this._heldItemId);
            tk2dSprite pickupSprite = pickup.gameObject.GetComponent<tk2dSprite>();
            this._heldItemRenderer = Lazy.SpriteObject(pickupSprite.collection, pickupSprite.spriteId);
            this._heldItemRenderer.HeightOffGround = -2f;
            this._heldItemRenderer.UpdateZDepth();
            this._heldItemRenderer.PlaceAtPositionByAnchor(m_companionController.aiActor.sprite.WorldCenter, Anchor.UpperCenter);
            m_companionController.sprite.AttachRenderer(this._heldItemRenderer);
            this._heldItemRenderer.gameObject.transform.parent = m_companionController.sprite.transform;
            SpriteOutlineManager.AddOutlineToSprite(this._heldItemRenderer, Color.black);
        }

        private void GrabItem()
        {
            if (!this._targetItem)
            {
                Lazy.RuntimeWarn("grabbing nonexistent item");
                return;
            }

            this._heldItemId = this._targetItem.PickupObjectId;
            SetupHeldItemRenderer();
            UnityEngine.Object.Destroy(this._targetItem.gameObject);

            this._targetItem = null;
            this._targetActor = m_companionController.m_owner;
            if (this._state == ITEM_INSPECT)
            {
                if (this._allayItem)
                    this._allayItem.scoutItemId = this._heldItemId;
                this._scouting = true;
                this._state = OWNER_FOLLOW;
            }
            else
            {
                this._state = ITEM_RETRIEVE;
            }
            RepathToTarget();
        }

        private void DropItemNearPlayer()
        {
            DropItem();
            m_aiActor.gameObject.Play("allay_drop_sound");
            this._targetActor = null;
            this._state = OWNER_FOLLOW;
            DetermineNewTarget();
        }

        private void DropItem()
        {
            if (this._heldItemId != -1 && !GameManager.Instance.IsLoadingLevel)
                LootEngine.SpawnItem(
                    PickupObjectDatabase.GetById(this._heldItemId).gameObject,
                    m_aiActor.sprite.WorldBottomCenter.ToVector3ZUp(),
                    UnityEngine.Random.insideUnitCircle.normalized,
                    0.1f);
            this._heldItemId = -1;
            if (this._allayItem && !GameManager.Instance.IsLoadingLevel)
                this._allayItem.scoutItemId = -1;

            if (this._heldItemRenderer)
            {
                m_companionController.sprite.DetachRenderer(this._heldItemRenderer);
                this._heldItemRenderer.gameObject.transform.parent = null;
                UnityEngine.Object.Destroy(this._heldItemRenderer.gameObject);
                this._heldItemRenderer = null;
            }
        }

        private float _targetAngle = 0.0f;
        private void DanceAroundItem()
        {
            m_aiActor.ClearPath();
            if (this._state == ITEM_LOCATE)
            {
                m_aiActor.specRigidbody.CollideWithTileMap = false;
                this._targetAngle = (m_aiActor.specRigidbody.UnitCenter - this._targetPos).ToAngle();
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

        private void UpdateMovementSpeed()
        {
            float sqrDist = (m_aiActor.sprite.WorldCenter - this._targetPos).sqrMagnitude;
            float newSpeed = Mathf.Clamp(sqrDist, 5, this._state == ENEMY_CARRY ? 12f : 20f);
            m_aiActor.MovementSpeed = newSpeed;
        }

        private void DoSparkles()
        {
            const float _SPARKLE_RATE = 0.06f;
            float now = BraveTime.ScaledTimeSinceStartup;
            if ((now - this._lastSparkle) < _SPARKLE_RATE)
                return;
            this._lastSparkle = now;
            CwaffVFX.Spawn(prefab: _AllaySparkles, position: m_aiActor.CenterPosition, rotation: Lazy.RandomEulerZ(),
                endScale: 0.1f, lifetime: 0.5f, fadeOutTime: 1.0f, randomFrame: true);
        }

        private float _lastSparkle = 0.0f;
        private void AdjustMovement(ref Vector2 voluntaryVel, ref Vector2 involuntaryVel)
        {
            UpdateMovementSpeed();
            DoSpinningChecks();
            if (DoDancingChecks())
            {
                DoSparkles();
                return;
            }

            if (m_companionController.IsBeingPet)
            {
                if (!this._wasBeingPet)
                    m_aiActor.gameObject.Play("allay_pet_sound");
                this._wasBeingPet = true;
                return;
            }

            this._wasBeingPet = false;
            bool inBounds = m_aiActor.specRigidbody.UnitBottomCenter.InBounds();
            if (inBounds && this._state != ENEMY_CARRY && !m_aiActor.specRigidbody.CollideWithTileMap)
            {
                m_aiActor.specRigidbody.CollideWithTileMap = true;
                m_aiActor.specRigidbody.CorrectForWalls();
            }

            Vector2 delta = DeltaToTarget();
            if (!inBounds || (m_aiActor.PathComplete && !ReachedTarget()))
                voluntaryVel = m_aiActor.MovementSpeed * delta.normalized; // move towards our target if we're stuck
            else if (this._state != OWNER_FOLLOW && delta.sqrMagnitude < _SNAP_DIST_SQR)
                voluntaryVel += 2f * delta.normalized; // nudge towards our target if we're close

            if (voluntaryVel.sqrMagnitude > 1f || this._lastVel.sqrMagnitude > 1f)
                voluntaryVel = Lazy.SmoothestLerp(this._lastVel, voluntaryVel, inBounds ? 6f : 10f);
            this._lastVel = voluntaryVel;

            if (voluntaryVel.sqrMagnitude > 10f)
                DoSparkles();

            if (voluntaryVel != Vector2.zero)
            {
                this._bobAmount = 0f;
                return;
            }
            this._bobAmount += 6f * BraveTime.DeltaTime;
            voluntaryVel = new Vector2(0, 0.5f * Mathf.Sin(this._bobAmount));
        }

        private bool DoDancingChecks()
        {
            if (this._state != ITEM_DANCE)
                return false;

            Vector2 pos = m_aiActor.specRigidbody.UnitCenter;
            this._targetAngle += 360f * BraveTime.DeltaTime;
            Vector2 danceTarget = this._targetPos + this._targetAngle.ToVector(_DANCE_RADIUS);
            Vector2 nextPos = Lazy.SmoothestLerp(pos, danceTarget, 4f);
            Vector2 offset = pos - m_aiActor.transform.position.XY();

            m_aiActor.transform.position = nextPos - offset;
            m_aiActor.specRigidbody.Reinitialize();
            m_aiActor.aiAnimator.FacingDirection = (nextPos - pos).ToAngle();
            return true;
        }

        private bool OffScreenAndInDifferentRoom()
        {
            Vector2 pos = m_aiActor.CenterPosition;
            if (GameManager.Instance.MainCameraController.PointIsVisible(pos, 0.4f))
                return false;
            RoomHandler ownerRoom = m_companionController.m_owner.CurrentRoom;
            return (ownerRoom == null || ownerRoom != pos.GetAbsoluteRoom());
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

            DecrementTimer(ref m_idleTimer);

            UpdateStateAndTargetPosition();
            if (OffScreenAndInDifferentRoom())
                m_aiActor.CompanionWarp(m_companionController.m_owner.CenterPosition);
            else if (ReachedTarget())
                OnReachedTarget();
            else
                RepathToTarget();

            // #if DEBUG
            // CwaffVFX.Spawn(VFX.MiniPickup, this._targetPos, lifetime: 0.1f, fadeOutTime: 0.1f);
            // #endif

            return BehaviorResult.SkipRemainingClassBehaviors;
        }
    }
}
