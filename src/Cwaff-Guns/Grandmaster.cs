namespace CwaffingTheGungy;

public class Grandmaster : CwaffGun
{
    public static string ItemName         = "Grandmaster";
    public static string ShortDescription = "Mate in Gun";
    public static string LongDescription  = "Fires assorted chess pieces that home towards enemies in discrete steps.";
    public static string Lore             = "This gun was wielded by the legendary Magnum Carlsen in his bullet chess world championship match against the equally legendary Garry Makarov. While the match ended in a draw, it was notable for being Makarov's final match before retiring to a life of mentorship for the new generation of aspiring Gungeoneers.";

    internal static tk2dSpriteAnimationClip _PawnSprite;
    internal static tk2dSpriteAnimationClip _RookSprite;
    internal static tk2dSpriteAnimationClip _BishopSprite;
    internal static tk2dSpriteAnimationClip _KnightSprite;
    internal static tk2dSpriteAnimationClip _QueenSprite;
    internal static tk2dSpriteAnimationClip _KingSprite;
    internal static tk2dSpriteAnimationClip _BlackPawnSprite;
    internal static tk2dSpriteAnimationClip _BlackRookSprite;
    internal static tk2dSpriteAnimationClip _BlackBishopSprite;
    internal static tk2dSpriteAnimationClip _BlackKnightSprite;
    internal static tk2dSpriteAnimationClip _BlackQueenSprite;
    internal static tk2dSpriteAnimationClip _BlackKingSprite;

    internal static Projectile _Projectile;

    public static void Init()
    {
        Lazy.SetupGun<Grandmaster>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.A, gunClass: GunClass.SILLY, reloadTime: 1.0f, ammo: 350, shootFps: 24, reloadFps: 16,
            muzzleFrom: Items.Mailbox, fireAudio: "chess_gun_fire", reloadAudio: "chess_gun_reload")
          .InitProjectile(GunData.New(clipSize: 20, cooldown: 0.1f, shootStyle: ShootStyle.SemiAutomatic, customClip: true,
            speed: 30f, damage: 5.5f, force: 9f, range: 1000f, shouldRotate: false))
          .AddAnimations(
            AnimatedBullet.Create(refClip: ref _PawnSprite,        name: "chess_pawn",         scale: 0.8f, anchor: Anchor.MiddleCenter),
            AnimatedBullet.Create(refClip: ref _RookSprite,        name: "chess_rook",         scale: 0.8f, anchor: Anchor.MiddleCenter),
            AnimatedBullet.Create(refClip: ref _BishopSprite,      name: "chess_bishop",       scale: 0.8f, anchor: Anchor.MiddleCenter),
            AnimatedBullet.Create(refClip: ref _KnightSprite,      name: "chess_knight",       scale: 0.8f, anchor: Anchor.MiddleCenter),
            AnimatedBullet.Create(refClip: ref _QueenSprite,       name: "chess_queen",        scale: 0.8f, anchor: Anchor.MiddleCenter),
            AnimatedBullet.Create(refClip: ref _KingSprite,        name: "chess_king",         scale: 0.8f, anchor: Anchor.MiddleCenter),
            AnimatedBullet.Create(refClip: ref _BlackPawnSprite,   name: "chess_pawn_black",   scale: 0.8f, anchor: Anchor.MiddleCenter),
            AnimatedBullet.Create(refClip: ref _BlackRookSprite,   name: "chess_rook_black",   scale: 0.8f, anchor: Anchor.MiddleCenter),
            AnimatedBullet.Create(refClip: ref _BlackBishopSprite, name: "chess_bishop_black", scale: 0.8f, anchor: Anchor.MiddleCenter),
            AnimatedBullet.Create(refClip: ref _BlackKnightSprite, name: "chess_knight_black", scale: 0.8f, anchor: Anchor.MiddleCenter),
            AnimatedBullet.Create(refClip: ref _BlackQueenSprite,  name: "chess_queen_black",  scale: 0.8f, anchor: Anchor.MiddleCenter),
            AnimatedBullet.Create(refClip: ref _BlackKingSprite,   name: "chess_king_black",   scale: 0.8f, anchor: Anchor.MiddleCenter))
          .Attach<PlayChessBehavior>()
          .Assign(out _Projectile);
    }

    public override void OnDestroy()
    {
        if (this.PlayerOwner)
            this.PlayerOwner.OnReloadedGun -= OnGrandmasterReloaded;
    }

    public override void OnPlayerPickup(PlayerController player)
    {
        base.OnPlayerPickup(player);
        player.OnReloadedGun += OnGrandmasterReloaded;
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        player.OnReloadedGun -= OnGrandmasterReloaded;
        base.OnDroppedByPlayer(player);
    }

    private static void OnGrandmasterReloaded(PlayerController usingPlayer, Gun usedGun)
    {
        if (usedGun.GetComponent<Grandmaster>() is not Grandmaster gm)
            return;
    }
}

