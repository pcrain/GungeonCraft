using System.Collections.Generic;
using System.Collections;

using UnityEngine;
using Brave.BulletScript;

namespace CwaffingTheGungy
{

public partial class SansBoss : AIActor
{
  private const string SOUND_SPAWN       = "Play_OBJ_turret_set_01";
  private const string SOUND_SPAWN_QUIET = "undertale_pullback";
  private const string SOUND_SHOOT       = "Play_WPN_spacerifle_shot_01";
  private const string SOUND_TELEPORT    = "teledasher";

  private class SecretBullet : Bullet
  {
      private Color? tint = null;
      public SecretBullet(Color? tint = null) : base("getboned")
      {
        this.tint = tint;
      }

      public override void Initialize()
      {
        this.Projectile.ChangeTintColorShader(0f,tint ?? new Color(1.0f,0.5f,0.5f,0.5f));
        base.Initialize();
      }
  }

  private class SineBullet : SecretBullet
  {
    private float  amplitude        = 1f;
    private float  freq             = 1f;
    private float  phase            = 0f;
    private float? rotationOverride = null;

    private float lifetime  = 0f;

    public SineBullet(float amplitude, float freq, float phase = 0, float? rotationOverride = null) : base()
    {
      this.amplitude        = amplitude;
      this.freq             = freq;
      this.phase            = phase;
      this.rotationOverride = rotationOverride;
    }

