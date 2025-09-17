

namespace CwaffingTheGungy;

public class BBGun : CwaffGun
{
    public static string ItemName         = "B. B. Gun";
    public static string ShortDescription = "Spare No One";
    public static string LongDescription  = "Fires a single large projectile that bounces off walls and knocks enemies around with extreme force. Projectile damage and knockback scale with projectile speed. Projectiles can be caught while in motion, restoring ammo but knocking the player back in the process. Ammo can only be regained by catching projectiles, interacting with them while they are at rest, or clearing a room.";
    public static string Lore             = "This gun was originally used in the mid-18th century for hunting turkeys, as they were the only birds slow enough to actually hit with any degree of reliability. While hunters quickly decided that using a large, slow, rolling projectile wasn't ideal for hunting, the gun's legacy lives on today in shooting arenas known as \"alleys\", where sporting enthusiasts roll similar projectiles against red and white wooden objects in hopes of scoring a \"turkey\" themselves.";

    private const float _BOUNCE_SPEED_DECAY = 0.8f;

    private static readonly float[] _CHARGE_LEVELS  = {0.25f,0.5f,1.0f,2.0f};
    internal static Projectile _PinProjectile = null;

    private float _lastCharge = 0.0f;
    private readonly List<TheBB> _extantBbs = new();

    public static void Init()
    {
        Lazy.SetupGun<BBGun>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.B, gunClass: GunClass.CHARGE, reloadTime: 0.5f, ammo: 1, canGainAmmo: false,
            shootFps: 10, chargeFps: 16, loopChargeAt: 32, muzzleVFX: "muzzle_b_b_gun", muzzleFps: 30, muzzleScale: 0.5f, muzzleAnchor: Anchor.MiddleCenter,
            fireAudio: "Play_WPN_seriouscannon_shot_01", reloadAudio: "Play_ENM_flame_veil_01")
          .AssignGun(out Gun gun)
          .InitProjectile(GunData.New(
            clipSize: -1, cooldown: 0.7f, angleVariance: 10.0f, shootStyle: ShootStyle.Charged, sequenceStyle: ProjectileSequenceStyle.Ordered,
            customClip: true, speed: 20f, range: 999999f, sprite: "bball", fps: 20, anchor: Anchor.MiddleCenter, hitSound: "bb_impact_sound",
            glowAmount: 1000f, glowColor: Color.magenta/*, anchorsChangeColliders: false, overrideColliderPixelSizes: new IntVector2(2, 2)*/))
          .Attach<PierceProjModifier>(pierce => {
            pierce.penetration = Mathf.Max(pierce.penetration, 999);
            pierce.penetratesBreakables = true; })
          .Attach<BounceProjModifier>(bounce => {
            bounce.numberOfBounces     = Mathf.Max(bounce.numberOfBounces, 999);
            bounce.chanceToDieOnBounce = 0f;
            bounce.percentVelocityToLoseOnBounce = 1f - _BOUNCE_SPEED_DECAY;
            bounce.onlyBounceOffTiles  = true; })
          .Attach<TheBB>()
          .CopyAllImpactVFX(Items.Crestfaller)
          .SetupChargeProjectiles(gun.DefaultModule, _CHARGE_LEVELS.Length, (i, p) => new() {
            Projectile = p.Clone(GunData.New(speed: 40f + 20f * i)),
            ChargeTime = _CHARGE_LEVELS[i] });

