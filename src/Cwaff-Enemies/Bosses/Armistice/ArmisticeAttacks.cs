
namespace CwaffingTheGungy;

public partial class ArmisticeBoss : AIActor
{
  private const string SOUND_SPAWN       = "no_sound"; // "Play_OBJ_turret_set_01";
  private const string SOUND_SPAWN_QUIET = "no_sound"; // "undertale_pullback";
  private const string SOUND_SHOOT       = "no_sound"; // "Play_WPN_spacerifle_shot_01";
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
        Material mat = this.Projectile.sprite.renderer.material;
        mat.SetFloat(CwaffVFX._EmissivePowerId, 10f);
        mat.SetFloat(CwaffVFX._EmissiveColorPowerId, 1.5f);
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

    private PathRect slamBoundsPath;

    protected override List<FluidBulletInfo> BuildChain()
    {
      this.slamBoundsPath = new PathRect(base.roomSlamBounds);

      return
         Run(SprayBullets(new Vector2(0.5f, 1f)))
        .And(SprayBullets(new Vector2(-0.5f, 1f)))
        .Finish();
    }

    private IEnumerator SprayBullets(Vector2 startDir)
    {
      Vector2 target = this.slamBoundsPath.At(0.5f, 0.2f); // near bottom of room

      Vector2 gravity = new Vector2(0, -GRAVITY);
      Speed s = new Speed(BASESPEED, SpeedType.Absolute);
      Offset o = Offset.OverridePosition(theBoss.CenterPosition);
      theBoss.gameObject.Play("sans_laugh");
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

}
