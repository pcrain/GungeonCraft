namespace CwaffingTheGungy;

/* References
  - Quadratic Bezier Sampling: https://stackoverflow.com/questions/6711707/draw-a-quadratic-b%C3%A9zier-curve-through-three-given-points
  - Alt: https://stackoverflow.com/questions/5634460/quadratic-b%C3%A9zier-curve-calculate-points
  - Cubic Bezier Sampling: https://web.archive.org/web/20131225210855/http://people.sc.fsu.edu/~jburkardt/html/bezier_interpolation.html
  - General Ellipse Equation: https://www.maa.org/external_archive/joma/Volume8/Kalman/General.html
  - Ellipse Point Sampling: https://math.stackexchange.com/questions/172766/calculating-equidistant-points-around-an-ellipse-arc
  - Rose Curves: https://en.wikipedia.org/wiki/Rose_(mathematics)
  - Spline Interpolation in C#: https://swharden.com/blog/2022-01-22-spline-interpolation/
  - Pendulum Physics: https://www.cfm.brown.edu/people/dobrush/am34/Mathematica/ch3/dpendulum.html
  - Lissajous Curves: https://en.wikipedia.org/wiki/Lissajous_curve
*/
/* Positioning Options
  - TangentToPlayerRelativeToCurrentVelocity()
  - NearestPlayer()
  - ReflectCurrentPositionAcrossMidpoint()
*/
/* Movement Options
  - LaunchTowards()
  - AccelerateTowards()
  - MoveTangentTo()
  - GravitateTowards()
  - OrbitPosition()
  - OrbitObject()
  - SineWaveTowards()
  - ParabolaTowards()
  - AngularAccelerateTowards()
  - HomeTowards()
  - RicochetToPoint()
*/

public abstract class PathShape
{
  // Get a point on the path between 0 and 1
  public abstract Vector2 At(float t);

  // Uniformly sample n points on a path
  public List<Vector2> SampleUniform(int numPoints, float start = 0f, float end = 1f)
  {
    List<Vector2> points = new List<Vector2>();
    float delta = (end-start)/(numPoints+1);
    for(int i = 1; i <= numPoints; ++i)
    {
      float offset = start+i*delta;
      points.Add(At(offset % 1f));
    }
    return points;
  }

  // Uniformly sample n points on a path, including endpoints
  public List<Vector2> SampleUniformInclusive(int numPoints, float start = 0f, float end = 1f)
  {
    List<Vector2> points = new List<Vector2>();
    float delta = (end-start)/numPoints;
    for(int i = 0; i < numPoints; ++i)
    {
      float offset = start+i*delta;
      points.Add(At(offset % 1f));
    }
    return points;
  }
}

public class PathLine : PathShape
{
  public Vector2 start {get; private set; }
  public Vector2 end {get; private set; }
  public PathLine(Vector2 start, Vector2 end)
  {
    this.start = start;
    this.end = end;
  }
  public PathLine(float x1, float y1, float x2, float y2)
  {
    this.start = new Vector2(x1,y1);
    this.end = new Vector2(x2,y2);
  }
  public override Vector2 At(float t)
    { return (1.0f-t)*this.start + t*this.end; }

  // Stretch a line to t% of its original length along the center
  public PathLine Stretch(float t)
  {
    // (2,3) to (6,4)
    Vector2 midpoint = 0.5f*(this.end + this.start);
    Vector2 halfline = 0.5f*(this.end - this.start);
    return new PathLine(midpoint - t*halfline, midpoint + t*halfline);
  }
}

