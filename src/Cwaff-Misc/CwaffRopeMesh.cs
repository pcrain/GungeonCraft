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

  private List<Vector2> _ropePrevPoints;
  private List<Vector2> _ropePoints;
  private float _segLength;
  private float _updateTimer;
  private StretchPolicy _stretchPolicy;
  private float _softMaxRopeLength;
  private int _numSegments;
  private float _lockThreshold; // if > 0, locks the rope once it's stopped moving
  private CwaffBoneManager _boneManager;

  public static CwaffRopeMesh Create(tk2dSpriteAnimationClip animation, Vector2 startPos, Vector2 endPos, int numSegments, float segLength,
    string name = null, StretchPolicy stretchPolicy = StretchPolicy.STRETCH)
  {
      CwaffRopeMesh mesh   = new GameObject(name ?? "new CwaffRopeMesh", typeof(CwaffRopeMesh)).GetComponent<CwaffRopeMesh>();
      mesh.animation       = animation;
      mesh.startPos        = startPos;
      mesh.endPos          = endPos;
      mesh.sprite          = mesh.GetOrAddComponent<tk2dTiledSprite>();
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
    this._boneManager = base.gameObject.AddComponent<CwaffBoneManager>();
    this._boneManager.Setup(animation: animation);
    _updateTimer = 0.0f;
  }

  private void Update()
  {
    if (this.locked)
      return;
    this._boneManager.UpdateTimers();
    this._updateTimer += BraveTime.DeltaTime;
    if (this._updateTimer < UPDATE_RATE)
      return; // rope updates need to happen at a fixed time rate due to verlet integration silliness
    this._updateTimer -= UPDATE_RATE;

    UpdateRope();
    this._boneManager.RecomputeNormals();
  }

  private void LateUpdate()
  {
    if (!this.locked)
      this._boneManager.ManualLateUpdate();
  }

  private void UpdateRope()
  {
    this._boneManager.ReturnAllBones();
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
    RopeSim.SimulateRope(curStartPos, curEndPos, this._ropePoints, this._ropePrevPoints,
      minSegLength: this._segLength, maxSegLength: this._segLength, updateRate: UPDATE_RATE);
    for (int j = 0; j < this._ropePoints.Count; j++)
      this._boneManager.RentBone(this._ropePoints[j]);
    if (this._lockThreshold > 0f)
    {
      float maxMovement = 0.0f;
      for (int i = this._ropePoints.Count - 1; i >= 0; --i)
        maxMovement = Mathf.Max(maxMovement, (this._ropePoints[i] - this._ropePrevPoints[i]).sqrMagnitude);
      if (maxMovement <= (this._lockThreshold * this._lockThreshold))
        this.locked = true;
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
