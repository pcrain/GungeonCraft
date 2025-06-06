namespace CwaffingTheGungy;

public class AimuHakurei : CwaffGun
{
    public static string ItemName         = "Aimu Hakurei";
    public static string ShortDescription = "Highly Responsive";
    public static string LongDescription  = "Fires a variety of projectiles based on its current level. Passively grants the ability to graze enemy projectiles while in the inventory, with the gun's level increasing at 10, 30, 60, and 100 graze. Reloading toggles focus mode, which slows down time to enable precision grazing. Focus mode is cancelled by reloading, firing, dodge rolling, or switching guns. Graze naturally decays over time and cannot be gained while invulnerable.";
    public static string Lore             = "One of the finest weapons ever crafted in Gunsokyo, a land whose denizens are renowned for their otherworldly bullet-dodging abilities that would put most Gungeoneers to shame. The potential dakka output of this gun is enough to keep up even with these impressive abilities. However, the Gunsokyo warriors being the showboats that they are, reaching this gun's full potential requires placing oneself in some rather precarious situations, making it a weapon of truly ludicrous risk and reward.";

    internal const float _GRAZE_THRES              = 1.5f;  // max distance from player a projectile can be to count as grazed
    internal const float _GRAZE_THRES_SQUARED      = _GRAZE_THRES * _GRAZE_THRES; // speed up distance calculations a bit
    internal const float _GRAZE_DECAY_RATE         = 0.75f; // seconds between our graze counter decreasing
    internal const float _GRAZE_COOLDOWN           = 1.0f;  // cooldown to prevent projectiles from being infinitely grazed
    internal const int   _GRAZE_PER_PROJECTILE     = 5;     // max amount of graze we can accumulate per projectile
    internal const int   _GRAZE_MAX                = 120;   // max amount of graze we can accumulate overall

    internal static readonly int[] _GRAZE_TIER_THRESHOLDS  = {10, 30, 60, 100};

    internal static Projectile _ProjBase;
    internal static GameObject _GrazeVFX;

    public int graze = 0;

    private float _lastDecayTime = 0f;
    private bool _focused = false;

    public static void Init()
    {
        Lazy.SetupGun<AimuHakurei>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.B, gunClass: GunClass.FULLAUTO, reloadTime: 0.0f, infiniteAmmo: true,
            ammo: 200, canGainAmmo: false, canReloadNoMatterAmmo: true, modulesAreTiers: true, shootFps: 60, muzzleVFX: "muzzle_aimu",
            muzzleFps: 30, muzzleScale: 0.3f, muzzleAnchor: Anchor.MiddleCenter)
          .Attach<AimuHakureiAmmoDisplay>()
          .AddToShop(ModdedShopType.TimeTrader)
          .AssignGun(out Gun gun);

        _ProjBase = gun.InitFirstProjectile(GunData.New(damage: 8f, speed: 44f, range: 100f, force: 3f, sprite: "aimu_projectile", scale: 0.625f));
        Projectile beamProj = Items._38Special.CloneProjectile(GunData.New(damage: 16.0f, speed: 300.0f, spawnSound: "aimu_beam_sound_2"))
          .SetAllImpactVFX(VFX.CreatePool("aimu_beam_impact", fps: 20, loops: false))
          .AttachTrail("aimu_beam_mid", fps: 60, startAnim: "aimu_beam_start", softMaxLength: 1f, cascadeTimer: C.FRAME, destroyOnEmpty: true,
            dispersalPrefab: Items.FlashRay.AsGun().DefaultModule.projectiles[0].GetComponentInChildren<TrailController>().DispersalParticleSystemPrefab);

