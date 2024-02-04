namespace CwaffingTheGungy;

public class Suncaster : AdvancedGunBehavior
{
    public static string ItemName         = "Suncaster";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    internal const int _PRISM_AMMO_COST = 5;
    internal const float _PRISM_LAUNCH_SPEED = 20f;

    internal static GameObject _PrismVFX = null;
    internal static TrailController _SunTrailPrefab = null;
    internal static List<SuncasterPrism> _ExtantPrisms = new();

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<Suncaster>(ItemName, ProjectileName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.C, gunClass: GunClass.FIRE, reloadTime: 1.2f, ammo: 80, canReloadNoMatterAmmo: true);
            gun.SetAnimationFPS(gun.shootAnimation, 30);
            gun.SetAnimationFPS(gun.reloadAnimation, 40);

        gun.InitProjectile(new(clipSize: 6, cooldown: 0.5f, shootStyle: ShootStyle.SemiAutomatic, damage: 0f, speed: 100f,
          sprite: "suncaster_projectile", fps: 12, anchor: Anchor.MiddleLeft)).Attach<SuncasterProjectile>();

        _SunTrailPrefab = VFX.CreateTrailObject(ResMap.Get("subtractor_beam_mid")[0], new Vector2(20, 4), new Vector2(0, 0), //TODO: get our own trail here
            ResMap.Get("subtractor_beam_mid"), 60, ResMap.Get("subtractor_beam_start"), 60, cascadeTimer: C.FRAME, destroyOnEmpty: true);

        _PrismVFX = VFX.Create("prism_vfx", fps: 6, loops: true, anchor: Anchor.LowerCenter); // TODO: doesn't exist yet
    }

    public override void OnReloadPressed(PlayerController player, Gun gun, bool manualReload)
    {
        base.OnReloadPressed(player, gun, manualReload);
        if (player.IsDodgeRolling || !player.AcceptingNonMotionInput || gun.CurrentAmmo <= _PRISM_AMMO_COST)
          return;
        GameObject prism = UnityEngine.Object.Instantiate(_PrismVFX, gun.barrelOffset.position, Quaternion.identity);
        prism.AddComponent<SuncasterPrism>().Setup(gun.CurrentAngle.ToVector(_PRISM_LAUNCH_SPEED));
        gun.LoseAmmo(_PRISM_AMMO_COST);
        AkSoundEngine.PostEvent("fire_coin_sound", gun.gameObject);
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        AkSoundEngine.PostEvent("subtractor_beam_fire_sound_stop_all", this.Owner.gameObject);
        AkSoundEngine.PostEvent("subtractor_beam_fire_sound", this.Owner.gameObject);
    }
}

public class SuncasterProjectile : MonoBehaviour
{
    private Projectile           _proj        = null;
    private PlayerController     _owner       = null;
    private Vector2              _lastPos     = Vector2.zero;
    private List<SuncasterPrism> _hitPrisms   = new();
    private int                  _amps        = 0;
    private TrailController      _trail       = null;

    private void Start()
    {
        this._proj  = base.GetComponent<Projectile>();
        this._owner = this._proj.ProjectilePlayerOwner();
        this._trail = this._proj.AddTrailToProjectileInstance(Suncaster._SunTrailPrefab);
        this._trail.gameObject.SetGlowiness(100f);
    }

    private void Update()
    {
      Vector2 curPos = base.transform.PositionVector2();
      Vector2 intersection;
      foreach (SuncasterPrism prism in Suncaster._ExtantPrisms)
      {
        tk2dSprite sprite = prism.GetComponent<tk2dSprite>();
        if (!BraveUtility.LineIntersectsAABB(this._lastPos, curPos, sprite.WorldBottomLeft, sprite.WorldTopRight - sprite.WorldBottomLeft, out intersection))
          continue; //TODO: double check the intersection logic for sprites
        if (this._hitPrisms.Contains(prism))
          continue;
        this._hitPrisms.Add(prism);
        Amplify();
      }
      this._lastPos = curPos;
    }

    private void Amplify()
    {
      ++this._amps;
      this._proj.baseData.damage *= 2;
      this._proj.sprite.SetGlowiness(this._proj.baseData.damage);
      AkSoundEngine.PostEvent("aimu_focus_sound", base.gameObject);
    }
}

public class SuncasterPrism : MonoBehaviour
{
    private const float _FRICTION   = 0.98f;
    private const float _BOB_SPEED  = 4f;
    private const float _BOB_HEIGHT = 0.20f;
    private const float _MAX_LIFE   = 20.0f;

    private bool    _setup          = false;
    private Vector2 _velocity       = Vector2.zero;
    private Vector3 _basePos        = Vector2.zero;
    private float   _lifetime       = 0.0f;

    public void Setup(Vector2 velocity)
    {
      this._velocity = velocity;
      this._setup    = true;
      Suncaster._ExtantPrisms.Add(this);
    }

    private void OnDestroy()
    {
      Suncaster._ExtantPrisms.Remove(this);
    }

    private void Update()
    {
        if (!this._setup)
          return;

        this._lifetime += BraveTime.DeltaTime;
        if (this._lifetime > _MAX_LIFE)
        {
          Lazy.DoSmokeAt(this._basePos);
          UnityEngine.Object.Destroy(base.gameObject);
          return;
        }

        if (this._velocity.sqrMagnitude > 1f)
        {
            this._velocity *= (float)Lazy.FastPow(_FRICTION, C.FPS * BraveTime.DeltaTime);
            this._basePos += (this._velocity * BraveTime.DeltaTime).ToVector3ZUp();
        }
        base.transform.position = new Vector2(this._basePos.x, this._basePos.y + _BOB_HEIGHT * Mathf.Sin(_BOB_SPEED * BraveTime.ScaledTimeSinceStartup)).ToVector3ZisY();
    }
}
