namespace CwaffingTheGungy;

public class Bubblebeam : CwaffGun
{
    public static string ItemName         = "Bubblebeam";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    internal static GameObject _BubbleVFX = null;
    internal static GameObject _BurstBubbleVFX = null;

    public static void Init()
    {
        Gun gun = Lazy.SetupGun<Bubblebeam>(ItemName, ShortDescription, LongDescription, Lore);
            gun.SetAttributes(quality: ItemQuality.B, gunClass: CwaffGunClass.UTILITY, reloadTime: 1.25f, ammo: 2400, shootFps: 60, reloadFps: 24,
                fireAudio: "bubble_pop_sound");
            gun.carryPixelOffset = new IntVector2(14, 4);
            gun.SetReloadAudio("bubblebeam_close_sound", 1);
            gun.SetReloadAudio("seltzer_insert_sound", 9);
            gun.SetReloadAudio("seltzer_shake_sound", 17, 19, 21, 23, 25, 27);

        gun.InitProjectile(GunData.New(sprite: "bubblebeam_projectile", fps: 16, clipSize: 100, cooldown: 0.05f, shootStyle: ShootStyle.Automatic,
            collidesWithProjectiles: true, damage: 0.0f, speed: 35f, range: 18f, force: 10f, scale: 0.5f))
          .Attach<BubblebeamProjectile>();

        _BubbleVFX = VFX.Create("capture_bubble", fps: 6);
        _BurstBubbleVFX = VFX.Create("burst_bubble_vfx", fps: 16, loops: false);
    }

    private int _lastReloadBubbleFrame = 0;
    public override void Update()
    {
        base.Update();
        if (!this.gun.IsReloading)
            return;
        int frame = this.gun.spriteAnimator.CurrentFrame;
        if (frame < 17 || frame == this._lastReloadBubbleFrame)
            return;
        if (BraveTime.DeltaTime == 0.0f)
            return;
        this._lastReloadBubbleFrame = frame;
        CwaffVFX.SpawnBurst(
            prefab           : _BurstBubbleVFX,
            numToSpawn       : 2,
            basePosition     : this.gun.barrelOffset.position,
            positionVariance : 1f,
            baseVelocity     : new Vector2(0.0f, 2.5f),
            velocityVariance : 2.5f,
            velType          : CwaffVFX.Vel.Random,
            rotType          : CwaffVFX.Rot.Random,
            lifetime         : 0.5f,
            fadeOutTime      : 0.1f
          );
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        CwaffVFX.SpawnBurst(
            prefab           : _BurstBubbleVFX,
            numToSpawn       : 4,
            basePosition     : this.gun.barrelOffset.position,
            positionVariance : 1f,
            velocityVariance : 5f,
            velType          : CwaffVFX.Vel.Random,
            rotType          : CwaffVFX.Rot.Random,
            lifetime         : 0.5f,
            fadeOutTime      : 0.1f
          );
    }
}

public class BubblebeamProjectile : MonoBehaviour
{
    private Projectile _projectile;
    private PlayerController _owner;
    private Bubblebeam _gun;
    private float _force = 0f;

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._owner = this._projectile.Owner as PlayerController;
        if (!this._owner)
            return;

        if (this._owner.CurrentGun is Gun gun)
            this._gun = gun.gameObject.GetComponent<Bubblebeam>();

