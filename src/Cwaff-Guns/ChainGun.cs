namespace CwaffingTheGungy;

using static CwaffingTheGungy.RopeSim; // StretchPolicy

/* TODO:
    - better sounds would still be great
    - better debris than chain links
    - add support for controller
    - add cleanup (destroy chains) when in another room and not onscreen

    - add and animate gun
    - scale chain length with range stat
    - scale whipping speed with damage stat

    - maybe figure out better enemy slamming physics
*/

public class ChainGun : CwaffGun
{
    public static string ItemName         = "Chain Gun";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    internal static GameObject[] _ChainDebris = new GameObject[2];
    internal static GameObject _ChainImpact = null;
    internal static tk2dSpriteAnimationClip _ChainLink = null;

    public static void Init()
    {
        Lazy.SetupGun<ChainGun>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.C, gunClass: GunClass.RIFLE, reloadTime: 0.9f, ammo: 600, shootFps: 14, reloadFps: 4,
            muzzleFrom: Items.Mailbox, fireAudio: "chain_launch_sound")
          .InitProjectile(GunData.New(clipSize: 12, cooldown: 0.18f, shootStyle: ShootStyle.SemiAutomatic, invisibleProjectile: true,
            damage: 5.5f, speed: 75f, range: 18f, force: 12f, pierceBreakables: true, hitSound: "chain_impact_sound"))
          .Attach<ChainGunProjectile>();

        _ChainLink = VFX.Create("chain_link").DefaultAnimation();
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
        new GameObject("chainlink").AddComponent<ChainkLink>().Setup(this.gun.GunPlayerOwner(), projectile, this);
    }

    public static void DoChainDebrisAt(Vector2 pos, int num)
    {
      for (int i = 0; i < num; ++i)
      {
          UnityEngine.Object.Instantiate(
            ChainGun._ChainDebris[UnityEngine.Random.Range(0, 2)], pos, Lazy.RandomEulerZ())
          .GetComponent<DebrisObject>().Trigger(Lazy.RandomVector(2f * UnityEngine.Random.value).ToVector3ZUp(2f), 0.25f);
      }
    }
}

public class ChainGunProjectile : MonoBehaviour
{
  private Projectile _proj;

  private void Start()
  {
    this._proj = base.gameObject.GetComponent<Projectile>();
    if (!this._proj)
      return;

    this._proj.OnDestruction += this.OnProjectileDestruction;
  }

  private void OnProjectileDestruction(Projectile projectile)
  {
      if (!projectile || projectile != this._proj)
        return;
      this._proj.OnDestruction -= this.OnProjectileDestruction;
      ChainGun.DoChainDebrisAt(this._proj.SafeCenter, 3);
  }
}

public class ChainkLink : MonoBehaviour
{
    private const int SEGMENTS         = 40;
    private const float SEGLENGTH      = 0.25f;
    private const float ROPELENGTH     = SEGMENTS * SEGLENGTH;
    private const float SQR_ROPELENGTH = ROPELENGTH * ROPELENGTH;

    private PlayerController _owner = null;
    private ChainGun _gun = null;
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

