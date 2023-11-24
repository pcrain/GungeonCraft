namespace CwaffingTheGungy;

public class Lightwing : AdvancedGunBehavior
{
    public static string ItemName         = "Lightwing";
    public static string SpriteName       = "lightwing";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = "Falcon Captain";
    public static string LongDescription  = "Fires birds that return to the user after hitting an enemy. Shooting an enemy's projectile will destroy it and cause the bird to home in on that enemy with increased speed and damage. When returning to the user, birds will attempt to retrieve an additional projectile belonging to the enemy they hit, restoring additional ammo if successful.";
    public static string Lore             = "Falconry is an art that is on the verge of being lost to time, with this weapon being one gunsmith's attempt to revive the art with a modern coat of paint. Boasting state-of-the-art military artificial intelligence in every projectile, the Lightwing allows its user to experience everything traditional falconry has to offer, except perhaps the bond between falcon and falconer. An improved model with projectiles that exhibit more realistic perching and pooping behaviors is currently under development.";

    internal static tk2dSpriteAnimationClip _NeutralSprite    = null;
    internal static tk2dSpriteAnimationClip _HuntingSprite    = null;
    internal static tk2dSpriteAnimationClip _RetrievingSprite = null;
    internal static tk2dSpriteAnimationClip _ReturningSprite  = null;

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<Lightwing>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.B, gunClass: GunClass.RIFLE, reloadTime: 1.0f, ammo: 120);
            gun.SetAnimationFPS(gun.shootAnimation, 32);
            gun.SetAnimationFPS(gun.reloadAnimation, 30);
            gun.SetMuzzleVFX("muzzle_lightwing", fps: 30, scale: 0.5f, anchor: Anchor.MiddleCenter);
            gun.SetFireAudio("lightwing_fire_sound");
            gun.SetReloadAudio("lightwing_reload_sound");

        gun.DefaultModule.SetAttributes(clipSize: 20, cooldown: 0.28f, shootStyle: ShootStyle.SemiAutomatic);

        _NeutralSprite = AnimateBullet.CreateProjectileAnimation(
            ResMap.Get("lightwing_projectile").Base(),
            12, true, new IntVector2(23, 32),
            false, Anchor.MiddleLeft, true, true);
        _HuntingSprite = AnimateBullet.CreateProjectileAnimation(
            ResMap.Get("lightwing_projectile_hunt").Base(),
            12, true, new IntVector2(23, 32),
            false, Anchor.MiddleLeft, true, true);
        _RetrievingSprite = AnimateBullet.CreateProjectileAnimation(
            ResMap.Get("lightwing_projectile_retrieve").Base(),
            12, true, new IntVector2(23, 32),
            false, Anchor.MiddleLeft, true, true);
        _ReturningSprite = AnimateBullet.CreateProjectileAnimation(
            ResMap.Get("lightwing_projectile_return").Base(),
            12, true, new IntVector2(23, 32),
            false, Anchor.MiddleLeft, true, true);

        Projectile projectile = Lazy.PrefabProjectileFromGun(gun);
            projectile.AddDefaultAnimation(_NeutralSprite);
            projectile.AddAnimation(_HuntingSprite);
            projectile.AddAnimation(_RetrievingSprite);
            projectile.AddAnimation(_ReturningSprite);
            projectile.transform.parent        = gun.barrelOffset;
            projectile.collidesWithProjectiles = true;  // needs to be set up front, can't be set later because caching silliness or something idk
            projectile.baseData.damage         = 4f;
            projectile.baseData.speed          = 20f;

            projectile.gameObject.AddComponent<LightwingProjectile>();
    }
}

public class LightwingProjectile : MonoBehaviour
{
    private enum State {
        NEUTRAL,    // default state after firing
        HUNTING,    // we hit an enemy projectile and now we're seeking that enemy
        RETRIEVING, // we hit an enemy and now we're seeking one of their projectiles
        RETURNING,  // we're returning to the player after retrieving a projectile (or failing to find one)
    }

    internal const float _START_SPEED       = 10f;
    internal const float _MAX_TURN_RATE     = 16f;
    internal const float _TURN_FRICTION     = 0.98f;
    internal const float _BASE_ACCEL        = 1.0f;
    internal const float _HUNT_ACCEL        = 2.0f;
    internal const float _HUNT_SPEED_SCALE  = 2.0f;
    internal const float _HUNT_DAMAGE_SCALE = 2.0f;

