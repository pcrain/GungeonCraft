namespace CwaffingTheGungy;

using static ShufflePuzzleRoomController.State;

public class ShufflePuzzleRoomController : MonoBehaviour
{
  private const string PUZZLE_STRING = "doing a puzzle";

  internal static readonly List<PrototypeDungeonRoom> _ShufflePuzzleRooms = new();
  internal static readonly string _GUID = "shuffle_puzzle_controller"; //WARNING: don't change without also updating GUID in RAT
  internal static GameObject _PuzzleBagPrefab = null;

  internal enum State
  {
    LOADING,     // level is loading in
    PRESTART,    // level has loaded, player has not started the shuffle
    SHUFFLING,   // shuffle in progress
    PLAYERWAIT,  // shuffle is done, waiting for player to make a selection
    COMPLETED,   // selection has been made
  }

  internal bool _puzzleStarted = false;
  internal PlayerController _puzzleDoer = null;

  private State _state        = LOADING;
  private RoomHandler _room   = null;
  private Vector2 _swapCenter = default;
  private float _swapRadius   = 4f;
  private int _numSwappables  = 7;
  private List<SwappyThing> _swappies = new();

  public static void Init()
  {
    // set up the controller itself
    Lazy.RegisterEasyRATPlaceable<ShufflePuzzleRoomController>(_GUID);
    // set up puzzle rooms using the controller
    _ShufflePuzzleRooms.Add(RoomFactory.BuildNewRoomFromResource($"{C.MOD_INT_NAME}/Resources/Rooms/shuffle_puzzle.newroom").room);

    _PuzzleBagPrefab = VFX.Create("puzzle_bag");
    _PuzzleBagPrefab.AutoRigidBody(Anchor.MiddleCenter);
  }

  private void Start()
  {
    System.Console.WriteLine($"created shuffle puzzle room");
    this._room = base.transform.position.GetAbsoluteRoom();
    this._swapCenter = this._room.area.UnitCenter.Quantize(0.0625f) + new Vector2(0, 1/32f); //NOTE: 1/32f offset to fix weird outlining issues with odd-pixeled sprites

    // determine chest contents
    PickupObject grandPrize = GameManager.Instance.PrimaryPlayer.GetRandomChestRewardOfQuality(ItemQuality.C).GetComponent<PickupObject>();
    PassiveItem junk = Items.Junk.AsPassive();
    int prizePos = UnityEngine.Random.Range(0, this._numSwappables);

    for (int i = 0; i < this._numSwappables; ++i)
    {
      GameObject puzzleBag = _PuzzleBagPrefab.Instantiate(position: this._swapCenter);
      tk2dSprite s = puzzleBag.GetComponent<tk2dSprite>();
      s.UpdateZDepth();
      SwappyThing swap = s.gameObject.AddComponent<SwappyThing>();
      Vector2 offset = (360f * (float)i / this._numSwappables).ToVector(this._swapRadius);
      swap.Setup(controller: this, index: i, contents: i == prizePos ? grandPrize : junk);
      swap.Relocate(pos: this._swapCenter);
      this._swappies.Add(swap);
      if (i == 0)
      {
        this._room.RegisterInteractable(swap);
        SpriteOutlineManager.AddOutlineToSprite(s, Color.black, SwappyThing._OUTLINE_OFFSET, 0f);
      }
      else
        s.renderer.enabled = false;
    }
  }

