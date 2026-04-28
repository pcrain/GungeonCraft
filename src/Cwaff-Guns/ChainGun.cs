namespace CwaffingTheGungy;

/* TODO:
    - handle overextended chains smoothly
    - fade out chains and destroy them a certain time after being connected
    - don't allow double connecting enemies
    - let enemies run into enemy bullets and other enemies while chained
    - add damage while chained
    - add particle effects from slamming into walls while chained

    - scale chain length with range stat
    - scale whipping speed with damage stat
*/

public class ChainGun : CwaffGun
{
    public static string ItemName         = "Chain Gun";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "TBD";

    internal static tk2dSpriteAnimationClip _ChainLink = null;

    public static void Init()
    {
        Lazy.SetupGun<ChainGun>(ItemName, ShortDescription, LongDescription, Lore)
          .SetAttributes(quality: ItemQuality.C, gunClass: GunClass.RIFLE, reloadTime: 0.9f, ammo: 600, shootFps: 14, reloadFps: 4,
            muzzleFrom: Items.Mailbox)
          .InitProjectile(GunData.New(clipSize: 12, cooldown: 0.18f, shootStyle: ShootStyle.SemiAutomatic,
            damage: 5.5f, speed: 25f, range: 18f, force: 12f, hitEnemySound: "paintball_impact_enemy_sound",
            hitWallSound: "paintball_impact_wall_sound"));

        _ChainLink = VFX.Create("chain_link").DefaultAnimation();
    }

    public override void PostProcessProjectile(Projectile projectile)
    {
        base.PostProcessProjectile(projectile);
        new GameObject("chainlink").AddComponent<ChainkLink>().Setup(this.gun.GunPlayerOwner(), projectile, this);
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
    private Vector2 _physicsEndPos = default;

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
      Vector2 endPos              = gun.gun.barrelOffset.position; //TODO: null check
      this._physicsEndPos         = endPos;
      this._mesh                  = CwaffRopeMesh.Create(ChainGun._ChainLink, startPos: endPos, endPos: endPos, numSegments: SEGMENTS, segLength: SEGLENGTH);
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
      this._enemyBody = body;
      this._enemy.gameObject.AddComponent<KnockbackUnleasher>(); // uncap knockback for this specific enemy while they're chained
      // bs.enabled = false;
      if (this._owner)
        this._enemyBody.RegisterSpecificCollisionException(this._owner.specRigidbody);
      bs.Stun(3600f, true);
      this._mesh.endPos = enemy.CenterPosition;
      base.gameObject.Play("chain_connect_sound"); // TODO: add this
      return true;
    }

    private void LateUpdate()
    {
      const float KB_SCALAR = 500.0f;

      if (!this._setup || !this._active || BraveTime.DeltaTime == 0.0f || GameManager.Instance.IsPaused)
        return;
      if (this._connectedToGun && (!this._owner || !this._gun || this._owner.CurrentGun != this._gun.gun))
      {
        // Lazy.DebugConsoleLog($"no gun");
        Disconnect();
        return;
      }
      if (this._connectedToProjectile && !this._projectile)
      {
        // Lazy.DebugConsoleLog($"no proj");
        Disconnect();
        return;
      }
      if (this._connectedToEnemy && (!this._enemy || !this._enemyBody))
      {
        // Lazy.DebugConsoleLog($"no enemy");
        Disconnect();
        return;
      }

      if (this._gun && this._gun.gun)
        this._mesh.startPos = this._gun.gun.barrelOffset.transform.position;
      else if (this._projectile)
        this._mesh.startPos += this._projectile.LastVelocity * BraveTime.DeltaTime;
      else
      {
        // Lazy.DebugConsoleLog($"no position");
        Disconnect();
        return;
      }

      Vector2 bpos = this._mesh.startPos;
      Vector2 targetPos = this._owner.unadjustedAimPoint.XY();
      Vector2 newEndPos;
      if (this._connectedToEnemy && this._enemy && this._enemyBody && this._enemy.knockbackDoer is KnockbackDoer kbd)
      {
        Vector2 enemyPos = this._enemyBody.UnitCenter;
        newEndPos = Lazy.SmoothestLerp(enemyPos, targetPos, 4f);
        Vector2 tempDelta = (newEndPos - enemyPos);
        float magnitude = tempDelta.magnitude;
        kbd.m_activeContinuousKnockbacks.Clear(); // nothing else but the chain should affect this enemy
        if ((targetPos - this._mesh.endPos).magnitude >= 0.25f) // move only if the mouse has been moved significantly
          this._knockbackId = kbd.ApplyContinuousKnockback(tempDelta, KB_SCALAR * magnitude);
        else
          this._knockbackId = kbd.ApplyContinuousKnockback(tempDelta, 0.0f);
        newEndPos = enemyPos;
        this._mesh.endPos = newEndPos;
      }
      else
      {
        newEndPos = Lazy.SmoothestLerp(this._mesh.endPos, targetPos, 14f);
        Vector2 delta = (newEndPos - bpos);
        if (delta.sqrMagnitude > SQR_ROPELENGTH)
          newEndPos = bpos + ROPELENGTH * delta.normalized;
        this._mesh.endPos = newEndPos;
      }
      this._physicsEndPos = newEndPos;
    }

