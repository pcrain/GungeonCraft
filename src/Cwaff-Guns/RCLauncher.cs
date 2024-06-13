namespace CwaffingTheGungy;

/*
   TODO
    -
*/

public class RCLauncher : CwaffGun
{
    public static string ItemName         = "R.C. Launcher";
    public static string ShortDescription = "Pedal to the Metal";
    public static string LongDescription  = "Launches R.C. cars that explode on impact. Each car follows the car fired before it, or the mouse cursor / controller aim direction if they are the lead car.";
    public static string Lore             = "A case study of the unreasonable effectiveness of retrofitting children's toys with AI and explosives, this launcher's steerable projectiles ensure swift and accurate destruction in the hands of a competent pilot. The projectiles also still make a surprisingly fun and entertaining diversion for children ages 4-14, provided they never crash into anything.";

    private const float _FULL_RELOAD_TIME = 2.0f;

    internal static ExplosionData _CarExplosion = null;

    public static void Init()
    {
        Gun gun = Lazy.SetupGun<RCLauncher>(ItemName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.C, gunClass: GunClass.SILLY, reloadTime: _FULL_RELOAD_TIME, ammo: 240, shootFps: 30, reloadFps: 16,
                loopReloadAt: 0, fireAudio: "rc_car_launch_sound", reloadAudio: "rc_car_reload_sound");

        gun.InitSpecialProjectile<RCGuidedProjectile>(GunData.New(sprite: "rc_car_projectile", clipSize: 7, cooldown: 0.1f,
            shootStyle: ShootStyle.SemiAutomatic, speed: 20f, damage: 9f, range: 9999f,
            shouldRotate: false, shouldFlipHorizontally: false, shouldFlipVertically: false,
            spawnSound: "rc_car_engine_sound", stopSoundOnDeath: true, deathSound: "rc_car_crash_sound")
        ).Attach<RCGuidedProjectile>(igp => {
            igp.dumbfireTime          = 0.2f;
            igp.trackingSpeed         = 360f;
            igp.minSpeed              = 20f;
            igp.accel                 = 15f;
            igp.followTheLeader       = true;
            igp.pierceMinorBreakables = true;
        }).Attach<RCProjectileBehavior>(
        );

        // Initialize our explosion data
        _CarExplosion = new ExplosionData()
        {
            forceUseThisRadius     = true,
            pushRadius             = 0.5f,
            damageRadius           = 0.5f,
            damageToPlayer         = 0f,
            doDamage               = true,
            damage                 = 5f,
            doDestroyProjectiles   = false,
            doForce                = true,
            debrisForce            = 10f,
            preventPlayerForce     = true,
            explosionDelay         = 0.01f,
            usesComprehensiveDelay = false,
            doScreenShake          = false,
            playDefaultSFX         = true,
            effect                 = Explosions.ExplosiveRounds.effect,
            ignoreList             = Explosions.DefaultSmall.ignoreList,
            ss                     = Explosions.DefaultSmall.ss,
        };
    }

    public override void Update()
    {
        base.Update();
        this.gun.reloadTime = _FULL_RELOAD_TIME * (1f - (float)this.gun.ClipShotsRemaining / (float)this.gun.ClipCapacity);
    }
}

public class RCProjectileBehavior : MonoBehaviour
{
    private Projectile _projectile;
    private PlayerController _owner;
    private tk2dSpriteAnimationClip _clip;
    private int _numFrames;
    private EasyTrailBullet _trail;

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._owner = this._projectile.Owner as PlayerController;

        this._projectile.spriteAnimator.Stop(); // stop animating immediately after creation so we can stick with our initial sprite
        this._clip = this._projectile.spriteAnimator.CurrentClip;
        this._numFrames = this._clip.frames.Length;

        Color c = new Color(
            0.25f + 0.5f * UnityEngine.Random.value,
            0.25f + 0.5f * UnityEngine.Random.value,
            0.25f + 0.5f * UnityEngine.Random.value
            );
        Color lightc = Color.Lerp(c, Color.white, 0.5f);
        this._projectile.AdjustPlayerProjectileTint(lightc, priority: 1);
        SpriteOutlineManager.AddOutlineToSprite(this._projectile.sprite, c);