        this._force = this._projectile.baseData.force;
        this._projectile.baseData.force = 0f; // don't want to apply normal knockback
        this._projectile.specRigidbody.OnPreRigidbodyCollision += this.OnPreRigidbodyCollision;
        this._projectile.OnHitEnemy += this.OnHitEnemy;
        this._projectile.OnDestruction += OnDestruction;
    }

    private static void OnDestruction(Projectile proj)
    {
        if (!proj || !proj.gameObject)
            return;

        CwaffVFX.SpawnBurst(
            prefab           : Bubblebeam._BurstBubbleVFX,
            numToSpawn       : 10,
            basePosition     : proj.SafeCenter,
            positionVariance : 1f,
            velocityVariance : 5f,
            velType          : CwaffVFX.Vel.Random,
            rotType          : CwaffVFX.Rot.Random,
            lifetime         : 0.5f,
            fadeOutTime      : 0.1f
          );
        proj.gameObject.Play("bubble_pop_sound");
        proj.OnDestruction -= OnDestruction;
    }

    private void OnPreRigidbodyCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
    {
        if (!this._projectile)
            return;
        if (otherRigidbody.gameObject.GetComponent<Projectile>() is not Projectile p)
            return;
        if (p.gameObject.GetComponent<BubblebeamProjectile>())
        {
            PhysicsEngine.SkipCollision = true;
            return;
        }
        if (p.Owner is not AIActor enemy)
            return;
        EnbubbleProjectile(p);
        PopSelf();
        PhysicsEngine.SkipCollision = true;
    }

    private void PopSelf()
    {
        this._projectile.DieInAir();
    }

    private void EnbubbleProjectile(Projectile p)
    {
        if (p.specRigidbody)
            p.gameObject.GetOrAddComponent<EnbubbledBehaviour>().PushBubble(this._projectile, this._force);
    }

    private const float _BASE_ENBUBBLE_ENEMY_CHANCE = 0.5f;
    private void OnHitEnemy(Projectile p, SpeculativeRigidbody enemy, bool _)
    {
        if (enemy.gameObject.GetComponent<EnbubbledBehaviour>() is EnbubbledBehaviour ebb)
        {
            ebb.PushBubble(this._projectile, this._force);
            return;
        }
        if (enemy.gameObject.GetComponent<tk2dBaseSprite>() is not tk2dBaseSprite sprite)
            return;
        float enbubbleChance = _BASE_ENBUBBLE_ENEMY_CHANCE / Mathf.Max(1f, 2f * sprite.GetCurrentSpriteDef().boundsDataExtents.y);
        if (UnityEngine.Random.value > enbubbleChance)
            return;
        if (enemy.gameObject.GetComponent<AIActor>() is not AIActor actor)
            return;
        if (enemy.gameObject.GetComponent<HealthHaver>() is not HealthHaver hh || hh.IsBoss || hh.IsSubboss)
            return;
        if (actor.gameObject.GetComponent<BehaviorSpeculator>() is not BehaviorSpeculator bs)
            return;
        if (bs.ImmuneToStun)
            return;
        actor.gameObject.AddComponent<EnbubbledBehaviour>().PushBubble(this._projectile, this._force);
    }
}

public class EnbubbledBehaviour : MonoBehaviour
{
    private const float _BUBBLE_FORCE = 0.01f;
    private const string _FLIGHT_REASON = "Enbubbled";
    private const float _MIN_DRIFT = 2.5f;
    private const float _BUBBLE_DRIFT_DECAY = 1f;

    private GameObject _vfx = null;
    private AIActor _enemy = null;
    private Projectile _proj = null;
    private tk2dSprite _bubble = null;
    private float _lifetime = 0.0f;
    private bool _setup = false;
    private Vector2 _drift = Vector2.zero;
    private float _vfxSize = 1f;
    private bool _projectileCollisionOverride = false;
    private bool _enemyCollisionOverride = false;
    private Transform _parent = null;
    private tk2dBaseSprite _parentSprite = null;
    private Vector3 _startPos;
    private SpeculativeRigidbody _body;

    public void PushBubble(Projectile pusher, float force)
    {
        Setup();
        PushEnemyBubble(pusher, force);
        PushProjectileBubble(pusher, force);
    }

    private void PushEnemyBubble(Projectile pusher, float force)
    {
        if (!this._enemy || !this._enemy.specRigidbody || !this._enemy.knockbackDoer)
            return;

        float weightFactor = (0.1f * Mathf.Max(1f, this._enemy.knockbackDoer.weight));
        this._enemy.specRigidbody.Velocity += (_BUBBLE_FORCE * force * pusher.Speed * pusher.Direction) / weightFactor;
        UpdateKnockback();
    }

    private void PushProjectileBubble(Projectile pusher, float force)
    {
        if (!this._proj || !this._proj.specRigidbody)
            return;
        Vector2 newVel = this._proj.specRigidbody.Velocity + (_BUBBLE_FORCE * force * pusher.Speed * pusher.Direction);
        this._proj.SetSpeed(newVel.magnitude);
        this._proj.SendInDirection(newVel, true, true);
    }

    private void Start()
    {
        Setup();
    }

