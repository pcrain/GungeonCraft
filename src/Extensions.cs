using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using MonoMod.RuntimeDetour;
using Brave.BulletScript;

using Dungeonator;
using ItemAPI;

namespace CwaffingTheGungy
{
  public static class Extensions
  {
    public class Expiration : MonoBehaviour  // kill projectile after a fixed amount of time
    {
      public void ExpireIn(float seconds)
      {
        this.StartCoroutine(Expire(seconds));
      }

      private IEnumerator Expire(float seconds)
      {
        yield return new WaitForSeconds(seconds);
        UnityEngine.Object.Destroy(this.gameObject);
      }
    }

    // Add an expiration timer to a GameObject
    public static void ExpireIn(this GameObject self, float seconds)
    {
      self.GetOrAddComponent<Expiration>().ExpireIn(seconds);
    }

    // Check if a rectangle contains a point
    public static bool Contains(this Rect self, Vector2 point)
    {
      return (point.x > self.xMin && point.x < self.xMax && point.y > self.yMin && point.y < self.yMax);
    }

    // Insets the borders of a rectangle by a specified amount on each side
    public static Rect Inset(this Rect self, float topInset, float rightInset, float bottomInset, float leftInset)
    {
      // ETGModConsole.Log($"  old bounds are {self.xMin},{self.yMin} to {self.xMax},{self.yMax}");
      Rect r = new Rect(self.x + leftInset, self.y + bottomInset, self.width - leftInset - rightInset, self.height - bottomInset - topInset);
      // ETGModConsole.Log($"  new bounds are {r.xMin},{r.yMin} to {r.xMax},{r.yMax}");
      return r;
    }

    // Insets the borders of a rectangle by a specified amount on each axis
    public static Rect Inset(this Rect self, float xInset, float yInset)
    {
      return self.Inset(yInset,xInset,yInset,xInset);
    }

    // Insets the borders of a rectangle by a specified amount on all sides
    public static Rect Inset(this Rect self, float inset)
    {
      return self.Inset(inset,inset,inset,inset);
    }

    // Get a random point on the perimeter of a rectangle
    public static Vector2 RandomPointOnPerimeter(this Rect self)
      { return self.PointOnPerimeter(UnityEngine.Random.Range(0.0f,1.0f)); }

    // Get a given point on the perimeter of a rectangle scales from 0 to 1
    public static Vector2 PointOnPerimeter(this Rect self, float t)
    {
      // ETGModConsole.Log($"bounds are {self.xMin},{self.yMin} to {self.xMax},{self.yMax}");
      float half  = self.width + self.height;
      float point = UnityEngine.Random.Range(0.0f,t*2.0f*half);
      Vector2 retPoint;
      if (point < self.width) // bottom edge
        retPoint = new Vector2(self.xMin + point, self.yMin);
      else if (point < half) // left edge
        retPoint = new Vector2(self.xMin, self.yMin + point-self.width);
      else if (point-half < self.width) // top edge
        retPoint = new Vector2(self.xMin + point-half, self.yMin + self.height);
      else
        retPoint = new Vector2(self.xMin + self.width, self.yMin + point-half-self.width); // right edge
      // ETGModConsole.Log($"  chose point {retPoint.x},{retPoint.y}");
      return retPoint;
    }

    // Given an angle and wall Rect, determine the intersection point from a ray cast from self to theWall
    public static Vector2 RaycastToWall(this Vector2 self, float angle, Rect theWall)
    {
      Vector2 intersection = Vector2.positiveInfinity;
      if(!BraveMathCollege.LineSegmentRectangleIntersection(self + 1000f * angle.ToVector(), self, theWall.position, theWall.position + theWall.size, ref intersection))
        ETGModConsole.Log("no intersection found");
      return intersection;
    }

    // Add a named bullet from a named enemy to a bullet bank
    public static void AddBulletFromEnemy(this AIBulletBank self, string enemyName, string bulletName)
    {
      self.Bullets.Add(EnemyDatabase.GetOrLoadByGuid(EnemyGuidDatabase.Entries[enemyName]).bulletBank.GetBullet(bulletName));
    }

    // Register a game object as a prefab
    public static void RegisterPrefab(this GameObject self)
    {
      self.gameObject.SetActive(false);
      FakePrefab.MarkAsFakePrefab(self.gameObject);
      UnityEngine.Object.DontDestroyOnLoad(self);
    }

    // Convert degrees to a Vector2 angle
    public static Vector2 ToVector(this float self)
    {
      return (Vector2)(Quaternion.Euler(0f, 0f, self) * Vector2.right);
    }

    // Rotate a Vector2 by specified number of degrees
    public static Vector2 Rotate(this Vector2 self, float rotation)
    {
      return (Vector2)(Quaternion.Euler(0f, 0f, rotation) * self);
    }

    // Clamp a floating point angle in degrees to [-180,180]
    public static float Clamp180(this float self)
    {
      return BraveMathCollege.ClampAngle180(self);
    }

    // Clamp a floating point angle in degrees to [0,360]
    public static float Clamp360(this float self)
    {
      return BraveMathCollege.ClampAngle360(self);
    }

    // Get a bullet's direction to the primary player
    public static float DirToNearestPlayer(this Bullet self)
    {
      return (GameManager.Instance.GetPlayerClosestToPoint(self.Position).CenterPosition - self.Position).ToAngle();
    }
  }
}