public class PathPolyLine : PathShape
{
  public List<Vector2> points  {get; private set; }
  public List<float> cumulativeLength {get; private set; }
  public List<float> offsets {get; private set; }
  public PathPolyLine(params Vector2[] points)
  {
    if (points.Length < 2)
      return;
    this.points = points.ToList();

    this.cumulativeLength = new List<float>(this.points.Count);
    this.cumulativeLength[0] = 0.0f;
    for(int i = 1; i < this.points.Count; ++i)
      this.cumulativeLength[i] = this.cumulativeLength[i-1] + (this.points[i]-this.points[i-1]).magnitude;

    this.offsets = new List<float>(this.points.Count);
    for(int i = 0; i < this.points.Count; ++i)
      this.offsets[i] = this.cumulativeLength[i] / this.cumulativeLength[this.points.Count-1];
  }
  public override Vector2 At(float t)
  {
    // handle extreme cases
    if (t <= 0.0f)
      return this.points[0];
    if (t >= 1.0f)
      return this.points[this.points.Count-1];

    // get the proper segment relative to interpretation
    int i = 1;
    while(this.offsets[i] < t)
      ++i;

    // adjust our t value and interpolate between the polyline segments
    float adjt = (t - this.offsets[i-1]) / (this.offsets[i] - this.offsets[i-1]);
    return (1.0f-adjt)*this.points[i-1] + adjt*this.points[i];
  }
}

public class PathRect : PathShape
{
  public Rect rect {get; private set; }

  public PathRect(Rect r)
  {
    this.rect = r;
  }

  public PathLine Top()
    { return new PathLine(this.rect.xMin,this.rect.yMax,this.rect.xMax,this.rect.yMax); }
  public PathLine Bottom()
    { return new PathLine(this.rect.xMin,this.rect.yMin,this.rect.xMax,this.rect.yMin); }
  public PathLine Left()
    { return new PathLine(this.rect.xMin,this.rect.yMin,this.rect.xMin,this.rect.yMax); }
  public PathLine Right()
    { return new PathLine(this.rect.xMax,this.rect.yMin,this.rect.xMax,this.rect.yMax); }
  public override Vector2 At(float t)
    { return this.rect.PointOnPerimeter(t); }
  public Vector2 At(float x, float y)
    { return new Vector2(this.rect.xMin + x * this.rect.width, this.rect.yMin + y * this.rect.height); }
  public float InverseAt(Vector2 p)
    { return this.rect.InversePointOnPerimeter(p); }
}

public class PathArc : PathShape
{
  const float TWOPI = 2.0f*Mathf.PI;

  public Vector2 center {get; private set; }
  public float radius {get; private set; }
  public float startAngle {get; private set; }
  public float endAngle {get; private set; }
  private float angleDelta;

  public PathArc(Vector2 center, float radius, float startAngle, float endAngle)
  {
    this.center     = center;
    this.radius     = radius;
    this.startAngle = startAngle.Clamp360();
    this.endAngle   = endAngle.Clamp360();
    this.angleDelta = ((this.endAngle - this.startAngle) + 360f) % 360f;
    if (this.angleDelta == 0)
      this.angleDelta = 360f; // assume we just want a full rotation
  }
  public override Vector2 At(float t)
    { return this.center + this.radius * (this.startAngle+t*this.angleDelta).ToVector(); }
}

public class PathCircle : PathArc
{
  public PathCircle(Vector2 center, float radius) : base(center, radius, 0f, 360f)
    {}
  public PathLine TangentLine(float angle, float length)
  {
    Vector2 midpoint = this.center + this.radius * angle.ToVector();
    Vector2 extension = (angle+90).ToVector();
    return new PathLine(midpoint-extension,midpoint+extension);
  }
  public float AngleTo(Vector2 p)
  {
    return (p-this.center).ToAngle();
  }
}

// curve that passes through 4 arbitrary points
//   (or a bezier curve as defined with two control points if using rawControlPoints)
public class PathCurve : PathShape
{
  public Vector2 p0 {get; private set; }
  public Vector2 p1 {get; private set; }
  public Vector2 p2 {get; private set; }
  public Vector2 p3 {get; private set; }
  public PathCurve(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, bool rawControlPoints = false)
  {
    this.p0     = p0;
    this.p1     = p1;
    this.p2     = p2;
    this.p3     = p3;
    if (rawControlPoints)
      return;
    // https://math.stackexchange.com/questions/301736/how-do-i-find-a-bezier-curve-that-goes-through-a-series-of-points
    // convert control points to passthrough points
    Vector2 t1 = -5f * p0 + 18 * p1 - 9 * p2 + 2 * p3;
    Vector2 t2 = -5f * p3 + 18 * p2 - 9 * p1 + 2 * p0;
    this.p1 = t1 / 6f;
    this.p2 = t2 / 6f;
  }
  // 3-argument constructor uses middle point twice
  public PathCurve(Vector2 p0, Vector2 pmid, Vector2 p3, bool rawControlPoints = false)
    : this(p0,pmid,pmid,p3,rawControlPoints) {}