    private void Setup()
    {
        if (this._setup)
            return;

        this._setup = true;
        this._enemy = base.gameObject.GetComponent<AIActor>();
        this._proj = base.gameObject.GetComponent<Projectile>();
        if (this._enemy && this._enemy.sprite && this._enemy.specRigidbody)
            SetupForEnemy();
        else if (this._proj && this._proj.sprite && this._proj.specRigidbody)
            SetupForProjectile();
        else
            return;

        this._vfx = SpawnManager.SpawnVFX(Bubblebeam._BubbleVFX, this._startPos, Quaternion.identity, ignoresPools: true);
        this._vfx.transform.parent = this._parent;
        this._bubble = this._vfx.GetComponent<tk2dSprite>();
        this._bubble.PlaceAtScaledPositionByAnchor(this._startPos, Anchor.MiddleCenter);
        this._bubble.SetAlphaImmediate(0.25f);

        tk2dSpriteDefinition parentSpriteDef = this._parentSprite.GetCurrentSpriteDef();
        tk2dSpriteDefinition bubbleDef = this._bubble.GetCurrentSpriteDef();
        this._vfxSize = bubbleDef.boundsDataExtents.y;
        float scale = 0.25f + Mathf.Max(parentSpriteDef.boundsDataExtents.x, parentSpriteDef.boundsDataExtents.y) / this._vfxSize;
        this._bubble.scale = new Vector3(scale, scale, 1f);
        this._vfxSize = 0.5f * bubbleDef.boundsDataExtents.y * scale;
    }

    private void SetupForEnemy()
    {
        this._body = this._enemy.specRigidbody;
        this._enemy.SetIsFlying(true, _FLIGHT_REASON, true, true);
        this._body.OnCollision += this.OnAnyCollision;
        if (!this._body.HasCollisionLayerIgnoreOverride(CollisionMask.LayerToMask(CollisionLayer.Projectile)))
        {
            this._projectileCollisionOverride = true;
            this._enemy.HitByEnemyBullets = true;
            this._body.AddCollisionLayerOverride(CollisionMask.LayerToMask(CollisionLayer.Projectile));
        }
        // if (!this._body.HasCollisionLayerIgnoreOverride(CollisionMask.LayerToMask(CollisionLayer.EnemyHitBox)))
        // {
        //     this._enemyCollisionOverride = true;
        //     this._body.AddCollisionLayerOverride(CollisionMask.LayerToMask(CollisionLayer.EnemyHitBox));
        // }
        if (this._enemy.behaviorSpeculator is BehaviorSpeculator bs && !bs.ImmuneToStun)
            bs.Stun(36000f, false);

        this._parent = this._enemy.transform;
        this._startPos = this._enemy.CenterPosition;
        this._parentSprite = this._enemy.sprite;
    }

    private void SetupForProjectile()
    {
        this._body = this._proj.specRigidbody;
        this._proj.RemoveBulletScriptControl();
        if (this._body && this._proj.Owner && this._proj.Owner.specRigidbody)
            this._body.DeregisterSpecificCollisionException(this._proj.Owner.specRigidbody);
        this._proj.baseData.range = 9999f;
        this._proj.allowSelfShooting = true;
        this._proj.collidesWithEnemies = true;
        this._proj.collidesWithPlayer = false;
        this._proj.collidesWithProjectiles = true;
        this._proj.collidesOnlyWithPlayerProjectiles = true;
        this._proj.ChangeTintColorShader(0f, Color.cyan);
        this._proj.baseData.damage = Mathf.Max(15f, this._proj.baseData.damage, ProjectileData.FixedFallbackDamageToEnemies);
        this._proj.UpdateCollisionMask();
        this._proj.ResetDistance();

        this._proj.BulletScriptSettings.surviveTileCollisions = true;
        this._body.OnCollision += this.OnAnyCollision;
        this._proj.OnDestruction += this.OnDestruction;
        this._parent = this._proj.transform;
        this._startPos = this._proj.SafeCenter;
        this._parentSprite = this._proj.sprite;
    }

    private void OnDestruction(Projectile proj)
    {
        UnityEngine.Object.Destroy(this);
    }

    private void OnAnyCollision(CollisionData collision)
    {
      if (collision.collisionType == CollisionData.CollisionType.TileMap)
      {
          Vector2 v2 = collision.MyRigidbody.Velocity;
          Vector2 newVel2 = collision.CollidedX ? v2.WithX(-v2.x) : v2.WithY(-v2.y);
          PhysicsEngine.PostSliceVelocity = newVel2;
          if (this._enemy)
              this._enemy.KnockbackVelocity = newVel2;
          else if (this._proj)
              this._proj.SendInDirection(newVel2, true);
          return;
      }
      if (this._proj)
      {
          BurstYourBubble();
          return;
      }
      if (collision.OtherRigidbody is not SpeculativeRigidbody other)
        return;
      if (other.gameObject.GetComponent<Projectile>() is Projectile p)
      {
        if (!p.gameObject.GetComponent<BubblebeamProjectile>())
        {
            p.DieInAir();
            BurstYourBubble();
        }
        return;
      }
      if (other.gameObject.GetComponent<PlayerController>())
      {
        BurstYourBubble();
        return;
      }
      Vector2 v = collision.MyRigidbody.Velocity;
      Vector2 newVel = collision.CollidedX ? v.WithX(-v.x) : v.WithY(-v.y);
      PhysicsEngine.PostSliceVelocity = newVel;
      if (this._enemy)
        this._enemy.KnockbackVelocity = newVel;
    }