        _PinProjectile = Items.Ak47.CloneProjectile(GunData.New(
            sprite: "bowling_pin_projectile", damage: 20.0f, speed: 40.0f, force: 40.0f, range: 80.0f, shouldRotate: false))
          .Attach<PinProjectile>()
          .Attach<EasyTrailBullet>(trail => {
            trail.TrailPos   = trail.transform.position;
            trail.StartWidth = 0.2f;
            trail.EndWidth   = 0.025f;
            trail.LifeTime   = 0.1f;
            trail.BaseColor  = Color.grey;
            trail.StartColor = Color.grey;
            trail.EndColor   = Color.Lerp(Color.grey, Color.white, 0.25f);
          });
    }

    public override void OnPlayerPickup(PlayerController player)
    {
        base.OnPlayerPickup(player);
        player.OnRoomClearEvent += this.OnRoomClear;
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        player.OnRoomClearEvent -= this.OnRoomClear;
        base.OnDroppedByPlayer(player);
    }

    public override void OnDestroy()
    {
        if (this.PlayerOwner)
            this.PlayerOwner.OnRoomClearEvent -= this.OnRoomClear;
        base.OnDestroy();
    }

    private void OnRoomClear(PlayerController player)
    {
        this.gun.CurrentAmmo = this.gun.AdjustedMaxAmmo;
        this.gun.ForceImmediateReload();
        foreach (TheBB bb in this._extantBbs)
            if (bb)
                bb.EraseFromExistence();
        this._extantBbs.Clear();
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        if (projectile.gameObject.GetComponent<TheBB>() is TheBB bb)
            this._extantBbs.Add(bb);
    }

    public class PinProjectile : MonoBehaviour
    {
        private const float _ROTSPEED = 2160f;

        private float _rotspeed        = 0f;
        private Projectile _proj       = null;
        private tk2dBaseSprite _sprite = null;
        private Transform _transform   = null;
        private float _rot             = 0f;

        private void Start()
        {
            this._proj      = base.gameObject.GetComponent<Projectile>();
            this._sprite    = base.gameObject.GetComponent<tk2dBaseSprite>();
            this._transform = base.gameObject.transform;
            this._rot       = Lazy.RandomAngle();
            this._rotspeed  = _ROTSPEED;

            this._proj.BulletScriptSettings.surviveRigidbodyCollisions = true;
            this._proj.specRigidbody.OnRigidbodyCollision += this.OnRigidbodyCollision;
            this._proj.DestroyMode = Projectile.ProjectileDestroyMode.DestroyComponent;
            this._proj.OnDestruction += this.OnProjectileDestroyed;
        }

        private void BecomeDebris(Vector2 force, float angularVelocity)
        {
            // Make into debris
            DebrisObject debris            = base.gameObject.GetOrAddComponent<DebrisObject>();
            debris.angularVelocity         = angularVelocity;
            debris.angularVelocityVariance = angularVelocity / 3f;
            debris.decayOnBounce           = 0.5f;
            debris.bounceCount             = 2;
            debris.canRotate               = true;
            debris.shouldUseSRBMotion      = true;
            debris.sprite                  = this._sprite;
            debris.animatePitFall          = true;
            // debris.audioEventName          = "monkey_tennis_bounce_first";
            debris.AssignFinalWorldDepth(-0.5f);
            debris.Trigger(force, 0.5f);

            this._proj.DieInAir();
            this._proj = null;
            UnityEngine.Object.Destroy(this);
        }

        private void OnProjectileDestroyed(Projectile projectile)
        {
            this._proj.OnDestruction -= this.OnProjectileDestroyed;
            BecomeDebris(force: 0.5f * this._proj.specRigidbody.Velocity, angularVelocity: 8f * this._proj.baseData.speed);
        }

        private void OnRigidbodyCollision(CollisionData rigidbodyCollision)
        {
            this._proj.specRigidbody.OnRigidbodyCollision -= this.OnRigidbodyCollision;
            this._proj.OnDestruction -= this.OnProjectileDestroyed;
            BecomeDebris(force: 0.5f * this._proj.baseData.speed * rigidbodyCollision.Normal, angularVelocity: 8f * this._proj.baseData.speed);
        }

        private void Update()
        {
            if (this._proj && this._proj.baseData.speed < 2f)
            {
                BecomeDebris(Vector2.zero, 0f);
                return;
            }
            if (this._rotspeed <= 0f)
                return;

            this._rotspeed = Mathf.Max(0f, this._rotspeed - 1000f * BraveTime.DeltaTime);
            this._rot += BraveTime.DeltaTime * this._rotspeed;
            this._transform.rotation = this._rot.EulerZ();
        }
    }
}

public class TheBB : MonoBehaviour
{
    private const float _BB_DAMAGE_SCALE    = 2.0f;
    private const float _BB_FORCE_SCALE     = 2.0f;
    private const float _BB_SPEED_DECAY     = 3.0f;
    private const float _BASE_EMISSION      = 3.0f;
    private const float _EXTRA_EMISSION     = 30.0f;
    private const float _BASE_ANIM_SPEED    = 2.0f;
    private const float _MIN_REFLECT_RADIUS = 1f;
    private const float _MAX_REFLECT_RADIUS = 2f;
    private const float _REFLECT_ANGLE_SNAP = 30f;
    private const float _DRAG = 0.9f;

    internal bool _createInteractible = true;
    internal Projectile _projectile;

