namespace CwaffingTheGungy;

/* TODO:
    - make projectiles reveal unrevealed tiles and explode when revealing mines

    - better actual projectiles
    - gun animations
    - gun sounds
*/

public class Cleansweep : CwaffGun
{
    public static string ItemName         = "Cleansweep";
    public static string ShortDescription = "Mine Craft";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    private const float _MINE_EXPLOSION_DAMAGE = 250f;
    private const int _GAME_AMMO_COST = 20;
    private const float _RESET_DELAY = 1.0f; // minimum time between being allowed to reset the minesweeper grid in a room

    internal static ExplosionData _MineExplosion = null;
    private static float _NextResetTime = 0.0f;

    public static void Init()
    {
        Lazy.SetupGun<Cleansweep>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.B, gunClass: GunClass.RIFLE, reloadTime: 0.75f, ammo: 480)
          // .Attach<CleansweepAmmoDisplay>()
          .InitProjectile(GunData.New(clipSize: 10, cooldown: 0.18f, shootStyle: ShootStyle.SemiAutomatic, damage: 7.0f));

        _MineExplosion = GameManager.Instance.Dungeon.sharedSettingsPrefab.DefaultExplosionData.Clone();
        _MineExplosion.damage = _MINE_EXPLOSION_DAMAGE;
    }

    public override void OnPlayerPickup(PlayerController player)
    {
        base.OnPlayerPickup(player);
        player.OnTriedToInitiateAttack += this.OnTriedToInitiateAttack;
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        player.OnTriedToInitiateAttack -= this.OnTriedToInitiateAttack;
        base.OnDroppedByPlayer(player);
    }

    public override void OnDestroy()
    {
        if (this.PlayerOwner)
            this.PlayerOwner.OnTriedToInitiateAttack -= this.OnTriedToInitiateAttack;
        base.OnDestroy();
    }

    private void OnTriedToInitiateAttack(PlayerController player)
    {
        if (!player || player.CurrentGun != this.gun)
            return; // inactive, do normal firing stuff
        if (!MinesweeperGame.IsGameActive)
        {
            if (player.IsInCombat && gun.CurrentAmmo >= _GAME_AMMO_COST)
            {
                gun.LoseAmmo(_GAME_AMMO_COST);
                MinesweeperGame.StartGame(player); // activate the Minesweeper game
                player.SuppressThisClick = true;
            }
            return; // inactive, do normal firing stuff
        }
        if (MinesweeperGame.FlagTarget())
            player.SuppressThisClick = true;
    }

    public override void OnFullClipReload(PlayerController player, Gun gun)
    {
        if (!MinesweeperGame.IsGameActive || !player.IsInCombat || gun.CurrentAmmo < _GAME_AMMO_COST)
            return;
        float now = BraveTime.ScaledTimeSinceStartup;
        if (now < _NextResetTime)
            return;
        _NextResetTime = now + _RESET_DELAY;
        gun.LoseAmmo(_GAME_AMMO_COST);
        MinesweeperGame.StartGame(player);
    }
}

public class MinesweeperTile : MonoBehaviour
{
    private const float _SHWOOP_TIME = 0.18f;
    private const float _MINE_CHANCE = 0.125f;
    private const float _TS          = MinesweeperGame._TS;
    private const float _NUM_OPACITY = 0.85f;

    private static readonly Color[] _NumberColors = [
        new Color(0.75f, 0.75f, 0.75f), // 0 (safe)
        new Color(0.00f, 0.00f, 1.00f), // 1
        new Color(0.00f, 0.50f, 0.00f), // 2
        new Color(1.00f, 0.00f, 0.00f), // 3
        new Color(0.00f, 0.00f, 0.50f), // 4
        new Color(0.50f, 0.00f, 0.00f), // 5
        new Color(0.00f, 0.50f, 0.50f), // 6
        new Color(0.00f, 0.00f, 0.00f), // 7
        new Color(0.50f, 0.50f, 0.50f), // 8
    ];

    private static readonly Color _UnknownColor = new Color(0.15f, 0.25f, 0.65f);
    private static readonly Color _FlaggedColor = new Color(0.55f, 0.95f, 0.65f);

    private static MinesweeperTile _Target = null;

    private bool _setup           = false;
    private IntVector2 _pos       = default;
    private Vector2 _tl           = default;
    private Vector2 _br           = default;
    private Vector2 _center       = default;
    private Vector2 _labelPos     = default;
    private float _lifetime       = 0.0f;
    private Geometry _square      = null;
    private int  _number          = 0;
    private dfLabel  _numberLabel = null;
    private Color _color          = default;
    private bool _isTargeted      = false;
    private bool _revealed        = false;
    private bool _flagged         = false;