    public void Setup(PlayerController owner, Projectile proj, ChainGun gun)
    {
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
        animation: ChainGun._ChainLink, startPos: endPos, endPos: endPos, numSegments: SEGMENTS, segLength: SEGLENGTH,
        stretchPolicy: StretchPolicy.GROWTEMPORARY);
      this._mesh.gameObject.SetLayerRecursively(LayerMask.NameToLayer("BG_Critical"));
      this._active                = true;
      this._setup                 = true;
    }

    private void OnHitEnemy(Projectile projectile, SpeculativeRigidbody rigidbody, bool arg3)
    {
        projectile.OnHitEnemy -= this.OnHitEnemy;
        if (!this || this._connectedToEnemy || !rigidbody)
          return;
        if (!rigidbody.aiActor || !TryChainLinkEnemy(rigidbody.aiActor))
          Disconnect();
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
      this._enemyBody.OnTileCollision += this.OnPossiblySlammedIntoWall;
      // this._enemyBody.OnPreRigidbodyCollision += this.OnPossiblySlammedIntoOtherEnemy; // TODO: see if we can make this interesting later
      // this._enemyBody.AddCollisionLayerOverride(CollisionMask.LayerToMask(CollisionLayer.EnemyHitBox));
      this._enemy.gameObject.AddComponent<KnockbackUnleasher>(); // uncap knockback for this specific enemy while they're chained
      if (this._owner)
        this._enemyBody.RegisterSpecificCollisionException(this._owner.specRigidbody);
      bs.Stun(3600f, true);
      this._mesh.endPos = enemy.CenterPosition;
      // base.gameObject.Play("chain_lockdown_sound");
      return true;
    }

    private void OnPossiblySlammedIntoOtherEnemy(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
    {
      const float COLLATERAL_DAMAGE_SCALE = 0.25f;
      if (otherRigidbody && otherRigidbody.aiActor is AIActor other)
      {
        if (other.IsNormalEnemy && other.healthHaver is HealthHaver hh)
        {
          hh.ApplyDamage(COLLATERAL_DAMAGE_SCALE * myRigidbody.Velocity.magnitude, myRigidbody.Velocity, "Pinball");
          otherRigidbody.RegisterTemporaryCollisionException(myRigidbody, 0.25f);
        }
        PhysicsEngine.SkipCollision = true;
      }
    }

    private void OnPossiblySlammedIntoWall(CollisionData tileCollision)
    {
      const float MIN_SLAM_GAP     = 0.1f;
      const float FORCE_SHAKE_MULT = 0.001f;
      const float DAMAGE_VEL_MULT  = 0.01f;

      float now = BraveTime.ScaledTimeSinceStartup;
      if (!this._enemyBody || now < this._nextSlamTime || !this._enemy || this._enemy.knockbackDoer is not KnockbackDoer kbd)
      {
          this._nextSlamTime = now + MIN_SLAM_GAP;
          return;
      }
      this._nextSlamTime = now + MIN_SLAM_GAP;

      float slamVel = this._enemyBody.Velocity.magnitude;
      float slamForce = FORCE_SHAKE_MULT * (kbd.weight * slamVel);
      float slamDamage = DAMAGE_VEL_MULT * slamVel * slamVel;
      // Lazy.DebugConsoleLog($"enemy slammed with force {slamForce}, velocity {slamVel}, and {slamDamage} damage");
      if (this._enemy.healthHaver is HealthHaver hh)
        hh.ApplyDamage(slamDamage, -tileCollision.Normal, "slammed", CoreDamageTypes.None, DamageCategory.Collision);
      base.gameObject.Play((slamForce > 1) ? "wall_slam_heavy" : (slamForce > 0.5) ? "wall_slam_medium" : "wall_slam_light");
      base.gameObject.Play("chain_snap_sound");
      CwaffVFX.Spawn(
        prefab        : ChainGun._ChainImpact,
        position      : tileCollision.Contact,
        rotation      : tileCollision.Normal.ToAngle().EulerZ(),
        velocity      : 2f * tileCollision.Normal.normalized,
        emissivePower : 5f,
        emissiveColor : Color.white,
        startScale    : Mathf.Clamp01(slamForce),
        endScale      : Mathf.Clamp01(slamForce)
        );
      // ChainGun.DoChainDebrisAt(tileCollision.Contact, Mathf.Clamp(Mathf.CeilToInt(10f * slamForce), 4, 20));
      // CwaffVFX.SpawnBurst(
      //     prefab           : Entropynnium._ManaParticlePrefab,
      //     numToSpawn       : Mathf.Clamp(Mathf.CeilToInt(10f * slamForce), 4, 20),
      //     basePosition     : tileCollision.Contact,
      //     positionVariance : 2f,
      //     minVariance      : 1f,
      //     velType          : CwaffVFX.Vel.OutwardFromCenter,
      //     rotType          : CwaffVFX.Rot.Random,
      //     lifetime         : 0.5f,
      //     startScale       : 0.6f,
      //     endScale         : 0.2f,
      //     emissivePower    : 100f,
      //     emissiveColor    : ExtendedColours.purple
      //   );
      GameManager.Instance.MainCameraController.DoScreenShake(new ScreenShakeSettings(
        mag: slamForce, spd: 2f * slamForce, tim: 0.3f * Mathf.Min(slamForce, 1f), foff: 0f), tileCollision.Contact);
    }

    private void LateUpdate()
    {
      const float KB_SCALAR = 12.0f;

      if (!this._setup || !this._active || BraveTime.DeltaTime == 0.0f || GameManager.Instance.IsPaused)
        return;
      if (this._connectedToGun && (!this._owner || !this._gun || this._owner.CurrentGun != this._gun.gun))
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

      if (this._gun && this._gun.gun)
        this._mesh.startPos = this._gun.gun.barrelOffset.transform.position;
      else if (this._projectile)
        this._mesh.startPos += this._projectile.LastVelocity * BraveTime.DeltaTime;
      else
      {
        Disconnect();
        return;
      }

      Vector2 bpos = this._mesh.startPos;
      Vector2 targetPos = this._owner.unadjustedAimPoint.XY();
      if (this._connectedToEnemy && this._enemy && this._enemyBody && this._enemy.knockbackDoer is KnockbackDoer kbd)
      {
        Vector2 enemyPos = this._enemyBody.UnitCenter;
        Vector2 tempDelta = (targetPos - enemyPos);
        kbd.m_activeContinuousKnockbacks.Clear(); // nothing else but the chain should affect this enemy
        if ((targetPos - this._mesh.endPos).magnitude >= 0.25f) // move only if the mouse has been moved significantly
          this._knockbackId = kbd.ApplyContinuousKnockback(tempDelta, KB_SCALAR * tempDelta.magnitude);
        else
          this._knockbackId = kbd.ApplyContinuousKnockback(tempDelta, 0.0f);
        this._mesh.endPos = enemyPos;
      }
      else if (this._connectedToProjectile && this._projectile)
      {
        this._mesh.endPos = this._projectile.SafeCenter;
      }
      else
      {
        Vector2 newEndPos = Lazy.SmoothestLerp(this._mesh.endPos, targetPos, 14f);
        Vector2 delta = (newEndPos - bpos);
        if (delta.sqrMagnitude > SQR_ROPELENGTH)
          newEndPos = bpos + ROPELENGTH * delta.normalized;
        this._mesh.endPos = newEndPos;
      }
    }

    private void Disconnect()
    {
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
          this._enemyBody.OnTileCollision -= this.OnPossiblySlammedIntoWall;
          this._enemyBody.OnPreRigidbodyCollision -= this.OnPossiblySlammedIntoOtherEnemy;
          if (this._owner)
            this._enemyBody.DeregisterSpecificCollisionException(this._owner.specRigidbody);
        }
      }
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

    private void OnDestroy()
    {
      // Lazy.DebugConsoleLog($"chain destroyed");
      if (this._enemyBody)
      {
        this._enemyBody.OnTileCollision -= this.OnPossiblySlammedIntoWall;
        this._enemyBody.OnPreRigidbodyCollision -= this.OnPossiblySlammedIntoOtherEnemy;
      }
      if (this._projectile)
        this._projectile.OnHitEnemy -= this.OnHitEnemy;
      if (this._mesh)
        UnityEngine.Object.Destroy(this._mesh.gameObject);
    }
}

