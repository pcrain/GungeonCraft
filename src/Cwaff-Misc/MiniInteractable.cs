namespace CwaffingTheGungy;

public class MiniInteractable : BraveBehaviour, IPlayerInteractable
{
  public delegate IEnumerator InteractionScript(MiniInteractable i, PlayerController p);
  public InteractionScript interactionScript = DefaultInteractionScript;

  public bool interacting = false;
  public bool doVFX = false;
  public bool doHover = false;
  public bool autoInteract = false;

  public DungeonData.Direction itemFacing = DungeonData.Direction.SOUTH;

  private VFXPool effect;
  private float _vfxTimer = 0;
  private float _hoverTimer = 0;

  public void Initialize(tk2dSpriteCollectionData collection, int spriteId)
  {
    InitializeInternal(collection, spriteId);
    base.sprite.depthUsesTrimmedBounds = true;
    base.sprite.HeightOffGround = -1.25f;
    base.sprite.UpdateZDepth();
  }

  private void InitializeInternal(tk2dSpriteCollectionData collection, int spriteId)
  {
    this.effect = Items.MagicLamp.AsGun().DefaultModule.projectiles[0].hitEffects.tileMapVertical;

    base.gameObject.AddComponent<tk2dSprite>();
    base.sprite.SetSprite(collection, spriteId);
    base.sprite.IsPerpendicular = true;
    base.sprite.HeightOffGround = 1f;
    base.sprite.PlaceAtPositionByAnchor(base.transform.parent.position, Anchor.MiddleCenter);
    base.sprite.transform.position = base.sprite.transform.position.Quantize(0.0625f);
    DepthLookupManager.ProcessRenderer(base.sprite.renderer);
    tk2dSprite componentInParent = base.transform.parent.gameObject.GetComponentInParent<tk2dSprite>();
    if (componentInParent)
      componentInParent.AttachRenderer(base.sprite);
    // SpriteOutlineManager.AddOutlineToSprite(base.sprite, Color.black, 0.1f, 0.05f);
    base.sprite.ignoresTiltworldDepth = true;
    // if (scaledOutline)
    //   SpriteOutlineManager.AddScaledOutlineToSprite<tk2dSprite>(base.sprite, Color.black, 0.1f, 0.05f);
    // else
      SpriteOutlineManager.AddOutlineToSprite(base.sprite, Color.black);
    base.sprite.UpdateZDepth();

    SpeculativeRigidbody body = base.gameObject.GetOrAddComponent<SpeculativeRigidbody>();
    body.CollideWithOthers = false;
    body.CollideWithTileMap = false;
    Vector2 vector = base.sprite.WorldCenter - base.transform.position.XY();
    body.PixelColliders = new(){
      new PixelCollider() {
        ColliderGenerationMode = PixelCollider.PixelColliderGeneration.Circle,
        CollisionLayer         = CollisionLayer.HighObstacle,
        ManualDiameter         = 14,
        ManualOffsetX          = PhysicsEngine.UnitToPixel(vector.x) - 7,
        ManualOffsetY          = PhysicsEngine.UnitToPixel(vector.y) - 7,
        }
      };
    body.Initialize();
    body.OnPreRigidbodyCollision += ItemOnPreRigidbodyCollision;
    RegenerateCache();
  }

  private void ItemOnPreRigidbodyCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
  {
      PhysicsEngine.SkipCollision = true;
  }

  private void Update()
  {
    if (doVFX)
      UpdateVFX();
    if (doHover)
      UpdateHover();
  }

  private void UpdateVFX()
  {
    const float RADIUS      = 1.3f;
    const int NUM_VFX       = 10;
    const float ANGLE_DELTA = 360f / NUM_VFX;
    const float VFX_FREQ    = 0.4f;

    _vfxTimer += BraveTime.DeltaTime;
    if (_vfxTimer < VFX_FREQ)
      return;

    _vfxTimer = 0;
    for (int i = 0; i < NUM_VFX; ++i)
    {
      Vector2 ppos = base.sprite.WorldCenter + BraveMathCollege.DegreesToVector(i*ANGLE_DELTA,RADIUS);
      effect.SpawnAtPosition(ppos.ToVector3ZisY(1f), 0, null, null, null, -0.05f);
    }
  }

