namespace CwaffingTheGungy;

public class FrostEffigy : CwaffActive
{
    public static string ItemName         = "Frost Effigy";
    public static string ShortDescription = "Fragile Immortality";
    public static string LongDescription  = "Places an effigy that prevents all damage to the player while active. The effigy will shatter and damage the player if anything other than the player touches it, including the player's own projectiles.";
    public static string Lore             = "A mystical relic capable of manifesting a veil of frost so cold, nothing can permeate it. Allegedly, its creator originally used it as a means of indefinitely preserving their favorite microwavable meals, with the potential for immortality coming as an afterthought.";

    private const float _VFX_TIMER = 0.1f;

    internal static GameObject _FrostVFX = null;

    private FrostEffigyPlaceable _extantEffigy = null;
    private float _vfxTimer = 0.0f;
    private PlayerController _owner = null;

    public static void Init()
    {
        PlayerItem item = Lazy.SetupActive<FrostEffigy>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality    = ItemQuality.A;
        item.consumable = false;
        ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.Timed, 1f);

        _FrostVFX = VFX.Create("frost_effigy_vfx", useBetterEmission: true, emissiveColour: new Color(95/255f, 205/255f, 228/255f),
          emissivePower: 10.0f, emissiveColorPower: 10.0f, emissiveSensitivity: 1.0f);
    }

    private void OnHealthUIUpdated(PlayerController associatedPlayer, GameUIHeartController guic)
    {
      if (!this._extantEffigy || this._owner != associatedPlayer)
          return;
      guic.m_currentFullHeartName  = "frost_effigy_heart_full_ui";
      guic.m_currentHalfHeartName  = "frost_effigy_heart_half_ui";
      guic.m_currentArmorName      = "frost_effigy_armor_ui";
    }

    public override void Update()
    {
        const float SPEED = 30.0f;
        base.Update();
        if (!this._extantEffigy || !this._owner)
          return;
        this._vfxTimer -= BraveTime.DeltaTime;
        if (this._vfxTimer > 0)
          return;

        this._vfxTimer = _VFX_TIMER;
        Vector2 startPos = this._owner.CenterPosition;
        Vector2 delta = this._extantEffigy._sprite.WorldCenter - startPos;
        Vector2 deltaNorm = delta.normalized;
        CwaffVFX.Spawn(
          prefab        : _FrostVFX,
          position      : startPos,
          rotation      : Lazy.RandomEulerZ(),
          velocity      : SPEED * deltaNorm,
          lifetime      : delta.magnitude / SPEED,
          startScale    : 1.0f,
          endScale      : 0.1f,
          copyShaders   : true,
          unoccluded    : true
          );
    }

    public override void Pickup(PlayerController player)
    {
      base.Pickup(player);
      CwaffEvents.OnChangedRooms -= this.OnChangedRooms;
      CwaffEvents.OnChangedRooms += this.OnChangedRooms;
      CwaffEvents.OnHealthUIUpdated -= this.OnHealthUIUpdated;
      CwaffEvents.OnHealthUIUpdated += this.OnHealthUIUpdated;
      this._owner = player;
    }

    public override void OnPreDrop(PlayerController player)
    {
      if (this._extantEffigy)
        this._extantEffigy.Break(wasGentle: !player.IsInCombat);
      CwaffEvents.OnChangedRooms -= this.OnChangedRooms;
      CwaffEvents.OnHealthUIUpdated -= this.OnHealthUIUpdated;
      this._owner = null;
      base.OnPreDrop(player);
    }

    public override void OnDestroy()
    {
      if (this._extantEffigy)
        this._extantEffigy.Break(wasGentle: !this._owner || !this._owner.IsInCombat);
      CwaffEvents.OnChangedRooms -= this.OnChangedRooms;
      CwaffEvents.OnHealthUIUpdated -= this.OnHealthUIUpdated;
      base.OnDestroy();
    }

    private void OnChangedRooms(PlayerController controller, RoomHandler handler1, RoomHandler handler2)
    {
      if (this._extantEffigy)
        this._extantEffigy.Break(wasGentle: true);
      this._extantEffigy = null;
    }

    public override bool CanBeUsed(PlayerController user)
    {
      if (!user || (this._extantEffigy && user.IsInCombat))
        return false;
      return base.CanBeUsed(user);
    }

    public override void DoEffect(PlayerController user)
    {
      if (this._extantEffigy)
      {
        this._extantEffigy.Break(wasGentle: true);
        this._extantEffigy = null;
        return;
      }
      tk2dBaseSprite sprite = user.sprite.DuplicateInWorld();
      this._extantEffigy = sprite.gameObject.AddComponent<FrostEffigyPlaceable>();
      this._extantEffigy.Setup(this, user);
      this.CanBeDropped = false;
      Material[] mats = user.SetOverrideShader(CwaffShaders.GoldShader);
      foreach (Material mat in mats)
      {
        mat.SetColor("_GoldColor", ExtendedColours.skyblue);
        mat.SetFloat("_SheenSpeed", 10.0f);
      }
      user.DoColorfulBlank(ExtendedColours.skyblue, vfxOnly: true);
    }

    internal void DeregisterEffigy()
    {
      this._extantEffigy = null;
      this.CanBeDropped = true;
      if (this._owner)
        this._owner.ClearOverrideShader();
    }

    public static void DoSparkles(Vector2 pos)
    {
      CwaffVFX.SpawnBurst(prefab: _FrostVFX, numToSpawn: 20,
        basePosition: pos, positionVariance: 1f,
        minVelocity: 1f, velocityVariance: 2f, velType: CwaffVFX.Vel.AwayRadial,
        rotType: CwaffVFX.Rot.Random, lifetime: 1.0f, startScale: 1.0f,
        endScale: 0.25f, emissivePower: 5f, copyShaders: true);
    }
}

