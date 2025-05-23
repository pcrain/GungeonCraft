﻿namespace CwaffingTheGungy;

public class Widowmaker : CwaffGun
{
    public static string ItemName         = "Widowmaker";
    public static string ShortDescription = "Itsy Bitsy Turrets";
    public static string LongDescription  = "Fires pods that deploy spider drones upon colliding with a wall. Drones will crawl along walls and shoot enemies within their range.";
    public static string Lore             = "Much like the black widow is one of the deadliest known spiders on earth, the Widowmaker is one of the deadliest known firearms with spider-based projectiles. The first model of the weapon failed every safety test thrown at it, while the second model ended up permanently dismembering its own creator. The penultimate model had ironed out all issues besides a slight gunpowder leak and the drones becoming  sentient and conspiring against humanity, but fortunately, these were fixed in the fourteenth and final model.";

    private const float _SCALE = 0.75f;

    internal static GameObject _WidowmakerPrefab = null;
    internal static Projectile _WidowTurretProjectile = null;
    internal static Projectile _WidowTurretLaser = null;

    public static void Init()
    {
        Lazy.SetupGun<Widowmaker>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.B, gunClass: GunClass.RIFLE, reloadTime: 1.4f, ammo: 160, shootFps: 20, reloadFps: 12,
            fireAudio: "widowmaker_fire_sound", smoothReload: 0.1f)
          .SetReloadAudio("widowmaker_reload_sound", 0, 4, 8, 10, 12, 14)
          .InitProjectile(GunData.New(clipSize: 5, cooldown: 0.18f, shootStyle: ShootStyle.SemiAutomatic, damage: 3.5f, pierceBreakables: true,
            sprite: "widowmaker_projectile", fps: 12, scale: _SCALE, anchor: Anchor.MiddleLeft, preventOrbiting: true, customClip: true))
          .Attach<WidowmakerProjectile>();

        _WidowmakerPrefab = VFX.Create("spider_turret", fps: 16, scale: _SCALE, emissivePower: 1f);
        _WidowmakerPrefab.AddAnimation("deploy", "widowmaker_deploy_vfx", fps: 12, loops: false, emissivePower: 1f);
        _WidowmakerPrefab.AutoRigidBody(anchor: Anchor.MiddleCenter, clayer: CollisionLayer.Projectile);
        _WidowmakerPrefab.AddComponent<Crawlyboi>();

        _WidowTurretProjectile = Items.Ak47.CloneProjectile(GunData.New(damage: 10.0f, speed: 50.0f, force: 10.0f, range: 80.0f,
            spawnSound: "widowmaker_turret_shoot_sound", uniqueSounds: true))
          .Attach<EasyTrailBullet>(trail => {
            trail.TrailPos   = trail.transform.position;
            trail.StartWidth = 0.3f;
            trail.EndWidth   = 0.05f;
            trail.LifeTime   = 0.07f;
            trail.BaseColor  = ExtendedColours.paleYellow;
            trail.EndColor   = Color.Lerp(ExtendedColours.paleYellow, Color.white, 0.25f);
          });

        _WidowTurretLaser = Items.Ak47.CloneProjectile(GunData.New(sprite: "widowmaker_laser_projectile", angleVariance: 0.0f,
            speed: 200f, damage: 12f, spawnSound: "widowmaker_laser_sound", customClip: true, shouldRotate: true,
            pierceBreakables: true));
    }
}

public class WidowmakerProjectile : MonoBehaviour
{
    private const float _MIN_TRAVEL_TIME = 0.02f; // if we fire projectile directly inside a wall, our crawlers can get stuck and cause issues

