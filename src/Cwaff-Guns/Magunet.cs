namespace CwaffingTheGungy;

public class Magunet : CwaffGun
{
    public static string ItemName         = "Magunet";
    public static string ShortDescription = "An Attractive Option";
    public static string LongDescription  = "Attracts debris in a cone in front of the player and holds it in stasis while fire is held. Upon releasing fire, launches all attracted debris forwards, damaging any enemies in the way. Corpses deal extra damage when launched. Increases curse by 1 while in inventory.";
    public static string Lore             = "Standing in sharp defiance of all that the Gungeon, electrical engineering, and common sense stand for, the Magunet manages to weaponize the messiness of battle-torn Gungeon rooms through questionable physics that only vaguely approximate how actual magnets operate.";

    internal static GameObject _MagunetBeamVFX     = null;
    internal static GameObject _MagunetChargeVFX   = null;
    internal static GameObject _DebrisImpactVFX    = null;
    internal static GameObject _DebrisBigImpactVFX = null;

    internal const float _REACH       =  8.00f; // how far (in tiles) the gun reaches
    internal const float _SPREAD      =    35f; // width (in degrees) of how wide our cone of suction is at the end of our reach
    internal const float _ACCEL_SEC   =  3.50f; // speed (in tiles per second) at which debris accelerates towards the gun near the end of the gun's reach
    internal const float _UPDATE_RATE =   0.1f; // amount of time between debris checks / updates
    internal const float _FX_RATE     =  0.15f; // rate at which attraction vfx and sounds are played

    internal const float _SQR_REACH    = _REACH * _REACH; // avoid an unnecessary sqrt() by using sqrmagnitude
    private const float _NUM_PARTICLES = 3f;

    private float _timeOfLastCheck = 0.0f;
    private float _timeOfLastFX    = 0.0f;
    private bool  _wasCharging     = false;
    private GameObject _extantChargeVFX = null;

    public static void Init()
    {
        Lazy.SetupGun<Magunet>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.B, gunClass: GunClass.CHARGE, reloadTime: 1.2f, ammo: 999, infiniteAmmo: true,
            chargeFps: 16, curse: 1f, banFromBlessedRuns: true)
          .AddToShop(ItemBuilder.ShopType.Cursula)
          .InitProjectile(GunData.New(clipSize: -1, shootStyle: ShootStyle.Charged, //TODO: can possibly use dummy charge module here
            ammoType: GameUIAmmoType.AmmoType.BEAM, chargeTime: float.MaxValue)); // absurdly high charge value so we never actually shoot

        _MagunetBeamVFX = VFX.Create("magbeam_alt", fps: 30, scale: 0.65f, emissivePower: 1f);
        _MagunetChargeVFX = VFX.Create("magunet_charge_vfx", fps: 30);
            _MagunetChargeVFX.SetAlpha(0.5f); //REFACTOR: combine