    public override IEnumerator Top()
    {
      AkSoundEngine.PostEvent(SOUND_SHOOT, GameManager.Instance.PrimaryPlayer.gameObject);

      Vector2 startSpeed   = this.RealVelocity();
      float rotationNormal = ((this.rotationOverride ?? startSpeed.ToAngle()) + 90f).Clamp180();
      Vector2 amp          = amplitude * rotationNormal.ToVector();
      Vector2 anchorPos    = this.Position - Mathf.Sin(phase) * amp;
      float adjfreq        = freq * 2f * Mathf.PI;
      this.ChangeSpeed(new Speed(0));
      while (true)
      {
        this.lifetime       += BraveTime.DeltaTime;
        anchorPos           += startSpeed;
        Vector2 oldPosition  = this.Position;
        float curPhase       = Mathf.Sin(adjfreq*this.lifetime + phase);
        this.Position        = anchorPos + curPhase * amp;
        this.ChangeDirection(new Direction((this.Position-oldPosition).ToAngle(),DirectionType.Absolute));
        yield return Wait(1);
      }
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

  // Shoots undertale-esque blue / orange bullets from all directions
  private class OrangeAndBlueScript : SecretBulletScript
  {
    private static readonly string orangeReticle = "reticle-orange";
    private static readonly string blueReticle   = "reticle-blue";
    private static readonly Color  orangeColor   = new Color(1.0f,0.75f,0f,0.5f);
    private static readonly Color  blueColor     = new Color(0.65f,0.65f,1.0f,0.5f);

    private const int   COUNT    = 32;
    private const int   WAVES    = 5;
    private const int   BATCH    = 8;
    private const float SPEED    = 50f;
    private const float LENIENCE = 30f;
    private const float COOLDOWN = 30f;

    // orange = harmless if you're moving; blue = harmless if you're stationary
    internal class OrangeAndBlueBullet : SecretBullet
    {
      private bool orange;
      public OrangeAndBlueBullet(bool orange) : base(orange ? orangeColor : blueColor)
      {
        this.orange = orange;
      }

      public override IEnumerator Top()
      {
        this.Projectile.specRigidbody.OnPreRigidbodyCollision += this.OnPreCollision;
        yield break;
      }

      private void OnPreCollision(SpeculativeRigidbody me, PixelCollider myPixelCollider, SpeculativeRigidbody other, PixelCollider otherPixelCollider)
      {
        if (!(other.gameActor is PlayerController))
          return;
        bool playerIsIdle           = other.gameActor.spriteAnimator.CurrentClip.name.Contains("idle",true);
        PhysicsEngine.SkipCollision = (this.orange != playerIsIdle);
      }
    }

    protected override List<FluidBulletInfo> BuildChain()
    {
      return
      Run(DoTheThing(Lazy.CoinFlip()))
      .Then(DoTheThing(Lazy.CoinFlip()))
      .Then(DoTheThing(Lazy.CoinFlip()))
      .Finish();
    }

    private IEnumerator DoTheThing(bool orange)
    {
      AkSoundEngine.PostEvent("undertale_eyeflash", GameManager.Instance.PrimaryPlayer.gameObject);
      Vector2 ppos = GameManager.Instance.PrimaryPlayer.CenterPosition;
      for (float i = 1f; i <= 4f; ++i)
        DoomZone(ppos - i*0.5f*Vector2.right, ppos + i*0.5f*Vector2.right, i, 0.5f, 1, orange ? orangeReticle : blueReticle);
      List<Vector2> points = new PathRect(base.roomBulletBounds).SampleUniform(COUNT);
      yield return Wait(LENIENCE);
      for(int wave = 0; wave < WAVES; ++wave)
      {
        AkSoundEngine.PostEvent(SOUND_SHOOT, GameManager.Instance.PrimaryPlayer.gameObject);
        ppos      = GameManager.Instance.PrimaryPlayer.CenterPosition;
        int count = 0;
        foreach(Vector2 p in points)
        {
          this.Fire(Offset.OverridePosition(p), new Direction(((ppos+Lazy.RandomAngle().ToVector(magnitude: 3f))-p).ToAngle(),DirectionType.Absolute),
            new Speed(SPEED,SpeedType.Absolute), new OrangeAndBlueBullet(orange:orange));
          if (++count % BATCH == 0)
            yield return Wait(1);
        }
      }
      yield return Wait(COOLDOWN);
    }
  }

  // Shoots an enclosing sinusoidal pattern of bullets from the walls of the room
  private class SineWaveScript : SecretBulletScript
  {

    protected override List<FluidBulletInfo> BuildChain()
    {
      AkSoundEngine.PostEvent("sans_laugh", GameManager.Instance.PrimaryPlayer.gameObject);
      return
      Run(DoTheThing())
        .And(DoTheThing(reverse: true))
        .And(DoTheThing(inverse: true))
        .And(DoTheThing(reverse: true, inverse: true))
      .Finish();
    }

    private const int COUNT = 50;
    private IEnumerator DoTheThing(bool reverse = false, bool inverse = false)
    {
      PathLine theEdge = inverse ? (new PathRect(base.roomBulletBounds).Right()) : (new PathRect(base.roomBulletBounds).Left());
      int i = 0;
      foreach(Vector2 p in theEdge.SampleUniform(COUNT,reverse ? 0.9f : 0.1f,reverse ? 0.1f : 0.9f))
      {
          this.Fire(Offset.OverridePosition(p), new Direction(inverse ? 180f : 0f,DirectionType.Absolute),
            new Speed(20f,SpeedType.Absolute), new SineBullet(3f,reverse ? -1f : 1f, (reverse ? -i : i) * 0.1f, 0f));
          yield return Wait(5);
          ++i;
      }
    }
  }

  // Shoots a spiral of bullets outward from the boss
  private class WiggleWaveScript : SecretBulletScript
  {

    protected override List<FluidBulletInfo> BuildChain()
    {
      AkSoundEngine.PostEvent("sans_laugh", theBoss.gameObject);
      int version = Lazy.CoinFlip() ? 1 : 2;
      return
      Run(DoTheThing(0f, version))
        .And(DoTheThing(0.25f, version))
        .And(DoTheThing(0.50f, version))
        .And(DoTheThing(0.75f, version))
      .Finish();
    }

    private const int COUNT = 57;
    private IEnumerator DoTheThing(float start, int version)
    {
      Vector2 middle       = theBoss.sprite.WorldCenter;
      PathCircle theCircle = new PathCircle(middle,2f);
      int i                = 0;
      int waitTime         = (version == 1 ? 5 : 4);
      foreach(Vector2 p in theCircle.SampleUniform(COUNT,start: start, end:start + (version == 1 ? 1f : 2f))) // 2 rotations
      {
          this.Fire(Offset.OverridePosition(p), new Direction(theCircle.AngleTo(p),DirectionType.Absolute),
            new Speed(12f,SpeedType.Absolute), new SineBullet(3f, 0.5f, 0f, null));
          yield return Wait(waitTime);
          if (version == 2 && ++i % 5 == 0)
          {
            yield return Wait(30);
          }
      }
    }
  }

  // Slams the player against the wall in alternating horizontal / vertical directions and follows up with a bullet shower
  private class WallSlamScript : SecretBulletScript
  {

    internal class GravityBullet : SecretBullet
      {
      private const int     LIFETIME       = 30;
      private const int     VANISHTIME     = 120;
      private       Vector2 gravity        = Vector2.zero;
      private       bool    skipCollisions = true;
      private       Vector2 startVelocity  = Vector2.zero;
      private       Rect    roomFullBounds;
      public GravityBullet(Vector2 velocity, Vector2 gravity, Rect roomFullBounds) : base()
      {
        this.gravity        = gravity;
        this.startVelocity  = velocity;
        this.roomFullBounds = roomFullBounds;
      }

      public override void Initialize()
      {
        base.Initialize();
        this.skipCollisions = true;
        this.Projectile.BulletScriptSettings.surviveTileCollisions = true;
        this.Projectile.specRigidbody.OnPreTileCollision += (_,_,_,_) => {
          if (this.skipCollisions)
            PhysicsEngine.SkipCollision = true;
        };
      }

      public override IEnumerator Top()
      {
        AkSoundEngine.PostEvent(SOUND_SHOOT, this.Projectile.gameObject);
        // Vector2 newVelocity = this.RealVelocity();
        Vector2 newVelocity = this.startVelocity;
        for (int i = 0; i < VANISHTIME; ++i)
        {
          if (i >= LIFETIME && this.skipCollisions && this.roomFullBounds.Contains(this.Position))
          {
            this.skipCollisions = false;
            this.Projectile.BulletScriptSettings.surviveTileCollisions = false;
          }
          newVelocity += gravity;
          this.ChangeDirection(new Direction(newVelocity.ToAngle(),DirectionType.Absolute));
          this.ChangeSpeed(new Speed(newVelocity.magnitude,SpeedType.Absolute));
          yield return Wait(1);
        }
        Vanish();
        yield break;
      }
    }

    private const int   COUNT     = 10;
    private const float SPREAD    = 9f;
    private const float GRAVITY   = 1.0f;
    private const float VELOCITY  = 20f;
    private const float BASESPEED = VELOCITY*GRAVITY;
    private const int   SHOTDELAY = 3;
    private const int   SLAMS     = 5;
    private const int   SLAMDELAY = 60;

    private PathRect slamBoundsPath;

    protected override List<FluidBulletInfo> BuildChain()
    {
      this.slamBoundsPath = new PathRect(base.roomSlamBounds);

      bool vertical = Lazy.CoinFlip();
      FluidBulletInfo f = Run(DoTheThing(Lazy.CoinFlip() ? (vertical ? "up" : "left") : (vertical ? "down" : "right")));
      for (int i = 1 ; i < SLAMS; ++i)
      {
        vertical = (!vertical);
        f = f.And(DoTheThing(Lazy.CoinFlip() ? (vertical ? "up" : "left") : (vertical ? "down" : "right")), withDelay: i * SLAMDELAY);
      }

      return f.Finish();
    }

    private IEnumerator DoTheThing(string direction)
    {
      PlayerController p = GameManager.Instance.PrimaryPlayer;

      PathLine segment;
        if (direction.Equals("up"))         segment = this.slamBoundsPath.Top();
        else if (direction.Equals("down"))  segment = this.slamBoundsPath.Bottom();
        else if (direction.Equals("left"))  segment = this.slamBoundsPath.Left();
        else if (direction.Equals("right")) segment = this.slamBoundsPath.Right();
        else /* should never happen */      segment = this.slamBoundsPath.Top();
      Vector2 target =  segment.At(0.5f);

      Vector2 delta    = (target - p.CenterPosition);
      Vector2 gravity  = GRAVITY*delta.normalized;
      Vector2 gravityB = GRAVITY*(target - theBoss.sprite.WorldCenter).normalized;
      Vector2 baseVel  = -VELOCITY * gravityB;
      Speed s = new Speed(BASESPEED,SpeedType.Absolute);
      Offset o = Offset.OverridePosition(theBoss.sprite.WorldCenter);
      AkSoundEngine.PostEvent("sans_laugh", theBoss.gameObject);
      for(int i = 0; i < COUNT; ++i)
      {
        Vector2 bulletvel = baseVel.Rotate(UnityEngine.Random.Range(-SPREAD,SPREAD));
        Direction d = new Direction(bulletvel.ToAngle().Clamp180(),DirectionType.Absolute);
        this.Fire(o, d, s, new GravityBullet(bulletvel,gravityB,base.roomFullBounds));
        yield return Wait(SHOTDELAY);
      }

      p.SetInputOverride("comeonandslam");
      p.ForceStopDodgeRoll();
      int collisionmask = CollisionMask.LayerToMask(CollisionLayer.EnemyHitBox, CollisionLayer.EnemyCollider, CollisionLayer.Projectile);
      p.specRigidbody.AddCollisionLayerIgnoreOverride(collisionmask);
      Vector2 slamStart        = base.roomSlamBounds.position;
      Vector2 slamEnd          = base.roomSlamBounds.position + base.roomSlamBounds.size;
      Vector2 finalPos         = Vector2.zero;
      p.specRigidbody.Velocity = Vector2.zero;
      int framesToReachTarget = Mathf.FloorToInt(Mathf.Sqrt(2*delta.magnitude/GRAVITY)); // solve x = (0.5*a*t*t) for t
      for (int frames = 0; frames < framesToReachTarget; ++frames)
      {
        p.specRigidbody.Velocity += gravity;
        Vector2 oldPos = p.specRigidbody.Position.GetPixelVector2();
        Vector2 newPos = oldPos + p.specRigidbody.Velocity;
        if (BraveMathCollege.LineSegmentRectangleIntersection(oldPos, newPos, segment.start, segment.end, ref finalPos))
          break;
        p.transform.position = newPos;
        p.specRigidbody.Reinitialize();
        yield return Wait(1);
      }
      p.specRigidbody.RemoveCollisionLayerIgnoreOverride(collisionmask);
      p.specRigidbody.Velocity = Vector2.zero;
      p.transform.position = (finalPos != Vector2.zero) ? finalPos : target;
      p.specRigidbody.Reinitialize();
      yield return Wait(1);

      p.ClearInputOverride("comeonandslam");
      GameManager.Instance.MainCameraController.DoScreenShake(new ScreenShakeSettings(0.5f,6f,0.5f,0f), null);
      AkSoundEngine.PostEvent("undertale_damage", p.gameObject);
    }
  }

  // Shoots telegraphed bullets from random points on the room perimeter toward the player
  private class ChainBulletScript : SecretBulletScript
  {

    public class ChainBullet : SecretBullet
    {
      public override IEnumerator Top()
      {
        AkSoundEngine.PostEvent(SOUND_SHOOT, this.Projectile.gameObject);
        yield break;
      }
    }

    private const int PHASES          = 3;
    private const int STREAMSPERPHASE = 5;
    private const int PHASEDELAY      = 20;
    private const int SHOTSPERSTREAM  = 12;
    private const int SHOTDELAY       = 5;
    private const int SHOTSPEED       = 20;
    private const float MINDIST       = 12f;

    protected override List<FluidBulletInfo> BuildChain()
    {
      return Run(DoTheThing())
      .Finish();
    }

    private IEnumerator DoTheThing()
    {
      for (int i = 0; i < PHASES; ++i)
      {
        PlayerController target = GameManager.Instance.PrimaryPlayer;
        AkSoundEngine.PostEvent("sans_laugh", theBoss.gameObject);
        Vector2 ppos = target.CenterPosition;
        List<Vector2> spawnPoints = new List<Vector2>(STREAMSPERPHASE);
        List<float> shotAngles = new List<float>(STREAMSPERPHASE);
        for (int s = 0; s < STREAMSPERPHASE; ++s)
        {
          Vector2 spawnPoint = base.roomBulletBounds.RandomPointOnPerimeter();
          while((ppos-spawnPoint).magnitude < MINDIST)
            spawnPoint = base.roomBulletBounds.RandomPointOnPerimeter();
          spawnPoints.Add(spawnPoint);
          shotAngles.Add((ppos-spawnPoint).ToAngle().Clamp180());
          DoomZone(spawnPoint, spawnPoints[s].RaycastToWall(shotAngles[s], base.roomFullBounds), 1f, PHASEDELAY / C.FPS, 10);
          AkSoundEngine.PostEvent(SOUND_SPAWN, target.gameObject);
          yield return Wait(SHOTDELAY);
        }
        for (int j = 0; j < SHOTSPERSTREAM; ++j)
        {
          for (int s = 0; s < STREAMSPERPHASE; ++s)
            this.Fire(Offset.OverridePosition(spawnPoints[s]), new Direction(shotAngles[s],DirectionType.Absolute), new Speed(SHOTSPEED,SpeedType.Absolute), new ChainBullet());
          yield return Wait(SHOTDELAY);
        }
        yield return Wait(PHASEDELAY);
      }
    }
  }

  // Shoots bullets in an interrupted circle around the boss that target the player after halting for a brief period
  private class SquareBulletScript : SecretBulletScript
  {
    public class SquareBullet : SecretBullet
    {
      private int goFrames;
      private int waitFrames;
      public SquareBullet(int goFrames = 30, int waitFrames = 60) : base()
      {
        this.waitFrames = waitFrames;
        this.goFrames   = goFrames;
      }

      public override IEnumerator Top()
      {
        AkSoundEngine.PostEvent(SOUND_SHOOT, this.Projectile.gameObject);
        yield return Wait(this.goFrames);
        float initSpeed = this.Speed;
        this.ChangeSpeed(new Speed(0,SpeedType.Absolute));
        yield return Wait(this.waitFrames);
        this.ChangeSpeed(new Speed(initSpeed,SpeedType.Absolute));
        this.ChangeDirection(new Direction(this.DirToNearestPlayer(),DirectionType.Absolute));
        AkSoundEngine.PostEvent(SOUND_SHOOT, this.Projectile.gameObject);
        yield return Wait(120);
        Vanish();
        yield break;
      }
    }

    private const int SIDES        = 5;
    private const int COUNTPERSIDE = 3;
    private const float SPREAD     = 0.5f; // percent of each side filled with bullets
    private const float SPEED      = 25f;
    private const int GOFRAMES     = 15;
    private const int SHOTDELAY    = 4;
    private const int SIDEDELAY    = 8;
    private const float SIDESPAN   = 360.0f / SIDES;
    private const float SPREADSPAN = SPREAD * SIDESPAN;
    private const float OFFSET     = 0.5f * (COUNTPERSIDE - 1);
    private const int FINALDELAY   = ((SHOTDELAY * COUNTPERSIDE) + SIDEDELAY) * SIDES;

    protected override List<FluidBulletInfo> BuildChain()
    {
      return Run(DoTheThing())
      .Finish();
    }

    private IEnumerator DoTheThing()
    {
      float initAngle = Lazy.RandomAngle();
      for (int i = 0; i < SIDES; ++i)
      {
        float sideAngle = (initAngle + i * SIDESPAN).Clamp180();
        for (int j = 0; j < COUNTPERSIDE; ++j)
        {
          float launchAngle = sideAngle + SPREADSPAN * ((1f+j) / (1f+COUNTPERSIDE) - 0.5f);
          this.Fire(Offset.OverridePosition(theBoss.sprite.WorldCenter), new Direction(launchAngle.Clamp180(),DirectionType.Absolute), new Speed(SPEED,SpeedType.Absolute), new SquareBullet(GOFRAMES, FINALDELAY));
          yield return this.Wait(SHOTDELAY);
        }
        yield return this.Wait(SIDEDELAY);
      }
      yield return this.Wait(60);
      yield break;
    }
  }

  // Shoots bullets that form a decelerating wall in front of the player then launch forward
  private class HesitantBulletWallScript : SecretBulletScript
  {
    public class HesitantBullet : SecretBullet
    {

      private int waitFrames;
      public HesitantBullet(int waitFrames = 60) : base()
      {
        this.waitFrames = waitFrames;
      }

      public override IEnumerator Top()
      {
        // AkSoundEngine.PostEvent("megalo_pause", GameManager.Instance.DungeonMusicController.gameObject);
        AkSoundEngine.PostEvent(SOUND_SPAWN, this.Projectile.gameObject);
        float initSpeed = this.Speed;
        this.ChangeSpeed(new Speed(0,SpeedType.Absolute),waitFrames);
        yield return Wait(waitFrames);
        this.ChangeSpeed(new Speed(initSpeed*2,SpeedType.Absolute));
        AkSoundEngine.PostEvent(SOUND_SHOOT, this.Projectile.gameObject);
        // AkSoundEngine.PostEvent("megalo_resume", GameManager.Instance.DungeonMusicController.gameObject);
        yield return Wait(120);
        Vanish();
        yield break;
      }
    }

    private const int COUNT       = 10;
    private const int WAIT        = 60;
    private const int SPAWN_DELAY = 5;
    private const float WALLWIDTH = 10f;
    private const float DISTANCE  = 7f;
    private const float SPEED     = 10f;
    private const float SPACING   = WALLWIDTH / COUNT;

    protected override List<FluidBulletInfo> BuildChain()
    {
      return Run(DoTheThing())
      .Finish();
    }

    private IEnumerator DoTheThing()
    {
      Vector2 incidentDirection, centerPoint, playerpos = GameManager.Instance.PrimaryPlayer.CenterPosition;
      do
      {
        incidentDirection = Lazy.RandomAngle().ToVector();
        centerPoint = playerpos + DISTANCE * incidentDirection;
      } while(IsPointInTile(centerPoint));
      List<Vector2> points = centerPoint.TangentLine(playerpos,WALLWIDTH).SampleUniform(COUNT);
      Direction towardsPlayerDirection = new Direction((-incidentDirection).ToAngle().Clamp180(),DirectionType.Absolute);
      foreach (Vector2 spawnPoint in points)
      {
        this.Fire(Offset.OverridePosition(spawnPoint), towardsPlayerDirection, new Speed(SPEED,SpeedType.Absolute), new HesitantBullet(WAIT));
        yield return this.Wait(SPAWN_DELAY);
      }
      yield break;
    }
  }

  // Shoots bullets that orbit the player closely then scatter
  private class OrbitBulletScript : SecretBulletScript
  {

    public class OrbitBullet : SecretBullet
    {
      private Vector2 center;
      private float radius;
      private float captureAngle;
      private float framesToApproach;
      private float degreesToOrbit;
      private float framesToOrbit;
      private int delay;

      private Vector2 initialTarget;
      private Vector2 delta;

      private const float SPEED = 60f;
      private const int   DELAY = 60;

      public OrbitBullet(Vector2 center, float radius, float captureAngle, float framesToApproach, float degreesToOrbit, float framesToOrbit, int delay)
        : base()
      {
        this.center           = center;
        this.radius           = radius;
        this.captureAngle     = captureAngle;
        this.framesToApproach = framesToApproach;
        this.degreesToOrbit   = degreesToOrbit;
        this.framesToOrbit    = framesToOrbit;
        this.delay            = delay;
      }

      public override void Initialize()
      {
        base.Initialize();

        this.Projectile.BulletScriptSettings.surviveTileCollisions = true;
        this.Projectile.specRigidbody.OnPreTileCollision += (_,_,_,_) => { PhysicsEngine.SkipCollision = true; };

        this.initialTarget = this.center + this.radius * this.captureAngle.ToVector();
        this.delta = this.initialTarget - this.Position;
        ChangeDirection(new Direction(delta.ToAngle(), DirectionType.Absolute));
        ChangeSpeed(new Speed(0, SpeedType.Absolute));
      }

      public override IEnumerator Top()
      {
        yield return Wait(this.delay);
        ChangeSpeed(new Speed(SPEED * delta.magnitude / (framesToApproach+1), SpeedType.Absolute));

        yield return Wait(framesToApproach);
        float degreesPerFrame = degreesToOrbit / framesToOrbit;
        float curAngle = captureAngle;
        float oldSpeed = this.Speed;
        this.UpdatePosition();
        ChangeSpeed(new Speed(0f,SpeedType.Absolute));
        this.UpdateVelocity();
        for (int i = 0; i < framesToOrbit; ++i)
        {
          yield return Wait(1);
          curAngle = (curAngle+degreesPerFrame).Clamp180();
          Vector2 newTarget = center + radius * curAngle.ToVector();
          ChangeDirection(new Direction((newTarget-this.Position).ToAngle(),DirectionType.Absolute));
          this.Position = newTarget;
        }
        AkSoundEngine.PostEvent(SOUND_SHOOT, this.Projectile.gameObject);
        ChangeSpeed(new Speed(oldSpeed,SpeedType.Absolute));
        yield return Wait(DELAY);

        Vanish();
      }
    }

    private const float ROTATIONS       = 5.0f;
    private const int   COUNT           = 37;
    private const float OUTER_RADIUS    = 8f;
    private const float INNER_RADIUS    = 1f;
    private const int   SPAWN_GAP       = 2;
    private const float SPIRAL          = 1.0f;  // higher spiral factor = bullets form a spiral instead of a circle
    private const float ANGLE_DELTA     = ROTATIONS * 360.0f / COUNT;
    private const float APPROACH_FRAMES = 12f;
    private const float ORBIT_FRAMES    = 60f;

    protected override List<FluidBulletInfo> BuildChain()
    {
      return Run(DoTheThing())
      .Finish();
    }

    private IEnumerator DoTheThing()
    {
      Vector2 playerpos = GameManager.Instance.PrimaryPlayer.CenterPosition;
      for (int j = 0; j < COUNT; j++)
      {
        if (j % 2 == 0)
          AkSoundEngine.PostEvent(SOUND_SPAWN, theBoss.gameObject);
        yield return this.Wait(SPAWN_GAP);
        float realAngle    = (j*ANGLE_DELTA).Clamp180();
        float targetRadius = INNER_RADIUS+(j*SPIRAL/COUNT);
        Vector2 spawnPoint = playerpos + (targetRadius * realAngle.ToVector()) + (OUTER_RADIUS * (realAngle-90f).ToVector());
        Bullet b           = new OrbitBullet(playerpos, targetRadius, realAngle, APPROACH_FRAMES, 360f, ORBIT_FRAMES, SPAWN_GAP*(COUNT-j));
        this.Fire(Offset.OverridePosition(spawnPoint), b);
      }
      yield break;
    }
  }

  // Shoots bullets from the ceiling / floor of the current room
  private class CeilingBulletsScript : SecretBulletScript
  {
    private const int COUNT       = 16;
    private const int SPAWN_DELAY = 4;

    protected override List<FluidBulletInfo> BuildChain()
    {
      bool flip = Lazy.CoinFlip();

      return
        Run(Laugh(10))
          .And(DoTheThing(15, warn: true, reverse:  flip)                                   )
          .And(DoTheThing(15, warn: true, reverse: !flip), withDelay:      SPAWN_DELAY*COUNT)
          .And(DoTheThing(30,             reverse:  flip), withDelay: 10                    )
          .And(DoTheThing(30,             reverse: !flip), withDelay: 10 + SPAWN_DELAY*COUNT)
        .Then(Laugh(10))
          .And(DoTheThing(15, warn: true, reverse:  flip)                                   )
          .And(DoTheThing(15, warn: true, reverse: !flip), withDelay:      SPAWN_DELAY*COUNT)
          .And(DoTheThing(45,             reverse:  flip), withDelay: 20                    )
          .And(DoTheThing(45,             reverse: !flip), withDelay: 20 + SPAWN_DELAY*COUNT)
        .Finish();
    }

    private IEnumerator Laugh(float delay)
    {
      AkSoundEngine.PostEvent("sans_laugh", theBoss.gameObject);
      yield return this.Wait(delay);
    }

    private IEnumerator DoTheThing(float speed, bool reverse = false, bool warn = false)
    {
      float offset         = base.roomBulletBounds.width / (float)COUNT;
      float angle          = reverse ? 90f : -90f;
      List<Vector2> points = new List<Vector2>();
      for (float j = (reverse ? 0.5f : 0); j < COUNT; j++)
        points.Add(new Vector2(base.roomBulletBounds.xMin + j*offset, reverse ? base.roomBulletBounds.yMin : base.roomBulletBounds.yMax));

      for(int i = 0; i < points.Count; ++i)
      {
        if (warn)
        {
          DoomZone(points[i], points[i].RaycastToWall(angle, base.roomBulletBounds), 1f, COUNT / 15.0f, 20);
          if (i % 2 == 0)
            AkSoundEngine.PostEvent(SOUND_SPAWN_QUIET, GameManager.Instance.PrimaryPlayer.gameObject);
        }
        yield return this.Wait(SPAWN_DELAY);
      }
      for(int i = 0; i < points.Count; ++i)
      {
        this.Fire(Offset.OverridePosition(points[i]), new Direction(angle, DirectionType.Absolute), new Speed(speed), new SecretBullet());
        if (i % 2 == 1)
          AkSoundEngine.PostEvent(SOUND_SHOOT, GameManager.Instance.PrimaryPlayer.gameObject);
        yield return this.Wait(SPAWN_DELAY);
      }
      yield break;
    }
  }

  // Teleport around the room with particle effects and sound effects for teleporting in and out
  private class CustomTeleportBehavior : TeleportBehavior
  {
    private bool    teleported = false;
    private Vector2 oldPos     = Vector2.zero;
    private Vector2 newPos     = Vector2.zero;
    public override ContinuousBehaviorResult ContinuousUpdate()
    {
      if (State == TeleportState.TeleportOut)
      {
        if (!teleported)
        {
          AkSoundEngine.PostEvent(SOUND_TELEPORT, base.m_aiActor.gameObject);
          oldPos = base.m_aiActor.Position.XY();
        }
        teleported = true;
      }
      else if (State == TeleportState.TeleportIn)
      {
        if (teleported)
        {
          AkSoundEngine.PostEvent(SOUND_TELEPORT, base.m_aiActor.gameObject);
          newPos = base.m_aiActor.Position.XY();
          Vector2 delta = (newPos-oldPos);
          for(int i = 0; i < 10; ++i)
            SpawnDust(oldPos + (i/10.0f) * delta + Lazy.RandomVector(UnityEngine.Random.Range(0.3f,1.25f)));
        }
        teleported = false;
      }
      return base.ContinuousUpdate();
    }
  }
}

}