    private Projectile _projectile;
    private PlayerController _owner;
    private float _startTime;

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._owner = this._projectile.Owner as PlayerController;
        SpeculativeRigidbody body = base.GetComponent<SpeculativeRigidbody>();
        this._startTime = BraveTime.ScaledTimeSinceStartup;
        body.OnTileCollision += this.OnTileCollision;
    }

    private void OnTileCollision(CollisionData tileCollision)
    {
        if ((BraveTime.ScaledTimeSinceStartup - this._startTime) < _MIN_TRAVEL_TIME)
            return; // we were probably fired while inside a wall and all sorts of jank can happen, so don't let it

        //NOTE: quantization needed or SpeculativeRigidBody rounding math doesn't push out of walls correctly
        UnityEngine.Object.Instantiate(Widowmaker._WidowmakerPrefab, tileCollision.Contact.Quantize(0.0625f), Quaternion.identity)
            .GetComponent<Crawlyboi>().Setup(this._owner, tileCollision.Normal, this._projectile.specRigidbody.Velocity, this._projectile.baseData.damage);
        this._projectile.DieInAir();
    }
}

public class Crawlyboi : MonoBehaviour
{
    private const float _CRAWL_SPEED          = 10f;
    private const float _SHOOT_TIMER          = 0.4f;
    private const float _SHOOT_TIMER_MASTERED = 0.25f;
    private const float _SOUND_TIMER          = 0.18f;
    private const float _EXPIRE_TIMER         = 8f;
    private const float _SIGHT_CONE           = 90f; // 180-degree cone
    private const float _SIGHT_DIST           = 12f;
    private const float _STUCK_TIME           = 0.15f; // amount of time we can go without moving until we're considered stuck

    private SpeculativeRigidbody _body;
    private tk2dSprite _sprite;
    private Vector2 _velocity;
    private Vector2 _wallNormal;
    private bool _deployed = false;
    private float _shootTimer = 0.0f;
    private float _soundTimer = 0.0f;
    private float _expireTimer = 0.0f;
    private PlayerController _owner;
    private float _rotateDir;
    private float _damage;
    private bool _oddStep;
    private Vector3 _lastPosition;
    private float _stuckTime = 0f;
    private bool _mastered = false;
    private float _shootRate;

    public void Setup(PlayerController owner, Vector2 wallNormal, Vector2 projVelocity, float damage)
    {
        bool clockwise  = ((-wallNormal).ToAngle().RelAngleTo(projVelocity.ToAngle()) > 0f);
        this._rotateDir = clockwise ? -90f : 90f;

        this._owner      = owner;
        this._mastered   = owner && owner.HasSynergy(Synergy.MASTERY_WIDOWMAKER);
        this._damage     = damage;
        this._shootRate  = this._mastered ? _SHOOT_TIMER_MASTERED : _SHOOT_TIMER;
        this._wallNormal = wallNormal;
        this._velocity   = wallNormal.Rotate(this._rotateDir);

        this._sprite = base.GetComponent<tk2dSprite>();
        this._sprite.HeightOffGround = 3f;
        this._sprite.transform.rotation = this._wallNormal.EulerZ();
        this._sprite.UpdateZDepth();

        this._body = base.GetComponent<SpeculativeRigidbody>();
        this._body.transform.position = base.transform.position;
        this._body.Reinitialize();
        this._body.PullOutOfWall(wallNormal.ToIntVector2());
        this._body.OnTileCollision         += this.OnTileCollision;
        this._body.OnPreRigidbodyCollision += this.OnPreRigidbodyCollision;
        this._body.OnRigidbodyCollision    += this.OnRigidbodyCollision;
        this._body.OnPostRigidbodyMovement += this.OnPostRigidbodyMovement;

    }

    private void Start()
    {
        base.GetComponent<tk2dSpriteAnimator>().Play("deploy");
        base.gameObject.Play("widowmaker_deploy_sound_alt");
    }