public enum ChessPieces {
    Pawn   = 0,
    Rook   = 1,
    Bishop = 2,
    Knight = 3,
    Queen  = 4,
    // King   = 5, // the gun itself is a king, don't need this for now; maybe a synergy later?
};

public abstract class ChessPiece : MonoBehaviour
{
    protected static float _BaseMoveDist  = 180f; // number of max pixels to travel each move
    protected static float _BaseMoveTime  = 0.1f; // seconds for a piece to move from one position to another
    protected static float _BaseMovePause = 0.4f; // seconds a piece waits after moving before moving again

    protected tk2dSpriteAnimationClip _sprite = null;
    protected Projectile _projectile  = null;
    protected PlayerController _owner = null;
    protected float _lifetime         = 0.0f;
    protected int _movePhase          = 1;     // 1 == moving, 2 == knight's second move, 0 == paused
    protected float _speed            = 0.0f;
    protected EasyTrailBullet _trail  = null;
    protected Vector2? _target        = null;  // the target position we're actually aiming at
    protected Vector2  _targetVec     = Vector2.zero;  // the vector we are traveling to reach that target position

    protected AIActor _targetEnemy    = null;  // which enemy, if any, we're currently targeting
    protected bool _twoPhaseMove      = false; // whether the piece moves in two phases; true only for knight
    protected bool _mastered          = false; // whether Grandmaster has been mastered
    protected float _moveTime         = 0.0f;
    protected float _pauseTime        = 0.0f;
    protected bool _spawnBlack        = false;
    protected bool _isBlack           = false;

    private void Update()
    {
        this._lifetime += BraveTime.DeltaTime;
        if (this._movePhase == 0)
        {
            if (this._lifetime >= this._pauseTime)
            {
                StartMoving();
                this._trail.Enable();
                this._projectile.collidesWithEnemies = true;
                this._movePhase                      = 1;
                this._lifetime                       = 0.0f;
                if (this._twoPhaseMove)
                    this._projectile.SendInDirection(this._targetVec.LargerComponent(), true);
            }
        }
        else if (this._twoPhaseMove && this._movePhase == 1)
        {
            if (this._lifetime >= this._moveTime)
            {
                this._projectile.SendInDirection(this._targetVec.SmallerComponent(), true);
                this._movePhase = 2; // very intentionally not resetting lifetime here
            }
        }
        else if (this._lifetime >= (this._moveTime * this._movePhase)) // double the wait time for knight moves
        {
            StopMoving();
            this._trail.Disable();
            this._projectile.collidesWithEnemies = false;
            this._movePhase                      = 0;
            this._lifetime                       = 0.0f;
            if (this._spawnBlack)
            {
                this._spawnBlack = false;
                Projectile p = VolleyUtility.ShootSingleProjectile(
                    Grandmaster._Projectile, this._projectile.SafeCenter, -this._targetVec.ToAngle(), false, this._owner);
                p.GetComponent<PlayChessBehavior>().isBlackPiece = true;
            }
        }
    }

