namespace CwaffingTheGungy;

using static IgnizolCompanion.IgnizolMovementBehavior.State;

public class ScaldingJelly : CwaffCompanion
{
    public static string ItemName         = "Scalding Jelly";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    public static string CompanionName    = "Ignizol";

    public static void Init()
    {
        PassiveItem item  = Lazy.SetupPassive<ScaldingJelly>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.D;

        IgnizolCompanion friend = item.InitCompanion<IgnizolCompanion>(friendName: CompanionName.ToID(), baseFps: 20,
            extraAnims: ["charge", "jump", "carry", "pitfall"]);

        tk2dSpriteAnimation library = friend.gameObject.GetComponent<tk2dSpriteAnimation>();
        foreach (string clipName in new List<string>{"ignizol_pitfall_right", "ignizol_pitfall_left"})
        {
            tk2dSpriteAnimationClip clip = library.GetClipByName(clipName);
            clip.fps = 5;
            clip.frames[0].AddSound("ignizol_fall_sound");
        }

        friend.gameObject.AutoRigidBody(Anchor.LowerLeft, CollisionLayer.EnemyCollider);

        BehaviorSpeculator bs = friend.gameObject.GetComponent<BehaviorSpeculator>();
        bs.MovementBehaviors.Add(new IgnizolCompanion.IgnizolMovementBehavior());
    }
}

public class IgnizolCompanion : CwaffCompanionController
{
    public class IgnizolMovementBehavior : MovementBehaviorBase
    {
        internal enum State {
            IDLE,   // bouncing in place without a care in the world
            WANDER, // moving towards a new random location
            CHASE,  // moving towards a specific target's location
            CHARGE, // charging up an attack for a specific target
            JUMP,   // jumping at a target
            CARRY,  // being carried by a player
            THROW,  // being thrown by a player
        }

        private const string _LAUNCH_REASON = "launched";
        private const string _CARRY_REASON  = "carried";

        private const float _MOVE_SPEED           = 3.5f;
        private const float _PATH_INTERVAL        = 0.25f;
        private const float _LAUNCH_RADIUS        = 4.5f;
        private const float _COLLSION_DAMAGE      = 10.0f;
        private const float _WANDER_TIME_MIN      = 1.5f;
        private const float _WANDER_TIME_MAX      = 4.0f;
        private const float _IDLE_TIME_MIN        = 1.0f;
        private const float _IDLE_TIME_MAX        = 3.0f;
        private const float _CHARGE_TIME          = 1.0f;
        private const float _PICKUP_TIME          = 0.3f;
        private const float _CHASE_RETARGET_TIME  = 1.0f;
        private const float _LAUNCH_HEIGHT        = 1f;
        private const float _LAUNCH_TIME          = 0.4f;
        private const float _AIM_DEVIANCE         = 0.5f;
        private const float _LAUNCH_DIST_MIN      = 2.0f;
        private const float _LAUNCH_DIST_MAX      = 5.0f;
        private const float _PICKUP_DIST          = 2.0f;
        private const float _LIGHT_RADIUS_MIN     = 2.0f;
        private const float _LIGHT_RADIUS_DLT     = 1.0f;
        private const float _LIGHT_FLICKER_RATE   = 10.0f;
        private const float _LIGHT_BRIGHTNESS_MIN = 10.0f;
        private const float _LIGHT_BRIGHTNESS_DLT = 5.0f;
        private const float _LAUNCH_RADIUS_SQR    = _LAUNCH_RADIUS * _LAUNCH_RADIUS;
        private const float _LAUNCH_DIST_MIN_SQR  = _LAUNCH_DIST_MIN * _LAUNCH_DIST_MIN;
        private const float _LAUNCH_DIST_MAX_SQR  = _LAUNCH_DIST_MAX * _LAUNCH_DIST_MAX;
        private const float _PICKUP_DIST_SQR      = _PICKUP_DIST * _PICKUP_DIST;

        private int m_sequentialPathFails;
        private float m_stateTimer;
        private float m_repathTimer;
        private CompanionController m_companionController;

        private GameActor _targetActor = null;
        private Vector2 _targetPos = default;
        private Vector2 _launchStart = default;
        private Vector2 _launchTarget = default;
        private Vector2 _launchVelocity = default;
        private float _launchBeginTime = 0.0f;
        private State _state = WANDER;
        private bool _airborne = false;
        private bool _allowPathing = true;
        private PlayerController _carrier = null;
        private tk2dSprite _jumpSprite = null;
        private EasyLight _light = null;
        private bool _launchSpriteFlipped = false;
        private int _lastIdleFrame = -1;

