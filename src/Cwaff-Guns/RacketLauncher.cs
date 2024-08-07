namespace CwaffingTheGungy;

public class RacketLauncher : CwaffGun
{
    public static string ItemName         = "Racket Launcher";
    public static string ShortDescription = "Paddle to the Metal";
    public static string LongDescription  = "Launches a tennis ball that bounces off of walls, enemies, projectiles, and other obstructions. The ball can be volleyed repeatedly and increases in power, speed, and knockback with each successive volley.";
    public static string Lore             = "The amount of speed, dexterity, and awareness required to play table tennis at the highest level is staggering to some when they first learn about it. The Racket takes patience and practice to wield to its full potential, but those willing to invest time honing their skills with it will be able to fearlessly return the most lethal of volleys with a Smile on their face.";

    internal const float _MAX_REFLECT_DISTANCE = 5f;
    internal const int   _IDLE_FPS             = 24;
    internal const int   _AMMO                 = 100;

    private List<TennisBall> _extantTennisBalls = new();

    public static void Init()
    {
        Gun gun = Lazy.SetupGun<RacketLauncher>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.B, gunClass: GunClass.SILLY, reloadTime: 0.0f, ammo: _AMMO, canReloadNoMatterAmmo: true,
            idleFps: _IDLE_FPS, shootFps: 60);

        gun.spriteAnimator.playAutomatically = false; //REFACTOR: don't autoplay idle animation when dropped

        gun.InitProjectile(GunData.New(ammoCost: 0, clipSize: -1, cooldown: 0.1f, shootStyle: ShootStyle.SemiAutomatic, customClip: true, preventOrbiting: true,
          damage: 10.0f, speed: 20.0f, range: 300.0f, force: 12f, sprite: "tennis_ball", fps: 12, scale: 0.6f, anchor: Anchor.MiddleCenter,
          surviveRigidbodyCollisions: true)).Attach<TennisBall>(); // DestroyMode must be set at creation time
    }

    public override void OnPlayerPickup(PlayerController player)
    {
        base.OnPlayerPickup(player);
        gun.SetAnimationFPS(gun.idleAnimation, _IDLE_FPS); // don't need to use SetIdleAnimationFPS() outside of Initializer
        gun.spriteAnimator.Play();
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        base.OnDroppedByPlayer(player);
        gun.SetAnimationFPS(gun.idleAnimation, 0); // don't need to use SetIdleAnimationFPS() outside of Initializer
        gun.spriteAnimator.StopAndResetFrameToDefault();
    }

    public override Projectile OnPreFireProjectileModifier(Gun gun, Projectile projectile, ProjectileModule mod)
    {
        if (this._extantTennisBalls.Count == 0 && gun.CurrentAmmo > 0)
        {
            mod.ammoCost = 1;
            // gun.LoseAmmo(1);
            gun.ClipShotsRemaining = gun.CurrentAmmo;
            return projectile;
        }
        Vector2 racketpos = gun.GunPlayerOwner().CenterPosition;
        foreach (TennisBall ball in this._extantTennisBalls)
        {
            if (!ball.Whackable())
                continue;
            Vector2 delta = (ball.Position() - racketpos);
            float dist = delta.magnitude;
            float angle = delta.ToAngle();
            // make sure it's within range and not behind us
            if (dist < _MAX_REFLECT_DISTANCE && Mathf.Abs(angle - gun.CurrentAngle) < 90f)
                ball.GotWhacked(gun.CurrentAngle.ToVector());
        }
        mod.ammoCost = 0;
        gun.ClipShotsRemaining = gun.CurrentAmmo + 1;
        return Lazy.NoProjectile();
    }

    public override void OnAmmoChanged(PlayerController player, Gun gun)
    {
        base.OnAmmoChanged(player, gun);
        gun.ClipShotsRemaining = gun.CurrentAmmo;
    }

    public void AddExtantTennisBall(TennisBall tennisBall)
    {
        this._extantTennisBalls.Add(tennisBall);
    }

    public void RemoveExtantTennisBall(TennisBall tennisBall)
    {
        this._extantTennisBalls.Remove(tennisBall);
    }
}

public class TennisBall : MonoBehaviour
{
    const float _RETURN_HOMING_STRENGTH = 0.1f;
    const float _SPREAD                 = 10f;
    const float _MAX_DEVIATION          = 30f; // max angle deviation we can be from player to home in
    const int   _MAX_VOLLEYS            = 16;
    const float _MAX_SPEED_BOOST        = 50f;
    const float _MAX_DAMAGE_BOOST       = 20f;
    const float _MAX_FORCE_BOOST        = 10f;

