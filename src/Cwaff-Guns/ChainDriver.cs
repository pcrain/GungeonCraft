namespace CwaffingTheGungy;

public class ChainDriver : CwaffGun
{
    public static string ItemName         = "Chain Driver";
    public static string ShortDescription = "The Strongest Links";
    public static string LongDescription  = "Fires chains that shackle enemies. Shackled enemies are dragged towards the current aim point, and can be slammed into solid objects. Enemy weight and damage stat influence drag speed. Reloading releases shackled enemies. Increases curse by 1 while in inventory.";
    public static string Lore             = "They say a chain is only as strong as its weakest link. By breaking and reassembling chains millions of times until only the strongest links survived, gunsmiths were able to produce a chain so powerful it developed arcane self-regenerating properties. In typical fashion, they ignored all the potential good an infinite supply of iron could offer, and instead chose to stuff it inside a firearm.";

    internal static GameObject[] _ChainDebris          = new GameObject[2];
    internal static GameObject _ChainImpact            = null;
    internal static tk2dSpriteAnimationClip _ChainLink = null;

    private List<ChainkLink> _attachedLinks            = new();

    public static void Init()
    {
        Lazy.SetupGun<ChainDriver>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.B, gunClass: GunClass.SILLY, reloadTime: 0.0f, ammo: 1, shootFps: 30, reloadFps: 4,
            muzzleFrom: Items.Mailbox, fireAudio: "chain_launch_sound", canGainAmmo: false, suppressReloadLabel: true, curse: 1f)
          .InitProjectile(GunData.New(clipSize: 1, cooldown: 1.0f, shootStyle: ShootStyle.SemiAutomatic, invisibleProjectile: true,
            damage: 5.5f, speed: 75f, range: 18f, force: 12f, pierceBreakables: true, hitSound: "chain_impact_sound_b", customClip: true))
          .Attach<ChainLinkDoer>();

        _ChainLink   = VFX.Create("chain_link").DefaultAnimation();
        _ChainImpact = VFX.Create("chain_impact_vfx", fps: 20, loops: false, anchor: Anchor.MiddleLeft);

        for (int i = 0; i < 2; ++i)
            _ChainDebris[i] = BreakableAPIToolbox.GenerateDebrisObject(
                shardSpritePath         : $"chain_debris_{i+1}",
                debrisObjectsCanRotate  : true,
                LifeSpanMin             : 1,
                LifeSpanMax             : 1,
                AngularVelocity         : 0,
                AngularVelocityVariance : 180,
                DebrisBounceCount       : 1).gameObject;
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        if (projectile.gameObject.GetComponent<ChainLinkDoer>() is ChainLinkDoer cdp)
          cdp.Setup(this);
    }

    public override void OnReloadPressed(PlayerController player, Gun gun, bool manualReload)
    {
        base.OnReloadPressed(player, gun, manualReload);
        if (!manualReload)
          return;
        for (int i = this._attachedLinks.Count - 1; i >= 0; --i)
          if (this._attachedLinks[i])
            this._attachedLinks[i].Disconnect(manual: true);
        this._attachedLinks.Clear();
    }

    public override void OnTriedToInitiateAttack(PlayerController player)
    {
        base.OnTriedToInitiateAttack(player);
        if (AnyAttachedChains())
          player.SuppressThisClick = true; // can't fire more than one chain at once
    }

    public override void Update()
    {
        base.Update();
        if (this.gun.ammo == 0 && !AnyAttachedChains())
        {
          this.gun.CurrentAmmo  = 1;
          this.gun.MoveBulletsIntoClip(1);
        }
    }

    public static void DoChainDebrisAt(Vector2 pos, int num)
    {
      for (int i = 0; i < num; ++i)
      {
          DebrisObject debris = UnityEngine.Object.Instantiate(ChainDriver._ChainDebris[UnityEngine.Random.Range(0, 2)], pos, Lazy.RandomEulerZ())
            .GetComponent<DebrisObject>();
          debris.Trigger(Lazy.RandomVector(2f * UnityEngine.Random.value).ToVector3ZUp(2f), 0.25f);
      }
    }

    private bool AnyAttachedChains()
    {
      for (int i = this._attachedLinks.Count - 1; i >= 0; --i)
      {
        if (this._attachedLinks[i] && this._attachedLinks[i].AttachedToGun)
          return true;
        this._attachedLinks.RemoveAt(i);
      }
      return false;
    }

    internal void RegisterChain(ChainkLink chain)
    {
      this._attachedLinks.Add(chain);
    }

    internal void DeregisterChain(ChainkLink chain)
    {
      this._attachedLinks.Remove(chain);
      if (this._attachedLinks.Count > 0)
        return;

      this.gun.CurrentAmmo  = 1;
      this.gun.MoveBulletsIntoClip(1);
    }
}