    private void BurstYourBubble()
    {
        if (this._parentSprite)
        {
            float targetSize = this._parentSprite.GetCurrentSpriteDef().boundsDataExtents.y;
            CwaffVFX.SpawnBurst(
                prefab           : Bubblebeam._BurstBubbleVFX,
                numToSpawn       : Mathf.RoundToInt(10f * this._vfxSize),
                basePosition     : this._parentSprite.WorldCenter,
                positionVariance : this._vfxSize,
                velocityVariance : 5f,
                velType          : CwaffVFX.Vel.Random,
                rotType          : CwaffVFX.Rot.Random,
                lifetime         : 0.5f,
                fadeOutTime      : 0.1f
              );
            this._parentSprite.gameObject.Play("bubble_pop_sound");
        }
        if (this._enemy)
        {
            this._enemy.SetIsFlying(false, _FLIGHT_REASON, true, true);
            if (this._enemy.behaviorSpeculator is BehaviorSpeculator bs && !bs.ImmuneToStun)
                bs.EndStun();
            if (this._enemy.knockbackDoer is KnockbackDoer kb)
            {
                kb.m_activeKnockbacks.Clear();
                kb.m_activeContinuousKnockbacks.Clear();
            }
        }
        if (this._proj)
            this._proj.DieInAir();
        UnityEngine.Object.Destroy(this);
    }

    private void UpdateKnockback()
    {
        if (this._enemy && this._enemy.specRigidbody)
            this._enemy.KnockbackVelocity = this._enemy.specRigidbody.Velocity;
    }

    private void Update()
    {
        if (this._proj)
        {
            if (this._proj.Speed < _MIN_DRIFT)
                return;
            this._proj.SetSpeed(Lazy.SmoothestLerp(this._proj.Speed, 0, _BUBBLE_DRIFT_DECAY));
            return;
        }
        if (!this._enemy || !this._body)
            return;

        Vector2 v = this._body.Velocity;
        float sqrMag = v.sqrMagnitude;
        if (sqrMag <= _MIN_DRIFT * _MIN_DRIFT)
            return;

        float newMag = Mathf.Max(_MIN_DRIFT, Lazy.SmoothestLerp(v.magnitude, 0, _BUBBLE_DRIFT_DECAY));
        this._body.Velocity = newMag * v.normalized;
        UpdateKnockback();
    }

    private void LateUpdate()
    {
        this._bubble.HeightOffGround = 4f;
        this._bubble.UpdateZDepth();
    }

    private void OnDestroy()
    {
        if (this._enemy && this._enemy.specRigidbody)
        {
            this._enemy.specRigidbody.OnCollision -= this.OnAnyCollision;
            if (this._projectileCollisionOverride)
            {
                this._enemy.HitByEnemyBullets = false;
                this._enemy.specRigidbody.RemoveCollisionLayerOverride(CollisionMask.LayerToMask(CollisionLayer.Projectile));
            }
            // if (this._enemyCollisionOverride)
            //     this._enemy.specRigidbody.RemoveCollisionLayerOverride(CollisionMask.LayerToMask(CollisionLayer.EnemyHitBox));
        }
        else if (this._proj && this._proj.specRigidbody)
        {
            this._proj.specRigidbody.OnCollision -= this.OnAnyCollision;
            this._proj.OnDestruction -= this.OnDestruction;
        }
        if (this._vfx)
            UnityEngine.Object.Destroy(this._vfx);
    }

    /// <summary>Handle knockback ourselves when bubblified and ignore the base knockback doer</summary>
    [HarmonyPatch(typeof(KnockbackDoer), nameof(KnockbackDoer.Update))]
    private class KnockbackDoerUpdateForBubblePatch
    {
        static bool Prefix(KnockbackDoer __instance)
        {
            if (__instance.gameObject.GetComponent<EnbubbledBehaviour>() is not EnbubbledBehaviour eeb)
                return true; // call the original method
            eeb.UpdateKnockback();
            return false; // skip the original method
        }
    }
}