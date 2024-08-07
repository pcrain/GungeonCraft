namespace CwaffingTheGungy;

/* TODO:
    - figure out nicely drawing while out of bounds
*/

public class Gunbrella : CwaffGun
{
    public static string ItemName         = "Gunbrella";
    public static string ShortDescription = "Cloudy with a Chance of Pain";
    public static string LongDescription  = "Charging and releasing fires projectiles that hail from the sky at the cursor's position.";
    public static string Lore             = "A normal umbrella that was genetically modified to fire bullets, older models fired projectiles from the front much like a traditional firearm. Gungeoneers quickly grew frustrated at being unable to actually see where they were shooting at due to the Gunbrella's large frame. With modern advances in technology and magic, newer models include a touchscreen and GPS that allows the user to target enemies directly with projectiles summoned from the sky itself.";

    private const float _MIN_CHARGE_TIME   = 0.75f;
    private const int   _BARRAGE_SIZE      = 16;
    private const float _BARRAGE_DELAY     = 0.04f;
    private const float _PROJ_DAMAGE       = 8f;
    private const float _MAX_RETICLE_RANGE = 10f;
    private const float _MAX_ALPHA         = 0.5f;

    internal static ProjectileModule _MasteryModule = null;

    private float _curChargeTime         = 0.0f;
    private Vector2 _chargeStartPos      = Vector2.zero;
    private int _nextProjectileNumber    = 0;
    private bool _mastered               = false;
    private bool _masteryVolleyReplaced  = false;

    public static void Init()
    {
        Gun gun = Lazy.SetupGun<Gunbrella>(ItemName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.A, gunClass: GunClass.CHARGE, reloadTime: 1.0f, ammo: 60 * _BARRAGE_SIZE, shootFps: 60, chargeFps: 16,
                loopChargeAt: 17, muzzleVFX: "muzzle_gunbrella", muzzleFps: 30, muzzleScale: 0.5f, muzzleAnchor: Anchor.MiddleCenter);

        gun.InitProjectile(GunData.New(clipSize: 1, shootStyle: ShootStyle.Charged, customClip: true, ammoCost: 1,
          damage: _PROJ_DAMAGE, sprite: "gunbrella_projectile", fps: 16, anchor: Anchor.MiddleLeft, freeze: 0.33f, chargeTime: _MIN_CHARGE_TIME,
          destroySound: "icicle_crash", barrageSize: _BARRAGE_SIZE, bossDamageMult: 0.6f)) // bosses are big and this does a lot of damage, so tone it down
        .SetAllImpactVFX(VFX.CreatePool("icicle_crash_particles", fps: 30, loops: false, anchor: Anchor.MiddleCenter, scale: 0.35f))
        .Attach<GunbrellaProjectile>();

        _MasteryModule = new ProjectileModule(){projectiles = new(){ gun.DefaultModule.projectiles[0] }}.SetAttributes(GunData.New(
            gun: gun, ammoCost: 1, clipSize: -1, cooldown: 0.05f, shootStyle: ShootStyle.Automatic, sequenceStyle: ProjectileSequenceStyle.Ordered, customClip: true));

        gun.AddReticle<CwaffReticle>(
            reticleVFX : VFX.Create("gunbrella_target_reticle", fps: 12, loops: true, anchor: Anchor.MiddleCenter, emissivePower: 10, emissiveColour: Color.cyan, scale: 0.75f),
            reticleAlpha: 0.25f, fadeInTime : _MIN_CHARGE_TIME, fadeOutTime : 0.25f, maxDistance : _MAX_RETICLE_RANGE, controllerScale : 1f + _MAX_RETICLE_RANGE,
            visibility : CwaffReticle.Visibility.CHARGING, background: true);
    }