    #if DEBUG
        private Nametag _debugNameTag = null;
    #endif

        public override void Start()
        {
            base.Start();
            m_companionController = m_gameObject.GetComponent<CompanionController>();
            m_aiActor.MovementModifiers += this.AdjustMovement;
            m_aiActor.spriteAnimator.OnPlayAnimationCalled += this.EnsureSmoothIdleAnimationTransition;
            m_aiActor.CustomPitDeathHandling += this.CustomPitDeathHandling;
            m_aiActor.specRigidbody.CollideWithOthers = true; //NOTE: this doesn't work in CwaffCompanion.InitCompanion for some reason...investigate later
            m_aiActor.PreventFallingInPitsEver = false; //NOTE: neither does this...
            m_aiActor.specRigidbody.AddCollisionLayerOverride(CollisionMask.LayerToMask(CollisionLayer.EnemyHitBox));
            m_aiActor.specRigidbody.OnPreRigidbodyCollision += this.OnPreRigidbodyCollision;

            this._jumpSprite = new GameObject("ignizol_jump_renderer").AddComponent<tk2dSprite>();
            this._jumpSprite.transform.parent = m_aiActor.sprite.transform;
            this._jumpSprite.SetSprite(m_aiActor.sprite.collection, m_aiActor.sprite.spriteId);
            this._jumpSprite.renderer.enabled = false;
            // ResetShadow();

            this._light = EasyLight.Create(parent: m_aiActor.transform, color: ExtendedColours.vibrantOrange,
                radius: _LIGHT_RADIUS_MIN, brightness: _LIGHT_BRIGHTNESS_MIN);
            this._light.gameObject.transform.localPosition = m_aiActor.sprite.GetRelativePositionFromAnchor(Anchor.UpperCenter);

            CwaffEvents.OnEmptyInteract += this.AttemptToPickUpOrThrow;

            #if DEBUG
                // _debugNameTag = m_aiActor.gameObject.AddComponent<Nametag>();
                // _debugNameTag.Setup();
            #endif
        }

        private void OnPreRigidbodyCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
        {
            PhysicsEngine.SkipCollision = true;
            if (this._state != JUMP && this._state != THROW)
                return;
            if (otherRigidbody.healthHaver is not HealthHaver hh)
                return;
            myRigidbody.RegisterTemporaryCollisionException(otherRigidbody, maxTime: 2.0f);
            hh.ApplyDamage(_COLLSION_DAMAGE, myRigidbody.Velocity.normalized, ScaldingJelly.CompanionName, CoreDamageTypes.Fire);
            this.m_aiActor.gameObject.Play("ignizol_hit_sound");
        }

        private void CustomPitDeathHandling(AIActor actor, ref bool suppressDamage)
        {
            ResetState();
            if (m_companionController.m_owner)
                m_aiActor.CompanionWarp(m_companionController.m_owner.CenterPosition);
        }

        private void AttemptToPickUpOrThrow(PlayerController interactor)
        {
            if (this._state == JUMP || this._state == THROW)
                return;
            if (this._state == CARRY)
            {
                if (interactor == this._carrier)
                    GetThrown();
                return;
            }
            if ((m_aiActor.sprite.WorldBottomCenter - interactor.CenterPosition).sqrMagnitude < _PICKUP_DIST_SQR)
                GetPickedUp(interactor);
        }

        private void GetPickedUp(PlayerController interactor)
        {
            this._carrier = interactor;
            this._state = CARRY;
            this.m_stateTimer = _PICKUP_TIME;
            this._allowPathing = false;
            base.m_aiAnimator.PlayUntilCancelled("carry");
            this.m_aiActor.gameObject.Play("ignizol_lift_sound");
            base.m_aiActor.specRigidbody.CollideWithTileMap = false;
            base.m_aiActor.SetIsFlying(true, _CARRY_REASON, adjustShadow: false);
            this.m_aiActor.ToggleShadowVisiblity(false);
            ClearPaths();
        }