// NOTE: interactible code is dummied out but fully functional, I figure it's just better to make it undismissable in combat
public class FrostEffigyPlaceable : MonoBehaviour/*, IPlayerInteractable*/
{
    private const float _MAX_VFX_RATE = 1.0f;

    internal tk2dBaseSprite _sprite    = null;
    private PlayerController _owner    = null;
    private SpeculativeRigidbody _body = null;
    private FrostEffigy _effigyItem    = null;
    private RoomHandler _room          = null;
    private float _nextVfx             = 0.0f;
    private MajorBreakable _breakable  = null;

    public void Setup(FrostEffigy effigyItem, PlayerController user)
    {
      this._effigyItem = effigyItem;
      this._owner = user;
      this._owner.healthHaver.ModifyDamage += this.PreventAllDamage;
      this._body = base.gameObject.AutoRigidBody(canBePushed: true);
      this._body.OnCollision += this.OnCollision;
      this._body.RegisterTemporaryCollisionException(user.specRigidbody);
      PhysicsEngine.Instance.RegisterOverlappingGhostCollisionExceptions(this._body);
      this._sprite = base.gameObject.GetComponent<tk2dBaseSprite>();
      this._sprite.OverrideShader(CwaffShaders.GoldShader);
      this._sprite.renderer.material.SetColor("_GoldColor", ExtendedColours.skyblue);
      this._sprite.renderer.material.SetFloat("_SheenSpeed", 10.0f);
      this._sprite.HeightOffGround = -0.0625f;
      this._sprite.IsPerpendicular = true;
      SpriteOutlineManager.AddOutlineToSprite(this._sprite, Color.black, 2.5f);
      this._sprite.UpdateZDepth();
      this._breakable = base.gameObject.AddComponent<MajorBreakable>();
      this._breakable.spawnShards = false;
      this._breakable.HitPoints = 1;
      this._breakable.destroyedOnBreak = false;
      this._breakable.OnBreak += this.OnBreak;
      this._room = user.CurrentRoom;
      // this._room.RegisterInteractable(this);
      base.gameObject.Play("frost_effigy_deploy_sound");
      FrostEffigy.DoSparkles(this._owner.CenterPosition);
    }

    private void OnBreak()
    {
        Break(wasGentle: false);
    }

    private void PreventAllDamage(HealthHaver hh, HealthHaver.ModifyDamageEventArgs data)
    {
      data.ModifiedDamage = 0.0f;
      float now = BraveTime.ScaledTimeSinceStartup;
      if (this._nextVfx > now)
        return;
      this._nextVfx = now + _MAX_VFX_RATE;
      Lazy.DoDamagedFlash(hh);
      hh.gameObject.Play("frost_effigy_block_sound");
      FrostEffigy.DoSparkles(hh.gameActor.CenterPosition);
    }

    public void Break(bool wasGentle = false)
    {
      if (this._breakable)
        this._breakable.OnBreak -= this.OnBreak;
      if (this._owner && this._owner.healthHaver is HealthHaver hh)
      {
        hh.ModifyDamage -= this.PreventAllDamage;
        if (!wasGentle)
          hh.ApplyDamage(0.5f, Vector2.zero, FrostEffigy.ItemName, CoreDamageTypes.Ice, DamageCategory.Unstoppable, ignoreInvulnerabilityFrames: true);
      }
      base.gameObject.Play("frost_effigy_break_sound");
      FrostEffigy.DoSparkles(this._sprite.WorldCenter);
      // this._room.DeregisterInteractable(this);
      this._effigyItem.DeregisterEffigy();
      UnityEngine.Object.Destroy(base.gameObject);
    }

    private void OnCollision(CollisionData data)
    {
      if (data.OtherRigidbody is SpeculativeRigidbody body && body.gameActor == this._owner)
        return;
      Break(wasGentle: false);
    }

    // public void Interact(PlayerController interactor)
    // {
    //   if (interactor == this._owner)
    //     Break(wasGentle: true);
    // }

    // public void OnEnteredRange(PlayerController interactor)
    // {
    //     if (interactor != this._owner)
    //       return;
    //     SpriteOutlineManager.AddOutlineToSprite(this._sprite, Color.white, 1f);
    //     this._sprite.UpdateZDepth();
    // }

    // public void OnExitRange(PlayerController interactor)
    // {
    //     if (interactor != this._owner)
    //       return;
    //     SpriteOutlineManager.AddOutlineToSprite(this._sprite, Color.black, 1f);
    // }

    // public string GetAnimationState(PlayerController interactor, out bool shouldBeFlipped)
    // {
    //   shouldBeFlipped = false;
    //   return string.Empty;
    // }

    // public float GetDistanceToPoint(Vector2 point)
    // {
    //   if (!this._body)
    //     return 999f;
    //   return Vector2.Distance(point, point.ClosestPointOnCollider(this._body.PrimaryPixelCollider));
    // }

    // public float GetOverrideMaxDistance() => -1f;
}
