namespace CwaffingTheGungy;

using static CwaffingTheGungy.RopeSim; // StretchPolicy

public class Nightlighter : CwaffGun
{
    public static string ItemName         = "Nightlighter";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    internal static tk2dSpriteAnimationClip _LightString = null;

    public static void Init()
    {
        Lazy.SetupGun<Nightlighter>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.B, gunClass: GunClass.SILLY, reloadTime: 1.5f, ammo: 200, shootFps: 14, smoothReload: 0.1f,
            fireAudio: "chain_launch_sound")
          .AssignGun(out Gun gun)
          .LoopAnimation(gun.emptyAnimation, 7)
          .InitProjectile(GunData.New(clipSize: 10, cooldown: 0.4f, shootStyle: ShootStyle.SemiAutomatic, invisibleProjectile: true,
            damage: 10.0f, speed: 75f, range: 30f, force: 12f, pierceBreakables: true, hitSound: "chain_impact_sound_b", customClip: true))
          .Attach<LightStringDoer>();

        _LightString = VFX.Create("nightlighter_light_string", fps: 12).DefaultAnimation();
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        if (projectile.gameObject.GetComponent<LightStringDoer>() is LightStringDoer lsd)
          lsd.Setup(this);
    }
}

public class LightStringDoer : MonoBehaviour
{
  private bool _setup = false;

  public void Setup(Nightlighter gun)
  {
    if (base.gameObject.GetComponent<Projectile>() is Projectile proj)
      new GameObject("lightstring").AddComponent<LightString>().Setup(gun ? gun.PlayerOwner : null, proj, gun);
    this._setup = true;
  }

  private void Start()
  {
    if (!this._setup)
      Setup(null);
  }
}

public class LightString : MonoBehaviour
{
    private const int SEGMENTS         = 40;
    private const float SEGLENGTH      = 0.25f;
    private const float ROPELENGTH     = SEGMENTS * SEGLENGTH;
    private const float SQR_ROPELENGTH = ROPELENGTH * ROPELENGTH;

    private PlayerController _owner = null;
    private Nightlighter _gun = null;
    private Projectile _projectile = null;
    private AIActor _enemy = null;
    private SpeculativeRigidbody _enemyBody = null;
    private CwaffRopeMesh _mesh = null;
    private bool _connectedToGun = false;
    private bool _connectedToProjectile = false;
    private bool _connectedToEnemy = false;
    private bool _setGlowiness = false;
    private bool _setup = false;
    private bool _active = false;

    private static List<LightString> _ExtantChainsOnFloor = new();

    private void Start()
    {
      _ExtantChainsOnFloor.Add(this);
    }

    public bool AttachedToGun => this._active && this._connectedToGun && this._gun != null;

    public void Setup(PlayerController owner, Projectile proj, Nightlighter gun)
    {
      CwaffEvents.OnFloorEnded -= CleanUpExtantChains;
      CwaffEvents.OnFloorEnded += CleanUpExtantChains;
      if (!proj)
      {
        UnityEngine.Object.Destroy(base.gameObject);
        return;
      }
      this._connectedToProjectile = true;
      this._projectile            = proj;
      proj.OnHitEnemy += this.OnHitEnemy;

      this._owner                 = owner;
      this._gun                   = gun;
      this._connectedToGun        = gun != null;
      Vector2 endPos              = gun ? gun.gun.barrelOffset.transform.position : proj.SafeCenter;
      this._mesh                  = CwaffRopeMesh.Create(
        animation: Nightlighter._LightString, startPos: endPos, endPos: endPos, numSegments: SEGMENTS, segLength: SEGLENGTH,
        stretchPolicy: StretchPolicy.GROWTEMPORARY);
      this._mesh.sprite.HeightOffGround = -10f; // draw behind most things
      // this._mesh.sprite.SetGlowiness(1f/*, glowColorPower: 1.55f*/);
      this._active                = true;
      this._setup                 = true;
    }

    private static void CleanUpExtantChains()
    {
      for (int i = _ExtantChainsOnFloor.Count - 1; i >= 0; --i)
        if (_ExtantChainsOnFloor[i])
          UnityEngine.Object.Destroy(_ExtantChainsOnFloor[i].gameObject);
      _ExtantChainsOnFloor.Clear();
    }