public class ChainLinkDoer : MonoBehaviour
{
  private Projectile _proj = null;
  private bool _setup = false;

  public void Setup(ChainDriver gun)
  {
    this._proj = base.gameObject.GetComponent<Projectile>();
    if (this._proj)
    {
      this._proj.OnDestruction += this.OnProjectileDestruction;
      new GameObject("chainlink").AddComponent<ChainkLink>().Setup(gun ? gun.PlayerOwner : null, this._proj, gun);
    }
    this._setup = true;
  }

  private void Start()
  {
    if (!this._setup)
      Setup(null);
  }

  private void OnProjectileDestruction(Projectile projectile)
  {
      if (!projectile || projectile != this._proj)
        return;
      this._proj.OnDestruction -= this.OnProjectileDestruction;
      ChainDriver.DoChainDebrisAt(this._proj.SafeCenter, 3);
  }
}

public class ChainkLink : MonoBehaviour
{
    private const int SEGMENTS         = 40;

    private PlayerController _owner = null;
    private ChainDriver _gun = null;
    private Projectile _projectile = null;
    private AIActor _enemy = null;
    private SpeculativeRigidbody _enemyBody = null;
    private CwaffRopeMesh _mesh = null;
    private bool _connectedToGun = false;
    private bool _connectedToProjectile = false;
    private bool _connectedToEnemy = false;
    private bool _setup = false;
    private bool _active = false;
    private int _knockbackId = -1;
    private float _nextSlamTime = 0.0f;
    private float _forceMultiplier = 0.0f;
    private bool _mastered = false;

    private static List<ChainkLink> _ExtantChainsOnFloor = new();

    private void Start()
    {
      _ExtantChainsOnFloor.Add(this);
    }

    public bool AttachedToGun => this._active && this._connectedToGun && this._gun != null;

    public void Setup(PlayerController owner, Projectile proj, ChainDriver gun)
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
      this._forceMultiplier       = owner ? owner.DamageMult() : 1.0f;
      if (owner.HasSynergy(Synergy.CHAIN_SMOKER))
        this._forceMultiplier += 0.1f * Mathf.Max(owner.Coolness(), 0);
      this._gun                   = gun;
      if (this._gun)
      {
        this._gun.RegisterChain(this);
        this._mastered = this._gun.Mastered;
      }
      this._connectedToGun        = gun != null;
      Vector2 endPos              = gun ? gun.gun.barrelOffset.transform.position : proj.SafeCenter;
      this._mesh                  = CwaffRopeMesh.Create(
        animation: ChainDriver._ChainLink, startPos: endPos, endPos: endPos, numSegments: SEGMENTS,
        stretchPolicy: CwaffingTheGungy.RopeSim.StretchPolicy.GROWTEMPORARY);
      this._mesh.sprite.HeightOffGround = -10f; // draw behind most things
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
      if (!enemy || enemy.healthHaver is not HealthHaver hh || hh.IsBoss || hh.IsSubboss)
        return false;
      if (enemy.gameObject.GetComponent<KnockbackUnleasher>())
        return false; // NOTE: this is the only real check if the enemy already has a chain linked to them, maybe we want something better?
      if (enemy.knockbackDoer is not KnockbackDoer kbd || kbd.m_isImmobile.Value)
        return false;
      if (enemy.behaviorSpeculator is not BehaviorSpeculator bs || bs.ImmuneToStun)
        return false;
      if (enemy.specRigidbody is not SpeculativeRigidbody body)
        return false;
      this._connectedToProjectile = false;
      this._projectile = null;

