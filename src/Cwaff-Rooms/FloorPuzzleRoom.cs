namespace CwaffingTheGungy;

public class FloorPuzzleRoomController : MonoBehaviour
{
  internal static readonly List<PrototypeDungeonRoom> _FloorPuzzleRooms = new();
  internal static readonly string _GUID = "floor_puzzle_controller"; //WARNING: don't change without also updating GUID in RAT

  private const float MAX_TIME_BETWEEN_TRIGGERS = 0.1f;

  internal FloorPuzzleTile _last = null;
  internal bool _puzzleSucceeded = false;
  internal bool _puzzleStarted = false;
  internal PlayerController _puzzleDoer = null;

  private float _dirtyCheaterTimer = 0.0f;
  private bool _puzzleFailed = false;
  private List<FloorPuzzleTile> _children = new();
  private List<FloorPuzzleTile> _tilesCollidedThisFrame = new();
  private FloorPuzzleTile _entry = null;
  private FloorPuzzleTile _exit = null;
  private RoomHandler _room = null;

  public static void Init()
  {
    // set up the controller itself
    Lazy.RegisterEasyRATPlaceable<FloorPuzzleRoomController>(_GUID);
    // set up the puzzle tiles
    FloorPuzzleTile.Init();
    // set up puzzle rooms using the controller
    _FloorPuzzleRooms.Add(RoomFactory.BuildNewRoomFromResource($"{C.MOD_INT_NAME}/Resources/Rooms/floor_puzzle.newroom").room);
  }

  private void Start()
  {
    const float TILE_SIZE = 24f / 16f; // size of the sprites in game units
    const float COVERAGE = 0.75f; // approximate percentage of puzzle that should be floor tiles (as opposed to wall tiles)
    int puzzleSize = 9;

    #if !DEBUG // always max difficulty in debug mode
    GlobalDungeonData.ValidTilesets tileSet = GameManager.Instance.Dungeon.tileIndices.tilesetId;
    if (tileSet == GlobalDungeonData.ValidTilesets.CASTLEGEON || tileSet == GlobalDungeonData.ValidTilesets.GUNGEON)
      puzzleSize = 7;
    else if (tileSet == GlobalDungeonData.ValidTilesets.MINEGEON || tileSet == GlobalDungeonData.ValidTilesets.CATACOMBGEON || tileSet == GlobalDungeonData.ValidTilesets.SEWERGEON)
      puzzleSize = 8;
    #endif

    float puzzleRadius = 0.5f * (puzzleSize - 1);
    this._room = base.transform.position.GetAbsoluteRoom();
    CellArea area = this._room.area;
    Lazy.DebugLog($"  spawning floor puzzle of size {puzzleSize} by {puzzleSize} in room of size {area.dimensions.x} by {area.dimensions.y} at {area.UnitCenter}");

    LinkedList<IntVector2> path = FloorPuzzleGenerator.GeneratePuzzleOfSize(puzzleSize, puzzleSize, targetCoverage: COVERAGE);
    HashSet<IntVector2> pathPositions = new(path);
    IntVector2 startPos = path.First.Value;
    IntVector2 endPos = path.Last.Value;
    for (int i = 0; i < puzzleSize; ++i)
    {
      for (int j = 0; j < puzzleSize; ++j)
      {
        GameObject puzzleTile = UnityEngine.Object.Instantiate(FloorPuzzleTile.Prefab);
        puzzleTile.transform.position = area.UnitCenter + TILE_SIZE * new Vector2(i - puzzleRadius, j - puzzleRadius);
        FloorPuzzleTile tile = puzzleTile.GetComponent<FloorPuzzleTile>();
        tile.Setup(controller: this, isWall: !pathPositions.Contains(new IntVector2(i, j)));
        if (i == startPos.x && j == startPos.y)
        {
          tile._type = FloorPuzzleTile.TileType.ENTRY;
          this._entry = tile;
        }
        else if (i == endPos.x && j == endPos.y)
        {
          tile._type = FloorPuzzleTile.TileType.EXIT;
          this._exit = tile;
        }
        this._children.Add(tile);
      }
    }
    this._entry._animator.PickFrame(3);
  }