  public override Vector2 At(float t)
  {
    float invt = 1.0f - t;
    return invt*invt*invt*p0 + 3*invt*invt*t*p1 + 3*invt*t*t*p2 + t*t*t*p3;
  }
}

public static class PathHelpers
{
  // a = major axis, b = minor axis, https://www.johndcook.com/blog/2013/05/05/ramanujan-circumference-ellipse/
  // Credit to Ramanujan
  public static float ApproximatePerimeterOfEllipse(float a, float b)
  {
    float lambda = (a-b)/(a+b);
    float c = 3.0f * lambda * lambda;
    return Mathf.PI * (a+b) * (1.0f + c / (10f + Mathf.Sqrt(4-c)));
  }

  public static Rect GetRoomBoundingRect()
  {
    Rect bounds = GameManager.Instance.BestActivePlayer.GetAbsoluteParentRoom().GetBoundingRect();
    Rect roomBounds = bounds.Inset(topInset: 2f, rightInset: 2f, bottomInset: 4f, leftInset: 2f);
    return roomBounds;
  }

  public static PathLine TangentLine(this Vector2 self, Vector2 other, float length)
  {
    Vector2 extension = 0.5f * length * (self-other).normalized.Rotate(90f);
    return new PathLine(self-extension,self+extension);
  }
}

