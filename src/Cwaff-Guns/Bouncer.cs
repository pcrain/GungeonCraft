namespace CwaffingTheGungy;

public class Bouncer : CwaffGun
{
    public static string ItemName         = "Bouncer";
    public static string ShortDescription = "Rebound to Go Wrong";
    public static string LongDescription  = "Fires slow but rapidly accelerating projectiles that phase through enemies and objects until bouncing at least once. The damage of each projectile scales with its speed upon initially bouncing. Projectiles bounce up to 3 times, creating a small explosion on their 4th impact.";
    public static string Lore             = "Originally developed as a proof-of-concept back in a time before true bouncing bullets existed, many Gungeoneers today still prefer this older design for flexing their \"mad trickshotting skillz yo\" and its ability to hit enemies behind cover.";

    internal static ExplosionData _MiniExplosion  = null;
    internal const float          _DAMAGE_FACTOR  = 0.3f; // % of speed converted to damage
    internal const float          _FORCE_FACTOR   = 0.5f; // % of speed converted to force
    internal const float          _ACCELERATION   = 1.9f;

    public static void Init()
    {
        Lazy.SetupGun<Bouncer>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.D, gunClass: GunClass.PISTOL, reloadTime: 1.3f, ammo: 300, shootFps: 14, reloadFps: 30,
            muzzleFrom: Items.Magnum, fireAudio: "MC_RocsCape", smoothReload: 0.1f)
          .SetReloadAudio("bouncer_reload_short", 5, 10, 15, 20)
          .SetReloadAudio("bouncer_reload", 25)
          .AddToShop(ModdedShopType.Boomhildr)
          .AddToShop(ModdedShopType.Rusty)
          .InitProjectile(GunData.New(clipSize: 6, cooldown: 0.16f, shootStyle: ShootStyle.SemiAutomatic, damage: _ACCELERATION, speed: _ACCELERATION,
            range: 9999f, sprite: "energy_bounce", fps: 10, scale: 0.2f, anchor: Anchor.MiddleCenter, customClip: true,
            overrideColliderPixelSizes: new IntVector2(1,1))) // 1-pixel collider for accurate bounce animation
          .Attach<BounceProjModifier>(bounce => {
            bounce.numberOfBounces = 3;
            bounce.chanceToDieOnBounce = 0f;
            bounce.onlyBounceOffTiles = true; })
          .Attach<HarmlessUntilBounce>();

        _MiniExplosion = Explosions.DefaultSmall.With(damage: 10f, force: 100f, debrisForce: 10f, radius: 0.5f, preventPlayerForce: true, shake: false);
    }
}

public class HarmlessUntilBounce : MonoBehaviour
{
    private const float _MIN_SOUND_GAP     = 0.25f;
    private const float _MAX_HOMING_SPREAD = 60f;
    private const float _BOUNCE_TIME       = 0.1f; // frames for half a bounce

    private static float _LastBouncePlayed = 0;

    private Projectile _projectile;
    private PlayerController _owner;
    private bool _bounceStarted  = false;
    private bool _bounceFinished = false;
    private int _currentBounces  = 0;
    private int _maxBounces      = 0;
    private float _damageMult    = 1.0f;
    private bool _mastered       = false;

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        if (this._projectile.Owner is not PlayerController pc)
            return;
        this._owner = pc;
        this._damageMult = this._owner.DamageMult();

        BounceProjModifier bounce = base.gameObject.GetComponent<BounceProjModifier>();
        this._maxBounces = bounce.numberOfBounces;
        bounce.OnBounce += this.OnBounce;

        this._mastered = this._projectile.Mastered<Bouncer>();
        if (this._mastered)
        {
            PierceProjModifier ppm   = this._projectile.gameObject.AddComponent<PierceProjModifier>();
            ppm.penetration          = 99;
            ppm.penetratesBreakables = true;
        }

