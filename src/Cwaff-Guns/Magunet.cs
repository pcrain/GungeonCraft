namespace CwaffingTheGungy;

public class Magunet : AdvancedGunBehavior
{
    public static string ItemName         = "Magunet";
    public static string SpriteName       = "magunet";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    internal static GameObject _MagunetVFX = null;

    internal const float _REACH       =  8.00f; // how far (in tiles) the gun reaches
    internal const float _SPREAD      =    35f; // width (in degrees) of how wide our cone of suction is at the end of our reach
    internal const float _ACCEL_SEC   =  3.50f; // speed (in tiles per second) at which debris accelerates towards the gun near the end of the gun's reach
    internal const float _UPDATE_RATE =   0.1f; // amount of time between debris checks / updates
    internal const float _FX_RATE     =  0.15f; // rate at which attraction vfx and sounds are played

    internal const float _SQR_REACH   = _REACH * _REACH; // avoid an unnecessary sqrt() by using sqrmagnitude

    private float _timeOfLastCheck = 0.0f;
    private float _timeOfLastFX    = 0.0f;

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<Magunet>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.B, gunClass: GunClass.CHARGE, reloadTime: 1.2f, ammo: 999, infiniteAmmo: true);
            gun.SetAnimationFPS(gun.chargeAnimation, 16);

        gun.InitProjectile(new(clipSize: -1, shootStyle: ShootStyle.Charged, ammoType: GameUIAmmoType.AmmoType.BEAM, chargeTime: float.MaxValue)); // absurdly high charge value so we never actually shoot

        _MagunetVFX = VFX.Create(/*"magunet_wave_sprite"*/"magbeam_alt", fps: 30, loops: true, anchor: Anchor.MiddleCenter, scale: 0.65f, emissivePower: 1f);
    }

    private const float _NUM_PARTICLES = 3f;
    protected override void Update()
    {
        base.Update();
        if (BraveTime.DeltaTime == 0.0f)
            return;
        if (!this.gun.IsCharging)
            return;

        Vector2 gunpos = this.gun.barrelOffset.position;

        // Particle effects
        if (BraveTime.ScaledTimeSinceStartup - this._timeOfLastFX >= _FX_RATE)
        {
            AkSoundEngine.PostEvent("magunet_attract_sound", this.gun.gameObject);
            this._timeOfLastFX = BraveTime.ScaledTimeSinceStartup;
            for (int i = 0; i < _NUM_PARTICLES; ++i)
            {
                float spread = _SPREAD * (i / _NUM_PARTICLES);
                float angleRight = this.gun.CurrentAngle + spread;
                GameObject or = SpawnManager.SpawnVFX(_MagunetVFX, (gunpos + angleRight.ToVector(_REACH)).ToVector3ZUp(), angleRight.EulerZ());
                    or.AddComponent<MagnetParticle>().Setup(this.gun, _REACH, spread);
                if (i == 0)
                    continue;
                float angleLeft = this.gun.CurrentAngle - spread;
                GameObject ol = SpawnManager.SpawnVFX(_MagunetVFX, (gunpos + angleLeft.ToVector(_REACH)).ToVector3ZUp(), angleLeft.EulerZ());
                    ol.AddComponent<MagnetParticle>().Setup(this.gun, _REACH, -spread);
            }
        }

        if (BraveTime.ScaledTimeSinceStartup - this._timeOfLastCheck < _UPDATE_RATE)
            return; // don't need to update debris 60 times a second

        this._timeOfLastCheck = BraveTime.ScaledTimeSinceStartup;

        // TODO: figure out how to make this less resource intensive...there can be a lot of debris
        foreach(DebrisObject debris in gunpos.DebrisWithinCone(_SQR_REACH, this.gun.CurrentAngle, _SPREAD))
        {
            if (debris.gameObject.GetComponent<MagnetParticle>())
                continue; // already added a vacuum particle component

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
        base.specRigidbody.Velocity = this._debris.m_velocity.XY();
        if (base.specRigidbody.Velocity.sqrMagnitude < 1)
            DieInAir(true, false, false, true);

        PixelCollider c = base.specRigidbody.PrimaryPixelCollider;
        // SpawnManager.SpawnVFX(VFX.MiniPickup, C.PIXEL_SIZE * (c.Position + c.Dimensions / 2).ToVector3(0), Quaternion.identity);
        // base.specRigidbody.transform.position = this._debris.m_currentPosition;
    }
}

public class MagnetParticle : MonoBehaviour
{
    private const float _MAX_LIFE           = 0.5f;
    private const float _MIN_DIST_TO_VACUUM = 1.25f;
    private const float _MIN_ALPHA          = 0.3f;
    private const float _MAX_ALPHA          = 1.0f;
    private const float _DLT_ALPHA          = _MAX_ALPHA - _MIN_ALPHA;

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
    private float _launchAngle     = 0.0f;

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

