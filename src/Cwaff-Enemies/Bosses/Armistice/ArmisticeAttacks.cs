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

      protected int originalLayer = -1;
      protected GameObject _trail = null;

      public SecretBullet(Color? tint = null, float emission = 10f, float emitColorPower = 1.5f) : base("getboned")
      {
        this._tint = tint;
        this._emission = emission;
        this._emitColorPower = emitColorPower;
      }

      protected void OnBaseDestruction(Projectile projectile)
      {
        if (this._trail)
        {
          UnityEngine.Object.Destroy(this._trail);
          this._trail = null;
        }
        if (this.originalLayer > -1)
        {
          projectile.gameObject.SetLayerRecursively(this.originalLayer);
          this.originalLayer = -1;
        }
        projectile.OnDestruction -= this.OnBaseDestruction;
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
        this.Projectile.OnDestruction += this.OnBaseDestruction;
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

    protected override List<FluidBulletInfo> BuildChain()
    {
      return Run(Attack()).Finish();
    }

    private IEnumerator Attack()
    {
      const float SPEED = 25f;
      const int RATE    = 5; // frames between bullets
      const int TIME    = 60 * 5; // frames the attack lasts
      const int GAP     = 5; // size of the path we have to navigate
      const int ROWS    = 50; // number of rows of bullets
      const int NOGAP   = 3; // number of columns at the end with no gap

      PathRect roomRect = new PathRect(this.roomFullBounds.Inset(1f, 0.25f));
      Speed sspeed = new Speed(SPEED);

      Vector2 ppos = GameManager.Instance.BestActivePlayer.CenterPosition;
      bool leftward = (ppos.x < this.roomFullBounds.center.x);
      Direction dir = new Direction(leftward ? 180f : 0f);
      PathLine wall = leftward ? roomRect.Right() : roomRect.Left();
      float wallOff = leftward ? -2.5f : 2.5f;

      int gapPos = Mathf.RoundToInt(ROWS * ((ppos.y - wall.start.y) / (wall.end.y - wall.start.y)));

      for (int i = 0; i < TIME; i += RATE)
      {
        bool hasGap = i < (TIME - RATE * NOGAP);
        for (int j = 0; j <= ROWS; ++j)
        {
          if (hasGap && Mathf.Abs(j - gapPos) < GAP)
            continue;
          float off = (float)j / ROWS;
          Vector2 bpos = wall.At(off) + new Vector2(wallOff * UnityEngine.Random.value, 0f);
          this.Fire(Offset.OverridePosition(bpos), dir, sspeed, new SecretBullet());
        }
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
        this.Projectile.OnDestruction += this.OnBaseDestruction;

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

    internal class BoxTrotBullet : SecretBullet
    {

      private bool _shooter = false;
      private float _offset = 0.0f;

      public BoxTrotBullet() : base()
      {
      }

      public override void Initialize()
      {
        base.Initialize();
      }

      public override IEnumerator Top()
      {
        this._trail = base.Projectile.AddTrail(SubtractorBeam._RedTrailPrefab).gameObject;
        this._trail.SetGlowiness(100f);
        yield break;
      }

    }

    protected override List<FluidBulletInfo> BuildChain()
    {
      return Run(Attack()).Finish();
    }

    private class ShootData
    {
      public Vector2 lastPos;
      public float nextShootTime;
    }

    private IEnumerator Attack()
    {
      const int BULLETS = 60;
      const int SHOOTERS = 2;
      const int SPEED = 50;
      const float LENGTH = 800;
      const float RPS = 1f / 400f;
      const float COOLDOWN = 0.5f;
      const float EPSILON = 0.1f;

      Rect bounds = this.roomFullBounds.Inset(1f, 1.25f, 2f, 1f);
      Vector2 boundsCenter = bounds.center;
      float top = bounds.yMin + EPSILON;
      float bottom = bounds.yMax - EPSILON;
      float midX = boundsCenter.x;
      float midY = boundsCenter.y;
      PathRect path = new PathRect(bounds);

      List<SecretBullet> bullets = new();
      List<SecretBullet> shooterBullets = new(SHOOTERS);
      List<int> shooterIndices = new(SHOOTERS);
      List<ShootData> shootData = new(SHOOTERS);

      for (int i = 0; i < SHOOTERS; ++i)
      {
        int index = -1;
        while (index < 0 || shooterIndices.Contains(index))
          index = (int)(BULLETS * UnityEngine.Random.value);
        shooterIndices.Add(index);
      }

      for (int i = 0; i < BULLETS; ++i)
      {
        float off = (float)i / BULLETS;
        Vector2 pos = path.At(off);
        bool isShooter = shooterIndices.Contains(i);
        SecretBullet bullet = new SecretBullet(tint: isShooter ? Color.cyan : null);
        bullet.ManualControl = true;
        bullets.Add(bullet);
        if (isShooter)
        {
          shooterBullets.Add(bullet);
          shootData.Add(new ShootData(){lastPos =  pos, nextShootTime = 0f});
        }
        this.Fire(Offset.OverridePosition(pos), bullet);
      }

      for (int i = 0; i < LENGTH; ++i)
      {
        float t = RPS * i;

        for (int n = 0; n < BULLETS; ++n)
        {
          if (!bullets[n].Projectile)
            continue;
          float off = (float)n / BULLETS;
          bullets[n].Position = path.At((t + off) % 1f);
        }

        bool oldSide;
        bool curSide;
        float now = BraveTime.ScaledTimeSinceStartup;
        for (int s = 0; s < SHOOTERS; ++s)
        {
          SecretBullet shooter = shooterBullets[s];
          if (!shooter.Projectile || shooter.Destroyed)
            continue;

          Vector2 curPos = shooter.Position;
          if (shootData[s].nextShootTime > now)
          {
            shootData[s].lastPos = curPos;
            continue;
          }

          float y = curPos.y;
          bool horizontal = y < bottom && y > top;
          foreach (PlayerController player in GameManager.Instance.AllPlayers)
          {
            Vector2 ppos = player.CenterPosition;
            if (horizontal)
            {
              oldSide = ppos.y < shootData[s].lastPos.y;
              curSide = ppos.y < curPos.y;
            }
            else
            {
              oldSide = ppos.x < shootData[s].lastPos.x;
              curSide = ppos.x < curPos.x;
            }

            if (oldSide == curSide)
              continue;

            float angle;
            if (horizontal)
              angle = curPos.x < midX ? 0f : 180f;
            else
              angle = curPos.y < midY ? 90f : 270f;

            BoxTrotBullet newBullet = new BoxTrotBullet();
            base.Fire(Offset.OverridePosition(curPos), new Direction(angle), new Speed(SPEED), newBullet);
            shootData[s].nextShootTime = now + COOLDOWN;
          }

          shootData[s].lastPos = curPos;
        }

        yield return Wait(1);
      }

      for (int n = 0; n < BULLETS; ++n)
        if (bullets[n].Projectile)
          bullets[n].Vanish();

      yield break;
    }

  }
}
