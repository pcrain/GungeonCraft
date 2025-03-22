
namespace CwaffingTheGungy;

public class Flakseed : CwaffGun
{
    public static string ItemName         = "Flakseed";
    public static string ShortDescription = "Orgunic Gardening";
    public static string LongDescription  = "Fires flak seeds that grow into flak flowers 6 seconds after landing. Bullets accelerate growth by pollinating flak sprouts they pass over, with stronger bullets causing faster growth. Grown flowers fire flak for 15 seconds before withering. Nearby flak flowers compete for nutrients and accelerate withering. Flak flowers will not wither while planted in water, and wither instantly when exposed to hostile terrain or when trampled by enemies.";
    public static string Lore             = "The quintessential tool for both practitioners of warfare-based gardening and practitioners of gardening-based warfare. Disregarding the fact that the combined demographic for both of these hobbies was 0 at the time of this tool's invention, a successful decade-long marketing campaign has since brought that number up to 2. With just a tiny bit of practice, you could be the one to let them technically, legally claim that number is 3!";

    internal static GameObject _FlakFlowerPrefab     = null;
    internal static GameObject _MegaFlakFlowerPrefab = null;
    internal static Projectile _FlakFlowerProjectile = null;
    internal static GameObject _PetalVFX             = null;
    internal static GameObject _VineVFX              = null;

    public static void Init()
    {
        Lazy.SetupGun<Flakseed>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.C, gunClass: GunClass.PISTOL, reloadTime: 1.5f, ammo: 300, idleFps: 6, shootFps: 30,
            reloadFps: 18, muzzleFrom: Items.Mailbox, fireAudio: "flakseed_shoot_sound", smoothReload: 0.1f)
          .SetReloadAudio("flakseed_reload_sound", 3, 22)
          .SetReloadAudio("flakseed_deposit_sound", 13, 14, 16, 17)
          .InitSpecialProjectile<GrenadeProjectile>(GunData.New(clipSize: 12, cooldown: 0.16f,
            shootStyle: ShootStyle.Automatic, damage: 0f, speed: 24f, force: 10f, range: 30f, customClip: true,
            sprite: "flakseed_bullet", fps: 12, anchor: Anchor.MiddleCenter, shouldRotate: false))
          .Attach<GrenadeProjectile>(g => { g.startingHeight = 0.5f; })
          .Attach<BounceProjModifier>(bounce => {
            bounce.percentVelocityToLoseOnBounce = 0.5f;
            bounce.numberOfBounces = Mathf.Max(bounce.numberOfBounces, 0) + 3; })
          .Attach<FlakseedProjectile>();

        _FlakFlowerPrefab = VFX.Create("flakseed_sprout", anchor: Anchor.LowerCenter, emissivePower: 1f);
        _FlakFlowerPrefab.AddAnimation("bloom", "flakseed_flower", fps: 4, anchor: Anchor.LowerCenter, emissivePower: 1f);
        _FlakFlowerPrefab.AddComponent<FlakseedFlower>();
        _FlakFlowerPrefab.AutoRigidBody(Anchor.LowerCenter, CollisionLayer.PlayerHitBox);

        _MegaFlakFlowerPrefab = VFX.Create("mega_flak_flower_sprout", anchor: Anchor.MiddleCenter, emissivePower: 1f);
        _MegaFlakFlowerPrefab.AddAnimation("wait", "mega_flak_flower_wait", fps: 10, anchor: Anchor.MiddleCenter, emissivePower: 1f);
        _MegaFlakFlowerPrefab.AddAnimation("attack", "mega_flak_flower_attack", fps: 10 , anchor: Anchor.MiddleCenter, emissivePower: 1f, loops: false);
        _MegaFlakFlowerPrefab.AddAnimation("vanish", "mega_flak_flower_vanish", fps: 10, anchor: Anchor.MiddleCenter, emissivePower: 1f, loops: false);
        _MegaFlakFlowerPrefab.AddComponent<FlakseedFlower>().mega = true;
        _MegaFlakFlowerPrefab.AutoRigidBody(Anchor.MiddleCenter, CollisionLayer.PlayerHitBox);