        // set up tiered projectiles
        gun.Volley.projectiles = new(){
            // Tier 0 / Level 1
            AimuMod(level: 1, fireRate: 16, gun: gun, projectiles: new(){
                AimuProj(invert: false, amplitude: 0.0f, sound: "aimu_shoot_sound",      trailWidth: 0.2f),
                }),
            // Tier 1 / Level 2
            AimuMod(level: 2, fireRate: 12, gun: gun, projectiles: new(){
                AimuProj(invert: false, amplitude: 0.75f, sound: "aimu_shoot_sound",     trailWidth: 0.3f),
                AimuProj(invert: true,  amplitude: 0.75f, sound: "aimu_shoot_sound",     trailWidth: 0.3f),
                }),
            // Tier 2 / Level 3
            AimuMod(level: 3, fireRate: 8, gun: gun, projectiles: new(){
                AimuProj(invert: false, amplitude: 0.75f, sound: "aimu_shoot_sound",     trailWidth: 0.4f),
                AimuProj(invert: true,  amplitude: 0.75f, sound: "aimu_shoot_sound",     trailWidth: 0.4f),
                }),
            // Tier 3 / Level 4
            AimuMod(level: 4, fireRate: 4, gun: gun, projectiles: new(){
                AimuProj(invert: false, amplitude: 0.75f, sound: "aimu_shoot_sound",     trailWidth: 0.5f),
                AimuProj(invert: true,  amplitude: 0.75f, sound: "aimu_shoot_sound",     trailWidth: 0.5f),
                AimuProj(invert: false, amplitude: 2.25f, sound: "aimu_shoot_sound_alt", trailWidth: 0.5f, trailColor: Color.white),
                AimuProj(invert: true,  amplitude: 2.25f, sound: "aimu_shoot_sound_alt", trailWidth: 0.5f, trailColor: Color.white),
                }),
            // Tier 4 / Level 5
            AimuMod(level: 5, fireRate: 2, gun: gun, projectiles: new(){
                AimuProj(invert: false, amplitude: 0.75f, sound: "aimu_shoot_sound",     trailWidth: 0.5f),
                AimuProj(invert: true,  amplitude: 0.75f, sound: "aimu_shoot_sound",     trailWidth: 0.5f),
                AimuProj(invert: false, amplitude: 2.25f, sound: "aimu_shoot_sound_alt", trailWidth: 0.5f, trailColor: Color.white),
                AimuProj(invert: true,  amplitude: 2.25f, sound: "aimu_shoot_sound_alt", trailWidth: 0.5f, trailColor: Color.white),
                beamProj,
                }),
        };