    public virtual void Setup(Projectile projectile, PlayerController owner, bool isBlackPiece)
    {
        this._projectile = projectile;
        this._owner      = owner;
        this._mastered   = this._owner.HasSynergy(Synergy.MASTERY_GRANDMASTER);
        this._spawnBlack = this._mastered && !isBlackPiece;
        this._isBlack    = isBlackPiece;

        this._sprite     = GetSprite();
        this._moveTime   = _BaseMoveTime * (this._mastered ? 0.5f : 1f);
        this._pauseTime  = _BaseMovePause * (this._mastered ? 0.5f : 1f) / owner.ProjSpeedMult();
        this._speed      = GetMoveDistance() / (_BaseMoveTime * C.FPS);
        this._movePhase  = this._twoPhaseMove ? 2 : 1;

        this._trail = this._projectile.gameObject.AddComponent<EasyTrailBullet>();
            this._trail.StartWidth = 0.2f;
            this._trail.EndWidth   = 0.1f;
            this._trail.LifeTime   = 0.1f;
            this._trail.BaseColor  = GetTrailColor();
            this._trail.StartColor = GetTrailColor();
            this._trail.EndColor   = GetTrailColor();

        this._projectile.SetAnimation(this._sprite);
        this._targetVec = this._projectile.m_currentDirection;
        UpdateCreate();
    }

    protected abstract tk2dSpriteAnimationClip GetSprite();
    protected abstract float GetMoveDistance();
    protected abstract Color GetTrailColor();

    protected virtual Vector2? ChooseNewTarget() => null; // don't change target by default
    protected virtual void UpdateCreate() {} /* do nothing by default */

    public void StartMoving()
    {
        this._target = ChooseNewTarget();
        if (this._target is Vector2 target)
        {
            this._targetVec = target - this._projectile.SafeCenter;
            float adjSpeed = this._targetVec.magnitude / this._moveTime;
            this._projectile.SetSpeed(Mathf.Min(this._speed, adjSpeed));
        }
        else
        {
            if (!this._twoPhaseMove || this._targetVec.LargerComponent().sqrMagnitude == 0)
                this._targetVec = this._speed * GetBestValidAngleForPiece(this._projectile.m_currentDirection.ToAngle()).ToVector();
            this._projectile.SetSpeed(this._speed);
        }

        this._projectile.SendInDirection(this._targetVec, true);
    }

    public void StopMoving()
    {
        this.gameObject.Play("chess_move");
        this._projectile.SetSpeed(0.001f);
        this._projectile.m_usesNormalMoveRegardless = true; // disable movement modifiers such as Helix Bullets and Orbital Bullets
    }

    protected float LockAngleToOneOf(float angle, IEnumerable<float> angles)
    {
        float a         = angle.Clamp360();
        float best      = angle;
        float bestDelta = 999f;
        foreach(float c in angles)
        {
            float delta = Mathf.Abs(c - a);
            if (delta > bestDelta)
                continue;
            bestDelta = delta;
            best = c;
        }
        return best;
    }

    protected float GetBestValidAngleForPiece(float angle) => LockAngleToOneOf(angle, GetValidAnglesForPiece());

    protected virtual IEnumerable<float> GetValidAnglesForPiece() => Enumerable.Empty<float>();