        if (this._debris?.gameObject.GetComponent<DebrisProjectile>() is DebrisProjectile p)
        {
            Debug.Log($"destroying old DebrisProjectile");
            p.DieInAir();
            // if (p.specRigidbody)
            //     UnityEngine.Object.Destroy(p.specRigidbody);
            // UnityEngine.Object.Destroy(p);
        }
        if (this._debris?.specRigidbody)
        {
            UnityEngine.Object.Destroy(this._debris.specRigidbody);
            this._debris.specRigidbody = null;
        }
    }

    private void LaunchDebrisInStasis(Vector2 velocity)
    {
        if (!this._debris)
        {
            ETGModConsole.Log($"SHOULD NEVER HAPPEN");
            UnityEngine.Object.Destroy(base.gameObject);
            return;
        }

        int collisionWidth = 16;
        if (this._debris.specRigidbody is not SpeculativeRigidbody body)
        {
            body                        = this._debris.gameObject.AddComponent<SpeculativeRigidbody>();
            body.CollideWithTileMap     = true;
            body.CollideWithOthers      = true;
            body.PixelColliders = new List<PixelCollider>{new(){
                // Enabled                = false,
                CollisionLayer         = CollisionLayer.Projectile,
                Enabled                = true,
                ColliderGenerationMode = PixelCollider.PixelColliderGeneration.Manual,
                ManualOffsetX          = collisionWidth / -2,
                ManualOffsetY          = collisionWidth / -2,
                ManualWidth            = collisionWidth,
                ManualHeight           = collisionWidth,
            }};
            body.Initialize();
            this._debris.specRigidbody = body;
        }
        else
            ETGModConsole.Log($"SHOULD NEVER HAPPEN 2");

        if (!body)
        {
            ETGModConsole.Log($"SHOULD NEVER HAPPEN 3");
            UnityEngine.Object.Destroy(base.gameObject);
            return;
        }

        DebrisProjectile p    = this._debris.gameObject.AddComponent<DebrisProjectile>();
        p.specRigidbody       = body;
        p.Owner               = this._gun.CurrentOwner;
        p.Shooter             = p.Owner.specRigidbody;
        p.baseData.damage     = 10f;
        p.baseData.range      = 1000000f;
        p.baseData.speed      = velocity.magnitude;
        p.baseData.force      = 50f;
        p.DestroyMode         = Projectile.ProjectileDestroyMode.DestroyComponent;
        // p.OnDestruction      += DestroyTrail;
        p.collidesWithEnemies = true;
        p.collidesWithPlayer  = false;
        p.ManualControl       = true; // let debris velocity take care of movement
        // p.BulletScriptSettings.surviveTileCollisions = true;
        p.RegenerateCache(); // TODO: can have stale transform caches somehow, figure out why this is necessary
        p.Start();
        body.Reinitialize();

        // if (!p.GetComponent<EasyTrailBullet>())
        // {
        //     EasyTrailBullet trail = p.gameObject.AddComponent<EasyTrailBullet>();
        //     trail.StartWidth      = 0.1f;
        //     trail.EndWidth        = 0f;
        //     trail.LifeTime        = 0.25f;
        //     trail.BaseColor       = Color.yellow;
        //     trail.StartColor      = Color.yellow;
        //     trail.EndColor        = Color.yellow;
        // }

        this._debris.enabled           = true;
        this._debris.m_currentPosition = this.gameObject.transform.position.WithZ(0f);
        this._debris.ApplyVelocity(velocity);
        // this._debris.m_hasBeenTriggered = false;

        UnityEngine.Object.Destroy(this);
    }

    // private static void DestroyTrail(Projectile p)
    // {
    //     // ETGModConsole.Log($"destroying projectile");
    //     // Lazy.DoSmokeAt(p.transform.position);
    //     if (p.GetComponent<EasyTrailBullet>() is EasyTrailBullet trail)
    //         UnityEngine.Object.Destroy(trail);
    // }

    // Using LateUpdate() here so alpha is updated correctly
    private void LateUpdate()
    {
        if (BraveTime.DeltaTime == 0.0f)
            return; // nothing to do if time isn't passing

        // handle particles in stasis
        if (this._inStasis)
        {
            if (!this._gun || !this._gun.IsCharging)
            {
                LaunchDebrisInStasis(this._launchAngle/*.Clamp360()*/.ToVector(30f));
                return;
            }
            this._launchAngle = (this._gun.CurrentAngle + this._statisAngle);
            this.gameObject.transform.position = this._gun.barrelOffset.position.XY() +
                this._launchAngle.Clamp360().ToVector(this._statisMag);
            return;
        }

        // handle particle fading logic exclusive to the magnet particles
        if (!this._isDebris)
        {
            this._lifetime += BraveTime.DeltaTime;
            float percentLeft = 1f - this._lifetime / _MAX_LIFE;
            float lerpFactor = percentLeft * percentLeft;
            float curAngle = (this._gun.CurrentAngle + this._startAngle);
            this.gameObject.transform.position = this._gun.barrelOffset.position.XY() +
                curAngle.Clamp360().ToVector(lerpFactor * this._startDistance);
            this._sprite.transform.rotation = curAngle.EulerZ();
            this._sprite.scale = new Vector3(this._startScaleX * lerpFactor, this._startScaleY * lerpFactor, 1f);
            if (!this._gun || this._lifetime > _MAX_LIFE)
                UnityEngine.GameObject.Destroy(base.gameObject);
            return;
        }

        Vector2 fromVacuum = (this._sprite.transform.position - this._gun.barrelOffset.position);
        float mag = fromVacuum.magnitude;
        if (mag < _MIN_DIST_TO_VACUUM)
        {
            // UnityEngine.GameObject.Destroy(base.gameObject);
            this._inStasis    = true;
            this._statisAngle = (fromVacuum.ToAngle() - this._gun.CurrentAngle);
            this._statisMag   = fromVacuum.magnitude;
            return;
        }

        // Home towards the magnet
        this._velocity = this._sprite.transform.position.XY().LerpNaturalAndDirectVelocity(
            target          : this._gun.barrelOffset.position,
            naturalVelocity : this._velocity,
            accel           : VacuumCleaner._ACCEL_SEC * BraveTime.DeltaTime,
            lerpFactor      : 0.5f);
        this.gameObject.transform.position += (this._velocity * C.FPS * BraveTime.DeltaTime).ToVector3ZUp(0f);
    }
}