    private void OnHitEnemy(Projectile projectile, SpeculativeRigidbody rigidbody, bool arg3)
    {
        projectile.OnHitEnemy -= this.OnHitEnemy;
        if (!this || this._connectedToEnemy || !rigidbody)
          return;
        if (this._gun && this._connectedToGun && rigidbody.aiActor && TryChainLinkEnemy(rigidbody.aiActor))
          projectile.DieInAir();
    }

    private bool TryChainLinkEnemy(AIActor enemy)
    {
      if (!enemy || enemy.healthHaver is not HealthHaver hh)
        return false;
      if (enemy.specRigidbody is not SpeculativeRigidbody body)
        return false;
      this._connectedToProjectile = false;
      this._projectile = null;

      this._connectedToEnemy = true;
      this._enemy = enemy;
      this._enemyBody = body;
      this._mesh.endPos = enemy.CenterPosition;
      base.gameObject.Play("chain_shackle_sound"); // TODO: use different sound
      return true;
    }

    private void LateUpdate()
    {
      const float DESPAWN_RADIUS = 30.0f;
      const float DESPAWN_RADIUS_SQR = DESPAWN_RADIUS * DESPAWN_RADIUS;

      if (!this._setGlowiness)
      {
        this._mesh.sprite.SetGlowiness(50f, glowColor: Color.white, glowColorPower: 10f);
        this._setGlowiness = true;
      }

      if (!this._active)
      {
        // despawn if the player has moved far enough away
        PlayerController nearestPlayer = this._owner;
        if (!nearestPlayer)
          nearestPlayer = GameManager.Instance.BestActivePlayer;
        bool despawn = nearestPlayer == null || this._mesh == null;
        if (!despawn)
        {
          Vector2 ppos = nearestPlayer.CenterPosition;
          if ((ppos - this._mesh.startPos).sqrMagnitude >= DESPAWN_RADIUS_SQR)
            if ((ppos - this._mesh.endPos).sqrMagnitude >= DESPAWN_RADIUS_SQR)
              despawn = true;
        }
        if (despawn)
          UnityEngine.Object.Destroy(base.gameObject);
        return;
      }
      if (!this._setup || BraveTime.DeltaTime == 0.0f || GameManager.Instance.IsPaused)
        return;
      if (this._connectedToGun && (!this._owner || !this._gun || !this._gun.gun || this._owner.CurrentGun != this._gun.gun))
      {
        Disconnect();
        return;
      }
      if (this._connectedToProjectile && !this._projectile)
      {
        Disconnect();
        return;
      }
      if (this._connectedToEnemy && (!this._enemy || !this._enemyBody || (this._enemy.healthHaver is HealthHaver hh && hh.IsDead)))
      {
        Disconnect();
        return;
      }

      // always update start position if we're connected to a gun
      if (this._gun)
        this._mesh.startPos = this._gun.gun.barrelOffset.transform.position;

      // if the other end is connected to a projectile, follow it
      if (this._projectile)
      {
        this._mesh.endPos = this._projectile.SafeCenter;
        return;
      }

      // if we have no projectile and we have nothing else to connect to, we're done
      if (!this._gun || !this._enemy || !this._enemyBody)
      {
          Disconnect(); // nothing else to do
          return;
      }

      // update connected enemy
      BraveInput currentInput = BraveInput.GetInstanceForPlayer(this._owner.PlayerIDX);
      Vector2 targetPos = (currentInput == null || this._owner.IsKeyboardAndMouse())
        ? this._owner.unadjustedAimPoint.XY()
        : this._owner.CenterPosition + 20f * currentInput.ActiveActions.Aim.Vector;
      this._mesh.endPos = this._enemyBody.UnitCenter;
    }

    internal void Disconnect(bool manual = false)
    {
      DeregisterEvents();
      this._connectedToEnemy      = false;
      this._enemy                 = null;
      this._gun                   = null;
      this._connectedToGun        = false;
      this._projectile            = null;
      this._connectedToProjectile = false;
      // base.gameObject.Play("chain_snap_sound"); // TODO: different sound
      if (this._mesh)
        this._mesh.LockWhenStationary(keepAnimating: true); // prevent the chain from updating once it stops moving around much
      this._active = false;
    }

    private void DeregisterEvents()
    {
      if (this._projectile)
        this._projectile.OnHitEnemy -= this.OnHitEnemy;
    }

    private void OnDestroy()
    {
      DeregisterEvents();
      if (this._mesh)
        UnityEngine.Object.Destroy(this._mesh.gameObject);
    }
}