        _DebrisImpactVFX    = Items.Ak47.EnemyImpactVFX();
        _DebrisBigImpactVFX = Items.HegemonyRifle.EnemyImpactVFX();
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        CeaseCharging();
        base.OnSwitchedAwayFromThisGun();
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        CeaseCharging();
        base.OnDroppedByPlayer(player);
    }

    public override void OnDestroy()
    {
        CeaseCharging();
        base.OnDestroy();
    }

    private void CeaseCharging()
    {
        this._extantChargeVFX.SafeDestroy();
        this._extantChargeVFX = null;
        if (this._wasCharging)
        {
            base.gameObject.Play("magunet_launch_sound");
            Exploder.DoDistortionWave(center: this.gun.barrelOffset.position.XY() + this.gun.gunAngle.ToVector(0.5f),
                distortionIntensity: 1.5f, distortionRadius: 0.05f, maxRadius: 2.75f, duration: 0.25f);
        }
        this._wasCharging = false;
    }

    public override void Update()
    {
        base.Update();
        if (BraveTime.DeltaTime == 0.0f)
            return;
        if (!this.gun.IsCharging)
        {
            CeaseCharging();
            return;
        }
        this._wasCharging = true;
        this._extantChargeVFX ??= SpawnManager.SpawnVFX(_MagunetChargeVFX, this.gun.barrelOffset.position, Quaternion.identity);
        this._extantChargeVFX.transform.position = this.gun.barrelOffset.position;
        this._extantChargeVFX.transform.rotation = this.gun.CurrentAngle.EulerZ();

        Vector2 gunpos = this.gun.barrelOffset.position;

        // Particle effects
        if (BraveTime.ScaledTimeSinceStartup - this._timeOfLastFX >= _FX_RATE)
        {
            this.gun.gameObject.Play("magunet_attract_sound");
            this._timeOfLastFX = BraveTime.ScaledTimeSinceStartup;
            for (int i = 0; i < _NUM_PARTICLES; ++i)
            {
                float spread = _SPREAD * (i / _NUM_PARTICLES);
                float angleRight = this.gun.CurrentAngle + spread;
                GameObject or = SpawnManager.SpawnVFX(_MagunetBeamVFX, (gunpos + angleRight.ToVector(_REACH)).ToVector3ZUp(), angleRight.EulerZ());
                    or.AddComponent<MagnetParticle>().Setup(this.gun, _REACH, spread);
                if (i == 0)
                    continue;
                float angleLeft = this.gun.CurrentAngle - spread;
                GameObject ol = SpawnManager.SpawnVFX(_MagunetBeamVFX, (gunpos + angleLeft.ToVector(_REACH)).ToVector3ZUp(), angleLeft.EulerZ());
                    ol.AddComponent<MagnetParticle>().Setup(this.gun, _REACH, -spread);
            }
        }

        foreach(DebrisObject debris in gunpos.DebrisWithinCone(_SQR_REACH, this.gun.CurrentAngle, _SPREAD, limit: 100, allowJunk: false))
        {
            if (debris.gameObject.GetComponent<MagnetParticle>())
                continue; // already added a magnet particle component
            if (debris.gameObject.GetComponent<Projectile>())
                continue; // can't vacuum active projectiles

            // Make sure our debris doesn't glitch out with existing additional movement modifiers
            debris.ClearVelocity();
            debris.PreventFallingInPits = true;
            debris.IsAccurateDebris = false;
            if (debris.GetComponent<SpeculativeRigidbody>() is SpeculativeRigidbody body)
                body.enabled = false;
            debris.isStatic = true;
            debris.enabled = false;

            // Actually add the MagnetParticle component to it
            debris.gameObject.AddComponent<MagnetParticle>().Setup(this.gun, (debris.gameObject.transform.position.XY() - gunpos).magnitude);
        }
    }
}

public class DebrisProjectile : Projectile
{
    private const float _MIN_PROJ_SPEED     = 4f;  // minimum speed for the debris to be considered a projectile
    private const float _MIN_PROJ_SPEED_SQR = _MIN_PROJ_SPEED * _MIN_PROJ_SPEED;

    private DebrisObject _debris;

    public override void Start()
    {
        base.Start();
        this._debris = base.GetComponent<DebrisObject>();
    }

    public override void Update()
    {
        base.Update();
        base.specRigidbody.Position = new Position(this._debris.m_currentPosition);
        base.specRigidbody.Velocity = this._debris.m_velocity.XY(); // necessary for some reason to register collisions
        if (base.specRigidbody.Velocity.sqrMagnitude < _MIN_PROJ_SPEED_SQR)
            DieInAir(true, false, false, true);
    }
}

public class MagnetParticle : MonoBehaviour
{
    private const float _MAX_LIFE           = 0.5f;
    private const float _MIN_DIST_TO_VACUUM = 1.25f;
    private const float _MIN_ALPHA          = 0.3f;
    private const float _MAX_ALPHA          = 1.0f;
    private const float _DLT_ALPHA          = _MAX_ALPHA - _MIN_ALPHA;
    private const float _MIN_LAUNCH_SPEED   = 24f;
    private const float _MAX_LAUNCH_SPEED   = 40f;

