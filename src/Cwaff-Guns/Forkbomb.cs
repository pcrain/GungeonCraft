namespace CwaffingTheGungy;

public class Forkbomb : CwaffGun
{
    public static string ItemName         = "Forkbomb";
    public static string ShortDescription = "Recursion Excursion";
    public static string LongDescription  = "Throws a large dinner fork that bursts into two more copies of itself shortly after impact. Can be charged up to 6 times, with each charge level doubling the maximum number of forks spawned.";
    public static string Lore             = "For more information on Forkbomb, please see the Ammonomicon entries for Forkbomb and Forkbomb.";

    private const float _EXPLODE_DAMAGE = 50f;

    internal static Projectile _ForkbombProj = null;
    internal static ExplosionData _Explosion = null;

    public static void Init()
    {
        Lazy.SetupGun<Forkbomb>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.A, gunClass: GunClass.SILLY, reloadTime: 1.0f, ammo: 300, shootFps: 60, reloadFps: 4, chargeFps: 30,
            loopChargeAt: 4, muzzleFrom: Items.Mailbox, fireAudio: "knife_gun_launch")
          .AssignGun(out Gun gun)
          .InitProjectile(GunData.New(sprite: "forkbomb_projectile", clipSize: 1, cooldown: 0.18f, shootStyle: ShootStyle.Charged/*, anchor: Anchor.MiddleRight*/,
            damage: 15.0f, speed: 75f, range: 100f, force: 12f,
            anchorsChangeColliders: true, overrideColliderPixelSizes: new IntVector2(1, 1), overrideColliderOffsets: new IntVector2(16, 3), pierceBreakables: true))
          .Attach<ForkbombProjectile>(f => f.forks = 1)
          .Assign(out _ForkbombProj)
          .SetupChargeProjectiles(gun.DefaultModule, 6, (i, p) => new() {
            Projectile     = p.Clone(GunData.New(chargeSound: $"forkbomb_charge_sound_{i+1}"))
                              .Attach<ForkbombProjectile>(f => f.forks = i + 2), // 4, 8, 16, 32, 64, 128 total projectiles
            ChargeTime     = 0.8f * (i + 1),
            AmmoCost       = i + 1,
            UsedProperties = ChargeProjectileProperties.ammo,
          });

        _Explosion = Explosions.ExplosiveRounds.With(damage: _EXPLODE_DAMAGE, force: 100f, debrisForce: 10f, radius: 1.5f,
            preventPlayerForce: false, shake: true);
        _Explosion.ss = new ScreenShakeSettings {
          magnitude               = 0.05f,
          speed                   = 12.0f,
          time                    = 0.05f,
          falloff                 = 0f,
          direction               = Vector2.zero,
          vibrationType           = ScreenShakeSettings.VibrationType.Auto,
          simpleVibrationTime     = Vibration.Time.Quick,
          simpleVibrationStrength = Vibration.Strength.UltraLight,
        };
    }

    public override void Update()
    {
        base.Update();
        if (!this.PlayerOwner)
          return;

        if (this.gun.IsReloading || this.gun.ClipShotsRemaining == 0)
          this.PlayerOwner.ToggleGunRenderers(false, ItemName);
        else
          this.PlayerOwner.ToggleGunRenderers(true, ItemName);
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        base.OnDroppedByPlayer(player);
        player.ToggleGunRenderers(true, ItemName);
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        if (this.PlayerOwner)
          this.PlayerOwner.ToggleGunRenderers(true, ItemName);
        base.OnSwitchedAwayFromThisGun();
    }

    public override void OnDestroy()
    {
        if (this.PlayerOwner)
          this.PlayerOwner.ToggleGunRenderers(true, ItemName);
        base.OnDestroy();
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        projectile.gameObject.GetComponent<ForkbombProjectile>().mastered = this.Mastered;
    }
}

public class ForkbombProjectile : MonoBehaviour
{
    private const float _DET_TIMER   = 0.42f;
    private const float _BASE_GLOW   = 10f;
    private const float _DET_GLOW    = 100f;
    private const float _FORK_SPREAD = 45f;

    private static int                      _VfxId;
    private static tk2dSpriteCollectionData _VfxCol;

    private PlayerController     _owner;
    private Projectile           _projectile;
    private bool                 _stuck;
    private Vector2              _stickPoint;
    private Vector2              _stickNormal;
    private AIActor              _stuckEnemy;
    private HealthHaver          _stuckHealthhaver;
    private float                _detTimer;
    private int                  _detPhase;
    private Material             _mat;
    private float                _originalSpeed;
    private SpeculativeRigidbody _body;

    public int forks = -1;
    public bool mastered;

    private void Start()
    {
        if (_VfxCol == null)
        {
          tk2dSprite vfxSprite = AllayCompanion._AllaySparkles.GetComponent<tk2dSprite>();
          _VfxCol = vfxSprite.collection;
          _VfxId = vfxSprite.spriteId;
        }
        this._projectile = base.GetComponent<Projectile>();
        this._owner      = this._projectile.Owner as PlayerController;
        this._mat        = this._projectile.sprite.renderer.material;
        this._body       = this._projectile.specRigidbody;
        Reinitialize();
    }

    private void Reinitialize()
    {
        this._stuck = false;
        this._stuckHealthhaver = null;
        this._stuckEnemy = null;
        this._detTimer = 0.0f;
        this._detPhase = 0;

        this._projectile.sprite.SetGlowiness(glowAmount: _BASE_GLOW, glowColor: Color.white);
        this._projectile.BulletScriptSettings.surviveRigidbodyCollisions = true;
        this._projectile.BulletScriptSettings.surviveTileCollisions = true;
        this._projectile.collidesWithEnemies = true;

        this._body.CollideWithOthers = true;
        this._body.CollideWithTileMap = true;
        this._body.OnRigidbodyCollision += this.StickToSurface;
        this._body.OnTileCollision += this.StickToSurface;
    }