    public override void Update()
    {
        base.Update();
        if (this.PlayerOwner is not PlayerController player)
            return;
        if (BraveTime.DeltaTime == 0.0f)
            return;
        this._mastered = player.HasSynergy(Synergy.MASTERY_GUNBRELLA);
        if (this._mastered && !this._masteryVolleyReplaced)
        {
            ProjectileVolleyData projectileVolleyData = ScriptableObject.CreateInstance<ProjectileVolleyData>();
            projectileVolleyData.projectiles = new(){ ProjectileModule.CreateClone(_MasteryModule) };
            this.gun.RawSourceVolley = projectileVolleyData;
            player.stats.RecalculateStats(player); // makes sure the volley is actually updated
            this._masteryVolleyReplaced = true;
        }
        if (!this.gun.IsCharging && !this.gun.IsFiring)
        {
            this._curChargeTime = 0.0f;
            return;
        }

        Lazy.PlaySoundUntilDeathOrTimeout(soundName: "gunbrella_charge_sound", source: this.gun.gameObject, timer: 0.05f);

        if (this._curChargeTime == 0.0f)
        {
            this._nextProjectileNumber = 0;
            this._chargeStartPos = this.gun.barrelOffset.PositionVector2();
        }

        this._chargeStartPos = base.GetComponent<CwaffReticle>().GetTargetPos();
        if (!GameManager.Instance.Dungeon.data.CheckInBoundsAndValid(this._chargeStartPos.ToIntVector2(VectorConversions.Floor))) // constrain to current room
        {
            Vector2 gunPos = this.gun.barrelOffset.PositionVector2();
            this._chargeStartPos = gunPos.ToNearestWall(out Vector2 normal, (this._chargeStartPos - gunPos).ToAngle(), 0f);
        }
        this._curChargeTime += BraveTime.DeltaTime;
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        projectile.GetComponent<GunbrellaProjectile>().Setup(gunbrella: this, index: this._nextProjectileNumber++, target: this._chargeStartPos, mastered: this._mastered);
    }

    public Vector2 CurrentTarget() => this._chargeStartPos;
}

public class GunbrellaProjectile : MonoBehaviour
{
    private const float _SPREAD               = 1.5f;   // max distance from the target an individual projectile can land
    private const float _LAUNCH_SPEED         = 80.0f;  // speed at which projectiles rise / fall
    private const float _LAUNCH_TIME          = 0.35f;  // time spent rising
    private const float _HANG_TIME            = 0.05f;  // time spent between rising and falling
    private const float _FALL_TIME            = 0.3f;   // time spent falling
    private const float _SPEAR_TIME           = 0.1f;   // time spent falling when mastered
    private const float _HOME_STRENGTH        = 0.1f;   // amount we adjust our velocity each frame when launching
    private const float _DELAY                = 0.03f;  // delay between firing projectiles
    private const float _TIME_TO_REACH_TARGET = _LAUNCH_TIME + _HANG_TIME + _FALL_TIME;

    private static float _LastFireTime = 0.0f;
    private static float _LastLaunchTime = 0.0f;
    private static int   _LastLaunchIndex = 0;

    private Projectile _projectile   = null;
    private PlayerController _owner  = null;
    private float _lifetime          = 0.0f;
    private bool _intangible         = true;
    private Vector2 _exactTarget     = Vector2.zero;
    private Vector2 _startVelocity   = Vector2.zero;

    private bool _launching          = false;
    private bool _falling            = false;
    private float _extraDelay        = 0.0f; // must be public so unity serializes it properly with the prefab
    private bool _naturalSpawn       = false;
    private bool _mastered           = false;
    private Gunbrella _gunbrella     = null;

    public void Setup(Gunbrella gunbrella, int index, Vector2 target, bool mastered)
    {
        this._naturalSpawn = true;
        this._gunbrella   = gunbrella;
        this._extraDelay  = mastered ? 0f : _DELAY * index;
        this._exactTarget = target;
        this._mastered    = mastered;
    }

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._owner = this._projectile.Owner as PlayerController;
        if (!this._owner)
            return;

        if (this._naturalSpawn && !this._mastered && (_LastLaunchTime < BraveTime.ScaledTimeSinceStartup))
        {
            _LastLaunchIndex = 0;
            _LastLaunchTime  = BraveTime.ScaledTimeSinceStartup;
            this._projectile.gameObject.PlayUnique("gunbrella_fire_sound");
        }

        if (!this._naturalSpawn)
        {
            this._extraDelay = _DELAY * (_LastLaunchIndex++);
            AIActor target   = null;
            List<AIActor> enemies = this._owner.CurrentRoom.SafeGetEnemiesInRoom();
            if (enemies.Count > 0)
            {
                const int TRIES = 10;
                for (int i = 0; i < TRIES; ++i)
                {
                    AIActor enemy = enemies.ChooseRandom();
                    if (!enemy || !enemy.healthHaver || enemy.healthHaver.IsDead)
                        continue;
                    target = enemy;
                    break;
                }
            }
            this._exactTarget = target ? target.CenterPosition : this._owner.CenterPosition;
        }

        this._projectile.damageTypes &= (~CoreDamageTypes.Electric); // remove robot's electric damage type from the projectile
        this._projectile.specRigidbody.OnPreRigidbodyCollision += this.OnPreCollision;
        this._projectile.specRigidbody.OnPreTileCollision += this.OnPreTileCollision;

        this._startVelocity = this._projectile.Direction.ToAngle().AddRandomSpread(10f).ToVector(1f);

