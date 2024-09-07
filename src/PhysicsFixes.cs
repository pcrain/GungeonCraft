namespace CwaffingTheGungy;

public static class PhysicsPatches
{
  /// <summary>Optimized version of PhysicsEngine.Pointcast(IntVector2, ...) without unnecessary delegate creation</summary>
  [HarmonyPatch]
  private static class OptimiseIntVectorPointcastPatch
  {
      static MethodBase TargetMethod() {
        return typeof(PhysicsEngine).GetMethod("Pointcast", new Type[] {
          typeof(IntVector2), typeof(SpeculativeRigidbody).MakeByRefType(), typeof(bool),
          typeof(bool), typeof(int), typeof(CollisionLayer?), typeof(bool), typeof(SpeculativeRigidbody[])
        });
      }

      private static bool _collideWithTriggers;
      private static int _rayMask;
      private static CollisionLayer? _sourceLayer;
      private static IntVector2 _point;
      private static ICollidableObject _tempResult;
      private static bool CollideWithRigidBodyStatic(SpeculativeRigidbody rigidbody)
      {
          if (!rigidbody || !rigidbody.enabled)
            return true;
          List<PixelCollider> colliders = rigidbody.GetPixelColliders();
          for (int i = 0; i < colliders.Count; i++)
          {
            PixelCollider pixelCollider = colliders[i];
            if ((_collideWithTriggers || !pixelCollider.IsTrigger) && pixelCollider.CanCollideWith(_rayMask, _sourceLayer) && pixelCollider.ContainsPixel(_point))
            {
              _tempResult = rigidbody;
              return false;
            }
          }
          return true;
      }

      static bool Prefix(PhysicsEngine __instance, ref bool __result, IntVector2 point, out SpeculativeRigidbody result, bool collideWithTiles,
        bool collideWithRigidbodies, int rayMask, CollisionLayer? sourceLayer, bool collideWithTriggers,
        params SpeculativeRigidbody[] ignoreList)
      {
        // Debug.LogError("speed o:");
        if (collideWithTiles && __instance.TileMap)
        {
          __instance.TileMap.GetTileAtPosition(PhysicsEngine.PixelToUnit(point), out var x, out var y);
          int tileMapLayerByName = BraveUtility.GetTileMapLayerByName("Collision Layer", __instance.TileMap);
          PhysicsEngine.Tile tile = __instance.GetTile(x, y, __instance.TileMap, tileMapLayerByName, "Collision Layer", GameManager.Instance.Dungeon.data);
          if (tile != null)
          {
            List<PixelCollider> colliders = tile.GetPixelColliders();
            for (int i = 0; i < colliders.Count; i++)
            {
              PixelCollider pixelCollider = colliders[i];
              if ((collideWithTriggers || !pixelCollider.IsTrigger) && pixelCollider.CanCollideWith(rayMask, sourceLayer) && pixelCollider.ContainsPixel(point))
              {
                result = null; // tile is not a SpeculativeRigidBody
                __result = true; // original return value
                return false; // skip original
              }
            }
          }
        }

        if (collideWithRigidbodies)
        {
          _tempResult          = null;
          _collideWithTriggers = collideWithTriggers;
          _rayMask             = rayMask;
          _sourceLayer         = sourceLayer;
          _point               = point;
          BraveDynamicTree.b2AABB b2aabb = PhysicsEngine.GetSafeB2AABB(point, point);
          __instance.m_rigidbodyTree.Query(b2aabb, CollideWithRigidBodyStatic);
          if (__instance.CollidesWithProjectiles(rayMask, sourceLayer))
            __instance.m_projectileTree.Query(b2aabb, CollideWithRigidBodyStatic);

          result = _tempResult as SpeculativeRigidbody;
          __result = result != null; // original return value
          return false; // skip the original method
        }

        result = null;
        __result = false; // original return value
        return false; // skip the original method
      }
  }

  /// <summary>Optimizations for preventing player projectile prefabs from constructing unnecessary objects</summary>
  [HarmonyPatch(typeof(SpawnManager), nameof(SpawnManager.SpawnProjectile), typeof(GameObject), typeof(Vector3), typeof(Quaternion), typeof(bool))]
  private class SpawnManagerSpawnProjectilePatch
  {
      private static HashSet<GameObject> _Processed = new();

      static void Prefix(SpawnManager __instance, GameObject prefab, Vector3 position, Quaternion rotation, bool ignoresPools)
      {
          if (_Processed.Contains(prefab))
            return;
          if (prefab.GetComponent<Projectile>() is not Projectile proj)
            return;
          if (!proj.AppliesPoison)                        { proj.healthEffect               = null; }
          if (!proj.AppliesSpeedModifier)                 { proj.speedEffect                = null; }
          if (!proj.AppliesCharm)                         { proj.charmEffect                = null; }
          if (!proj.AppliesFreeze)                        { proj.freezeEffect               = null; }
          if (!proj.AppliesCheese)                        { proj.cheeseEffect               = null; }
          if (!proj.AppliesBleed)                         { proj.bleedEffect                = null; }
          if (!proj.AppliesFire)                          { proj.fireEffect                 = null; }
          if (!proj.baseData.UsesCustomAccelerationCurve) { proj.baseData.AccelerationCurve = null; }
          _Processed.Add(prefab);
      }
  }
}
