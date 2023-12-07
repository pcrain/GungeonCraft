namespace CwaffingTheGungy;

public class AimuHakurei : AdvancedGunBehavior
{
    public static string ItemName         = "Aimu Hakurei";
    public static string SpriteName       = "aimu_hakurei";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = "Highly Responsive";
    public static string LongDescription  = "Fires a variety of projectiles based on its current level. Grazing nearby enemy projectiles while the gun is active increases the graze counter, with the gun's level increasing at 10, 30, 60, and 100 graze. Reloading toggles focus mode, which slows down time to enable precision grazing. Focus mode is cancelled by reloading, firing, dodge rolling, or switching guns.";
    public static string Lore             = "One of the finest weapons ever crafted in Gunsokyo, a land whose denizens are renowned for their otherworldly bullet-dodging abilities that would put most Gungeoneers to shame. The potential dakka output of this gun is enough to keep up even with these impressive abilities. However, the Gunsokyo warriors being the showboats that they are, reaching this gun's full potential requires placing oneself in some rather precarious situations, making it a weapon of truly ludicrous risk and reward.";

    internal const float _GRAZE_THRES              = 1.5f;  // max distance from player a projectile can be to count as grazed
    internal const float _GRAZE_THRES_SQUARED      = _GRAZE_THRES * _GRAZE_THRES; // speed up distance calculations a bit
    internal const float _GRAZE_DECAY_RATE         = 0.75f; // seconds between our graze counter decreasing
    internal const float _GRAZE_COOLDOWN           = 1.0f;  // cooldown to prevent projectiles from being infinitely grazed
    internal const int   _MAX_GRAZE_PER_PROJECTILE = 5;     // max amount of graze we can accumulate per projectile
    internal const int   _GRAZE_MAX                = 120;   // max amount of graze we can accumulate overall

    internal static readonly int[] _GRAZE_TIER_THRESHOLDS  = {10, 30, 60, 100};

    internal static tk2dSpriteAnimationClip _BulletSprite = null;
    internal static Projectile _ProjBase;
    internal static GameObject _GrazeVFX      = null;

    public int graze = 0;

