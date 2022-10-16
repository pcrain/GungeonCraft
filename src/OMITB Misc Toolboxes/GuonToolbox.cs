using Alexandria.ItemAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NevernamedsItems
{
    class GuonToolbox
    {
        public static GameObject MakeAnimatedOrbital(string name, float orbitRadius, float orbitalDegreesPerSecond, int orbitalTier, PlayerOrbital.OrbitalMotionStyle motionStyle, float perfectOrbitalFactor,  List<string> idleAnimPaths, int fps, Vector2 colliderDimensions, Vector2 colliderOffsets, tk2dBaseSprite.Anchor anchorMode,  tk2dSpriteAnimationClip.WrapMode wrapMode)
        {
            GameObject prefab = SpriteBuilder.SpriteFromResource(idleAnimPaths[0]);
            prefab.name = name;
            var body = prefab.GetComponent<tk2dSprite>().SetUpSpeculativeRigidbody(colliderOffsets.ToIntVector2(), colliderDimensions.ToIntVector2());
            
            body.CollideWithTileMap = false;
            body.CollideWithOthers = true;
            body.PrimaryPixelCollider.CollisionLayer = CollisionLayer.EnemyBulletBlocker;

            tk2dSpriteCollectionData spriteCollection = SpriteBuilder.ConstructCollection(prefab, $"{name}_Collection");
            prefab.AddAnimationToObject(spriteCollection, "start", idleAnimPaths, fps, colliderDimensions, colliderOffsets, anchorMode, wrapMode, true);

            PlayerOrbital orbitalPrefab = prefab.AddComponent<PlayerOrbital>();
            orbitalPrefab.motionStyle = motionStyle;
            orbitalPrefab.shouldRotate = false;
            orbitalPrefab.orbitRadius = orbitRadius;
            orbitalPrefab.perfectOrbitalFactor = perfectOrbitalFactor;
            orbitalPrefab.orbitDegreesPerSecond = orbitalDegreesPerSecond;
            orbitalPrefab.SetOrbitalTier(orbitalTier);

            prefab.MakeFakePrefab();
            return prefab;
        }
    }
}
