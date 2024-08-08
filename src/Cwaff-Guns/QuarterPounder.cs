namespace CwaffingTheGungy;

public class QuarterPounder : CwaffGun
{
    public static string ItemName         = "Quarter Pounder";
    public static string ShortDescription = "Pay Per Pew";
    public static string LongDescription  = "Uses casings as ammo. Fires high-powered projectiles that transmute enemies to gold upon death, spawning an extra casing.";
    public static string Lore             = "Legend says that Dionysus granted King Midas' wish that everything he touched would turn to gold. Midas was overjoyed at first, but upon turning his food and daughter to gold, realized his wish was ill thought out, and eventually died of starvation.\n\nThe average person might interpret King Midas as a cautionary tale to be mindful of what you wish for. One gunsmith, however, heard the tale and thought, \"wow, turning my enemies to gold sure would be useful!\". Despite completely missing the moral of King Midas, the gunsmith did succeed in forging a rather powerful weapon, proving that the meaning of art is indeed up to the beholder.";

    internal static GameObject _MidasParticleVFX;
    internal static Projectile _GoldProjectile;

    public static void Init()
    {
        //NOTE: can't use vanilla RequiresFundsToShoot because that gives us an infinite clip size
        Lazy.SetupGun<QuarterPounder>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.C, gunClass: GunClass.RIFLE, reloadTime: 1.1f, ammo: 9999, canGainAmmo: false,
            shootFps: 24, reloadFps: 16, muzzleVFX: "muzzle_quarter_pounder", muzzleFps: 30, muzzleScale: 0.4f, muzzleAnchor: Anchor.MiddleCenter,
            fireAudio: "fire_coin_sound", reloadAudio: "coin_gun_reload", banFromBlessedRuns: true)
          .InitProjectile(GunData.New(clipSize: 10, angleVariance: 15.0f, shootStyle: ShootStyle.SemiAutomatic, customClip: true, damage: 20.0f, speed: 44.0f,
            sprite: "coin_gun_projectile", fps: 2, anchor: Anchor.MiddleCenter, hitSound: "coin_hit_wall_sound"))
          .Attach<MidasProjectile>();

        _GoldProjectile = Items.Ak47.CloneProjectile(GunData.New(sprite: "midas_gold_projectile", damage: 30.0f, speed: 80.0f, force: 10.0f,
          range: 80.0f, shouldRotate: true)).Attach<MidasProjectile>();

        _MidasParticleVFX = VFX.Create("midas_sparkle", fps: 8, emissivePower: 5);
    }

    public override void OnPlayerPickup(PlayerController player)
    {
        base.OnPlayerPickup(player);
        AdjustAmmoToMoney();
    }

    public override void OnSwitchedToThisGun()
    {
        base.OnSwitchedToThisGun();
        AdjustAmmoToMoney();
    }

    public override void Update()
    {
        base.Update();
        if (!this.PlayerOwner)
            return;
        AdjustAmmoToMoney();
    }

    public override void OnPostFired(PlayerController player, Gun gun)
    {
        base.OnPostFired(player, gun);
        --GameManager.Instance.PrimaryPlayer.carriedConsumables.Currency;
        AdjustAmmoToMoney();
    }

    private void AdjustAmmoToMoney()
    {
        int money = GameManager.Instance.PrimaryPlayer.carriedConsumables.Currency;
        this.gun.CurrentAmmo = money;
        if (this.gun.ClipShotsRemaining > money)
            this.gun.ClipShotsRemaining = money;
    }
}

public class MidasProjectile : MonoBehaviour
{
    private const float _SHEEN_WIDTH = 20.0f;
    internal static Color _Gold      = new Color(1f,1f,0f,1f);
    internal static Color _White     = new Color(1f,1f,1f,1f);

    private void Start()
    {
        Projectile p = base.GetComponent<Projectile>();
        p.OnWillKillEnemy += this.OnWillKillEnemy;
    }