    private float _lastDecayTime = 0f;
    private Coroutine _decayCoroutine = null;
    private bool _focused = false;

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<AimuHakurei>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.B, gunClass: GunClass.FULLAUTO, reloadTime: 0.0f,
                ammo: 200, /*infiniteAmmo: true,*/ canGainAmmo: false, canReloadNoMatterAmmo: true);
            gun.Volley.ModulesAreTiers = true;
            gun.SetAnimationFPS(gun.shootAnimation, 60);
            gun.SetMuzzleVFX("muzzle_aimu", fps: 30, scale: 0.3f, anchor: Anchor.MiddleCenter);
            gun.AddToSubShop(ModdedShopType.TimeTrader);

        _BulletSprite = AnimatedBullet.Create(name: "aimu_projectile", fps: 2, scale: 0.625f, anchor: Anchor.MiddleCenter);

        _GrazeVFX = VFX.Create("graze_vfx", fps: 5, loops: true, anchor: Anchor.MiddleCenter, scale: 1.0f, emissivePower: 5f);

        _ProjBase = gun.InitFirstProjectile(new(damage: 8f, speed: 44f, range: 100f, force: 3f));

        Projectile beamProj = Items._38Special.CloneProjectile(new(damage: 16.0f, speed: 300.0f));
            TrailController tc = beamProj.AddTrailToProjectilePrefab(ResMap.Get("aimu_beam_mid")[0], new Vector2(25, 39), new Vector2(0, 0),
                ResMap.Get("aimu_beam_mid"), 60, ResMap.Get("aimu_beam_start"), 60, cascadeTimer: C.FRAME, destroyOnEmpty: true);
                tc.UsesDispersalParticles = true;
                tc.DispersalParticleSystemPrefab = (ItemHelper.Get(Items.FlashRay) as Gun).DefaultModule.projectiles[0].GetComponentInChildren<TrailController>().DispersalParticleSystemPrefab;
            beamProj.SetAllImpactVFX(VFX.CreatePool("gaster_beam_impact", fps: 20, loops: false, scale: 1.0f, anchor: Anchor.MiddleCenter));

        // set up tiered projectiles
        gun.Volley.projectiles = new(){
            // Tier 0 / Level 1
            AimuMod(level: 1, fireRate: 16, projectiles: new(){
                AimuProj(invert: false, amplitude: 0.0f, sound: "aimu_shoot_sound",      trailWidth: 0.2f),
                }),
            // Tier 1 / Level 2
            AimuMod(level: 2, fireRate: 12, projectiles: new(){
                AimuProj(invert: false, amplitude: 0.75f, sound: "aimu_shoot_sound",     trailWidth: 0.3f),
                AimuProj(invert: true,  amplitude: 0.75f, sound: "aimu_shoot_sound",     trailWidth: 0.3f),
                }),
            // Tier 2 / Level 3
            AimuMod(level: 3, fireRate: 8, projectiles: new(){
                AimuProj(invert: false, amplitude: 0.75f, sound: "aimu_shoot_sound",     trailWidth: 0.4f),
                AimuProj(invert: true,  amplitude: 0.75f, sound: "aimu_shoot_sound",     trailWidth: 0.4f),
                }),
            // Tier 3 / Level 4
            AimuMod(level: 4, fireRate: 4, projectiles: new(){
                AimuProj(invert: false, amplitude: 0.75f, sound: "aimu_shoot_sound",     trailWidth: 0.5f),
                AimuProj(invert: true,  amplitude: 0.75f, sound: "aimu_shoot_sound",     trailWidth: 0.5f),
                AimuProj(invert: false, amplitude: 2.25f, sound: "aimu_shoot_sound_alt", trailWidth: 0.5f, trailColor: Color.white),
                AimuProj(invert: true,  amplitude: 2.25f, sound: "aimu_shoot_sound_alt", trailWidth: 0.5f, trailColor: Color.white),
                }),
            // Tier 4 / Level 5
            AimuMod(level: 5, fireRate: 2, projectiles: new(){
                AimuProj(invert: false, amplitude: 0.75f, sound: "aimu_shoot_sound",     trailWidth: 0.5f),
                AimuProj(invert: true,  amplitude: 0.75f, sound: "aimu_shoot_sound",     trailWidth: 0.5f),
                AimuProj(invert: false, amplitude: 2.25f, sound: "aimu_shoot_sound_alt", trailWidth: 0.5f, trailColor: Color.white),
                AimuProj(invert: true,  amplitude: 2.25f, sound: "aimu_shoot_sound_alt", trailWidth: 0.5f, trailColor: Color.white),
                beamProj,
                }),
        };

        gun.gameObject.AddComponent<AimuHakureiAmmoDisplay>();
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        SetFocus(false);
        if (projectile.GetComponentInChildren<TrailController>())
        {
            AkSoundEngine.PostEvent("aimu_beam_sound_2_stop_all", this.Owner.gameObject);
            AkSoundEngine.PostEvent("aimu_beam_sound_2", this.Owner.gameObject);
        }
    }

    protected override void OnPickedUpByPlayer(PlayerController player)
    {
        base.OnPickedUpByPlayer(player);
        this.graze                   = 0; // reset graze when dropped
        this.gun.CurrentStrengthTier = 0;
        SetFocus(false);
        player.OnRollStarted += this.OnDodgeRoll;
    }

    protected override void OnPostDroppedByPlayer(PlayerController player)
    {
        base.OnPostDroppedByPlayer(player);
        player.OnRollStarted -= this.OnDodgeRoll;
    }

    private void OnDodgeRoll(PlayerController player, Vector2 dirVec)
    {
        SetFocus(false);
    }

    public override void OnSwitchedToThisGun()
    {
        base.OnSwitchedToThisGun();
        if (this._decayCoroutine != null)
        {
            StopCoroutine(this._decayCoroutine);
            this._decayCoroutine = null;
        }
        SetFocus(false);
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        base.OnSwitchedAwayFromThisGun();
        if (this._decayCoroutine == null)
            this._decayCoroutine = this.Owner.StartCoroutine(DecayWhileInactive());
        SetFocus(false);
    }

    public override void OnReloadPressed(PlayerController player, Gun gun, bool manualReload)
    {
        base.OnReloadPressed(player, gun, manualReload);
        if (!player.IsDodgeRolling && player.AcceptingNonMotionInput)
            SetFocus(!this._focused);
    }

    private void SetFocus(bool focus)
    {
        if (focus == this._focused)
            return;
        this._focused = focus;
        this.gun.CanBeDropped = !focus;
        BraveTime.SetTimeScaleMultiplier(focus ? 0.65f : 1.0f, base.gameObject);
        if (this._focused)
            AkSoundEngine.PostEvent("aimu_focus_sound", this.Owner.gameObject);

        this.gun.RemoveStatFromGun(PlayerStats.StatType.MovementSpeed);
        // NOTE: since time is slowed down, the player's effective speed is 0.65 * 0.65. This is intentional
        this.gun.AddStatToGun(PlayerStats.StatType.MovementSpeed, focus ? 0.65f : 1.0f, StatModifier.ModifyMethod.MULTIPLICATIVE);
        this.Player.stats.RecalculateStats(this.Player);
    }

    private IEnumerator DecayWhileInactive()
    {
        while (this.gameObject != null)
        {
            if (!GameManager.Instance.IsPaused && !GameManager.Instance.IsLoadingLevel)
                UpdateGraze();
            yield return null;
        }
    }

    private static ProjectileModule AimuMod(List<Projectile> projectiles, float fireRate, int level)
    {
        ProjectileModule mod = new ProjectileModule().SetAttributes(new(
            ammoCost: 0, clipSize: -1, cooldown: C.FRAME * fireRate, angleVariance: 15f - (2 * level),
            shootStyle: ShootStyle.Burst, sequenceStyle: ProjectileSequenceStyle.Ordered, customClip: SpriteName));
        mod.projectiles         = projectiles;
        mod.burstShotCount      = mod.projectiles.Count();
        mod.burstCooldownTime   = C.FRAME * fireRate;
        return mod;
    }

    private static Projectile AimuProj(bool invert, float amplitude, string sound, float trailWidth, Color? trailColor = null)
    {
        Projectile proj = _ProjBase.ClonePrefab<Projectile>();
            proj.AddDefaultAnimation(_BulletSprite);
            proj.gameObject.AddComponent<AimuHakureiProjectileBehavior>()
                .Setup(invert: invert, amplitude: amplitude, sound: sound);
            AddTrail(proj, trailWidth, trailColor);
        return proj;
    }

    private static void AddTrail(Projectile p, float trailWidth, Color? trailColor = null)
    {
        EasyTrailBullet trail = p.gameObject.AddComponent<EasyTrailBullet>();
            trail.TrailPos   = p.transform.position.XY() + new Vector2(5f / C.PIXELS_PER_TILE, 5f / C.PIXELS_PER_TILE); // offset by middle of the sprite
            trail.StartWidth = trailWidth;
            trail.EndWidth   = 0.05f;
            trail.LifeTime   = 0.1f;
            trail.BaseColor  = trailColor ?? Color.Lerp(Color.magenta, Color.red, 0.5f);
            trail.EndColor   = trailColor ?? Color.Lerp(Color.magenta, Color.red, 0.5f);
    }

    protected override void Update()
    {
        base.Update();
        if (this.Owner is PlayerController pc && pc.CurrentInputState != PlayerInputState.AllInput)
            SetFocus(false);
        UpdateGraze();
    }

    private void PowerUp()
    {
        ++this.gun.CurrentStrengthTier;
        AkSoundEngine.PostEvent("aimu_power_up_sound", this.Owner.gameObject);
        this.gun.gameObject.SetGlowiness(this.gun.CurrentStrengthTier * this.gun.CurrentStrengthTier);
    }

    private void PowerDown()
    {
        --this.gun.CurrentStrengthTier;
        this.gun.gameObject.SetGlowiness(this.gun.CurrentStrengthTier * this.gun.CurrentStrengthTier);
    }

    private static Dictionary<Projectile, int> _GrazeDict = new();
    private static Dictionary<Projectile, float> _GrazeTimeDict = new();
    private void UpdateGraze()
    {
        if (this.Owner is not PlayerController pc)
            return; // if our owner isn't a player, we have nothing to do

        if (this.graze > 0 && (this._lastDecayTime + _GRAZE_DECAY_RATE <= BraveTime.ScaledTimeSinceStartup))
        {
            --this.graze;
            if (this.gun.CurrentStrengthTier > 0 && graze < _GRAZE_TIER_THRESHOLDS[this.gun.CurrentStrengthTier-1])
                PowerDown();
            this._lastDecayTime = BraveTime.ScaledTimeSinceStartup;
        }

        if (!pc.healthHaver.IsVulnerable)
            return; // can't graze if we're invincible, that's cheating!!!

        if (pc.CurrentGun != this.gun)
            return;// if this isn't our active gun, we can't benefit from grazing

        Vector2 ppos = pc.sprite.WorldCenter;
        Vector2 bottom = pc.sprite.WorldBottomCenter;
        foreach (Projectile p in StaticReferenceManager.AllProjectiles)
        {
            if (!p.isActiveAndEnabled || !p.sprite.renderer.enabled || !p.collidesWithPlayer || p.Owner == this.Owner)
                continue; // if the projectile can't collide with us, we're not impressed

            if (p.sprite?.WorldCenter is not Vector2 epos)
                continue; // don't care about projectiles without sprites

            if ((epos-ppos).sqrMagnitude >= _GRAZE_THRES_SQUARED)
                continue; // bullet's too far away, so doesn't need to be considered

            // Shenanigans to make sure pooled projectiles don't count as already-grazed when they respawn from the pool
            if (!_GrazeTimeDict.ContainsKey(p))
                _GrazeTimeDict[p] = 0;
            if (_GrazeTimeDict[p] + _GRAZE_COOLDOWN < BraveTime.ScaledTimeSinceStartup)
                _GrazeDict[p] = 0; // reset our grazedict timer if we haven't been near it for at least one second
            _GrazeTimeDict[p] = BraveTime.ScaledTimeSinceStartup;
            if (_GrazeDict[p] >= _MAX_GRAZE_PER_PROJECTILE)
                continue; // we've already grazed the bullet a bunch, so put it on cooldown

            ++_GrazeDict[p];
            if (++this.graze > _GRAZE_MAX)
                this.graze = _GRAZE_MAX;

            // Vector2 finalpos = ppos + BraveMathCollege.DegreesToVector(Lazy.RandomAngle());
            FancyVFX.Spawn(AimuHakurei._GrazeVFX, bottom, Quaternion.identity, parent: pc.sprite?.transform,
                velocity: 5f * Vector2.up, lifetime: 0.2f, fadeOutTime: 0.4f, /*emissivePower: 50f,*/ emissiveColor: Color.white);

            if (this.gun.CurrentStrengthTier < _GRAZE_TIER_THRESHOLDS.Count() && graze >= _GRAZE_TIER_THRESHOLDS[this.gun.CurrentStrengthTier])
                PowerUp();
        }
    }
}

