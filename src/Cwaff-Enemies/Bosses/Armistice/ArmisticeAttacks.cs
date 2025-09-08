namespace CwaffingTheGungy;

public partial class ArmisticeBoss : AIActor
{
  private static Vector3 GunBarrelOffset(bool facingLeft)
     => C.PIXEL_SIZE * new Vector3(facingLeft ? -36f : 36f, 23f, 0f);

  private static Vector3 GunBarrelHighOffset(bool facingLeft)
     => C.PIXEL_SIZE * new Vector3(facingLeft ? -15f : 15f, 48f, 0f);

  private static Vector3 GunBarrelLowOffset(bool facingLeft)
     => C.PIXEL_SIZE * new Vector3(facingLeft ? -29f : 29f, 18f, 0f);

  private class ArmisticeMoveAndShootBehavior : MoveAndShootBehavior
  {
    private const float _MAX_RELOCATE_TIME = 0.75f;
    private const float _MIN_RELOCATE_SPEED = 40f;
    private const float _AFTERIMAGE_RATE = 0.05f;

    private Rect _roomBounds;
    private Vector2 _startPos;
    private Vector2 _targetPos;
    private Vector2 _targetDelta;
    private float _travelStartTime;
    private float _travelEndTime;
    private float _travelDeltaTime;
    private Transform _transform;
    private SpeculativeRigidbody _body;
    private float _nextAfterimage;
    private PlayerController _targetPlayer;

    private void DetermineTarget()
    {
      bool playerOnLeft = this._targetPlayer.CenterPosition.x < this._roomBounds.center.x;
      bool playerOnBottom = this._targetPlayer.CenterPosition.y < this._roomBounds.center.y;
      bool selfOnLeft = base.m_aiActor.CenterPosition.x < this._roomBounds.center.x;
      bool selfOnBottom = base.m_aiActor.CenterPosition.y < this._roomBounds.center.y;

      string scriptType = base.BulletScript.scriptTypeName.Split('+')[1].Split(',')[0];
      // System.Console.WriteLine($"reloacting for bullet script {scriptType}");
      switch (scriptType)
      {
        case "ClocksTickingScript":
          this._targetPos = this._roomBounds.center;
          break;
        case "BoneTunnelScript":
        case "TrickshotScript":
        case "SniperScript":
          float playerY = Mathf.Clamp(this._targetPlayer.sprite.WorldBottomCenter.y, this._roomBounds.yMin, this._roomBounds.yMax);
          this._targetPos = new Vector2( // cross to the far side of the room opposite to the player
            this._roomBounds.xMin + (playerOnLeft ? 1.0f : 0.0f) * this._roomBounds.width, playerY);
          break;
        case "BoxTrotScript":
          this._targetPos = new Vector2( // cross to the other side of the room
            this._roomBounds.xMin + (selfOnLeft ? 0.75f : 0.25f) * this._roomBounds.width,
            this._roomBounds.center.y + UnityEngine.Random.Range(-0.2f, 0.2f) * this._roomBounds.height);
          break;
        case "LaserBarrageScript":
          this._targetPos = new Vector2(playerOnLeft ? this._roomBounds.xMax : this._roomBounds.xMin, this._roomBounds.center.y);
          break;
        case "MeteorShowerScript":
          this._targetPos = new Vector2( // cross to the other side of the room, stand near the bottom
            this._roomBounds.xMin + (selfOnLeft ? 0.75f : 0.25f) * this._roomBounds.width,
            this._roomBounds.center.y + UnityEngine.Random.Range(-0.4f, -0.25f) * this._roomBounds.height);
          break;
        case "MagicMissileScript":
          this._targetPos = new Vector2( // cross to the other side of the room, near the top
            this._roomBounds.xMin + (selfOnLeft ? 0.75f : 0.25f) * this._roomBounds.width,
            this._roomBounds.center.y + 0.4f * this._roomBounds.height);
          break;
        case "CrossBulletsScript":
        case "WalledInScript":
        case "DanceMonkeyScript":
        case "PendulumScript":
          break;
      }
    }

    /// <summary>Performs setup for calling Relocate() in future frames.</summary>
    protected internal override void PrepareToRelocate(Vector2? overridePos = null)
    {
      this._transform = base.m_gameObject.transform;
      this._body = m_gameObject.GetComponent<SpeculativeRigidbody>();
      this._startPos = this._transform.position;
      this._roomBounds = this._startPos.GetAbsoluteRoom().GetBoundingRect().Inset(2f);
      this._targetPlayer = GameManager.Instance.GetRandomActivePlayer();
      this._targetPos = new PathRect(this._roomBounds).At(UnityEngine.Random.value, UnityEngine.Random.value);

      if (overridePos.HasValue)
        this._targetPos = overridePos.Value;
      else
        DetermineTarget();

      this._targetDelta = this._targetPos - this._startPos;
      this._travelStartTime = BraveTime.ScaledTimeSinceStartup;
      this._travelDeltaTime = Mathf.Min(this._targetDelta.magnitude / _MIN_RELOCATE_SPEED, _MAX_RELOCATE_TIME);
      this._travelEndTime = this._travelStartTime + this._travelDeltaTime;
      if (this._travelDeltaTime >= BraveTime.DeltaTime) // don't update unless we actually have somewhere to move
      {
        this.m_aiAnimator.PlayUntilCancelled("run", true);
        this.m_aiActor.sprite.FlipX = this._targetPos.x < this._startPos.x;
      }
    }

    /// <summary>Returns true if we're in position to attack, false otherwise.</summary>
    protected internal override bool Relocate()
    {
      float now = BraveTime.ScaledTimeSinceStartup;
      if (now >= this._travelEndTime)
      {
        this.m_aiActor.sprite.FlipX = this._transform.position.x > this._roomBounds.center.x;
        this.m_aiAnimator.PlayUntilCancelled("idle", true);
        this._transform.position = this._targetPos;
        this._body.Reinitialize();
        return true;
      }

      if (now >= this._nextAfterimage)
      {
        this.m_aiActor.sprite.SpriteAfterImage();
        this._nextAfterimage = now + _AFTERIMAGE_RATE;
      }
      float t = (now - this._travelStartTime) / this._travelDeltaTime;
      this._transform.position = this._startPos + t * this._targetDelta;
      this._body.Reinitialize();

      return false;
    }
  }

  private class SecretBullet : Bullet
  {
      private static readonly Color _DefaultTint = new Color(1.0f,0.5f,0.5f,0.5f);

      private Color? _tint = null;
      private float _emission;
      private float _emitColorPower;
      private bool _fancySpawn;
      private Shader _originalShader = null;

      protected int originalLayer = -1;
      protected GameObject _trail = null;

      public SecretBullet(Color? tint = null, float emission = 10f, float emitColorPower = 1.5f, bool fancySpawn = false, string baseProj = "getboned") : base(baseProj)
      {
        this._tint = tint;
        this._emission = emission;
        this._emitColorPower = emitColorPower;
        this._fancySpawn = fancySpawn;
      }

      public void AddTrail(CwaffTrailController trailPrefab, float glow = -1f)
      {
        this._trail = this.Projectile.AddTrail(trailPrefab).gameObject;
        if (glow >= 0f)
          this._trail.SetGlowiness(glow);
      }

      protected virtual void OnBaseDestruction(Projectile projectile)
      {
        if (this._trail)
        {
          // UnityEngine.Object.Destroy(this._trail);
          this._trail.GetComponent<CwaffTrailController>().DisconnectFromSpecRigidbody();
          this._trail = null;
        }
        if (this.originalLayer > -1)
        {
          projectile.gameObject.SetLayerRecursively(this.originalLayer);
          this.originalLayer = -1;
        }
        if (this._fancySpawn)
          CwaffVFX.Spawn(_BulletSpawnVFX, position: projectile.SafeCenter, startScale: 0.5f, endScale: 0.1f);
        tk2dBaseSprite sprite = projectile.sprite;
        if (sprite && sprite.usesOverrideMaterial)
        {
          sprite.usesOverrideMaterial = false;
          if (sprite.renderer && sprite.renderer.material)
            sprite.renderer.material.shader = this._originalShader;
        }
        projectile.OnDestruction -= this.OnBaseDestruction;
      }

      public override void Initialize()
      {
        this.Projectile.ChangeTintColorShader(0f, this._tint ?? _DefaultTint);
        this.Projectile.OnDestruction += this.OnBaseDestruction;
        if (this._fancySpawn)
          CwaffVFX.Spawn(_BulletSpawnVFX, position: this.Projectile.SafeCenter, startScale: 0.1f, endScale: 1.0f,
            anchorTransform: this.Projectile.gameObject.transform);
        Material mat = this.Projectile.sprite.renderer.material;
        mat.SetFloat(CwaffVFX._EmissivePowerId, this._emission);
        mat.SetFloat(CwaffVFX._EmissiveColorPowerId, this._emitColorPower);
        this._originalShader = mat.shader;

        base.Initialize();
      }
  }

  private abstract class ArmisticeBulletScript : FluidBulletScript
  {
    protected AIActor theBoss          {get; private set;}
    protected Rect    roomFullBounds   {get; private set;}
    protected Rect    roomBulletBounds {get; private set;}
    protected Rect    roomSlamBounds   {get; private set;}

    public override void Initialize()
    {
      base.Initialize();
      this.theBoss            = this.BulletBank.aiActor;
      this.roomFullBounds     = this.theBoss.GetAbsoluteParentRoom().GetBoundingRect();
      this.roomBulletBounds   = this.roomFullBounds.Inset(topInset: 2f, rightInset: 2f, bottomInset: 4f, leftInset: 2f);
      this.roomSlamBounds     = this.roomFullBounds.Inset(topInset: 2f, rightInset: 2.5f, bottomInset: 2f, leftInset: 1.5f);
    }

    /// <summary>Hijack ArmisticeMoveAndShootBehavior's relocation logic</summary>
    public IEnumerator Relocate(Vector2? overridePos = null)
    {
      if (base.BulletBank.aiActor.behaviorSpeculator.ActiveContinuousAttackBehavior is not BehaviorBase bb)
        yield break;
      while (bb is AttackBehaviorGroup abg && abg.CurrentBehavior != null)
        bb = abg.CurrentBehavior;
      if (bb is not ArmisticeMoveAndShootBehavior amb)
        yield break;

      float oldScale = base.TimeScale;
      base.TimeScale = -1f; // update every frame while relocating
      amb.PrepareToRelocate(overridePos: overridePos);
      while (!amb.Relocate())
        yield return Wait(0);
      base.TimeScale = oldScale;

      yield break;
    }
  }

