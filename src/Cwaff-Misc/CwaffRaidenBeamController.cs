namespace CwaffingTheGungy;

public class CwaffRaidenBeamController : BeamController
{
  public enum TargetType
  {
    Screen = 10,
    Room = 20
  }

  public string beamAnimation;
  public bool usesStartAnimation;
  public string startAnimation;
  public tk2dBaseSprite ImpactRenderer;
  public string EnemyImpactAnimation;
  public string BossImpactAnimation;
  public string OtherImpactAnimation;
  public TargetType targetType = TargetType.Screen;
  public int maxTargets = -1;
  public bool SelectRandomTarget;

  private List<AIActor> s_enemiesInRoom = new List<AIActor>();
  private tk2dTiledSprite m_sprite;
  private bool m_isCurrentlyFiring = true;
  private bool m_audioPlaying;
  private List<AIActor> m_targets = new List<AIActor>();
  private SpeculativeRigidbody m_hitRigidbody;
  private bool m_isDirty;
  private float _growthTime = 0.0f;
  private bool _lockedOn = false;
  private int _firstWrapBoneIndex;
  private Vector2 lastImpactPosition;
  private CwaffBoneManager _boneManager;

  private Vector3 mainBezierPoint1;
  private Vector3 mainBezierPoint2;
  private Vector3 mainBezierPoint3;
  private Vector3 mainBezierPoint4;

  private const float _EXTEND_TIME = 1.25f;
  private const int c_segmentCount = 20;
  private const int c_bonePixelLength = 4;
  private const float c_boneUnitLength = 0.25f;
  private const float c_trailHeightOffset = 0.5f;

  public override bool ShouldUseAmmo => true;

  public void Start()
  {
    base.transform.parent = SpawnManager.Instance.VFX;
    float projectileScale = 1f;
    if (base.projectile.Owner is PlayerController playerController)
      projectileScale = playerController.BulletScaleModifier;
    tk2dSpriteAnimationClip animation = base.spriteAnimator.GetClipByName(beamAnimation);
    tk2dSpriteAnimationClip startAnimationClip = null;
    if (usesStartAnimation)
      startAnimationClip = base.spriteAnimator.GetClipByName(startAnimation);

    this.m_sprite = base.gameObject.GetOrAddComponent<tk2dTiledSprite>();
    this._boneManager = base.gameObject.AddComponent<CwaffBoneManager>();
    this._boneManager.Setup(animation: animation, startAnimation: startAnimationClip, projectileScale: projectileScale);

    if (ImpactRenderer)
      ImpactRenderer.transform.localScale = new Vector3(projectileScale, projectileScale, 1f);
    this.lastImpactPosition = base.Origin;
  }

  public void Update()
  {
    this._boneManager.UpdateTimers();
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
      damage *= playerController.stats.GetStatValue(StatType.RateOfFire);
      damage *= playerController.stats.GetStatValue(StatType.Damage);
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

    m_sprite.HeightOffGround = 0.5f;

    Vector3 oldPosition = base.transform.position;
    this._boneManager.ManualLateUpdate();
    if (ImpactRenderer)
    {
      Vector3 vector = (base.transform.position - oldPosition).WithZ(0f);
      ImpactRenderer.transform.position -= vector;
    }
    m_isDirty = false;
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
        if (m_targets.Contains(aIActor2))
          continue;
        if (aIActor2.IsNormalEnemy && aIActor2.renderer.isVisible && aIActor2.healthHaver.IsAlive && !aIActor2.IsGone)
          m_targets.Add(aIActor2);
        if (maxTargets > 0 && m_targets.Count >= maxTargets)
          break;
      }
    }
    this._boneManager.ReturnAllBones();
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
        while (_vineAngles.Count < _EXTRA_WRAPS) // randomize some parameters for nice tangly vine effects
        {
          _vineAngles.Add(UnityEngine.Random.Range(10f, 90f));
          _vineAngleDevs.Add(UnityEngine.Random.Range(5f, 15f));
          _vineLengths.Add(UnityEngine.Random.Range(1.5f, 3f));
          _vineLengthDevs.Add(UnityEngine.Random.Range(1f, 3f));
        }

        this._lockedOn = true;
        this.lastImpactPosition = curTarget;
        DrawMainBezierCurve(prevTarget, startAnchor, endAnchor, curTarget);
        for (int k = 0; k < m_targets.Count; k++)
        {
          nextBoneDir = nextBoneDir.normalized * _WRAP_TIGHTNESS;
          _firstWrapBoneIndex = this._boneManager.BoneCount();
          float time = BraveTime.ScaledTimeSinceStartup;

          // draw wrapping VFX around target
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

          // draw bezier to next target
          if (k < m_targets.Count - 1)
          {
            prevTarget = m_targets[k].specRigidbody.HitboxPixelCollider.UnitCenter;
            startAnchor = prevTarget + nextBoneDir;
            nextBoneDir = Quaternion.Euler(0f, 0f, 90f) * nextBoneDir;

            curTarget = m_targets[k + 1].specRigidbody.HitboxPixelCollider.UnitCenter;
            endAnchor = curTarget + nextBoneDir;
            nextBoneDir = -nextBoneDir;

            DrawBezierCurve(prevTarget, startAnchor, endAnchor, curTarget);
          }
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

    this._boneManager.RecomputeNormals();

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
    if (!this._boneManager.HasBones())
      this._boneManager.RentBone(curveStart);
    for (int j = 1; j <= approxPixelLength; j++)
      this._boneManager.RentBone(BraveMathCollege.CalculateBezierPoint(j / approxPixelLength, p0, p1, p2, p3));
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
}