    public bool IsRevealed => this._revealed;
    public bool IsFlagged => this._flagged;
    public bool IsSafe    => this._number == 0;
    public bool IsMine    => this._number == -1;
    public IntVector2 Pos => this._pos;

    public void ToggleFlag()
    {
        if (this._revealed)
            return;
        this._flagged = !this._flagged;
        base.gameObject.PlayOnce("minesweeper_place_sound");
    }

    public static MinesweeperTile Setup(IntVector2 pos, int number)
    {
        MinesweeperTile tile                = new GameObject("minesweeper tile").AddComponent<MinesweeperTile>();
        tile._square                        = new GameObject("minesweeper tile square").AddComponent<Geometry>();
        tile._pos                           = pos;
        tile._tl                            = pos.ToVector2();
        tile._center                        = tile._tl + new Vector2(0.5f * _TS, 0.5f * _TS);
        tile._br                            = tile._tl + new Vector2(_TS, _TS);
        tile._labelPos                      = tile._tl + new Vector2(_TS * 0.6f, _TS * 0.3f);
        tile._color                         = (number < 0) ? Color.red : _NumberColors[0];
        tile._number                        = number;
        tile._numberLabel                   = CwaffLabel.MakeNewLabel(unicode: false, outline: false);
        tile._numberLabel.Text              = (number <= 0) ? string.Empty : $"{number}";
        tile._numberLabel.TextScale         = tile._numberLabel.TextScale * _TS;
        tile._numberLabel.Color             = (number < 0) ? Color.magenta : _NumberColors[number];
        tile._numberLabel.Opacity           = 0.0f;
        tile._numberLabel.TextAlignment     = TextAlignment.Center; // NOTE: seems to have no effect ):
        tile._numberLabel.VerticalAlignment = dfVerticalAlignment.Middle; // NOTE: seems to have no effect ):
        tile._setup                         = true;

        int bglayer = LayerMask.NameToLayer("BG_Critical");
        tile.gameObject.SetLayerRecursively(bglayer);
        tile._square.gameObject.SetLayerRecursively(bglayer);
        tile._numberLabel.gameObject.SetLayerRecursively(bglayer);

        return tile;
    }

    public static void SetTarget(MinesweeperTile newTarget)
    {
        if (_Target != null)
            _Target._isTargeted = false;
        _Target = newTarget;
        if (newTarget != null)
            newTarget._isTargeted = true;
    }

    public void Reveal(bool detonateMines = true, bool playSounds = true)
    {
        if (this._revealed || this._flagged)
            return;
        this._revealed = true;
        this._lifetime = 0f;
        if (this.IsMine && detonateMines)
            Exploder.Explode(this._center, Cleansweep._MineExplosion, Vector2.zero, ignoreQueues: true, ignoreDamageCaps: true);
        else if (playSounds)
            base.gameObject.PlayOnce("minesweeper_place_sound");
    }

    public void ManualUpdate(float dtime, float alpha)
    {
        if (!this._setup || GameManager.Instance.IsPaused)
        {
            this._numberLabel.IsVisible = false;
            this._numberLabel.Opacity = 0.0f;
            return;
        }
        this._lifetime += dtime;
        float percentDone = Mathf.Min(this._lifetime / _SHWOOP_TIME, 1f);
        float sizeFactor = percentDone < 0.33f ? (4f * percentDone) : (1.33f - 0.5f * (percentDone - 0.33f));
        float ease = Ease.InQuad(sizeFactor) - 1f;
        if (this._lifetime >= _SHWOOP_TIME)
            ease = 0f;
        Color renderColor = this._revealed ? this._color : this._flagged ? _FlaggedColor : _UnknownColor;
        if (!this._revealed && this._isTargeted)
            renderColor = Color.Lerp(renderColor.Invert(), renderColor, Mathf.Abs(Mathf.Sin(8f * BraveTime.ScaledTimeSinceStartup)));
        this._square.Setup(
          shape : Geometry.Shape.RECTANGLE,
          color : renderColor.WithAlpha(alpha * (0.75f - 0.5f * percentDone)),
          pos   : this._tl - ease * Vector2.one,
          pos2  : this._br + ease * Vector2.one);
        this._numberLabel.TextAlignment = TextAlignment.Center;
        this._numberLabel.VerticalAlignment = dfVerticalAlignment.Middle;
        this._numberLabel.IsVisible = this._revealed;
        this._numberLabel.Opacity = this._revealed ? alpha * _NUM_OPACITY : 0.0f;
        this._numberLabel.Place(this._labelPos);
    }