    private PlayerController _owner;
    private float _maxSpeed = 0f;
    private float _damageMult = _BB_DAMAGE_SCALE;
    private float _knockbackMult = _BB_FORCE_SCALE;
    private bool _mastered = false;
    private bool _freebie = false;
    private DebrisObject _debris = null;
    private tk2dBaseSprite _sprite = null;

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._projectile.baseData.damping = 0.05f;
        this._owner = this._projectile.Owner as PlayerController;

        if (this._owner)
        {
            this._damageMult = _BB_DAMAGE_SCALE * this._owner.DamageMult();
            this._knockbackMult = _BB_FORCE_SCALE * this._owner.KnockbackMult();
            this._mastered = this._projectile.Mastered<BBGun>();
            this._projectile.collidesWithPlayer = true;
            this._projectile.UpdateCollisionMask();
            this._projectile.specRigidbody.OnPreRigidbodyCollision += this.OnMightCollideWithPlayer;
            _projectile.specRigidbody.m_specificCollisionExceptions.TryRemove(this._owner.specRigidbody);
        }

        this._freebie = this._projectile.FiredForFree();
        if (!this._freebie)
        {
            this._projectile.DestroyMode = Projectile.ProjectileDestroyMode.DestroyComponent;
            this._projectile.OnDestruction += this.BecomeDebrisImmediate;
        }
        this._maxSpeed = this._projectile.baseData.speed;