        this._trail = this._projectile.gameObject.AddComponent<EasyTrailBullet>();
            this._trail.StartWidth = 0.2f;
            this._trail.EndWidth   = 0.01f;
            this._trail.LifeTime   = 0.2f;
            this._trail.BaseColor  = lightc;
            this._trail.StartColor = lightc;
            this._trail.EndColor   = lightc;
    }

    private void Update()
    {
      int frameForRotation = Mathf.RoundToInt((float)this._numFrames * (1f + this._projectile.LastVelocity.ToAngle() / 360.0f)) % this._numFrames;
      this._projectile.sprite.SetSprite(this._clip.frames[frameForRotation].spriteId);
    }

    private void OnDestroy()
    {
      Exploder.Explode(this._projectile.transform.position, RCLauncher._CarExplosion, this._projectile.Direction, ignoreQueues: true);
    }
}

/// <summary>Guided projectile that doesn't depend on sprite rotation like the base game's InputGuidedProjectile does</summary>
public class RCGuidedProjectile : Projectile
{
    private static List<Projectile> _ExtantCars = new();

    public float trackingSpeed = 45f;
    public float minSpeed      = -1f;
    public float accel         = -1f;
    public float dumbfireTime  = 0f;
    public float turnFriction  = 0.1f;
    public bool followTheLeader = false;
    public Func<Projectile, Vector2?> overrideTargetFunc;

    private float _dumbfireTimer;
    private Projectile _currentLeader = null;

    public override void Start()
    {
        base.Start();
        _ExtantCars.Add(this);
        if (followTheLeader)
            overrideTargetFunc += FollowTheLeader;
    }

    public override void OnDestroy()
    {
        _ExtantCars.Remove(this);
        base.OnDestroy();
    }

    public override void Move()
    {
        bool shouldAdjustMovement = true;
        if (dumbfireTime > 0f && _dumbfireTimer < dumbfireTime)
        {
            _dumbfireTimer += BraveTime.DeltaTime;
            shouldAdjustMovement = false;
        }
        if (shouldAdjustMovement && base.Owner is PlayerController)
        {
            BraveInput instanceForPlayer = BraveInput.GetInstanceForPlayer((base.Owner as PlayerController).PlayerIDX);
            Vector2 targetVector = GetOverrideTarget() ?? ((!instanceForPlayer.IsKeyboardAndMouse())
                ? instanceForPlayer.ActiveActions.Aim.Vector
                : ((base.Owner as PlayerController).unadjustedAimPoint.XY() - base.specRigidbody.UnitCenter));
            float targetAngle = targetVector.ToAngle();
            if (base.specRigidbody.Velocity != Vector2.zero)
            {
                float z = base.specRigidbody.Velocity.ToAngle();
                float z2 = Mathf.MoveTowardsAngle(z, targetAngle, trackingSpeed * BraveTime.DeltaTime);
                base.specRigidbody.Velocity = (Quaternion.Euler(0f, 0f, z2) * new Vector2(baseData.speed, 0f));
                if (this.turnFriction > 0f)
                {
                    float frictionFactor = Mathf.Abs( (z2 - z).Clamp180() ) / 180f;
                    this.ApplyFriction(1f - (turnFriction * frictionFactor));
                }
            }
            else
                base.specRigidbody.Velocity = baseData.speed * targetVector.normalized;
            if (this.accel > 0f)
                this.Accelerate(this.accel);
        }
        else
            base.specRigidbody.Velocity = m_currentDirection * m_currentSpeed;
        base.LastVelocity = base.specRigidbody.Velocity;
    }

    private Vector2? FollowTheLeader(Projectile p)
    {
        if (this._currentLeader)
            return this._currentLeader.SafeCenter - p.SafeCenter; // if we have a viable leader already, continue following them

        int myIndex = _ExtantCars.IndexOf(p);
        if (myIndex == -1)
            return null; // if we're not in the list of extant cars, we have nothing to do

        for (int i = myIndex - 1; i >= 0; --i) // find the first car ahead of us that is viable
        {
            Projectile leader = _ExtantCars[i];
            if (!leader)
                continue;
            this._currentLeader = leader;
            return this._currentLeader.SafeCenter - p.SafeCenter;
        }

        this._currentLeader = null;
        return null; // nothing is viable, so unset _currentLeader and return
    }

    private Vector2? GetOverrideTarget()
    {
        return (overrideTargetFunc != null) ? overrideTargetFunc(this) : null;
    }
}
