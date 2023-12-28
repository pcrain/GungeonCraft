namespace CwaffingTheGungy;

public class TeslaCannon : AdvancedGunBehavior
{
    public static string ItemName         = "Tesla Cannon";
    public static string SpriteName       = "tesla_cannon";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    internal static GameObject _DrowsyVFX = null;

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<TeslaCannon>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.B, gunClass: GunClass.RIFLE, reloadTime: 1.0f, ammo: 800);
            gun.SetAnimationFPS(gun.shootAnimation, 30);
            gun.SetAnimationFPS(gun.reloadAnimation, 40);
            gun.SetMuzzleVFX(Items.Mailbox); // innocuous muzzle flash effects
            gun.SetFireAudio("fire_coin_sound");

        gun.InitProjectile(new(clipSize: 10, cooldown: 0.2f, shootStyle: ShootStyle.Automatic, range: 999999f, speed: 60f,
          sprite: "tesla_projectile", scale: 0.5f, fps: 12, anchor: Anchor.MiddleCenter)).Attach<TeslaProjectileBehavior>();
    }
}

public class TeslaProjectileBehavior : MonoBehaviour
{
    private const float _ACCEL      = 500.0f;
    private const float _MIN_SPEED  =  60.0f;
    private const float _MAX_SPEED  =  90.0f;
    private const float _PRECESSION =   1.0f; // speed at which projectiles change their angle of orbit around the player

    private static readonly Color _TrailColor = new Color(0.35f, 1.0f, 1.0f, 0.45f);
    private Projectile _projectile;
    private PlayerController _owner;
    private float _mySpeed;

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._owner      = this._projectile.Owner as PlayerController;
        this._mySpeed    = UnityEngine.Random.Range(_MIN_SPEED, _MAX_SPEED);

        this._projectile.specRigidbody.CollideWithTileMap = false;
        this._projectile.specRigidbody.OnPreRigidbodyCollision += this.OnPreCollision;
        EasyTrailBullet trail = this._projectile.AddComponent<EasyTrailBullet>();
            trail.StartWidth = 0.1f;
            trail.EndWidth   = 0f;
            trail.LifeTime   = 0.15f;
            trail.BaseColor  = _TrailColor;
            trail.StartColor = _TrailColor;
            trail.EndColor   = _TrailColor;
    }

    private void OnPreCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
    {
        if (!otherRigidbody.GetComponent<AIActor>())
            PhysicsEngine.SkipCollision = true;
    }

    private void Update()
    {
        if (!this._owner || !this._projectile)
        {
            UnityEngine.Object.Destroy(this);
            return;
        }

        Vector2 oldVel    = this._projectile.m_currentSpeed * this._projectile.m_currentDirection;
        Vector2 playerVec = (this._owner.CenterPosition - this._projectile.transform.position.XY());
        Vector2 newVel    = oldVel + _ACCEL * BraveTime.DeltaTime * playerVec.normalized.Rotate(_PRECESSION);

        this._projectile.baseData.speed = Mathf.Min(this._mySpeed, newVel.magnitude);
        this._projectile.SendInDirection(newVel, true);
        this._projectile.UpdateSpeed();
    }
}
