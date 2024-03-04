namespace CwaffingTheGungy;

public class Wavefront : AdvancedGunBehavior
{
    public static string ItemName         = "Wavefront";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = "Particle-larly Interesting";
    public static string LongDescription  = "Fires bullets that persistently orbit and gravitate towards the player for up to 30 seconds until they collide with an enemy.";
    public static string Lore             = "The primary difficulty of working with projectiles that gravitate towards you is, hopefully unsurprisingly, the fact that those projectiles can hit you. The Gungineer in charge of redesigning this gun to meet modern safety standards came up with a rather ingenious workaround for this issue: do nothing, but claim that you have incorporated proprietary technology that reduces the likelihood of shooting yourself so people will buy it anyway. The redesigned gun received 100% approval from those who survived using it, and the Gungineer received an employee of the year award from management shortly after the redesign went live.";

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<Wavefront>(ItemName, ProjectileName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.A, gunClass: GunClass.RIFLE, reloadTime: 1.0f, ammo: 320);
            gun.SetAnimationFPS(gun.idleAnimation, 12);
            gun.SetAnimationFPS(gun.shootAnimation, 50);
            gun.SetAnimationFPS(gun.reloadAnimation, 16);
            gun.SetMuzzleVFX("muzzle_wavefront", fps: 30, scale: 1.0f, anchor: Anchor.MiddleCenter, emissivePower: 10f);
            gun.SetFireAudio("wavefront_fire_sound");
            gun.SetReloadAudio("wavefront_reload_sound", 0, 6, 12, 18);

        gun.InitProjectile(GunData.New(clipSize: 8, cooldown: 0.125f, shootStyle: ShootStyle.Automatic, range: 999999f, speed: 60f, shouldRotate: true,
          customClip: true, sprite: "wavefront_projectile_alt", scale: 0.25f, fps: 24, anchor: Anchor.MiddleCenter)
        ).SetEnemyImpactVFX(VFX.CreatePool("wavefront_impact_particles", fps: 24, loops: false, anchor: Anchor.MiddleCenter, scale: 0.5f, emissivePower: 2f)
        ).Attach<TeslaProjectileBehavior>();
    }
}

public class TeslaProjectileBehavior : MonoBehaviour
{
    private const float _LIFESPAN   =  30.0f; // projectiles dissipate after 30 seconds
    private const float _ACCEL      = 500.0f;
    private const float _MIN_SPEED  =  60.0f;
    private const float _MAX_SPEED  =  90.0f;
    private const float _PRECESSION =   1.0f; // speed at which projectiles change their angle of orbit around the player

    private static readonly Color _TrailColor = new Color(0.35f, 1.0f, 1.0f, 0.35f);
    private Projectile _projectile;
    private PlayerController _owner;
    private float _myMaxSpeed;
    private float _lifespan;

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._owner      = this._projectile.Owner as PlayerController;
        this._myMaxSpeed = UnityEngine.Random.Range(_MIN_SPEED, _MAX_SPEED);
        this._lifespan   = _LIFESPAN;

        this._projectile.specRigidbody.CollideWithTileMap = false;
        this._projectile.specRigidbody.OnPreRigidbodyCollision += this.OnPreCollision;
        EasyTrailBullet trail = this._projectile.AddComponent<EasyTrailBullet>();
            trail.StartWidth = 0.1f;
            trail.EndWidth   = 0f;
            trail.LifeTime   = 0.15f;
            trail.BaseColor  = _TrailColor;
            trail.StartColor = _TrailColor;
            trail.EndColor   = _TrailColor;

        this._projectile.sprite.SetGlowiness(glowAmount: 1f, glowColor: Color.cyan);
    }

    private void OnPreCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
    {
        if (!otherRigidbody.GetComponent<AIActor>())
            PhysicsEngine.SkipCollision = true;
        else
            base.gameObject.Play("wavefront_projectile_impact_sound");
    }

    private void Update()
    {
        if (!this._owner || !this._projectile)
        {
            UnityEngine.Object.Destroy(this);
            return;
        }

        if ((this._lifespan -= BraveTime.DeltaTime) <= 0.0f)
        {
            this._projectile.DieInAir();
            return;
        }

        this._projectile.OverrideMotionModule = null; // get rid of Helix Bullets and the like

        Vector2 oldVel    = this._projectile.m_currentSpeed * this._projectile.m_currentDirection;
        Vector2 playerVec = (this._owner.CenterPosition - this._projectile.transform.position.XY());
        Vector2 newVel    = oldVel + _ACCEL * BraveTime.DeltaTime * playerVec.normalized.Rotate(_PRECESSION);

        this._projectile.SetSpeed(Mathf.Min(this._myMaxSpeed, newVel.magnitude));
        this._projectile.SendInDirection(newVel, true);
    }
}