  internal void ResetTimers()
  {
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
    PlayerController player = this._puzzleDoer;
    if (!player)
    {
      Lazy.DebugWarn("handling puzzle collisions without a puzzle doer...this shouldn't happen");
      return;
    }

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
    if (!this._puzzleStarted)
      this._puzzleDoer = null; // allow any player to do the puzzle if we haven't started it yet
  }

  internal void StartPuzzle()
  {
    this._puzzleStarted = true;
    this._exit._animator.PickFrame(3);
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
    this._puzzleDoer = null;
    if (this._puzzleFailed)
      this._puzzleFailed = false;
    else
      base.gameObject.Play("puzzle_fail");
  }

  internal void CollidedWithTileThisFrame(FloorPuzzleTile tile, PlayerController pc)
  {
    if (this._puzzleDoer == null)
      this._puzzleDoer = pc;
    else if (this._puzzleDoer != pc)
      return;
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
    {
      Chest chest = this._room.SpawnRoomRewardChest(null, bestRewardLocation);
      chest.ForceUnlock();
    }
    base.gameObject.Play("puzzle_solve");
    return true;
  }
}

public class FloorPuzzleTile : MonoBehaviour
{
  public enum TileType
  {
    NORMAL,
    WALL,
    ENTRY,
    EXIT,
  }

  private const float LEEWAY = 0.1f; // leeway to prevent double-triggering

  public static GameObject Prefab = null;

  internal bool _active = false;
  internal TileType _type = TileType.NORMAL;
  internal PixelCollider _trigger = null;
  internal tk2dSpriteAnimator _animator = null;

  private FloorPuzzleRoomController _controller = null;
  private SpeculativeRigidbody _body = null;
  private tk2dSprite _sprite = null;
  private float _triggerTimer = 0.0f;

  public static void Init()
  {
    Prefab = VFX.Create("puzzle_tile_square", emissivePower: 2f);
    Prefab.AutoRigidBody(anchor: Anchor.MiddleCenter, clayer: CollisionLayer.PlayerBlocker);
    Prefab.AddComponent<FloorPuzzleTile>();
  }

  public void Setup(FloorPuzzleRoomController controller, bool isWall)
  {
    this._controller = controller;

    this._sprite = base.gameObject.GetComponent<tk2dSprite>();
    this._sprite.usesOverrideMaterial = true;
    this._sprite.renderer.material.shader = ShaderCache.Acquire("Brave/PlayerShader"); // flat shading
    this._sprite.HeightOffGround = -4f;
    this._sprite.UpdateZDepth();
    this._animator = base.gameObject.GetComponent<tk2dSpriteAnimator>();
    this._animator.PickFrame(2);

    this._body = base.gameObject.GetComponent<SpeculativeRigidbody>();
    this._trigger = this._body.PixelColliders[0];
    if (isWall)
    {
      this._type = TileType.WALL;
      this._active = true; // walls are always active
      this._trigger.IsTrigger = false; // make the rigidbody solid
      this._sprite.SetGlowiness(0f);
    }
    else
    {
      this._trigger.IsTrigger = true; // rigidbody can be walked over
      this._body.OnTriggerCollision += this.OnTriggerCollision;
    }
  }

  private void OnTriggerCollision(SpeculativeRigidbody specRigidbody, SpeculativeRigidbody sourceSpecRigidbody, CollisionData collisionData)
  {
    if (!this._controller || this._controller._puzzleSucceeded || this._type == TileType.WALL)
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
    if (!this._controller._puzzleStarted && this._type != TileType.ENTRY)
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
    if (this._type == TileType.ENTRY)
      this._controller.StartPuzzle();
    if (this._controller.CheckSuccess())
      return;
    if (this._type == TileType.EXIT) // stepping on the exit prematurely is a fail
    {
        this._animator.PickFrame(0);
        this._controller.Fail();
        return;
    }

    base.gameObject.Play("puzzle_tile_step_sound");
  }

