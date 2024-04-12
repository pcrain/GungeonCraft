namespace CwaffingTheGungy;

public class Bouncer : AdvancedGunBehavior
{
    public static string ItemName         = "Bouncer";
    public static string ShortDescription = "Rebound to Go Wrong";
    public static string LongDescription  = "Fires slow but rapidly accelerating projectiles that phase through enemies and objects until bouncing at least once. The damage of each projectile scales with its speed upon initially bouncing. Projectiles bounce up to 3 times, creating a small explosion on their 4th impact.";
    public static string Lore             = "Originally developed as a proof-of-concept back in a time before true bouncing bullets existed, many Gungeoneers today still prefer this older design for flexing their \"mad trickshotting skillz yo\" and its ability to hit enemies behind cover.";

    internal static ExplosionData _MiniExplosion  = null;
    internal const float          _DAMAGE_FACTOR  = 0.3f; // % of speed converted to damage
    internal const float          _FORCE_FACTOR   = 0.5f; // % of speed converted to force
    internal const float          _ACCELERATION   = 1.9f;

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<Bouncer>(ItemName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.D, gunClass: GunClass.PISTOL, reloadTime: 1.3f, ammo: 300, shootFps: 14, reloadFps: 30,
                muzzleFrom: Items.Magnum, fireAudio: "MC_RocsCape");
            gun.SetReloadAudio("bouncer_reload_short", 5, 10, 15, 20);
            gun.SetReloadAudio("bouncer_reload", 25);
            gun.AddToSubShop(ModdedShopType.Boomhildr);
            gun.AddToSubShop(ModdedShopType.Rusty);

        gun.InitProjectile(GunData.New(clipSize: 6, cooldown: 0.16f, shootStyle: ShootStyle.SemiAutomatic, damage: _ACCELERATION, speed: _ACCELERATION,
          range: 9999f, sprite: "energy_bounce", fps: 10, scale: 0.2f, anchor: Anchor.MiddleCenter,
          overrideColliderPixelSizes: new IntVector2(1,1) // 1-pixel collider for accurate bounce animation
          )).Attach<HarmlessUntilBounce>();

        // Initialize our explosion data
        ExplosionData defaultExplosion = GameManager.Instance.Dungeon.sharedSettingsPrefab.DefaultSmallExplosionData;
        _MiniExplosion = new ExplosionData()
        {
            forceUseThisRadius     = true,
            pushRadius             = 0.5f,
            damageRadius           = 0.5f,
            damageToPlayer         = 0f,
            doDamage               = true,
            damage                 = 10,
            doDestroyProjectiles   = false,
            doForce                = true,
            debrisForce            = 10f,
            preventPlayerForce     = true,
            explosionDelay         = 0.01f,
            usesComprehensiveDelay = false,
            doScreenShake          = false,
            playDefaultSFX         = true,
            effect                 = defaultExplosion.effect,
            ignoreList             = defaultExplosion.ignoreList,
            ss                     = defaultExplosion.ss,
        };
    }
}

public class HarmlessUntilBounce : MonoBehaviour
{
    private Projectile _projectile;
    private PlayerController _owner;
    private bool _bounceStarted  = false;
    private bool _bounceFinished = false;
    private int _currentBounces  = 0;
    private int _maxBounces      = 0;
    private float _damageMult    = 1.0f;

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        if (this._projectile.Owner is not PlayerController pc)
            return;
        this._owner = pc;
        this._damageMult = this._owner.DamageMult();

        BounceProjModifier bounce = this._projectile.gameObject.GetOrAddComponent<BounceProjModifier>();
            bounce.numberOfBounces     += 3; // needs to be more than 1 or projectile dies immediately in special handling code below
            bounce.chanceToDieOnBounce = 0f;
            bounce.OnBounce += OnBounce;
            bounce.onlyBounceOffTiles = true;

        this._maxBounces = bounce.numberOfBounces;

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
            Exploder.Explode(p.sprite.WorldCenter, Bouncer._MiniExplosion, p.Direction);
    }

    private void OnPreCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
    {
        if (this._bounceFinished)
            return;

        // skip non-tile collisions if we haven't bounced yet
        if (!otherRigidbody.PrimaryPixelCollider.IsTileCollider)
            PhysicsEngine.SkipCollision = true;
        else if (otherRigidbody.GetComponent<DungeonPlaceable>() != null)
            PhysicsEngine.SkipCollision = true;
        else if (otherRigidbody.GetComponent<MinorBreakable>() != null)
            PhysicsEngine.SkipCollision = true;
    }

    private static float _LastBouncePlayed = 0;
    private const  float _MIN_SOUND_GAP = 0.25f;
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
    }

    private const float _BOUNCE_TIME = 0.1f; // frames for half a bounce
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
        this._projectile.specRigidbody.Reinitialize();

        this._bounceFinished = true;
        this._projectile.specRigidbody.CollideWithTileMap = true;
        this._projectile.specRigidbody.CollideWithOthers = true;
        this._projectile.m_usesNormalMoveRegardless = false; // reenable Helix Projectile shenanigans
    }
}
