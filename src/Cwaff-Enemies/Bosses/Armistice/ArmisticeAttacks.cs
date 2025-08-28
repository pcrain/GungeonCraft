namespace CwaffingTheGungy;

public partial class ArmisticeBoss : AIActor
{
  private const string SOUND_SPAWN       = "no_sound"; // "Play_OBJ_turret_set_01";
  private const string SOUND_SPAWN_QUIET = "no_sound"; // "undertale_pullback";
  private const string SOUND_SHOOT       = "no_sound"; // "Play_WPN_spacerifle_shot_01";
  private const string SOUND_TELEPORT    = "teledasher";

  private class SecretBullet : Bullet
  {
      private static readonly Color _DefaultTint = new Color(1.0f,0.5f,0.5f,0.5f);

      private Color? _tint = null;
      private float _emission;
      private float _emitColorPower;

      public SecretBullet(Color? tint = null, float emission = 10f, float emitColorPower = 1.5f) : base("getboned")
      {
        this._tint = tint;
        this._emission = emission;
        this._emitColorPower = emitColorPower;
      }

      public override void Initialize()
      {
        this.Projectile.ChangeTintColorShader(0f, this._tint ?? _DefaultTint);
        Material mat = this.Projectile.sprite.renderer.material;
        mat.SetFloat(CwaffVFX._EmissivePowerId, this._emission);
        mat.SetFloat(CwaffVFX._EmissiveColorPowerId, this._emitColorPower);
        base.Initialize();
      }
  }

  private abstract class SecretBulletScript : FluidBulletScript
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
  }

  // Shoots bullets to the top corners of the screen that violently fly down towards the bottom center after a short delay
  private class CrossBulletsScript : SecretBulletScript
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
      private       GameObject trail = null;
      private       int originalLayer = -1;

      public CrossBullet(Vector2 velocity, Vector2 gravity, Vector2 target, Rect roomFullBounds) : base()
      {
        this.gravity        = gravity;
        this.startVelocity  = velocity;
        this.target         = target;
        this.roomFullBounds = roomFullBounds;
      }

      private void OnDestruction(Projectile projectile)
      {
        if (this.trail)
        {
          UnityEngine.Object.Destroy(this.trail);
          this.trail = null;
        }
        if (this.originalLayer > -1)
        {
          projectile.gameObject.SetLayerRecursively(this.originalLayer);
          this.originalLayer = -1;
        }
        projectile.OnDestruction -= this.OnDestruction;
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
        this.Projectile.OnDestruction += this.OnDestruction;
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
            this.trail = this.Projectile.AddTrail(SubtractorBeam._RedTrailPrefab).gameObject;
            this.trail.SetGlowiness(100f);
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

  private class ClocksTickingScript : SecretBulletScript
  {
    private const float _LIFETIME = 6f;
    private const float _GAP = 1.4f;

    private float _time = 0f;

    internal class ClocksTickingBullet : SecretBullet
    {

      private ClocksTickingScript _parent = null;
      private Vector2 _spawnCenter        = default;
      private float _spawnTime            = 0f;
      private float _spawnRadius          = 0f;
      private float _spawnAngle           = 0f;
      private float _rps                  = 0f;
      private bool  _flood                = false;

      public ClocksTickingBullet(ClocksTickingScript parent, Vector2 spawnCenter, float spawnTime, float spawnRadius, float spawnAngle, float rps, bool flood) : base()
      {
        this._parent      = parent;
        this._spawnCenter = spawnCenter;
        this._spawnTime   = spawnTime;
        this._spawnRadius = spawnRadius;
        this._spawnAngle  = spawnAngle;
        this._rps         = rps;
        this._flood       = flood;

        base.ManualControl = true;
      }

      public override void Initialize()
      {
        base.Initialize();
        if (this._flood)
        {
          SpeculativeRigidbody srb = this.Projectile.specRigidbody;
          srb.OnPreRigidbodyCollision += this.OnPreRigidbodyCollision;
          srb.CollideWithTileMap = false;
        }
      }

      private void OnPreRigidbodyCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
      {
          if (!otherRigidbody || !otherRigidbody.gameObject.GetComponent<PlayerController>())
            PhysicsEngine.SkipCollision = true;
      }

      public override IEnumerator Top()
      {
        while (true)
        {
            float elapsed = BraveTime.ScaledTimeSinceStartup - this._spawnTime;
            if (elapsed > _LIFETIME)
              break;
            // if (!this._flood)
            {
              float rot = 360f * elapsed * this._rps;
              base.Position = this._spawnCenter + (this._spawnAngle + rot).ToVector(this._spawnRadius);
            }
            yield return Wait(1);
        }
        Vanish();
        yield break;
      }
    }

    protected override List<FluidBulletInfo> BuildChain()
    {
      return
         Run(Attack(radius:  8f, rps: -0.300f, offAngle: 110f))
        .And(Attack(radius: 11f, rps: -0.625f, offAngle:  90f))
        .And(Attack(radius: 14f, rps: -0.850f, offAngle:  70f))
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
          Vector2 pos = center + (baseAngle + quantRot + trueRot).ToVector(trueRadius); //NOTE: deliberately need to offset by rot twice
          this.Fire(Offset.OverridePosition(pos), new Direction(0f, DirectionType.Absolute), new Speed(0f),
            new ClocksTickingBullet(
              parent      : this,
              spawnCenter : center,
              spawnTime   : start,
              spawnRadius : trueRadius,
              spawnAngle  : baseAngle + quantRot,
              rps         : rps,
              flood       : true
              ));
        }
      }
      yield break;
    }

    private IEnumerator Attack(float radius, float rps, float offAngle)
    {
      float start = BraveTime.ScaledTimeSinceStartup;
      int count = Mathf.FloorToInt(radius / _GAP);
      Vector2 center = this.roomFullBounds.center;
      float baseAngle = ((center - GameManager.Instance.BestActivePlayer.CenterPosition).ToAngle() + offAngle).Clamp360();
      for (int i = 1; i <= count; ++i)
      {
        this._time = BraveTime.ScaledTimeSinceStartup;
        float trueRadius = _GAP * i;
        float rot = 360f * (this._time - start) * rps;
        this.Fire(Offset.OverridePosition(center + (baseAngle + rot).ToVector(trueRadius)), new Direction(0f, DirectionType.Absolute), new Speed(0f),
          new ClocksTickingBullet(
            parent      : this,
            spawnCenter : center,
            spawnTime   : start,
            spawnRadius : trueRadius,
            spawnAngle  : baseAngle,
            rps         : rps,
            flood       : false
            ));
        yield return Wait(3);
      }
      float endTime = start + _LIFETIME;
      while (this._time < endTime)
      {
        yield return Wait(1);
        this._time = BraveTime.ScaledTimeSinceStartup;
      }
      yield break;
    }
  }

  private class WalledInScript : SecretBulletScript
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
}
