namespace CwaffingTheGungy;

public class CwaffRaidenBeamController : BeamController
{
  public enum TargetType
  {
    Screen = 10,
    Room = 20
  }

  private class Bone
  {
    private static int _Counter = 0;

    public Vector2 pos;

    public Vector2 normal;

    private static int _BonesCreated = 0;

    private static readonly LinkedList<Bone> _BonePool = new();

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

  public string beamAnimation;

  public bool usesStartAnimation;

  public string startAnimation;

  public tk2dBaseSprite ImpactRenderer;

  // [CheckAnimation(null)]
  public string EnemyImpactAnimation;

  // [CheckAnimation(null)]
  public string BossImpactAnimation;

  // [CheckAnimation(null)]
  public string OtherImpactAnimation;

  public TargetType targetType = TargetType.Screen;

  public int maxTargets = -1;

  public float endRampHeight;

  public int endRampSteps;

  public bool FlipUvsY;

  public bool SelectRandomTarget;

  private List<AIActor> s_enemiesInRoom = new List<AIActor>();

  private tk2dTiledSprite m_sprite;

  private tk2dSpriteAnimationClip m_startAnimationClip;

  private tk2dSpriteAnimationClip m_animationClip;

  private bool m_isCurrentlyFiring = true;

  private bool m_audioPlaying;

  private List<AIActor> m_targets = new List<AIActor>();

  private SpeculativeRigidbody m_hitRigidbody;

  private int m_spriteSubtileWidth;

  private LinkedList<Bone> m_bones = new LinkedList<Bone>();

  private Vector2 m_minBonePosition;

  private Vector2 m_maxBonePosition;

  private bool m_isDirty;

  private float m_globalTimer;

  private float _growthTime = 0.0f;

  private bool _lockedOn = false;

  private int _firstWrapBoneIndex;

  private Vector2 lastImpactPosition;

  private Vector3 mainBezierPoint1;
  private Vector3 mainBezierPoint2;
  private Vector3 mainBezierPoint3;
  private Vector3 mainBezierPoint4;

  private const float _EXTEND_TIME = 1.25f;

  private const int c_segmentCount = 20;

  private const int c_bonePixelLength = 4;

  private const float c_boneUnitLength = 0.25f;

  private const float c_trailHeightOffset = 0.5f;

  private float m_projectileScale = 1f;

  public override bool ShouldUseAmmo
  {
    get
    {
      return true;
    }
  }

  public float RampHeightOffset { get; set; }

  public void Start()
  {
    base.transform.parent = SpawnManager.Instance.VFX;
    base.transform.rotation = Quaternion.identity;
    base.transform.position = Vector3.zero;
    m_sprite = GetComponent<tk2dTiledSprite>();
    m_sprite.OverrideGetTiledSpriteGeomDesc = GetTiledSpriteGeomDesc;
    m_sprite.OverrideSetTiledSpriteGeom = SetTiledSpriteGeom;
    tk2dSpriteDefinition currentSpriteDef = m_sprite.GetCurrentSpriteDef();
    m_spriteSubtileWidth = Mathf.RoundToInt(currentSpriteDef.untrimmedBoundsDataExtents.x / currentSpriteDef.texelSize.x) / 4;
    if (usesStartAnimation)
      m_startAnimationClip = base.spriteAnimator.GetClipByName(startAnimation);
    m_animationClip = base.spriteAnimator.GetClipByName(beamAnimation);
    if (base.projectile.Owner is PlayerController playerController)
      m_projectileScale = playerController.BulletScaleModifier;
    if (ImpactRenderer)
      ImpactRenderer.transform.localScale = new Vector3(m_projectileScale, m_projectileScale, 1f);
    this.lastImpactPosition = base.Origin;
  }

