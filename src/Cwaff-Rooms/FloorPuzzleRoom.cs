namespace CwaffingTheGungy;

public class FloorPuzzleRoomController : MonoBehaviour
{
  internal static readonly List<PrototypeDungeonRoom> _FloorPuzzleRooms = new();
  internal static readonly string _Id                                   = "floor_puzzle_controller";

  private const float MAX_TIME_BETWEEN_TRIGGERS = 0.1f;

  internal FloorPuzzleTile _last = null;
  internal bool _puzzleSucceeded = false;

  private float _dirtyCheaterTimer = 0.0f;
  private bool _puzzleStarted = false;
  private bool _puzzleFailed = false;
  private List<FloorPuzzleTile> _children = new();
  private List<FloorPuzzleTile> _tilesCollidedThisFrame = new();
  private RoomHandler _room = null;

  public static void Init()
  {
    // set up the controller itself
    GameObject protoController = new GameObject(_Id).RegisterPrefab();
    protoController.AddComponent<FloorPuzzleRoomController>(); // called after registering prefab so Start() isn't immediately called
    DungeonPlaceable placeable = BreakableAPIToolbox.GenerateDungeonPlaceable(new(){{protoController, 1f}});
    StaticReferences.StoredDungeonPlaceables.Add(_Id, placeable);
    Alexandria.DungeonAPI.StaticReferences.customPlaceables.Add($"{C.MOD_PREFIX}:{_Id}", placeable);

    // set up the puzzle tiles
    FloorPuzzleTile.Init();

    // set up puzzle rooms using the controller
    _FloorPuzzleRooms.Add(RoomFactory.BuildNewRoomFromResource($"{C.MOD_INT_NAME}/Resources/Rooms/floor_puzzle.newroom").room);
  }

  private void Start()
  {
    const float TILE_SIZE = 32f / 16f;
    const int PUZZLE_SIZE = 3;
    System.Console.WriteLine($"hey it worked maybe");
    this._room = base.transform.position.GetAbsoluteRoom();
    CellArea area = this._room.area;
    Lazy.DebugLog($"  spawned in room of size {area.dimensions.x} by {area.dimensions.y} at {area.UnitCenter}");
    for (int i = -PUZZLE_SIZE; i <= PUZZLE_SIZE; ++i)
    {
      for (int j = -PUZZLE_SIZE; j <= PUZZLE_SIZE; ++j)
      {
        GameObject puzzleTile = UnityEngine.Object.Instantiate(FloorPuzzleTile.Prefab);
        puzzleTile.transform.position = area.UnitCenter + TILE_SIZE * new Vector2(i, j);
        FloorPuzzleTile tile = puzzleTile.GetComponent<FloorPuzzleTile>();
        tile.Setup(parent: this, isWall: i == 0 && j == 0);
        this._children.Add(tile);
      }
    }
  }

  internal void ResetTimers()
  {
    this._puzzleStarted = true;
    this._dirtyCheaterTimer = 0.0f;
  }

  internal void Fail()
  {
    if (this._puzzleFailed)
      return;

    this._puzzleFailed = true;
    base.gameObject.Play("puzzle_fail");
  }

  internal bool Failed() => this._puzzleFailed;

  private void HandleQueuedTileCollisions()
  {
    PlayerController player = GameManager.Instance.BestActivePlayer; // TODO: handle coop better
    Vector2 ppos = player.specRigidbody.UnitCenter;
    FloorPuzzleTile closest = _tilesCollidedThisFrame[0];
    float closestDist = (ppos - closest._trigger.UnitCenter).sqrMagnitude;
    for (int i = 1; i < _tilesCollidedThisFrame.Count; ++i)
    {
      float dist = (ppos - _tilesCollidedThisFrame[i]._trigger.UnitCenter).sqrMagnitude;
      if (dist >= closestDist)
        continue;
      closestDist = dist;
      closest = _tilesCollidedThisFrame[i];
    }
    closest.HandleCollision();
    _tilesCollidedThisFrame.Clear();
  }

