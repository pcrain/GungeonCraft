namespace CwaffingTheGungy;

public static class PhysicsPatches
{
  /// <summary>Optimized version of PhysicsEngine.Pointcast(IntVector2, ...) without unnecessary delegate creation</summary>
  [HarmonyPatch]
  private class OptimiseIntVectorPointcastPatch
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
        // System.Console.WriteLine ("speed o:");
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

  /*
  public bool Pointcast(IntVector2 point, out SpeculativeRigidbody result, bool collideWithTiles = true, bool collideWithRigidbodies = true, int rayMask = int.MaxValue, CollisionLayer? sourceLayer = null, bool collideWithTriggers = false, params SpeculativeRigidbody[] ignoreList)
  {
    ICollidableObject tempResult = null;
    Func<ICollidableObject, IntVector2, ICollidableObject> collideWithCollidable = delegate(ICollidableObject collidable, IntVector2 p)
    {
      SpeculativeRigidbody speculativeRigidbody = collidable as SpeculativeRigidbody;
      if ((bool)speculativeRigidbody && !speculativeRigidbody.enabled)
      {
        return null;
      }
      for (int i = 0; i < collidable.GetPixelColliders().Count; i++)
      {
        PixelCollider pixelCollider = collidable.GetPixelColliders()[i];
        if ((collideWithTriggers || !pixelCollider.IsTrigger) && pixelCollider.CanCollideWith(rayMask, sourceLayer) && pixelCollider.ContainsPixel(p))
        {
          return collidable;
        }
      }
      return null;
    };
    if (collideWithTiles && (bool)TileMap)
    {
      TileMap.GetTileAtPosition(PixelToUnit(point), out var x, out var y);
      int tileMapLayerByName = BraveUtility.GetTileMapLayerByName("Collision Layer", TileMap);
      Tile tile = GetTile(x, y, TileMap, tileMapLayerByName, "Collision Layer", GameManager.Instance.Dungeon.data);
      if (tile != null)
      {
        tempResult = collideWithCollidable(tile, point);
        if (tempResult != null)
        {
          result = tempResult as SpeculativeRigidbody;
          return true;
        }
      }
    }
    if (collideWithRigidbodies)
    {
      Func<SpeculativeRigidbody, bool> callback = delegate(SpeculativeRigidbody rigidbody)
      {
        tempResult = collideWithCollidable(rigidbody, point);
        return tempResult == null;
      };
      m_rigidbodyTree.Query(GetSafeB2AABB(point, point), callback);
      if (CollidesWithProjectiles(rayMask, sourceLayer))
      {
        m_projectileTree.Query(GetSafeB2AABB(point, point), callback);
      }
    }
    result = tempResult as SpeculativeRigidbody;
    return result != null;
  }
  */

}

// public static class PhysicsFixes
// {
//   [HarmonyPatch(typeof(PhysicsEngine), nameof(PhysicsEngine.Pointcast), typeof(IntVector2))]
//   private class PhysicsEnginePointcastPatch
//   {
//       static bool Prefix(PhysicsEngine __instance, object arg, ref ReturnType __result)
//       {
//           return true;     // call the original method

//           __result = null; // change the original result
//           return false;    // skip the original method
//           return;          // if return type is void, always calls original method
//       }
//   }


//   public bool Pointcast(
//     PhysicsEngine __instance, IntVector2 point, out SpeculativeRigidbody result, bool collideWithTiles, bool collideWithRigidbodies,
//     int rayMask, CollisionLayer? sourceLayer, bool collideWithTriggers, params SpeculativeRigidbody[] ignoreList, ref bool __result)
//   {
//     ICollidableObject tempResult = null;
//     Func<ICollidableObject, IntVector2, ICollidableObject> collideWithCollidable = delegate(ICollidableObject collidable, IntVector2 p)
//     {
//       SpeculativeRigidbody speculativeRigidbody = collidable as SpeculativeRigidbody;
//       if ((bool)speculativeRigidbody && !speculativeRigidbody.enabled)
//       {
//         return null;
//       }
//       for (int i = 0; i < collidable.GetPixelColliders().Count; i++)
//       {
//         PixelCollider pixelCollider = collidable.GetPixelColliders()[i];
//         if ((collideWithTriggers || !pixelCollider.IsTrigger) && pixelCollider.CanCollideWith(rayMask, sourceLayer) && pixelCollider.ContainsPixel(p))
//         {
//           return collidable;
//         }
//       }
//       return null;
//     };
//     if (collideWithTiles && (bool)TileMap)
//     {
//       TileMap.GetTileAtPosition(PixelToUnit(point), out var x, out var y);
//       int tileMapLayerByName = BraveUtility.GetTileMapLayerByName("Collision Layer", TileMap);
//       Tile tile = GetTile(x, y, TileMap, tileMapLayerByName, "Collision Layer", GameManager.Instance.Dungeon.data);
//       if (tile != null)
//       {
//         tempResult = collideWithCollidable(tile, point);
//         if (tempResult != null)
//         {
//           result = tempResult as SpeculativeRigidbody;
//           return true;
//         }
//       }
//     }
//     if (collideWithRigidbodies)
//     {
//       Func<SpeculativeRigidbody, bool> callback = delegate(SpeculativeRigidbody rigidbody)
//       {
//         tempResult = collideWithCollidable(rigidbody, point);
//         return tempResult == null;
//       };
//       m_rigidbodyTree.Query(GetSafeB2AABB(point, point), callback);
//       if (CollidesWithProjectiles(rayMask, sourceLayer))
//       {
//         m_projectileTree.Query(GetSafeB2AABB(point, point), callback);
//       }
//     }
//     result = tempResult as SpeculativeRigidbody;
//     return result != null;
//   }

// }
