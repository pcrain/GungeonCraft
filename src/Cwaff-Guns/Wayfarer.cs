namespace CwaffingTheGungy;

public class Wayfarer : CwaffGun
{
    public static string ItemName         = "Wayfarer";
    public static string ShortDescription = "Search and Destroy";
    public static string LongDescription  = "Launches a high velocity drone projectile that pierces enemies and sticks to walls. Attempting to fire while the drone is stuck to a wall will relaunch the drone towards the cursor without consuming additional ammo. The drone is destroyed upon reloading, colliding with certain objects, or exiting the current room. Only one drone can be deployed at any given time. Guns cannot be changed while a drone is active.";
    public static string Lore             = "Developed as a secret research project coincidentally timed around the invention of sticky notes, this weapons grants its wielder unprecedented control over the trajectory of its projectiles. Once released to the general public, it quickly became apparent this level of control was often both unnecessary and disorienting. While it never reached mainstream popularity, it did find a niche use among the wealthy as an excellent car key locator.";

    private const string _WAYFARER_OVERRIDE = "Wayfarer Gunlock";

    private Projectile _extantProjectile = null;
    private PlayerController _prevOwner = null;
    private OverrideLerper _lerpyboi = null;

    public static void Init()
    {
        Lazy.SetupGun<Wayfarer>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.A, gunClass: GunClass.PISTOL, reloadTime: 0.0f, ammo: 60, shootFps: 14, reloadFps: 4,
            muzzleFrom: Items.Mailbox, fireAudio: "wayfarer_launch_sound")
          .AddToShop(ItemBuilder.ShopType.Goopton)
          .InitProjectile(GunData.New(sprite: "wayfarer_projectile", scale: 0.9f, clipSize: 1, cooldown: 0.18f, shootStyle: ShootStyle.SemiAutomatic,
            damage: 30.0f, speed: 70f, range: 1000f, force: 12f, hitSound: "wayfarer_impact_sound", customClip: true,
            pierceBreakables: true, anchorsChangeColliders: false, overrideColliderPixelSizes: new IntVector2(2, 2)))
          .SetAllImpactVFX(Items.Blooper.AsGun().DefaultModule.projectiles[0].hitEffects.enemy)
          .Attach<PierceProjModifier>(pierce => { pierce.penetration = 10000; pierce.penetratesBreakables = true; })
          .Attach<WayfarerProjectile>()
          .AttachTrail("wayfarer_trail", fps: 30, cascadeTimer: C.FRAME, softMaxLength: 1f);
    }

    private void OnTriedToInitiateAttack(PlayerController player)
    {
        if (!player || player.CurrentGun != this.gun)
            return; // inactive, do normal firing stuff
        if (this._extantProjectile)
        {
          float aimAngle = player.IsKeyboardAndMouse()
            ? (player.unadjustedAimPoint.XY() - this._extantProjectile.SafeCenter).ToAngle()
            : this.gun.gunAngle;
          this._extantProjectile.gameObject.GetComponent<WayfarerProjectile>().Redirect(aimAngle);
          player.SuppressThisClick = true;
        }
    }

    public override void OnPlayerPickup(PlayerController player)
    {
        base.OnPlayerPickup(player);
        player.OnTriedToInitiateAttack += this.OnTriedToInitiateAttack;
        this._lerpyboi = player.gameObject.GetOrAddComponent<OverrideLerper>();
    }

    public override void OnDroppedByPlayer(PlayerController player)
    {
        base.OnDroppedByPlayer(player);
        CleanupWayfarer();
        player.OnTriedToInitiateAttack -= this.OnTriedToInitiateAttack;
    }

    public override void OnReloadPressed(PlayerController player, Gun gun, bool manualReload)
    {
        base.OnReloadPressed(player, gun, manualReload);
        if (manualReload)
          CleanupWayfarer();
    }

    public override void OnSwitchedAwayFromThisGun()
    {
        base.OnSwitchedAwayFromThisGun();
        CleanupWayfarer();
    }

    private void CleanupWayfarer()
    {
      if (this.PlayerOwner)
        this.PlayerOwner.OverrideCursorCenter(null);
      if (this._lerpyboi)
        this._lerpyboi.Deactivate();
      if (!this._extantProjectile)
        return;
      if (this.Mastered)
        this._extantProjectile.gameObject.GetComponent<WayfarerProjectile>().MakeAutonomous();
      else
        this._extantProjectile.DieInAir();
      this._extantProjectile = null;
    }

    public override void OnDestroy()
    {
        if (this.PlayerOwner)
            this.PlayerOwner.OnTriedToInitiateAttack -= this.OnTriedToInitiateAttack;
        CleanupWayfarer();
        base.OnDestroy();
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        if (this.PlayerOwner is not PlayerController player)
            return;

        if (this._extantProjectile)
          projectile.DieInAir();  // effectively disable scattershot and similar effects
        else
          this._extantProjectile = projectile;
    }

    public override void Update()
    {
        base.Update();
        CameraController cc = GameManager.Instance.MainCameraController;
        if (this.PlayerOwner is not PlayerController player/* || !player.AcceptingNonMotionInput*/)
        {
          if (this._prevOwner)
          {
            if (this._extantProjectile)
            {
              this._extantProjectile.DieInAir();
              this._extantProjectile = null;
            }
            if (this._prevOwner.IsPrimaryPlayer)
              cc.UseOverridePlayerOnePosition = false;
            else
              cc.UseOverridePlayerTwoPosition = false;
          }
          return;
        }
        this._prevOwner = player;

        if (this._extantProjectile)
        {
          player.inventory.GunLocked.SetOverride(_WAYFARER_OVERRIDE, true);
          this.gun.CanBeDropped = false;
          Vector2 curPos = this._extantProjectile.SafeCenter;
          if (curPos != Vector2.zero)
          {
            this._lerpyboi.Recenter(0.5f * (player.CenterPosition + curPos), lerpFactor: 2f);
            player.OverrideCursorCenter(curPos);
          }
        }
        else
        {
          player.inventory.GunLocked.SetOverride(_WAYFARER_OVERRIDE, false);
          this.gun.CanBeDropped = true;
          this._lerpyboi.Deactivate();
          player.OverrideCursorCenter(null);
        }
    }
}