/// <summary>Class for creating curvy beam-like sprites without beams</summary>
public class CwaffRopeMesh : MonoBehaviour
{
  private class Bone
  {
    private static readonly LinkedList<Bone> _BonePool = new();
    private static int _BonesCreated = 0;

    public Vector2 pos;
    public Vector2 normal;

    internal static LinkedListNode<Bone> Rent(Vector2 pos)
    {
      if (_BonePool.Count == 0)
        _BonePool.AddLast(new Bone());

      LinkedListNode<Bone> node = _BonePool.Last;
      _BonePool.RemoveLast();

      Bone bone = node.Value;
      bone.pos    = pos;
      bone.normal = default;

      return node;
    }

    internal static void Return(LinkedListNode<Bone> bone)
    {
      _BonePool.AddLast(bone);
      // System.Console.WriteLine($"returned {_BonePool.Count}/{_BonesCreated} bones");
    }

    internal static void ReturnAll(ref LinkedList<Bone> bones)
    {
      if (bones == null)
        return;
      while (bones.Count > 0)
      {
        LinkedListNode<Bone> bone = bones.Last;
        bones.RemoveLast();
        _BonePool.AddLast(bone);
      }
      // System.Console.WriteLine($"returned {_BonePool.Count}/{_BonesCreated} bones");
    }

