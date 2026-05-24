namespace CwaffingTheGungy;

using static CwaffingTheGungy.RopeSim; // StretchPolicy

/// <summary>Class for creating massless rope sprites with known start and end points.</summary>
public class CwaffRopeMesh : MonoBehaviour
{
  private const float UPDATE_RATE = 1.0f / 60.0f; // seconds between rope updates (needs to be fixed due to verlet integration)
  private const float SEGLENGTH   = 0.25f; // should always be 1/4 since sprites are divided into 4 subtiles

  public tk2dSpriteAnimationClip animation;
  public Vector2 startPos;
  public Vector2 endPos;
  public bool locked; // if true, prevents the rope from updating
  public bool animateWhileLocked; // if true, keeps updating the rope even while locked
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

  public static CwaffRopeMesh Create(tk2dSpriteAnimationClip animation, Vector2 startPos, Vector2 endPos, int numSegments,
    string name = null, StretchPolicy stretchPolicy = StretchPolicy.STRETCH)
  {
      CwaffRopeMesh mesh   = new GameObject(name ?? "new CwaffRopeMesh", typeof(CwaffRopeMesh)).GetComponent<CwaffRopeMesh>();
      mesh.animation       = animation;
      mesh.startPos        = startPos;
      mesh.endPos          = endPos;
      mesh._boneManager    = mesh.gameObject.AddComponent<CwaffBoneManager>();
      mesh._boneManager.Setup(animation: animation);
      mesh.sprite          = mesh.GetComponent<tk2dTiledSprite>(); // guaranteed set up by CwaffBoneManager.Setup()
      mesh._segLength      = SEGLENGTH;
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
      mesh._softMaxRopeLength = SEGLENGTH * numSegments;
      mesh.locked = false;
      mesh.animateWhileLocked = false;
      mesh._lockThreshold = 0f;
      mesh._updateTimer = 0.0f;
      return mesh;
  }

  public void LockWhenStationary(float threshold = 0.01f, bool keepAnimating = false)
  {
    this._lockThreshold = threshold;
    this.animateWhileLocked = keepAnimating;
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
    else if (this.animateWhileLocked)
      this._boneManager.UpdateAnimations();
  }

  private void UpdateRope()
  {
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
      case StretchPolicy.STRETCH:
      default:
        break;
    }
    // System.Diagnostics.Stopwatch ropesimeWatch = System.Diagnostics.Stopwatch.StartNew();
    RopeSim.SimulateRope(curStartPos, curEndPos, this._ropePoints, this._ropePrevPoints, segLength: this._segLength, deltaTime: UPDATE_RATE);
    // ropesimeWatch.Stop(); System.Console.WriteLine($"    {ropesimeWatch.ElapsedTicks,6} ticks ropesim of size {this._ropePoints.Count}");
    // for (int j = 0; j < this._ropePoints.Count; j++)
    //   this._boneManager.RentBone(this._ropePoints[j]);
    this._boneManager.ReplaceBones(this._ropePoints);
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
    private const int DEFAULT_VERLET_ITERATIONS = 25; // NOTE: stops iterating if chain is sufficiently constrained
    private const float DEFAULT_DAMPING         = 0.98f;
    private const float DEFAULT_GRAVITY_X       = 0.0f;
    private const float DEFAULT_GRAVITY_Y       = -1.5f;

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
    public static void SimulateRope(Vector2 start, Vector2 end, List<Vector2> points, List<Vector2> prevPoints, float segLength = 100.0f, float deltaTime = 1.0f / 60.0f,
      int verletIters = DEFAULT_VERLET_ITERATIONS, float damping = DEFAULT_DAMPING, float gravX = DEFAULT_GRAVITY_X, float gravY = DEFAULT_GRAVITY_Y)
    {
        const float VERLET_THRESHOLD = 0.01f; // if no point moves more than this much, end verlet iteration early

        // setup config
        int last           = points.Count - 1;
        int secondlast     = last - 1;
        float segLengthSqr = segLength * segLength;
        float thresholdLength = segLength + VERLET_THRESHOLD;
        float thresholdLengthSqr = thresholdLength * thresholdLength;

        // do verlet integration
        float dt2         = deltaTime * deltaTime;
        Vector2 sqrdtgrav = new Vector2(dt2 * gravX, dt2 * gravY);
        for (int i = 1; i < last; i++)
        {
            Vector2 velocity = (points[i] - prevPoints[i]) * damping;
            prevPoints[i] = points[i];
            points[i] += velocity + sqrdtgrav;
        }

        // pin endpoints
        points[0]    = prevPoints[0]    = start;
        points[last] = prevPoints[last] = end;

        // constraint solve with optimized math
        float d2, dx, dy, invD, dirX, dirY;
        bool doneEarly;
        for (int it = 0; it != verletIters; it++)
        {
            doneEarly = true;

            dx = points[1].x - points[0].x;
            dy = points[1].y - points[0].y;
            d2 = dx * dx + dy * dy;
            if (d2 > segLengthSqr)
            {
              invD      = 1f - segLength / Mathf.Sqrt(d2);
              points[1] = new Vector2(points[1].x - invD * dx, points[1].y - invD * dy);
            }

            for (int i = 1; i != secondlast; i++)
            {
                dx = points[i + 1].x - points[i].x;
                dy = points[i + 1].y - points[i].y;
                d2 = dx * dx + dy * dy;
                if (d2 > segLengthSqr)
                {
                  if (d2 > thresholdLengthSqr)
                      doneEarly = false;
                  invD          = 0.5f * (1f - segLength / Mathf.Sqrt(d2));
                  dirX          = invD * dx;
                  dirY          = invD * dy;
                  points[i]     = new Vector2(points[i].x + dirX, points[i].y + dirY);
                  points[i + 1] = new Vector2(points[i + 1].x - dirX, points[i + 1].y - dirY);
                }
            }

            dx = points[last].x - points[secondlast].x;
            dy = points[last].y - points[secondlast].y;
            d2 = dx * dx + dy * dy;
            if (d2 > segLengthSqr)
            {
              invD               = 1f - segLength / Mathf.Sqrt(d2);
              points[secondlast] = new Vector2(points[secondlast].x + invD * dx, points[secondlast].y + invD * dy);
            }

            if (doneEarly)
              return; // early break if few adjustments needed to be made
        }

        return;
    }
}
