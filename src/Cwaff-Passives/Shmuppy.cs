namespace CwaffingTheGungy;

using static ShmuppyCompanion.ShmuppyMovementBehavior.State;

public class Shmuppy : CwaffCompanion
{
    public static string ItemName         = "Shmuppy";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    public static string CompanionName    = "Shmuppy";

    public static void Init()
    {
        PassiveItem item  = Lazy.SetupPassive<Shmuppy>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality      = ItemQuality.D;

        ShmuppyCompanion friend = item.InitCompanion<ShmuppyCompanion>(friendName: CompanionName.ToID(), baseFps: 20,
            extraAnims: ["bark", "hurt", "death"/*, "pitfall"*/]);

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
            FLEE,     // fleeing from a distracted enemy once a projectile gets close
            PLAYDEAD, // playing dead during a boss fight
        }

        private const float _SPEED = 10.0f;

        private State _state = IDLE;

        public override void Start()
        {
            base.Start();
            this.retargetOnPathingFailure = true;
            m_aiActor.specRigidbody.CollideWithOthers = true; //NOTE: this doesn't work in CwaffCompanion.InitCompanion for some reason...investigate later
            m_aiActor.MovementSpeed = _SPEED;
        }

        protected override void TickMovement(ref Vector2 voluntaryVel, ref Vector2 involuntaryVel)
        {
            base.TickMovement(ref voluntaryVel, ref involuntaryVel);
        }

        protected override void UpdateStateAndTargetPosition()
        {
            if (!IsTargetValid())
                DetermineNewTarget();
        }

        protected override void DetermineNewTarget()
        {
            if (m_companionController.m_owner)
              this._targetPos = m_companionController.m_owner.CenterPosition;
        }

        protected override bool ReachedTarget()
        {
            return NearTargetPos(2f);
        }
    }
}