public class AimuHakureiAmmoDisplay : CustomAmmoDisplay
{
    private Gun _gun;
    private AimuHakurei _aimu;
    private PlayerController _owner;
    private void Start()
    {
        this._gun = base.GetComponent<Gun>();
        this._aimu = this._gun.GetComponent<AimuHakurei>();
        this._owner = this._gun.CurrentOwner as PlayerController;
    }

    public override bool DoCustomAmmoDisplay(GameUIAmmoController uic)
    {
        if (!this._owner)
            return false;

        if (this._gun.CurrentStrengthTier < 4)
        {
            uic.SetAmmoCountLabelColor(Color.magenta);
            uic.GunAmmoCountLabel.Text = $"{this._aimu.graze} - L{1 + this._gun.CurrentStrengthTier}";
        }
        else
        {
            float phase = Mathf.Sin(4f * BraveTime.ScaledTimeSinceStartup);
            uic.SetAmmoCountLabelColor(Color.Lerp(Color.magenta, Color.cyan, Mathf.Abs(phase)));
            uic.GunAmmoCountLabel.Text = $"{this._aimu.graze} - MAX";
        }
        return true;
    }
}

public class AimuHakureiProjectileBehavior : MonoBehaviour
{
    private Projectile _projectile;
    private PlayerController _owner;
    private AimuHakureiProjectileMotionModule _aimu;