    private void Disconnect()
    {
      if (this._enemy)
      {
        if (this._enemy.behaviorSpeculator is BehaviorSpeculator bs)
        {
          // bs.enabled = true;
          bs.EndStun();
        }
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
        if (this._enemyBody && this._owner)
          this._enemyBody.DeregisterSpecificCollisionException(this._owner.specRigidbody);
      }
      this._connectedToEnemy      = false;
      this._enemy                 = null;
      this._gun                   = null;
      this._connectedToGun        = false;
      this._projectile            = null;
      this._connectedToProjectile = false;
      base.gameObject.Play("chain_snap_sound"); // TODO: add this
      this._active = false; //TODO: become debris
    }

    private void OnDestroy()
    {
      Lazy.DebugConsoleLog($"chain destroyed");
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

  private const int _BEZIER_CURVE_SEGMENTS = 20;
  private const int c_bonePixelLength = 4;
  private const float c_boneUnitLength = 0.25f;
  private const float c_trailHeightOffset = 0.5f;

  public static CwaffRopeMesh Create(tk2dSpriteAnimationClip animation, Vector2 startPos, Vector2 endPos, int numSegments, float segLength, string name = null)
  {
      CwaffRopeMesh mesh   = new GameObject(name ?? "new CwaffRopeMesh", typeof(CwaffRopeMesh)).GetComponent<CwaffRopeMesh>();
      mesh.animation       = animation;
      mesh.startPos        = startPos;
      mesh.endPos          = endPos;
      mesh._segLength      = segLength;
      mesh._ropePrevPoints = new();
      mesh._ropePoints     = new();
      Vector2 delta = (1f / numSegments) * (endPos - startPos);
      for (int i = 0; i <= numSegments; ++i)
      {
        mesh._ropePrevPoints.Add(startPos + i * delta);
        mesh._ropePoints.Add(startPos + i * delta);
      }
      return mesh;
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
    m_globalTimer += BraveTime.DeltaTime;
    this._updateTimer += BraveTime.DeltaTime;
    if (this._updateTimer < UPDATE_RATE)
      return;
    this._updateTimer -= UPDATE_RATE;

    Bone.ReturnAll(ref m_bones);
    DrawRope();
    for (LinkedListNode<Bone> n = m_bones.First; n != m_bones.Last; n = n.Next)
      n.Value.normal = (_Rot90 * (n.Next.Value.pos - n.Value.pos)).normalized;
    if (m_bones.Count > 1)
      m_bones.Last.Value.normal = m_bones.Last.Previous.Value.normal;
  }

  private void LateUpdate()
  {
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

  private void DrawRope()
  {
    RopeSim.SimulateRope(this.startPos, this.endPos, this._ropePoints, this._ropePrevPoints,
      minSegLength: this._segLength, maxSegLength: this._segLength, updateRate: UPDATE_RATE);
    for (int j = 0; j < this._ropePoints.Count; j++)
      m_bones.AddLast(Bone.Rent(this._ropePoints[j]));
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

        // clamp end to max length
        Vector2 toEnd = end - start;
        float dist = toEnd.magnitude;
        if (dist > maxRopeLength && dist > 0f)
            end = start + (toEnd / dist) * maxRopeLength;

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
