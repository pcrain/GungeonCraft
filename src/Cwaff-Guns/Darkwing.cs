namespace CwaffingTheGungy;

public class Darkwing : AdvancedGunBehavior
{
    public static string ItemName         = "Darkwing";
    public static string SpriteName       = "darkwing";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";

    internal static tk2dSpriteAnimationClip _NeutralSprite;
    internal static tk2dSpriteAnimationClip _HuntingSprite;
    internal static tk2dSpriteAnimationClip _RetrievingSprite;
    internal static tk2dSpriteAnimationClip _ReturningSprite;

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<Darkwing>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription);
            gun.SetAttributes(quality: PickupObject.ItemQuality.B, gunClass: GunClass.RIFLE, reloadTime: 1.0f, ammo: 80);
            gun.SetAnimationFPS(gun.shootAnimation, 30);
            gun.SetAnimationFPS(gun.reloadAnimation, 40);
            gun.SetMuzzleVFX(Items.Mailbox); // innocuous muzzle flash effects
            gun.SetFireAudio("soul_kaliber_fire");
            gun.SetReloadAudio("soul_kaliber_reload");

        ProjectileModule mod = gun.DefaultModule;
            mod.ammoCost            = 1;
            mod.shootStyle          = ProjectileModule.ShootStyle.SemiAutomatic;
            mod.sequenceStyle       = ProjectileModule.ProjectileSequenceStyle.Random;
            mod.cooldownTime        = 0.25f;
            mod.numberOfShotsInClip = 20;

        _NeutralSprite = AnimateBullet.CreateProjectileAnimation(
            ResMap.Get("darkwing_projectile").Base(),
            12, true, new IntVector2(23 / 2, 32 / 2),
            false, tk2dBaseSprite.Anchor.MiddleLeft, true, true);
        _HuntingSprite = AnimateBullet.CreateProjectileAnimation(
            ResMap.Get("darkwing_projectile_hunt").Base(),
            12, true, new IntVector2(23 / 2, 32 / 2),
            false, tk2dBaseSprite.Anchor.MiddleLeft, true, true);
        _RetrievingSprite = AnimateBullet.CreateProjectileAnimation(
            ResMap.Get("darkwing_projectile_retrieve").Base(),
            12, true, new IntVector2(23 / 2, 32 / 2),
            false, tk2dBaseSprite.Anchor.MiddleLeft, true, true);
        _ReturningSprite = AnimateBullet.CreateProjectileAnimation(
            ResMap.Get("darkwing_projectile_return").Base(),
            12, true, new IntVector2(23 / 2, 32 / 2),
            false, tk2dBaseSprite.Anchor.MiddleLeft, true, true);

        Projectile projectile = Lazy.PrefabProjectileFromGun(gun);
            projectile.AddDefaultAnimation(_NeutralSprite);
            projectile.AddAnimation(_HuntingSprite);
            projectile.AddAnimation(_RetrievingSprite);
            projectile.AddAnimation(_ReturningSprite);
            projectile.transform.parent        = gun.barrelOffset;
            projectile.collidesWithProjectiles = true;  // needs to be set up front, can't be set later because caching silliness or something idk
            projectile.baseData.damage         = 4f;
            projectile.baseData.speed          = 20f;

            projectile.gameObject.AddComponent<DarkwingProjectile>();
    }

}

public class DarkwingProjectile : MonoBehaviour
{
    private enum State {
        NEUTRAL,    // default state after firing
        HUNTING,    // we hit an enemy projectile and now we're seeking that enemy
        RETRIEVING, // we hit an enemy and now we're seeking one of their projectiles
        RETURNING,  // we're returning to the player after retrieving a projectile (or failing to find one)
    }

    private Projectile _projectile       = null;
    private PlayerController _owner      = null;
    private Gun _gun                     = null;
    private GameActor _target            = null;
    private Projectile _targetProjectile = null;
    private EasyTrailBullet _trail       = null;
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
                    this._projectile.spriteAnimator.Play(Darkwing._NeutralSprite);
                    newTrailColor = Color.white;
                    break;
                case State.HUNTING:
                    this._projectile.spriteAnimator.Play(Darkwing._HuntingSprite);
                    newTrailColor = Color.red;
                    break;
                case State.RETRIEVING:
                    this._projectile.spriteAnimator.Play(Darkwing._RetrievingSprite);
                    newTrailColor = ExtendedColours.purple;
                    break;
                case State.RETURNING:
                    this._projectile.spriteAnimator.Play(Darkwing._ReturningSprite);
                    newTrailColor = Color.blue;
                    break;
            }

            if (!this._trail)
                return;

            this._trail.BaseColor  = newTrailColor;
            this._trail.StartColor = newTrailColor;
            this._trail.EndColor   = Color.Lerp(newTrailColor, Color.white, 0.5f);
            this._trail.UpdateTrail();
        }
    }

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._owner = this._projectile.Owner as PlayerController;
        if (this._owner.CurrentGun.GetComponent<Darkwing>() is Darkwing darkwing)
            this._gun = this._owner.CurrentGun;

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
        // const float _TRACKING_SPEED = 5f;
        Vector2 targetPos = Vector2.zero;

        switch(this._state)
        {
            case State.NEUTRAL:
                return;  // nothing to do
            case State.HUNTING:
            case State.RETURNING:
                if (this._target == null)
                    return; // nothing to do
                targetPos = this._target.CenterPosition;
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

        Vector2 targetDir  = targetPos - this._projectile.sprite?.WorldCenter ?? this._projectile.transform.position.XY();
        if (this._state == State.RETURNING && targetDir.sqrMagnitude < 1f)
        {
            DissipateNearPlayer();
            return;
        }
        Vector2 currentDir = this._projectile.m_currentDirection;
        this._projectile.SendInDirection(0.5f * targetDir.normalized + 0.5f * currentDir.normalized, true);
        this._projectile.UpdateSpeed();

        // this._projectile.specRigidbody.Velocity = this._projectile.transform.right * this._projectile.baseData.speed;
        // this._projectile.LastVelocity = this._projectile.specRigidbody.Velocity;
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
        this._projectile.baseData.speed          *= 2f;
        this._projectile.baseData.damage         *= 2f;
        this._projectile.collidesWithProjectiles  = false;

        if (this._state == State.NEUTRAL)
        {
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
                this._target                         = enemy;
                this._projectile.collidesWithEnemies = false;
                this._state                          = State.RETRIEVING;
                return;
            // case State.HUNTING:
            //     this._target                         = this._owner;
            //     this._projectile.collidesWithEnemies = false;
            //     this._state                          = State.RETURNING;
            //     return;
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
