namespace CwaffingTheGungy;

using static ShmuppyCompanion.ShmuppyMovementBehavior.State;

public class ChewToy : CwaffCompanion
{
    public static string ItemName         = "Chew Toy";
    public static string ShortDescription = "Bullet Bitten";
    public static string LongDescription  = "Spawns Shmuppy. While in combat, Shmuppy will distract and draw fire from a single enemy. Shmuppy is scared of bosses, and will play dead during boss fights.";
    public static string Lore             = "TBD";

    public static string CompanionName    = "Shmuppy";

    private static readonly string[] Directions = ["back", "back_right", "right", "front_right", "front", "front_left", "left", "back_left"];

    public static void Init()
    {
        PassiveItem item  = Lazy.SetupPassive<ChewToy>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.D;

        ShmuppyCompanion friend = item.InitCompanion<ShmuppyCompanion>(friendName: CompanionName.ToID(), baseFps: 20,
            extraAnims: ["bark", "hurt", "death"/*, "pitfall"*/])
          .SetPettingOffsets(new Vector2(-0.875f, -0.25f), new Vector2(0.6875f, -0.25f));

        tk2dSpriteAnimation library = friend.gameObject.GetComponent<tk2dSpriteAnimation>();
        foreach (string s in Directions)
        {
            tk2dSpriteAnimationClip idleClip = library.GetClipByName("shmuppy_idle_"+s);
            idleClip.fps = 8;
            tk2dSpriteAnimationClip barkClip = library.GetClipByName("shmuppy_bark_"+s);
            barkClip.frames[2].AddSound("shmuppy_bark_sound");
        }

        BehaviorSpeculator bs = friend.gameObject.GetComponent<BehaviorSpeculator>();
        bs.MovementBehaviors.Add(new ShmuppyCompanion.ShmuppyMovementBehavior());
    }
}

public class ShmuppyCompanion : CwaffCompanionController
{
    public class ShmuppyMovementBehavior : CwaffCompanionMovementBehaviorBase
    {
        internal enum State {
            IDLE,     // doing nothing
            FOLLOW,   // following the player
            CHASE,    // chasing an enemy
            BARK,     // barking at an enemy to gain their attention
            DISTRACT, // running around an enemy once they're paying attention
            PLAYDEAD, // playing dead during a boss fight
        }

        private const float _SPEED         = 10.0f;
        private const float NEAR_OWNER     = 2.0f;
        private const float FAR_FROM_OWNER = 6.0f;

        private State _state = IDLE;

        public override void Start()
        {
            base.Start();
            this.retargetOnPathingFailure = true;
            m_aiActor.specRigidbody.CollideWithOthers = true; //NOTE: this doesn't work in CwaffCompanion.InitCompanion for some reason...investigate later
            m_aiActor.MovementSpeed = _SPEED;
        }

        private void ResetState()
        {
            this._state = IDLE;
            this.m_stateTimer = 0.5f;
            base.m_aiAnimator.EndAnimation();
            if (this._targetActor is AIActor enemy && enemy.OverrideTarget == this.m_aiActor.specRigidbody)
              enemy.OverrideTarget = null;
        }

        private void FollowOwner()
        {
            this._targetActor = m_companionController.m_owner;
            this._targetPos = this._targetActor.CenterPosition;
            this._state = NearTargetActor(FAR_FROM_OWNER) ? IDLE : FOLLOW;
        }

        protected override void TickMovement(ref Vector2 voluntaryVel, ref Vector2 involuntaryVel)
        {
            base.TickMovement(ref voluntaryVel, ref involuntaryVel);
        }

        protected override bool IsTargetValid()
        {
            switch(this._state)
            {
                case IDLE:     return true;                  // end state manually
                case FOLLOW:   return this._targetActor;     // end state manually or when player is dead
                case CHASE:    return this._targetActor;     // end state manually or when enemy is dead
                case BARK:     return this._targetActor;     // end state manually or when enemy is dead
                case DISTRACT: return this._targetActor;     // end state manually or when enemy is dead
                case PLAYDEAD: return true;                  // end state manually
            }
            return false;
        }