        private void GetThrown()
        {
            PlayerController thrower = this._carrier;
            SpeculativeRigidbody body = base.m_aiActor.specRigidbody;
            body.CollideWithTileMap = true;
            body.Reinitialize();
            body.CorrectForWalls();

            this._state = THROW;
            float throwAngle = (this._carrier.unadjustedAimPoint.XY() - this._carrier.CenterPosition).ToAngle();
            this._launchTarget = this._carrier.CenterPosition + throwAngle.ToVector(_LAUNCH_DIST_MAX);
            LaunchTowardsTarget(this._launchTarget, wasThrown: true);
            this._carrier = null;
        }

        private static bool _RecursivePlay = false;
        private void EnsureSmoothIdleAnimationTransition(tk2dSpriteAnimator animator, tk2dSpriteAnimationClip clip)
        {
            if (_RecursivePlay)
                return;

            tk2dSpriteAnimationClip curClip = animator.CurrentClip;
            if (curClip == null || !curClip.name.Contains("idle") || !clip.name.Contains("idle"))
                return;

            _RecursivePlay = true;
            animator.PlayFromFrame(clip, (m_aiActor.spriteAnimator.CurrentFrame + 1) % curClip.frames.Length);
            _RecursivePlay = false;
        }

        public override void Destroy()
        {
            if (m_aiActor)
                m_aiActor.MovementModifiers -= this.AdjustMovement;
            if (this._jumpSprite)
                UnityEngine.Object.Destroy(this._jumpSprite);
            CwaffEvents.OnEmptyInteract -= this.AttemptToPickUpOrThrow;
            base.Destroy();
        }

        public override void Upkeep()
        {
            base.Upkeep();
            DecrementTimer(ref m_repathTimer);
        }

        private AIActor FindTargetableEnemy()
        {
            PlayerController owner = m_companionController.m_owner;
            if (owner.CurrentRoom is not RoomHandler room)
                return null;
            if (m_aiActor.CenterPosition.GetAbsoluteRoom() != room)
                return null;

            Vector2 myPos = m_aiActor.specRigidbody.UnitCenter;
            AIActor nearest = null;
            float nearestDist = 999f;

            foreach (AIActor enemy in room.SafeGetEnemiesInRoom())
            {
                if (!enemy || !enemy.isActiveAndEnabled || !enemy.IsWorthShootingAt)
                    continue;
                if (enemy.healthHaver is not HealthHaver hh || hh.IsDead)
                    continue;

                float sqrDist = (enemy.CenterPosition - myPos).sqrMagnitude;
                if (sqrDist > nearestDist)
                    continue;

                nearest = enemy;
                nearestDist = sqrDist;
            }
            return nearest;
        }

        private void DetermineNewTarget()
        {
            PlayerController owner = m_companionController.m_owner;

            this._allowPathing = true;

            if (!base.m_aiAnimator.IsPlaying("idle"))
                base.m_aiAnimator.PlayUntilCancelled("idle");
            if (owner.IsInCombat && FindTargetableEnemy() is AIActor target)
            {
                this._targetActor = target;
                this._state = CHASE;
                this.m_stateTimer = _CHASE_RETARGET_TIME;
                return;
            }

            this._targetActor = null;
            RoomHandler targetRoom;
            if (owner && owner.healthHaver && owner.healthHaver.IsAlive && owner.CurrentRoom is RoomHandler ownerRoom)
                targetRoom = ownerRoom;
            else
                targetRoom = m_aiActor.CenterPosition.GetAbsoluteRoom();

            if (targetRoom == null || this._state == WANDER)
            {
                this._targetPos = this.m_aiActor.CenterPosition;
                this._state = IDLE;
                this._allowPathing = false;
                ClearPaths();
                this.m_stateTimer = UnityEngine.Random.Range(_IDLE_TIME_MIN, _IDLE_TIME_MAX);
                return;
            }

            this._targetPos = targetRoom.GetRandomVisibleClearSpot(2, 2).ToVector2();
            this._state = WANDER;
            this.m_stateTimer = UnityEngine.Random.Range(_WANDER_TIME_MIN, _WANDER_TIME_MAX);
        }

        private bool IsTargetValid()
        {
            if (this._state == CHASE)
                return this.m_stateTimer > 0f && this._targetActor && this._targetActor.healthHaver && this._targetActor.healthHaver.IsAlive;
            return true;
        }