public static class BulletPatterns
{
  public static List<Vector2> SortByX(this List<Vector2> self, bool reverse = false)
    { return (reverse ? self.OrderByDescending(v => v.x) : self.OrderBy(v => v.x)).ToList(); }
  public static List<Vector2> SortByY(this List<Vector2> self, bool reverse = false)
    { return (reverse ? self.OrderByDescending(v => v.y) : self.OrderBy(v => v.y)).ToList(); }
  public static List<Vector2> SortByDistance(this List<Vector2> self, Vector2 other, bool reverse = false)
    { return (reverse ? self.OrderByDescending(v => (v-other).magnitude) : self.OrderBy(v => (v-other).magnitude)).ToList(); }
  public static List<Vector2> SortByAngle(this List<Vector2> self, Vector2 other, float startAngle = 0, bool reverse = false)
    {  //offset 0 = starting from right, 90 = starting from top, etc.
      return (reverse
        ? self.OrderByDescending(v => (startAngle+(v-other).ToAngle()).Clamp360())
        :           self.OrderBy(v => (startAngle+(v-other).ToAngle()).Clamp360())
        ).ToList();
    }
  public static List<Vector2> MirrorHorizontally(this List<Vector2> points, float mirrorX)
    {
      List<Vector2> newPoints = new(points);
      foreach(Vector2 p in points)
        newPoints.Add(p.WithX(2f*mirrorX-p.x));
      return newPoints;
    }
  public static List<Vector2> MirrorVertically(this List<Vector2> points, float mirrorY)
    {
      List<Vector2> newPoints = new(points);
      foreach(Vector2 p in points)
        newPoints.Add(p.WithY(2f*mirrorY-p.y));
      return newPoints;
    }
  public static List<Vector2> MirrorAcrossPoint(this List<Vector2> points, Vector2 mirrorPoint, float angle)
    {
      List<Vector2> newPoints = new(points);
      foreach(Vector2 p in points)
        newPoints.Add(2f*mirrorPoint-p);
      return newPoints;
    }
  public static IEnumerator WaitFrames(this Bullet bullet, int frames)
    { yield return bullet.Wait(frames); }
  public static IEnumerator AccelerateInCurrentDirection(this Bullet bullet, float accel, int forFrames = -1)
  {
    for (int i = 0; i != forFrames; ++i)
    {
      bullet.Speed += accel;
      bullet.UpdateVelocity();
      yield return bullet.Wait(1);
    }
    yield break;
  }
  public static IEnumerator Accelerate(this Bullet bullet, Vector2 accelVec, int forFrames = -1)
  {
    for (int i = 0; i != forFrames; ++i)
    {
      bullet.Velocity += accelVec;
      bullet.UpdateVelocity();
      yield return bullet.Wait(1);
    }
    yield break;
  }
  public static IEnumerator AccelerateTowardsPoint(this Bullet bullet, float accel, Vector2 point, int forFrames = -1, bool gravitate = false)
  {
    Vector2 accelVec = accel*(point - bullet.Position).normalized;
    for (int i = 0; i != forFrames; ++i)
    {
      bullet.Velocity += accelVec;
      bullet.UpdateVelocity();
      yield return bullet.Wait(1);
      if (gravitate) // gravitate constantly recomputes acceleration relative to the bullet's location
        accelVec = accel*(point - bullet.Position).normalized;
    }
    yield break;
  }
  public static IEnumerator GravitateTowardsPoint(this Bullet bullet, float accel, Vector2 point, int forFrames = -1)
    { return bullet.AccelerateTowardsPoint(accel, point, forFrames, true); }
  public static IEnumerator AccelerateWithTrajectoryThroughPoint(this Bullet bullet, Vector2 point, int framesToReachPoint)
  {
    Vector2 accelVec = 2.0f * (point - bullet.Position - bullet.Velocity*framesToReachPoint) / (framesToReachPoint*framesToReachPoint);
    return bullet.Accelerate(accelVec, framesToReachPoint);
  }
  public static IEnumerator AccelerateWithTrajectoryThroughTwoPoints(this Bullet bullet, Vector2 pointA, int framesA, Vector2 pointB, int framesB)
  {
    bullet.Velocity = (framesA * framesA * (pointB - bullet.Position) - framesB * framesB * (pointA - bullet.Position)) / (framesA*framesB*(framesA - framesB));
    bullet.UpdateVelocity();
    return bullet.AccelerateWithTrajectoryThroughPoint(pointB, framesB);
  }
  public static IEnumerator MoveTangentToPlayer(this Bullet bullet, PlayerController player = null)
    {
      player ??= GameManager.Instance.BestActivePlayer;
      Vector2 ppos = player.CenterPosition;
      float angle = bullet.Velocity.ToAngle();

      Vector2 p1 = bullet.Position;
      Vector2 p2 = p1 + 1000f*bullet.Velocity;
      Vector2 q1 = ppos + 500f*((angle+90f).ToVector());
      Vector2 q2 = ppos + 500f*((angle-90f).ToVector());

      Vector2 isect;
      if (!BraveUtility.LineIntersectsLine(p1,p2,q1,q2,out isect))
        yield break;

      float dist = (isect - p1).magnitude;
      int time = Mathf.CeilToInt(dist / bullet.Velocity.magnitude);
      yield return bullet.Wait(time);
      yield break;
    }
  public static IEnumerator OrbitPointAtCurrentRadiusAndVelocity(this Bullet bullet)
    { yield break; }
}
// hi