    private Gun _gun               = null;
    private tk2dBaseSprite _sprite = null;
    private float _accel           = 0.0f;
    private Vector2 _velocity      = Vector2.zero;
    private float _lifetime        = 0.0f;
    private float _alpha           = _MIN_ALPHA;
    private bool _isDebris         = true; // false for the VFX particles created by the vacuum animation itself, true for actual debris
    private DebrisObject _debris   = null;
    private float _startDistance   = 0.0f;
    private float _startScaleX     = 1.0f;
    private float _startScaleY     = 1.0f;
    private float _startAngle      = 1.0f;
    private bool  _inStasis        = false;
    private float _statisAngle     = 0.0f;
    private float _statisMag       = 0.0f;
    private float _trueAngle       = 0.0f;
    private float _timeInStasis    = 0.0f;

    public void Setup(Gun g, float startDistance = 0.0f, float offsetAngle = 0f)
    {
        this._gun           = g;
        this._startDistance = startDistance;
        this._debris        = base.gameObject.GetComponent<DebrisObject>();
        this._isDebris      = this._debris != null;
        this._sprite        = this._isDebris ? this._debris.sprite : base.gameObject.GetComponent<tk2dSprite>();
        this._startScaleX   = this._sprite.scale.x;
        this._startScaleY   = this._sprite.scale.y;
        this._startAngle    = offsetAngle;

        // get rid of any previously-cached speculativerigidbody information so that enemies we've previously
        //   collided with don't detect us as the same projectile and ignore us
        if (this._debris && this._debris.specRigidbody)
        {
            UnityEngine.Object.Destroy(this._debris.specRigidbody);
            this._debris.specRigidbody = null;
            this._debris.RegenerateCache();
        }
    }

    private void LaunchDebrisInStasis(Vector2 velocity)
    {
        int collisionWidth = 8;
        if (base.gameObject.GetComponent<SpeculativeRigidbody>() is SpeculativeRigidbody srb)
            UnityEngine.Object.Destroy(srb);
        SpeculativeRigidbody body = this._debris.specRigidbody = base.gameObject.AddComponent<SpeculativeRigidbody>();
        body.CollideWithTileMap = true;
        body.CollideWithOthers  = true;
        body.PixelColliders     = new List<PixelCollider>{new(){
            CollisionLayer         = CollisionLayer.Projectile,
            Enabled                = true,
            ColliderGenerationMode = PixelCollider.PixelColliderGeneration.Manual,
            ManualOffsetX          = collisionWidth / -2,
            ManualOffsetY          = collisionWidth / -2,
            ManualWidth            = collisionWidth,
            ManualHeight           = collisionWidth,
        }};

        DebrisProjectile p    = base.gameObject.AddComponent<DebrisProjectile>();
        p.specRigidbody       = body;
        p.Owner               = this._gun.CurrentOwner;
        p.Shooter             = p.Owner.specRigidbody;
        p.baseData.damage     = this._debris.IsCorpse ? 30f : 2f;
        p.baseData.range      = 1000000f;
        p.baseData.speed      = velocity.magnitude;
        p.baseData.force      = 50f;
        p.DestroyMode         = Projectile.ProjectileDestroyMode.BecomeDebris;
        p.collidesWithPlayer  = false;
        p.ManualControl       = true; // let debris velocity take care of movement
        p.OnHitEnemy         += OnHitEnemy;
        p.shouldRotate        = true;

        body.RegenerateCache();
        body.Reinitialize();
        p.Start();

        this._debris.m_currentPosition    = this.gameObject.transform.position.WithZ(0f);
        this._debris.m_transform.rotation = this.gameObject.transform.rotation;
        this._debris.enabled              = true;
        if (!this._debris.sprite)
            this._debris.sprite = this._sprite;  // debris sometimes forgets its cached sprite because of course it does D:
        this._debris.ApplyVelocity(velocity);

        UnityEngine.Object.Destroy(this);
    }