  // Shoots bullets to the top corners of the screen that violently fly down towards the bottom center after a short delay
  private class CrossBulletsScript : ArmisticeBulletScript
  {

    internal class CrossBullet : SecretBullet
    {
      private const int     LIFETIME       = 30;
      private const int     VANISHTIME     = 120;
      private       Vector2 gravity        = Vector2.zero;
      private       Vector2 startVelocity  = Vector2.zero;
      private       Vector2 target         = Vector2.zero;
      private       Rect    roomFullBounds;
      private       bool    snapped = false;

      public CrossBullet(Vector2 velocity, Vector2 gravity, Vector2 target, Rect roomFullBounds) : base()
      {
        this.gravity        = gravity;
        this.startVelocity  = velocity;
        this.target         = target;
        this.roomFullBounds = roomFullBounds;
      }

      public override void Initialize()
      {
        base.Initialize();
        this.Projectile.specRigidbody.CollideWithTileMap = false;
        this.Projectile.specRigidbody.CollideWithOthers = false;
      }

      public override IEnumerator Top()
      {
        // this.Projectile.gameObject.Play(SOUND_SHOOT);
        this.originalLayer = this.Projectile.gameObject.layer;
        this.Projectile.gameObject.SetLayerRecursively(LayerMask.NameToLayer("Unoccluded"));
        Vector2 newVelocity = this.startVelocity;
        for (int i = 0; i < VANISHTIME; ++i)
        {
          if (i >= LIFETIME && this.roomFullBounds.Contains(this.Position))
          {
            this.Projectile.specRigidbody.CollideWithTileMap = true;
            this.Projectile.specRigidbody.CollideWithOthers = true;
          }
          if (this.snapped)
          {
            yield return Wait(1);
            continue;
          }
          if (newVelocity.y < 0)
          {
            Vector2 toTarget = (this.target - this.Position);
            this.ChangeDirection(new Direction(toTarget.ToAngle(), DirectionType.Absolute));
            this.ChangeSpeed(new Speed(STRIKESPEED, SpeedType.Absolute));
            this.Projectile.gameObject.PlayUnique("subtractor_beam_fire_sound");
            this.snapped = true;
            yield return Wait(1);
            this._trail = this.Projectile.AddTrail(SubtractorBeam._RedTrailPrefab).gameObject;
            this._trail.SetGlowiness(100f);
            continue;
          }
          newVelocity += gravity * BraveTime.DeltaTime;
          this.ChangeDirection(new Direction(newVelocity.ToAngle(), DirectionType.Absolute));
          this.ChangeSpeed(new Speed(newVelocity.magnitude, SpeedType.Absolute));
          yield return Wait(1);
        }
        Vanish();
        yield break;
      }
    }

    private const int   BATCHES     = 10;
    private const int   BATCHSIZE   = 3;
    private const int   COUNT       = BATCHES * BATCHSIZE;
    private const float SPREAD      = 9f;
    private const float GRAVITY     = 200f;
    private const float BASESPEED   = 70f;
    private const float STRIKESPEED = 100f;
    private const int   SHOTDELAY   = 3;

    private PathRect bounds;

    protected override List<FluidBulletInfo> BuildChain()
    {
      this.bounds = new PathRect(base.roomSlamBounds);

      return
         Run(SprayBullets(new Vector2(0.5f, 1f)))
        .And(SprayBullets(new Vector2(-0.5f, 1f)))
        .Finish();
    }

    private IEnumerator SprayBullets(Vector2 startDir)
    {
      Vector2 target = this.bounds.At(0.5f, 0.2f); // near bottom of room

      Vector2 gravity = new Vector2(0, -GRAVITY);
      Speed s = new Speed(BASESPEED, SpeedType.Absolute);
      Offset o = Offset.OverridePosition(theBoss.CenterPosition);
      theBoss.gameObject.Play("armistice_laugh_2");
      int b = 0;
      for(int i = 0; i < COUNT; ++i)
      {
        Vector2 bulletvel = (BASESPEED * startDir.normalized).Rotate(UnityEngine.Random.Range(-SPREAD,SPREAD));
        Direction d = new Direction(bulletvel.ToAngle().Clamp180(),DirectionType.Absolute);
        this.Fire(o, d, s, new CrossBullet(bulletvel, gravity, target + Lazy.RandomVector(2f * UnityEngine.Random.value), base.roomFullBounds));
        if ((++b) % BATCHSIZE == 0)
          yield return Wait(SHOTDELAY);
      }
    }
  }

  private class ClocksTickingScript : ArmisticeBulletScript
  {
    private const float _LIFETIME = 6f;
    private const float _GAP = 1.4f;

    private float _time = 0f;

    internal class ClocksTickingBullet : SecretBullet
    {
      private const float _REST_TIME = 0.35f;
      internal const float _SPAWN_RADIUS = 32f;

      private ClocksTickingScript _parent = null;
      private Vector2 _spawnCenter        = default;
      private float _spawnTime            = 0f;
      private float _spawnRadius          = 0f;
      private float _restRadius           = 0f;
      private float _spawnAngle           = 0f;
      private float _rps                  = 0f;
      private bool  _flood                = false;

      public ClocksTickingBullet(ClocksTickingScript parent, Vector2 spawnCenter, float spawnTime, float spawnRadius, float restRadius,
        float spawnAngle, float rps, bool flood) : base(fancySpawn: true)
      {
        this._parent      = parent;
        this._spawnCenter = spawnCenter;
        this._spawnTime   = spawnTime;
        this._spawnRadius = spawnRadius;
        this._restRadius  = restRadius;
        this._spawnAngle  = spawnAngle;
        this._rps         = rps;
        this._flood       = flood;

        base.ManualControl = true;
        base.TimeScale = -1f; // tick every frame
        // base.EndOnBlank = true;
      }

      public override void Initialize()
      {
        base.Initialize();
        SpeculativeRigidbody srb = this.Projectile.specRigidbody;
        srb.CollideWithTileMap = false;
        if (this._flood)
          srb.OnPreRigidbodyCollision += this.OnPreRigidbodyCollision;
      }

      private void OnPreRigidbodyCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
      {
          if (!otherRigidbody || !otherRigidbody.gameObject.GetComponent<PlayerController>())
            PhysicsEngine.SkipCollision = true;
      }

      public override IEnumerator Top()
      {
        float topTime = BraveTime.ScaledTimeSinceStartup;
        while (true)
        {
            float now = BraveTime.ScaledTimeSinceStartup;
            float topElapsed = now - topTime;
            float elapsed = now - this._spawnTime;
            if (elapsed > _LIFETIME)
              break;
            float radius = this._restRadius;
            if (topElapsed < _REST_TIME)
              radius += (1f - Ease.OutCubic(topElapsed / _REST_TIME)) * (this._spawnRadius - this._restRadius);
            // if (!this._flood)
            {
              float rot = 360f * elapsed * this._rps;
              base.Position = this._spawnCenter + (this._spawnAngle + rot).ToVector(radius);
            }
            yield return Wait(1);
        }
        float vanishTime = BraveTime.ScaledTimeSinceStartup + 0.5f * UnityEngine.Random.value;
        while (BraveTime.ScaledTimeSinceStartup < vanishTime)
          yield return Wait(1);
        Vanish(suppressInAirEffects: true);
        yield break;
      }
    }

    protected override List<FluidBulletInfo> BuildChain()
    {
      base.BulletBank.aiActor.gameObject.Play("armistice_clocks_ticking_sound");
      // base.EndOnBlank = true;
      return
         Run(Attack(radius:  8f, rps: -0.300f, offAngle: 110f, playSounds: true))
        .And(Attack(radius: 11f, rps: -0.625f, offAngle:  90f, playSounds: false))
        .And(Attack(radius: 14f, rps: -0.850f, offAngle:  70f, playSounds: false))
        .And( Flood(radius: 14f, rps:  0.150f, offAngle: -90f, spawnRps: 0.75f))
        .Finish();
    }

    private IEnumerator Flood(float radius, float rps, float offAngle, float spawnRps)
    {
      const float MAX_RADIUS = 26f; // 28 is definitely enough
      const int BANDS = 40;
      const int BANDSIZE = 360 / BANDS;

      int iStart = Mathf.FloorToInt(radius / _GAP);
      int iEnd = Mathf.FloorToInt(MAX_RADIUS / _GAP);

      float start = BraveTime.ScaledTimeSinceStartup;
      Vector2 center = this.roomFullBounds.center;
      float baseAngle = ((center - GameManager.Instance.BestActivePlayer.CenterPosition).ToAngle() + offAngle).Clamp360();
      int curBand = 0;
      while (curBand < BANDS)
      {
        this._time = BraveTime.ScaledTimeSinceStartup;
        float trueRot = 360f * (this._time - start) * rps;
        int band = Mathf.CeilToInt(BANDS * (this._time - start) * spawnRps);
        if (curBand >= band)
        {
          yield return Wait(1);
          continue;
        }
        ++curBand;
        float quantRot = curBand * BANDSIZE;
        for (int i = iStart; i <= iEnd; ++i)
        {
          float trueRadius = i * _GAP;
          Vector2 pos = center + (baseAngle + quantRot + trueRot).ToVector(ClocksTickingBullet._SPAWN_RADIUS); //NOTE: deliberately need to offset by rot twice
          this.Fire(Offset.OverridePosition(pos), new Direction(0f, DirectionType.Absolute), new Speed(0f),
            new ClocksTickingBullet(
              parent      : this,
              spawnCenter : center,
              spawnTime   : start,
              spawnRadius : ClocksTickingBullet._SPAWN_RADIUS,
              restRadius  : trueRadius,
              spawnAngle  : baseAngle + quantRot,
              rps         : rps,
              flood       : true
              ));
        }
      }
      yield break;
    }