        this._projectile.specRigidbody.OnPreRigidbodyCollision += this.OnPreCollision;
        this._projectile.OnDestruction += this.OnDestruction;
    }

    private void Update()
    {
        if (_bounceStarted)
            return;
        this._projectile.Accelerate(C.FPS * Bouncer._ACCELERATION);
    }

    private void OnDestruction(Projectile p)
    {
        if (this._currentBounces == this._maxBounces) // explode only on final bounce
            Exploder.Explode(p.SafeCenter, Bouncer._MiniExplosion, p.Direction);
    }

    private void OnPreCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
    {
        if (this._bounceFinished)
            return;

        // skip non-tile collisions if we haven't bounced yet
        if (!otherRigidbody.PrimaryPixelCollider.IsTileCollider)
            PhysicsEngine.SkipCollision = true;
        else if (otherRigidbody.GetComponent<DungeonPlaceable>())
            PhysicsEngine.SkipCollision = true;
        else if (otherRigidbody.GetComponent<MinorBreakable>())
            PhysicsEngine.SkipCollision = true;
    }

    private void HandleBounceSounds()
    {
        float now = BraveTime.ScaledTimeSinceStartup;
        if ((now - _LastBouncePlayed) < _MIN_SOUND_GAP)
            return;
        _LastBouncePlayed = now;
        this._projectile.gameObject.Play("MC_RocsCape");
        this._projectile.gameObject.Play("MC_Mushroom_Bounce");
    }

    private void OnBounce()
    {
        ++this._currentBounces;

        this._projectile.m_usesNormalMoveRegardless = true; // temporarily disable Helix Projectile shenanigans
        this._bounceStarted = true;
        this._projectile.StartCoroutine(DoElasticBounce());
        this._projectile.ResetPiercing();
    }

    private IEnumerator DoElasticBounce()
    {
        float oldSpeed = this._projectile.baseData.speed;
        Vector3 oldScale = this._projectile.spriteAnimator.transform.localScale;
        this._projectile.specRigidbody.CollideWithTileMap = false;
        this._projectile.specRigidbody.CollideWithOthers = false;

        this._projectile.baseData.damage = this._damageMult * oldSpeed * Bouncer._DAMAGE_FACTOR;  // base damage should scale with speed
        this._projectile.baseData.force = oldSpeed * Bouncer._FORCE_FACTOR;  // force should scale with speed
        this._projectile.SetSpeed(0.001f);
        this._projectile.specRigidbody.Reinitialize();

        // Squeeze
        for (float elapsed = 0f; elapsed < _BOUNCE_TIME; elapsed += BraveTime.DeltaTime)
        {
            float percentDone = elapsed / _BOUNCE_TIME;
            this._projectile.spriteAnimator.transform.localScale = oldScale.WithX(Mathf.Max(0.1f, 1f - percentDone));
            yield return null;
        }

        HandleBounceSounds();
        this._projectile.sprite.SetGlowiness(glowAmount: 0f, glowColor: Color.yellow, overrideColor: Color.yellow);
        Material m = this._projectile.sprite.renderer.material;

        // Stretch
        for (float elapsed = 0f; elapsed < _BOUNCE_TIME; elapsed += BraveTime.DeltaTime)
        {
            float percentDone = elapsed / _BOUNCE_TIME;
            Color newColor = Color.Lerp(Color.white, Color.yellow, 0.8f * percentDone);
            m.SetFloat("_EmissivePower", 100f * percentDone);
            m.SetColor("_EmissiveColor", newColor);
            m.SetColor("_OverrideColor", newColor);
            this._projectile.spriteAnimator.transform.localScale = oldScale.WithX(Mathf.Max(0.1f, percentDone));
            yield return null;
        }
        this._projectile.spriteAnimator.transform.localScale = oldScale;

        this._projectile.SetSpeed(oldSpeed);
        if (this._mastered)
            if (Lazy.NearestEnemyPosWithinConeOfVision(this._projectile.SafeCenter, this._projectile.Direction.ToAngle(), _MAX_HOMING_SPREAD) is Vector2 target)
                this._projectile.SendInDirection(target - this._projectile.SafeCenter, true);
        this._projectile.specRigidbody.Reinitialize();

        this._bounceFinished = true;
        this._projectile.specRigidbody.CollideWithTileMap = true;
        this._projectile.specRigidbody.CollideWithOthers = true;
        this._projectile.m_usesNormalMoveRegardless = false; // reenable Helix Projectile shenanigans
    }
}