    // must be public or it won't serialize in prefab
    public bool invert;
    public float amplitude;
    public string sound;

    public void Setup(bool invert, float amplitude, string sound)
    {
        this.invert = invert;
        this.amplitude = amplitude;
        this.sound = sound;
    }

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._owner = this._projectile.Owner as PlayerController;
        this._aimu = new AimuHakureiProjectileMotionModule();
            this._aimu.ForceInvert = this.invert;
            this._aimu.amplitude = this.amplitude;
        this._projectile.OverrideMotionModule = this._aimu;

        AkSoundEngine.PostEvent(this.sound, base.gameObject);
    }
}

// modified from HelixProjectileMotionModule
public class AimuHakureiProjectileMotionModule : ProjectileMotionModule
{
    public float wavelength = 8f;
    public float amplitude = 0f;
    public bool ForceInvert;

    private bool _initialized;
    private Vector2 _initialRightVector;
    private Vector2 _initialUpVector;
    private Vector2 _privateLastPosition;
    private float _xDisplacement;
    private float _yDisplacement;

    public override void UpdateDataOnBounce(float angleDiff)
    {
        if (float.IsNaN(angleDiff))
            return;

        _initialUpVector = Quaternion.Euler(0f, 0f, angleDiff) * _initialUpVector;
        _initialRightVector = Quaternion.Euler(0f, 0f, angleDiff) * _initialRightVector;
    }