    private void Update()
    {
        if (BraveTime.DeltaTime == 0.0f)
            return;

        this._sprite.transform.rotation = this._wallNormal.EulerZ();

        if (!this._deployed)
        {
            if (base.GetComponent<tk2dSpriteAnimator>().IsPlaying("deploy"))
                return;
            base.GetComponent<tk2dSpriteAnimator>().Play("start");
            this._body.Velocity = _CRAWL_SPEED * this._velocity;
            this._deployed = true;
        }

        if (base.transform.position == this._lastPosition)
        {
            if ((this._stuckTime += BraveTime.DeltaTime) >= _STUCK_TIME)
            {
                Explode();
                return;
            }
        }
        else
        {
            this._stuckTime = 0f;
            // NOTE: needs to be inside else due to Unity's approximate equality check for vectors causing issues:
            // see: https://youtu.be/Ydg2ApPouOg?t=206
            this._lastPosition = base.transform.position;
        }

        if ((this._expireTimer += BraveTime.DeltaTime) >= _EXPIRE_TIMER)
        {
            Explode();
            return;
        }
        if ((this._soundTimer += BraveTime.DeltaTime) > _SOUND_TIMER)
        {
            this._soundTimer = 0.0f;
            base.gameObject.PlayUnique(_oddStep ? "widowmaker_turret_crawl_sound_2" : "widowmaker_turret_crawl_sound");
            this._oddStep = !this._oddStep;
        }
        if ((this._shootTimer += BraveTime.DeltaTime) < this._shootRate)
            return;

        Vector2 shootPoint = base.transform.position.XY() + this._wallNormal;
        Vector2? enemyPos = Lazy.NearestEnemyPosWithinConeOfVision(
            start                            : shootPoint,
            coneAngle                        : this._wallNormal.ToAngle(),
            maxDeviation                     : _SIGHT_CONE,
            maxDistance                      : _SIGHT_DIST,
            useNearestAngleInsteadOfDistance : true);
        if (!enemyPos.HasValue)
            return;

        Projectile proj = SpawnManager.SpawnProjectile(
            prefab   : (this._mastered ? Widowmaker._WidowTurretLaser : Widowmaker._WidowTurretProjectile).gameObject,
            position : shootPoint,
            rotation : (enemyPos.Value - shootPoint).EulerZ()).GetComponent<Projectile>();
        proj.baseData.damage     = this._damage;
        proj.SetOwnerAndStats(this._owner);
        this._owner.DoPostProcessProjectile(proj);
        if (this._mastered)
            proj.AddTrail(OmnidirectionalLaser._OmniTrailMasteredPrefab).gameObject.SetGlowiness(10f);

        this._shootTimer = 0f;
    }

    private void Explode()
    {
        Exploder.Explode(base.transform.position, Scotsman._ScotsmanExplosion, Vector2.zero, ignoreQueues: true);
        UnityEngine.Object.Destroy(base.gameObject);
    }

    private void OnPreRigidbodyCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
    {
        if ((otherRigidbody.transform.parent && otherRigidbody.transform.parent.GetComponent<DungeonDoorController>() is DungeonDoorController ddc))
            return; // do not skip collision

        PhysicsEngine.SkipCollision = true;
        if (otherRigidbody.IsActuallyOubiletteEntranceRoom() || (otherRigidbody.GetComponent<MajorBreakable>() || otherRigidbody.GetComponent<AIActor>() || !this._body.IsAgainstWall(-this._wallNormal.ToIntVector2())))
            Explode();
    }

    /// <summary>Reverse direction when colliding with unknown objects</summary>
    private void OnRigidbodyCollision(CollisionData rigidbodyCollision)
    {
        if (!this._body.IsAgainstWall(-this._wallNormal.ToIntVector2()))
        {
            Explode();
            return;
        }
        this._velocity = -this._velocity;
        PhysicsEngine.PostSliceVelocity = _CRAWL_SPEED * this._velocity;
    }

    /// <summary>wrap along inner walls</summary>
    private void OnTileCollision(CollisionData tileCollision)
    {
        this._velocity = this._wallNormal.normalized;
        this._wallNormal = tileCollision.Normal;
        PhysicsEngine.PostSliceVelocity = _CRAWL_SPEED * this._velocity;
    }

    /// <summary>wrap along outer walls, if necessary</summary>
    private void OnPostRigidbodyMovement(SpeculativeRigidbody specRigidbody, Vector2 unitDelta, IntVector2 pixelDelta)
    {
        if (!this._deployed)
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
        this._body.Velocity = _CRAWL_SPEED * this._velocity;
    }
}