        Color lightGreen = new Color(0.7f, 0.85f, 0.65f);
        _FlakFlowerProjectile = Items.Ak47.CloneProjectile(GunData.New(
            sprite: "flakseed_flower_bullet", damage: 1.0f, speed: 50.0f, force: 1.0f, range: 80.0f))
          .Attach<EasyTrailBullet>(trail => {
            trail.TrailPos   = trail.transform.position;
            trail.StartWidth = 0.1f;
            trail.EndWidth   = 0.025f;
            trail.LifeTime   = 0.05f;
            trail.BaseColor  = lightGreen;
            trail.StartColor = lightGreen;
            trail.EndColor   = Color.Lerp(lightGreen, Color.white, 0.25f);
          });

        _PetalVFX = VFX.Create("flakseed_petal", emissivePower: 2f);
        _VineVFX = VFX.Create("vine_attack_vfx", fps: 60, loops: false, emissivePower: 2f);
    }
}

public class FlakseedProjectile : MonoBehaviour
{
    private const float _AIR_FRICTION    = 0.96f;

    private GrenadeProjectile _projectile = null;
    private PlayerController  _owner      = null;
    private bool              _mastered   = false;

    private void Start()
    {
        this._projectile = base.GetComponent<GrenadeProjectile>();
        this._owner = this._projectile.Owner as PlayerController;
        this._mastered = this._projectile.Mastered<Flakseed>();
        this._projectile.baseData.speed *= 0.95f + 0.1f * UnityEngine.Random.value; // randomize the velocity slightly
        this._projectile.OnDestruction += CreateSprout;
    }

    private void CreateSprout(Projectile projectile)
    {
        Vector2 finalPos = projectile.SafeCenter;
        if (GameManager.Instance.Dungeon.CellIsPit(finalPos))
            return;

        FlakseedFlower ff = (this._mastered ? Flakseed._MegaFlakFlowerPrefab : Flakseed._FlakFlowerPrefab)
          .Instantiate(finalPos).GetComponent<FlakseedFlower>();
        ff._owner = this._owner;
        SpawnManager.SpawnVFX(VFX.MiniPickup, finalPos, Lazy.RandomEulerZ());
    }

    private void Update()
    {
        if (!this._projectile)
            return;

        if (this._projectile.m_current3DVelocity.z < 0)
        {
            this._projectile.ApplyFriction(_AIR_FRICTION);
            return;
        }

        base.gameObject.Play("flak_plant_sound");
        this._projectile.DieInAir(suppressInAirEffects: true);
    }
}

public class FlakseedFlower : MonoBehaviour
{
    const float _GROWTH_TIME = 6.0f;
    const float _MIN_FIRE_RATE = 0.5f;
    const float _FIRE_RATE_VARIANCE = 0.25f;
    const float _WILT_TIME = 15.0f;
    const float _WILT_CHECK_RATE = 0.1f;
    const float _ATTACK_RATE = 2.0f;
    const float _ATTACK_CHECK_RATE = 0.5f;
    const float _ATTACK_DAMAGE = 25f;
    const float _ATTACK_RADIUS = 5.0f;
    const float _ATTACK_RADIUS_SQR = _ATTACK_RADIUS * _ATTACK_RADIUS;

    private static List<FlakseedFlower> _ExtantFlowers = new();

    private bool _grown = false;
    private float _growthTimer = 0;
    private float _fireTimer = 0;
    private float _wiltTimer = 0;
    private float _wiltRate = 1f;
    private float _nextWiltCheck = 0f;
    private float _nextFireRate = 0f;
    private tk2dSprite _sprite = null;
    private SpeculativeRigidbody _body = null;
    private Vector2 _firePos = default;
    private bool _wilting = false;
    private float _nextAttackCheck = 0;
    private tk2dSpriteAnimator _animator = null;

    internal PlayerController _owner = null;

    public bool mega = false;

    private void Start()
    {
        _ExtantFlowers.Add(this);
        this._sprite = base.GetComponent<tk2dSprite>();
        this._sprite.renderer.material.shader = ShaderCache.Acquire("Brave/PlayerShader");
        this._sprite.usesOverrideMaterial = true;
        this._body = base.gameObject.GetComponent<SpeculativeRigidbody>();
        this._animator = base.gameObject.GetComponent<tk2dSpriteAnimator>();
        this._body.OnPreRigidbodyCollision += this.PollinatedByBullets;
        this._firePos = this._sprite.WorldTopCenter + new Vector2(0f, -0.125f);
        this._nextFireRate = _MIN_FIRE_RATE + _FIRE_RATE_VARIANCE * UnityEngine.Random.value;
        this._animator.AnimationCompleted += OnAnimationCompleted;
    }