        private void UpdateStateAndTargetPosition()
        {
            if (!IsTargetValid())
                DetermineNewTarget();
            if (!this._airborne && this._state != CARRY)
                SetTheWorldAblaze();
            if (!this._targetActor || !this._targetActor.sprite)
                return;
            if (this._state == CHASE)
                this._targetPos = this._targetActor.sprite.WorldBottomCenter;
            else if (this._state == CHARGE)
                this._launchTarget = this._targetActor.sprite.WorldBottomCenter;
        }

        private static DeadlyDeadlyGoopManager _FireGooper = null;
        private void SetTheWorldAblaze()
        {
            if (!_FireGooper)
                _FireGooper = DeadlyDeadlyGoopManager.GetGoopManagerForGoopType(EasyGoopDefinitions.FireDef);
            _FireGooper.AddGoopCircle(m_aiActor.specRigidbody.UnitBottomCenter, 1.5f);
        }

        private bool ReachedTarget()
        {
            switch(this._state)
            {
                case CHASE:
                    return ((this._targetPos - m_aiActor.CenterPosition).sqrMagnitude <= _LAUNCH_RADIUS_SQR) && m_aiActor.CenterPosition.HasLineOfSight(this._targetPos);
                case IDLE:
                case WANDER:
                case CHARGE:
                    return this.m_stateTimer <= 0f;
                case JUMP:
                case THROW:
                    return !this._airborne;
                case CARRY:
                    return false;
            }
            return false;
        }

        private void ResetState()
        {
            ClearPaths();
            this._allowPathing = false;
            this._airborne = false;
            this._targetActor = null;
            this._state = IDLE;
            this.m_stateTimer = UnityEngine.Random.Range(_IDLE_TIME_MIN, _IDLE_TIME_MAX);
            this._targetPos = this.m_aiActor.CenterPosition;
            this.m_aiActor.SetIsFlying(false, _LAUNCH_REASON, adjustShadow: false);
            this.m_aiActor.SetIsFlying(false, _CARRY_REASON, adjustShadow: false);
            ResetShadow();
            DisableSecondaryRenderer();
            if (!base.m_aiAnimator.IsPlaying("idle"))
                base.m_aiAnimator.PlayUntilCancelled("idle");
        }

        private void ResetShadow()
        {
            this.m_aiActor.ToggleShadowVisiblity(true);
            GameObject shadowObject = this.m_aiActor.ShadowObject;
            shadowObject.transform.localPosition =
                (this.m_aiActor.specRigidbody.UnitBottomCenter - this.m_aiActor.transform.position.XY()).ToVector3ZUp(0.1f);
            shadowObject.transform.position = shadowObject.transform.position.Quantize(0.0625f);
            shadowObject.transform.localPosition += this.m_aiActor.ActorShadowOffset;
        }

        private void OnReachedTarget()
        {
            switch(this._state)
            {
                case IDLE:
                case WANDER:
                    DetermineNewTarget();
                    return;
                case CHASE:
                    this._state = CHARGE;
                    this.m_stateTimer = _CHARGE_TIME;
                    base.m_aiAnimator.PlayUntilCancelled("charge");
                    this._launchTarget = this._targetPos;
                    this._targetPos = this.m_aiActor.CenterPosition;
                    return;
                case CHARGE:
                    if (!this._targetActor || !this._targetActor.isActiveAndEnabled ||
                        !this._targetActor.healthHaver || !this._targetActor.healthHaver.IsAlive)
                    {
                        ResetState();
                        return;
                    }
                    this._state = JUMP;
                    this._launchTarget = this._targetActor.sprite ? this._targetActor.sprite.WorldBottomCenter : this._targetActor.CenterPosition;
                    LaunchTowardsTarget(this._launchTarget, wasThrown: false);
                    return;
                case JUMP:
                case THROW:
                    ResetState();
                    return;
                case CARRY:
                    return;
            }
        }

        private void LaunchTowardsTarget(Vector2 targetPos, bool wasThrown)
        {
            base.m_aiAnimator.PlayUntilCancelled("jump");
            this.m_aiActor.gameObject.Play("ignizol_launch_sound");
            this._launchStart = this.m_aiActor.CenterPosition;
            this._launchTarget = targetPos + Lazy.RandomVector(_AIM_DEVIANCE);
            Vector2 delta = this._launchTarget - this._launchStart;
            float sqrMag = delta.sqrMagnitude;
            if (sqrMag > _LAUNCH_DIST_MAX_SQR)
            {
                delta = _LAUNCH_DIST_MAX * delta.normalized;
                this._launchTarget = this._launchStart + delta;
            }
            else if (sqrMag < _LAUNCH_DIST_MIN_SQR)
            {
                delta = _LAUNCH_DIST_MIN * delta.normalized;
                this._launchTarget = this._launchStart + delta;
            }
            this._launchVelocity = delta / _LAUNCH_TIME;
            this._launchBeginTime = BraveTime.ScaledTimeSinceStartup;
            this.m_aiActor.SetIsFlying(true, _LAUNCH_REASON, adjustShadow: false);
            this.m_aiActor.ToggleShadowVisiblity(false);
            this._airborne = true;
            this._allowPathing = false;
        }

