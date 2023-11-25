namespace CwaffingTheGungy;

public class Grandmaster : AdvancedGunBehavior
{
    public static string ItemName         = "Grandmaster";
    public static string SpriteName       = "grandmaster";
    public static string ProjectileName   = "ak-47"; // no rotation
    public static string ShortDescription = "Mate in Gun";
    public static string LongDescription  = "Fires assorted chess pieces that home towards enemies in discrete steps.";
    public static string Lore             = "This gun was wielded by the legendary Magnum Carlsen in his bullet chess world championship match against the equally legendary Garry Makarov. While the match ended in a draw, it was notable for being Makarov's final match before retiring to a life of mentorship for the new generation of aspiring Gungeoneers.";

    internal static tk2dSpriteAnimationClip _PawnSprite;
    internal static tk2dSpriteAnimationClip _RookSprite;
    internal static tk2dSpriteAnimationClip _BishopSprite;
    internal static tk2dSpriteAnimationClip _KnightSprite;
    internal static tk2dSpriteAnimationClip _QueenSprite;
    internal static tk2dSpriteAnimationClip _KingSprite;

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<Grandmaster>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.A, gunClass: GunClass.SILLY, reloadTime: 1.0f, ammo: 350);
            gun.SetAnimationFPS(gun.shootAnimation, 24);
            gun.SetAnimationFPS(gun.reloadAnimation, 16);
            gun.SetMuzzleVFX(Items.Mailbox); // innocuous muzzle flash effects
            gun.SetFireAudio("chess_gun_fire");
            gun.SetReloadAudio("chess_gun_reload");

        gun.SetupSingularProjectile(clipSize: 20, cooldown: 0.1f, shootStyle: ShootStyle.SemiAutomatic, customClip: SpriteName, speed: 30f
          ).AttachComponent<PlayChessBehavior>(
          ).AddAnimations(
            AnimatedBullet.Create(refClip: ref _PawnSprite,   name: "chess_pawn",   scale: 0.8f, anchor: Anchor.MiddleCenter),
            AnimatedBullet.Create(refClip: ref _RookSprite,   name: "chess_rook",   scale: 0.8f, anchor: Anchor.MiddleCenter),
            AnimatedBullet.Create(refClip: ref _BishopSprite, name: "chess_bishop", scale: 0.8f, anchor: Anchor.MiddleCenter),
            AnimatedBullet.Create(refClip: ref _KnightSprite, name: "chess_knight", scale: 0.8f, anchor: Anchor.MiddleCenter),
            AnimatedBullet.Create(refClip: ref _QueenSprite,  name: "chess_queen",  scale: 0.8f, anchor: Anchor.MiddleCenter),
            AnimatedBullet.Create(refClip: ref _KingSprite,   name: "chess_king",   scale: 0.8f, anchor: Anchor.MiddleCenter)
          );
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

    private void Start()
    {
        // ETGModConsole.Log($"chose {this.GetType()}");
    }

    private void Update()
    {
        this._lifetime += BraveTime.DeltaTime;
        if (this._movePhase == 0)
        {
            if (this._lifetime >= _BaseMovePause)
            {
                StartMoving();
                this._trail.Enable();
                this._projectile.collidesWithEnemies = true;
                this._movePhase                      = 1;
                this._lifetime                       = 0.0f;
                if (this._twoPhaseMove)
                {
                    // Get the magnitude of the both components of the vector
                    float xmag = Mathf.Abs(this._targetVec.x);
                    float ymag = Mathf.Abs(this._targetVec.y);
                    // Return the larger component on phase 1
                    Vector2 axisVec;
                    if (xmag > ymag)
                        axisVec = this._targetVec.WithY(0);
                    else
                        axisVec = this._targetVec.WithX(0);
                    // ETGModConsole.Log($"sending in major direction {axisVec}");
                    this._projectile.SendInDirection(axisVec, true);
                }
            }
            else
                UpdatePaused();
        }
        else if (this._twoPhaseMove && this._movePhase == 1)
        {
            if (this._lifetime >= _BaseMoveTime)
            {
                // Get the magnitude of the both components of the vector
                float xmag = Mathf.Abs(this._targetVec.x);
                float ymag = Mathf.Abs(this._targetVec.y);
                // Return the smaller component on phase 2
                Vector2 axisVec;
                if (xmag > ymag)
                    axisVec = this._targetVec.WithX(0);
                else
                    axisVec = this._targetVec.WithY(0);
                // ETGModConsole.Log($"sending in minor direction {axisVec}");
                this._projectile.SendInDirection(axisVec, true);
                this._movePhase = 2; // very intentionally not resetting lifetime here
            }
            else
                UpdateMoving();
        }
        else
        {
            if (this._lifetime >= (_BaseMoveTime * this._movePhase)) // double the wait time for knight moves
            {
                StopMoving();
                this._trail.Disable();
                this._projectile.collidesWithEnemies = false;
                this._movePhase                      = 0;
                this._lifetime                       = 0.0f;
            }
            else
                UpdateMoving();
        }
        UpdateAlways();
    }

    protected float ComputeSpeed(float dist, float time)
    {
        return dist / (time * C.FPS);
    }

    public virtual void Setup(Projectile projectile, PlayerController owner)
    {
        this._projectile = projectile;
        this._owner      = owner;

        this._sprite     = GetSprite();
        this._speed      = ComputeSpeed(GetMoveDistance(), GetMoveTime());

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
    protected abstract float GetMoveTime();
    protected abstract Color GetTrailColor();

    protected virtual Vector2? ChooseNewTarget()
    {
        return null; // don't change target by default
    }
    protected virtual void UpdateCreate() { /* do nothing by default */ }
    protected virtual void UpdatePaused() { /* do nothing by default */ }
    protected virtual void UpdateMoving() { /* do nothing by default */ }
    protected virtual void UpdateAlways() { /* do nothing by default */ }

    public void StartMoving()
    {
        this._target = ChooseNewTarget();
        if (this._target is Vector2 target)
        {
            this._targetVec = target - this._projectile.sprite.WorldCenter;
            float adjSpeed = this._targetVec.magnitude / _BaseMoveTime;
            this._projectile.baseData.speed = Mathf.Min(this._speed, adjSpeed);
        }
        else
        {
            this._projectile.baseData.speed = this._speed;
        }
        this._projectile.SendInDirection(this._targetVec, true);
        this._projectile.UpdateSpeed();
    }

    public void StopMoving()
    {
        AkSoundEngine.PostEvent("chess_move", this.gameObject);
        this._projectile.baseData.speed = 0.001f;
        this._projectile.UpdateSpeed();
    }

    protected float LockAngleToOneOf(float angle, List<float> angles)
    {
        if (angles.Count == 0)
            return angle; // all angles are valid

        float a = angle.Clamp360();

        float best      = 0.0f;
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

    protected float GetBestValidAngleForPiece(float angle)
    {
        return LockAngleToOneOf(angle, GetValidAnglesForPiece());
    }

    protected virtual List<float> GetValidAnglesForPiece()
    {
        return new();
    }

    protected Vector2? PointOrthognalTo(Vector2 start, Vector2 target, Vector2 dir, float projAmount = 1000f)
    {
        // Project a point outward from start in direction dir by amount projAmount
        Vector2 projection = start + (projAmount * dir);

        // Project a line orthogonal to dir through our target
        Vector2 ortho = projAmount * dir.Rotate(degrees: 90);
        Vector2 bbeg  = target + ortho;
        Vector2 bend  = target - ortho;

        // Find the orthogonal intersection point, or return null if no such point exists
        Vector2 ipoint;
        if (!BraveUtility.LineIntersectsLine(start, projection, bbeg, bend, out ipoint))
            return null;
        return ipoint;
    }

    protected Vector2? ScanForTarget()
    {
        // If we've already found a target previously, we have nothing else to do
        if (this._targetEnemy)
        {
            if (!this._targetEnemy.healthHaver.IsDead)
            {
                float bestAngle = GetBestValidAngleForPiece((this._targetEnemy.sprite.WorldCenter - this._projectile.sprite.WorldCenter).ToAngle());
                return PointOrthognalTo(this._projectile.sprite.WorldCenter, this._targetEnemy.sprite.WorldCenter, bestAngle.ToVector());
            }
            this._targetEnemy = null; // reset our target and march onward
        }

        // Get our position and direction
        Vector2 ppos = this._projectile.sprite.WorldCenter;

        // Find the closest viable enemy == one which we can move into the line of sight
        Vector2? closestViableEnemyPosition = null;
        float closestEnemyDistance = 999999f;
        // float closestOrthoDistance = 999999f;
        foreach (AIActor enemy in this._projectile.transform.position.GetAbsoluteRoom()?.GetActiveEnemies(RoomHandler.ActiveEnemyType.All).EmptyIfNull())
        {
            if (!enemy.IsNormalEnemy || !enemy.healthHaver || enemy.IsHarmlessEnemy)
                continue; // we only care about normal, alive, hostile enemies

            // Get the enemy's position and distance
            Vector2 epos = enemy.sprite.WorldCenter;
            float edist  = (epos - ppos).magnitude;
            if (edist >= closestEnemyDistance)
                continue; // we only care about the closest enemy

            // Check if we can move orthogonal to the enemy in any of our valid directions
            Vector2? ipoint = null;
            Vector2 dirVec = Vector2.zero;
            foreach(float candidateAngle in GetValidAnglesForPiece())
            {
                ipoint = PointOrthognalTo(ppos, epos, candidateAngle.ToVector());
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
    protected override tk2dSpriteAnimationClip GetSprite() => Grandmaster._PawnSprite;
    protected override float GetMoveDistance()             => _BaseMoveDist;
    protected override float GetMoveTime()                 => _BaseMoveTime;
    protected override Color GetTrailColor()               => Color.white;
}

public class RookPiece : ChessPiece
{
    protected override tk2dSpriteAnimationClip GetSprite() => Grandmaster._RookSprite;
    protected override float GetMoveDistance()             => 450f;
    protected override float GetMoveTime()                 => ChessPiece._BaseMoveTime;
    protected override Color GetTrailColor()               => Color.magenta;

    protected override void UpdateCreate()
    {
        // Rook should snap to 90 degree angles after initial shot
        this._targetVec = GetBestValidAngleForPiece(this._projectile.m_currentDirection.ToAngle()).ToVector();
    }

    private static List<float> _AnglesOf90 = new(){0f, 90f, 180f, 270f};

    protected override List<float> GetValidAnglesForPiece()
    {
        return _AnglesOf90;
    }

    protected override Vector2? ChooseNewTarget()
    {
        return ScanForTarget();
    }
}

public class BishopPiece : ChessPiece
{
    protected override tk2dSpriteAnimationClip GetSprite() => Grandmaster._BishopSprite;
    protected override float GetMoveDistance()             => 350f;
    protected override float GetMoveTime()                 => ChessPiece._BaseMoveTime;
    protected override Color GetTrailColor()               => Color.cyan;

    protected override void UpdateCreate()
    {
        // Bishop should snap to 45 degree angles after initial shot
        this._targetVec = GetBestValidAngleForPiece(this._projectile.m_currentDirection.ToAngle()).ToVector();
    }

    private static List<float> _AnglesOf45 = new(){45f, 135f, 225f, 315f};

    protected override List<float> GetValidAnglesForPiece()
    {
        return _AnglesOf45;
    }

    protected override Vector2? ChooseNewTarget()
    {
        return ScanForTarget();
    }
}

public class KnightPiece : ChessPiece
{
    protected override tk2dSpriteAnimationClip GetSprite() => Grandmaster._KnightSprite;
    protected override float GetMoveDistance()             => 200f;
    protected override float GetMoveTime()                 => ChessPiece._BaseMoveTime;
    protected override Color GetTrailColor()               => Color.green;

    protected override void UpdateCreate()
    {
        // Knight should snap to 30 degree angles after initial shot
        this._targetVec = GetBestValidAngleForPiece(this._projectile.m_currentDirection.ToAngle()).ToVector();

        // Knights also have a two step move
        this._twoPhaseMove = true;
        this._movePhase = 2;
    }

    private static List<float> _AnglesOf30 = new(){30f, 60f, 120f, 150f, 210f, 240f, 300f, 330f};

    protected override List<float> GetValidAnglesForPiece()
    {
        return _AnglesOf30;
    }

    protected override Vector2? ChooseNewTarget()
    {
        return ScanForTarget();
    }
}

public class QueenPiece : ChessPiece
{
    protected override tk2dSpriteAnimationClip GetSprite() => Grandmaster._QueenSprite;
    protected override float GetMoveDistance()             => 500f;
    protected override float GetMoveTime()                 => ChessPiece._BaseMoveTime;
    protected override Color GetTrailColor()               => Color.yellow;

    protected override void UpdateCreate()
    {
        // Queen should snap to 45 and 90 degree angles after initial shot
        this._targetVec = GetBestValidAngleForPiece(this._projectile.m_currentDirection.ToAngle()).ToVector();
    }

    private static List<float> _AnglesOf45And90 = new(){0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f};

    protected override List<float> GetValidAnglesForPiece()
    {
        return _AnglesOf45And90;
    }

    protected override Vector2? ChooseNewTarget()
    {
        return ScanForTarget();
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
    private Projectile _projectile  = null;
    private PlayerController _owner = null;
    private ChessPiece _piece       = null;

    private static List<ChessPieces> _PiecePool = new() {
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

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        if (this._projectile.Owner is not PlayerController pc)
            return;
        this._owner = pc;

        switch(_PiecePool.ChooseRandom())
        {
            case ChessPieces.Pawn:
                this._piece = this._projectile.gameObject.AddComponent<PawnPiece>();
                break;
            case ChessPieces.Rook:
                this._piece = this._projectile.gameObject.AddComponent<RookPiece>();
                break;
            case ChessPieces.Bishop:
                this._piece = this._projectile.gameObject.AddComponent<BishopPiece>();
                break;
            case ChessPieces.Knight:
                this._piece = this._projectile.gameObject.AddComponent<KnightPiece>();
                break;
            case ChessPieces.Queen:
                this._piece = this._projectile.gameObject.AddComponent<QueenPiece>();
                break;
            // case ChessPieces.King:
            //     this._piece = this._projectile.gameObject.AddComponent<KingPiece>();
            //     break;
            default:
                this._piece = this._projectile.gameObject.AddComponent<PawnPiece>();
                break;
        }

        this._piece.Setup(this._projectile, pc);
    }
}