  public void Update()
  {
    m_globalTimer += BraveTime.DeltaTime;
    for (int i = m_targets.Count - 1; i >= 0; i--)
    {
      AIActor aIActor = m_targets[i];
      if (!aIActor || !aIActor.healthHaver || aIActor.healthHaver.IsDead || aIActor.IsGone)
      {
        m_targets.RemoveAt(i);
        this._growthTime = 0.0f;
        this._lockedOn = false;
      }
    }
    m_hitRigidbody = null;
    HandleBeamFrame(base.Origin, base.Direction, m_isCurrentlyFiring);
    if (m_targets == null || m_targets.Count == 0)
    {
      if (GameManager.AUDIO_ENABLED && m_audioPlaying)
      {
        m_audioPlaying = false;
        AkSoundEngine.PostEvent("Stop_WPN_loop_01", base.gameObject);
      }
    }
    else if (GameManager.AUDIO_ENABLED && !m_audioPlaying)
    {
      m_audioPlaying = true;
      AkSoundEngine.PostEvent("Play_WPN_shot_01", base.gameObject);
    }

    float damage = base.projectile.baseData.damage + base.DamageModifier;
    PlayerController playerController = base.projectile.Owner as PlayerController;
    if (playerController)
    {
      damage *= playerController.stats.GetStatValue(PlayerStats.StatType.RateOfFire);
      damage *= playerController.stats.GetStatValue(PlayerStats.StatType.Damage);
    }
    if (base.ChanceBasedShadowBullet)
      damage *= 2f;
    string impactAnimation = OtherImpactAnimation;
    if (this._lockedOn && m_targets != null && m_targets.Count > 0)
    {
      foreach (AIActor target in m_targets)
      {
        if (!target || !target.healthHaver)
          continue;

        impactAnimation = ((string.IsNullOrEmpty(BossImpactAnimation) || !target.healthHaver.IsBoss) ? EnemyImpactAnimation : BossImpactAnimation);
        if (target.healthHaver.IsBoss && (bool)base.projectile)
          damage *= base.projectile.BossDamageMultiplier;
        if ((bool)base.projectile && base.projectile.BlackPhantomDamageMultiplier != 1f && target.IsBlackPhantom)
          damage *= base.projectile.BlackPhantomDamageMultiplier;
        float damageThisTick = damage * BraveTime.DeltaTime;
        target.healthHaver.ApplyDamage(damageThisTick, Vector2.zero, base.Owner.ActorName);
        if (playerController && playerController.CurrentGun && playerController.CurrentGun.gameObject.GetComponent<Yggdrashell>() is Yggdrashell ygg)
          ygg.UpdateDamageDealt(damageThisTick);
      }
    }
    if (m_hitRigidbody)
    {
      if (m_hitRigidbody.minorBreakable)
        m_hitRigidbody.minorBreakable.Break(base.Direction);
      if (m_hitRigidbody.majorBreakable)
        m_hitRigidbody.majorBreakable.ApplyDamage(damage * BraveTime.DeltaTime, base.Direction, false);
    }
    if (ImpactRenderer && ImpactRenderer.spriteAnimator && !string.IsNullOrEmpty(impactAnimation))
      ImpactRenderer.spriteAnimator.Play(impactAnimation);
  }

  public void LateUpdate()
  {
    if (!m_isDirty)
      return;

    m_minBonePosition = new Vector2(float.MaxValue, float.MaxValue);
    m_maxBonePosition = new Vector2(float.MinValue, float.MinValue);
    for (LinkedListNode<Bone> linkedListNode = m_bones.First; linkedListNode != null; linkedListNode = linkedListNode.Next)
    {
      m_minBonePosition = Vector2.Min(m_minBonePosition, linkedListNode.Value.pos);
      m_maxBonePosition = Vector2.Max(m_maxBonePosition, linkedListNode.Value.pos);
    }
    Vector2 vector = new Vector2(m_minBonePosition.x, m_minBonePosition.y) - base.transform.position.XY();
    base.transform.position = new Vector3(m_minBonePosition.x, m_minBonePosition.y);
    m_sprite.HeightOffGround = 0.5f;
    if (ImpactRenderer)
      ImpactRenderer.transform.position -= vector.ToVector3ZUp();
    m_sprite.ForceBuild();
    m_sprite.UpdateZDepth();
  }

  // [CompilerGenerated]
  /// <summary>Using static version to avoid allocations.</summary>
  private static class BeamSorter
  {
    internal static Vector2 barrelPosition;

    internal static int Compare(AIActor a, AIActor b)
    {
      return Vector2.Distance(barrelPosition, a.CenterPosition).CompareTo(Vector2.Distance(barrelPosition, b.CenterPosition));
    }
  }