        this._projectile.collidesWithEnemies = false;
        this._projectile.specRigidbody.CollideWithOthers = false;
        this._projectile.specRigidbody.CollideWithTileMap = false;
        StartCoroutine(TakeToTheSkies());
    }

    private void OnPreCollision(SpeculativeRigidbody me, PixelCollider myPixelCollider, SpeculativeRigidbody other, PixelCollider otherPixelCollider)
    {
        if (this._intangible)
            PhysicsEngine.SkipCollision = true;
    }

    private void OnPreTileCollision(SpeculativeRigidbody me, PixelCollider myPixelCollider, PhysicsEngine.Tile other, PixelCollider otherPixelCollider)
    {
        if (this._intangible)
            PhysicsEngine.SkipCollision = true;
    }

    private IEnumerator TakeToTheSkies()
    {
        this._projectile.gameObject.SetLayerRecursively(LayerMask.NameToLayer("Unoccluded"));
        if (!this._mastered || !this._gunbrella)
        {
            // Phase 1 / 4 -- become intangible and launch to the skies
            Vector2 targetLaunchVelocity = (85f + 10f * UnityEngine.Random.value).ToVector(1f);
            this._projectile.IgnoreTileCollisionsFor(_TIME_TO_REACH_TARGET);
            this._projectile.SetSpeed(_LAUNCH_SPEED);
            this._projectile.baseData.range = float.MaxValue;
            this._launching = true;
            while (this._lifetime < _LAUNCH_TIME)
            {
                this._startVelocity = ((1f - _HOME_STRENGTH) * this._startVelocity) + (_HOME_STRENGTH * targetLaunchVelocity);
                this._projectile.SendInDirection(this._startVelocity, true);
                yield return null;
                this._lifetime += BraveTime.DeltaTime;
            }
            this._lifetime -= _LAUNCH_TIME;

            // Phase 2 / 4 -- slight delay
            this._launching = false;
            this._projectile.SetSpeed(0.01f);
            while (this._lifetime < (_HANG_TIME + this._extraDelay))
            {
                yield return null;
                this._lifetime += BraveTime.DeltaTime;
            }
            this._lifetime -= (_HANG_TIME + this._extraDelay);
        }
        else // if mastered, we skip the first two phases and fall straight from the sky
        {
            this._projectile.IgnoreTileCollisionsFor(_SPEAR_TIME);
            yield return null; // skip a frame so we can actually properly fall on the reticle
            this._projectile.IgnoreTileCollisionsFor(_SPEAR_TIME);
            this._projectile.baseData.range = float.MaxValue;
            this._exactTarget = this._gunbrella.CurrentTarget(); // update target immediately when falling for mastered projectiles
        }

        // Phase 3 / 4 -- fall from the skies
        float fallTime = this._mastered ? _SPEAR_TIME : _FALL_TIME;
        float launchSpeed = (this._mastered ? (_FALL_TIME / _SPEAR_TIME) : 1f) * _LAUNCH_SPEED;
        this._falling = true;
        Vector2 targetFallVelocity = (250f + 40f*UnityEngine.Random.value).ToVector(1f);
        this._projectile.SetSpeed(launchSpeed);
        Vector2 offsetTarget = this._exactTarget + Lazy.RandomVector(_SPREAD * UnityEngine.Random.value);
        this._projectile.specRigidbody.Position = new Position(offsetTarget + (fallTime * launchSpeed) * (-targetFallVelocity));
        this._projectile.specRigidbody.UpdateColliderPositions();
        this._projectile.SendInDirection(targetFallVelocity, true);

        EasyTrailBullet trail = this._projectile.gameObject.AddComponent<EasyTrailBullet>();
            trail.StartWidth = 0.2f;
            trail.EndWidth   = 0.01f;
            trail.LifeTime   = 0.1f;
            trail.BaseColor  = Color.cyan.WithAlpha(0.6f);
            trail.StartColor = Color.cyan.WithAlpha(0.6f);
            trail.EndColor   = Color.cyan.WithAlpha(0.2f);

        while (this._lifetime + BraveTime.DeltaTime < fallTime) // stop a frame early so we can collide with enemies on our last frame
        {
            this._lifetime += BraveTime.DeltaTime;
            yield return null;
        }
        this._lifetime -= fallTime;

        // Phase 4 / 4 -- become tangible, wait a frame to collide with enemies, then die
        this._projectile.specRigidbody.Velocity = this._projectile.specRigidbody.Velocity.normalized; // small but nonzero velocity
        this._projectile.specRigidbody.Position = new Position(offsetTarget);
        this._projectile.specRigidbody.UpdateColliderPositions();
        this._projectile.collidesWithEnemies = true;
        this._projectile.specRigidbody.CollideWithOthers = true;
        this._projectile.specRigidbody.CollideWithTileMap = true;
        this._intangible = false;
        yield return null;

        this._projectile.DieInAir();
    }
}