    private void OnDestroy()
    {
        if (this._square)
            UnityEngine.Object.Destroy(this._square.gameObject);
        if (this._numberLabel)
            UnityEngine.Object.Destroy(this._numberLabel.gameObject);
    }
}

public class MinesweeperGame : MonoBehaviour
{
    private const int _SAFETY_BUFFER = 2;
    private const float SPAWN_DELAY  = 0.10f;
    private const float SETUP_DELAY  = 0.05f;
    private const float _MINE_CHANCE = 0.2f;
    private const int ITER_DELAY     = 4; // number of iterations to delay revealing tiles after initial population
    private const float DISMISS_TIME = 1.0f;

    internal const int _TS           = 2; // tile scale -> number of game tiles per side corresponding to one minesweeper tile

    private static MinesweeperGame _Game = null;
    public static bool IsGameActive => _Game != null;
    public static MinesweeperGame ActiveGame
    {
        get
        {
            if (!_Game || !_Game.gameObject)
                _Game = new GameObject("MinesweeperGame").AddComponent<MinesweeperGame>();
            return _Game;
        }
    }

    private PlayerController _player                         = null;
    private bool _setup                                      = false;
    private bool _populated                                  = false;
    private IntVector2 _gameCenter                           = default;
    private List<MinesweeperTile> _tiles                     = null;
    private Dictionary<IntVector2, MinesweeperTile> _tileMap = null;
    private Coroutine _spawner                               = null;
    private MinesweeperTile _targetTile                      = null;
    private RoomHandler _gameRoom                            = null;
    private bool _dismissing                                 = false;
    private float _dismissTime                               = 0.0f;
    private Queue<MinesweeperTile> _revealQueue              = new();
    private float _nextRevealTime                            = 0f;

    public static void StartGame(PlayerController player)
    {
        RoomHandler playerRoom = player.CurrentRoom;
        if (playerRoom == null)
            return;
        IntVector2 playerTile = player.SpriteBottomCenter.XY().QuantizeTileRound(_TS);
        if (!IsValidStartingTile(playerTile, GameManager.Instance.Dungeon.data, playerRoom))
            return;
        if (_Game)
            _Game.Teardown();
        ActiveGame.Setup(player, playerTile);
    }

    public void Setup(PlayerController player, IntVector2 startingTile)
    {
        if (this._setup)
            return;

        this._player     = player;
        this._gameCenter = startingTile;
        this._tiles      = new List<MinesweeperTile>();
        this._tileMap    = new Dictionary<IntVector2, MinesweeperTile>();
        this._gameRoom   = this._player.CurrentRoom;
        this._gameRoom.OnEnemiesCleared += this.OnRoomCleared;
        this._spawner    = StartCoroutine(PopulateGrid());
        this._setup      = true;
    }

    private void DismissGameInternal()
    {
        if (this._dismissing)
            return;
        this._dismissing = true;
        this._dismissTime = DISMISS_TIME;
        foreach (var tile in this._tiles)
            tile.Reveal(detonateMines: false, playSounds: false);
        if (this._gameRoom != null)
            this._gameRoom.OnEnemiesCleared -= this.OnRoomCleared;
        base.gameObject.Play("minesweeper_place_sound");
    }

    public static void DismissGame()
    {
        if (_Game)
            _Game.DismissGameInternal();
    }

    private void OnRoomCleared()
    {
        this._gameRoom.OnEnemiesCleared -= this.OnRoomCleared;
        DismissGameInternal();
    }

    public void Teardown(bool calledFromDestroy = false)
    {
        if (this._gameRoom != null)
            this._gameRoom.OnEnemiesCleared -= this.OnRoomCleared;
        MinesweeperTile.SetTarget(null);
        for (int i = this._tiles.Count - 1; i >= 0; --i)
        {
            if (this._tiles[i])
                UnityEngine.Object.Destroy(this._tiles[i].gameObject);
        }
        this._tiles.Clear();
        if (this._spawner != null)
            StopCoroutine(this._spawner);
        if (_Game == this)
            _Game = null;
        if (!calledFromDestroy)
            UnityEngine.Object.Destroy(this);
    }

    public static bool FlagTarget()
    {
        if (!_Game || !_Game._targetTile || _Game._targetTile.IsRevealed)
            return false;
        _Game._targetTile.ToggleFlag();
        return true;
    }