    protected Vector2? ScanForTarget()
    {
        // If we've already found a target previously, we have nothing else to do
        if (this._targetEnemy)
        {
            if (!this._targetEnemy.healthHaver.IsDead)
            {
                float bestAngle = GetBestValidAngleForPiece((this._targetEnemy.CenterPosition - this._projectile.SafeCenter).ToAngle());
                return Lazy.PointOrthognalTo(this._projectile.SafeCenter, this._targetEnemy.CenterPosition, bestAngle.ToVector());
            }
            this._targetEnemy = null; // reset our target and march onward
        }

        // Get our position and direction
        Vector2 ppos = this._projectile.SafeCenter;

        // Find the closest viable enemy == one which we can move into the line of sight
        Vector2? closestViableEnemyPosition = null;
        float closestEnemyDistance = 999999f;
        // float closestOrthoDistance = 999999f;
        foreach (AIActor enemy in this._projectile.SafeCenter.SafeGetEnemiesInRoom())
        {
            if (!enemy.IsNormalEnemy || !enemy.healthHaver || enemy.IsHarmlessEnemy)
                continue; // we only care about normal, alive, hostile enemies

            // Get the enemy's position and distance
            Vector2 epos = enemy.CenterPosition;
            float edist  = (epos - ppos).magnitude;
            if (edist >= closestEnemyDistance)
                continue; // we only care about the closest enemy

            // Check if we can move orthogonal to the enemy in any of our valid directions
            Vector2? ipoint = null;
            Vector2 dirVec = Vector2.zero;
            foreach(float candidateAngle in GetValidAnglesForPiece())
            {
                ipoint = Lazy.PointOrthognalTo(ppos, epos, candidateAngle.ToVector());
                if (ipoint.HasValue)
                    break;
            }
            if (!ipoint.HasValue)
                continue; // if we're not orthogonal to the enemy in any direction, ignore it
            if (!ppos.HasLineOfSight(ipoint.Value))
                continue; // if we collide with a wall, we don't care

            // If there's no collision, it's a good position!
            closestEnemyDistance       = edist;
            closestViableEnemyPosition = ipoint;
            this._targetEnemy          = enemy;
        }

        return closestViableEnemyPosition;
    }
}

public class PawnPiece   : ChessPiece {
    protected override tk2dSpriteAnimationClip GetSprite() => this._isBlack ? Grandmaster._BlackPawnSprite : Grandmaster._PawnSprite;
    protected override float GetMoveDistance()             => _BaseMoveDist;
    protected override Color GetTrailColor()               => Color.white;
}

public class RookPiece : ChessPiece
{
    private static readonly List<float> _AnglesOf90 = new(){0f, 90f, 180f, 270f};

    protected override tk2dSpriteAnimationClip GetSprite()         => this._isBlack ? Grandmaster._BlackRookSprite : Grandmaster._RookSprite;
    protected override float GetMoveDistance()                     => 450f;
    protected override Color GetTrailColor()                       => Color.magenta;
    protected override IEnumerable<float> GetValidAnglesForPiece() => _AnglesOf90;
    protected override Vector2? ChooseNewTarget()                  => ScanForTarget();

    protected override void UpdateCreate()
    {
        // Rook should snap to 90 degree angles after initial shot
        this._targetVec = GetBestValidAngleForPiece(this._projectile.m_currentDirection.ToAngle()).ToVector();
    }
}

public class BishopPiece : ChessPiece
{
    private static readonly List<float> _AnglesOf45 = new(){45f, 135f, 225f, 315f};

    protected override tk2dSpriteAnimationClip GetSprite()         => this._isBlack ? Grandmaster._BlackBishopSprite : Grandmaster._BishopSprite;
    protected override float GetMoveDistance()                     => 350f;
    protected override Color GetTrailColor()                       => Color.cyan;
    protected override IEnumerable<float> GetValidAnglesForPiece() => _AnglesOf45;
    protected override Vector2? ChooseNewTarget()                  => ScanForTarget();

    protected override void UpdateCreate()
    {
        // Bishop should snap to 45 degree angles after initial shot
        this._targetVec = GetBestValidAngleForPiece(this._projectile.m_currentDirection.ToAngle()).ToVector();
    }
}

public class KnightPiece : ChessPiece
{
    private static List<float> _AnglesOf30 = new(){30f, 60f, 120f, 150f, 210f, 240f, 300f, 330f};

    protected override tk2dSpriteAnimationClip GetSprite()         => this._isBlack ? Grandmaster._BlackKnightSprite : Grandmaster._KnightSprite;
    protected override float GetMoveDistance()                     => 200f;
    protected override Color GetTrailColor()                       => Color.green;
    protected override IEnumerable<float> GetValidAnglesForPiece() => _AnglesOf30;
    protected override Vector2? ChooseNewTarget()                  => ScanForTarget();