        _GrazeVFX = VFX.Create("graze_vfx", fps: 5, emissivePower: 5f);
    }

    public override void OnPostFired(PlayerController player, Gun gun)
    {
        base.OnPostFired(player, gun);
        SetFocus(false);
    }

    public override void OnPlayerPickup(PlayerController player)
    {
        base.OnPlayerPickup(player);
        this.graze                   = 0; // reset graze when dropped
        this.gun.CurrentStrengthTier = 0;
        SetFocus(false);
        player.OnRollStarted += this.OnDodgeRoll;
        player.OnReceivedDamage += this.OnReceivedDamage;
    }

    private void OnReceivedDamage(PlayerController player)
    {
        if (!player.HasSynergy(Synergy.LOTUS_LAND_STORY))
            return;
        this.graze = _GRAZE_MAX;
        PowerUp();
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        base.OnDroppedByPlayer(player);
        player.OnRollStarted -= this.OnDodgeRoll;
        player.OnReceivedDamage -= this.OnReceivedDamage;
    }

    public override void OnDestroy()
    {
        if (this.PlayerOwner)
        {
            this.PlayerOwner.OnRollStarted -= this.OnDodgeRoll;
            this.PlayerOwner.OnReceivedDamage -= this.OnReceivedDamage;
        }
        base.OnDestroy();
    }

    private void OnDodgeRoll(PlayerController player, Vector2 dirVec)
    {
        SetFocus(false);
    }

    public override void OnSwitchedToThisGun()
    {
        base.OnSwitchedToThisGun();
        SetFocus(false);
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        base.OnSwitchedAwayFromThisGun();
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
        BraveTime.SetTimeScaleMultiplier(focus ? (this.Mastered ? 0.4f : 0.65f) : 1.0f, base.gameObject);
        if (this._focused)
            this.PlayerOwner.gameObject.Play("aimu_focus_sound");

        this.gun.RemoveStatFromGun(StatType.MovementSpeed);
        // NOTE: since time is slowed down, the player's effective speed is 0.65 * 0.65. This is intentional
        this.gun.AddStatToGun(StatType.MovementSpeed.Mult((focus && !this.Mastered) ? 0.65f : 1.0f));
        this.PlayerOwner.stats.RecalculateStats(this.PlayerOwner);
    }

    private static ProjectileModule AimuMod(List<Projectile> projectiles, float fireRate, int level, Gun gun)
    {
        return
          new ProjectileModule() {
            projectiles       = projectiles,
            burstShotCount    = projectiles.Count,
            burstCooldownTime = C.FRAME * fireRate }
          .SetAttributes(GunData.New(gun: gun, ammoCost: 0, clipSize: -1, cooldown: C.FRAME * fireRate, angleVariance: 15f - (2 * level),
            shootStyle: ShootStyle.Burst, sequenceStyle: ProjectileSequenceStyle.Ordered, customClip: true));
    }

    private static Projectile AimuProj(bool invert, float amplitude, string sound, float trailWidth, Color? trailColor = null)
    {
        Projectile proj = _ProjBase.ClonePrefab<Projectile>();
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

    public override void Update()
    {
        base.Update();
        if (this.PlayerOwner is PlayerController pc && pc.CurrentInputState != PlayerInputState.AllInput)
            SetFocus(false);
    }

    public override void OwnedUpdatePlayer(PlayerController player, GunInventory inventory)
    {
        base.OwnedUpdatePlayer(player, inventory);
        UpdateGraze();
    }

    private void PowerUp()
    {
        while (this.gun.CurrentStrengthTier < _GRAZE_TIER_THRESHOLDS.Length && this.graze >= _GRAZE_TIER_THRESHOLDS[this.gun.CurrentStrengthTier])
        {
            ++this.gun.CurrentStrengthTier;
            this.PlayerOwner.gameObject.PlayOnce("aimu_power_up_sound");
            this.gun.gameObject.SetGlowiness(this.gun.CurrentStrengthTier * this.gun.CurrentStrengthTier);
        }
    }

    private void PowerDown()
    {
        while (this.gun.CurrentStrengthTier > 0 && this.graze < _GRAZE_TIER_THRESHOLDS[this.gun.CurrentStrengthTier - 1])
        {
            --this.gun.CurrentStrengthTier;
            this.gun.gameObject.SetGlowiness(this.gun.CurrentStrengthTier * this.gun.CurrentStrengthTier);
        }
    }

    private static LinkedList<Projectile> _GrazeCandidates = new();
    private void UpdateGraze()
    {
        if (!this || this.PlayerOwner is not PlayerController pc)
            return; // if our owner isn't a player, we have nothing to do

        if (this.graze > 0 && (this._lastDecayTime + _GRAZE_DECAY_RATE <= BraveTime.ScaledTimeSinceStartup))
        {
            --this.graze;
            PowerDown();
            this._lastDecayTime = BraveTime.ScaledTimeSinceStartup;
        }

        float maxGrazeDist                 = (_GRAZE_THRES_SQUARED * (this.Mastered ? 2f : 1f));
        bool playerVulnerable              = pc.healthHaver && pc.healthHaver.IsVulnerable;
        Vector2 ppos                       = pc.CenterPosition;
        Vector2 bottom                     = pc.SpriteBottomCenter;
        int numCandidates                  = _GrazeCandidates.Count;
        LinkedListNode<Projectile> curNode = _GrazeCandidates.First;
        for (int i = 0; i < numCandidates; ++i)
        {
            LinkedListNode<Projectile> nextNode = curNode.Next;
            Projectile p = curNode.Value;
            if (p && p.isActiveAndEnabled && p.sprite && p.sprite.renderer && p.sprite.renderer.enabled && p.collidesWithPlayer && p.Owner is not PlayerController
                  && (p.HeadingTowardPlayer(pc) || p.Direction == Vector2.zero) && (p.SafeCenter - ppos).sqrMagnitude <= maxGrazeDist)
            {
                curNode = nextNode;
                continue;
            }
            if (!playerVulnerable)
            {
                _GrazeCandidates.Remove(curNode);
                curNode = nextNode;
                continue;
            }
            if (this.Mastered && !this._focused && p && p.Owner is not PlayerController)
            {
                PassiveReflectItem.ReflectBullet(p: p, retargetReflectedBullet: true, newOwner: this.PlayerOwner,
                    minReflectedBulletSpeed: 30f, scaleModifier: 1f, damageModifier: 1f, spread: 0f);
                this.PlayerOwner.gameObject.Play("aimu_reflect_sound");
            }
            else
                this.PlayerOwner.gameObject.Play("aimu_graze_sound");
            this.graze = Mathf.Min(this.graze + _GRAZE_PER_PROJECTILE, _GRAZE_MAX);
            CwaffVFX.Spawn(AimuHakurei._GrazeVFX, bottom, velocity: new Vector2(0f, 5f), lifetime: 0.2f, fadeOutTime: 0.4f);
            PowerUp();
            _GrazeCandidates.Remove(curNode);
            curNode = nextNode;
        }

        if (!playerVulnerable)
            return; // can't graze if we're invincible, that's cheating!!!

        foreach (Projectile p in StaticReferenceManager.AllProjectiles)
        {
            if (!p || !p.isActiveAndEnabled || !p.sprite || !p.sprite.renderer || !p.sprite.renderer.enabled || !p.collidesWithPlayer || p.Owner is PlayerController)
                continue; // if the projectile can't collide with us, we're not impressed
            if (!_GrazeCandidates.Contains(p) && (p.SafeCenter - ppos).sqrMagnitude <= maxGrazeDist && (p.Direction == Vector2.zero || p.HeadingTowardPlayer(pc)))
                _GrazeCandidates.AddLast(p);
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
    // must be public or it won't serialize in prefab
    public bool invert;
    public float amplitude;
    public string sound;

    public void Setup(bool invert, float amplitude, string sound)
    {
        this.invert    = invert;
        this.amplitude = amplitude;
        this.sound     = sound;
    }

    private void Start()
    {
        base.GetComponent<Projectile>().OverrideMotionModule =
            new AimuHakureiProjectileMotionModule(){ ForceInvert = this.invert, amplitude = this.amplitude };
        base.gameObject.Play(this.sound);
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

    private void ResetAngle(float angleDiff)
    {
        if (float.IsNaN(angleDiff))
            return;

        Quaternion q        = Quaternion.Euler(0f, 0f, angleDiff);
        _initialUpVector    = q * _initialUpVector;
        _initialRightVector = q * _initialRightVector;
    }

    public override void UpdateDataOnBounce(float angleDiff) => ResetAngle(angleDiff);
    public override void AdjustRightVector(float angleDiff) => ResetAngle(angleDiff);

    private void Initialize(Vector2 lastPosition, Transform projectileTransform, float m_timeElapsed, Vector2 m_currentDirection, bool shouldRotate)
    {
        _privateLastPosition = lastPosition;
        _initialRightVector  = ((!shouldRotate) ? m_currentDirection : projectileTransform.right.XY());
        _initialUpVector     = ((!shouldRotate) ? (Quaternion.Euler(0f, 0f, 90f) * m_currentDirection) : projectileTransform.up);
        _initialized         = true;
        _xDisplacement       = 0f;
        _yDisplacement       = 0f;
    }

    public override void Move(Projectile source, Transform projectileTransform, tk2dBaseSprite projectileSprite, SpeculativeRigidbody specRigidbody, ref float m_timeElapsed, ref Vector2 m_currentDirection, bool Inverted, bool shouldRotate)
    {
        ProjectileData baseData = source.baseData;
        Vector2 oldPos = ((!projectileSprite) ? projectileTransform.position.XY() : projectileSprite.WorldCenter);
        if (!_initialized)
            Initialize(oldPos, projectileTransform, m_timeElapsed, m_currentDirection, shouldRotate);
        m_timeElapsed   += BraveTime.DeltaTime;
        int invertSign           = (Inverted ^ ForceInvert) ? -1 : 1;
        float phaseAngle         = Mathf.PI * baseData.speed / wavelength;
        float newDisplacementX   = m_timeElapsed * baseData.speed;
        float newDisplacementY   = invertSign * amplitude * Mathf.Sin(m_timeElapsed * phaseAngle);
        float deltaDisplacementX = newDisplacementX - _xDisplacement;
        float deltaDisplacementY = newDisplacementY - _yDisplacement;
        Vector2 newPos           = (_privateLastPosition = _privateLastPosition + _initialRightVector * deltaDisplacementX + _initialUpVector * deltaDisplacementY);
        if (shouldRotate)
        {
            float futureDisplacementY = invertSign * amplitude * Mathf.Sin((m_timeElapsed + 0.01f) * phaseAngle);
            float angleFromStart = BraveMathCollege.Atan2Degrees(futureDisplacementY - newDisplacementY, 0.01f * baseData.speed);
            projectileTransform.localRotation = (angleFromStart + _initialRightVector.ToAngle()).EulerZ();
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
