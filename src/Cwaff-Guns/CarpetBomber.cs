namespace CwaffingTheGungy;

public class CarpetBomber : AdvancedGunBehavior
{
    public static string ItemName         = "Carpet Bomber";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = "Rugged Terrain";
    public static string LongDescription  = "Fires a barrage of explosive bouncing carpets. Launches more carpets with more force the longer it is charged.";
    public static string Lore             = "Developed by Mike (of 'Mike' fame), this ornate launcher fires carpets that have been tightly rolled using hydraulics, laced with glued-on patches of C4, and vacuum-sealed to preserve freshness.";

    private const int   _MAX_WALL_BOUNCES      = 3;
    private const int   _MAX_PROJECTILES       = 10;
    private const float _CHARGE_PER_PROJECTILE = 0.33f;
    private const float _MAX_SPEED             = 20f;
    private const float _MIN_SPEED             = 10f;
    private const float _DLT_SPEED             = _MAX_SPEED - _MIN_SPEED;
    private const float _SPEED_PER_CHARGE      = 2.4f;
    private const float _RANGE_DELTA           = 12f;

    internal static ExplosionData _CarpetExplosion = null;

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<CarpetBomber>(ItemName, ProjectileName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.B, gunClass: GunClass.CHARGE, reloadTime: 1.5f, ammo: 360);
            gun.muzzleFlashEffects = (ItemHelper.Get(Items.SeriousCannon) as Gun).muzzleFlashEffects;
            gun.SetAnimationFPS(gun.shootAnimation, 30);
            gun.SetAnimationFPS(gun.reloadAnimation, 20);
            gun.SetAnimationFPS(gun.chargeAnimation, (int)(1f / _CHARGE_PER_PROJECTILE));
            gun.LoopAnimation(gun.chargeAnimation, 10);
            gun.SetMuzzleVFX("muzzle_carpet_bomber", fps: 30, scale: 0.5f, anchor: Anchor.MiddleCenter);
            gun.SetFireAudio("carpet_bomber_shoot_sound");
            gun.SetReloadAudio("carpet_bomber_reload_sound", 2, 10, 18);
            gun.SetChargeAudio("carpet_bomber_charge_stage", 1, 2, 3, 4, 5, 6, 7, 8, 9, 10);

        Projectile p = gun.InitSpecialProjectile<FancyGrenadeProjectile>(GunData.New(
          clipSize: 3, cooldown: 0.15f, angleVariance: 10.0f, shootStyle: ShootStyle.Charged, range: 9999f, customClip: true,
          sequenceStyle: ProjectileSequenceStyle.Ordered, sprite: "carpet_bomber_projectile", fps: 20, anchor: Anchor.MiddleCenter,
          scale: 0.5f, barrageSize: _MAX_PROJECTILES, shouldRotate: true, shouldFlipHorizontally: true, surviveRigidbodyCollisions: true,
          anchorsChangeColliders: false, overrideColliderPixelSizes: new IntVector2(8, 8)
        )).Attach<BounceProjModifier>(bounce => {
          bounce.numberOfBounces = _MAX_WALL_BOUNCES;
          bounce.onlyBounceOffTiles = false;
          bounce.ExplodeOnEnemyBounce = false;
        }).Attach<CarpetProjectile>();

        for (int i = 0; i < _MAX_PROJECTILES; i++)
        {
            ProjectileModule imod = gun.RawSourceVolley.projectiles[i];
            imod.projectiles.Clear();
            imod.chargeProjectiles = new List<ProjectileModule.ChargeProjectile>();
            imod.triggerCooldownForAnyChargeAmount = true;
            for (int j = 0; j <= i; ++j)
                gun.RawSourceVolley.projectiles[j].chargeProjectiles.Add(new ProjectileModule.ChargeProjectile {
                    Projectile = p.Clone(GunData.New(
                      // speed increases both with the charge and with the projectile's index in the array
                      speed: _MIN_SPEED + (_DLT_SPEED + _SPEED_PER_CHARGE * i) * (i == 0 ? 0.5f : ((float)j / (float)i)))
                      ).Attach<FancyGrenadeProjectile>(g => {
                        g.startingHeight   = 0.5f;
                        g.minBounceAngle   = 10f;
                        g.maxBounceAngle   = 30f;
                        g.startingVelocity = 0.5f * j;
                      }),
                    ChargeTime = _CHARGE_PER_PROJECTILE * (i + 1),
                });
        }

        // Initialize our explosion data
        _CarpetExplosion = new ExplosionData()
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
            effect                 = Explosions.ExplosiveRounds.effect,
            ignoreList             = Explosions.DefaultSmall.ignoreList,
            ss                     = Explosions.DefaultSmall.ss,
        };
    }

    protected override void Update()
    {
        base.Update();
        if (this.Player) // Synchronize ammo clips between projectile modules as necessary
            this.gun.SynchronizeReloadAcrossAllModules();
    }
}

public class CarpetProjectile : MonoBehaviour
{
    private const int _MAX_BOUNCES          = 3;
    private const float _AIR_FRICTION       = 0.8f;
    private const float _BOUNCE_FRICTION    = 0.75f;

    private Projectile _projectile          = null;
    private PlayerController _owner         = null;
    private FancyGrenadeProjectile _grenade = null;
    private int _bounces                    = 0;

    private void Start()
    {
        this._projectile        = base.GetComponent<Projectile>();
        this._owner             = this._projectile.Owner as PlayerController;
        this._grenade           = base.GetComponent<FancyGrenadeProjectile>();
        this._grenade.OnBounce += OnGroundBounce;
        this.GetComponent<BounceProjModifier>().OnBounce += OnGroundBounce;

        this._projectile.OnDestruction += (Projectile p) => Exploder.Explode(
            p.transform.position, CarpetBomber._CarpetExplosion, p.Direction, ignoreQueues: true);
        if (this._projectile.specRigidbody)
            this._projectile.specRigidbody.OnRigidbodyCollision += (CollisionData rigidbodyCollision) => {
                this._grenade?.Redirect(rigidbodyCollision.Normal);
                OnGroundBounce();
            };
    }

    private void Update()
    {
        if (!this._projectile)
            return;

        if (this._bounces >= _MAX_BOUNCES)
        {
            this._projectile.DieInAir(suppressInAirEffects: true);
            return;
        }

        if (this._bounces == 0)
            return;

        this._projectile.ApplyFriction(_AIR_FRICTION);
    }

    public void OnGroundBounce()
    {
        this._projectile.baseData.speed *= _BOUNCE_FRICTION;
        Exploder.Explode(this._projectile.transform.position, CarpetBomber._CarpetExplosion, this._projectile.Direction, ignoreQueues: true);
        ++this._bounces;
    }
}