    private void OnWillKillEnemy(Projectile bullet, SpeculativeRigidbody enemy)
    {
        if (!enemy.aiActor || !enemy.healthHaver || enemy.healthHaver.IsBoss || enemy.healthHaver.IsSubboss)
            return; // don't do anything to bosses //NOTE: technically works on most bosses, but causes problems with Dragun and who knows what modded bosses...so just disabling

        tk2dBaseSprite sprite = enemy.aiActor.sprite.DuplicateInWorld();
        GameObject statue = sprite.gameObject;

        IntVector2 offset = (16f * (sprite.WorldBottomLeft - statue.transform.position.XY())).ToIntVector2(); // compute offsets to make speculative rigid body work correctly
        PixelCollider pixelCollider = new PixelCollider();
            pixelCollider.CollisionLayer         = CollisionLayer.PlayerBlocker;
            pixelCollider.Enabled                = true;
            pixelCollider.IsTrigger              = false;;
            pixelCollider.ColliderGenerationMode = PixelCollider.PixelColliderGeneration.Manual;
            pixelCollider.ManualOffsetX          = offset.x;
            pixelCollider.ManualOffsetY          = offset.y;
            pixelCollider.ManualWidth            = Mathf.CeilToInt(C.PIXELS_PER_TILE * sprite.GetBounds().size.x);
            pixelCollider.ManualHeight           = Mathf.CeilToInt(C.PIXELS_PER_TILE * sprite.GetBounds().size.y);
        SpeculativeRigidbody s = statue.AddComponent<SpeculativeRigidbody>();
            s.CanBePushed        = true;
            s.CanBeCarried       = true;
            s.CollideWithOthers  = true;
            s.CollideWithTileMap = false;
            s.PixelColliders     = new List<PixelCollider>{pixelCollider};
            s.Initialize();
        statue.AddComponent<GoldenDeath>();

        // if (enemy.aiActor.IsABoss()) // Unsure why this doesn't trigger normally, but this seems to fix it
        //     enemy.aiActor.ParentRoom.HandleRoomClearReward(); //TODO: it's possible non-boss room rewards also don't spawn if final enemy is midas'd...look into later
        LootEngine.SpawnCurrency(enemy.aiActor.CenterPosition, 1);
        enemy.aiActor.EraseFromExistenceWithRewards(true);
    }
}

public class GoldenDeath : MonoBehaviour, IPlayerInteractable
{
    private const float _START_EMIT    = 30.0f;
    private const float _MAX_EMIT      = 50.0f;
    private const float _MIN_EMIT      = 0.0f;
    private const float _GROW_TIME     = 0.25f;
    private const float _DECAY_TIME    = 0.5f;
    private const float _EXPLODE_TIME  = 0.75f;
    private const int   _NUM_PARTICLES = 10;
    private const float _PART_SPEED    = 2f;
    private const float _PART_SPREAD   = 0.5f;
    private const float _PART_LIFE     = 0.5f;
    private const float _PART_EMIT     = 20f;

    private float _lifetime;
    private bool _decaying;
    private tk2dSprite _sprite;
    private RoomHandler _room;
    private SpeculativeRigidbody _body;
    private bool _exploding;
    private PlayerController _midasOWner;

    private void Start()
    {
        this._lifetime = 0.0f;
        this._decaying = false;

        this._body = base.gameObject.GetComponent<SpeculativeRigidbody>();
        this._sprite = base.gameObject.GetComponent<tk2dSprite>();
        this._sprite.usesOverrideMaterial = true;
        this._sprite.renderer.material.shader = CwaffShaders.GoldShader;

        CwaffVFX.SpawnBurst(prefab: QuarterPounder._MidasParticleVFX, numToSpawn: _NUM_PARTICLES, basePosition: this._sprite.WorldCenter,
            positionVariance: _PART_SPREAD, baseVelocity: Vector2.zero, velocityVariance: _PART_SPEED, velType: CwaffVFX.Vel.Radial,
            rotType: CwaffVFX.Rot.Random, lifetime: _PART_LIFE, fadeOutTime: _PART_LIFE, emissivePower: _PART_EMIT, emissiveColor: Color.white);

        this._room = base.transform.position.GetAbsoluteRoom();
        if (this._room != null)
            this._room.RegisterInteractable(this);

        base.gameObject.Play("turn_to_gold");
    }