    private void StickToSurface(CollisionData coll)
    {
        if (coll.OtherRigidbody && coll.OtherRigidbody.minorBreakable)
          return; // don't stick to minor breakables

        PhysicsEngine.PostSliceVelocity = Vector2.zero;

        this._stickNormal = coll.Normal;
        this._body.OnRigidbodyCollision -= this.StickToSurface;
        this._body.OnTileCollision -= this.StickToSurface;
        this._body.CollideWithOthers = false;
        this._body.CollideWithTileMap = false;
        this._projectile.collidesWithEnemies = false;
        this._originalSpeed = this._projectile.baseData.speed;
        this._projectile.SetSpeed(0f);
        this._projectile.sprite.HeightOffGround = 10f;
        this._projectile.sprite.UpdateZDepth();

        if (coll.OtherRigidbody && coll.OtherRigidbody.GetComponent<AIActor>() is AIActor enemy)
        {
            this._stuckEnemy = enemy;
            this._stuckHealthhaver = this._stuckEnemy.healthHaver;
            this._stickPoint -= enemy.specRigidbody.transform.position.XY();
            this._body.transform.parent = enemy.specRigidbody.transform;
        }

        this._projectile.m_usesNormalMoveRegardless = true; // disable movement modifiers such as Helix Bullets
        this._projectile.damageTypes &= (~CoreDamageTypes.Electric);  // remove electric effect after stopping
        this._detTimer = 0.0f;
        this._detPhase = 0;
        this._stuck = true;

        base.gameObject.Play("fork_impact_sound");
        CwaffVFX.SpawnBurst( //NOTE: using collection and id directly to avoid expensive prefab component lookups, since we're making a big mess
            spriteCol        : _VfxCol,
            spriteId         : _VfxId,
            numToSpawn       : 5,
            basePosition     : coll.Contact,
            baseVelocity     : 8f * this._stickNormal,
            velocityVariance : 4f,
            spread           : 45f,
            lifetime         : 0.5f,
            fadeOutTime      : 0.1f,
            startScale       : 0.5f,
            endScale         : 0.1f);
    }

    private void Update()
    {
        if (!this._stuck)
          return;

        if (this._stuckHealthhaver && this._stuckHealthhaver.IsDead)
        {
          Detonate(); // enemy died, so unstick immediately
          return;
        }

        if (this._stuckEnemy && this._stuckEnemy.specRigidbody)
        {
            this._body.Position = new Position(this._stickPoint + this._stuckEnemy.specRigidbody.transform.position.XY());
            this._body.UpdateColliderPositions();
        }

        if ((this._detTimer += BraveTime.DeltaTime) >= _DET_TIMER)
        {
          Detonate();
          return;
        }

        int newDetPhase = (int)((6f * this._detTimer) / _DET_TIMER);
        if (newDetPhase != this._detPhase)
        {
          if (this._mat)
            this._mat.SetFloat(CwaffVFX._EmissivePowerId, (newDetPhase % 2 == 0) ? _DET_GLOW : _BASE_GLOW);
          this._detPhase = newDetPhase;
        }
    }

    private void Detonate()
    {
        if (this.forks <= 0)
        {
          this._projectile.DieInAir(suppressInAirEffects: true);
          return;
        }

        --this.forks;
        Vector2 explodePos =
          this._projectile.sprite ? this._projectile.sprite.WorldCenterRight() :
          this._projectile.transform.position;
        if (this._stuckEnemy || this._stickNormal == default(Vector2))
          this._stickNormal = -this._projectile.transform.right;
        float baseAngle = this._stickNormal.ToAngle();
        for (int i = 0; i < 2; ++i)
        {
          float zRotation = (baseAngle.AddRandomSpread(_FORK_SPREAD)).Clamp360();
          Projectile newFork;
          if (i == 0)
          {
            newFork = SpawnManager.SpawnProjectile(
              Forkbomb._ForkbombProj.gameObject, explodePos, Quaternion.Euler(0f, 0f, zRotation))
              .GetComponent<Projectile>();
            newFork.SpawnedFromOtherPlayerProjectile = true;
            newFork.Owner = this._projectile.Owner;
            newFork.Shooter = this._projectile.Shooter;
            ForkbombProjectile newFbp = newFork.gameObject.GetComponent<ForkbombProjectile>();
            newFbp.forks = this.forks;
            newFbp.mastered = this.mastered;
          }
          else
          {
            newFork = this._projectile;
            newFork.transform.position = explodePos;
            newFork.specRigidbody.Reinitialize();
            newFork.ResetPiercing();
            newFork.SetSpeed(this._originalSpeed);
            newFork.SendInDirection(zRotation.ToVector(), resetDistance: true);
            this.Reinitialize();
          }
          if (this._stuckEnemy && this._stuckEnemy.specRigidbody is SpeculativeRigidbody otherBody)
          {
            newFork.specRigidbody.RegisterTemporaryCollisionException(otherBody, 0.25f, 0.5f);
            otherBody.RegisterTemporaryCollisionException(newFork.specRigidbody, 0.25f, 0.5f); // is this redundant?
          }
          PhysicsEngine.Instance.RegisterOverlappingGhostCollisionExceptions(newFork.specRigidbody);
          if (i == 0 && this._owner)
            this._owner.DoPostProcessProjectile(newFork);
        }
        if (this.mastered)
          Exploder.Explode(explodePos, Forkbomb._Explosion, this._projectile.transform.right, ignoreQueues: true);
    }
}