    private Projectile _projectile       = null;
    private PlayerController _owner      = null;
    private Gun _gun                     = null;
    private GameActor _target            = null;
    private Projectile _targetProjectile = null;
    private EasyTrailBullet _trail       = null;
    private float _topSpeed              = 0f;
    private bool _retrievedAmmo          = false;
    private State _state_internal        = State.NEUTRAL;
    private State _state
    {
        get
        {
            return this._state_internal;
        }
        set
        {
            this._state_internal = value;
            if (!this._projectile)
                return;

            Color newTrailColor = Color.white;
            switch(value)
            {
                case State.NEUTRAL:
                    this._projectile.spriteAnimator.Play(Lightwing._NeutralSprite);
                    newTrailColor = Color.white;
                    break;
                case State.HUNTING:
                    this._projectile.spriteAnimator.Play(Lightwing._HuntingSprite);
                    newTrailColor = Color.red;
                    break;
                case State.RETRIEVING:
                    this._projectile.spriteAnimator.Play(Lightwing._RetrievingSprite);
                    newTrailColor = ExtendedColours.purple;
                    break;
                case State.RETURNING:
                    this._projectile.spriteAnimator.Play(Lightwing._ReturningSprite);
                    newTrailColor = Color.blue;
                    break;
            }

            if (!this._trail)
                return;

            this._trail.BaseColor  = newTrailColor;
            this._trail.StartColor = newTrailColor;
            this._trail.EndColor   = Color.Lerp(newTrailColor, Color.white, 0.75f);
            this._trail.UpdateTrail();
        }
    }

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._owner = this._projectile.Owner as PlayerController;
        if (this._owner.CurrentGun.GetComponent<Lightwing>() is Lightwing lightwing)
            this._gun = this._owner.CurrentGun;

        this._topSpeed = this._projectile.baseData.speed;
        this._projectile.baseData.speed = _START_SPEED;
        this._projectile.BulletScriptSettings.surviveRigidbodyCollisions = true;
        this._projectile.specRigidbody.OnCollision += this.OnCollision;
        this._projectile.specRigidbody.OnPreRigidbodyCollision += this.OnPreCollision;