public class OverrideLerper : MonoBehaviour
{
  private bool _active = false;
  private PlayerController _player = null;
  private CameraController _cc = null;

  private void Setup()
  {
    if (!this._player)
      this._player = base.gameObject.GetComponent<PlayerController>();
    if (!this._cc)
      this._cc = GameManager.Instance.MainCameraController;
  }

  public void Recenter(Vector2 pos, float lerpFactor = 0f)
  {
    Setup();
    this._active = true;
    Vector2 finalPos = pos;
    if (this._player.IsPrimaryPlayer)
    {
      if (lerpFactor == 0)
        this._cc.OverridePlayerOnePosition = pos;
      else if (this._cc.UseOverridePlayerOnePosition)
        this._cc.OverridePlayerOnePosition = Lazy.SmoothestLerp(this._cc.OverridePlayerOnePosition, pos, lerpFactor);
      else
        this._cc.OverridePlayerOnePosition = Lazy.SmoothestLerp(this._player.CenterPosition, pos, lerpFactor);
      this._cc.UseOverridePlayerOnePosition = true;
    }
    else
    {
      if (lerpFactor == 0)
        this._cc.OverridePlayerTwoPosition = pos;
      else if (this._cc.UseOverridePlayerTwoPosition)
        this._cc.OverridePlayerTwoPosition = Lazy.SmoothestLerp(this._cc.OverridePlayerTwoPosition, pos, lerpFactor);
      else
        this._cc.OverridePlayerTwoPosition = Lazy.SmoothestLerp(this._player.CenterPosition, pos, lerpFactor);
      this._cc.UseOverridePlayerTwoPosition = true;
    }
  }

  public void Deactivate()
  {
    Setup();
    this._active = false;
  }

  private void Update()
  {
    const float INACTIVE_LERP_FACTOR = 5f;
    if (this._active || !this._player)
      return;
    if (this._player.IsPrimaryPlayer && this._cc.UseOverridePlayerOnePosition)
    {
      Vector2 newOverridePos = Lazy.SmoothestLerp(this._cc.OverridePlayerOnePosition, this._player.CenterPosition, INACTIVE_LERP_FACTOR);
      this._cc.OverridePlayerOnePosition = newOverridePos;
      if ((this._player.CenterPosition - newOverridePos).sqrMagnitude < 1/256f)
        this._cc.UseOverridePlayerOnePosition = false;
    }
    else if (!this._player.IsPrimaryPlayer && this._cc.UseOverridePlayerTwoPosition)
    {
      Vector2 newOverridePos = Lazy.SmoothestLerp(this._cc.OverridePlayerTwoPosition, this._player.CenterPosition, INACTIVE_LERP_FACTOR);
      this._cc.OverridePlayerTwoPosition = newOverridePos;
      if ((this._player.CenterPosition - newOverridePos).sqrMagnitude < 1/256f)
        this._cc.UseOverridePlayerTwoPosition = false;
    }
  }
}

