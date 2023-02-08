using System;
using System.Collections.Generic;
using Gungeon;
using ItemAPI;
using EnemyAPI;
using UnityEngine;
//using DirectionType = DirectionalAnimation.DirectionType;
// using AnimationType = ItemAPI.BossBuilder.AnimationType;
using System.Collections;
using Dungeonator;
using System.Linq;
using Brave.BulletScript;
using System.Text.RegularExpressions;
using ResourceExtractor = ItemAPI.ResourceExtractor;
using GungeonAPI;
using System.Reflection;
using MonoMod.RuntimeDetour;

/* Referennces
  - Quadratic Bezier Sampling: https://stackoverflow.com/questions/6711707/draw-a-quadratic-b%C3%A9zier-curve-through-three-given-points
  - Alt: https://stackoverflow.com/questions/5634460/quadratic-b%C3%A9zier-curve-calculate-points
  - Cubic Bezier Sampling: https://web.archive.org/web/20131225210855/http://people.sc.fsu.edu/~jburkardt/html/bezier_interpolation.html
  - General Ellipse Equation: https://www.maa.org/external_archive/joma/Volume8/Kalman/General.html
  - Ellipse Point Sampling: https://math.stackexchange.com/questions/172766/calculating-equidistant-points-around-an-ellipse-arc
  - Rose Curves: https://en.wikipedia.org/wiki/Rose_(mathematics)
*/

namespace CwaffingTheGungy
{
  public abstract class PathShape
  {
    // Get a point on the path between 0 and 1
    public abstract Vector2 At(float t);
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
  }

  public class PathRect : PathShape
  {
    public Rect rect {get; private set; }

    public PathRect(Rect r)
    {
      this.rect = rect;
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
  }

  public class PathCircle : PathShape
  {
    const float TWOPI = 2.0f*Mathf.PI;

    public Vector2 center {get; private set; }
    public float radius {get; private set; }

    public PathCircle(Vector2 center, float radius)
    {
      this.center = center;
      this.radius = radius;
    }
    public override Vector2 At(float t)
      { return this.center + this.radius * (t*TWOPI).ToVector(); }
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
      // convert control points to passthrough points
      Vector2 t1 = -5f * p0 + 18 * p1 - 9 * p2 + 2 * p3;
      Vector2 t2 = -5f * p3 + 18 * p2 - 9 * p1 + 2 * p0;
      this.p1 = t1;
      this.p2 = t2;
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

  public class PathableBullet
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
  }
}
