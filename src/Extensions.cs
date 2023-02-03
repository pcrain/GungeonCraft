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

    // Insets the borders of a rectangle by a specified amount
    public static Rect Inset(this Rect self, float amount)
    {
      return new Rect(self.x + amount, self.y + amount, self.width - amount, self.height - amount);
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

    // Clamp a floating point angle in degrees to [-180,180]
    public static float Clamp180(this float self)
    {
      return BraveMathCollege.ClampAngle180(self);
    }

    // Get a bullet's direction to the primary player
    public static float DirToNearestPlayer(this Bullet self)
    {
      return (GameManager.Instance.GetPlayerClosestToPoint(self.Position).CenterPosition - self.Position).ToAngle();
    }
  }
}