    private Projectile          _projectile    = null;
    private PlayerController    _owner         = null;
    private int                 _volleys       = 0;
    private bool                _returning     = false;
    private bool                _missedPlayer  = false;
    private bool                _dead          = false;
    private RacketLauncher      _parentGun     = null;
    private EasyTrailBullet     _trail         = null;
    private float               _baseSpeed     = 0f;
    private float               _baseDamage    = 0f;
    private float               _baseForce     = 0f;
    private BounceProjModifier  _bounce        = null;
    private Vector2             _deathVelocity = Vector2.zero;

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        if (this._projectile.Owner is not PlayerController pc)
            return;
        this._owner = pc;
        // this._projectile.DestroyMode = Projectile.ProjectileDestroyMode.BecomeDebris;
        this._projectile.DestroyMode = Projectile.ProjectileDestroyMode.DestroyComponent;

        if (this._parentGun = pc.FindGun<RacketLauncher>())
        {
            this._parentGun.AddExtantTennisBall(this);
            this._projectile.BulletScriptSettings.surviveRigidbodyCollisions = true;
            this._projectile.collidesWithProjectiles = true;
            this._projectile.collidesOnlyWithPlayerProjectiles = false;
            this._projectile.UpdateCollisionMask();
            this._projectile.specRigidbody.OnPreRigidbodyCollision += this.ReflectProjectiles;
            this._projectile.specRigidbody.OnRigidbodyCollision += (CollisionData rigidbodyCollision) => {
                this._projectile.SendInDirection(rigidbodyCollision.Normal, false);
                ReturnToSender();
            };
            this._projectile.OnDestruction += (Projectile p) => {
                if (p.GetComponent<TennisBall>() is TennisBall tc)
                    this._parentGun.RemoveExtantTennisBall(tc);
            };
            this._projectile.gameObject.Play("monkey_tennis_hit_serve");
        }

        this._bounce = this._projectile.gameObject.GetOrAddComponent<BounceProjModifier>();
            this._bounce.numberOfBounces     = 9999;
            this._bounce.chanceToDieOnBounce = 0f;
            this._bounce.onlyBounceOffTiles  = false;
            this._bounce.ExplodeOnEnemyBounce = false;
            this._bounce.OnBounce += this.ReturnToSender;

        this._trail = this._projectile.gameObject.AddComponent<EasyTrailBullet>();
            this._trail.StartWidth = 0.2f;
            this._trail.EndWidth   = 0.1f;
            this._trail.LifeTime   = 0.1f;
            this._trail.BaseColor  = ExtendedColours.lime;
            this._trail.StartColor = ExtendedColours.lime;
            this._trail.EndColor   = Color.green;

        this._baseSpeed  = this._projectile.baseData.speed;
        this._baseDamage = this._projectile.baseData.damage;
        this._baseForce  = this._projectile.baseData.force;

