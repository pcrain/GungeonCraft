namespace CwaffingTheGungy;

using System;
using static FrisbeeBehaviour.State;

public class Frisbee : CwaffActive
{
    public static string ItemName         = "Frisbee";
    public static string ShortDescription = "Well-inspired";
    public static string LongDescription  = "";
    public static string Lore             = "";

    private const float GRAB_RANGE = 2f;
    private const float GRAB_RANGE_SQR = GRAB_RANGE * GRAB_RANGE;

    internal static GameObject _FrisbeePrefab = null;

    private PlayerController _owner = null;
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
                this._frisbee.Launch(user);
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
        this.CanBeDropped = (this._state == INACTIVE);
    }

    public override void Pickup(PlayerController player)
    {
        this._owner = player;
        this._owner.specRigidbody.OnPreRigidbodyCollision -= this.OnPreRigidbodyCollision;
        this._owner.specRigidbody.OnPreRigidbodyCollision += this.OnPreRigidbodyCollision;
        base.Pickup(player);
    }

    private void OnPreRigidbodyCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
    {
        if (this._state != RIDDEN)
            return;
        if (otherRigidbody.GetComponent<Projectile>() is not Projectile proj)
            return;
        if (proj.Owner is PlayerController)
            return;
        PassiveReflectItem.ReflectBullet(
            p                       : proj,
            retargetReflectedBullet : true,
            newOwner                : myRigidbody.gameActor,
            minReflectedBulletSpeed : 30f,
            scaleModifier           : 1f,
            damageModifier          : 1f,
            spread                  : 0f);
        PhysicsEngine.SkipCollision = true;
    }

    public override void OnPreDrop(PlayerController player)
    {
        if (this._owner)
            this._owner.specRigidbody.OnPreRigidbodyCollision -= this.OnPreRigidbodyCollision;
        this._owner = null;
        base.OnPreDrop(player);
    }

    public override void OnDestroy()
    {
        if (this._owner)
            this._owner.specRigidbody.OnPreRigidbodyCollision -= this.OnPreRigidbodyCollision;
        base.OnDestroy();
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
    private const float _SOUND_RATE = 0.16f;

    internal State _state = State.INACTIVE;
    private PlayerController _owner = null;
    private PlayerController _rider = null;
    private SpeculativeRigidbody _body = null;
    private float _soundTimer = 0.0f;
    private int _framesSinceLastCollision = 9999;

    private void Awake()
    {
        this._body = base.GetComponent<SpeculativeRigidbody>();
        this._body.OnPreRigidbodyCollision += this.OnPreRigidbodyCollision;
        this._body.OnRigidbodyCollision += this.OnRigidbodyCollision;
        this._body.OnTileCollision += this.OnTileCollision;
    }

    private void OnRigidbodyCollision(CollisionData rigidbodyCollision)
    {
        if (this._framesSinceLastCollision < 2)
        {
            Catch();
            return;
        }
        this._framesSinceLastCollision = 0;
        PhysicsEngine.PostSliceVelocity = _FRISBEE_SPEED * rigidbodyCollision.Normal.ToAngle().Quantize(90f, VectorConversions.Round).ToVector();
        base.gameObject.PlayOnce("frisbee_bounce_sound");
    }

    private void Start()
    {
        base.GetComponent<tk2dSprite>().HeightOffGround = 0.4f;
    }

    private void OnTileCollision(CollisionData tileCollision)
    {
        if (this._framesSinceLastCollision < 2)
        {
            Catch();
            return;
        }
        this._framesSinceLastCollision = 0;
        if (this._owner && this._state == FLYING && this._body.UnitCenter.GetAbsoluteRoom() != this._owner.CurrentRoom)
        {
            Catch();
            return;
        }
        PhysicsEngine.PostSliceVelocity = _FRISBEE_SPEED * tileCollision.Normal.ToAngle().Quantize(90f, VectorConversions.Round).ToVector();
        base.gameObject.PlayOnce("frisbee_bounce_sound");
    }

    private void OnPreRigidbodyCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
    {
        if (otherRigidbody.majorBreakable && otherRigidbody.majorBreakable.IsSecretDoor)
        {
            // NOTE: this is dog roll code, idk why they do it 3 times but i'm scared it'll break if i don't
            otherRigidbody.gameObject.Play("Play_OBJ_wall_reveal_01");
            otherRigidbody.majorBreakable.ApplyDamage(damage: 1E+10f, sourceDirection: Vector2.zero, isSourceEnemy: false, isExplosion: true, ForceDamageOverride: true);
            otherRigidbody.majorBreakable.ApplyDamage(damage: 1E+10f, sourceDirection: Vector2.zero, isSourceEnemy: false, isExplosion: true, ForceDamageOverride: true);
            otherRigidbody.majorBreakable.ApplyDamage(damage: 1E+10f, sourceDirection: Vector2.zero, isSourceEnemy: false, isExplosion: true, ForceDamageOverride: true);
        }
        if (otherRigidbody.minorBreakable)
            PhysicsEngine.SkipCollision = true;
        else if (otherRigidbody.GetComponent<GameActor>())
            PhysicsEngine.SkipCollision = true;
        else if (otherRigidbody.transform.parent && otherRigidbody.transform.parent.GetComponent<DungeonDoorController>() is DungeonDoorController door)
        {
            if (!door.IsOpen && !door.isLocked && this._state == RIDDEN)
                door.Open();
            if (door.IsOpen)
                PhysicsEngine.SkipCollision = true;
        }

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
        pc.FallingProhibited = true;
        // pc.IsGunLocked = true;
        this._body.RegisterCarriedRigidbody(pc.specRigidbody);

        UpdateRider();
    }

    public void Launch(PlayerController user)
    {
        this._owner = user;
        float launchAngle = user.m_currentGunAngle.Quantize(90f, VectorConversions.Round);
        this._state = FLYING;
        this._soundTimer = 0.0f;
        this._body.CorrectForWalls();
        this._body.Velocity = launchAngle.ToVector(_FRISBEE_SPEED);
        base.gameObject.PlayOnce("frisbee_throw_sound");
    }

    public void Catch()
    {
        if (this._rider is PlayerController pc)
        {
            pc.m_overrideGunAngle = null;
            pc.forceAimPoint = null;
            pc.ClearInputOverride(Frisbee.ItemName);
            pc.FallingProhibited = false;
            // pc.IsGunLocked = false;
            this._body.DeregisterCarriedRigidbody(pc.specRigidbody);
            if (!pc.healthHaver.IsDead)
            {
                pc.specRigidbody.CorrectForWalls(andRigidBodies: true);
                pc.CurrentInputState = PlayerInputState.AllInput;
                pc.ForceStartDodgeRoll();
            }
        }

        this._state = COOLDOWN;
        this._rider = null;
        LootEngine.DoDefaultItemPoof(this._body.UnitBottomCenter);
        UnityEngine.Object.Destroy(this.gameObject);
        // play catch sound
    }

    private void Update()
    {
        if (this._state != FLYING && this._state != RIDDEN)
            return;

        ++this._framesSinceLastCollision;
        if ((this._soundTimer += BraveTime.DeltaTime) > _SOUND_RATE)
        {
            this._soundTimer = 0.0f;
            base.gameObject.PlayOnce("frisbee_spin_sound_alt");
        }

        base.GetComponent<tk2dSprite>().transform.localRotation = AngleFromFrisbeeAnimation().EulerZ();
        if (this._state != RIDDEN || !this._rider)
            return;

        if (this._rider.healthHaver.IsDead)
        {
            Catch();
            return;
        }

        GungeonActions activeActions = BraveInput.GetInstanceForPlayer(this._rider.PlayerIDX).ActiveActions;
        if (!activeActions.DodgeRollAction.WasPressed || this._rider.WasPausedThisFrame)
            return;
        if (activeActions.Move.Vector.magnitude <= 0.1f)
            return;
        Catch();
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

/// <summary>Patch to make FlippableCovers not spew debug warnings when we're riding a frisbee above them (more generally, when we're in a NoMovement state).</summary>
[HarmonyPatch(typeof(FlippableCover), nameof(FlippableCover.GetFlipDirection))]
internal static class FlippableCoverPatch
{
    static bool Prefix(FlippableCover __instance, SpeculativeRigidbody flipperRigidbody, ref DungeonData.Direction __result)
    {
        if (flipperRigidbody.GetComponent<PlayerController>() is not PlayerController pc)
            return true; // call the original method
        if (pc.CurrentInputState != PlayerInputState.NoMovement)
            return true; // call the original method

          __result = DungeonData.Direction.NORTH; // change the original result
        return false;    // skip the original method
    }
}