    protected override void UpdateCreate()
    {
        // Knight should snap to 30 degree angles after initial shot
        this._targetVec = GetBestValidAngleForPiece(this._projectile.m_currentDirection.ToAngle()).ToVector();

        // Knights also have a two step move
        this._twoPhaseMove = true;
        this._movePhase    = 2;
    }
}

public class QueenPiece : ChessPiece
{
    private static List<float> _AnglesOf45And90 = new(){0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f};

    protected override tk2dSpriteAnimationClip GetSprite()         => this._isBlack ? Grandmaster._BlackQueenSprite : Grandmaster._QueenSprite;
    protected override float GetMoveDistance()                     => 500f;
    protected override Color GetTrailColor()                       => Color.yellow;
    protected override IEnumerable<float> GetValidAnglesForPiece() => _AnglesOf45And90;
    protected override Vector2? ChooseNewTarget()                  => ScanForTarget();

    protected override void UpdateCreate()
    {
        // Queen should snap to 45 and 90 degree angles after initial shot
        this._targetVec = GetBestValidAngleForPiece(this._projectile.m_currentDirection.ToAngle()).ToVector();
    }
}

// public class KingPiece : ChessPiece
// {
//     protected override tk2dSpriteAnimationClip GetSprite() => Grandmaster._PawnSprite;
//     protected override float GetMoveDistance()             => _BaseMoveDist;
//     protected override float GetMoveTime()                 => _BaseMoveTime;
//     protected override Color GetTrailColor()               => Color.yellow;
// }

public class PlayChessBehavior : MonoBehaviour
{
    public bool isBlackPiece = false;

    private Projectile _projectile  = null;
    private PlayerController _owner = null;
    private ChessPiece _piece       = null;

    private static readonly List<ChessPieces> _PiecePool = new() {
        ChessPieces.Pawn,
        ChessPieces.Pawn,
        ChessPieces.Pawn,
        ChessPieces.Pawn,
        ChessPieces.Pawn,
        ChessPieces.Pawn,
        ChessPieces.Pawn,
        ChessPieces.Pawn, // 8 pawns
        ChessPieces.Rook,
        ChessPieces.Rook, // 2 rooks
        ChessPieces.Bishop,
        ChessPieces.Bishop, // 2 bishops
        ChessPieces.Knight,
        ChessPieces.Knight, // 2 knights
        ChessPieces.Queen, // 1 queen (and 0 kings for now)
    };

    private static readonly List<ChessPieces> _MasteredPiecePool = new() {
        ChessPieces.Rook,
        ChessPieces.Rook, // 2 rooks
        ChessPieces.Bishop,
        ChessPieces.Bishop, // 2 bishops
        ChessPieces.Knight,
        ChessPieces.Knight, // 2 knights
        ChessPieces.Queen, // 1 queen (and 0 kings for now)
    };

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._owner      = this._projectile.Owner as PlayerController;
        if (!this._owner)
            return;

        switch((this._owner.HasSynergy(Synergy.MASTERY_GRANDMASTER) ? _MasteredPiecePool : _PiecePool).ChooseRandom())
        {
            case ChessPieces.Pawn:   this._piece = this._projectile.gameObject.AddComponent<PawnPiece>();   break;
            case ChessPieces.Rook:   this._piece = this._projectile.gameObject.AddComponent<RookPiece>();   break;
            case ChessPieces.Bishop: this._piece = this._projectile.gameObject.AddComponent<BishopPiece>(); break;
            case ChessPieces.Knight: this._piece = this._projectile.gameObject.AddComponent<KnightPiece>(); break;
            case ChessPieces.Queen:  this._piece = this._projectile.gameObject.AddComponent<QueenPiece>();  break;
            // case ChessPieces.King:   this._piece = this._projectile.gameObject.AddComponent<KingPiece>();   break;
            default:                 this._piece = this._projectile.gameObject.AddComponent<PawnPiece>();   break;
        }

        this._piece.Setup(this._projectile, this._owner, isBlackPiece);
    }
}