  internal void Reset()
  {
    if (this._type == TileType.WALL)
      return; // walls are always active
    this._active = false;
    this._animator.PickFrame(this._type == TileType.ENTRY ? 3 : 2);
  }

  private void Update()
  {
    this._triggerTimer += BraveTime.DeltaTime;
  }
}

internal static class FloorPuzzleGenerator
{
  /// <summary>Generates a solution path through a grid of size (width, height) where the first node of the path is the starting node at the
  /// bottom of the grid and the final node is the exit at the top of the grid. All tiles not on the solution path should be considered walls.</summary>
  public static LinkedList<IntVector2> GeneratePuzzleOfSize(int width = 9, int height = 9, float targetCoverage = 0.75f)
  {
      IntVector2 start = new IntVector2(UnityEngine.Random.Range(0, width), 0); // entry is always on the bottom of the grid
      IntVector2 end = new IntVector2(UnityEngine.Random.Range(0, width), height - 1); // exit is always on the top of the grid

      LinkedList<IntVector2> path = new();
      HashSet<IntVector2> visited = new();
      path.AddLast(start);
      visited.Add(start);

      int c = start.x;
      int r = start.y;

      int vertSteps = end.y - start.y;
      int horizSteps = end.x - start.x;

      // generate manhattan path from entrance to exit
      List<char> steps = new List<char>();
      for (int i = 0; i < Math.Abs(vertSteps); i++)
          steps.Add(vertSteps > 0 ? 'U' : 'D');
      for (int i = 0; i < Math.Abs(horizSteps); i++)
          steps.Add(horizSteps > 0 ? 'R' : 'L');
      steps.Shuffle();
      foreach (char step in steps)
      {
          switch (step)
          {
              case 'D': r -= 1; break;
              case 'U': r += 1; break;
              case 'L': c -= 1; break;
              case 'R': c += 1; break;
          }
          IntVector2 newPoint = new IntVector2(c, r);
          path.AddLast(newPoint);
          visited.Add(newPoint);
      }

      // extrude random loop nodes until failure
      const int MAX_RETRIES = 1000;
      int retries = MAX_RETRIES;
      while (retries > 0)
      {
          // get two random adjacent nodes on the solution path
          int pathLength = path.Count;
          int nodeId = UnityEngine.Random.Range(0, pathLength - 1);
          LinkedListNode<IntVector2> nthNode = path.First;
          for (int i = 0; i < nodeId; ++i)
            nthNode = nthNode.Next;
          LinkedListNode<IntVector2> neighbor = nthNode.Next;

          // get the nodes' positions
          IntVector2 nthValue      = nthNode.Value;
          IntVector2 neighborValue = neighbor.Value;

          // get the delta vector between the two positions
          IntVector2 delta = new IntVector2(neighborValue.x - nthValue.x, neighborValue.y - nthValue.y);

          // pick a random vector perpendicular to the delta
          IntVector2 perp = delta.y == 0 ? new IntVector2(0, Lazy.CoinFlip() ? 1 : -1) : new IntVector2(Lazy.CoinFlip() ? 1 : -1, 0);

          // get the positions adjacent to the two path nodes
          IntVector2 adjMe = nthValue + perp;
          IntVector2 adjNeighbor = neighborValue + perp;

          // check if the adjacent positions are out of bounds or already part of the solution path
          if (
            adjMe.x < 0 || adjMe.x >= width || adjMe.y < 0 || adjMe.y >= height ||
            adjNeighbor.x < 0 || adjNeighbor.x >= width || adjNeighbor.y < 0 || adjNeighbor.y >= height ||
            visited.Contains(adjMe) || visited.Contains(adjNeighbor))
          {
            --retries;  // they're already part of the solution path or out of bounds, so retry
            continue;
          }

          // bend the path to include the adjacent nodes
          path.AddAfter(nthNode, adjMe);
          path.AddBefore(neighbor, adjNeighbor);
          visited.Add(adjMe);
          visited.Add(adjNeighbor);

          // if more than targetCoverage percent of tiles are part of the solution path, we've achieved a sufficiently complex puzzle
          if ((float)visited.Count / (height * width) >= targetCoverage)
            break;
      }

      return path;
  }
}
