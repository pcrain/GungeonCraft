namespace CwaffingTheGungy;

public class CarpetBomber : AdvancedGunBehavior
{
    public static string ItemName         = "Carpet Bomber";
    public static string SpriteName       = "carpet_bomber";
    public static string ProjectileName   = "38_special";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    private const int   _MAX_WALL_BOUNCES      = 3;
    private const int   _MAX_PROJECTILES       = 12;
    private const float _CHARGE_PER_PROJECTILE = 0.33f;
    private const float _MAX_SPEED             = 20f;
    private const float _MIN_SPEED             = 10f;
    private const float _DLT_SPEED             = _MAX_SPEED - _MIN_SPEED;
    private const float _SPEED_PER_CHARGE      = 2f;
    private const float _RANGE_DELTA           = 12f;

    public static void Add()
    {
        Gun gun = Lazy.SetupGun<CarpetBomber>(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.B, gunClass: GunClass.CHARGE, reloadTime: 1.5f, ammo: 9999);
            gun.muzzleFlashEffects = (ItemHelper.Get(Items.SeriousCannon) as Gun).muzzleFlashEffects;
            gun.SetAnimationFPS(gun.shootAnimation, 10);
            gun.SetAnimationFPS(gun.chargeAnimation, 16);
            // gun.LoopAnimation(gun.chargeAnimation, 32);
            gun.SetMuzzleVFX("muzzle_b_b_gun", fps: 30, scale: 0.5f, anchor: Anchor.MiddleCenter);
            gun.SetFireAudio("Play_WPN_seriouscannon_shot_01");
            gun.SetReloadAudio("Play_ENM_flame_veil_01");

        Projectile p = gun.InitSpecialProjectile<FancyGrenadeProjectile>(new(
          clipSize: _MAX_PROJECTILES, cooldown: 0.15f, angleVariance: 10.0f, shootStyle: ShootStyle.Charged, range: 9999f,
          sequenceStyle: ProjectileSequenceStyle.Ordered, sprite: "carpet_projectile", fps: 20, anchor: Anchor.MiddleCenter,
          scale: 0.5f, barrageSize: _MAX_PROJECTILES, shouldRotate: true, surviveRigidbodyCollisions: true,
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
                    Projectile = p.Clone(new(
                      // speed increases both with the charge and with the projectile's index in the array
                      speed: _MIN_SPEED + (_DLT_SPEED + _SPEED_PER_CHARGE * i) * (i == 0 ? 0.5f : ((float)j / (float)i)))
                      ).Attach<FancyGrenadeProjectile>(g => {
                        g.startingHeight   = 0.5f;
                        g.minBounceAngle   = 5f;
                        g.maxBounceAngle   = 15f;
                        g.startingVelocity = 0.5f * j;
                      }),
                    ChargeTime = _CHARGE_PER_PROJECTILE * (i + 1),
                });
        }

        UnityEngine.Object.Destroy(p);
    }

    protected override void Update()
    {
        base.Update();
        if (!this.Player)
            return;

        // Synchronize ammo clips between projectile modules as necessary
        bool needsReload = this.gun.m_moduleData[this.gun.DefaultModule].needsReload;
        foreach (ProjectileModule mod in this.gun.Volley.projectiles)
            this.gun.m_moduleData[mod].needsReload |= needsReload;
    }
}

public class CarpetProjectile : MonoBehaviour
{
    private const int _MAX_BOUNCES          = 4;
    private const double _AIR_FRICTION      = 0.5d;
    private const float _BOUNCE_FRICTION    = 0.7f;

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
            p.transform.position, Bouncer._MiniExplosion, p.Direction, ignoreQueues: true);
        if (this._projectile.specRigidbody)
            this._projectile.specRigidbody.OnRigidbodyCollision += (CollisionData rigidbodyCollision) => {
                this._grenade?.Redirect(rigidbodyCollision.Normal);
                this._projectile.UpdateSpeed();
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

        this._projectile.baseData.speed *= (float)Lazy.FastPow(_AIR_FRICTION, this._projectile.LocalDeltaTime);
        this._projectile.UpdateSpeed();
    }

    public void OnGroundBounce()
    {
        this._projectile.baseData.speed *= _BOUNCE_FRICTION;
        Exploder.Explode(this._projectile.transform.position, Bouncer._MiniExplosion, this._projectile.Direction, ignoreQueues: true);
        ++this._bounces;
    }
}