    private Bone() // can only be created by Rent
    {
      ++_BonesCreated;
    }
  }

  private const float UPDATE_RATE = 1.0f / 60.0f; // seconds between rope updates (needs to be fixed due to verlet integration)

  public tk2dSpriteAnimationClip animation;
  public Vector2 startPos;
  public Vector2 endPos;
  public bool locked; // if true, prevents the rope from updating

  private tk2dTiledSprite m_sprite;
  private int m_spriteSubtileWidth;
  private LinkedList<Bone> m_bones = new LinkedList<Bone>();
  private Vector2 m_minBonePosition;
  private Vector2 m_maxBonePosition;
  private float m_globalTimer;
  private List<Vector2> _ropePrevPoints;
  private List<Vector2> _ropePoints;
  private float _segLength;
  private float _updateTimer;
  private StretchPolicy _stretchPolicy;
  private float _softMaxRopeLength;
  private int _numSegments;
  private float _lockThreshold; // if > 0, locks the rope once it's stopped moving

  private const int _BEZIER_CURVE_SEGMENTS = 20;
  private const int c_bonePixelLength = 4;
  private const float c_boneUnitLength = 0.25f;
  private const float c_trailHeightOffset = 0.5f;

  public static CwaffRopeMesh Create(tk2dSpriteAnimationClip animation, Vector2 startPos, Vector2 endPos, int numSegments, float segLength,
    string name = null, StretchPolicy stretchPolicy = StretchPolicy.STRETCH)
  {
      CwaffRopeMesh mesh   = new GameObject(name ?? "new CwaffRopeMesh", typeof(CwaffRopeMesh)).GetComponent<CwaffRopeMesh>();
      mesh.animation       = animation;
      mesh.startPos        = startPos;
      mesh.endPos          = endPos;
      mesh._segLength      = segLength;
      mesh._ropePrevPoints = new();
      mesh._ropePoints     = new();
      mesh._stretchPolicy  = stretchPolicy;
      mesh._numSegments    = numSegments;
      Vector2 delta = (1f / numSegments) * (endPos - startPos);
      for (int i = 0; i <= numSegments; ++i)
      {
        mesh._ropePrevPoints.Add(startPos + i * delta);
        mesh._ropePoints.Add(startPos + i * delta);
      }
      mesh._softMaxRopeLength = segLength * numSegments;
      mesh.locked = false;
      mesh._lockThreshold = 0f;
      return mesh;
  }

  public void LockWhenStationary(float threshold = 0.01f)
  {
    this._lockThreshold = threshold;
  }

  private void Start()
  {
    base.transform.rotation = Quaternion.identity;
    base.transform.position = Vector3.zero;
    m_sprite = this.AddComponent<tk2dTiledSprite>();
    m_sprite.collection = animation.frames[0].spriteCollection;
    m_sprite.spriteId = animation.frames[0].spriteId;
    m_sprite.OverrideGetTiledSpriteGeomDesc = GetTiledSpriteGeomDesc;
    m_sprite.OverrideSetTiledSpriteGeom = SetTiledSpriteGeom;
    tk2dSpriteDefinition currentSpriteDef = m_sprite.collection.spriteDefinitions[m_sprite.spriteId];
    m_spriteSubtileWidth = Mathf.RoundToInt(currentSpriteDef.untrimmedBoundsDataExtents.x / currentSpriteDef.texelSize.x) / 4;
    _updateTimer = 0.0f;
  }