    private static bool IsValidStartingTile(IntVector2 v, DungeonData dd, RoomHandler room)
    {
        CellData cellData = dd[v];
        if (cellData == null || cellData.parentRoom != room || cellData.type != CellType.FLOOR)
            return false;
        return true;
    }

    private static bool IsValidMineTile(IntVector2 v, DungeonData dd, RoomHandler room)
    {
        CellData cellData = dd[v];
        if (cellData == null || cellData.parentRoom != room || cellData.type != CellType.FLOOR || !cellData.IsPassable || cellData.isOccludedByTopWall)
            return false;
        return true;
    }

    private static Queue<IntVector2> _Frontier = new();
    private static HashSet<IntVector2> _Processed = new();
    private static HashSet<IntVector2> _Mines = new();
    private IEnumerator PopulateGrid()
    {
        DungeonData dd = GameManager.Instance.Dungeon.data;
        if (dd ==  null)
            yield break;

        // compute the game grid
        _Processed.Clear();
        _Frontier.Clear();
        _Mines.Clear();
        _Frontier.Enqueue(this._gameCenter);
        _Processed.Add(this._gameCenter);
        int generation = 0;
        while (_Frontier.Count > 0)
        {
            ++generation;
            for (int n = _Frontier.Count - 1; n >= 0; --n)
            {
                IntVector2 tilePos = _Frontier.Dequeue();
                for (int i = -_TS; i <= _TS; i += _TS)
                {
                    for (int j = -_TS; j <= _TS; j += _TS)
                    {
                        IntVector2 newPos = new IntVector2(tilePos.x + i, tilePos.y + j);
                        if (_Processed.Contains(newPos))
                            continue;
                        _Processed.Add(newPos);
                        bool viable = true;
                        for (int ii = 0; ii < _TS; ++ii)
                        {
                            for (int jj = 0; jj < _TS; ++jj)
                            {
                                IntVector2 subPos = new IntVector2(newPos.x + ii, newPos.y + jj);
                                if (!IsValidStartingTile(subPos, dd, this._gameRoom))
                                {
                                    viable = false;
                                    break;
                                }
                            }
                        }
                        if (!viable)
                            continue;
                        _Frontier.Enqueue(newPos);
                        if (generation > _SAFETY_BUFFER && UnityEngine.Random.value < _MINE_CHANCE)
                        {
                            if (IsValidMineTile(newPos, dd, this._gameRoom))
                                _Mines.Add(newPos);
                        }
                    }
                }
            }
        }

        // render the game grid
        _Processed.Clear();
        _Frontier.Clear();
        _Frontier.Enqueue(this._gameCenter);
        _Processed.Add(this._gameCenter);
        int iter = ITER_DELAY;
        while (_Frontier.Count > 0)
        {
            for (int n = _Frontier.Count - 1; n >= 0; --n)
            {
                IntVector2 tilePos = _Frontier.Dequeue();
                int neighborMines = 0;
                for (int i = -_TS; i <= _TS; i += _TS)
                {
                    for (int j = -_TS; j <= _TS; j += _TS)
                    {
                        IntVector2 newPos = new IntVector2(tilePos.x + i, tilePos.y + j);
                        if (_Mines.Contains(newPos))
                            ++neighborMines;
                        if (_Processed.Contains(newPos))
                            continue;
                        _Processed.Add(newPos);
                        bool viable = true;
                        for (int ii = 0; ii < _TS; ++ii)
                        {
                            for (int jj = 0; jj < _TS; ++jj)
                            {
                                IntVector2 subPos = new IntVector2(newPos.x + ii, newPos.y + jj);
                                if (!IsValidStartingTile(subPos, dd, this._gameRoom))
                                {
                                    viable = false;
                                    break;
                                }
                            }
                        }
                        if (!viable)
                            continue;
                        _Frontier.Enqueue(newPos);
                    }
                }
                MinesweeperTile newTile = MinesweeperTile.Setup(tilePos, _Mines.Contains(tilePos) ? -1 : neighborMines);
                if (this._tiles.Count == 0)
                    this._revealQueue.Enqueue(newTile); // first tile automatically gets added to reveal queue
                this._tiles.Add(newTile);
                this._tileMap[tilePos] = newTile;
            }
            this._player.gameObject.PlayOnce("minesweeper_place_sound");
            yield return new WaitForSeconds(SETUP_DELAY);
            if (--iter == 0)
                this._populated = true; // allow propagating after the first iteration of tiles is complete
        }

        this._populated = true; // in case we're in a really small room and didn't get to set this in the while loop above
        yield break;
    }