  private void Update()
  {
    if (this._puzzleSucceeded)
      return;

    if (_tilesCollidedThisFrame.Count > 0)
      HandleQueuedTileCollisions();

    if (!this._puzzleStarted)
      return;

    this._dirtyCheaterTimer += BraveTime.DeltaTime;
    if (this._dirtyCheaterTimer < MAX_TIME_BETWEEN_TRIGGERS)
      return;

    foreach (FloorPuzzleTile child in this._children)
      child.Reset();
    this._puzzleStarted = false;
    if (this._puzzleFailed)
      this._puzzleFailed = false;
    else
      base.gameObject.Play("puzzle_fail");
  }

  internal void CollidedWithTileThisFrame(FloorPuzzleTile tile, PlayerController pc)
  {
    _tilesCollidedThisFrame.Add(tile);
  }

  internal bool CheckSuccess()
  {
    if (this._puzzleSucceeded || this._puzzleFailed)
      return false;

    foreach (FloorPuzzleTile child in this._children)
      if (!child._active)
        return false; // haven't succeeded yet

    this._puzzleSucceeded = true;
    IntVector2 bestRewardLocation = this._room.GetBestRewardLocation(new IntVector2(2, 1));
    if (GameStatsManager.Instance.IsRainbowRun)
        LootEngine.SpawnBowlerNote(GameManager.Instance.RewardManager.BowlerNoteChest, bestRewardLocation.ToCenterVector2(), this._room, true);
    else
      this._room.SpawnRoomRewardChest(null, bestRewardLocation);
    base.gameObject.Play("puzzle_solve");
    return true;
  }
}

public class FloorPuzzleTile : MonoBehaviour
{
  private const float LEEWAY = 0.1f; // leeway to prevent double-triggering

  public static GameObject Prefab = null;

  internal bool _active = false;
  internal PixelCollider _trigger = null;
  internal tk2dSpriteAnimator _animator = null;

  private FloorPuzzleRoomController _controller = null;
  private SpeculativeRigidbody _body = null;
  private tk2dSprite _sprite = null;
  private float _triggerTimer = 0.0f;
  private bool _isWall = false;

  public static void Init()
  {
    Prefab = VFX.Create("puzzle_tile_square", emissivePower: 5f);
    Prefab.AutoRigidBody(anchor: Anchor.MiddleCenter, clayer: CollisionLayer.PlayerBlocker);
    Prefab.AddComponent<FloorPuzzleTile>();
  }

  public void Setup(FloorPuzzleRoomController parent, bool isWall)
  {
    this._controller = parent;

    this._sprite = base.gameObject.GetComponent<tk2dSprite>();
    this._sprite.HeightOffGround = -4f;
    this._sprite.UpdateZDepth();
    this._animator = base.gameObject.GetComponent<tk2dSpriteAnimator>();
    this._animator.PickFrame(2);

    this._body = base.gameObject.GetComponent<SpeculativeRigidbody>();
    this._trigger = this._body.PixelColliders[0];
    this._isWall = isWall;
    if (this._isWall)
    {
      this._active = true; // walls are always active
      this._trigger.IsTrigger = false; // make the rigidbody solid
      this._sprite.SetGlowiness(0f);
    }
    else
      this._body.OnTriggerCollision += this.OnTriggerCollision;
  }

  private void OnTriggerCollision(SpeculativeRigidbody specRigidbody, SpeculativeRigidbody sourceSpecRigidbody, CollisionData collisionData)
  {
    if (!this._controller || this._controller._puzzleSucceeded)
      return;
    if (specRigidbody.gameObject.GetComponent<PlayerController>() is not PlayerController pc)
      return;
    this._controller.CollidedWithTileThisFrame(this, pc); // anti-frustration delay in case we collide with multiple tiles in one frame
  }

  public void HandleCollision()
  {
    this._controller.ResetTimers();
    if (this._controller.Failed())
      return;

    if (this._active)
    {
      if (this._controller._last != this && this._triggerTimer >= LEEWAY)
      {
        this._animator.PickFrame(0);
        this._controller.Fail();
      }
      this._triggerTimer = 0.0f;
      return;
    }
    this._active = true;
    this._controller._last = this;
    this._animator.PickFrame(1);
    if (this._controller.CheckSuccess())
      return;

    base.gameObject.Play("maestro_launch_asharp");
  }

  internal void Reset()
  {
    if (this._isWall)
      return; // walls are always active
    this._active = false;
    this._animator.PickFrame(2);
  }

  private void Update()
  {
    this._triggerTimer += BraveTime.DeltaTime;
  }
}
