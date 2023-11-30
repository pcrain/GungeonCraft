namespace CwaffingTheGungy;

public class KingsLaw : AdvancedGunBehavior
{
    public static string ItemName         = "King's Law";
    public static string SpriteName       = "kings_law";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = "Accept Your Fate";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    internal const float _SPREAD      = 45.0f;
    internal const float _MIN_MAG     = 3.0f;
    internal const float _MAX_MAG     = 6.0f;
    internal const int   _LINES       = 5;
    internal const int   _LINE_SIZE   = 4;
    internal const int   _LINES_SIDE  = (int)((float)_LINES / 2.0f);
    internal const float _DLT_MAG     = (_MAX_MAG - _MIN_MAG) / (_LINES - 1);
    internal const int   _NUM_BULLETS = _LINES * _LINE_SIZE;
    internal const float _GAP         = (2f * _SPREAD / (_LINES - 1));
    internal const float _SPAWN_RATE  = 0.1f;

    internal static Projectile _KingsLawBullet;
    internal static List<Vector2> _OffsetAnglesAndMags = new(_NUM_BULLETS);
    internal static GameObject _RuneLarge;
    internal static GameObject _RuneSmall;

    private int _nextIndex = 0;
    private float _chargeTime = 0.0f;

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<KingsLaw>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.A, gunClass: GunClass.CHARGE, reloadTime: 0.75f, ammo: 700);
            gun.SetAnimationFPS(gun.shootAnimation, 24);
            gun.SetAnimationFPS(gun.reloadAnimation, 24);
            gun.SetMuzzleVFX("muzzle_iron_maid", fps: 30, scale: 0.5f, anchor: Anchor.MiddleLeft);
            gun.SetFireAudio("knife_gun_launch");
            gun.SetReloadAudio("knife_gun_reload");
            gun.AddToSubShop(ItemBuilder.ShopType.Trorc);

        gun.InitProjectile(new(clipSize: _NUM_BULLETS, shootStyle: ShootStyle.Charged, chargeTime: float.MaxValue, // absurdly high charge value so we never actually shoot
          shouldRotate: true/*, collidesWithTilemap: false*/));  // collidesWithTilemap doesn't actually work

        _KingsLawBullet = Items.Ak47.CloneProjectile(new(damage: 5.0f, speed: 40.0f, range: 30.0f
          )).AddAnimations(AnimatedBullet.Create(name: "kings_law_projectile", fps: 12, scale: 0.5f, anchor: Anchor.MiddleCenter)
          ).Attach<KingsLawBullets>();

        // Stagger projectile spawns alternating left and right from the starting angle
        int i = 0;
        for (float mag = _MIN_MAG; i++ < _LINE_SIZE; mag += _DLT_MAG)
        {
          int j = 0;
          for (float angle = 0f; j++ <= _LINES_SIDE; angle += _GAP)
          {
              _OffsetAnglesAndMags.Add(new Vector2(angle, mag));
              if (j > 1)
                _OffsetAnglesAndMags.Add(new Vector2(-angle, mag));
          }
        }

        _RuneLarge = VFX.Create("law_rune_large", fps: 2);
        _RuneSmall = VFX.Create("law_rune_small", fps: 2);
    }

    public override void OnReload(PlayerController player, Gun gun)
    {
        base.OnReload(player, gun);
        Reset();
    }

    public override void OnSwitchedToThisGun()
    {
        base.OnSwitchedToThisGun();
        Reset();
    }

    public override void OnDropped()
    {
        base.OnDropped();
        Reset();
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        base.OnSwitchedAwayFromThisGun();
        Reset();
    }

    private void Reset()
    {
        this._chargeTime = 0.0f;
        this._nextIndex = 0;
    }

    public int GetNextIndex()
    {
        return this._nextIndex++;
    }

    protected override void Update()
    {
        base.Update();
        if (BraveTime.DeltaTime == 0.0f)
            return;
        if (!this.gun.IsCharging)
        {
            Reset();
            return;
        }
        if (this.Owner is not PlayerController)
            return;

        if (this._nextIndex >= _NUM_BULLETS)
            return;

        this._chargeTime += BraveTime.DeltaTime;
        if (this._chargeTime > _SPAWN_RATE)
        {
            this._chargeTime -= _SPAWN_RATE;
            SpawnNextProjectile();
        }
    }

    private void SpawnNextProjectile()
    {
        int index = GetNextIndex();
        if (index >= _NUM_BULLETS || (this.gun.CurrentAmmo < 1 && !this.gun.InfiniteAmmo))
            return;

        if (!this.gun.InfiniteAmmo)
            this.gun.LoseAmmo(1);

        PlayerController player = this.Owner as PlayerController;
        VolleyUtility.ShootSingleProjectile(_KingsLawBullet, player.sprite.WorldCenter, player.m_currentGunAngle, false, player
          ).GetComponent<KingsLawBullets>().Setup(index);
    }
}