// Adapted from: https://swharden.com/blog/2022-01-22-spline-interpolation/
public static class Cubic
{
    /// <summary>
    /// Generate a smooth (interpolated) curve that follows the path of the given X/Y points
    /// </summary>
    public static List<Vector2> InterpolateXY(List<Vector2> points, int count)
    {
        double[] xs = new double[points.Count];
        double[] ys = new double[points.Count];
        for (int i = 0; i < points.Count; i++)
        {
          xs[i] = points[i].x;
          ys[i] = points[i].y;
        }

        int inputPointCount = xs.Length;
        double[] inputDistances = new double[inputPointCount];
        for (int i = 1; i < inputPointCount; i++)
        {
            double dx = xs[i] - xs[i - 1];
            double dy = ys[i] - ys[i - 1];
            double distance = Math.Sqrt(dx * dx + dy * dy);
            inputDistances[i] = inputDistances[i - 1] + distance;
        }

        double meanDistance = inputDistances.Last() / (count - 1);
        double[] evenDistances = Enumerable.Range(0, count).Select(x => x * meanDistance).ToArray();
        double[] xsOut = Interpolate(inputDistances, xs, evenDistances);
        double[] ysOut = Interpolate(inputDistances, ys, evenDistances);

        List<Vector2> ret = new List<Vector2>();
        for (int i = 0; i < xsOut.Length; ++i)
          ret.Add(new Vector2(((float)xsOut[i]),((float)ysOut[i])));
        return ret;
    }

    private static double[] Interpolate(double[] xOrig, double[] yOrig, double[] xInterp)
    {
        double[] a;
        double[] b;
        FitMatrix(xOrig, yOrig, out a, out b);

        double[] yInterp = new double[xInterp.Length];
        for (int i = 0; i < yInterp.Length; i++)
        {
            int j;
            for (j = 0; j < xOrig.Length - 2; j++)
                if (xInterp[i] <= xOrig[j + 1])
                    break;

            double dx = xOrig[j + 1] - xOrig[j];
            double t = (xInterp[i] - xOrig[j]) / dx;
            double y = (1 - t) * yOrig[j] + t * yOrig[j + 1] +
                t * (1 - t) * (a[j] * (1 - t) + b[j] * t);
            yInterp[i] = y;
        }

        return yInterp;
    }

    private static void FitMatrix(double[] x, double[] y, out double[] a, out double[] b)
    {
        int n = x.Length;
        a = new double[n - 1];
        b = new double[n - 1];
        double[] r = new double[n];
        double[] A = new double[n];
        double[] B = new double[n];
        double[] C = new double[n];

        double dx1, dx2, dy1, dy2;

        dx1 = x[1] - x[0];
        C[0] = 1.0f / dx1;
        B[0] = 2.0f * C[0];
        r[0] = 3 * (y[1] - y[0]) / (dx1 * dx1);

        for (int i = 1; i < n - 1; i++)
        {
            dx1 = x[i] - x[i - 1];
            dx2 = x[i + 1] - x[i];
            A[i] = 1.0f / dx1;
            C[i] = 1.0f / dx2;
            B[i] = 2.0f * (A[i] + C[i]);
            dy1 = y[i] - y[i - 1];
            dy2 = y[i + 1] - y[i];
            r[i] = 3 * (dy1 / (dx1 * dx1) + dy2 / (dx2 * dx2));
        }

        dx1 = x[n - 1] - x[n - 2];
        dy1 = y[n - 1] - y[n - 2];
        A[n - 1] = 1.0f / dx1;
        B[n - 1] = 2.0f * A[n - 1];
        r[n - 1] = 3 * (dy1 / (dx1 * dx1));

        double[] cPrime = new double[n];
        cPrime[0] = C[0] / B[0];
        for (int i = 1; i < n; i++)
            cPrime[i] = C[i] / (B[i] - cPrime[i - 1] * A[i]);

        double[] dPrime = new double[n];
        dPrime[0] = r[0] / B[0];
        for (int i = 1; i < n; i++)
            dPrime[i] = (r[i] - dPrime[i - 1] * A[i]) / (B[i] - cPrime[i - 1] * A[i]);

        double[] k = new double[n];
        k[n - 1] = dPrime[n - 1];
        for (int i = n - 2; i >= 0; i--)
            k[i] = dPrime[i] - cPrime[i] * k[i + 1];

        for (int i = 1; i < n; i++)
        {
            dx1 = x[i] - x[i - 1];
            dy1 = y[i] - y[i - 1];
            a[i - 1] = k[i - 1] * dx1 - dy1;
            b[i - 1] = -k[i] * dx1 + dy1;
        }
    }
}