    private static void OnHitEnemy(Projectile bullet, SpeculativeRigidbody body, bool what)
    {
        SpawnManager.SpawnVFX(
            prefab: body.gameObject.GetComponent<DebrisObject>().IsCorpse ? Magunet._DebrisBigImpactVFX : Magunet._DebrisImpactVFX,
            position: body.UnitCenter + Lazy.RandomVector(0.5f),
            rotation: Quaternion.identity);
        UnityEngine.Object.Destroy(bullet.gameObject);
    }

    // Using LateUpdate() here so alpha is updated correctly
    private void LateUpdate()
    {
        if (BraveTime.DeltaTime == 0.0f)
            return; // nothing to do if time isn't passing

        // handle particle fading logic exclusive to the magnet particles
        if (!this._isDebris)
        {
            this._lifetime += BraveTime.DeltaTime;
            if (!this._gun || this._lifetime > _MAX_LIFE)
            {
                UnityEngine.GameObject.Destroy(base.gameObject);
                return;
            }
            float percentLeft = 1f - this._lifetime / _MAX_LIFE;
            float lerpFactor = percentLeft * percentLeft;
            float curAngle = (this._gun.CurrentAngle + this._startAngle);
            this.gameObject.transform.position = this._gun.barrelOffset.position.XY() +
                curAngle.Clamp360().ToVector(lerpFactor * this._startDistance);
            this._sprite.transform.rotation = curAngle.EulerZ();
            this._sprite.scale = new Vector3(this._startScaleX * lerpFactor, this._startScaleY * lerpFactor, 1f);
            return;
        }

        // handle no more charging / no more gun state
        if (!this._gun || !this._gun.IsCharging)
        {
            if (this._inStasis)
            {
                float launchAngle = this._statisAngle * (Magunet._SPREAD / 180f);
                float gunAngle = this._gun ? this._gun.CurrentAngle : Lazy.RandomAngle();
                LaunchDebrisInStasis((gunAngle + launchAngle).ToVector(UnityEngine.Random.Range(_MIN_LAUNCH_SPEED, _MAX_LAUNCH_SPEED)));
                return;
            }
            UnityEngine.Object.Destroy(this);
            return;
        }

        // handle particles in stasis
        Vector2 fromVacuum = (this._sprite.transform.position - this._gun.barrelOffset.position);
        this.gameObject.transform.rotation = fromVacuum.ToAngle().EulerZ();
        if (this._inStasis)
        {
            this._timeInStasis += BraveTime.DeltaTime;
            Vector2 yOff = new Vector2(0f, 0.2f * Mathf.Sin(24f * this._timeInStasis));
            this._trueAngle = (this._gun.CurrentAngle + this._statisAngle);
            this.gameObject.transform.position = this._gun.barrelOffset.position.XY() + yOff +
                this._trueAngle.ToVector(this._statisMag);
            this.gameObject.transform.rotation = this._gun.CurrentAngle.EulerZ();
            return;
        }

        float mag = fromVacuum.magnitude;
        if (mag < _MIN_DIST_TO_VACUUM)
        {
            this._inStasis    = true;
            this._statisAngle = (fromVacuum.ToAngle() - this._gun.CurrentAngle).Clamp180();
            this._statisMag   = fromVacuum.magnitude;
            return;
        }

        // Home towards the magnet
        this._velocity = this._sprite.transform.position.XY().LerpDirectAndNaturalVelocity(
            target          : this._gun.barrelOffset.position,
            naturalVelocity : this._velocity,
            accel           : VacuumCleaner._ACCEL_SEC * BraveTime.DeltaTime,
            lerpFactor      : 1f);
        this.gameObject.transform.position += (this._velocity * C.FPS * BraveTime.DeltaTime).ToVector3ZUp(0f);
    }
}