      this._connectedToEnemy = true;
      this._enemy = enemy;
      this._enemy.HitByEnemyBullets = true;
      this._enemyBody = body;
      this._enemyBody.OnTileCollision += this.OnSlammedIntoSolidObject;
      this._enemyBody.OnPreRigidbodyCollision += this.OnPossiblySlammedIntoOtherEnemy;
      this._enemyBody.OnRigidbodyCollision += this.OnSlammedIntoAnything;
      this._enemyBody.AddCollisionLayerOverride(CollisionMask.LayerToMask(CollisionLayer.EnemyHitBox));
      this._enemy.gameObject.AddComponent<KnockbackUnleasher>(); // uncap knockback for this specific enemy while they're chained
      if (this._owner)
        this._enemyBody.RegisterSpecificCollisionException(this._owner.specRigidbody);
      bs.Stun(3600f, true);
      this._mesh.endPos = enemy.CenterPosition;
      base.gameObject.Play("chain_shackle_sound");
      return true;
    }

    private void OnPossiblySlammedIntoOtherEnemy(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
    {
      const float COLLATERAL_DAMAGE_SCALE = 0.25f;
      const float COLLATERAL_KB_SCALE     = 10.0f;
      if (!otherRigidbody || otherRigidbody.aiActor is not AIActor other)
        return;

      PhysicsEngine.SkipCollision = true;
      myRigidbody.RegisterTemporaryCollisionException(otherRigidbody, 0.25f);
      otherRigidbody.RegisterTemporaryCollisionException(myRigidbody, 0.25f);
      if (!other.IsNormalEnemy || other.healthHaver is not HealthHaver hh)
        return;

      float damage = COLLATERAL_DAMAGE_SCALE * myRigidbody.Velocity.magnitude;
      hh.ApplyDamage(damage, myRigidbody.Velocity, "Chain Slam");
      Vector2 kbAngle = myRigidbody.Velocity.ToAngle().AddRandomSpread(15f).ToVector();
      if (hh.IsAlive && other.knockbackDoer is KnockbackDoer kbd)
        kbd.ApplyKnockback(kbAngle, COLLATERAL_KB_SCALE * damage);
      OnSlammedCommon(contact: otherRigidbody.UnitCenter, normal: -kbAngle, tile: false);
    }

    private void OnSlammedIntoAnything(CollisionData collisionData)
    {
      if (collisionData.OtherRigidbody is not SpeculativeRigidbody other)
        return;
      if (other.aiActor is AIActor actor)
        return; // already handled above
      if (other.gameObject.GetComponent<MinorBreakable>() is MinorBreakable breakable && !breakable.IsBroken)
        breakable.Break();
      OnSlammedIntoSolidObject(collisionData);
    }

    private void OnSlammedIntoSolidObject(CollisionData collisionData)
    {
      const float MIN_SLAM_GAP = 0.05f;

      float now = BraveTime.ScaledTimeSinceStartup;
      if (!this._enemyBody || now < this._nextSlamTime || !this._enemy)
      {
          this._nextSlamTime = now + MIN_SLAM_GAP;
          return;
      }
      this._nextSlamTime = now + MIN_SLAM_GAP;
      OnSlammedCommon(contact: collisionData.Contact, normal: collisionData.Normal, tile: true);
    }

    private void OnSlammedCommon(Vector2 contact, Vector2 normal, bool tile)
    {
      const float FORCE_SHAKE_MULT = 0.001f;
      const float DAMAGE_VEL_MULT  = 0.01f;

      float weight = 30f; // default value, though we should always have a KnockbackDoer
      if (this._enemy.knockbackDoer is KnockbackDoer kbd)
        weight = kbd.weight;
      float slamVel = this._enemyBody.Velocity.magnitude;
      float slamForce = FORCE_SHAKE_MULT * (weight * slamVel);
      float slamDamage = DAMAGE_VEL_MULT * slamVel * slamVel;
      if (this._enemy.healthHaver is HealthHaver hh)
        hh.ApplyDamage(slamDamage, -normal, "slammed", CoreDamageTypes.None, DamageCategory.Collision);
      base.gameObject.Play((slamForce > 1) ? "wall_slam_heavy" : (slamForce > 0.5) ? "wall_slam_medium" : "wall_slam_light");
      base.gameObject.Play("chain_snap_sound");
      CwaffVFX.Spawn(
        prefab        : ChainDriver._ChainImpact,
        position      : contact,
        rotation      : normal.ToAngle().EulerZ(),
        velocity      : 2f * normal.normalized,
        emissivePower : 5f,
        emissiveColor : Color.white,
        startScale    : Mathf.Clamp01(slamForce),
        endScale      : Mathf.Clamp01(slamForce)
        );
      // ChainDriver.DoChainDebrisAt(tileCollision.Contact, Mathf.Clamp(Mathf.CeilToInt(10f * slamForce), 4, 20));
      GameManager.Instance.MainCameraController.DoScreenShake(new ScreenShakeSettings(
        mag: slamForce, spd: slamForce, tim: 0.3f * Mathf.Min(slamForce, 1f), foff: 0f), contact);
    }

    private void LateUpdate()
    {
      const float KB_SCALAR = 12.0f;
      const float DESPAWN_RADIUS = 30.0f;
      const float DESPAWN_RADIUS_SQR = DESPAWN_RADIUS * DESPAWN_RADIUS;

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
      if (!this._gun || !this._enemy || !this._enemyBody || this._enemy.knockbackDoer is not KnockbackDoer kbd)
      {
          Disconnect(); // nothing else to do
          return;
      }

      // update connected enemy
      BraveInput currentInput = BraveInput.GetInstanceForPlayer(this._owner.PlayerIDX);
      Vector2 targetPos = (currentInput == null || this._owner.IsKeyboardAndMouse())
        ? this._owner.unadjustedAimPoint.XY()
        : this._owner.CenterPosition + 20f * currentInput.ActiveActions.Aim.Vector;
      Vector2 enemyPos = this._enemyBody.UnitCenter;
      Vector2 tempDelta = (targetPos - enemyPos);
      kbd.m_activeContinuousKnockbacks.Clear(); // nothing else but the chain should affect this enemy
      if ((targetPos - this._mesh.endPos).magnitude >= 0.25f) // move only if the mouse has been moved significantly
      {
          float kbforce = this._forceMultiplier * KB_SCALAR * tempDelta.magnitude;
          if (this._mastered)
            kbforce *= Mathf.Max(kbd.weight / 15f, 1f); // everything is treated as if it has the weight of a Tazie
          this._knockbackId = kbd.ApplyContinuousKnockback(tempDelta, kbforce);
      }
      else
          this._knockbackId = kbd.ApplyContinuousKnockback(tempDelta, 0.0f);
      this._mesh.endPos = enemyPos;
    }

    internal void Disconnect(bool manual = false)
    {
      DeregisterEvents();
      if (this._enemy)
      {
        if (this._enemy.behaviorSpeculator is BehaviorSpeculator bs)
          bs.EndStun();
        if (this._enemy.knockbackDoer is KnockbackDoer kbd)
        {
          Vector2 kbforce = Vector2.zero;
          foreach (var kb in kbd.m_activeContinuousKnockbacks)
            kbforce += kb;
          kbd.ClearContinuousKnockbacks();
          kbd.ApplySourcedKnockback(kbforce, kbforce.magnitude, base.gameObject);
        }
        if (this._enemy.gameObject.GetComponent<KnockbackUnleasher>() is KnockbackUnleasher kbu)
          UnityEngine.Object.Destroy(kbu);
        if (this._enemyBody)
        {
          if (this._owner)
            this._enemyBody.DeregisterSpecificCollisionException(this._owner.specRigidbody);
        }
      }
      if (this._gun)
        this._gun.DeregisterChain(this);
      this._connectedToEnemy      = false;
      this._enemy                 = null;
      this._gun                   = null;
      this._connectedToGun        = false;
      this._projectile            = null;
      this._connectedToProjectile = false;
      base.gameObject.Play("chain_snap_sound");
      if (this._mesh)
        this._mesh.LockWhenStationary(); // prevent the chain from updating once it stops moving around much
      this._active = false;
    }

    private void DeregisterEvents()
    {
      if (this._enemyBody)
      {
        this._enemyBody.OnTileCollision -= this.OnSlammedIntoSolidObject;
        this._enemyBody.OnPreRigidbodyCollision -= this.OnPossiblySlammedIntoOtherEnemy;
        this._enemyBody.OnRigidbodyCollision -= this.OnSlammedIntoAnything;
      }
      if (this._projectile)
        this._projectile.OnHitEnemy -= this.OnHitEnemy;
    }

    private void OnDestroy()
    {
      if (this._gun)
        this._gun.DeregisterChain(this);
      DeregisterEvents();
      if (this._mesh)
        UnityEngine.Object.Destroy(this._mesh.gameObject);
    }
}