  private static readonly Quaternion _Rot90 = Quaternion.Euler(0f, 0f, 90f);
  private void Update()
  {
    if (this.locked)
      return;
    m_globalTimer += BraveTime.DeltaTime;
    this._updateTimer += BraveTime.DeltaTime;
    if (this._updateTimer < UPDATE_RATE)
      return; // rope updates need to happen at a fixed time rate due to verlet integration silliness
    this._updateTimer -= UPDATE_RATE;

    UpdateRope();
    for (LinkedListNode<Bone> n = m_bones.First; n != m_bones.Last; n = n.Next)
      n.Value.normal = (_Rot90 * (n.Next.Value.pos - n.Value.pos)).normalized;
    if (m_bones.Count > 1)
      m_bones.Last.Value.normal = m_bones.Last.Previous.Value.normal;
  }

  private void LateUpdate()
  {
    if (this.locked)
      return;
    m_minBonePosition = new Vector2(float.MaxValue, float.MaxValue);
    m_maxBonePosition = new Vector2(float.MinValue, float.MinValue);
    for (LinkedListNode<Bone> n = m_bones.First; n != null; n = n.Next)
    {
      m_minBonePosition = Vector2.Min(m_minBonePosition, n.Value.pos);
      m_maxBonePosition = Vector2.Max(m_maxBonePosition, n.Value.pos);
    }
    base.transform.position = new Vector3(m_minBonePosition.x, m_minBonePosition.y);
    m_sprite.HeightOffGround = 0.5f;
    m_sprite.ForceBuild();
    m_sprite.UpdateZDepth();
  }

  private void OnDestroy()
  {
    Bone.ReturnAll(ref m_bones);
  }

  private void UpdateRope()
  {
    Bone.ReturnAll(ref m_bones);
    Vector2 curStartPos = this.startPos;
    Vector2 curEndPos = this.endPos;
    switch (this._stretchPolicy)
    {
      case StretchPolicy.CLAMP:
      {
        // clamp end to max length
        Vector2 toEnd = curEndPos - curStartPos;
        float dist = toEnd.magnitude;
        float extraLength = dist - this._softMaxRopeLength;
        if (extraLength > 0f)
            curEndPos = curStartPos + (toEnd / dist) * this._softMaxRopeLength;
        break;
      }
      case StretchPolicy.GROWTEMPORARY:
      case StretchPolicy.GROWPERMANENT:
      {
        Vector2 toEnd = curEndPos - curStartPos;
        float dist = toEnd.magnitude;
        float extraLength = dist - this._softMaxRopeLength;
        int curExtraPoints = Mathf.Max(0, Mathf.CeilToInt((float)extraLength / this._segLength));
        int prevExtraPoints = this._ropePoints.Count - (this._numSegments + 1);
        if (prevExtraPoints > curExtraPoints && this._stretchPolicy != StretchPolicy.GROWPERMANENT)
        {
          int pointsToRemove = prevExtraPoints - curExtraPoints;
          this._ropePoints.RemoveRange(this._ropePoints.Count - pointsToRemove, pointsToRemove);
          this._ropePrevPoints.RemoveRange(this._ropePrevPoints.Count - pointsToRemove, pointsToRemove);
        }
        else if (prevExtraPoints < curExtraPoints)
        {
          int pointsToAdd = curExtraPoints - prevExtraPoints;
          while (--pointsToAdd >= 0)
          {
            Vector2 nextPos = this.endPos;
            if (pointsToAdd > 0)
            {
              Vector2 prevPos = this._ropePoints[this._ropePoints.Count - 1];
              nextPos = prevPos + this._segLength * (this.endPos - prevPos).normalized;
            }
            this._ropePoints.Add(nextPos);
            this._ropePrevPoints.Add(nextPos);
          }
        }
        break;
      }
      default:
        break;
    }
    // Lazy.DebugConsoleLog($"simulating {this._ropePoints.Count} points");
    RopeSim.SimulateRope(curStartPos, curEndPos, this._ropePoints, this._ropePrevPoints,
      minSegLength: this._segLength, maxSegLength: this._segLength, updateRate: UPDATE_RATE);
    for (int j = 0; j < this._ropePoints.Count; j++)
      m_bones.AddLast(Bone.Rent(this._ropePoints[j]));
    if (this._lockThreshold > 0f)
    {
      float maxMovement = 0.0f;
      for (int i = this._ropePoints.Count - 1; i >= 0; --i)
        maxMovement = Mathf.Max(maxMovement, (this._ropePoints[i] - this._ropePrevPoints[i]).sqrMagnitude);
      // Lazy.DebugConsoleLog($" max movement is {Mathf.Sqrt(maxMovement)}");
      if (maxMovement <= (this._lockThreshold * this._lockThreshold))
      {
        // Lazy.DebugConsoleLog($"    locking updates!");
        this.locked = true;
      }
    }
  }

