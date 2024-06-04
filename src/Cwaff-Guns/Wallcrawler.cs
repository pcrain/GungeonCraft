namespace CwaffingTheGungy;

public class Wallcrawler : CwaffGun
{
    public static string ItemName         = "Wallcrawler";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    internal static GameObject _WallCrawlerPrefab = null;

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<Wallcrawler>(ItemName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.B, gunClass: GunClass.PISTOL, reloadTime: 0.9f, ammo: 320, shootFps: 14, reloadFps: 4);

        Projectile p = gun.InitProjectile(GunData.New(clipSize: 8, cooldown: 0.1f, shootStyle: ShootStyle.SemiAutomatic, damage: 6.5f,
          sprite: "wallcrawler_projectile", fps: 12, anchor: Anchor.MiddleLeft)).Attach<WallcrawlerProjectile>();
            p.pierceMinorBreakables = true;

        _WallCrawlerPrefab = VFX.Create("spider_turret", fps: 16, loops: true, scale: 0.75f, anchor: Anchor.MiddleCenter, emissivePower: 1f);
        _WallCrawlerPrefab.AutoRigidBody(anchor: Anchor.MiddleCenter, clayer: CollisionLayer.Projectile);
        _WallCrawlerPrefab.AddComponent<Crawlyboi>();
    }
}


public class WallcrawlerProjectile : MonoBehaviour
{
    private Projectile _projectile;
    private PlayerController _owner;
    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._owner = this._projectile.Owner as PlayerController;
        SpeculativeRigidbody body = base.GetComponent<SpeculativeRigidbody>();
        body.OnTileCollision += this.OnTileCollision;
    }

    private void OnTileCollision(CollisionData tileCollision)
    {
        //NOTE: quantization needed or SpeculativeRigidBody rounding math doesn't push out of walls correctly
        GameObject go = UnityEngine.Object.Instantiate(Wallcrawler._WallCrawlerPrefab, tileCollision.Contact.Quantize(0.0625f), Quaternion.identity);
        go.GetComponent<Crawlyboi>().Setup(this._owner, tileCollision.Normal, this._projectile.specRigidbody.Velocity, this._projectile.baseData.damage);
        this._projectile.DieInAir();
    }
}

public class Crawlyboi : MonoBehaviour
{
    private const float _SPEED        = 10f;
    private const float _SHOOT_TIMER  = 0.45f;
    private const float _EXPIRE_TIMER = 8f;
    private const float _SIGHT_CONE   = 40f; // 80-degree cone

    private SpeculativeRigidbody _body;
    private tk2dSprite _sprite;
    private Vector2 _velocity;
    private Vector2 _wallNormal;
    private bool _hitWall = false;
    private float _shootTimer = 0.0f;
    private float _expireTimer = 0.0f;
    private PlayerController _owner;
    private float _rotateDir;
    private float _damage;

    public void Setup(PlayerController owner, Vector2 normal, Vector2 projVelocity, float damage)
    {
        if (this._hitWall)
            return;

        bool clockwise  = ((-normal).ToAngle().RelAngleTo(projVelocity.ToAngle()) < 0f);
        this._rotateDir = clockwise ? 90f : -90f;

        this._hitWall    = true;
        this._owner      = owner;
        this._damage     = damage;
        this._wallNormal = normal;
        this._velocity   = normal.Rotate(this._rotateDir);

        this._sprite = base.GetComponent<tk2dSprite>();
        this._sprite.HeightOffGround = 3f;
        this._sprite.UpdateZDepth();

        this._body = base.GetComponent<SpeculativeRigidbody>();
        this._body.transform.position = base.transform.position;
        this._body.Velocity = _SPEED * this._velocity;
        this._body.Reinitialize();
        IntVector2 intNormal = normal.ToIntVector2();
        this._body.PushAgainstWalls(-intNormal);
        this._body.PullOutOfWall(intNormal);
        this._body.OnTileCollision         += this.OnTileCollision;
        this._body.OnPreRigidbodyCollision += this.OnPreRigidbodyCollision;
        this._body.OnPostRigidbodyMovement += this.OnPostRigidbodyMovement;
    }

    private void Update()
    {
        this._sprite.transform.rotation = this._wallNormal.EulerZ();
        if ((this._expireTimer += BraveTime.DeltaTime) >= _EXPIRE_TIMER)
        {
            Explode();
            return;
        }
        if ((this._shootTimer += BraveTime.DeltaTime) < _SHOOT_TIMER)
            return;

        Vector2 shootPoint = base.transform.position.XY() + this._wallNormal;
        Vector2? enemyPos = Lazy.NearestEnemyWithinConeOfVision(shootPoint, this._wallNormal.ToAngle(), _SIGHT_CONE, useNearestAngleInsteadOfDistance: true);
        if (!enemyPos.HasValue)
            return;

        this._shootTimer = 0f;

        Quaternion aimDir = (enemyPos.HasValue ? (enemyPos.Value - shootPoint) : this._wallNormal).EulerZ();

        Projectile proj = SpawnManager.SpawnProjectile(
            prefab   : PistolWhip._PistolWhipProjectile.gameObject,
            position : shootPoint,
            rotation : aimDir).GetComponent<Projectile>();

        proj.collidesWithPlayer  = false;
        proj.collidesWithEnemies = true;
        proj.baseData.damage     = this._damage;
        proj.Owner               = this._owner;
        proj.Shooter             = this._owner.specRigidbody;

        proj.SetSpeed(50f);
        this._owner.gameObject.PlayOnce("chess_gun_fire");
    }

    private void Explode()
    {
        Exploder.Explode(base.transform.position, Scotsman._ScotsmanExplosion, Vector2.zero, ignoreQueues: true);
        UnityEngine.Object.Destroy(base.gameObject);
    }

    private void OnPreRigidbodyCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
    {
        PhysicsEngine.SkipCollision = true;
        if (!otherRigidbody)
            return;
        if (otherRigidbody.GetComponent<AIActor>() ||
            otherRigidbody.GetComponent<MajorBreakable>() ||
            (otherRigidbody.transform.parent && otherRigidbody.transform.parent.GetComponent<DungeonDoorController>()))
            Explode();
    }

    private void OnTileCollision(CollisionData tileCollision)
    {
        Vector2 oldNormal = this._wallNormal;
        if (oldNormal == Vector2.zero)
            oldNormal = tileCollision.Normal.Rotate(this._rotateDir);
        this._wallNormal = tileCollision.Normal;
        this._velocity = oldNormal.normalized;
        PhysicsEngine.PostSliceVelocity = _SPEED * this._velocity;
    }

    private void OnPostRigidbodyMovement(SpeculativeRigidbody specRigidbody, Vector2 unitDelta, IntVector2 pixelDelta)
    {
        if (!this._hitWall)
            return; // don't do anything until we've hit a wall once
        if (this._body.IsAgainstWall(-this._wallNormal.ToIntVector2()))
            return; // if we're already up against a wall, no adjustments are needed
        // move slightly towards the direction we're supposed to be going
        Vector2 newVelocity = -this._wallNormal.normalized;
        this._body.transform.position += (C.PIXEL_SIZE * newVelocity).ToVector3ZUp(0f);
        this._body.Reinitialize();
        // snap to the wall in the opposite direction we've overshot from
        int pixelsAdjusted = this._body.PushAgainstWalls(-this._velocity.normalized.ToIntVector2());
        // set the new normal to our current velocity
        this._wallNormal = this._velocity.normalized;
        // set the new velocity
        this._velocity = newVelocity;
        this._body.Velocity = _SPEED * this._velocity;
    }
}