  private void UpdateHover()
  {
    _hoverTimer += BraveTime.DeltaTime;
    base.sprite.transform.localPosition = Vector3.zero.WithY(Mathf.CeilToInt(2f*Mathf.Sin(4f*_hoverTimer))/C.PIXELS_PER_TILE);
  }

  public override void OnDestroy()
  {
    RoomHandler.unassignedInteractableObjects.TryRemove(this);
    base.OnDestroy();
  }

  public void OnEnteredRange(PlayerController interactor)
  {
    if (!this)
      return;
    SpriteOutlineManager.RemoveOutlineFromSprite(base.sprite);
    // if (scaledOutline)
    //   SpriteOutlineManager.AddScaledOutlineToSprite<tk2dSprite>(base.sprite, Color.white, 0.1f, 0.05f);
    // else
      SpriteOutlineManager.AddOutlineToSprite(base.sprite, Color.white, 0.01f, 0.005f);
    if (this.autoInteract)
      Interact(interactor);
  }

  public void OnExitRange(PlayerController interactor)
  {
    if (!this)
      return;
    SpriteOutlineManager.RemoveOutlineFromSprite(base.sprite);
    // if (scaledOutline)
    //   SpriteOutlineManager.AddScaledOutlineToSprite<tk2dSprite>(base.sprite, Color.black, 0.1f, 0.05f);
    // else
      SpriteOutlineManager.AddOutlineToSprite(base.sprite, Color.black, 0.01f, 0.005f);
  }

  public float GetDistanceToPoint(Vector2 point)
  {
    if (!this)
    {
      return 1000f;
    }
    if (base.sprite == null)
        return 100f;
    Vector3 v = BraveMathCollege.ClosestPointOnRectangle(point, base.specRigidbody.UnitBottomLeft, base.specRigidbody.UnitDimensions);
    return Vector2.Distance(point, v) / 1.5f;
  }

  public float GetOverrideMaxDistance()
  {
    return -1f;
  }

  public void Interact(PlayerController player)
  {
    if (interacting)
      return;
    interacting = true;
    player.StartCoroutine(interactionScript(this,player));
  }

  public static IEnumerator DefaultInteractionScript(MiniInteractable i, PlayerController p)
  {
    ETGModConsole.Log("interacted! now override this and actually do something o:");
    i.interacting = false;
    yield break;
  }

  public string GetAnimationState(PlayerController interactor, out bool shouldBeFlipped)
  {
    shouldBeFlipped = false;
    return string.Empty;
  }

  public static MiniInteractable CreateInteractableAtPosition(tk2dBaseSprite sprite, Vector2 position, InteractionScript iscript = null)
  {
    return CreateInteractableAtPosition(sprite.collection, sprite.spriteId, position, iscript);
  }

  public static MiniInteractable CreateInteractableAtPosition(tk2dSpriteCollectionData collection, int spriteId, Vector2 position, InteractionScript iscript = null)
  {
      GameObject iPos = new GameObject("Mini interactible position test");
          iPos.transform.position = position.ToVector3ZisY();
      GameObject iMini = new GameObject("Mini interactible test");
          iMini.transform.parent        = iPos.transform;
          iMini.transform.localPosition = Vector3.zero;
          iMini.transform.position      = Vector3.zero;
      MiniInteractable mini = iMini.AddComponent<MiniInteractable>();
          // NOTE: the below transform position absolutely has to be linked to a game object
          if (!RoomHandler.unassignedInteractableObjects.Contains(mini))
              RoomHandler.unassignedInteractableObjects.Add(mini);
          mini.interactionScript = iscript ?? DefaultInteractionScript;
          mini.Initialize(collection, spriteId);
      return mini;
  }
}
