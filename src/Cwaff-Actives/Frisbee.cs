namespace CwaffingTheGungy;

using System;
using static FrisbeeBehaviour.State;

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
        item.SetCooldownType(ItemBuilder.CooldownType.Timed, 1f);

        _FrisbeePrefab = VFX.Create("frisbee_vfx", fps: 8, loops: true, anchor: Anchor.MiddleCenter);
        _FrisbeePrefab.AddComponent<FrisbeeBehaviour>();
        SpeculativeRigidbody body = _FrisbeePrefab.AddComponent<SpeculativeRigidbody>();
        body.CanBePushed          = true;
        body.CollideWithTileMap   = true;
        body.CollideWithOthers    = true;
        body.PixelColliders       = new List<PixelCollider>(){new(){
          ColliderGenerationMode = PixelCollider.PixelColliderGeneration.Manual,
          ManualOffsetX          = -6,  //TODO: adjust these from suncaster prism to frisbee dimensions
          ManualOffsetY          = -18,
          ManualWidth            = 13,
          ManualHeight           = 24,
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
                this._frisbee.Launch(user.m_lastNonzeroCommandedDirection.ToAngle().Quantize(45f, VectorConversions.Round));
                break;
            case RIDDEN:
            this._frisbee.RollOff();
                this._frisbee.Catch();
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
            case RIDDEN   : return true;
            case DROPPED  : return (this._frisbee.GetComponent<tk2dSprite>().WorldCenter - user.CenterPosition).sqrMagnitude < GRAB_RANGE_SQR;
            case COOLDOWN : return false;
        }
        return true;
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
    private int _preride = 0;

    private void Awake()
    {
        this._body = base.GetComponent<SpeculativeRigidbody>();
        this._body.OnPreRigidbodyCollision += this.OnPreRigidbodyCollision;
        this._body.OnTileCollision += this.OnTileCollision;
    }

    private void Start()
    {
        base.GetComponent<tk2dSprite>().HeightOffGround = -2f;
    }

    private void OnTileCollision(CollisionData tileCollision)
    {
        PhysicsEngine.PostSliceVelocity = _FRISBEE_SPEED * tileCollision.Normal;
    }

    private void OnPreRigidbodyCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
    {
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

        this._preride = 120;
        // Vector2 center = base.GetComponent<tk2dSprite>().WorldCenter - this._rider.sprite.GetRelativePositionFromAnchor(Anchor.LowerCenter);
        // pc.specRigidbody.Reinitialize();
        // pc.gameObject.transform.localPosition = Vector3.zero;
        // pc.gameObject.transform.position = center;
        // pc.sprite.transform.position     = center;
        // pc.transform.position            = center;
        // pc.gameObject.transform.parent = this._body.transform;
        // pc.specRigidbody.Reinitialize();

        // pc.gameObject.transform.position =
        //     base.GetComponent<tk2dSprite>().WorldCenter - this._rider.sprite.GetRelativePositionFromAnchor(Anchor.LowerCenter);
        // pc.specRigidbody.Reinitialize();

        if (pc.knockbackDoer)
            pc.knockbackDoer.ClearContinuousKnockbacks();
        if (pc.IsDodgeRolling)
            pc.ForceStopDodgeRoll();

        pc.CurrentInputState = PlayerInputState.NoMovement;
        pc.knockbackDoer.ClearContinuousKnockbacks();
        pc.specRigidbody.Velocity = Vector2.zero;
        pc.SetIsFlying(true, Frisbee.ItemName);

        Vector2 center = base.GetComponent<tk2dSprite>().WorldCenter - this._rider.sprite.GetRelativePositionFromAnchor(Anchor.LowerCenter);
        pc.transform.position = center;
        this._body.RegisterCarriedRigidbody(pc.specRigidbody);
    }

    public void RollOff()
    {
        PlayerController pc = this._rider;
        pc.ClearInputOverride(Frisbee.ItemName);
        pc.SetIsFlying(true, Frisbee.ItemName);
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

    private void UpdateRider()
    {
        if (this._state == RIDDEN && this._rider)
        {
            PlayerController pc   = this._rider;
            Vector2 center        = base.GetComponent<tk2dSprite>().WorldCenter + this._rider.transform.position.XY() - this._rider.specRigidbody.UnitBottomCenter;
            pc.transform.position = center;
            pc.specRigidbody.Reinitialize();
        }
    }

    private void Update()
    {
        // UpdateRider();
    }

    private void LateUpdate()
    {
        UpdateRider();
        return;

        if (this._state != FLYING && this._state != RIDDEN)
            return;

        if (this._state == RIDDEN && this._rider)
        {
            // if (this._repositioned > 0)
            // {
            //     Vector2 myPos = base.GetComponent<tk2dSprite>().WorldCenter;
            //     // this._rider.gameObject.transform.parent = null;
            //     this._rider.gameObject.transform.position = myPos - this._rider.sprite.GetRelativePositionFromAnchor(Anchor.LowerCenter);
            //     // this._rider.gameObject.transform.parent = this._body.transform;
            //     --this._repositioned;
            // }
            // this._rider.specRigidbody.Reinitialize();

            // if (this._preride)
            // {
            //     // ETGModConsole.Log($"preride");
            //     PlayerController pc = this._rider;

            //     Vector2 center = base.GetComponent<tk2dSprite>().WorldCenter - this._rider.sprite.GetRelativePositionFromAnchor(Anchor.LowerCenter);
            //     pc.specRigidbody.Reinitialize();
            //     pc.gameObject.transform.localPosition = Vector3.zero;
            //     pc.gameObject.transform.position = center;
            //     pc.sprite.transform.position     = center;
            //     pc.transform.position            = center;
            //     pc.gameObject.transform.parent = this._body.transform;
            //     pc.specRigidbody.Position = new Position(center);
            //     pc.specRigidbody.Reinitialize();
            // }

            // PlayerController pc = this._rider;

            // Vector2 center = base.GetComponent<tk2dSprite>().WorldCenter - this._rider.sprite.GetRelativePositionFromAnchor(Anchor.LowerCenter);
            // // pc.specRigidbody.Reinitialize();
            // pc.gameObject.transform.localPosition = Vector3.zero;
            // pc.gameObject.transform.position = center;
            // pc.sprite.transform.position     = center;
            // pc.transform.position            = center;
            // pc.gameObject.transform.parent = this._body.transform;
            // pc.specRigidbody.Position = new Position(center);
            // pc.specRigidbody.Reinitialize();
        }
    }
}