  private void GetTiledSpriteGeomDesc(out int numVertices, out int numIndices, tk2dSpriteDefinition spriteDef, Vector2 dimensions)
  {
    int segments = Mathf.Max(m_bones.Count - 1, 0);
    numVertices = segments * 4;
    numIndices = segments * 6;
  }

  private void SetTiledSpriteGeom(Vector3[] pos, Vector2[] uv, int offset, out Vector3 boundsCenter, out Vector3 boundsExtents, tk2dSpriteDefinition spriteDef, Vector3 scale, Vector2 dimensions, tk2dBaseSprite.Anchor anchor, float colliderOffsetZ, float colliderExtentZ)
  {
    int spritePixelLength = Mathf.RoundToInt(spriteDef.untrimmedBoundsDataExtents.x / spriteDef.texelSize.x);
    int numSubtilesInSprite = spritePixelLength / 4;
    int lastBoneIndex = Mathf.Max(m_bones.Count - 1, 0);
    int totalSpritesToDraw = Mathf.CeilToInt((float)lastBoneIndex / (float)numSubtilesInSprite);
    boundsCenter = 0.5f * (m_maxBonePosition + m_minBonePosition);
    boundsExtents = 0.5f * (m_maxBonePosition - m_minBonePosition);
    LinkedListNode<Bone> bone = m_bones.First;
    int verticesDrawn = 0;
    int animationFrame = Mathf.FloorToInt(Mathf.Repeat(m_globalTimer * animation.fps, animation.frames.Length));
    tk2dSpriteAnimationFrame frame = animation.frames[animationFrame];
    tk2dSpriteDefinition segmentSprite = frame.spriteCollection.spriteDefinitions[frame.spriteId];
    for (int i = 0; i < totalSpritesToDraw; i++)
    {
      int lastSubtileIndex = numSubtilesInSprite - 1;
      if (i == totalSpritesToDraw - 1 && lastBoneIndex % numSubtilesInSprite != 0)
        lastSubtileIndex = lastBoneIndex % numSubtilesInSprite - 1;
      float numSpritesDrawn = 0f;
      for (int j = 0; j <= lastSubtileIndex; j++)
      {
        float fractionOfSubtileToDraw = 1f;
        Bone curBone = bone.Value;
        Bone nextBone = bone.Next.Value;
        if (i == totalSpritesToDraw - 1 && j == lastSubtileIndex)
          fractionOfSubtileToDraw = Vector2.Distance(nextBone.pos, curBone.pos);
        int uvCurrent = offset + verticesDrawn;
        pos[uvCurrent++] = (curBone.pos  + curBone.normal  * segmentSprite.position0.y - m_minBonePosition).ToVector3ZUp(/*40*/0f);
        pos[uvCurrent++] = (nextBone.pos + nextBone.normal * segmentSprite.position1.y - m_minBonePosition).ToVector3ZUp(/*40*/0f);
        pos[uvCurrent++] = (curBone.pos  + curBone.normal  * segmentSprite.position2.y - m_minBonePosition).ToVector3ZUp(/*40*/0f);
        pos[uvCurrent++] = (nextBone.pos + nextBone.normal * segmentSprite.position3.y - m_minBonePosition).ToVector3ZUp(/*40*/0f);
        Vector2 minUV = Vector2.Lerp(segmentSprite.uvs[0], segmentSprite.uvs[1], numSpritesDrawn);
        Vector2 maxUV = Vector2.Lerp(segmentSprite.uvs[2], segmentSprite.uvs[3], numSpritesDrawn + fractionOfSubtileToDraw / numSubtilesInSprite);
        uvCurrent = offset + verticesDrawn;
        uv[uvCurrent++] = minUV;
        uv[uvCurrent++] = new Vector2(maxUV.x, minUV.y);
        uv[uvCurrent++] = new Vector2(minUV.x, maxUV.y);
        uv[uvCurrent++] = maxUV;
        verticesDrawn += 4;
        numSpritesDrawn += fractionOfSubtileToDraw / m_spriteSubtileWidth;
        bone = bone.Next;
      }
    }
  }
}

