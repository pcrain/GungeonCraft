namespace CwaffingTheGungy;

using System;
using static FrisbeeBehaviour.State;


/* TODO:
    - don't allow throwing frisbee too close to the wall (or it can get stuck)
    - improve collision masks
    - add sounds
    - add better frisbee with spinning animation
    - add invulnerability
*/

public class Frisbee : CwaffActive
{
    public static string ItemName         = "Frisbee";
    public static string ShortDescription = "";
    public static string LongDescription  = "";
    public static string Lore             = "";

    private const float GRAB_RANGE = 2f;
    private const float GRAB_RANGE_SQR = GRAB_RANGE * GRAB_RANGE;

    internal static GameObject _FrisbeePrefab = null;

    private FrisbeeBehaviour _frisbee = null;
    private FrisbeeBehaviour.State _state => _frisbee ? _frisbee._state : FrisbeeBehaviour.State.INACTIVE;

    public static void Init()
    {
        PlayerItem item = Lazy.SetupActive<Frisbee>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality    = ItemQuality.D;
        item.consumable = false;
        item.SetCooldownType(ItemBuilder.CooldownType.Timed, 0.2f);

        _FrisbeePrefab = VFX.Create("frisbee_vfx", fps: 8, loops: true, anchor: Anchor.MiddleCenter);
        _FrisbeePrefab.AddComponent<FrisbeeBehaviour>();
        SpeculativeRigidbody body = _FrisbeePrefab.AddComponent<SpeculativeRigidbody>();
        body.CanBePushed          = true;
        body.CollideWithTileMap   = true;
        body.CollideWithOthers    = true;
        body.PixelColliders       = new List<PixelCollider>(){new(){
          ColliderGenerationMode = PixelCollider.PixelColliderGeneration.Manual,
          ManualOffsetX          = -7,  //TODO: adjust frisbee dimensions once sprite is finalized
          ManualOffsetY          = -7,
          ManualWidth            = 14,
          ManualHeight           = 14,
          CollisionLayer         = CollisionLayer.Projectile,
          Enabled                = true,
          IsTrigger              = false,
        }};
    }

    public override void DoEffect(PlayerController user)
    {
        switch(this._state)
        {
            case INACTIVE:
                if (!this._frisbee)
                    this._frisbee = _FrisbeePrefab.Instantiate(user.CenterPosition).GetComponent<FrisbeeBehaviour>();
                this._frisbee.Launch(user.m_lastNonzeroCommandedDirection.ToAngle().Quantize(90f, VectorConversions.Round));
                break;
            case FLYING:
            case DROPPED:
                this._frisbee.Catch();
                break;
        }
    }

    public override bool CanBeUsed(PlayerController user)
    {
        if (!base.CanBeUsed(user))
            return false;

        switch(this._state)
        {
            case INACTIVE : return true;
            case FLYING   : return (this._frisbee.GetComponent<tk2dSprite>().WorldCenter - user.CenterPosition).sqrMagnitude < GRAB_RANGE_SQR;
            case RIDDEN   : return false;
            case DROPPED  : return (this._frisbee.GetComponent<tk2dSprite>().WorldCenter - user.CenterPosition).sqrMagnitude < GRAB_RANGE_SQR;
            case COOLDOWN : return false;
        }
        return true;
    }

    public override void Update()
    {
        base.Update();
        this.CanBeDropped = this._state == INACTIVE;
    }
}

public class FrisbeeBehaviour : MonoBehaviour
{
    public enum State {
        INACTIVE,
        FLYING,
        RIDDEN,
        DROPPED,
        COOLDOWN,
    }

    private const float _FRISBEE_SPEED = 20f;

    internal State _state = State.INACTIVE;
    private PlayerController _rider = null;
    private SpeculativeRigidbody _body = null;

    private void Awake()
    {
        this._body = base.GetComponent<SpeculativeRigidbody>();
        this._body.OnPreRigidbodyCollision += this.OnPreRigidbodyCollision;
        this._body.OnRigidbodyCollision += this.OnRigidbodyCollision;
        this._body.OnTileCollision += this.OnTileCollision;
    }