public class WayfarerProjectile : MonoBehaviour
{
    private const float _MAX_DUPE_COLLISIONS = 5;
    private const float _AUTO_FIRE_RATE = 0.75f;
    private const float _MAX_ANGLE_DEV = 88f;

    private Projectile _projectile;
    private PlayerController _owner;
    private bool stationary = false;
    private Vector2 normal = default;
    private float normalAngle = default;
    private float prevSpeed = 0.0f;
    private SpeculativeRigidbody _lastCollisionBody = null;
    private int _duplicateCollisions = 0;
    private bool _autonomous = false;
    private Geometry _pingRing = null;
    private float _pingTimer = 0.0f;
    private float _shootTimer = 0.0f;

    public bool Autonomous {
      get { return this._autonomous; }
      private set { this._autonomous = value; }
    }

    private void Start()
    {
        this._projectile = base.GetComponent<Projectile>();
        this._projectile.sprite.usesOverrideMaterial = true;
        this._projectile.sprite.renderer.material.shader = ShaderCache.Acquire("Brave/PlayerShader");
        SpriteOutlineManager.AddOutlineToSprite(this._projectile.sprite, Color.black);
        this._owner = this._projectile.Owner as PlayerController;
        if (!this._owner)
          return;

        this._projectile.BulletScriptSettings.surviveTileCollisions = true;
        this._projectile.specRigidbody.OnTileCollision += this.OnTileCollision;
        this._projectile.specRigidbody.OnRigidbodyCollision += this.OnRigidBodyCollision;
        this._pingRing = new GameObject().AddComponent<Geometry>();
    }

    public void MakeAutonomous() {
      this._autonomous = true;
      base.gameObject.Play("wayfarer_autonomize_sound");
    }

    private void OnRigidBodyCollision(CollisionData rigidbodyCollision)
    {
      if (rigidbodyCollision.OtherRigidbody == this._lastCollisionBody)
      {
        if ((++this._duplicateCollisions) >= _MAX_DUPE_COLLISIONS)
        {
          Lazy.DebugLog($"wayfarer drone got stuck in a wall ):");
          this._projectile.DieInAir();
          return;
        }
      }
      else
        _duplicateCollisions = 0;
      this._lastCollisionBody = rigidbodyCollision.OtherRigidbody;
    }

    private void OnTileCollision(CollisionData tileCollision)
    {
      this.stationary = true;
      this._projectile.shouldRotate = false;
      this.normal = tileCollision.Normal;
      this.normalAngle = this.normal.ToAngle();
      this.prevSpeed = this._projectile.baseData.speed;
      this._projectile.m_usesNormalMoveRegardless = true; // temporarily disable Helix Projectile shenanigans
      this._projectile.specRigidbody.PullOutOfWall(this.normal.ToIntVector2(), forceAtLeastOne: true);
      this._projectile.SetSpeed(0f);
      this._projectile.specRigidbody.CollideWithTileMap = false;
      this._projectile.specRigidbody.CollideWithOthers = false;
      this._projectile.specRigidbody.Reinitialize();
      this._projectile.ResetPiercing();
      PhysicsEngine.PostSliceVelocity = Vector2.zero;
    }

    public void Redirect(float angle)
    {
      if (!this.stationary)
        return;
      if (angle.AbsAngleTo(this.normalAngle) > _MAX_ANGLE_DEV)
        return; // disallow shooting into wall

      this.stationary = false;
      this._projectile.shouldRotate = true;
      this._projectile.m_usesNormalMoveRegardless = false; // reenable Helix Projectile shenanigans
      this._projectile.SetSpeed(this.prevSpeed);
      this._projectile.SendInDirection(angle.ToVector(), true, true);
      this._projectile.specRigidbody.CollideWithTileMap = true;
      this._projectile.specRigidbody.CollideWithOthers = true;
      this._projectile.specRigidbody.Reinitialize();
      base.gameObject.Play("wayfarer_relaunch_sound");
    }