    private IEnumerator Attack(float radius, float rps, float offAngle, bool playSounds)
    {
      const float SOUND_RATE = 60f / 180f; // 180BPM song

      float start = BraveTime.ScaledTimeSinceStartup;
      int count = Mathf.FloorToInt(radius / _GAP);
      Vector2 center = this.roomFullBounds.center;
      float baseAngle = ((center - GameManager.Instance.BestActivePlayer.CenterPosition).ToAngle() + offAngle).Clamp360();
      for (int i = 1; i <= count; ++i)
      {
        this._time = BraveTime.ScaledTimeSinceStartup;
        float trueRadius = _GAP * i;
        float rot = 360f * (this._time - start) * rps;
        this.Fire(Offset.OverridePosition(center + (baseAngle + rot).ToVector(ClocksTickingBullet._SPAWN_RADIUS)), new Direction(0f, DirectionType.Absolute), new Speed(0f),
          new ClocksTickingBullet(
            parent      : this,
            spawnCenter : center,
            spawnTime   : start,
            spawnRadius : ClocksTickingBullet._SPAWN_RADIUS,
            restRadius  : trueRadius,
            spawnAngle  : baseAngle,
            rps         : rps,
            flood       : false
            ));
        yield return Wait(3);
      }
      float endTime = start + _LIFETIME;
      float nextSound = this._time;
      int c = 0;
      while (this._time < endTime)
      {
        yield return Wait(1);
        this._time = BraveTime.ScaledTimeSinceStartup;
        if (playSounds && this._time >= nextSound)
        {
          nextSound += SOUND_RATE;
          base.BulletBank.aiActor.gameObject.Play($"ticking_clock_{(c % 8) + 1}");
          ++c;
        }
      }
      yield break;
    }
  }

  private class WalledInScript : ArmisticeBulletScript
  {
    private const int _STREAMS = 50;
    private const int _WALLSIZE = 25;
    private const int _ITERS   = (_STREAMS / 2);
    private const float _FRAMES = 180;
    private const float _MINSPEED = 50f;
    private const float _MAXSPEED = 100f;
    private const float _DLTSPEED = _MAXSPEED - _MINSPEED;

    private PathLine _bounds;

    protected override List<FluidBulletInfo> BuildChain()
    {
      this._bounds = new PathRect(base.roomFullBounds.Inset(2f, 1f)).Top();
      FluidBulletInfo fbi = Run(Attack(0, speed: _MINSPEED));
      for (int i = 1; i <= _ITERS; ++i)
      {
        float ease = Ease.OutQuad((float)i / _STREAMS);
        fbi = fbi.And(Attack(i, speed: _MINSPEED + _DLTSPEED * ease), withDelay: (int)(ease * _FRAMES));
      }
      for (int i = _ITERS - 1; i >= 1; --i)
      {
        float ease = Ease.OutQuad((float)(_STREAMS - i) / _STREAMS);
        fbi = fbi.And(Attack(i, speed: _MINSPEED + _DLTSPEED * ease), withDelay: (int)(ease * _FRAMES));
      }
      return fbi.Finish();
    }

    private IEnumerator Attack(int end, float speed)
    {
      base.BulletBank.aiActor.gameObject.Play("gradius_blaster_sound");
      Direction dir = new Direction(270f);
      Speed sspeed = new Speed(speed);
      int start = Mathf.Max(end - _WALLSIZE, 0);
      float now = BraveTime.ScaledTimeSinceStartup;
      Color c = Color.HSVToRGB(now - (int)now, 1f, 1f)/*.WithAlpha(1.0f)*/;
      for (int k = start; k <= end; ++k)
      {
        float off = (float)k / _STREAMS;
        this.Fire(Offset.OverridePosition(this._bounds.At(off)), dir, sspeed,
          new SecretBullet(tint: c, emission: 100f, emitColorPower: 0.5f));
        this.Fire(Offset.OverridePosition(this._bounds.At(1f - off)), dir, sspeed,
          new SecretBullet(tint: c, emission: 100f, emitColorPower: 0.5f));
      }
      yield break;
    }

  }

  private class BoneTunnelScript : ArmisticeBulletScript
  {
    private const float _YOFFSET = 10f;
    private const float _CONVERGE_TIME = 0.3f;
    private const float SPEED = 58f;

    internal class BoneTunnelBullet : SecretBullet
    {
      private float _targetY;
      private float _startY;

      public BoneTunnelBullet(float targetY) : base()
      {
        this._targetY = targetY;
      }

      public override void Initialize()
      {
        base.Initialize();
        base.TimeScale = -1f;
        this._startY = base.Position.y;
        base.Projectile.specRigidbody.CollideWithTileMap = false;
      }

      public override IEnumerator Top()
      {
        float lifeTime = 0f;
        float yOffset = this._startY - this._targetY;
        while (lifeTime < _CONVERGE_TIME)
        {
          lifeTime += BraveTime.DeltaTime;
          float t = Mathf.Clamp01(lifeTime / _CONVERGE_TIME);
          base.Position = base.Position.WithY(this._startY - Ease.OutCubic(t) * yOffset);
          yield return Wait(1);
        }
        base.Position = base.Position.WithY(this._targetY);
        base.Projectile.specRigidbody.CollideWithTileMap = true;
        yield break;
      }

    }

    protected override List<FluidBulletInfo> BuildChain()
    {
      return Run(Attack()).Finish();
    }

    private IEnumerator Attack()
    {
      const int RATE          = 5; // frames between bullets
      const int WAVES         = 60; // base waves the attack lasts
      const int WAVE_VARIANCE = 10; // max additional waves the attack lasts
      const int GAP           = 5; // size of the path we have to navigate
      const int ROWS          = 50; // number of rows of bullets
      const int NOGAP         = 3; // number of columns at the end with no gap

      //TODO: sound / visual cue
      yield return Wait(15); // wait for a quarter of a second

      PathRect roomRect = new PathRect(this.roomFullBounds.Inset(0f, 0.25f));
      Speed sspeed = new Speed(SPEED);

      Vector2 ppos = GameManager.Instance.BestActivePlayer.CenterPosition;
      // bool leftward = (ppos.x < this.roomFullBounds.center.x);
      bool leftward = (base.BulletBank.aiActor.Position.x > this.roomFullBounds.center.x);
      Direction dir = new Direction(leftward ? 180f : 0f);
      PathLine wall = leftward ? roomRect.Right() : roomRect.Left();
      float wallOff = leftward ? -2.5f : 2.5f;

      int gapPos = Mathf.RoundToInt(ROWS * ((ppos.y - wall.start.y) / (wall.end.y - wall.start.y)));

      int numWaves = WAVES + UnityEngine.Random.Range(0, WAVE_VARIANCE);
      for (int i = 0; i < numWaves; ++i)
      {
        bool hasGap = i < (numWaves - NOGAP);
        float amplitude = Mathf.Clamp01(0.1f * (numWaves - i)); // the last few waves close in to prevent cheesing by standing near the edge
        for (int j = 0; j <= ROWS; ++j)
        {
          if (hasGap && Mathf.Abs(j - gapPos) < GAP)
            continue;
          float off = (float)j / ROWS;
          Vector2 bpos = wall.At(off) + new Vector2(wallOff * UnityEngine.Random.value, 0f);
          float offset = amplitude * ((j > gapPos) ? _YOFFSET : -_YOFFSET);
          this.Fire(Offset.OverridePosition(bpos + new Vector2(0f, offset)), dir, sspeed, new BoneTunnelBullet(targetY: bpos.y));
        }
        base.BulletBank.aiActor.gameObject.Play($"armistice_bullet_storm_sound_{1 + (i % 4)}");
        if (gapPos <= GAP)
          ++gapPos;
        else if (gapPos >= ROWS - GAP)
          --gapPos;
        else
          gapPos += (Lazy.CoinFlip() ? 1 : -1);
        yield return Wait(RATE);
      }
    }

  }

  private class DanceMonkeyScript : ArmisticeBulletScript
  {

    internal class DanceMonkeyBullet : SecretBullet
    {
      private const float LERP_FACTOR = 10f;

      private float _startAngle;
      private float _radius;

      private Vector2 _center;
      private float _holdRadius;
      private int _holdFrames;
      private float _rps;
      private float _spawnTime;

      public DanceMonkeyBullet(Vector2 center, float holdRadius, int holdFrames, float rps, float spawnTime) : base()
      {
        this._center     = center;
        this._holdRadius = holdRadius;
        this._holdFrames   = holdFrames;
        this._rps        = rps;
        this._spawnTime  = spawnTime;

        base.ManualControl = true;
      }

      public override void Initialize()
      {
        base.Initialize();
      }

      public override IEnumerator Top()
      {
        Vector2 startPos = base.Position;
        Vector2 startVec = (base.Position - this._center);
        this._startAngle = startVec.ToAngle();
        this._radius = startVec.magnitude;

        this.Projectile.specRigidbody.CollideWithTileMap = false;

        for (int i = 0; i < this._holdFrames; ++i)
        {
          float now = BraveTime.ScaledTimeSinceStartup;
          float lifetime = now - this._spawnTime;
          float angle = (this._startAngle + lifetime * 360f * this._rps);
          if (Mathf.Abs(this._radius - this._holdRadius) > 0.0125f)
            this._radius = Lazy.SmoothestLerp(this._radius, this._holdRadius, LERP_FACTOR);
          else
            this._radius = this._holdRadius;
          base.Position = this._center + angle.ToVector(this._radius);
          yield return Wait(1);
        }

        base.ManualControl = false;
        this.Projectile.specRigidbody.CollideWithTileMap = true;
        this.ChangeDirection(new Direction((this._center - base.Position).ToAngle().AddRandomSpread(12f)));
        this.ChangeSpeed(new Speed(100f));

        yield return Wait(1);
        this._trail = this.Projectile.AddTrail(SubtractorBeam._RedTrailPrefab).gameObject;
        this._trail.SetGlowiness(100f);

        yield break;
      }

    }

    protected override List<FluidBulletInfo> BuildChain()
    {
      return Run(Attack()).Finish();
    }

    private IEnumerator Attack()
    {
      const int CIRCLESIZE    = 17;
      const float GAP         = 360f / CIRCLESIZE;
      const int JUMPS         = 5;
      const int JUMPFRAMES    = 60;
      const float STARTRADIUS = 10f;
      const float HOLDRADIUS  = 3f;
      const int HOLDFRAMES    = 40;
      const float RPS         = 3.0f;

      PlayerController pc = GameManager.Instance.BestActivePlayer;

      for (int i = 0; (i < JUMPS) && pc; ++i)
      {
        Vector2 ppos = pc.CenterPosition;
        float now    = BraveTime.ScaledTimeSinceStartup;
        float off    = 360f * UnityEngine.Random.value;

        for (int j = 0; j < CIRCLESIZE; ++j)
          this.Fire(Offset.OverridePosition(ppos + (off + (GAP * j)).ToVector(STARTRADIUS)),
            new DanceMonkeyBullet(ppos, HOLDRADIUS, HOLDFRAMES, RPS, now));

        yield return Wait(JUMPFRAMES);
      }

      yield break;
    }

  }

