using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Dungeonator;

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
  }
}