    private void Update()
    {
      if (!this._owner || this._owner.CurrentRoom == null || this._owner.CurrentRoom != base.transform.position.GetAbsoluteRoom())
      {
        this._projectile.DieInAir();
        return;
      }

      HandlePing();

      if (!this.stationary)
        return;

      if (!this._autonomous)
      {
        this._projectile.transform.rotation = this._owner.IsKeyboardAndMouse()
            ? (this._owner.unadjustedAimPoint.XY() - this._projectile.SafeCenter).EulerZ()
            : this._owner.m_currentGunAngle.EulerZ();
        return;
      }

      if ((this._shootTimer += BraveTime.DeltaTime) < _AUTO_FIRE_RATE)
          return;

      //NOTE: we might have a bad wall normal, so check in all 4 cardinal directions
      Vector2 scanCenter = this._projectile.sprite.WorldCenter;
      Vector2 scanPoint = default;
      Vector2 enemyPos = default;
      bool foundEnemy = false;
      for (int i = 0; i < 4; ++i)
      {
        Vector2 tempNormal = this.normal.Rotate(90f * i);
        scanPoint = this._projectile.sprite.WorldCenter + tempNormal;
        if (Lazy.NearestEnemyPos(scanPoint) is not Vector2 tempEnemyPos)
            continue;
        enemyPos = tempEnemyPos;
        this.normal = tempNormal;
        this.normalAngle = this.normal.ToAngle();
        foundEnemy = true;
        break;
      }
      if (!foundEnemy)
        return;

      float angleToEnemy = (enemyPos - scanPoint).ToAngle();
      Quaternion rot = angleToEnemy.EulerZ();
      this._projectile.transform.rotation = rot;
      Redirect(angleToEnemy);
      this._shootTimer = 0f;
    }

    private void HandlePing()
    {
      const float MAX_RING_DIST  = 2f;
      const float MAX_TIME       = Mathf.PI * 2f;
      const float RING_THICKNESS = 0.1f;
      const float PING_SPEED     = 6f;

      this._pingTimer += BraveTime.DeltaTime;
      float time = (PING_SPEED * this._pingTimer) % MAX_TIME;
      float percentDone = time / MAX_TIME;
      float dist = percentDone * MAX_RING_DIST;
      float alpha = Mathf.Min(percentDone, 1f - percentDone);
      this._pingRing.Setup(Geometry.Shape.RING,
        color: (this._autonomous ? Color.red : Color.green).WithAlpha(alpha),
        pos: this._projectile.SafeCenter, radius: dist, radiusInner: Mathf.Max(0f, dist - RING_THICKNESS));
    }

    private void OnDestroy()
    {
      if (this._pingRing)
        UnityEngine.Object.Destroy(this._pingRing.gameObject);
      base.gameObject.Play("wayfarer_destroy_sound");
    }
}

/// <summary>Make the aim cursor draw relative to the projectile as necessary</summary>
[HarmonyPatch]
internal static class AimCursorOverride
{
    private static Vector2? _P1 = null;
    private static Vector2? _P2 = null;

    [HarmonyPatch(typeof(GameCursorController), nameof(GameCursorController.DrawCursor))]
    [HarmonyILManipulator]
    private static void GameCursorControllerDrawCursorIL(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);
        if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt<GameActor>("get_CenterPosition")))
          return;

        cursor.Emit(OpCodes.Ldloc_S, (byte)6); // primaryPlayer
        cursor.CallPrivate(typeof(AimCursorOverride), nameof(AdjustPlayerCursorCenter));

        if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt<GameActor>("get_CenterPosition")))
          return;

        cursor.Emit(OpCodes.Ldloc_S, (byte)14); // secondaryPlayer
        cursor.CallPrivate(typeof(AimCursorOverride), nameof(AdjustPlayerCursorCenter));
    }

    private static Vector2 AdjustPlayerCursorCenter(Vector2 orig, PlayerController player)
    {
      return (player.IsPrimaryPlayer ? _P1 : _P2) ?? orig;
    }

    internal static void OverrideCursorCenter(this PlayerController player, Vector2? overridePos)
    {
      if (player.IsPrimaryPlayer)
        _P1 = overridePos;
      else
        _P2 = overridePos;
    }
}
