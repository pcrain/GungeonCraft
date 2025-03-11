namespace CwaffingTheGungy;

/* Behavior sketch:
    - fires flak seeds that deal no damage, but plant flak sprouts on the ground beneath them when dying
    - flak sprouts grow into flak flowers after 6 seconds
        - sprout growth can be sped up by "pollinating" them via firing enemy or player bullets over them
        - each player bullet accelerates growth by (0.1 seconds * damage)
        - each enemy bullet accelerates growth by a flat 1 second
    - fully grown flak flowers fire a weak bullet towards the nearest enemy every 0.5 seconds
    - fully grown flak flowers wither after 15 seconds
        - nearby flowers compete for nutrients, causing each other to wither faster
        - watering flowers resets wither timer
        - flowers wither immediately when exposed to fire, poison, oil, ice, or electricity
        - flowers wither immediately when trampled by enemies
*/

public class Flakseed : CwaffGun
{
    public static string ItemName         = "Flakseed";
    public static string ShortDescription = "Orgunic Gardening";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    internal static GameObject _FlakFlowerPrefab     = null;
    internal static Projectile _FlakFlowerProjectile = null;
    internal static GameObject _PetalVFX             = null;

    public static void Init()
    {
        Lazy.SetupGun<Flakseed>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.C, gunClass: GunClass.PISTOL, reloadTime: 1.5f, ammo: 300, idleFps: 6, shootFps: 30,
            reloadFps: 18, muzzleFrom: Items.Mailbox, fireAudio: "flakseed_shoot_sound")
          .SetReloadAudio("flakseed_reload_sound", 3, 22)
          .SetReloadAudio("flakseed_deposit_sound", 13, 14, 16, 17)
          .InitSpecialProjectile<GrenadeProjectile>(GunData.New(clipSize: 12, cooldown: 0.16f,
            shootStyle: ShootStyle.Automatic, damage: 0f, speed: 24f, force: 10f, range: 30f,/* customClip: true,*/
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
    }
}

public class FlakseedProjectile : MonoBehaviour
{
    private const float _AIR_FRICTION    = 0.96f;

    private GrenadeProjectile _projectile = null;
    private PlayerController  _owner      = null;

    private void Start()
    {
        this._projectile = base.GetComponent<GrenadeProjectile>();
        this._owner = this._projectile.Owner as PlayerController;
        this._projectile.baseData.speed *= 0.95f + 0.1f * UnityEngine.Random.value; // randomize the velocity slightly
        this._projectile.OnDestruction += CreateSprout;
    }

    private void CreateSprout(Projectile projectile)
    {
        Vector2 finalPos = projectile.SafeCenter;
        if (GameManager.Instance.Dungeon.CellIsPit(finalPos))
            return;
        FlakseedFlower ff = Flakseed._FlakFlowerPrefab.Instantiate(finalPos).GetComponent<FlakseedFlower>();
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

    internal PlayerController _owner = null;

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

    private void Start()
    {
        _ExtantFlowers.Add(this);
        this._sprite = base.GetComponent<tk2dSprite>();
        this._body = base.gameObject.GetComponent<SpeculativeRigidbody>();
        this._body.OnPreRigidbodyCollision += this.PollinatedByBullets;
        this._firePos = this._sprite.WorldTopCenter + new Vector2(0f, -0.125f);
        this._nextFireRate = _MIN_FIRE_RATE + _FIRE_RATE_VARIANCE * UnityEngine.Random.value;
    }

    private static bool CanTrample(AIActor enemy)
    {
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

    private void Update()
    {
        GoopPositionData goopData = this._body.UnitBottomCenter.GoopData(out GoopDefinition goopDef);
        if (InHostileSoil(goopData, goopDef))
        {
            Wilt();
            return;
        }

        float dtime = BraveTime.DeltaTime;
        if (!this._grown)
        {
            this._growthTimer += dtime;
            if (this._growthTimer >= _GROWTH_TIME)
            {
                this._sprite.scale = Vector3.one;
                this._grown = true;
                base.gameObject.GetComponent<tk2dSpriteAnimator>().Play("bloom");
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
            proj.collidesWithPlayer  = false;
            proj.collidesWithEnemies = true;
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

    private void Wilt()
    {
        if (this._grown)
        {
            DoPetalVFX();
            base.gameObject.Play("flak_wilt_sound");
        }
        UnityEngine.Object.Destroy(base.gameObject);
    }
}