        this.GetComponent<BounceProjModifier>().OnBounce += this.OnBounce;
    }

    private void BecomeDebrisImmediate(Projectile proj)
    {
        base.gameObject.AllowFallingIntoPits();
        this._debris = base.gameObject.GetComponent<DebrisObject>();
        this._sprite = base.gameObject.GetComponentInChildren<tk2dBaseSprite>();
        if (this._sprite.spriteAnimator is tk2dSpriteAnimator animator)
            animator.Pause();
    }

    public void EraseFromExistence()
    {
        if (this._debris)
        {
            UnityEngine.Object.Destroy(base.gameObject);
            return;
        }

        this._createInteractible = false;
        this._projectile.DestroyMode = Projectile.ProjectileDestroyMode.Destroy;
        this._projectile.OnDestruction -= this.BecomeDebrisImmediate;
        SpawnManager.SpawnVFX(VFX.MiniPickup, this._projectile.SafeCenter, Lazy.RandomEulerZ());
        this._projectile.DieInAir(false, false, false, true);
    }

    private void OnMightCollideWithPlayer(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
    {
        if (!this._owner || this._owner.specRigidbody != otherRigidbody)
        {
            if (otherRigidbody.gameObject.GetComponent<PlayerController>())
                PhysicsEngine.SkipCollision = true; // don't collide with other player in coop
            return;
        }
        if (this._owner.FindBaseGun<BBGun>() is Gun gun)
        {
            gun.CanGainAmmo = true;
            gun.GainAmmo(1); // temporarily allow gaining ammo normally to take advantage of OnAmmoChanged callbacks
            gun.CanGainAmmo = false;
            gun.ForceImmediateReload();
        }
        // don't do knockback if player is behind us
        float angleToPlayer = (this._owner.CenterPosition - this._projectile.SafeCenter).ToAngle();
        float knockbackAngle = this._projectile.specRigidbody.Velocity.ToAngle();
        if (angleToPlayer.AbsAngleTo(knockbackAngle) < 85f)
            otherRigidbody.gameObject.GetComponent<KnockbackDoer>().ApplyKnockback(this._projectile.specRigidbody.Velocity, this._projectile.baseData.force);

        // do vfx / sfx as necessary
        float speed = this._projectile.m_currentSpeed;
        if (speed > 10f)
        {
            if (speed > 20f)
                GameManager.Instance.MainCameraController.DoScreenShake(new ScreenShakeSettings(0.01f * speed, 6f, 0.1f, 0f), this._projectile.SafeCenter);
            CwaffVFX.SpawnBurst(prefab: Groundhog._EarthClod, numToSpawn: 10, basePosition: this._projectile.SafeCenter, positionVariance: 1f,
              velType: CwaffVFX.Vel.AwayRadial, velocityVariance: 0.1f * speed, lifetime: 0.5f, fadeOutTime: 0.25f,
              startScale: 1.0f,  endScale: 0.1f, uniform: true, randomFrame: true);
        }
        otherRigidbody.gameObject.Play((speed > 20f) ? "bb_catch_sound" : "bb_impact_sound");

        // finish up
        EraseFromExistence();
        PhysicsEngine.SkipCollision = true;
    }

    private static AIActor CheckValidEnemyOwner(Projectile proj)
    {
        if (proj.Owner is not AIActor enemy)
            return null;
        if (!enemy || !enemy.isActiveAndEnabled || enemy.IsGone)
            return null;
        if (enemy.healthHaver is not HealthHaver hh || hh.IsDead)
            return null;
        return enemy;
    }

    private void ReflectNearbyProjectiles()
    {
        float mySpeed          = this._projectile.baseData.speed;
        float reflectRadius    = Mathf.Min(_MIN_REFLECT_RADIUS + 0.01f * mySpeed, _MAX_REFLECT_RADIUS);
        float reflectRadiusSqr = reflectRadius * reflectRadius;
        Vector2 pos            = this._projectile.SafeCenter;
        bool didReflect        = false;

        ReadOnlyCollection<Projectile> allProj = StaticReferenceManager.AllProjectiles;
        for (int j = allProj.Count - 1; j >= 0; j--)
        {
            Projectile p = allProj[j];
            if (!p || p.Owner is PlayerController)
                continue;

            Vector2 ppos = p.SafeCenter;
            Vector2 delta = ppos - pos;
            if (delta.sqrMagnitude > reflectRadiusSqr)
                continue;

            Vector2 dirToEnemy = (CheckValidEnemyOwner(p) is AIActor enemy) ? (enemy.CenterPosition - ppos) : -p.Direction;
            if (dirToEnemy.ToAngle().AbsAngleTo(delta.ToAngle()) < _REFLECT_ANGLE_SNAP)
                delta = dirToEnemy;

            p.DieInAir(true, false, false, true);
            Projectile pin = SpawnManager.SpawnProjectile(
                prefab   : BBGun._PinProjectile.gameObject,
                position : ppos,
                rotation : delta.EulerZ()).GetComponent<Projectile>();
            pin.SetOwnerAndStats(this._owner);
            pin.SetSpeed(mySpeed + 10f);
            didReflect = true;
        }
        if (didReflect)
            base.gameObject.Play("bowling_pin_sound");
        // #if DEBUG
        //     base.gameObject.DrawDebugCircle(pos, reflectRadius, Color.green.WithAlpha(0.15f));
        // #endif
    }

    public void OnBounce()
    {
        base.gameObject.Play("bb_impact_sound");
    }

    private void DoDebrisChecks()
    {
        const float PICKUP_RADIUS = 3f;
        const float PICKUP_RADIUS_SQR = PICKUP_RADIUS * PICKUP_RADIUS;

        Vector2 pos = this._sprite ? this._sprite.WorldCenter : base.transform.position;
        for (int i = 0; i < GameManager.Instance.AllPlayers.Length; ++i)
        {
            PlayerController p = GameManager.Instance.AllPlayers[i];
            if (!p || p.IsGhost || p.healthHaver.IsDead)
                continue;
            if ((p.CenterPosition - pos).sqrMagnitude > PICKUP_RADIUS_SQR)
                continue;
            if ((p.FindBaseGun<BBGun>() is not Gun gun))
                continue;
            if (gun.CurrentAmmo < gun.AdjustedMaxAmmo)
                gun.CurrentAmmo += 1;
            p.gameObject.PlayUnique("card_pickup_sound");
            SpawnManager.SpawnVFX(VFX.MiniPickup, pos, Lazy.RandomEulerZ());
            UnityEngine.Object.Destroy(base.gameObject);
            break;
        }
    }

    private void Update()
    {
        if (this._debris)
        {
            DoDebrisChecks();
            return;
        }

        float speed = this._projectile.m_currentSpeed;
        if (speed <= 1)
        {
            this._projectile.DieInAir(suppressInAirEffects: true);
            return;
        }
        if (speed <= 10)
            this._projectile.baseData.damping = 0.25f;

        Material m = this._projectile.sprite.renderer.material;
        m.SetFloat(CwaffVFX._EmissivePowerId, _BASE_EMISSION + _EXTRA_EMISSION * (speed / _maxSpeed));
        m.SetFloat("_Cutoff", 0.1f);

        this._projectile.baseData.damage        = this._damageMult * speed;
        this._projectile.baseData.force         = this._knockbackMult * speed;
        this._projectile.spriteAnimator.ClipFps = Mathf.Min(_BASE_ANIM_SPEED * speed, 60f);
        this._projectile.LoopSoundIf(true, "bb_rolling");

        if (this._mastered)
            ReflectNearbyProjectiles();
    }
}