  private IEnumerator DoShuffle()
  {
    const float START_DELAY = 1f;
    const float SWAP_TIME   = 0.4f;
    const float SWAP_DELAY  = 0.1f;
    const int   NUM_SWAPS   = 2;
    const float MAX_RPS     = 120f; // max rotation per second, in degrees
    const float ROT_ACCEL   = 120f; // max rotation change per second, in degrees
    const float SPREAD_TIME = 1f;
    const float ABSORB_TIME = 1f;
    const float ITEM_OFFSET = 2f;
    const float TEASE_TIME  = 1.5f;

    Lazy.DebugLog($"shuffle starting in {START_DELAY} seconds with {this._numSwappables} swappables");

    float rotspeed = 0;
    float rotation = 0;
    float rotdir = 1;
    float swapTimer = 0;
    int swapIndexOne = -1;
    int swapIndexTwo = -1;
    bool doingSwap = false;
    int swapsLeft = NUM_SWAPS;

    // spread out
    for (float elapsed = 0f; elapsed < SPREAD_TIME; elapsed += BraveTime.DeltaTime)
    {
      float percentLeft = 1f - elapsed / SPREAD_TIME;
      float lerp = 1f - (percentLeft * percentLeft * percentLeft);
      for (int i = 0; i < this._numSwappables; ++i)
        this._swappies[i].Relocate(this._swapCenter + (360f * ((float)i / this._numSwappables) + rotation).ToVector(lerp * this._swapRadius));
      yield return null;
    }

    // spawn the items
    for (int i = 0; i < this._numSwappables; ++i)
    {
      this._swappies[i].UpdateItemRenderer(this._swapCenter + (360f * ((float)i / this._numSwappables) + rotation).ToVector(ITEM_OFFSET + this._swapRadius));
      this._swappies[i].AddOutlineToItem();
    }
    yield return new WaitForSeconds(TEASE_TIME);

    // absorb items
    for (float elapsed = 0f; elapsed < ABSORB_TIME; elapsed += BraveTime.DeltaTime)
    {
      float percentLeft = 1f - elapsed / ABSORB_TIME;
      float lerp = 1f - (percentLeft * percentLeft * percentLeft);
      float radius = (percentLeft * ITEM_OFFSET) + this._swapRadius;
      for (int i = 0; i < this._numSwappables; ++i)
        this._swappies[i].UpdateItemRenderer(this._swapCenter + (360f * ((float)i / this._numSwappables) + rotation).ToVector(radius), scale: percentLeft);
      yield return null;
    }
    yield return new WaitForSeconds(0.5f);

    // swapping time
    float dtime = BraveTime.DeltaTime;
    while (swapsLeft > 0)
    {
      yield return null;
      dtime = BraveTime.DeltaTime;
      rotspeed += ROT_ACCEL * rotdir * dtime;
      if (rotspeed > MAX_RPS)
        rotspeed = MAX_RPS;
      else if (rotspeed < -MAX_RPS)
        rotspeed = -MAX_RPS;
      rotation = (rotation + rotspeed * dtime) % 360f;

      if (!doingSwap)
      {
        swapTimer += dtime;
        if (swapTimer >= SWAP_DELAY)
        {
          swapTimer = 0;
          doingSwap = true;
          base.gameObject.Play("puzzle_item_swap_sound");
          swapIndexOne = UnityEngine.Random.Range(0, this._numSwappables);
          // don't swap with self or adjacent items
          swapIndexTwo = (swapIndexOne + UnityEngine.Random.Range(2, this._numSwappables - 1)) % this._numSwappables;
        }
      }
      else
      {
        swapTimer += dtime;
        if (swapTimer >= SWAP_TIME)
        {
          swapTimer = 0;
          doingSwap = false;
          SwappyThing temp = this._swappies[swapIndexOne];
          this._swappies[swapIndexOne] = this._swappies[swapIndexTwo];
          this._swappies[swapIndexTwo] = temp;
          swapIndexOne = -1;
          swapIndexTwo = -1;
          --swapsLeft;
        }
      }

      for (int i = 0; i < this._numSwappables; ++i)
      {
        Vector2 basePos = this._swapCenter + (360f * ((float)i / this._numSwappables) + rotation).ToVector(this._swapRadius);
        if (i != swapIndexOne && i != swapIndexTwo)
        {
          this._swappies[i].Relocate(basePos);
          continue;
        }
        Vector2 otherBasePos = this._swapCenter + (360f * ((float)(i != swapIndexOne ? swapIndexOne : swapIndexTwo) / this._numSwappables) + rotation).ToVector(this._swapRadius);
        float percentSwapDone = swapTimer / SWAP_TIME;
        Vector2 swapPos = Vector2.Lerp(basePos, otherBasePos, percentSwapDone);
        Vector2 perpVec = Mathf.Min(percentSwapDone , 1f - percentSwapDone) * (otherBasePos - basePos).normalized.Rotate(90f);
        this._swappies[i].Relocate(swapPos + perpVec);
      }
    }

    this._state = PLAYERWAIT;
    this._puzzleDoer.ClearInputOverride(PUZZLE_STRING);
    for (int i = 0; i < this._numSwappables; ++i)
    {
      this._room.RegisterInteractable(this._swappies[i]);
      this._swappies[i]._trail.transform.parent = null;
      UnityEngine.GameObject.Destroy(this._swappies[i]._trail.gameObject);
      this._swappies[i]._body.Reinitialize();
      foreach (PlayerController p in GameManager.Instance.AllPlayers)
        if (p && p.specRigidbody)
          this._swappies[i]._body.RegisterGhostCollisionException(p.specRigidbody);
    }
    System.Console.WriteLine($"shuffling done!");
  }

  private void StartPuzzle(PlayerController puzzleDoer)
  {
    if (this._puzzleStarted)
      return;

    this._puzzleDoer = puzzleDoer;
    this._puzzleDoer.SetInputOverride(PUZZLE_STRING);
    this._room.DeregisterInteractable(this._swappies[0]);
    this._puzzleStarted = true;
    this._state = SHUFFLING;
    base.StartCoroutine(DoShuffle());
  }

  private void EndPuzzle()
  {
    if (this._state != PLAYERWAIT)
      return;

    this._state = COMPLETED;
    for (int i = 0; i < this._numSwappables; ++i)
    {
      this._room.DeregisterInteractable(this._swappies[i]);
      LootEngine.DoDefaultItemPoof(this._swappies[i]._sprite.WorldCenter, muteAudio: i > 0);
      UnityEngine.Object.Destroy(this._swappies[i].gameObject);
    }
  }