public static class RopeSim
{
    private const int DEFAULT_VERLET_ITERATIONS     = 100; // NOTE: stops iterating if chain is sufficiently constrained
    private const float DEFAULT_DAMPING             = 0.98f;
    private static readonly Vector2 DEFAULT_GRAVITY = new Vector2(0f, -1.5f); // visual sag

    public enum StretchPolicy
    {
      STRETCH,       // rope stretches each segment to fill its entire length
      CLAMP,         // clamps the rope length to maxSegmentLength * numSegments, remainder of rope does not render
      GROWTEMPORARY, // rope temporarily grows to visually render to its current length, but shrinks back as its able to
      GROWPERMANENT, // rope permanently grows to visually render to its current length
    }

    /// <summary>
    /// Given start and end points for a rope and a list of current and previous points for rope segments, updates
    /// the list of previous and current intermediate rope points with the specified physics parameters. Uses
    /// default physics parameters if nothing is passed.
    /// </summary>
    public static List<Vector2> SimulateRope(Vector2 start, Vector2 end, List<Vector2> points, List<Vector2> prevPoints,
      int? verletIters = null, float? damping = null, Vector2? gravity = null, float minSegLength = 0.0f,
      float maxSegLength = 100.0f, float updateRate = 1.0f / 60.0f)
    {
        const float VERLET_THRESHOLD = 0.001f; // if no point moves more than this much, end verlet iteration early

        int count = points.Count;
        if (count < 2)
          return points;

        // setup config
        float dt            = updateRate; // BraveTime.DeltaTime;
        int _verletIters    = verletIters ?? DEFAULT_VERLET_ITERATIONS;
        float _damping      = damping ?? DEFAULT_DAMPING;
        Vector2 _gravity    = gravity ?? DEFAULT_GRAVITY;
        float maxRopeLength = maxSegLength * (count - 1);

        // do verlet integration
        Vector2 sqrdtgrav = _gravity * dt * dt;
        for (int i = 1; i < count - 1; i++)
        {
            Vector2 velocity = (points[i] - prevPoints[i]) * _damping;
            prevPoints[i] = points[i];
            points[i] += velocity + sqrdtgrav;
        }

        // pin endpoints
        points[0] = prevPoints[0] = start;
        points[count - 1] = prevPoints[count - 1] = end;

        // constraint solve
        for (int it = 0; it < _verletIters; it++)
        {
            float maxAdjust = 0f;
            for (int i = 0; i < count - 1; i++)
            {
                Vector2 delta = points[i + 1] - points[i];
                float d = delta.magnitude;
                if (d == 0f)
                    continue;

                Vector2 dir = delta / d;
                float correctionAmount = 0f;
                if (d > maxSegLength)
                    correctionAmount = d - maxSegLength; // too long -> pull together
                else if (d < minSegLength)
                    correctionAmount = d - minSegLength; // too short -> push apart
                else
                    continue; // already within bounds

                maxAdjust = Mathf.Max(maxAdjust, correctionAmount);
                if (i == 0)
                    points[i + 1] -= dir * correctionAmount; // start fixed
                else if (i + 1 == count - 1)
                    points[i] += dir * correctionAmount; // end fixed
                else
                {
                    Vector2 correction = dir * (correctionAmount * 0.5f);
                    points[i] += correction;
                    points[i + 1] -= correction;
                }
            }
            if (maxAdjust < VERLET_THRESHOLD)
            {
              // Lazy.DebugConsoleLog($"early verlet finish after {it} iters");
              break; // early break if few adjustments needed to be made
            }
        }

        return points;
    }
}