  private static readonly int _BEAM_MASK =
    (CollisionLayerMatrix.GetMask(CollisionLayer.Projectile)
    | CollisionMask.LayerToMask(CollisionLayer.BeamBlocker))
    & ~CollisionMask.LayerToMask(CollisionLayer.PlayerCollider, CollisionLayer.PlayerHitBox);

  private const float _BEZIER_TIGHTNESS = 5f;
  private const float _RAYCAST_DIST = 30f;
  private const int _EXTRA_WRAPS = 5;
  private const float _WRAP_TIGHTNESS = 2f;
  private const float _WRAP_ANGLE = 180f / _EXTRA_WRAPS;
  private const float _WRAP_SPREAD = 63f;
  private const float _SNAP_DISTANCE = 2f;
  private const float _VINE_LERP = 15f;

  private List<float> _vineAngles = new();
  private List<float> _vineLengths = new();
  private List<float> _vineAngleDevs = new();
  private List<float> _vineLengthDevs = new();
  public void HandleBeamFrame(Vector2 barrelPosition, Vector2 direction, bool isCurrentlyFiring)
  {
    if (base.Owner is PlayerController)
      HandleChanceTick();

    if (targetType == TargetType.Screen)
    {
      m_targets.Clear();
      List<AIActor> allEnemies = StaticReferenceManager.AllEnemies;
      for (int i = 0; i < allEnemies.Count; i++)
      {
        AIActor aIActor = allEnemies[i];
        if (aIActor.IsNormalEnemy && aIActor.renderer.isVisible && aIActor.healthHaver.IsAlive && !aIActor.IsGone)
          m_targets.Add(aIActor);
        if (maxTargets > 0 && m_targets.Count >= maxTargets)
          break;
      }
    }
    else if (maxTargets <= 0 || m_targets.Count < maxTargets)
    {
      RoomHandler absoluteRoomFromPosition = GameManager.Instance.Dungeon.data.GetAbsoluteRoomFromPosition(barrelPosition.ToIntVector2(VectorConversions.Floor));
      absoluteRoomFromPosition.GetActiveEnemies(RoomHandler.ActiveEnemyType.All, ref s_enemiesInRoom);
      if (SelectRandomTarget)
        s_enemiesInRoom = BraveUtility.Shuffle(s_enemiesInRoom);
      else
      {
        BeamSorter.barrelPosition = barrelPosition;
        s_enemiesInRoom.Sort(BeamSorter.Compare);
      }
      for (int j = 0; j < s_enemiesInRoom.Count; j++)
      {
        AIActor aIActor2 = s_enemiesInRoom[j];
        if (aIActor2.IsNormalEnemy && aIActor2.renderer.isVisible && aIActor2.healthHaver.IsAlive && !aIActor2.IsGone)
          m_targets.Add(aIActor2);
        if (maxTargets > 0 && m_targets.Count >= maxTargets)
          break;
      }
    }
    Bone.ReturnAll(ref m_bones);
    this._growthTime = 1f - Mathf.Min(this._growthTime + BraveTime.DeltaTime, _EXTEND_TIME) / _EXTEND_TIME;
    this._growthTime = 1f - (this._growthTime * this._growthTime); // cubic ease in
    Vector3? impactPos = null;
    if (m_targets.Count > 0)
    {
      Vector3 nextBoneDir = direction.normalized * _BEZIER_TIGHTNESS;

      Vector3 prevTarget  = barrelPosition;
      Vector3 startAnchor = prevTarget + nextBoneDir;
      Vector3 curTarget   = m_targets[0].specRigidbody.HitboxPixelCollider.UnitCenter;
      Vector3 endAnchor   = curTarget - nextBoneDir;

      float delta = (this.lastImpactPosition - curTarget.XY()).sqrMagnitude;
      if (delta > _SNAP_DISTANCE)
      {
        this._lockedOn = false;
        this.lastImpactPosition = Lazy.SmoothestLerp(this.lastImpactPosition, curTarget, _VINE_LERP);
        DrawMainBezierCurve(prevTarget, startAnchor, this.lastImpactPosition - nextBoneDir.XY(), this.lastImpactPosition);
      }
      else
      {
        this._lockedOn = true;
        this.lastImpactPosition = curTarget;
        DrawMainBezierCurve(prevTarget, startAnchor, endAnchor, curTarget);
        for (int k = 0; k < m_targets.Count - 1; k++)
        {
          prevTarget = m_targets[k].specRigidbody.HitboxPixelCollider.UnitCenter;
          startAnchor = prevTarget + nextBoneDir;
          nextBoneDir = Quaternion.Euler(0f, 0f, 90f) * nextBoneDir;

          curTarget = m_targets[k + 1].specRigidbody.HitboxPixelCollider.UnitCenter;
          endAnchor = curTarget + nextBoneDir;
          nextBoneDir = -nextBoneDir;

          DrawBezierCurve(prevTarget, startAnchor, endAnchor, curTarget);
        }
        nextBoneDir = nextBoneDir.normalized * _WRAP_TIGHTNESS;
        while (_vineAngles.Count < _EXTRA_WRAPS) // randomize some parameters for nice tangly vine effects
        {
          _vineAngles.Add(UnityEngine.Random.Range(10f, 90f));
          _vineAngleDevs.Add(UnityEngine.Random.Range(5f, 15f));
          _vineLengths.Add(UnityEngine.Random.Range(1.5f, 3f));
          _vineLengthDevs.Add(UnityEngine.Random.Range(1f, 3f));
        }
        _firstWrapBoneIndex = m_bones.Count;
        float time = BraveTime.ScaledTimeSinceStartup;
        for (int i = 0; i < _EXTRA_WRAPS; ++i)
        {
          float angleDelta = this._growthTime * Mathf.Sin(_vineAngleDevs[i] * time);
          float lengthDelta = this._growthTime * Mathf.Abs(Mathf.Sin(_vineLengthDevs[i] * time));
          nextBoneDir = lengthDelta * _vineLengths[i] * nextBoneDir.normalized;
          startAnchor = curTarget + nextBoneDir;
          Vector3 midVec = Quaternion.Euler(0f, 0f, _vineAngles[i] / 2f) * nextBoneDir;
          Vector3 midTarget = curTarget + midVec;
          nextBoneDir = Quaternion.Euler(0f, 0f, _vineAngles[i]) * nextBoneDir;

          endAnchor = curTarget + nextBoneDir;
          nextBoneDir = -nextBoneDir;

          DrawBezierCurve(curTarget, curTarget + (Quaternion.Euler(0f, 0f, -_WRAP_SPREAD * angleDelta) * midVec), startAnchor, midTarget);
          DrawBezierCurve(midTarget, endAnchor, curTarget + (Quaternion.Euler(0f, 0f, _WRAP_SPREAD * angleDelta) * midVec), curTarget);
        }
      }
      if (ImpactRenderer)
        ImpactRenderer.renderer.enabled = false;
    }
    else
    {
      this._lockedOn = false;
      Vector3 initialAngle = Quaternion.Euler(0f, 0f, Mathf.PingPong(Time.realtimeSinceStartup * 15f, 60f) - 30f) * direction.normalized * _BEZIER_TIGHTNESS;
      Vector3 barrelPosition3D = barrelPosition;
      RaycastResult result;
      bool haveCollision = PhysicsEngine.Instance.RaycastWithIgnores(unitOrigin: barrelPosition, direction: direction, dist: _RAYCAST_DIST, out result,
        collideWithTiles: true, collideWithRigidbodies: true, rayMask: _BEAM_MASK, sourceLayer: null, collideWithTriggers: false, rigidbodyExcluder: null,
        ignoreList: GetIgnoreRigidbodies());
      Vector3 beamEnd = barrelPosition3D + (direction.normalized * _RAYCAST_DIST).ToVector3ZUp();
      if (haveCollision)
      {
        beamEnd = result.Contact;
        m_hitRigidbody = result.SpeculativeRigidbody;
      }
      RaycastResult.Pool.Free(ref result);
      impactPos = beamEnd;

      float delta = (this.lastImpactPosition - beamEnd.XY()).sqrMagnitude;
      this.lastImpactPosition = (delta > _SNAP_DISTANCE) ? Lazy.SmoothestLerp(this.lastImpactPosition, beamEnd, _VINE_LERP) : beamEnd;
      beamEnd = this.lastImpactPosition;

      DrawMainBezierCurve(barrelPosition3D, barrelPosition3D + initialAngle, beamEnd - initialAngle, beamEnd);
      if (ImpactRenderer)
        ImpactRenderer.renderer.enabled = false;
    }
    LinkedListNode<Bone> linkedListNode = m_bones.First;
    while (linkedListNode != null && linkedListNode != m_bones.Last)
    {
      linkedListNode.Value.normal = (Quaternion.Euler(0f, 0f, 90f) * (linkedListNode.Next.Value.pos - linkedListNode.Value.pos)).normalized;
      linkedListNode = linkedListNode.Next;
    }
    if (m_bones.Count > 0)
      m_bones.Last.Value.normal = m_bones.Last.Previous.Value.normal;
    m_isDirty = true;
    if (ImpactRenderer)
    {
      ImpactRenderer.renderer.enabled = true;
      if (m_targets.Count == 0)
      {
        ImpactRenderer.transform.position = ((!impactPos.HasValue) ? (base.Gun.CurrentOwner as PlayerController).unadjustedAimPoint.XY() : impactPos.Value.XY());
        ImpactRenderer.IsPerpendicular = false;
      }
      else
      {
        ImpactRenderer.transform.position = m_targets[m_targets.Count - 1].CenterPosition;
        ImpactRenderer.IsPerpendicular = true;
      }
      ImpactRenderer.HeightOffGround = 6f;
      ImpactRenderer.UpdateZDepth();
    }
  }

