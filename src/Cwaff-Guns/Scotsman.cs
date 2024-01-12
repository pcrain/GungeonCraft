namespace CwaffingTheGungy;

public class Scotsman : AdvancedGunBehavior
{
    public static string ItemName         = "Scotsman";
    public static string SpriteName       = "scotsman";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    private const float _MAX_RETICLE_RANGE = 16f;
    private const float _MAX_SPREAD        = 2f;

    internal static ExplosionData _ScotsmanExplosion = null;
    internal static GameObject _ScotsmanReticle = null;

    private Vector2 _aimPoint            = Vector2.zero;
    private GameObject _targetingReticle = null;

    private int _nextIndex                    = 0;
    private Vector2 _whereIsThePlayerLooking  = Vector2.zero;
    private List<Stickybomb> _extantStickies = new();

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<Scotsman>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.A, gunClass: GunClass.PISTOL, reloadTime: 2.00f, ammo: 300, canReloadNoMatterAmmo: true);
            gun.SetAnimationFPS(gun.shootAnimation, 24);
            gun.SetAnimationFPS(gun.reloadAnimation, 24);
            gun.SetMuzzleVFX("muzzle_iron_maid", fps: 30, scale: 0.5f, anchor: Anchor.MiddleLeft);
            gun.SetFireAudio("stickybomblauncher_shoot");
            gun.SetReloadAudio("stickybomblauncher_worldreload");
            gun.AddToSubShop(ItemBuilder.ShopType.Trorc);

        gun.InitProjectile(new(clipSize: 20, cooldown: 0.22f, shootStyle: ShootStyle.SemiAutomatic,
          damage: 5.0f, speed: 40.0f, sprite: "stickybomb_projectile", fps: 12, anchor: Anchor.MiddleCenter)).Attach<Stickybomb>();

        _ScotsmanReticle = VFX.Create("scotsman_reticle", fps: 12, loops: true, anchor: Anchor.MiddleCenter);

        // Initialize our explosion data
        _ScotsmanExplosion = new ExplosionData()
        {
            forceUseThisRadius     = true,
            pushRadius             = 1.5f,
            damageRadius           = 1.5f,
            damageToPlayer         = 0f,
            doDamage               = true,
            damage                 = 10,
            doDestroyProjectiles   = false,
            doForce                = true,
            debrisForce            = 10f,
            preventPlayerForce     = false,
            explosionDelay         = 0.01f,
            usesComprehensiveDelay = false,
            doScreenShake          = false,
            playDefaultSFX         = true,
            effect                 = Explosions.ExplosiveRounds.effect,
            ignoreList             = Explosions.DefaultSmall.ignoreList,
            ss                     = Explosions.DefaultSmall.ss,
        };
    }

    public override void OnReloadPressed(PlayerController player, Gun gun, bool manualReload)
    {
        base.OnReloadPressed(player, gun, manualReload);
        DetonateStickies(player);
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        if (this.Owner is PlayerController pc)
        {
            ETGModConsole.Log($"aim nulled");
            pc.forceAimPoint = null;
        }
        if (this._targetingReticle)
        {
            UnityEngine.Object.Destroy(this._targetingReticle);
            this._targetingReticle = null;
        }
        base.OnSwitchedAwayFromThisGun();
    }

    protected override void Update()
    {
        base.Update();
        if (this.Owner is not PlayerController player)
            return;

        // smoothly handle reticle postion, compensating extra distance for controller users (modified from Gunbrella)
        if (player.IsKeyboardAndMouse())
        {
            player.forceAimPoint = null;
            this._aimPoint = player.unadjustedAimPoint.XY();
            Vector2 gunPos = player.CurrentGun.barrelOffset.PositionVector2();
            if ((this._aimPoint - player.CenterPosition).sqrMagnitude < 32f)
                this._aimPoint = gunPos + player.m_currentGunAngle.ToVector(1f);
            if (this._targetingReticle)
            {
                UnityEngine.Object.Destroy(this._targetingReticle);
                this._targetingReticle = null;
            }
            return;
        }

        this._targetingReticle ??= Instantiate<GameObject>(_ScotsmanReticle, base.transform.position, Quaternion.identity);
        Vector2 rawAim = player.m_activeActions.Aim.Vector;
        bool showReticle = ((rawAim != Vector2.zero) && (rawAim.sqrMagnitude > 0.02f));
        if (showReticle)
        {
            this._aimPoint = player.CenterPosition + _MAX_RETICLE_RANGE * rawAim;
            player.forceAimPoint = this._aimPoint;
            this._targetingReticle.SetAlpha(1.0f);
        }
        else
        {
            this._aimPoint = player.CenterPosition + player.m_currentGunAngle.ToVector(_MAX_RETICLE_RANGE);
            this._targetingReticle.SetAlpha(0.0f);
        }
        this._targetingReticle.transform.position = this._aimPoint;
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        if (this.Owner is not PlayerController player)
            return;

        Stickybomb sticky = projectile.GetComponent<Stickybomb>();
        this._extantStickies.Add(sticky);

        float spread = _MAX_SPREAD * player.stats.GetStatValue(PlayerStats.StatType.Accuracy);
        Vector2 adjustedTarget = this._aimPoint + Lazy.RandomVector(spread * UnityEngine.Random.value);
        projectile.SendInDirection(adjustedTarget - this.gun.barrelOffset.PositionVector2(), true);
        sticky.Setup(adjustedTarget);
    }

    private void DetonateStickies(PlayerController pc)
    {
        List<Stickybomb> remainingStickies = new();
        foreach (Stickybomb sticky in this._extantStickies)
        {
            if (!(sticky?.Detonate(pc) ?? true))
                remainingStickies.Add(sticky);
        }
        this._extantStickies = remainingStickies;
    }
}