public class KingsLawBullets : MonoBehaviour
{
    private const float _TIME_BEFORE_STASIS     = 0.2f;
    private const float _GLOW_TIME              = 0.5f;
    private const float _GLOW_MAX               = 10f;
    private const float _LAUNCH_DELAY           = 0.04f;
    private const float _LAUNCH_SPEED           = 50f;
    private const float _RUNE_ALPHA             = 0.30f;
    private const float _RUNE_ROT_FAST          = 69.0f;
    private const float _RUNE_ROT_SLOW          = 47.0f;

    private PlayerController _owner;
    private Projectile       _projectile;
    private float            _moveTimer;
    private bool             _launchSequenceStarted;
    private bool             _wasEverInStasis;
    private int              _index;

    private GameObject       _runeLarge = null;
    private GameObject       _runeSmall = null;

    private float _offsetAngle = 0.0f;
    private float _offsetMag   = 0.0f;

    public void Setup(int index)
    {
        this._projectile            = base.GetComponent<Projectile>();
        this._owner                 = _projectile.Owner as PlayerController;
        this._launchSequenceStarted = false;
        this._wasEverInStasis       = false;
        this._index                 = index;

        Vector2 baseOffset = KingsLaw._OffsetAnglesAndMags[index];
        this._offsetAngle  = baseOffset.x;
        this._offsetMag    = baseOffset.y;

        this._projectile.specRigidbody.OnCollision += (_) => {
            AkSoundEngine.PostEvent("knife_gun_hit", this._projectile.gameObject);
        };

        AkSoundEngine.PostEvent("snd_undynedis", base.gameObject);

        StartCoroutine(TheLaw());
    }

    private void OnDestroy()
    {
        if (_runeLarge)
            UnityEngine.Object.Destroy(_runeLarge);
    }