    private void Update()
    {
        float emit;
        this._sprite.UpdateZDepth();
        if (this._exploding)
        {
            this._lifetime += BraveTime.DeltaTime;
            if (this._lifetime >= _EXPLODE_TIME)
            {
                const int COINS = 20;
                const float GAP = 360f / COINS;
                for (int i = 0; i < COINS; ++i)
                {
                    Quaternion angle = (i * GAP).AddRandomSpread(0.5f * GAP).EulerZ();
                    Projectile p = SpawnManager.SpawnProjectile(QuarterPounder._GoldProjectile.gameObject, this._sprite.WorldCenter, angle).GetComponent<Projectile>();
                    p.baseData.speed *= UnityEngine.Random.Range(0.9f, 1.1f);
                    p.collidesWithPlayer = false;
                    p.Owner = this._midasOWner;
                    p.DestroyMode = Projectile.ProjectileDestroyMode.BecomeDebris;
                    p.sprite.usesOverrideMaterial = true;
                    p.sprite.renderer.material.shader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTiltedCutoutEmissive");
                    p.sprite.renderer.material.SetFloat("_EmissivePower", 50f);
                }
                base.gameObject.Play("midas_explode_sound");
                UnityEngine.Object.Destroy(base.gameObject);
                return;
            }
            emit = _MIN_EMIT + (_MAX_EMIT - _MIN_EMIT) * (this._lifetime / _EXPLODE_TIME);
            this._sprite.renderer.material.SetFloat("_EmissivePower", emit);
            return;
        }
        if (this._decaying)
        {
            if (this._lifetime >= _DECAY_TIME)
                return;
            this._lifetime = Mathf.Min(this._lifetime + BraveTime.DeltaTime, _DECAY_TIME);
            emit = _MAX_EMIT - (_MAX_EMIT - _MIN_EMIT) * (this._lifetime / _DECAY_TIME);
            this._sprite.renderer.material.SetFloat("_EmissivePower", emit);
            return;
        }
        this._lifetime = Mathf.Min(this._lifetime + BraveTime.DeltaTime, _GROW_TIME);
        emit = _START_EMIT + (_MAX_EMIT - _START_EMIT) * (this._lifetime / _GROW_TIME);
        this._sprite.renderer.material.SetFloat("_EmissivePower", emit);
        if (this._lifetime >= _GROW_TIME)
        {
            this._decaying = true;
            this._lifetime = 0.0f;
        }
    }

  public void OnEnteredRange(PlayerController interactor)
  {
    if (!this || this._exploding || !interactor.HasSynergy(Synergy.MASTERY_QUARTER_POUNDER))
      return;

    this._exploding = true;
    this._lifetime = 0.0f;
    this._midasOWner = interactor;
    base.gameObject.Play("midas_touch_sound");
  }

  public void OnExitRange(PlayerController interactor)
  {
    if (!this)
      return;
  }

  public float GetDistanceToPoint(Vector2 point)
  {
    if (!this)
      return 1000f;
    if (this._sprite == null)
      return 100f;
    Vector3 v = BraveMathCollege.ClosestPointOnRectangle(point, this._body.UnitBottomLeft, this._body.UnitDimensions);
    return Vector2.Distance(point, v) / 1.5f;
  }

  public float GetOverrideMaxDistance()
  {
    return -1f;
  }

  public string GetAnimationState(PlayerController interactor, out bool shouldBeFlipped)
  {
    shouldBeFlipped = false;
    return string.Empty;
  }

  public void Interact(PlayerController player)
  {
  }
}