        // this._projectile.gameObject.Play("racket_hit");
    }

    private void ReflectProjectiles(SpeculativeRigidbody myRigidbody, PixelCollider myCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherCollider)
    {
        if (otherRigidbody.GetComponent<Projectile>() is not Projectile p)
            return;
        if (this._returning || p.Owner is PlayerController)
        {
            PhysicsEngine.SkipCollision = true;
            return;
        }
        PassiveReflectItem.ReflectBullet(p, true, this._owner.gameActor, 10f, 1f, 1f, 0f);
        PhysicsEngine.SkipCollision = true;
        ReturnToSender();
    }

    public Vector2 Position()
    {
        return this._projectile.SafeCenter;
    }

    public void DieInAir()
    {
        this._deathVelocity = 0.5f * this._projectile.LastVelocity;

        // Make into debris
        DebrisObject debris            = base.gameObject.GetOrAddComponent<DebrisObject>();
        debris.angularVelocity         = 45;
        debris.angularVelocityVariance = 20;
        debris.decayOnBounce           = 0.5f;
        debris.bounceCount             = 4;
        debris.canRotate               = true;
        debris.shouldUseSRBMotion      = true;
        debris.sprite                  = this._projectile.sprite;
        debris.animatePitFall          = true;
        debris.audioEventName          = "monkey_tennis_bounce_first";
        debris.AssignFinalWorldDepth(-0.5f);
        debris.Trigger(this._deathVelocity, 0.5f);

        // Stop animating
        debris.spriteAnimator.Stop();
        // Destroy unused components that may interfere with rendering
        EasyTrailBullet tr = debris.GetComponent<EasyTrailBullet>();
            tr.Disable();
            UnityEngine.GameObject.Destroy(tr);
        UnityEngine.GameObject.Destroy(debris.GetComponent<TennisBall>()); // destroy the TennisBall component

        this._dead = true;
        this._projectile.gameObject.Play("monkey_tennis_bounce_second");
        this._projectile.DieInAir(suppressInAirEffects: true);
    }

    public bool Whackable()
    {
        return this._returning;
    }

    public void GotWhacked(Vector2 direction)
    {
        if (!this._returning)
            return;

        this._volleys                    = Mathf.Min(this._volleys + 1, _MAX_VOLLEYS);
        float percentPower               = (float)this._volleys / (float)_MAX_VOLLEYS;
        this._projectile.baseData.speed  = this._baseSpeed  + _MAX_SPEED_BOOST * percentPower;
        this._projectile.baseData.damage = this._baseDamage + _MAX_DAMAGE_BOOST * percentPower;
        this._projectile.baseData.force  = this._baseForce  + _MAX_FORCE_BOOST * percentPower;
        this._trail.LifeTime             = 0.1f + (this._volleys * 0.02f);
        Color newColor                   = Color.Lerp(ExtendedColours.lime, Color.red, 0.1f * (float)this._volleys);
        this._trail.BaseColor            = newColor;
        this._trail.StartColor           = newColor;
        this._trail.UpdateTrail();

        this._returning = false;
        this._missedPlayer = false;
        this._projectile.Speed = this._projectile.baseData.speed;
        this._projectile.SendInDirection(direction, true);
        // this._projectile.gameObject.Play("racket_hit");
        this._projectile.gameObject.Play("monkey_tennis_hit_return_mid");
        if (this._volleys > 6)
            this._projectile.gameObject.Play("sonic_olympic_smash");
        else if (this._volleys > 3)
            this._projectile.gameObject.Play("sonic_olympic_sidespin"/*"monkey_tennis_hit_return_mid"*/);
    }

    private IEnumerator DieNextFrame()
    {
        yield return null;
        this.DieInAir();
    }

    private void ReturnToSender()
    {
        if (this._dead)
            return;
        if (this._returning)
        {
            UnityEngine.Object.Destroy(this._bounce);
            StartCoroutine(DieNextFrame()); // avoid glitch with bounce modifier messing with debris object velocity
            return;
        }
        this._returning = true;
        float dirToOwner = (this._owner.CenterPosition - this._projectile.SafeCenter).ToAngle();
        float acc = this._owner.AccuracyMult();
        this._projectile.SendInDirection(dirToOwner.AddRandomSpread(_SPREAD * Mathf.Sqrt(acc)).ToVector(), true);
        this._projectile.gameObject.Play("racket_hit");
    }

    private void HomeTowardsTarget(Vector2 targetPos, Vector2 curVelocity)
    {
        Vector2 targetVelocity = (targetPos - this._projectile.SafeCenter).normalized;
        if (this._returning && (Mathf.Abs(curVelocity.ToAngle().Clamp360() - targetVelocity.ToAngle().Clamp360()) > _MAX_DEVIATION))
        {
            this._missedPlayer = true;
            return;
        }
        Vector2 newVelocty = (_RETURN_HOMING_STRENGTH * targetVelocity) + ((1 - _RETURN_HOMING_STRENGTH) * curVelocity);
        this._projectile.SendInDirection(newVelocty, false);
    }

    private void Update()
    {
        if (this._dead || this._missedPlayer)
            return;

        Vector2 curVelocity = this._projectile.LastVelocity.normalized;

        // Returning to the player
        if (this._returning)
        {
            HomeTowardsTarget(this._owner.CenterPosition, curVelocity);
            return;
        }

        // Homing in on nearest enemy
        Vector2? maybeTarget = Lazy.NearestEnemyPosWithinConeOfVision(
            start                            : this._projectile.transform.position,
            coneAngle                        : curVelocity.ToAngle().Clamp360(),
            maxDeviation                     : _MAX_DEVIATION,
            useNearestAngleInsteadOfDistance : true,
            ignoreWalls                      : false
            );
        if (maybeTarget is Vector2 target)
            HomeTowardsTarget(target, curVelocity);
    }
}