  private class PendulumScript : ArmisticeBulletScript
  {

    internal class PendulumBullet : SecretBullet
    {

      private PendulumScript _parent;
      private Vector2 _relPos;

      public PendulumBullet(PendulumScript parent, Vector2 relPos) : base()
      {
        this._parent = parent;
        this._relPos = relPos;

        base.ManualControl = true;
      }

      public override void Initialize()
      {
        base.Initialize();
        SpeculativeRigidbody srb = this.Projectile.specRigidbody;
        srb.CollideWithTileMap = false;
      }

      public override IEnumerator Top()
      {
        while (this._parent != null && this._parent._active)
        {
          base.Position = this._parent._ballCenter + this._relPos;
          yield return Wait(1);
        }
        Vanish();
        yield break;
      }

    }

    private Vector2 _ballCenter;
    private bool _active;

    protected override List<FluidBulletInfo> BuildChain()
    {
      return Run(Attack()).Finish();
    }

    private IEnumerator Attack()
    {
      const float BALL_RADIUS    = 2f;
      const int BALL_DENSITY     = 5;
      const int RING_SIZE        = 5;
      const int RING_GROWTH      = 4;
      const int NUM_SWINGS       = 11;
      const int SWING_FRAMES_MAX = 60;
      const int SWING_FRAMES_MIN = 30;
      const int SWING_FRAMES_DLT = (SWING_FRAMES_MAX - SWING_FRAMES_MIN) / (NUM_SWINGS - 1);
      const int HOLD_FRAMES      = 5;
      const float SWING_DEPTH    = -10f;
      const int CRASH_SPEED_MIN  = 5;
      const int CRASH_SPEED_MAX  = 25;
      const int CRASH_SPEED_DLT  = (CRASH_SPEED_MAX - CRASH_SPEED_MIN) / (NUM_SWINGS - 1);
      const int CRASH_SIZE       = 20;
      const float CRASH_GAP      = 180f / CRASH_SIZE;

      PlayerController pc = GameManager.Instance.BestActivePlayer;
      Vector2 roomCenter = this.roomFullBounds.center;
      bool backswing = pc.CenterPosition.x < roomCenter.x;
      bool onTop = pc.CenterPosition.y < roomCenter.y;

      PathRect roomRect = new PathRect(this.roomFullBounds.Inset(BALL_RADIUS, 0f));
      Vector2 left  = roomRect.At(0f, 0.5f);
      Vector2 right = roomRect.At(1f, 0.5f);
      float ybase = right.y;
      float width = right.x - left.x;

      this._ballCenter = backswing ? right : left;

      this._active = true;
      for (int d = 0; d < BALL_DENSITY; ++d)
      {
        float radius = (float)(d + 1) * (BALL_RADIUS / BALL_DENSITY);
        int ringSize = RING_SIZE + d * RING_GROWTH;
        for (int n = 0; n < ringSize; ++n)
        {
          float angle = (360f / ringSize) * n;
          Vector2 relPos = angle.ToVector(radius);
          PendulumBullet b = new PendulumBullet(this, relPos);
          this.Fire(Offset.OverridePosition(this._ballCenter + relPos), b);
        }
      }

      for (int i = 0; i < NUM_SWINGS; ++i)
      {
        Speed crashSpeed = new Speed(CRASH_SPEED_MIN + CRASH_SPEED_DLT * i);
        int swingFrames = SWING_FRAMES_MAX - SWING_FRAMES_DLT * i;

        for (int j = 0; j <= swingFrames; ++j)
        {
          float swingPos = Ease.InCubic((float)j / swingFrames);
          float xoff = Mathf.Sin(0.5f * Mathf.PI * swingPos);
          float yoff = (onTop ? -1f : 1f) *  SWING_DEPTH * Mathf.Sin(Mathf.PI * xoff);
          if (backswing)
            this._ballCenter = new Vector2(right.x - xoff * width, ybase + yoff);
          else
            this._ballCenter = new Vector2(left.x + xoff * width, ybase + yoff);
          yield return Wait(1);
        }

        this._ballCenter = backswing ? left : right;
        float baseAngle = (backswing ? 270f : 90f) + (0.5f * CRASH_GAP * UnityEngine.Random.value);
        for (int k = 0; k < CRASH_SIZE; ++k)
        {
          float angle = baseAngle + k * CRASH_GAP;
          this.Fire(Offset.OverridePosition(this._ballCenter + angle.ToVector(BALL_RADIUS)),
            new Direction(angle), crashSpeed, new SecretBullet());
        }

        backswing = !backswing;
        yield return Wait(HOLD_FRAMES);
      }

      this._active = false;

      yield break;
    }

  }

  private class BoxTrotScript : ArmisticeBulletScript
  {

    internal class BoxTrotTurret : SecretBullet
    {
      private const float _BEAM_TIME = 0.5f; // time it takes beam to fully grow

      private PathRect _path;
      private float _offset;
      private float _top;
      private float _bottom;
      private GameObject _sightline = null;
      private Transform _transform;
      private float _lastAngle = 361f;

      public BoxTrotTurret(PathRect path, float offset, float top, float bottom) : base(baseProj: "turret"/*, emission: 0f*/)
      {
        this._path = path;
        this._offset = offset;
        this._top = top;
        this._bottom = bottom;
      }

      public override void Initialize()
      {
        base.Initialize();
        tk2dBaseSprite sprite = base.Projectile.sprite;
        sprite.usesOverrideMaterial = true;
        sprite.renderer.material.shader = ShaderCache.Acquire("Brave/PlayerShader");

        // base.Projectile.specRigidbody.DebugColliders();
        base.Projectile.specRigidbody.OnPreTileCollision += this.OnPreTileCollision;
      }

      private void OnPreTileCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, PhysicsEngine.Tile tile, PixelCollider tilePixelCollider)
      {
        myRigidbody.CollideWithTileMap = false;
        PhysicsEngine.SkipCollision = true;
        if (!base.ManualControl)
          Deploy();
      }

      protected override void OnBaseDestruction(Projectile projectile)
      {
        base.OnBaseDestruction(projectile);
        if (projectile.specRigidbody)
          projectile.specRigidbody.OnPreTileCollision -= this.OnPreTileCollision;
        if (this._sightline)
        {
          UnityEngine.Object.Destroy(this._sightline);
          this._sightline = null;
        }
      }

      private void ManualUpdate()
      {
        if (!this._path.rect.Contains(base.Position))
          Deploy();
      }

      private void Deploy()
      {
        this._offset = this._path.InverseAt(base.Position);
        base.Position = this._path.At(this._offset);
        base.TimeScale = -1f; // update every tick
        base.ManualControl = true; // we now control it
        base.Projectile.gameObject.Play("armistice_laser_deploy_sound");
      }

      private void DrawBeamToWall(float startTime, Vector2 center)
      {
        const float SIGHT_WIDTH = 2.0f;

        float beamLength = Ease.OutCubic(Mathf.Clamp01((BraveTime.ScaledTimeSinceStartup - startTime) / _BEAM_TIME));
        float x = base.Position.x;
        float y = base.Position.y;
        bool horizontal = y < this._bottom && y > this._top;
        float angle;
        if (horizontal)
          angle = x < center.x ? 0f : 180f;
        else
          angle = y < center.y ? 90f : 270f;

        if (this._lastAngle > 360f)
          this._lastAngle = angle;

        float angleDelta = this._lastAngle.RelAngleTo(angle);
        this._lastAngle = (this._lastAngle + Lazy.SmoothestLerp(0, angleDelta, 10f)) % 360f;

        if (!this._sightline)
        {
          this._sightline = VFX.CreateLaserSight(this._transform.position, 1f, SIGHT_WIDTH, this._lastAngle, colour: Color.red, power: 50f);
          this._sightline.transform.parent = this._transform;
        }

        int rayMask = CollisionMask.LayerToMask(CollisionLayer.HighObstacle);
        RaycastResult result;
        if (!PhysicsEngine.Instance.Raycast(base.Projectile.SafeCenter, this._lastAngle.ToVector(), 999f, out result, true, false, rayMask, null, false))
        {
            RaycastResult.Pool.Free(ref result);
            return;
        }
        float length = beamLength * C.PIXELS_PER_TILE * result.Distance;
        Vector2 target = result.Contact;
        RaycastResult.Pool.Free(ref result);

        tk2dTiledSprite sprite = this._sightline.GetComponent<tk2dTiledSprite>();
        sprite.renderer.enabled = true;
        sprite.dimensions = new Vector2(length, SIGHT_WIDTH);
        this._sightline.transform.rotation = this._lastAngle.EulerZ();
        this._sightline.transform.localPosition = Vector2.zero;
        sprite.ForceRotationRebuild();
        sprite.UpdateZDepth();
      }

      public override IEnumerator Top()
      {
        const int SPEED = 50;
        const float COOLDOWN = 0.5f;
        const float RPS = 0.2f;

        while (!base.ManualControl) // can't do anything until colliding with the wall
        {
          ManualUpdate();
          yield return Wait(1);
        }

        float now = BraveTime.ScaledTimeSinceStartup;
        float nextShootTime = now + _BEAM_TIME;
        float startTime = now;
        Vector2 lastPos = base.Position;
        Vector2 center = this._path.At(0.5f, 0.5f);
        bool?[] oldSideH = [null, null];
        bool?[] oldSideV = [null, null];

        this._transform = base.Projectile.gameObject.transform;

        while (true)
        {
          yield return Wait(1);

          now = BraveTime.ScaledTimeSinceStartup;
          lastPos = base.Position;
          float t = RPS * (now - startTime);
          base.Position = this._path.At((this._offset + t) % 1f);
          // base.Projectile.specRigidbody.DrawDebugHitbox();
          DrawBeamToWall(startTime, center);
          base.Projectile.SetRotation(this._lastAngle);
          if (nextShootTime > now)
            continue;

          float y = base.Position.y;
          bool horizontal = y < this._bottom && y > this._top;
          foreach (PlayerController player in GameManager.Instance.AllPlayers)
          {
            int idx = player.PlayerIDX;
            if (idx < 0 || idx > 1)
              continue;

            bool changedSides = false;
            Vector2 ppos = player.CenterPosition;
            if (horizontal)
            {
              bool curSide = ppos.y < base.Position.y;
              changedSides = (oldSideH[idx] ?? curSide) != curSide;
              oldSideH[idx] = curSide;
            }
            else
            {
              bool curSide = ppos.x < base.Position.x;
              changedSides = (oldSideV[idx] ?? curSide) != curSide;
              oldSideV[idx] = curSide;
            }

            if (!changedSides)
              continue;

            float angle;
            if (horizontal)
              angle = base.Position.x < center.x ? 0f : 180f;
            else
              angle = base.Position.y < center.y ? 90f : 270f;

            base.Fire(Offset.OverridePosition(base.Position), new Direction(angle), new Speed(SPEED), new BoxTrotBullet());
            CwaffVFX.Spawn(prefab: _MuzzleVFXTurret, position: base.Position, rotation: angle.EulerZ(),
              emissivePower: 0.5f, emissiveColor: Color.white, startScale: 0.5f, anchorTransform: base.Projectile.transform);
            nextShootTime = now + COOLDOWN;
          }
        }
      }

    }