public class Stickybomb : MonoBehaviour
{
    private const float _DET_TIMER = 0.6f;
    private const float _BASE_GLOW = 10f;
    private const float _DET_GLOW  = 100f;

    private PlayerController _owner;
    private Projectile       _projectile;
    private Scotsman         _scotsman;
    private bool             _detonateSequenceStarted;
    private bool             _stuck;
    private Vector2          _target;
    private Vector2          _stickPoint = Vector2.zero;
    private AIActor          _stuckEnemy = null;

    public void Setup(Vector2 target)
    {
        this._projectile              = base.GetComponent<Projectile>();
        this._owner                   = _projectile.Owner as PlayerController;
        this._scotsman                = this._owner.CurrentGun.GetComponent<Scotsman>();
        this._detonateSequenceStarted = false;
        this._stuck                   = false;
        this._target                  = target;

        StartCoroutine(LockAndLoad());
    }

    private void StickToSurface(Vector2 stickPoint)
    {
        this._projectile.specRigidbody.OnRigidbodyCollision -= this.StickToSurface;
        this._projectile.specRigidbody.OnTileCollision -= this.StickToSurface;
        this._projectile.specRigidbody.CollideWithOthers = false;
        this._projectile.specRigidbody.CollideWithTileMap = false;
        this._projectile.collidesWithEnemies = false;

        this._stickPoint = stickPoint;
        this._projectile.specRigidbody.Position = new Position(this._stickPoint);
        this._projectile.specRigidbody.UpdateColliderPositions();
        this._projectile.baseData.speed = 0f;
        this._projectile.UpdateSpeed();

        this._projectile.sprite.HeightOffGround = 10f;
        this._projectile.sprite.UpdateZDepth();

        this._stuck = true;
    }

    private void StickToSurface(CollisionData rigidbodyCollision)
    {
        StickToSurface(rigidbodyCollision.Contact);
        if (rigidbodyCollision.OtherRigidbody?.GetComponent<AIActor>() is not AIActor enemy)
            return;

        this._stuckEnemy = enemy;
        this._stickPoint -= enemy.specRigidbody.transform.position.XY();
        this._projectile.specRigidbody.transform.parent = enemy.specRigidbody.transform;
    }

    private void Update()
    {
        if (!this._stuckEnemy?.specRigidbody)
            return;

        this._projectile.specRigidbody.Position = new Position(this._stickPoint + this._stuckEnemy.specRigidbody.transform.position.XY());
        this._projectile.specRigidbody.UpdateColliderPositions();
    }

    private IEnumerator LockAndLoad()
    {
        float launchTime = BraveTime.ScaledTimeSinceStartup;
        float originalDamage = this._projectile.baseData.damage;
        this._projectile.sprite.SetGlowiness(glowAmount: _BASE_GLOW, glowColor: Color.red);

        // Phase 1, fire towards target
        this._projectile.shouldRotate = false; // prevent automatic rotation after creation
        this._projectile.baseData.damage = 0f;
        this._projectile.BulletScriptSettings.surviveRigidbodyCollisions = true;
        this._projectile.BulletScriptSettings.surviveTileCollisions = true;
        this._projectile.specRigidbody.OnRigidbodyCollision += this.StickToSurface;
        this._projectile.specRigidbody.OnTileCollision += this.StickToSurface;
        float lastDistanceToTarget = 99999f;
        while (!this._stuck)
        {
            Vector2 curpos = this._projectile.specRigidbody.Position.GetPixelVector2();
            Vector2 delta = (this._target - curpos);
            float distanceToTarget = delta.magnitude;
            if (distanceToTarget > lastDistanceToTarget)
            {
                StickToSurface(curpos);
                break;
            }
            float lifetime = BraveTime.ScaledTimeSinceStartup - launchTime;
            this._projectile.sprite.transform.localRotation = (delta.ToAngle() + 3000f * Mathf.Sin(lifetime)).EulerZ();
            lastDistanceToTarget = distanceToTarget;
            yield return null;
        }

        // Phase 2, lie in wait
        this._projectile.damageTypes &= (~CoreDamageTypes.Electric);  // remove electric effect after stopping
        while (!this._detonateSequenceStarted)
            yield return null;

        // Phase 3, primed for detonation
        for (int i = 0; i < 3; ++i)
        {
            AkSoundEngine.PostEvent("stickybomblauncher_det", base.gameObject);
            this._projectile.sprite.SetGlowiness(glowAmount: _DET_GLOW, glowColor: Color.red);
            yield return new WaitForSeconds(_DET_TIMER / 6);
            this._projectile.sprite.SetGlowiness(glowAmount: _BASE_GLOW, glowColor: Color.red);
            yield return new WaitForSeconds(_DET_TIMER / 6);
        }

        // Phase 4, explode
        Exploder.Explode(this._projectile.transform.position, Scotsman._ScotsmanExplosion, Vector2.zero, ignoreQueues: true);
        this._projectile.DieInAir(suppressInAirEffects: true);
    }

    public bool Detonate(PlayerController pc)
    {
        if (pc != this._owner)
            return false; // don't launch projectiles that don't belong to us
        if (!this._stuck)
            return false; // don't detonate moving stickies
        this._detonateSequenceStarted = true;
        return true;
    }
}