        private void ClearPaths()
        {
            m_aiActor.ClearPath();
        }

        private bool CorrectForInaccessibleCell()
        {
            IntVector2 pos = m_aiActor.specRigidbody.UnitCenter.ToIntVector2(VectorConversions.Floor);
            CellData currentCell = GameManager.Instance.Dungeon.data[pos];
            if (currentCell == null || !currentCell.IsPlayerInaccessible)
                return false;

            if (m_repathTimer > 0f)
                return true;

            m_repathTimer = _PATH_INTERVAL;
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
            // adjust relative to the center of our sprite
            Vector2 bottomLeft = m_aiActor.transform.position.XY();
            Vector2 center = m_aiActor.sprite.WorldCenter;
            Vector2 adjustedTarget = this._targetPos + (bottomLeft - center);

            if (m_repathTimer > 0f)
                return;

            m_repathTimer = _PATH_INTERVAL;
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
                DetermineNewTarget();
            }
            else if (!m_aiActor.Path.WillReachFinalGoal && (++m_sequentialPathFails) > 3)
            {
                CellData cellData2 = GameManager.Instance.Dungeon.data[adjustedTarget.ToIntVector2(VectorConversions.Floor)];
                if (cellData2 != null && cellData2.IsPassable)
                {
                    m_sequentialPathFails = 0;
                    m_aiActor.CompanionWarp(adjustedTarget);
                    DetermineNewTarget();
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
            float newSpeed = _MOVE_SPEED;
            tk2dSpriteAnimationClip clip = m_aiActor.spriteAnimator.currentClip;
            if (clip.name.Contains("idle"))
            {
                int frame = m_aiActor.spriteAnimator.CurrentFrame;
                if (this._state != IDLE && frame == 7 && this._lastIdleFrame != 7 && m_aiActor.MovementSpeed > 0)
                    this.m_aiActor.gameObject.Play("ignizol_hop_sound");
                this._lastIdleFrame = frame;
                newSpeed = (frame >= 3 && frame <= 6) ? _MOVE_SPEED : 0f;  // only move during airborne frames of animation
            }
            m_aiActor.MovementSpeed = newSpeed;
        }

        private void ToggleRendererAndOutlines(tk2dBaseSprite sprite, bool enabled)
        {
            if (sprite.renderer.enabled == enabled)
                return;
            sprite.renderer.enabled = enabled;
            if (enabled)
                SpriteOutlineManager.AddOutlineToSprite(sprite, Color.black, 0.2f, 0.05f);
            else
                SpriteOutlineManager.RemoveOutlineFromSprite(sprite);
        }

        private void AdjustMovement(ref Vector2 voluntaryVel, ref Vector2 involuntaryVel)
        {
            #if DEBUG
                if (_debugNameTag)
                {
                    _debugNameTag.SetName($"{this._state.ToString()}\n clip: {base.m_aiAnimator.spriteAnimator.CurrentClip.name}");
                    _debugNameTag.UpdateWhileParentAlive();
                }
            #endif

            Vector2 curPos = m_aiActor.specRigidbody.UnitCenter;

            if (this._light)
            {
                float flicker = Mathf.Abs(Mathf.Sin(_LIGHT_FLICKER_RATE * BraveTime.ScaledTimeSinceStartup));
                this._light.SetRadius(_LIGHT_RADIUS_MIN + _LIGHT_RADIUS_DLT * flicker);
                this._light.SetBrightness(_LIGHT_BRIGHTNESS_MIN + _LIGHT_BRIGHTNESS_DLT * flicker);
            }

            if (this._state == CHARGE || this._state == CARRY)
            {
                voluntaryVel = Vector2.zero;
                involuntaryVel = Vector2.zero;
                if (this._state == CHARGE)
                {
                    m_aiActor.aiAnimator.FacingDirection = (this._launchTarget - curPos).ToAngle();
                    return;
                }
                if (this._carrier)
                {
                    m_aiActor.aiAnimator.FacingDirection = this._carrier.FacingDirection;
                    // Vector2 offset = m_aiActor.sprite.WorldBottomCenter - m_aiActor.transform.position.XY();
                    // ETGModConsole.Log($"offset is {offset.x},{offset.y}");
                    // Vector3 carryPos = (this._carrier.sprite.WorldBottomCenter/* - offset*/ + new Vector2(-0.5f, 1f)).Quantize(0.0625f);
                    Vector3 carryPos = (this._carrier.sprite.WorldTopCenter/* - offset*/ + new Vector2(-0.5f, 0f)).Quantize(0.0625f);
                    if (this.m_stateTimer <= 0.0f)
                    {
                        m_aiActor.transform.position = carryPos; // snap to player
                        bool flipped = this._carrier.sprite.FlipX;
                        if (UpdateSecondaryRenderer() || this._launchSpriteFlipped != flipped)
                        {
                            this._jumpSprite.transform.position = carryPos;
                            this._jumpSprite.transform.parent = this._carrier.sprite.transform;
                            this._launchSpriteFlipped = flipped;
                        }
                        this._jumpSprite.transform.position = this._jumpSprite.transform.position.WithY(this._carrier.sprite.WorldTopCenter.y);
                        this._jumpSprite.UpdateZDepth();
                    }
                    else
                    {
                        Vector3 carryDelta = carryPos - m_aiActor.transform.position;
                        float pickupPercent = (1f - this.m_stateTimer / _PICKUP_TIME);
                        Vector3 forcedLerp = (pickupPercent * pickupPercent) * carryDelta;
                        m_aiActor.transform.position = Lazy.SmoothestLerp(m_aiActor.transform.position + forcedLerp, carryPos, 14f);
                    }
                    m_aiActor.specRigidbody.Reinitialize();
                }
                return;
            }

            if (this._state != JUMP && this._state != THROW)
            {
                involuntaryVel = Vector2.zero;
                UpdateMovementSpeed();
                return;
            }

            voluntaryVel = Vector2.zero;
            involuntaryVel = this._launchVelocity;
            float percentDone = (BraveTime.ScaledTimeSinceStartup - this._launchBeginTime) / _LAUNCH_TIME;
            if (percentDone < 1f)
            {
                UpdateSecondaryRenderer();
                this._jumpSprite.transform.parent = m_aiActor.sprite.transform;
                this._jumpSprite.transform.localPosition = new Vector3(0, _LAUNCH_HEIGHT * Mathf.Sin(Mathf.PI * percentDone), 0);
                return;
            }

            involuntaryVel = Vector2.zero;
            this._airborne = false; // triggers the next state
            this.m_aiActor.SetIsFlying(false, "launched");
            ResetShadow();
            DisableSecondaryRenderer();
        }

        private bool UpdateSecondaryRenderer()
        {
            bool enabledThisFrame = !this._jumpSprite.renderer.enabled;
            ToggleRendererAndOutlines(this._jumpSprite, true);
            this._jumpSprite.SetSprite(this.m_aiActor.sprite.collection, this.m_aiActor.sprite.spriteId);
            ToggleRendererAndOutlines(this.m_aiActor.sprite, false);
            return enabledThisFrame;
        }

        private void DisableSecondaryRenderer()
        {
            ToggleRendererAndOutlines(this._jumpSprite, false);
            ToggleRendererAndOutlines(this.m_aiActor.sprite, true);
        }

        private bool InDifferentRoom()
        {
            Vector2 pos = m_aiActor.CenterPosition;
            if (m_companionController.m_owner is PlayerController pc && pc.CurrentRoom is RoomHandler ownerRoom)
                return ownerRoom != pos.GetAbsoluteRoom();
            return !GameManager.Instance.MainCameraController.PointIsVisible(pos, 0.4f);
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

            DecrementTimer(ref m_stateTimer);

            UpdateStateAndTargetPosition();
            if (InDifferentRoom() && this._state != CARRY)
            {
                m_aiActor.CompanionWarp(m_companionController.m_owner.CenterPosition);
                DetermineNewTarget();
            }
            else if (ReachedTarget())
                OnReachedTarget();
            else if (this._allowPathing)
                RepathToTarget();

            return BehaviorResult.SkipRemainingClassBehaviors;
        }
    }
}