        protected override void DetermineNewTarget()
        {
            this._allowPathing = true;
            ResetState();
            if (m_companionController.m_owner is not PlayerController owner)
              return;

            FollowOwner();
            if (!owner.IsInCombat)
              return;

            List<AIActor> enemies = Lazy.GetAllNearbyEnemies(m_aiActor.CenterPosition, 16f, ignoreWalls: true);
            if (enemies.Count == 0 || enemies.ChooseRandom() is not AIActor enemy)
              return;

            this._targetActor = enemy;
            this._targetPos = this._targetActor.CenterPosition;
            this._state = CHASE;
            // Lazy.DebugConsoleLog($"switch to chase");
        }

        protected override void UpdateStateAndTargetPosition()
        {
            if (this._targetActor && this._state != DISTRACT)
              this._targetPos = this._targetActor.CenterPosition;
        }

        protected override bool ReachedTarget()
        {
            if (m_companionController.IsBeingPet)
              return false;

            switch(this._state)
            {
                case IDLE:     return true;                        // idle state is always reached
                case FOLLOW:   return NearTargetActor(NEAR_OWNER); // near player
                case CHASE:    return NearTargetActor(5f);         // near enemy
                case BARK:     return NearTargetActor(3f);         // near enemy
                case DISTRACT: return NearTargetPos(3f);           // near target position
                case PLAYDEAD: return !InCombat();                 // boss battle is over
            }
            return false;
        }

        private Vector2 GetRandomVisiblePointInRoom()
        {
            if (m_companionController.m_owner is not PlayerController player)
              return m_aiActor.CenterPosition;
            if (player.CurrentRoom is not RoomHandler room)
              return m_aiActor.CenterPosition;
            return room.GetRandomVisibleClearSpot(2, 2).ToVector2();
        }

        protected override void OnReachedTarget()
        {
            switch(this._state)
            {
                case IDLE:
                {
                  DetermineNewTarget();
                  break;
                }
                case FOLLOW:
                {
                  ResetState();
                  break;
                }
                case CHASE:
                {
                  this._allowPathing = false;
                  m_aiActor.ClearPath();
                  if (this._targetActor && this._targetActor.gameObject.GetComponent<HealthHaver>() is HealthHaver hh)
                    if (hh.IsBoss || hh.IsSubboss)
                      {
                        this._state = PLAYDEAD;
                        base.m_aiAnimator.PlayUntilCancelled("death");
                        break;
                      }
                  // Lazy.DebugConsoleLog($"switch to bark");
                  this._state = BARK;
                  base.m_aiAnimator.PlayUntilCancelled("bark");
                  if (this._targetActor is AIActor enemy && enemy.OverrideTarget == null)
                    enemy.OverrideTarget = this.m_aiActor.specRigidbody;
                  // Lazy.DebugConsoleLog($"  targeting {this._targetPos.x},{this._targetPos.y}");
                  break;
                }
                case BARK:
                {
                  ResetState();
                  if (this._targetActor is AIActor enemy && enemy.OverrideTarget == null)
                    enemy.OverrideTarget = this.m_aiActor.specRigidbody;
                  this._allowPathing = true;
                  this._state = DISTRACT;
                  this.m_stateTimer = 5.0f;
                  this._targetPos = GetRandomVisiblePointInRoom();
                  // Lazy.DebugConsoleLog($"switch to distract");
                  break;
                }
                case DISTRACT:
                {
                  if (this.m_stateTimer <= 0.0f)
                  {
                    // Lazy.DebugConsoleLog($"switch to follow");
                    ResetState();
                    FollowOwner();
                    break;
                  }
                  this._targetPos = GetRandomVisiblePointInRoom();
                  // Lazy.DebugConsoleLog($"  targeting {this._targetPos.x},{this._targetPos.y}");
                  break;
                }
                case PLAYDEAD:
                {
                  FollowOwner();
                  break;
                }
            }
        }
    }
}
