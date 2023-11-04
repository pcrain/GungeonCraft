namespace CwaffingTheGungy;

public class Bouncer : AdvancedGunBehavior
{
    public static string ItemName         = "Bouncer";
    public static string SpriteName       = "bouncer";
    public static string ProjectileName   = "38_special"; //for rotation niceness
    public static string ShortDescription = "Rebound to Go Wrong";
    public static string LongDescription  = "Fires strong projectiles that phase through enemies and objects until bouncing at least once, creating a small explosion on the final impact.\n\nOriginally developed as a proof-of-concept back in a time before true bouncing bullets existed, many Gungeoneers today still prefer this older design for flexing their \"mad trickshotting skillz yo\" and its ability to hit enemies behind cover.";

    internal static ExplosionData           _MiniExplosion      = null;
    internal static float                   _Damage_Factor      = 0.5f; // % of speed converted to damage
    internal static float                   _Force_Factor       = 0.5f; // % of speed converted to force

    internal const float                    _ACCELERATION       = 1.9f;

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<Bouncer>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription);
            gun.SetAttributes(quality: PickupObject.ItemQuality.C, gunClass: GunClass.PISTOL, reloadTime: 0.8f, ammo: 300);
            gun.SetAnimationFPS(gun.shootAnimation, 14);
            gun.SetAnimationFPS(gun.reloadAnimation, 20);
            gun.SetFireAudio("MC_RocsCape");
            gun.SetReloadAudio("MC_Link_Grow");
            gun.AddToSubShop(ModdedShopType.Boomhildr);
            gun.AddToSubShop(ModdedShopType.Rusty);

        ProjectileModule mod = gun.DefaultModule;
            mod.ammoCost               = 1;
            mod.shootStyle             = ProjectileModule.ShootStyle.SemiAutomatic;
            mod.sequenceStyle          = ProjectileModule.ProjectileSequenceStyle.Random;
            mod.cooldownTime           = 0.16f;
            mod.numberOfShotsInClip    = 6;

        IntVector2 colliderSize = new IntVector2(1,1); // 1-pixel collider for accurate bounce animation

        tk2dSpriteAnimationClip anim = AnimateBullet.CreateProjectileAnimation(
            ResMap.Get("energy_bounce").Base(),
            10, true, new IntVector2(10, 10), // reduced sprite size
            false, tk2dBaseSprite.Anchor.MiddleCenter, true, true, null, colliderSize);

        Projectile projectile = Lazy.PrefabProjectileFromGun(gun);
            projectile.AddDefaultAnimation(anim);
            projectile.baseData.damage = _ACCELERATION;
            projectile.baseData.speed  = _ACCELERATION;
            projectile.baseData.range  = 9999f;
            projectile.gameObject.AddComponent<HarmlessUntilBounce>();

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
    private bool _bounceStarted = false;
    private bool _bounceFinished = false;

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        if (this._projectile.Owner is not PlayerController pc)
            return;
        this._owner = pc;

        BounceProjModifier bounce = this._projectile.gameObject.GetOrAddComponent<BounceProjModifier>();
            bounce.numberOfBounces     = 1; // needs to be more than 1 or projectile dies immediately in special handling code below
            bounce.chanceToDieOnBounce = 0f;
            bounce.OnBounce += OnBounce;
            bounce.onlyBounceOffTiles = true;

        this._projectile.specRigidbody.OnPreRigidbodyCollision += this.OnPreCollision;
    }

    private void Update()
    {
        if (_bounceStarted)
            return;
        this._projectile.baseData.speed += Bouncer._ACCELERATION;
        this._projectile.UpdateSpeed();
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
        AkSoundEngine.PostEvent("MC_RocsCape", this._projectile.gameObject);
        AkSoundEngine.PostEvent("MC_Mushroom_Bounce", this._projectile.gameObject);
    }

    private void OnBounce()
    {
        this._bounceStarted = true;
        this._projectile.StartCoroutine(DoElasticBounce());
    }

    private const float BOUNCE_TIME = 15.0f; // frames for half a bounce
    private IEnumerator DoElasticBounce()
    {
        float oldSpeed = this._projectile.baseData.speed;
        Vector3 oldScale = this._projectile.spriteAnimator.transform.localScale;

        this._projectile.baseData.damage = oldSpeed * Bouncer._Damage_Factor;  // base damage should scale with speed
        this._projectile.baseData.force = oldSpeed * Bouncer._Force_Factor;  // force should scale with speed
        this._projectile.baseData.speed = 0.001f;
        this._projectile.UpdateSpeed();
        this._projectile.specRigidbody.Reinitialize();

        // Squeeze
        float bounceScale = 1.0f / BOUNCE_TIME;
        for (int i = (int)BOUNCE_TIME; i > 1; --i)
        {
            this._projectile.spriteAnimator.transform.localScale = oldScale.WithX(bounceScale*i);
            yield return null;
        }

        HandleBounceSounds();
        this._projectile.sprite.usesOverrideMaterial = true;
        Material m = this._projectile.sprite.renderer.material;
            m.shader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTintableTiltedCutoutEmissive");
            m.SetFloat("_EmissivePower", 0f);
            m.SetFloat("_EmissiveColorPower", 1.55f);
            m.SetColor("_EmissiveColor", Color.yellow);
            m.SetColor("_OverrideColor", Color.yellow);

        // Stretch
        for (int i = 1; i < BOUNCE_TIME; ++i)
        {
            float percentDone = bounceScale*i;
            Color newColor = new Color(
                r: Mathf.Lerp(Color.white.r, Color.yellow.r, percentDone),
                g: Mathf.Lerp(Color.white.g, Color.yellow.g, percentDone),
                b: Mathf.Lerp(Color.white.b, Color.yellow.b, percentDone));
            m.SetFloat("_EmissivePower", 100f * percentDone);
            m.SetColor("_EmissiveColor", newColor);
            m.SetColor("_OverrideColor", newColor);
            this._projectile.spriteAnimator.transform.localScale = oldScale.WithX(percentDone);
            yield return null;
        }
        this._projectile.spriteAnimator.transform.localScale = oldScale;

        this._projectile.baseData.speed = oldSpeed;
        this._projectile.UpdateSpeed();
        this._projectile.specRigidbody.Reinitialize();
        this._projectile.OnDestruction += (Projectile p) => Exploder.Explode(
            this._projectile.sprite.WorldCenter, Bouncer._MiniExplosion, p.Direction);

        this._bounceFinished = true;
    }
}
