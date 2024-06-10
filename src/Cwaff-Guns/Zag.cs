namespace CwaffingTheGungy;

public class Zag : CwaffGun
{
    public static string ItemName         = "Zag";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    internal static TrailController _ZagTrailPrefab = null;
    internal static GameObject _ZagZigVFX = null;

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<Zag>(ItemName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.C, gunClass: GunClass.PISTOL, reloadTime: 0.8f, ammo: 400, shootFps: 30, reloadFps: 40,
                fireAudio: "zag_zig_sound", reloadAudio: "zag_zig_sound");
            gun.LoopAnimation(gun.reloadAnimation);

        gun.InitProjectile(GunData.New(clipSize: 9, cooldown: 0.125f, shootStyle: ShootStyle.SemiAutomatic,
          damage: 5.0f, speed: 40.0f, sprite: "zag_bullet", fps: 8, anchor: Anchor.MiddleCenter)).Attach<ZagProjectile>();

        _ZagTrailPrefab = VFX.CreateTrailObject("zag_trail_mid", fps: 30, cascadeTimer: C.FRAME, destroyOnEmpty: true);

        _ZagZigVFX = VFX.Create("zag_zig_vfx", fps: 15, loops: false, anchor: Anchor.MiddleCenter);
    }
}

public class ZagProjectile : MonoBehaviour
{
    private const int _MAX_TILE_COLLISIONS = 10;

    private Projectile _projectile;
    private PlayerController _owner;
    private bool _blockedByWall;
    private Vector2 _wallAngle;
    private int _tileCollisionsLeft;
    private SpeculativeRigidbody _body;
    private bool _hasTarget = false;
    private bool _straightened = false;
    private TrailController _trail = null;

    private static readonly Color _ZAG_GRAY = new Color(0.5f, 0.625f, 0.5f, 1f);

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._owner = this._projectile.Owner as PlayerController;
        if (!this._owner)
            return;

        this._body = base.GetComponent<SpeculativeRigidbody>();
        this._blockedByWall = false;
        this._wallAngle = this._projectile.Direction;
        this._projectile.specRigidbody.OnPreRigidbodyCollision += this.OnPreRigidbodyCollision;
        this._projectile.specRigidbody.OnTileCollision += this.OnTileCollision;
        this._projectile.BulletScriptSettings.surviveTileCollisions = true;
        this._projectile.m_usesNormalMoveRegardless = true; // ignore Helix / Orbital bullets
        this._tileCollisionsLeft = _MAX_TILE_COLLISIONS;

        this._trail = this._projectile.AddTrailToProjectileInstance(Zag._ZagTrailPrefab);
        this._trail.gameObject.SetGlowiness(10f);
    }

    private void OnPreRigidbodyCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
    {
        if (otherRigidbody.IsActuallyOubiletteEntranceRoom())
            this._projectile.DieInAir();
    }

    private void OnTileCollision(CollisionData tileCollision)
    {
        if (this._blockedByWall || this._hasTarget || this._tileCollisionsLeft <= 0)
        {
            this._projectile.DieInAir();
            return;
        }
        if ((--this._tileCollisionsLeft) <= 0)
            this._projectile.BulletScriptSettings.surviveTileCollisions = false;
        SpeculativeRigidbody body = tileCollision.MyRigidbody;
        this._wallAngle = -tileCollision.Normal;
        bool clockwise  = (this._wallAngle.ToAngle().RelAngleTo(this._projectile.Direction.ToAngle()) > 0f);
        Vector2 newDir = tileCollision.Normal.Rotate(clockwise ? -90f : 90f);
        PhysicsEngine.PostSliceVelocity = newDir;
        this._blockedByWall = true;
        this._straightened = true;
        DoZigZag(newDir);
    }

    private void DoZigZag(Vector2 newDir)
    {
        Vector2 backwards = -newDir;
        FancyVFX.SpawnUnpooled(
            prefab        : Zag._ZagZigVFX,
            position      : base.transform.position,
            rotation      : backwards.EulerZ(),
            velocity      : 2f * backwards.normalized,
            lifetime      : 0.2f,
            fadeOutTime   : 0.4f);
        this._projectile.SendInDirection(dirVec: newDir, resetDistance: true, updateRotation: true);
        base.gameObject.PlayUnique("zag_zig_sound");
        if (this._trail && base.GetComponent<SpeculativeRigidbody>())
            this._trail.DisconnectFromSpecRigidbody();
        this._trail = this._projectile.AddTrailToProjectileInstance(Zag._ZagTrailPrefab);
        this._trail.gameObject.SetGlowiness(10f);
    }

    private void Reorient()
    {
        if (!this._blockedByWall)
            return;
        if (this._body.IsAgainstWall(this._wallAngle.ToIntVector2(), pixels: 8))
            return;
        this._blockedByWall = false;
        DoZigZag(this._wallAngle);
    }

    private const float _MAX_DIST = 1f;
    private const float _MAX_DIST_SQR = _MAX_DIST * _MAX_DIST;
    private static AIActor FindClosestPerpendicularEnemy(Vector2 ppos, float trajectory, out Vector2 trueIpoint)
    {
        trueIpoint = Vector2.zero;
        if (ppos.GetAbsoluteRoom() is not RoomHandler room)
            return null;
        if (room.GetActiveEnemies(RoomHandler.ActiveEnemyType.All) is not List<AIActor> enemies)
            return null;
        Vector2 trajVector = trajectory.ToVector();
        float closest = _MAX_DIST_SQR;
        AIActor target = null;
        foreach (AIActor enemy in enemies)
        {
            if (!enemy || !enemy.IsHostile(canBeNeutral: true))
                continue;
            Vector2? ipoint = Lazy.PointOrthognalTo(ppos, enemy.CenterPosition, trajVector);
            if (!ipoint.HasValue)
                continue;
            float sqrDist = (ppos - ipoint.Value).sqrMagnitude; // closest orthogonally
            // float sqrDist = (ppos - enemy.CenterPosition).sqrMagnitude; // closest overall (need to adjust _MAX_DIST_SQR if i ever use this)
            if (sqrDist > closest)
                continue;
            if (!ppos.HasLineOfSight(ipoint.Value))
                continue;
            if (!enemy.CenterPosition.HasLineOfSight(ipoint.Value))
                continue;
            closest = sqrDist;
            target = enemy;
            trueIpoint = ipoint.Value;
        }
        return target;
    }

    private void StraightenOut()
    {
        if (this._straightened)
            return;
        float dir = this._projectile.Direction.ToAngle().Clamp360();
        float targetDir = dir.Quantize(90f, VectorConversions.Round);
        float deltaDir = (targetDir - dir).Clamp180();
        float maxRot = 360f * BraveTime.DeltaTime;
        if (Mathf.Abs(deltaDir) < maxRot)
        {
            this._projectile.SendInDirection(targetDir.ToVector(), true, true);
            this._straightened = true;
        }
        else
            this._projectile.SendInDirection((dir + Mathf.Sign(deltaDir) * maxRot).ToVector(), true, true);
    }

    private void Update()
    {
        StraightenOut();
        Reorient();
        if (this._hasTarget || !this._straightened)
            return;
        if (FindClosestPerpendicularEnemy(this._projectile.SafeCenter, this._projectile.Direction.ToAngle(), out Vector2 ipoint) is not AIActor target)
            return;
        this._hasTarget = true;
        this._projectile.specRigidbody.Position = new Position(ipoint);
        this._projectile.specRigidbody.UpdateColliderPositions();
        DoZigZag(target.CenterPosition - ipoint);
    }
}
