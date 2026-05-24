namespace CwaffingTheGungy;

using static CwaffingTheGungy.RopeSim; // StretchPolicy

/// <summary>Class for creating massless rope sprites with known start and end points.</summary>
public class CwaffRopeMesh : MonoBehaviour
{
  private const float UPDATE_RATE = 1.0f / 60.0f; // seconds between rope updates (needs to be fixed due to verlet integration)
  private const float SEGLENGTH   = 0.25f; // should always be 1/4 since sprites are divided into 4 subtiles

  private static readonly FieldInfo _PointsBackingArray = typeof(List<Vector2>).GetField("_items", BindingFlags.NonPublic | BindingFlags.Instance);

  public tk2dSpriteAnimationClip animation;
  public Vector2 startPos;
  public Vector2 endPos;
  public bool locked; // if true, prevents the rope from updating
  public bool animateWhileLocked; // if true, keeps updating the rope even while locked
  public tk2dTiledSprite sprite;

  private List<Vector2> _ropePrevPoints;
  private List<Vector2> _ropePoints;
  private Vector2[] _ropePrevPointsBacking;
  private Vector2[] _ropePointsBacking;
  private float _segLength;
  private float _updateTimer;
  private StretchPolicy _stretchPolicy;
  private float _softMaxRopeLength;
  private int _numSegments;
  private float _lockThreshold; // if > 0, locks the rope once it's stopped moving
  private CwaffBoneManager _boneManager;
  private int _cachedCapacity = -1;

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
    // System.Diagnostics.Stopwatch ropesimWatch = System.Diagnostics.Stopwatch.StartNew();
    if (this._cachedCapacity != this._ropePoints.Capacity)
    {
      this._ropePrevPointsBacking = (Vector2[])_PointsBackingArray.GetValue(this._ropePrevPoints);
      this._ropePointsBacking = (Vector2[])_PointsBackingArray.GetValue(this._ropePoints);
      this._cachedCapacity = this._ropePoints.Capacity;
    }
    RopeSim.SimulateRope(curStartPos, curEndPos, this._ropePointsBacking, this._ropePrevPointsBacking, numPoints: this._ropePoints.Count,
      segLength: this._segLength, deltaTime: UPDATE_RATE);
    // ropesimWatch.Stop(); System.Console.WriteLine($"    {ropesimWatch.ElapsedTicks,6} ticks ropesim of size {this._ropePoints.Count}");
    this._boneManager.ReplaceBones(this._ropePoints);
    this._boneManager.RecomputeNormals();
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
    public static void SimulateRope(Vector2 start, Vector2 end, Vector2[] pointsArray, Vector2[] prevPointsArray, int numPoints, float segLength = 100.0f, float deltaTime = 1.0f / 60.0f,
      int verletIters = DEFAULT_VERLET_ITERATIONS, float damping = DEFAULT_DAMPING, float gravX = DEFAULT_GRAVITY_X, float gravY = DEFAULT_GRAVITY_Y)
    {
        const float VERLET_THRESHOLD = 0.01f; // if no point moves more than this much, end verlet iteration early
        // set up some local constants
        int last                 = numPoints - 1;
        int secondlast           = last - 1;
        float segLengthSqr       = segLength * segLength;
        float thresholdLength    = segLength + VERLET_THRESHOLD;
        float thresholdLengthSqr = thresholdLength * thresholdLength;
        float dt2                = deltaTime * deltaTime;
        float sqrdtgravX         = dt2 * gravX;
        float sqrdtgravY         = dt2 * gravY;
        unsafe
        {
            fixed (Vector2* points = pointsArray)
            fixed (Vector2* prev = prevPointsArray)
            {
                float d2, dx, dy, vx, vy, invD, dirX, dirY;
                Vector2* b, p;
                bool doneEarly;
                // pin endpoints
                points->x = prev->x = start.x;
                points->y = prev->y = start.y;
                p         = points + last;
                b         = prev + last;
                p->x      = b->x = end.x;
                p->y      = b->y = end.y;
                // verlet integration
                for (int i = 1; i < last; ++i)
                {
                    p    = points + i;
                    b    = prev + i;
                    vx   = (p->x - b->x) * damping;
                    vy   = (p->y - b->y) * damping;
                    *b   = *p;
                    p->x += vx + sqrdtgravX;
                    p->y += vy + sqrdtgravY;
                }
                // constraint solve with optimized math
                for (int it = 0; it != verletIters; it++)
                {
                    doneEarly = true;
                    b         = points;
                    p         = points + 1;
                    dx        = p->x - b->x;
                    dy        = p->y - b->y;
                    d2        = dx * dx + dy * dy;
                    if (d2 > segLengthSqr)
                    {
                      invD = 1f - segLength / Mathf.Sqrt(d2);
                      p->x -= invD * dx;
                      p->y -= invD * dy;
                    }
                    for (int i = 1; i != secondlast; i++)
                    {
                        b  = p;
                        p  += 1;
                        dx = p->x - b->x;
                        dy = p->y - b->y;
                        d2 = dx * dx + dy * dy;
                        if (d2 > segLengthSqr)
                        {
                          if (d2 > thresholdLengthSqr)
                              doneEarly = false; // speedup + avoids stretching the rope at the beginning
                          invD = 0.5f * (1f - segLength / Mathf.Sqrt(d2));
                          dirX = invD * dx;
                          dirY = invD * dy;
                          b->x += dirX;
                          b->y += dirY;
                          p->x -= dirX;
                          p->y -= dirY;
                        }
                    }
                    b  = p;
                    p  += 1;
                    dx = p->x - b->x;
                    dy = p->y - b->y;
                    d2 = dx * dx + dy * dy;
                    if (d2 > segLengthSqr)
                    {
                      invD = 1f - segLength / Mathf.Sqrt(d2);
                      b->x += invD * dx;
                      b->y += invD * dy;
                    }
                    if (doneEarly)
                      return; // early break if few adjustments needed to be made
                }
            }
        }
        return;
    }
}