    private void RevealTile(MinesweeperTile tile)
    {
        if (tile.IsRevealed)
            return;
        tile.Reveal(detonateMines: true, playSounds: true);
        if (!tile.IsSafe)
            return;
        // if the tile has 0 mines next to it, reveal adjacent tiles
        IntVector2 revealPos = tile.Pos;
        for (int i = -_TS; i <= _TS; i += _TS)
        {
            for (int j = -_TS; j <= _TS; j += _TS)
            {
                IntVector2 neighborPos = new IntVector2(revealPos.x + i, revealPos.y + j);
                if (this._tileMap.TryGetValue(neighborPos, out MinesweeperTile neighborTile) && !neighborTile.IsRevealed)
                    this._revealQueue.Enqueue(neighborTile);
            }
        }
    }

    private void LateUpdate()
    {
        if (!this._setup)
            return;

        if (!this._player || this._player.CurrentRoom != this._gameRoom)
        {
            Teardown();
            return;
        }

        // update tiles
        float dtime = BraveTime.DeltaTime;
        if (this._dismissing)
        {
            this._dismissTime -= dtime;
            if (this._dismissTime <= 0.0f)
            {
                Teardown();
                return;
            }
        }

        foreach (MinesweeperTile tile in this._tiles)
            tile.ManualUpdate(dtime: dtime, this._dismissing ? this._dismissTime : 1.0f);

        if (this._dismissing || !this._populated || this._player.IsGhost || !this._player.CurrentGun || GameManager.Instance.IsPaused)
            return;

        // check if any enemies are standing on unrevealed mines if we've fully finished populating the minefield
        foreach (AIActor enemy in this._gameRoom.SafeGetEnemiesInRoom())
        {
            if (!enemy || !enemy.isActiveAndEnabled || enemy.IsGone || !enemy.IsValid || enemy.healthHaver is not HealthHaver hh || hh.IsDead)
                continue;
            if (enemy.specRigidbody is not SpeculativeRigidbody body)
                continue;
            MinesweeperTile enemyTile = null;
            if (this._tileMap.TryGetValue(body.UnitBottomCenter.QuantizeTileRound(_TS), out enemyTile))
                RevealTile(enemyTile);
            if (this._tileMap.TryGetValue(body.UnitBottomLeft.QuantizeTileRound(_TS), out enemyTile))
                RevealTile(enemyTile);
            if (this._tileMap.TryGetValue(body.UnitBottomRight.QuantizeTileRound(_TS), out enemyTile))
                RevealTile(enemyTile);
        }

        // update targeted tile
        IntVector2 curTile = this._player.SpriteBottomCenter.XY().FloorToInt().ToIntVector2().QuantizeRound(_TS);
        bool validTile = this._tileMap.TryGetValue(curTile, out MinesweeperTile standingTile);
        if (validTile && !standingTile.IsFlagged) // don't reveal flagged tiles so we can safely walk over them
            this._revealQueue.Enqueue(standingTile);

        int numToReveal = this._revealQueue.Count;
        float now = BraveTime.ScaledTimeSinceStartup;
        if (numToReveal > 0 && this._nextRevealTime <= now)
        {
            for (int n = 0; n < numToReveal; ++n)
                RevealTile(this._revealQueue.Dequeue());
            this._nextRevealTime = now + SPAWN_DELAY;
        }

        float aimAngle;
        if (!this._player.IsKeyboardAndMouse())  //WARNING: this has caused a null dereference, but not sure how
          aimAngle = this._player.m_activeActions.Aim.Vector.ToAngle();
        else
          aimAngle = (this._player.unadjustedAimPoint.XY() - this._player.CenterPosition).ToAngle();
        int quadrant = Mathf.RoundToInt(aimAngle.Clamp360() / 45f);
        IntVector2 aimVec = quadrant switch {
            1 => IntVector2.UpRight,
            2 => IntVector2.Up,
            3 => IntVector2.UpLeft,
            4 => IntVector2.Left,
            5 => IntVector2.DownLeft,
            6 => IntVector2.Down,
            7 => IntVector2.DownRight,
            _ => IntVector2.Right,
        };
        IntVector2 aimTile = curTile + _TS * aimVec;
        Gun playerGun = this._player.CurrentGun;
        bool validTarget = this._tileMap.TryGetValue(aimTile, out MinesweeperTile targetTile);
        this._targetTile = (validTarget && playerGun && playerGun.gameObject.GetComponent<Cleansweep>()) ? targetTile : null;
        MinesweeperTile.SetTarget(this._targetTile);
    }

    private void OnDestroy()
    {
        Teardown(calledFromDestroy: true);
    }
}