    private void OnAnimationCompleted(tk2dSpriteAnimator animator, tk2dSpriteAnimationClip clip)
    {
        if (this._wilting)
            Wilt(vanish: true);
        else if (clip.name.Contains("attack"))
            animator.Play("wait");
    }

    private bool CanTrample(AIActor enemy)
    {
        if (this.mega)
            return false;
        if (!enemy || enemy.IsFlying || enemy.IsGone || enemy.IsHarmlessEnemy || !enemy.isActiveAndEnabled)
            return false;
        if (enemy.healthHaver is HealthHaver hh && hh.IsDead)
            return false;
        return true;
    }

    private void PollinatedByBullets(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
    {
        PhysicsEngine.SkipCollision = true;
        if (!otherRigidbody)
            return;
        if (otherRigidbody.gameObject.GetComponent<AIActor>() is AIActor enemy && CanTrample(enemy))
        {
            Wilt();
            return;
        }
        if (this._grown)
            return;
        if (otherRigidbody.gameObject.GetComponent<Projectile>() is not Projectile proj)
            return;
        float pollination = (proj.Owner is PlayerController) ? (0.1f * proj.baseData.damage) : 1f;
        this._growthTimer += pollination;
        base.gameObject.Play("flakseed_pollinate_sound");
        DoPollinationVFX();
        myRigidbody.RegisterSpecificCollisionException(otherRigidbody);
    }

    private void DoPollinationVFX()
    {
        DoPetalVFX(true);
    }

    private void DoMegaPetalVFX()
    {
        CwaffVFX.SpawnBurst(
            prefab: Yggdrashell._LeafVFX,
            numToSpawn: 60,
            basePosition: this._firePos,
            positionVariance: 1f,
            minVelocity: 4f,
            velocityVariance: 4f,
            velType: CwaffVFX.Vel.Random,
            rotType: CwaffVFX.Rot.Random,
            lifetime: 0.5f,
            startScale: 1.0f,
            endScale: 0.1f,
            randomFrame: true
          );
    }

    private void DoPetalVFX(bool pollination = false)
    {
        CwaffVFX.SpawnBurst(
            prefab           : pollination ? Flakseed._PetalVFX : Flakseed._PetalVFX,
            numToSpawn       : 6,
            basePosition     : this._firePos,
            positionVariance : 0.25f,
            velocityVariance : 4f,
            velType          : CwaffVFX.Vel.AwayRadial,
            rotType          : CwaffVFX.Rot.Random,
            lifetime         : 0.35f,
            fadeOutTime      : 0.1f,
            uniform          : true,
            startScale       : 1.0f,
            endScale         : 0.5f
          );
    }

    private void AttackNearbyEnemies()
    {
        float now = BraveTime.ScaledTimeSinceStartup;
        if (now < this._nextAttackCheck)
            return;
        this._nextAttackCheck = now + _ATTACK_CHECK_RATE;

        AIActor nearestEnemy = Lazy.NearestEnemy(this._firePos);
        if (!nearestEnemy)
            return;

        Vector2 delta = (nearestEnemy.CenterPosition - this._firePos);
        if (delta.sqrMagnitude > _ATTACK_RADIUS_SQR)
            return;

        this._animator.Play("attack");
        this._sprite.FlipX = nearestEnemy.sprite.WorldCenter.x < this._sprite.WorldCenter.x;
        base.gameObject.Play("vine_attack_sound");
        CwaffVFX.Spawn(prefab: Flakseed._VineVFX, position: nearestEnemy.CenterPosition, lifetime: 0.25f, fadeOutTime: 0.05f, height: 10f);
        if (!nearestEnemy.IsGone && nearestEnemy.healthHaver is HealthHaver hh && hh.IsAlive && hh.IsVulnerable)
        {
            hh.ApplyDamage(_ATTACK_DAMAGE, delta, Flakseed.ItemName, CoreDamageTypes.None, DamageCategory.Normal);
            if (hh.behaviorSpeculator is BehaviorSpeculator bs && !bs.ImmuneToStun)
                bs.Stun(_ATTACK_RATE + 0.1f);
        }
        this._nextAttackCheck = now + _ATTACK_RATE;
    }

    private void Update()
    {
        // #if DEBUG
        //     base.gameObject.DrawDebugCircle(this._firePos, _ATTACK_RADIUS, Color.green.WithAlpha(0.15f));
        // #endif

        if (this._wilting)
            return;

        GoopPositionData goopData = this._body.UnitBottomCenter.GoopData(out GoopDefinition goopDef);
        if (!this.mega && InHostileSoil(goopData, goopDef))
        {
            Wilt();
            return;
        }
        if (this.mega && this._grown)
            AttackNearbyEnemies();

        float dtime = BraveTime.DeltaTime;
        if (!this._grown)
        {
            this._growthTimer += dtime;
            if (this._growthTimer >= _GROWTH_TIME)
            {
                this._sprite.scale = Vector3.one;
                this._grown = true;
                this._animator.Play(this.mega ? "wait" : "bloom");
                if (this.mega)
                    DoMegaPetalVFX();
                else
                    DoPetalVFX();
                base.gameObject.Play("flak_bloom_sound");
                // base.gameObject.Play("flak_growth_sound");
            }
            else
            {
                float growth = 0.3f + 0.7f * (this._growthTimer / _GROWTH_TIME);
                this._sprite.scale = new Vector3(growth, growth, growth);
            }
            return;
        }
        if (InNutritiousSoil(goopData, goopDef))
            this._wiltTimer = 0.0f;
        else if ((this._wiltTimer += (dtime * RateOfWilting())) >= _WILT_TIME)
        {
            Wilt();
            return;
        }
        if ((this._fireTimer += dtime) >= this._nextFireRate)
        {
            this._fireTimer = 0;
            this._nextFireRate = _MIN_FIRE_RATE + _FIRE_RATE_VARIANCE * UnityEngine.Random.value;
            Vector2 targetPos = Lazy.NearestEnemyPos(this._firePos) ?? (this._firePos + Lazy.RandomVector());
            Projectile proj = SpawnManager.SpawnProjectile(
                prefab   : Flakseed._FlakFlowerProjectile.gameObject,
                position : this._firePos,
                rotation : (targetPos - this._firePos).EulerZ()).GetComponent<Projectile>();
            proj.SetOwnerAndStats(this._owner);
            proj.gameObject.Play("flak_flower_shoot_sound");
        }
    }

    private float RateOfWilting()
    {
        const float MAX_DIST = 2f;
        const float MAX_SQR_DIST = MAX_DIST * MAX_DIST;

        float now = BraveTime.ScaledTimeSinceStartup;
        if (this._nextWiltCheck > now)
            return this._wiltRate;

        this._nextWiltCheck = now + _WILT_CHECK_RATE;
        this._wiltRate = 1f;
        if (!this._sprite)
            return this._wiltRate;

        Vector2 pos = this._body.UnitBottomCenter;
        foreach (FlakseedFlower f in _ExtantFlowers)
        {
            if (!f || f == this || !f._sprite)
                continue;
            float sqrMag = (pos - f._sprite.WorldBottomCenter).sqrMagnitude;
            if (sqrMag < MAX_SQR_DIST)
                this._wiltRate += (1f - (sqrMag / MAX_SQR_DIST));
        }

        #if DEBUG
            // base.gameObject.DrawDebugCircle(pos, MAX_DIST, Color.green.WithAlpha(0.15f));
        #endif

        return this._wiltRate;
    }

    private bool InHostileSoil(GoopPositionData goopData, GoopDefinition goopDef)
    {
        if (!goopDef)
            return false;
        if (goopData.IsOnFire)
            return true;
        if (goopData.IsFrozen)
            return true;
        if (goopData.IsElectrified)
            return true;
        if (goopDef.isOily)
            return true;
        if (goopDef.AppliesDamageOverTime)
            return true;
        return false;
    }

    private bool InNutritiousSoil(GoopPositionData goopData, GoopDefinition goopDef)
    {
        return goopDef && goopDef.usesWaterVfx;
    }

    private void OnDestroy()
    {
        _ExtantFlowers.Remove(this);
    }

    private void Wilt(bool vanish = false)
    {
        if (this._wilting && !vanish)
            return;
        if (!this._grown)
        {
            UnityEngine.Object.Destroy(base.gameObject);
            return;
        }
        base.gameObject.Play("flak_wilt_sound");
        if (!this.mega)
        {
            DoPetalVFX();
            UnityEngine.Object.Destroy(base.gameObject);
            return;
        }
        if (!vanish)
        {
            this._animator.Play("vanish");
            this._wilting = true;
            return;
        }
        UnityEngine.Object.Destroy(base.gameObject);
    }
}