        this._trail = this._projectile.gameObject.AddComponent<EasyTrailBullet>();
            this._trail.StartWidth = 0.35f;
            this._trail.EndWidth   = 0.05f;
            this._trail.LifeTime   = 0.10f;
            this._trail.StartColor = Color.white;
            this._trail.BaseColor  = Color.white;
            this._trail.EndColor   = Color.white;
    }

    private void Update()
    {
        Vector2 targetPos = Vector2.zero;

        bool haveTarget = true;
        switch(this._state)
        {
            case State.NEUTRAL:
                haveTarget = false;
                break;  // nothing to do
            case State.HUNTING:
                if (this._target)
                    targetPos = this._target.CenterPosition;
                else if (this._owner)
                {
                    this._target = this._owner;
                    targetPos = this._target.CenterPosition;
                    this._state  = State.RETURNING;
                }
                else
                    haveTarget = false;
                break;
            case State.RETURNING:
                if (this._target)
                    targetPos = this._target.CenterPosition;
                else
                    haveTarget = false;
                break;
            case State.RETRIEVING:
                if (!this._targetProjectile || !this._targetProjectile.isActiveAndEnabled || !(this._targetProjectile.sprite?.renderer.enabled ?? false))
                {
                    this._targetProjectile = null;
                    // first try to find a projectile to latch onto
                    float closestSquareDistance = 9999f;
                    foreach (Projectile p in StaticReferenceManager.AllProjectiles)
                    {
                        if (!p.isActiveAndEnabled)
                            continue;
                        if (p.Owner != this._target)
                            continue;
                        float sqrDistance = (p.transform.position - this._projectile.transform.position).sqrMagnitude;
                        if (sqrDistance > closestSquareDistance)
                            continue;
                        closestSquareDistance = sqrDistance;
                        this._targetProjectile = p;
                    }
                    // if we still can't find a projectile to target, give up and return to player empty handed
                    if (this._targetProjectile == null)
                    {
                        this._target = this._owner;
                        this._state  = State.RETURNING;
                        return;
                    }
                }
                targetPos = this._targetProjectile.sprite?.WorldCenter ?? this._targetProjectile.transform.position.XY();
                break;
        }

        float relTime = C.FPS * BraveTime.DeltaTime;
        if (haveTarget)
        {
            Vector2 targetDir  = targetPos - this._projectile.sprite?.WorldCenter ?? this._projectile.transform.position.XY();
            if (this._state == State.RETURNING && targetDir.sqrMagnitude < 1f)
            {
                DissipateNearPlayer();
                return;
            }
            Vector2 currentDir = this._projectile.m_currentDirection;

            float turnRate      = _MAX_TURN_RATE * relTime;
            float curAngle      = currentDir.ToAngle();
            float angleDelta    = (curAngle.RelAngleTo(targetDir.ToAngle()));
            if (Mathf.Abs(angleDelta) < turnRate)
                this._projectile.SendInDirection(targetDir, true);
            else
            {
                this._projectile.baseData.speed *= (_TURN_FRICTION * relTime);
                this._projectile.SendInDirection((curAngle + turnRate * Mathf.Sign(angleDelta)).ToVector(), true);
            }
        }
        this._projectile.baseData.speed += ((this._state == State.NEUTRAL ? _BASE_ACCEL : _HUNT_ACCEL) * relTime);
        this._projectile.UpdateSpeed();
    }

    private void OnCollision(CollisionData collision)
    {
        if (collision.OtherRigidbody?.gameObject is not GameObject other)
            return;

        if (other.GetComponent<Projectile>() is Projectile projectile)
            OnProjectileCollision(projectile);
        else if (other.GetComponent<AIActor>() is AIActor enemy)
            OnEnemyCollision(enemy);
    }

    private void OnPreCollision(SpeculativeRigidbody me, PixelCollider myPixelCollider, SpeculativeRigidbody other, PixelCollider otherPixelCollider)
    {
        if (other.GetComponent<Projectile>() is not Projectile projectile)
            return;

        switch(this._state)
        {
            case State.NEUTRAL:
                if (projectile.Owner is not AIActor enemy)
                    PhysicsEngine.SkipCollision = true; // only collides with enemy projectiles in neutral mode
                return;
            case State.RETRIEVING:
                PhysicsEngine.SkipCollision = projectile.Owner != this._target; // only collides with target's projectiles in retrieval mode
                return;
            case State.HUNTING:
            case State.RETURNING:
               PhysicsEngine.SkipCollision = true; // don't collide with any projectiles when hunting or returning
               return;
        }
    }

    private void OnProjectileCollision(Projectile projectile)
    {
        if (this._state != State.NEUTRAL && this._state != State.RETRIEVING)
            return;
        if (projectile.Owner is not AIActor enemy)
            return;
        if (this._state == State.RETRIEVING && this._target != enemy)
            return; // we collided with a projectile that wasn't owned by the enemy we were originally targeting

        projectile.DieInAir(allowActorSpawns: false, allowProjectileSpawns: false, killedEarly: true);
        this._topSpeed                           *= _HUNT_SPEED_SCALE;
        this._projectile.baseData.damage         *= _HUNT_DAMAGE_SCALE;
        this._projectile.collidesWithProjectiles  = false;

        if (this._state == State.NEUTRAL)
        {
            AkSoundEngine.PostEvent("lightwing_hunt_sound", base.gameObject);
            this._target = enemy;
            this._state  = State.HUNTING;
        }
        else
        {
            this._retrievedAmmo = true;
            this._target        = this._owner;
            this._state         = State.RETURNING;
        }
    }

    private void OnEnemyCollision(AIActor enemy)
    {
        switch(this._state)
        {
            case State.NEUTRAL:
            case State.HUNTING:
                AkSoundEngine.PostEvent("lightwing_impact_sound", base.gameObject);
                this._target                         = enemy;
                this._projectile.collidesWithEnemies = false;
                this._state                          = State.RETRIEVING;
                return;
            case State.RETRIEVING:
            case State.RETURNING:
                return; // theoretically can't happen
        }
    }

    private void DissipateNearPlayer()
    {
        if (this._gun)
            this._gun.GainAmmo(this._retrievedAmmo ? 2 : 1);
        this._projectile.DieInAir();
        UnityEngine.Object.Destroy(this);
    }
}