  public override void LateUpdatePosition(Vector3 origin)
  {
  }

  public override void CeaseAttack()
  {
    DestroyBeam();
  }

  public override void DestroyBeam()
  {
    UnityEngine.Object.Destroy(base.gameObject);
  }

  public override void OnDestroy()
  {
    Bone.ReturnAll(ref m_bones);
    base.OnDestroy();
  }

  public override void AdjustPlayerBeamTint(Color targetTintColor, int priority, float lerpTime = 0f)
  {
  }

  private const int _BEZIER_CURVE_SEGMENTS = 20;
  private void DrawBezierCurve(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
  {
    Vector3 curveStart = BraveMathCollege.CalculateBezierPoint(0f, p0, p1, p2, p3);
    float approxLength = 0f;
    for (int i = 1; i <= _BEZIER_CURVE_SEGMENTS; i++)
    {
      Vector2 curveEnd = BraveMathCollege.CalculateBezierPoint((float)i / _BEZIER_CURVE_SEGMENTS, p0, p1, p2, p3);
      approxLength += Vector2.Distance(curveStart, curveEnd);
      curveStart = curveEnd;
    }
    float approxPixelLength = c_bonePixelLength * approxLength;
    curveStart = BraveMathCollege.CalculateBezierPoint(0f, p0, p1, p2, p3);
    if (m_bones.Count == 0)
      m_bones.AddLast(Bone.Rent(curveStart));
    for (int j = 1; j <= approxPixelLength; j++)
      m_bones.AddLast(Bone.Rent(BraveMathCollege.CalculateBezierPoint(j / approxPixelLength, p0, p1, p2, p3)));
  }

  private void DrawMainBezierCurve(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
  {
    DrawBezierCurve(p0, p1, p2, p3);
    mainBezierPoint1 = p0;
    mainBezierPoint2 = p1;
    mainBezierPoint3 = p2;
    mainBezierPoint4 = p3;
  }

  public Vector2 GetPointOnMainBezier(float t)
  {
    return BraveMathCollege.CalculateBezierPoint(t, mainBezierPoint1, mainBezierPoint2, mainBezierPoint3, mainBezierPoint4);
  }

  public void GetTiledSpriteGeomDesc(out int numVertices, out int numIndices, tk2dSpriteDefinition spriteDef, Vector2 dimensions)
  {
    int segments = Mathf.Max(m_bones.Count - 1, 0);
    numVertices = segments * 4;
    numIndices = segments * 6;
  }

  public void SetTiledSpriteGeom(Vector3[] pos, Vector2[] uv, int offset, out Vector3 boundsCenter, out Vector3 boundsExtents, tk2dSpriteDefinition spriteDef, Vector3 scale, Vector2 dimensions, tk2dBaseSprite.Anchor anchor, float colliderOffsetZ, float colliderExtentZ)
  {
    int spritePixelLength = Mathf.RoundToInt(spriteDef.untrimmedBoundsDataExtents.x / spriteDef.texelSize.x);
    int numSubtilesInSprite = spritePixelLength / 4;
    int lastBoneIndex = Mathf.Max(m_bones.Count - 1, 0);
    int totalSpritesToDraw = Mathf.CeilToInt((float)lastBoneIndex / (float)numSubtilesInSprite);
    boundsCenter = (m_minBonePosition + m_maxBonePosition) / 2f;
    boundsExtents = (m_maxBonePosition - m_minBonePosition) / 2f;
    LinkedListNode<Bone> linkedListNode = m_bones.First;
    int verticesDrawn = 0;
    int animationFrame = Mathf.FloorToInt(Mathf.Repeat(m_globalTimer * m_animationClip.fps, m_animationClip.frames.Length));
    for (int i = 0; i < totalSpritesToDraw; i++)
    {
      int lastSubtileIndex = numSubtilesInSprite - 1;
      if (i == totalSpritesToDraw - 1 && lastBoneIndex % numSubtilesInSprite != 0)
      {
        lastSubtileIndex = lastBoneIndex % numSubtilesInSprite - 1;
      }
      tk2dSpriteDefinition segmentSprite = spriteDef;
      if (usesStartAnimation && i == 0)
      {
        int startAnimationFrame = Mathf.FloorToInt(Mathf.Repeat(m_globalTimer * m_startAnimationClip.fps, m_startAnimationClip.frames.Length));
        segmentSprite = m_sprite.Collection.spriteDefinitions[m_startAnimationClip.frames[startAnimationFrame].spriteId];
      }
      else
        segmentSprite = m_sprite.Collection.spriteDefinitions[m_animationClip.frames[animationFrame].spriteId];
      float numSpritesDrawn = 0f;
      for (int j = 0; j <= lastSubtileIndex; j++)
      {
        float fractionOfSubtileToDraw = 1f;
        if (i == totalSpritesToDraw - 1 && j == lastSubtileIndex)
          fractionOfSubtileToDraw = Vector2.Distance(linkedListNode.Next.Value.pos, linkedListNode.Value.pos);
        int uvCurrent = offset + verticesDrawn;
        Bone curBone = linkedListNode.Value;
        Bone nextBone = linkedListNode.Next.Value;
        pos[uvCurrent++] = (curBone.pos  + curBone.normal  * (segmentSprite.position0.y * m_projectileScale) - m_minBonePosition).ToVector3ZUp(0f);
        pos[uvCurrent++] = (nextBone.pos + nextBone.normal * (segmentSprite.position1.y * m_projectileScale) - m_minBonePosition).ToVector3ZUp(0f);
        pos[uvCurrent++] = (curBone.pos  + curBone.normal  * (segmentSprite.position2.y * m_projectileScale) - m_minBonePosition).ToVector3ZUp(0f);
        pos[uvCurrent++] = (nextBone.pos + nextBone.normal * (segmentSprite.position3.y * m_projectileScale) - m_minBonePosition).ToVector3ZUp(0f);
        Vector2 minUV = Vector2.Lerp(segmentSprite.uvs[0], segmentSprite.uvs[1], numSpritesDrawn);
        Vector2 maxUV = Vector2.Lerp(segmentSprite.uvs[2], segmentSprite.uvs[3], numSpritesDrawn + fractionOfSubtileToDraw / numSubtilesInSprite);
        uvCurrent = offset + verticesDrawn;
        uv[uvCurrent++] = minUV;
        uv[uvCurrent++] = new Vector2(maxUV.x, minUV.y);
        uv[uvCurrent++] = new Vector2(minUV.x, maxUV.y);
        uv[uvCurrent++] = maxUV;
        verticesDrawn += 4;
        numSpritesDrawn += fractionOfSubtileToDraw / m_spriteSubtileWidth;
        if (linkedListNode != null)
          linkedListNode = linkedListNode.Next;
      }
    }
  }
}