    private void OnRigidbodyCollision(CollisionData rigidbodyCollision)
    {
        PhysicsEngine.PostSliceVelocity = _FRISBEE_SPEED * rigidbodyCollision.Normal.ToAngle().Quantize(90f, VectorConversions.Round).ToVector();
    }

    private void Start()
    {
        base.GetComponent<tk2dSprite>().HeightOffGround = 0.4f;
    }

    private void OnTileCollision(CollisionData tileCollision)
    {
        PhysicsEngine.PostSliceVelocity = _FRISBEE_SPEED * tileCollision.Normal.ToAngle().Quantize(90f, VectorConversions.Round).ToVector();
    }

    private void OnPreRigidbodyCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
    {

        if (otherRigidbody.GetComponent<GameActor>())
            PhysicsEngine.SkipCollision = true;
        if (this._state != FLYING)
            return;
        if (otherRigidbody.GetComponent<PlayerController>() is not PlayerController pc)
            return;
        if (!pc.IsDodgeRolling)
            return;
        HopOn(pc);
    }

    private void HopOn(PlayerController pc)
    {
        this._state = RIDDEN;
        this._rider = pc;

        if (pc.knockbackDoer)
            pc.knockbackDoer.ClearContinuousKnockbacks();
        if (pc.IsDodgeRolling)
            pc.ForceStopDodgeRoll();
        pc.CurrentInputState = PlayerInputState.NoMovement;
        pc.knockbackDoer.ClearContinuousKnockbacks();
        pc.specRigidbody.Velocity = Vector2.zero;
        // pc.SetIsFlying(true, Frisbee.ItemName);
        pc.FallingProhibited = true;
        pc.IsGunLocked = true;
        this._body.RegisterCarriedRigidbody(pc.specRigidbody);

        UpdateRider();
    }

    public void RollOff()
    {
        PlayerController pc = this._rider;
        pc.m_overrideGunAngle = null;
        pc.forceAimPoint = null;
        pc.ClearInputOverride(Frisbee.ItemName);
        pc.FallingProhibited = false;
        pc.IsGunLocked = false;
        this._body.DeregisterCarriedRigidbody(pc.specRigidbody);
        pc.CurrentInputState = PlayerInputState.AllInput;
        pc.ForceStartDodgeRoll();
        Catch();
    }

    public void Launch(float angle)
    {
        this._state = FLYING;
        this._body.Velocity = angle.ToVector(_FRISBEE_SPEED);
    }

    public void Catch()
    {
        this._state = COOLDOWN;
        UnityEngine.Object.Destroy(this.gameObject);
        // play catch sound
    }

    private void Update()
    {
        if (this._state != RIDDEN || !this._rider || this._rider.healthHaver.IsDead)
            return;

        GungeonActions activeActions = BraveInput.GetInstanceForPlayer(this._rider.PlayerIDX).ActiveActions;
        if (!activeActions.DodgeRollAction.WasPressed || this._rider.WasPausedThisFrame)
            return;
        if (activeActions.Move.Vector.magnitude <= 0.1f)
            return;
        RollOff();
    }

    private void LateUpdate()
    {
        UpdateRider();
    }

    private void UpdateRider()
    {
        if (this._state != RIDDEN || !this._rider || this._rider.healthHaver.IsDead)
            return;

        float angle = AngleFromFrisbeeAnimation();
        this._rider.m_overrideGunAngle = angle;
        this._rider.forceAimPoint = this._rider.sprite.WorldCenter + angle.ToVector();
        this._rider.spriteAnimator.PlayFromFrame(this._rider.GetEvenlySpacedIdleAnimation(angle), frame: 0);
        this._rider.spriteAnimator.UpdateAnimation(GameManager.INVARIANT_DELTA_TIME);
        this._rider.transform.position = base.GetComponent<tk2dSprite>().WorldCenter + this._rider.transform.position.XY() - this._rider.specRigidbody.UnitBottomCenter + new Vector2(0.125f, 0.0f);
        this._rider.specRigidbody.Reinitialize();
    }

    private float AngleFromFrisbeeAnimation()
    {
        return (720f * BraveTime.ScaledTimeSinceStartup).Clamp180();
    }
}