  private class SwappyThing : MonoBehaviour, IPlayerInteractable
  {
    internal const float _OUTLINE_OFFSET = 0.1f;

    internal int _index                  = -1;
    internal int _contentsId             = -1;
    internal tk2dSprite _sprite          = null;
    internal SpeculativeRigidbody _body  = null;
    internal CwaffTrailController _trail = null;

    private tk2dSprite _itemSprite      = null;
    private ShufflePuzzleRoomController _controller = null;
    private PickupObject _contents = null;

    public void Setup(ShufflePuzzleRoomController controller, int index, PickupObject contents)
    {
      this._controller = controller;
      this._index      = index;
      this._contentsId = contents.PickupObjectId;
      this._contents   = contents;

      this._sprite     = base.gameObject.GetComponent<tk2dSprite>();
      this._trail      = this._sprite.AddTrail(Uppskeruvel._SoulTrailPrefab); //TODO: use different trail
      this._body       = base.gameObject.GetComponent<SpeculativeRigidbody>();

      this._body.OnPreRigidbodyCollision += this.OnPreRigidbodyCollision;

      tk2dSprite pickupSprite           = contents.gameObject.GetComponent<tk2dSprite>();
      this._itemSprite                  = Lazy.SpriteObject(pickupSprite.collection, pickupSprite.spriteId);
      this._itemSprite.renderer.enabled = false;
    }

    private void OnPreRigidbodyCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
    {
      if (this._controller._puzzleStarted && this._controller._state != PLAYERWAIT)
      {
        System.Console.WriteLine($"skippadoo");
        PhysicsEngine.SkipCollision = true;
      }
    }

    public void Relocate(Vector2 pos)
    {
      if (!this._sprite.renderer.enabled)
      {
        this._sprite.renderer.enabled = true;
        SpriteOutlineManager.AddOutlineToSprite(this._sprite, Color.black, _OUTLINE_OFFSET, 0f);
      }
      base.transform.position = (pos - this._sprite.GetRelativePositionFromAnchor(Anchor.MiddleCenter));
      this._sprite.UpdateZDepth();
    }

    public void Interact(PlayerController player)
    {
      SpriteOutlineManager.RemoveOutlineFromSprite(this._sprite);
      if (!this._controller._puzzleStarted)
      {
        this._controller.StartPuzzle(player);
        return;
      }
      if (player != this._controller._puzzleDoer)
        return;
      Vector2 extents = this._itemSprite.GetCurrentSpriteDef().boundsDataExtents.XY();
      LootEngine.SpawnItem(this._contents.gameObject, this._sprite.WorldCenter - 0.5f * extents, Vector2.up, 1f);
      this._controller.EndPuzzle();
    }

    public void UpdateItemRenderer(Vector2 pos, float scale = 1f)
    {
      if (!this._itemSprite.renderer.enabled)
      {
        this._itemSprite.renderer.enabled = true;
        LootEngine.DoDefaultItemPoof(pos, muteAudio: this._index > 0);
      }
      this._itemSprite.scale = scale * Vector3.one;
      this._itemSprite.PlaceAtScaledPositionByAnchor(pos, Anchor.MiddleCenter);
      this._itemSprite.UpdateZDepth();
    }

    public void AddOutlineToItem()
    {
      SpriteOutlineManager.AddOutlineToSprite(this._itemSprite, Color.black, 2.0f, 0f);
      this._itemSprite.UpdateZDepth();
    }

    public void RemoveItemRenderer()
    {
      UnityEngine.Object.Destroy(this._itemSprite.gameObject);
    }

    public float GetDistanceToPoint(Vector2 point)
    {
      if (!this || !this._sprite)
        return 1000f;
      return Vector2.Distance(point, this._sprite.WorldCenter);
    }

    public float GetOverrideMaxDistance()
    {
      return -1f;
    }

    public string GetAnimationState(PlayerController interactor, out bool shouldBeFlipped)
    {
      shouldBeFlipped = false;
      return string.Empty;
    }

    public void OnEnteredRange(PlayerController interactor)
    {
      if (!this)
        return;
      SpriteOutlineManager.RemoveOutlineFromSprite(this._sprite);
      SpriteOutlineManager.AddOutlineToSprite(this._sprite, Color.white, _OUTLINE_OFFSET, 0f);
      this._sprite.UpdateZDepth();
    }

    public void OnExitRange(PlayerController interactor)
    {
      if (!this)
        return;
      SpriteOutlineManager.RemoveOutlineFromSprite(this._sprite);
      SpriteOutlineManager.AddOutlineToSprite(this._sprite, Color.black, _OUTLINE_OFFSET, 0f);
      this._sprite.UpdateZDepth();
    }
  }
}