    internal class BoxTrotBullet : SecretBullet
    {
      public override void Initialize()
      {
        base.Initialize();
        base.Projectile.gameObject.Play("armistice_turret_fire_sound");
        this._trail = base.Projectile.AddTrail(SubtractorBeam._RedTrailPrefab).gameObject;
        this._trail.SetGlowiness(100f);
      }

      // public override IEnumerator Top()
      // {
      //   yield break;
      // }

    }

    protected override List<FluidBulletInfo> BuildChain()
    {
      base.TimeScale = -1f;
      return Run(Attack()).Finish();
    }

    private IEnumerator Attack()
    {
      const int TURRETS              = 3;
      const float LENGTH             = 6f;
      const float EPSILON            = 0.1f;
      const float TIME_BETWEEN_SHOTS = 0.5f;

      AIActor boss               = base.BulletBank.aiActor;
      Rect bounds                = this.roomFullBounds.Inset(0.5f, 1.25f, 2f, 1f);
      float top                  = bounds.yMin + EPSILON;
      float bottom               = bounds.yMax - EPSILON;
      PathRect path              = new PathRect(bounds);
      List<SecretBullet> bullets = new();

      for (int i = 0; i < TURRETS; ++i)
      {
        bool facingLeft = boss.gameObject.transform.position.x > bounds.center.x;
        boss.sprite.FlipX = facingLeft;
        Vector2 firePos = boss.gameObject.transform.position + GunBarrelOffset(facingLeft: facingLeft);

        //TODO: pick a different sound and muzzle later maybe
        BoxTrotTurret bullet = new BoxTrotTurret(path, 0f, top, bottom);
        bullets.Add(bullet);
        float shootDir = (facingLeft ? 180f : 0f).AddRandomSpread(15f);
        this.Fire(Offset.OverridePosition(firePos), new Direction(shootDir), new Speed(50f), bullet);
        boss.gameObject.Play("armistice_launch_turret_sound");
        CwaffVFX.Spawn(prefab: _MuzzleVFXBullet, position: firePos, rotation: shootDir.EulerZ(),
          emissivePower: 10f, emissiveColor: ExtendedColours.vibrantOrange, emitColorPower: 8f);

        boss.aiAnimator.PlayUntilFinished("attack_snipe");
        while (boss.spriteAnimator.IsPlaying("attack_snipe"))
          yield return Wait(1);
        boss.aiAnimator.PlayUntilCancelled("idle");
        for (float elapsed = 0f; elapsed < TIME_BETWEEN_SHOTS; elapsed += BraveTime.DeltaTime)
          yield return Wait(1);
        if (i < TURRETS - 1)
        {
          boss.aiAnimator.PlayUntilCancelled("ready");
          while (boss.spriteAnimator.IsPlaying("ready"))
            yield return Wait(1);
        }
      }

      float endTime = BraveTime.ScaledTimeSinceStartup + LENGTH;
      while (BraveTime.ScaledTimeSinceStartup < endTime)
        yield return Wait(1);

      for (int n = 0; n < TURRETS; ++n)
        if (bullets[n].Projectile)
          bullets[n].Vanish();

      yield break;
    }

  }

  private class LaserBarrageScript : ArmisticeBulletScript
  {
    protected override List<FluidBulletInfo> BuildChain() => Run(Attack()).Finish();

    private IEnumerator Attack()
    {
      const int RAMP_ITERS   = 24;
      const int EXTRA_ITERS  = 10;
      const int TOTAL_ITERS  = RAMP_ITERS + EXTRA_ITERS;
      const float MIN_SPEED  = 10;
      const float MAX_SPEED  = 25;
      const float MAX_SPREAD = 70;
      const int BARRAGE_SIZE = 23;
      const int LASERSPEED   = 50;
      const float DELAY_MAX  = 1.0f;
      const float DELAY_MIN  = 0.15f;

      PlayerController pc = GameManager.Instance.GetRandomActivePlayer();
      AIActor actor = base.BulletBank.aiActor;
      Transform t = actor.gameObject.transform;
      bool facingLeft = t.position.x > this.roomFullBounds.center.x;
      Vector2 shootPoint = t.position + GunBarrelOffset(facingLeft);
      Offset shootOff = Offset.OverridePosition(shootPoint);
      float shootAngle = facingLeft ? 180f : 0f;

      float nextShot = 0f;
      float now = BraveTime.ScaledTimeSinceStartup;
      for (int i = 0; i < TOTAL_ITERS; ++i)
      {
        float progress = Mathf.Min((float)i / RAMP_ITERS, 1f);
        float delay = 0.5f * Mathf.Lerp(DELAY_MAX, DELAY_MIN, progress);

        if (i % 2 == 0)
        {
          Speed curSpeed = new Speed(Mathf.Lerp(MIN_SPEED, MAX_SPEED, progress));
          for (int n = 0; n < BARRAGE_SIZE; ++n)
            base.Fire(shootOff, new Direction(shootAngle.AddRandomSpread(MAX_SPREAD)), curSpeed, new SecretBullet());
          actor.gameObject.PlayUnique("armistice_gun_sound");
          CwaffVFX.Spawn(prefab: _MuzzleVFXBullet, position: shootPoint, rotation: shootAngle.EulerZ(), emissivePower: 10f,
            emissiveColor: ExtendedColours.vibrantOrange, emitColorPower: 8f);
        }
        else
        {
          float launchAngle = (pc.CenterPosition - shootPoint).ToAngle();
          SecretBullet laser = new SecretBullet(tint: Color.cyan);
          base.Fire(shootOff, new Direction(launchAngle), new Speed(LASERSPEED), laser);
          laser.AddTrail(_LaserTrailPrefab, glow: 100f);
          actor.gameObject.PlayUnique("armistice_electro_sound");
          CwaffVFX.Spawn(prefab: _MuzzleVFXElectro, position: shootPoint, rotation: launchAngle.EulerZ(), emissivePower: 10f,
            emissiveColor: ExtendedColours.vibrantOrange, emitColorPower: 8f);
        }

        nextShot = now + delay;
        while (now < nextShot)
        {
          yield return Wait(1);
          now = BraveTime.ScaledTimeSinceStartup;
        }
      }

      yield break;
    }

  }

  private class MeteorShowerScript : ArmisticeBulletScript
  {

    internal class MeteorShowerBullet : SecretBullet
    {

      private static ExplosionData _Explosion = null;

      private Rect _bounds;
      private bool _exploded;

      public MeteorShowerBullet(Rect bounds) : base(baseProj: "warhead")
      {
        this._bounds = bounds;
        this._exploded = false;
      }

      public override void Initialize()
      {
        base.Initialize();
        SpeculativeRigidbody body = base.Projectile.specRigidbody;
        body.CollideWithOthers = false;
        body.CollideWithTileMap = false;
        base.AddTrail(_WarheadTrailPrefab, glow: 1f);

        tk2dBaseSprite sprite = base.Projectile.sprite;
        sprite.usesOverrideMaterial = true;
        sprite.renderer.material.shader = ShaderCache.Acquire("Brave/PlayerShader");
      }

      protected override void OnBaseDestruction(Projectile projectile)
      {
        base.OnBaseDestruction(projectile);
        if (!this._exploded)
          return;

        if (_Explosion == null)
        {
          _Explosion = Explosions.ExplosiveRounds.With(damage: 0f, force: 100f, debrisForce: 10f, radius: 1.5f,
            preventPlayerForce: false, shake: false);
          _Explosion.doDamage = true;
          _Explosion.damageToPlayer = 0.5f;
          _Explosion.effect = null; // we handle the vfx ourselves
        }
        Exploder.Explode(position: projectile.SafeCenter, data: _Explosion, sourceNormal: Vector2.zero, ignoreQueues: true);
        projectile.gameObject.Play("armistice_warhead_explode_sound"); //TODO: find better sound
        CwaffVFX.Spawn(_ExplosionVFX, position: projectile.SafeCenter);
        CwaffVFX.SpawnBurst(prefab: _SmokeVFX, numToSpawn: 20, basePosition: projectile.SafeCenter, positionVariance: 2f,
          velocityVariance: 4f, rotType: CwaffVFX.Rot.Random, lifetime: 0.5f, fadeOutTime: 0.5f);
      }

      public override IEnumerator Top()
      {
        const float HEIGHT = 100f;
        const float TIME = 0.75f;
        const float ROTSPEED = 1080f;
        const float WAIT = 0.5f;
        const float VARIANCE = 0.5f;

        CameraController cam = GameManager.Instance.MainCameraController;
        while (cam.PointIsVisible(base.Position))
          yield return Wait(1);

        float wait = WAIT + UnityEngine.Random.value * VARIANCE;
        for (float elapsed = 0f; elapsed < wait; elapsed += BraveTime.DeltaTime)
            yield return Wait(1);

        PlayerController pc = GameManager.Instance.BestActivePlayer;
        Vector2 target = pc ? pc.CenterPosition : new PathRect(this._bounds.Inset(3f)).At(UnityEngine.Random.value, UnityEngine.Random.value);
        if (pc) // Easeing biases the position to something closer to the player
        {
          target += TIME * (Ease.InCubic(UnityEngine.Random.value) * pc.Velocity) + Lazy.RandomVector(2f * Ease.InCubic(UnityEngine.Random.value));
          if (!this._bounds.Contains(target))
            target = BraveMathCollege.ClosestPointOnRectangle(target, this._bounds.min, this._bounds.size);
        }
        base.TimeScale = -1f; // update every frame
        base.ManualControl = true;
        base.Projectile.sprite.transform.rotation = 270f.EulerZ();

        GameObject sigil = SpawnManager.SpawnVFX(VFX.MasterySigil, target, (ROTSPEED * BraveTime.ScaledTimeSinceStartup).EulerZ(), ignoresPools: true);
        sigil.ExpireIn(TIME); // fallback in case the script gets interrupted
        base.Projectile.gameObject.Play("armistice_warhead_fall_sound");

        for (float elapsed = 0f; elapsed < TIME; elapsed += BraveTime.DeltaTime)
        {
            float percentLeft = 1f - elapsed / TIME;
            base.Position = target + new Vector2(0f, percentLeft * HEIGHT);
            sigil.transform.rotation = (ROTSPEED * BraveTime.ScaledTimeSinceStartup).EulerZ();
            sigil.transform.localScale = percentLeft * Vector3.one;
            yield return Wait(1);
        }

        if (sigil)
          UnityEngine.Object.Destroy(sigil);

        base.Position = target;
        this._exploded = true;
        Vanish(suppressInAirEffects: true);

        yield break;
      }
    }

    protected override List<FluidBulletInfo> BuildChain() => Run(Attack()).Finish();

    private bool _fired = false;

    private void OnFired(tk2dSpriteAnimator animator, tk2dSpriteAnimationClip clip, int frame)
    {
      if (animator.CurrentFrame < 5 || !clip.name.Contains("skyshot"))
        return;
      animator.AnimationEventTriggered -= this.OnFired;
      this._fired = true;
    }

    private IEnumerator Attack()
    {
      const int VOLLEY_SIZE = 5;

      AIActor boss = base.BulletBank.aiActor;
      Transform t = boss.gameObject.transform;
      bool facingLeft = t.position.x > this.roomFullBounds.center.x;
      boss.sprite.FlipX = facingLeft;
      Vector2 shootPoint = t.position + GunBarrelHighOffset(boss.sprite.FlipX);
      float baseShootAngle = facingLeft ? 123f : 57f;

      for (int i = 0; i < 10; ++i)
      {
        this._fired = false;
        boss.spriteAnimator.AnimationEventTriggered += this.OnFired;
        boss.aiAnimator.PlayUntilFinished("skyshot");
        while (!this._fired)
          yield return Wait(1);
        for (int v = 0; v < VOLLEY_SIZE; ++v)
        {
          float shootAngle = baseShootAngle.AddRandomSpread(10f);
          base.Fire(Offset.OverridePosition(shootPoint), new Direction(shootAngle), new Speed(50f), new MeteorShowerBullet(this.roomFullBounds));
        }
        boss.gameObject.Play("armistice_gun_sound");
        CwaffVFX.Spawn(prefab: _MuzzleVFXBullet, position: shootPoint, rotation: baseShootAngle.EulerZ(), emissivePower: 10f,
          emissiveColor: ExtendedColours.vibrantOrange, emitColorPower: 8f);
        while (boss.aiAnimator.IsPlaying("skyshot"))
          yield return Wait(1);
      }
      yield break;
    }

  }

  private class TrickshotScript : ArmisticeBulletScript
  {
    protected override List<FluidBulletInfo> BuildChain() => Run(Attack()).Finish();

    private List<Geometry> _lines = null;
    private static readonly int _RayMask = CollisionMask.LayerToMask(CollisionLayer.HighObstacle);

    private static Vector2 Contact(Vector2 pos, Vector2 dir)
    {
        RaycastResult result;
        Vector2 contact = default;
        if (PhysicsEngine.Instance.Raycast(pos, dir, 999f, out result, true, false, _RayMask))
          contact = result.Contact;
        RaycastResult.Pool.Free(ref result);
        return contact;
    }

    private static readonly Color[] _Colors = [Color.cyan, Color.red, Color.green];

    private IEnumerator Attack()
    {
      base.TimeScale = -1f; // update every frame

      const int BLINKS = 3;
      const int ATTACKS = 15;
      const int ATTACK_MAX_RAMP = 5;
      const int TRICKS = 5;
      const int TRICKRATE = ATTACKS / TRICKS;
      const float BLINKTIME = 0.125f;
      const float MINBLINKTIME = 0.07f;
      const float ATTACK_GAP = 0.4f;
      const float MIN_ATTACK_GAP = 0.15f;

      PlayerController pc = GameManager.Instance.BestActivePlayer;
      Vector2 target = pc.CenterPosition;

      AIActor boss = base.BulletBank.aiActor;
      Vector2 shootPoint = boss.gameObject.transform.position + GunBarrelOffset(facingLeft: boss.sprite.FlipX);
      tk2dSlicedSprite dangerZone = BossShared.DoomZone(shootPoint, target, width: 1f, rise: false);
      dangerZone.TileStretchedSprites = true;
      dangerZone.SetBorder(1/3f, 0f, 1/3f, 0f);
      int orangeId = VFX.Collection.GetSpriteIdByName(boss.CenterPosition.x < pc.CenterPosition.x ? "reticle_caution_small" : "reticle_caution_small_inverted");
      int blueId = VFX.Collection.GetSpriteIdByName("reticle_safe_small");
      List<int> tricks = new List<int>(4);
      for (int i = 0; i < TRICKS; ++i)
        tricks.Add(UnityEngine.Random.Range(TRICKRATE * i, TRICKRATE * (i + 1)));

      for (int a = 0; a < ATTACKS; ++a)
      {
        bool trickery = tricks.Contains(a);
        dangerZone.SetSprite(trickery ? blueId : orangeId);

        float t = 1f - Mathf.Clamp01((float)a / ATTACK_MAX_RAMP);

        float blinkTime = MINBLINKTIME + (BLINKTIME - MINBLINKTIME) * t;
        boss.aiAnimator.PlayUntilCancelled("ready");
        for (int i = 0; i < BLINKS; ++i)
        {
          dangerZone.renderer.enabled = true;
          base.BulletBank.aiActor.gameObject.Play("armistice_danger_beep_sound");
          for (float wait = BraveTime.ScaledTimeSinceStartup + blinkTime; BraveTime.ScaledTimeSinceStartup < wait; )
          {
            dangerZone.Retarget(shootPoint, pc.CenterPosition);
            yield return Wait(1);
          }

          dangerZone.renderer.enabled = false;
          for (float wait = BraveTime.ScaledTimeSinceStartup + blinkTime; BraveTime.ScaledTimeSinceStartup < wait; )
            yield return Wait(1);
        }

        float delay = MIN_ATTACK_GAP + (ATTACK_GAP - MIN_ATTACK_GAP) * t;
        if (trickery)
        {
          bool shouldFire = false;
          for (float wait = BraveTime.ScaledTimeSinceStartup + delay; BraveTime.ScaledTimeSinceStartup < wait; )
          {
            if (pc.IsDodgeRolling)
            {
              shouldFire = true;
              break;
            }
            yield return Wait(1);
          }
          if (!shouldFire)
            continue; // continue with outer loop
          boss.gameObject.Play("armistice_laugh_2");
          while (pc.IsDodgeRolling && !pc.healthHaver.IsVulnerable)
            yield return Wait(1);
        }

        boss.aiAnimator.PlayUntilFinished("attack_snipe");
        for (int x = -1; x <= 1; ++x)
        {
          float launchAngle = (pc.CenterPosition - shootPoint).ToAngle();
          if (x != 0)
          {
            const float OFFSET = 1f;
            Vector2 perpendicular = (launchAngle + x * 90f).ToVector(OFFSET);
            launchAngle = (pc.CenterPosition + perpendicular - shootPoint).ToAngle();
          }
          SecretBullet laser = new SecretBullet(tint: Color.cyan);
          base.Fire(Offset.OverridePosition(shootPoint), new Direction(launchAngle), new Speed(200f), laser);
          laser.AddTrail(_TrickshotTrailPrefab, glow: 100f);
          boss.gameObject.PlayUnique("armistice_trickshot_sound");
          CwaffVFX.Spawn(prefab: _MuzzleVFXSnipe, position: shootPoint, rotation: launchAngle.EulerZ(), emissivePower: 10f,
            emissiveColor: ExtendedColours.vibrantOrange, emitColorPower: 8f);
        }

        for (float wait = BraveTime.ScaledTimeSinceStartup + delay; BraveTime.ScaledTimeSinceStartup < wait; )
        {
          if (!boss.aiAnimator.IsPlaying("attack_snipe"))
            boss.aiAnimator.PlayUntilCancelled("ready");
          yield return Wait(1);
        }
      }

      boss.aiAnimator.PlayUntilCancelled("idle");
      UnityEngine.Object.Destroy(dangerZone.gameObject);
      yield break;
    }

    //REFACTOR: factor out room bounds finding code to something shared by all scripts
    private IEnumerator AttackOld()
    {
      const int BOUNCES   = 10;
      const float EPSILON = 0.01f;
      const int RAYS      = 1;
      const float SPREAD  = 10f;

      if (_lines == null)
      {
        int numLines = (BOUNCES + 1) * (RAYS * 2 + 1);
        _lines = new List<Geometry>(numLines);
        for (int i = 0; i <= numLines; ++i)
          _lines.Add(new GameObject().AddComponent<Geometry>());
      }

      AIActor boss = base.BulletBank.aiActor;
      Vector2 refPoint = this.roomFullBounds.min + 0.25f * this.roomFullBounds.size;
      float left = Contact(refPoint, Vector2.left).x;
      float bottom = Contact(refPoint, Vector2.down).y;
      float right = Contact(refPoint, Vector2.right).x;
      float top = Contact(refPoint, Vector2.up).y;
      Rect trueBounds = new Rect(left, bottom, right - left, top - bottom);

      while (this != null)
      {
        Vector2 pos = boss.gameObject.transform.position + GunBarrelOffset(facingLeft: boss.sprite.FlipX);
        Vector2 ppos = GameManager.Instance.BestActivePlayer.CenterPosition;
        Vector2 angleVec = (ppos - pos).normalized;
        Vector2 isect = default;
        int idx = 0;
        for (int n = -RAYS; n <= RAYS; ++n)
        {
          Vector2 curAngleVec = angleVec.Rotate(n * SPREAD);
          Vector2 curPos = pos;
          Color c = _Colors[idx];
          for (int i = 0; i <= BOUNCES; ++i)
          {
            BraveMathCollege.LineSegmentRectangleIntersection(curPos + curAngleVec / 32f, curPos + 100 * curAngleVec, trueBounds.min, trueBounds.max, ref isect);
            this._lines[(BOUNCES + 1) * idx + i].Setup(shape: Geometry.Shape.LINE, color: c, pos: curPos, pos2: isect);
            curPos = isect;
            if (Mathf.Abs(isect.x - left) < EPSILON || Mathf.Abs(isect.x - right) < EPSILON)
              curAngleVec = new Vector2(-curAngleVec.x, curAngleVec.y);
            else
              curAngleVec = new Vector2(curAngleVec.x, -curAngleVec.y);
          }
          ++idx;
        }
        yield return Wait(1);
      }
      yield break;
    }

  }

  private class MagicMissileScript : ArmisticeBulletScript
  {

    internal class MagicMissileBullet : SecretBullet
    {
      private static ExplosionData _Explosion = null;

      private bool _orbiting = false;
      private bool _canBeDestroyed = false;
      private float _orbitRadius = 0f;
      private Rect _bounds;
      private EasyLight _light = null;

      public MagicMissileBullet(float orbitRadius, Rect bounds) : base(baseProj: "magicmissile")
      {
        this._orbitRadius = orbitRadius;
        this._bounds = bounds;
      }

      public override void Initialize()
      {
        base.Initialize();
        base.Projectile.specRigidbody.CollideWithTileMap = false;
        base.TimeScale = -1f; // update every frame
        if (_Explosion == null)
        {
          _Explosion = Explosions.ExplosiveRounds.With(damage: 0f, force: 100f, debrisForce: 10f, radius: 1.5f,
            preventPlayerForce: false, shake: false);
          _Explosion.doDamage = true;
          _Explosion.damageToPlayer = 0.5f;
        }
      }

      private float _lastVFXTime = 0f;
      private void DoVFX(float rate, Vector2? pos = null, float? dir = null)
      {
        float now = BraveTime.ScaledTimeSinceStartup;
        if (this._lastVFXTime + rate > now)
          return;
        this._lastVFXTime = now;
        float rot = dir ?? (base.Projectile.transform.rotation.eulerAngles.z + 180f);
        string sound = (rate > 0f) ? "armistice_missile_smoke_sound" : "armistice_missile_smoke_sound_short";
        if (rate > 0f)
          base.Projectile.gameObject.PlayOnce(sound);
        else
          base.Projectile.LoopSoundIf(true, sound);
        CwaffVFX.Spawn(
          prefab        : _MissileSmokeVFX,
          position      : pos ?? base.Projectile.sprite.WorldCenterLeft(),
          rotation      : rot.EulerZ(),
          velocity      : rot.ToVector(0.1f * base.Speed),
          lifetime      : 0.25f,
          fadeOutTime   : 0.25f
          );
      }

      private static readonly Color[] _LightColors = [
        Color.Lerp(Color.green, Color.white, 0.5f),
        Color.Lerp(Color.yellow, Color.white, 0.5f),
        Color.Lerp(ExtendedColours.orange, Color.white, 0.5f),
        Color.Lerp(Color.red, Color.white, 0.5f),
        Color.Lerp(Color.red, Color.white, 0.15f),
      ];

      public override IEnumerator Top()
      {
        // homing
        const float MAX_ACCEL       = 20f; // max positive speed change per second
        const float MAX_DECEL       = 60f; // max negative speed change per second
        const float MIN_SPEED       = 2f;  // minimum speed we can travel at
        const float MIN_SPEED_ANGLE = 90f;  // max angle from target before we travel at minimum speed
        const float MAX_SPEED       = 60f; // maximum speed we can travel at
        const float MIN_TURN        = 60f;  // minimum degrees per second we can turn at
        const float MAX_TURN        = 180f;  // maximum degrees per second we can turn at
        // const float ORBIT_RAD       = 1f;  // max distance to player before orbiting
        // const float ORBIT_RAD_SQR   = ORBIT_RAD * ORBIT_RAD;

        PlayerController pc = GameManager.Instance.BestActivePlayer;
        Projectile proj = base.Projectile;
        proj.collidesWithProjectiles = true;
        proj.collidesOnlyWithPlayerProjectiles = true;
        // proj.BulletScriptSettings.surviveRigidbodyCollisions = true;
        proj.specRigidbody.OnPreRigidbodyCollision += this.OnPreRigidbodyCollision;
        proj.UpdateCollisionMask();

        // course correct towards the player
        Color debugGreen = Color.green.WithAlpha(0.5f);
        while (!this._orbiting)
        {
          // proj.specRigidbody.DrawDebugHitbox(debugGreen);
          // DebugDraw.DrawDebugCircle(proj.gameObject, proj.gameObject.transform.position, 0.125f, Color.cyan.WithAlpha(0.5f));
          Vector2 ppos      = pc.CenterPosition;
          Vector2 delta     = (ppos - base.Position);
          float dtime       = BraveTime.DeltaTime;
          float curSpeed    = base.Speed;
          float curDir      = base.Direction;
          float relAngle    = curDir.RelAngleTo(delta.ToAngle());
          float dpsTurn     = Mathf.Lerp(MAX_TURN, MIN_TURN, Mathf.InverseLerp(MIN_SPEED, MAX_SPEED, curSpeed));
          float frameTurn   = Mathf.Sign(relAngle) * Mathf.Min(dpsTurn * dtime, Mathf.Abs(relAngle));
          float targetSpeed = Mathf.Lerp(MAX_SPEED, MIN_SPEED, 1f - Ease.InQuad(1f - Mathf.Min(Mathf.Abs(relAngle) / MIN_SPEED_ANGLE, 1f)) );
          float newSpeed    = Mathf.Clamp(targetSpeed, curSpeed - MAX_DECEL * dtime, curSpeed + MAX_ACCEL * dtime);
          float newDir      = curDir + frameTurn;

          DoVFX(1f / newSpeed);
          base.ChangeSpeed(new Speed(newSpeed));
          base.ChangeDirection(new Direction(newDir));
          yield return Wait(1);
        }

        // zip around player for a variable amount of time before locking on
        const float RPS_MIN    = 1f;
        const float RPS_MAX    = 8f;
        const float DPS_MIN    = 360f * RPS_MIN;
        const float DPS_MAX    = 360f * RPS_MAX;
        const float ACCEL_TIME = 1f;
        const float ORBIT_TIME = 0.85f;
        const float ORBIT_VAR  = 0.10f;
        const float DECEL_TIME = 0.25f;
        const float ORBIT_LERP = 5f;
        const float SMOKE_RATE = 13f;

        base.ManualControl = true;
        proj.collidesWithProjectiles = false;
        proj.UpdateCollisionMask();
        // bool counterClockwise = false; // TODO: be smart about this later
        float zipStart = BraveTime.ScaledTimeSinceStartup;
        float zipEnd = zipStart + ORBIT_TIME.AddRandomSpread(ORBIT_VAR);
        float decelStart = zipEnd - DECEL_TIME;
        float angleTotal = 0f;
        float lastAngle  = (base.Position - pc.CenterPosition).ToAngle();
        while (true)
        {
          float dtime       = BraveTime.DeltaTime;
          float now         = BraveTime.ScaledTimeSinceStartup;
          bool lastIter     = now >= zipEnd;
          if (lastIter)
            dtime -= (now - zipEnd);
          Vector2 ppos      = pc.CenterPosition;
          Vector2 delta     = (base.Position - ppos); //NOTE: inverted from above delta, we are now staying away from the player
          float zipSpeed    = Mathf.Lerp(DPS_MIN, DPS_MAX, Mathf.Clamp01((now - zipStart) / ACCEL_TIME));
          float decelLerp   = (now >= decelStart) ? Ease.OutQuad(Mathf.Clamp01((now - decelStart) / DECEL_TIME)) : 0f;
          if (decelLerp > 0)
            zipSpeed        = Mathf.Lerp(DPS_MAX, 0f, decelLerp);
          float curAngle    = delta.ToAngle();
          float curRad      = delta.magnitude;
          float newRad      = Lazy.SmoothestLerp(curRad, this._orbitRadius, ORBIT_LERP);
          float angleDelta  = dtime * zipSpeed;
          float newAngle    = curAngle + angleDelta;
          base.Position     = ppos + newAngle.ToVector(newRad);
          base.Direction    = (90f + newAngle + 90f * decelLerp);
          angleTotal       += angleDelta;
          while (angleTotal >= SMOKE_RATE)
          {
            angleTotal -= SMOKE_RATE;
            lastAngle += SMOKE_RATE;
            DoVFX(0f, pos: ppos + lastAngle.ToVector(newRad), dir: lastAngle - 90f);
          }
          if (lastIter)
            break;
          yield return Wait(1);
        }
        float finalAngle  = (base.Position - pc.CenterPosition).ToAngle();

        // stopping
        const int BLINKS_PER_PHASE = 8;
        const int BLINK_PHASES = 5;
        const float BLINK_TIME = 0.21f;
        const float BLINK_TIME_DEC = 0.03f;
        base.ManualControl = false; //NOTE: collisions stop working if our speed is zero, so just make the projectile move very slowly
        base.ChangeSpeed(new Speed(0.01f));
        this._canBeDestroyed = true;
        proj.collidesWithProjectiles = true;
        proj.UpdateCollisionMask();
        bool glow = false;
        this._light = EasyLight.Create(parent: base.Projectile.transform, color: ExtendedColours.vibrantOrange, radius: 2f, brightness: 10.0f);
        for (int p = 0; p < BLINK_PHASES; ++p)
        {
          this._light.SetColor(_LightColors[p]);
          for (int i = 0; i < BLINKS_PER_PHASE; ++i)
          {
            glow = !glow;
            if (glow)
            {
              base.BulletBank.aiActor.gameObject.Play("armistice_missile_beep_sound");
              this._light.TurnOn();
            }
            else
              this._light.TurnOff();
            for (float wait = BraveTime.ScaledTimeSinceStartup + (BLINK_TIME - p * BLINK_TIME_DEC); BraveTime.ScaledTimeSinceStartup < wait; )
            {
              if (!this._bounds.Contains(base.Position))
                base.Position = Lazy.SmoothestLerp(base.Position, pc.CenterPosition, 3f);
              base.Direction = (pc.CenterPosition - base.Position).ToAngle();
              DoVFX(1f / 5f);
              yield return Wait(1);
            }
          }
        }

        CwaffTrailController.Spawn(SubtractorBeam._RedTrailPrefab, base.Position, pc.CenterPosition); //TODO: use better trail
        Exploder.Explode(pc.CenterPosition, _Explosion, (pc.CenterPosition - base.Position).normalized, ignoreQueues: true);
        Vanish();
        yield break;
      }

      private void OnPreRigidbodyCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
      {
          if (otherRigidbody.gameObject.GetComponent<Projectile>() is not Projectile p || p.Owner is not PlayerController)
            return;
          if (this._canBeDestroyed)
          {
            Exploder.Explode(base.Position, _Explosion, default, ignoreQueues: true);
            Vanish();
          }
          else if (!this._orbiting)
          {
            this._orbiting = true;
            PhysicsEngine.SkipCollision = true;
            p.DieInAir();
          }
      }

      protected override void OnBaseDestruction(Projectile projectile)
      {
        if (this._light)
        {
          this._light.gameObject.transform.parent = null;
          UnityEngine.Object.Destroy(this._light.gameObject);
        }

        projectile.specRigidbody.OnPreRigidbodyCollision -= this.OnPreRigidbodyCollision;
        // projectile.sprite.transform.rotation = Quaternion.identity;
        // projectile.gameObject.transform.rotation = Quaternion.identity;
        base.OnBaseDestruction(projectile);
      }
    }

    protected override List<FluidBulletInfo> BuildChain() => Run(Attack()).Finish();

    private bool _fired = false;

    private void OnFired(tk2dSpriteAnimator animator, tk2dSpriteAnimationClip clip, int frame)
    {
      if (animator.CurrentFrame < 4 || !clip.name.Contains("crouch"))
        return;
      animator.AnimationEventTriggered -= this.OnFired;
      this._fired = true;
    }

    private IEnumerator Attack()
    {
      const int MISSILES = 4;
      const float BASE_RADIUS = 6f;
      const float RADIUS_GROW = 1f;
      AIActor boss = base.BulletBank.aiActor;

      List<MagicMissileBullet> mms = new(MISSILES);
      for (int n = 0; n < MISSILES; ++n)
      {
        if (n > 0)
        {
          this._fired = false;
          while (boss.spriteAnimator.IsPlaying("crouch"))
            yield return Wait(1);
          boss.aiAnimator.PlayUntilFinished("reload");
          while (boss.spriteAnimator.IsPlaying("reload"))
            yield return Wait(1);
          boss.aiAnimator.PlayUntilFinished("crouch");
        }
        boss.spriteAnimator.AnimationEventTriggered += this.OnFired;
        while (!this._fired)
          yield return Wait(1);
        Vector2 shootPoint = boss.gameObject.transform.position + GunBarrelLowOffset(boss.sprite.FlipX);

        MagicMissileBullet mm = new MagicMissileBullet(BASE_RADIUS + RADIUS_GROW * n, this.roomBulletBounds);
        mms.Add(mm);
        bool left = boss.sprite.FlipX;
        float angle = left ? 180f : 0f;
        base.Fire(Offset.OverridePosition(shootPoint), new Direction(angle), new Speed(60f), mm);
        boss.gameObject.Play("armistice_missile_launch_sound");
        CwaffVFX.Spawn(prefab: _MuzzleVFXBullet, position: shootPoint, rotation: angle.EulerZ(), emissivePower: 10f,
          emissiveColor: ExtendedColours.vibrantOrange, emitColorPower: 8f); // TODO: add missile muzzle
        CwaffVFX.SpawnBurst(
          prefab           : _MissileSmokeVFX,
          numToSpawn       : 15,
          basePosition     : shootPoint + new Vector2(left ? -0.75f : 0.75f, 0f),
          positionVariance : 1f,
          baseVelocity     : new Vector2(left ? -6f : 6f, 0f),
          velocityVariance : 3f,
          velType          : CwaffVFX.Vel.Random,
          rotType          : CwaffVFX.Rot.Velocity,
          lifetime         : 0.5f,
          fadeOutTime      : 0.5f
          );
      }

      for (int n = 0; n < MISSILES; ++n)
        while (!mms[n].IsEnded && !mms[n].Destroyed)
          yield return Wait(1);
      yield break;
    }

  }

  private class RunaroundScript : ArmisticeBulletScript
  {
    protected override List<FluidBulletInfo> BuildChain()
    {
      base.TimeScale = -1f; // update every frame while relocating

      return Run(Relocate())
        .Then(Relocate())
        .Then(Relocate())
        .Finish();
    }

    // private IEnumerator Attack()
    // {
    //   for (int i = 0; i < 10; ++i)
    //   {
    //     System.Console.WriteLine($"attempting run {i + 1}");
    //     IEnumerator relocator = Relocate();
    //     while (relocator.MoveNext())
    //       yield return (int)relocator.Current;
    //     yield return Wait(30);
    //   }
    //   yield break;
    // }

  }

  private class SniperScript : ArmisticeBulletScript
  {

    internal class SniperBullet : SecretBullet
    {

      public SniperBullet() : base()
      {
      }

      public override void Initialize()
      {
        base.Initialize();
      }

      public override IEnumerator Top()
      {
        yield break;
      }

    }

    protected override List<FluidBulletInfo> BuildChain() => Run(Attack()).Finish();

    private class VelSnapshot
    {
      public Vector2 velocity = default;
      public float expireTime = default;
    }

    private const float _VEL_SMOOTH_TIME = 0.5f;
    private const int _VEL_BUFFER_SIZE = 200;
    private static VelSnapshot[] _Velocities = new VelSnapshot[_VEL_BUFFER_SIZE];

    private IEnumerator Attack()
    {
      const float ATTACK_TIME = 9f;
      const float MAX_PROJ_SPEED = 45f;
      const float MIN_PROJ_SPEED = 15f;
      const float MAX_FIRE_RATE = 0.5f;
      const float MIN_FIRE_RATE = 0.06f;

      for (int i = 0; i < _VEL_BUFFER_SIZE; ++i)
        _Velocities[i] = new VelSnapshot();

      #if DEBUG
      // Geometry line = new GameObject().AddComponent<Geometry>();
      // Geometry vline = new GameObject().AddComponent<Geometry>();
      // Geometry shootLine = new GameObject().AddComponent<Geometry>();
      #endif
      //WARNING: can't use frame-perfect time scale or _VEL_BUFFER_SIZE can overflow during time slow effects
      // base.TimeScale = -1f;

      PlayerController pc = GameManager.Instance.GetRandomActivePlayer();
      AIActor actor       = base.BulletBank.aiActor;
      Transform tr        = actor.gameObject.transform;
      bool facingLeft     = tr.position.x > this.roomFullBounds.center.x;
      Vector2 shootPoint  = tr.position + GunBarrelOffset(facingLeft);
      Offset shootOff     = Offset.OverridePosition(shootPoint);

      float aimAngle;
      float time;
      Vector2 accumVel = default;

      float startTime = BraveTime.ScaledTimeSinceStartup;
      float endTime = startTime + ATTACK_TIME;
      float prevTime = startTime;
      float lastFireTime = startTime;

      int firstValidVel = 0;
      int nextValidVel = 0;

      actor.aiAnimator.PlayUntilCancelled("attack_basic");
      tk2dSpriteAnimator spriteAnim = actor.spriteAnimator;
      for (float now = startTime; now < endTime; now = BraveTime.ScaledTimeSinceStartup)
      {
        // update sliding average velocity window
        Vector2 frameVel = (now - prevTime) * pc.Velocity.ZeroIfNan();
        accumVel += frameVel;
        _Velocities[nextValidVel].velocity = frameVel;
        _Velocities[nextValidVel].expireTime = now + _VEL_SMOOTH_TIME;
        nextValidVel = (nextValidVel + 1) % _VEL_BUFFER_SIZE;
        for (int v = firstValidVel; v != nextValidVel; v = ((v + 1) % _VEL_BUFFER_SIZE))
        {
          firstValidVel = v;
          if (_Velocities[v].expireTime > now)
            break;
          accumVel -= _Velocities[v].velocity;
        }
        prevTime = now;
        Vector2 avgVelocity = (1f / _VEL_SMOOTH_TIME) * accumVel;

        // determine fire rate stats
        float t = (now - startTime) / ATTACK_TIME;
        float fireRate = Mathf.Lerp(MAX_FIRE_RATE, MIN_FIRE_RATE, Ease.OutCubic(t));
        float projSpeed = Mathf.Lerp(MAX_PROJ_SPEED, MIN_PROJ_SPEED, Ease.OutCubic(t));
        spriteAnim.ClipFps = 4f / fireRate; // 4 frames at 16fps == 0.25s per loop
        bool valid = Lazy.DeterminePerfectAngleToShootAt(shootPoint, pc.CenterPosition, avgVelocity, projSpeed, out aimAngle, out time, adjustForTurboMode: true);

        #if DEBUG
        // line.Setup(shape: Geometry.Shape.LINE, color: valid ? Color.green : Color.red, pos: shootPoint, pos2: pc.CenterPosition + time * avgVelocity);
        // vline.Setup(shape: Geometry.Shape.LINE, color: Color.yellow, pos: pc.CenterPosition, pos2: pc.CenterPosition + time * avgVelocity);
        #endif

        // figure out if we actually need to fire
        if (now >= (lastFireTime + fireRate))
        {
          lastFireTime = now;
          #if DEBUG
          // shootLine.Setup(shape: Geometry.Shape.LINE, color: Color.cyan, pos: shootPoint, pos2: pc.CenterPosition + time * avgVelocity);
          #endif
          actor.gameObject.PlayUnique("armistice_gun_spread_sound");
          for (int i = -3; i <= 3; ++i)
            base.Fire(shootOff, new Direction(aimAngle + 5f * i), new Speed(projSpeed), new SniperBullet());
          CwaffVFX.Spawn(prefab: _MuzzleVFXBullet, position: shootPoint, rotation: ((facingLeft ? 180f : 0f).AddRandomSpread(10f)).EulerZ(), emissivePower: 10f,
            emissiveColor: ExtendedColours.vibrantOrange, emitColorPower: 8f);
          CwaffVFX.SpawnBurst(
            prefab           : _MissileSmokeVFX,
            numToSpawn       : 5,
            basePosition     : shootPoint + new Vector2(facingLeft ? -1f : 1f, 0f),
            positionVariance : 1f,
            baseVelocity     : new Vector2(facingLeft ? -3f : 3f, 0f),
            velocityVariance : 1.5f,
            velType          : CwaffVFX.Vel.Random,
            rotType          : CwaffVFX.Rot.Velocity,
            lifetime         : 0.6f,
            fadeOutTime      : 0.6f
            );
        }
        yield return Wait(1);
      }

      spriteAnim.ClipFps = 0f;
      actor.aiAnimator.PlayUntilCancelled("idle");

      #if DEBUG
      // UnityEngine.Object.Destroy(line.gameObject);
      // UnityEngine.Object.Destroy(vline.gameObject);
      // UnityEngine.Object.Destroy(shootLine.gameObject);
      #endif

      yield break;
    }

  }
}
