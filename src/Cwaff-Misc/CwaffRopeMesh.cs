namespace CwaffingTheGungy;

using static CwaffingTheGungy.RopeSim; // StretchPolicy

/// <summary>Class for creating massless rope sprites with known start and end points.</summary>
public class CwaffRopeMesh : MonoBehaviour
{
  private const float UPDATE_RATE = 1.0f / 60.0f; // seconds between rope updates (needs to be fixed due to verlet integration)

  public tk2dSpriteAnimationClip animation;
  public Vector2 startPos;
  public Vector2 endPos;
  public bool locked; // if true, prevents the rope from updating
  public tk2dTiledSprite sprite;

  private int m_spriteSubtileWidth;
  private LinkedList<CwaffBone> m_bones = new LinkedList<CwaffBone>();
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

  public static CwaffRopeMesh Create(tk2dSpriteAnimationClip animation, Vector2 startPos, Vector2 endPos, int numSegments, float segLength,
    string name = null, StretchPolicy stretchPolicy = StretchPolicy.STRETCH)
  {
      CwaffRopeMesh mesh   = new GameObject(name ?? "new CwaffRopeMesh", typeof(CwaffRopeMesh)).GetComponent<CwaffRopeMesh>();
      mesh.animation       = animation;
      mesh.startPos        = startPos;
      mesh.endPos          = endPos;
      mesh.sprite          = mesh.AddComponent<tk2dTiledSprite>();
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
    if (!sprite)
      sprite = this.AddComponent<tk2dTiledSprite>();
    sprite.collection = animation.frames[0].spriteCollection;
    sprite.spriteId = animation.frames[0].spriteId;
    sprite.OverrideGetTiledSpriteGeomDesc = GetTiledSpriteGeomDesc;
    sprite.OverrideSetTiledSpriteGeom = SetTiledSpriteGeom;
    tk2dSpriteDefinition currentSpriteDef = sprite.collection.spriteDefinitions[sprite.spriteId];
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
    for (LinkedListNode<CwaffBone> n = m_bones.First; n != m_bones.Last; n = n.Next)
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
    for (LinkedListNode<CwaffBone> n = m_bones.First; n != null; n = n.Next)
    {
      m_minBonePosition = Vector2.Min(m_minBonePosition, n.Value.pos);
      m_maxBonePosition = Vector2.Max(m_maxBonePosition, n.Value.pos);
    }
    base.transform.position = new Vector3(m_minBonePosition.x, m_minBonePosition.y);
    sprite.ForceBuild();
    sprite.UpdateZDepth();
  }

  private void OnDestroy()
  {
    CwaffBone.ReturnAll(ref m_bones);
  }

  private void UpdateRope()
  {
    CwaffBone.ReturnAll(ref m_bones);
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
      m_bones.AddLast(CwaffBone.Rent(this._ropePoints[j]));
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
    LinkedListNode<CwaffBone> bone = m_bones.First;
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
        CwaffBone curBone = bone.Value;
        CwaffBone nextBone = bone.Next.Value;
        if (i == totalSpritesToDraw - 1 && j == lastSubtileIndex)
          fractionOfSubtileToDraw = Vector2.Distance(nextBone.pos, curBone.pos);
        int uvCurrent = offset + verticesDrawn;
        pos[uvCurrent++] = (curBone.pos  + curBone.normal  * segmentSprite.position0.y - m_minBonePosition).ToVector3ZUp(0f);
        pos[uvCurrent++] = (nextBone.pos + nextBone.normal * segmentSprite.position1.y - m_minBonePosition).ToVector3ZUp(0f);
        pos[uvCurrent++] = (curBone.pos  + curBone.normal  * segmentSprite.position2.y - m_minBonePosition).ToVector3ZUp(0f);
        pos[uvCurrent++] = (nextBone.pos + nextBone.normal * segmentSprite.position3.y - m_minBonePosition).ToVector3ZUp(0f);
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
      GROWTEMPORARY, // rope temporarily adds segments to visually render to its current length, but shrinks back as its able to
      GROWPERMANENT, // rope permanently adds segments to visually render to its current length
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