    public override void AdjustRightVector(float angleDiff)
    {
        if (float.IsNaN(angleDiff))
            return;

        _initialUpVector = Quaternion.Euler(0f, 0f, angleDiff) * _initialUpVector;
        _initialRightVector = Quaternion.Euler(0f, 0f, angleDiff) * _initialRightVector;
    }

    private void Initialize(Vector2 lastPosition, Transform projectileTransform, float m_timeElapsed, Vector2 m_currentDirection, bool shouldRotate)
    {
        _privateLastPosition = lastPosition;
        _initialRightVector  = ((!shouldRotate) ? m_currentDirection : projectileTransform.right.XY());
        _initialUpVector     = ((!shouldRotate) ? (Quaternion.Euler(0f, 0f, 90f) * m_currentDirection) : projectileTransform.up);
        _initialized         = true;
        _xDisplacement       = 0f;
        _yDisplacement       = 0f;
        m_timeElapsed        = 0f;
    }

    public override void Move(Projectile source, Transform projectileTransform, tk2dBaseSprite projectileSprite, SpeculativeRigidbody specRigidbody, ref float m_timeElapsed, ref Vector2 m_currentDirection, bool Inverted, bool shouldRotate)
    {
        ProjectileData baseData = source.baseData;
        Vector2 oldPos = ((!projectileSprite) ? projectileTransform.position.XY() : projectileSprite.WorldCenter);
        if (!_initialized)
            Initialize(oldPos, projectileTransform, m_timeElapsed, m_currentDirection, shouldRotate);
        m_timeElapsed   += BraveTime.DeltaTime;
        int invertSign           = ((!(Inverted ^ ForceInvert)) ? 1 : (-1));
        float phaseAngle         = (float)Math.PI * baseData.speed / wavelength;
        float newDisplacementX   = m_timeElapsed * baseData.speed;
        float newDisplacementY   = (float)invertSign * amplitude * Mathf.Sin(m_timeElapsed * phaseAngle);
        float deltaDisplacementX = newDisplacementX - _xDisplacement;
        float deltaDisplacementY = newDisplacementY - _yDisplacement;
        Vector2 newPos           = (_privateLastPosition = _privateLastPosition + _initialRightVector * deltaDisplacementX + _initialUpVector * deltaDisplacementY);
        if (shouldRotate)
        {
            float futureDisplacementY = (float)invertSign * amplitude * Mathf.Sin((m_timeElapsed + 0.01f) * phaseAngle);
            float angleFromStart = BraveMathCollege.Atan2Degrees(futureDisplacementY - newDisplacementY, 0.01f * baseData.speed);
            projectileTransform.localRotation = Quaternion.Euler(0f, 0f, angleFromStart + _initialRightVector.ToAngle());
        }
        Vector2 velocity = (newPos - oldPos) / BraveTime.DeltaTime;
        if (!float.IsNaN(BraveMathCollege.Atan2Degrees(velocity)))
            m_currentDirection = velocity.normalized;
        _xDisplacement        = newDisplacementX;
        _yDisplacement        = newDisplacementY;
        specRigidbody.Velocity = velocity;
    }

    public override void SentInDirection(ProjectileData baseData, Transform projectileTransform, tk2dBaseSprite projectileSprite, SpeculativeRigidbody specRigidbody, ref float m_timeElapsed, ref Vector2 m_currentDirection, bool shouldRotate, Vector2 dirVec, bool resetDistance, bool updateRotation)
    {
        Initialize(((!projectileSprite) ? projectileTransform.position.XY() : projectileSprite.WorldCenter), projectileTransform, m_timeElapsed, m_currentDirection, shouldRotate);
    }
}