    private IEnumerator TheLaw()
    {
        // Phase 1 / 5 -- the initial fire
        this._projectile.baseData.speed = 0.01f;
        this._projectile.UpdateSpeed();
        this._projectile.specRigidbody.CollideWithTileMap = false;
        this._projectile.specRigidbody.CollideWithOthers = false;
        this._projectile.specRigidbody.Reinitialize();
        this._runeLarge = SpawnManager.SpawnVFX(KingsLaw._RuneLarge, this._projectile.transform.position, Quaternion.identity);
            this._runeLarge.SetAlphaImmediate(_RUNE_ALPHA);
            this._runeLarge.transform.parent = this._projectile.transform;
        this._runeSmall = SpawnManager.SpawnVFX(KingsLaw._RuneSmall, this._projectile.transform.position, Quaternion.identity);
            this._runeSmall.SetAlphaImmediate(_RUNE_ALPHA);
            this._runeSmall.transform.parent = this._projectile.transform;
        for (float elapsed = 0f; elapsed < _TIME_BEFORE_STASIS; elapsed += BraveTime.DeltaTime)
        {
            float percentLeft        = 1f - elapsed / _TIME_BEFORE_STASIS;
            float cubicEase          = 1f - (percentLeft * percentLeft * percentLeft);
            float mag                = this._offsetMag * cubicEase;
            float behindPlayerAngle  = this._owner.m_currentGunAngle + 180f;
            Vector2 offset           = (behindPlayerAngle + this._offsetAngle).Clamp360().ToVector(mag);
            this._projectile.specRigidbody.Position = new Position(this._owner.CenterPosition + offset);
            this._projectile.SendInDirection(this._owner.m_currentGunAngle.ToVector(), resetDistance: true);
            this._projectile.transform.localRotation = this._owner.m_currentGunAngle.EulerZ();
            this._projectile.specRigidbody.UpdateColliderPositions();
            this._runeLarge.transform.localRotation = (_RUNE_ROT_FAST * BraveTime.ScaledTimeSinceStartup).EulerZ();
            this._runeSmall.transform.localRotation = (-_RUNE_ROT_SLOW * BraveTime.ScaledTimeSinceStartup).EulerZ();
            yield return null;
        }

        // Phase 2 / 5 -- the freeze
        while (this._owner && this._owner.CurrentGun?.GetComponent<KingsLaw>() && (this._owner.CurrentGun?.IsCharging ?? false))
        {
            float behindPlayerAngle = this._owner.m_currentGunAngle + 180f;
            Vector2 offset          = (behindPlayerAngle + this._offsetAngle).Clamp360().ToVector(this._offsetMag);
            this._projectile.specRigidbody.Position = new Position(this._owner.CenterPosition + offset);
            this._projectile.SendInDirection(this._owner.m_currentGunAngle.ToVector(), resetDistance: true);
            this._projectile.transform.localRotation = this._owner.m_currentGunAngle.EulerZ();
            this._projectile.specRigidbody.UpdateColliderPositions();
            this._runeLarge.transform.localRotation = (_RUNE_ROT_FAST * BraveTime.ScaledTimeSinceStartup).EulerZ();
            this._runeSmall.transform.localRotation = (-_RUNE_ROT_SLOW * BraveTime.ScaledTimeSinceStartup).EulerZ();
            yield return null;
        }

        // Phase 3 / 5 -- the glow
        float targetAngle                = this._owner.m_currentGunAngle;
        Vector2 targetDir                = targetAngle.ToVector();
        this._runeLarge.transform.parent = null;
        this._runeSmall.transform.parent = null;
        this._projectile.sprite.usesOverrideMaterial = true;
        Material m = this._projectile.sprite.renderer.material;
            m.shader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTintableTiltedCutoutEmissive");
            m.SetFloat("_EmissivePower", 0f);
            m.SetFloat("_EmissiveColorPower", 1.55f);
            m.SetColor("_EmissiveColor", Color.cyan);
        AkSoundEngine.PostEvent("knife_gun_glow_stop_all", this._projectile.gameObject);
        AkSoundEngine.PostEvent("knife_gun_glow", this._projectile.gameObject);
        this._moveTimer = _GLOW_TIME;
        while (this._moveTimer > 0)
        {
            float runeAlpha = _RUNE_ALPHA * this._moveTimer / _GLOW_TIME;
            this._runeLarge.SetAlpha(runeAlpha);
            this._runeSmall.SetAlpha(runeAlpha);
            float glowAmount = (_GLOW_TIME - this._moveTimer) / _GLOW_TIME;
            m.SetFloat("_EmissivePower", glowAmount * _GLOW_MAX);
            this._moveTimer -= BraveTime.DeltaTime;
            yield return null;
        }
        this._runeLarge.SetAlpha(0f);
        this._runeSmall.SetAlpha(0f);

        // Phase 4 / 5 -- the launch queue
        this._moveTimer = _LAUNCH_DELAY * this._index;
        while (this._moveTimer > 0)
        {
            this._moveTimer -= BraveTime.DeltaTime;
            yield return null;
        }

        // Phase 5 / 5 -- the launch
        this._projectile.baseData.speed = _LAUNCH_SPEED;
        this._projectile.specRigidbody.CollideWithOthers = true;
        this._projectile.specRigidbody.Reinitialize();
        _projectile.SendInDirection(targetDir, true);
        _projectile.UpdateSpeed();
        AkSoundEngine.PostEvent("knife_gun_launch", this._projectile.gameObject);

        // Post-launch: wait for the projectiles to pass the player's original point at their launch, then re-enable tile collision
        while (this._owner && (this._projectile?.isActiveAndEnabled ?? false))
        {
            float angleToPlayer = (this._projectile.transform.position.XY() - this._owner.transform.position.XY()).ToAngle();
            if (angleToPlayer.IsNearAngle(targetAngle, 90f))
                break;
            yield return null;
        }
        this._projectile.specRigidbody.CollideWithTileMap = true;

        yield break;
    }
}
