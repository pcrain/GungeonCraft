using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dungeonator;
using UnityEngine;

namespace Alexandria.cAPI
{
	public class cAPIToolBox
	{
        public static SpeculativeRigidbody GenerateOrAddToRigidBody(GameObject targetObject, CollisionLayer collisionLayer, PixelCollider.PixelColliderGeneration colliderGenerationMode = PixelCollider.PixelColliderGeneration.Tk2dPolygon, bool collideWithTileMap = false, bool CollideWithOthers = true, bool CanBeCarried = true, bool CanBePushed = false, bool RecheckTriggers = false, bool IsTrigger = false, bool replaceExistingColliders = false, bool UsesPixelsAsUnitSize = false, IntVector2? dimensions = null, IntVector2? offset = null)
        {
            SpeculativeRigidbody m_CachedRigidBody = GameObjectExtensions.GetOrAddComponent<SpeculativeRigidbody>(targetObject);
            m_CachedRigidBody.CollideWithOthers = CollideWithOthers;
            m_CachedRigidBody.CollideWithTileMap = collideWithTileMap;
            m_CachedRigidBody.Velocity = Vector2.zero;
            m_CachedRigidBody.MaxVelocity = Vector2.zero;
            m_CachedRigidBody.ForceAlwaysUpdate = false;
            m_CachedRigidBody.CanPush = false;
            m_CachedRigidBody.CanBePushed = CanBePushed;
            m_CachedRigidBody.PushSpeedModifier = 1f;
            m_CachedRigidBody.CanCarry = false;
            m_CachedRigidBody.CanBeCarried = CanBeCarried;
            m_CachedRigidBody.PreventPiercing = false;
            m_CachedRigidBody.SkipEmptyColliders = false;
            m_CachedRigidBody.RecheckTriggers = RecheckTriggers;
            m_CachedRigidBody.UpdateCollidersOnRotation = false;
            m_CachedRigidBody.UpdateCollidersOnScale = false;

            IntVector2 Offset = IntVector2.Zero;
            IntVector2 Dimensions = IntVector2.Zero;
            if (colliderGenerationMode != PixelCollider.PixelColliderGeneration.Tk2dPolygon)
            {
                if (dimensions.HasValue)
                {
                    Dimensions = dimensions.Value;
                    if (!UsesPixelsAsUnitSize)
                    {
                        Dimensions = (new IntVector2(Dimensions.x * 16, Dimensions.y * 16));
                    }
                }
                if (offset.HasValue)
                {
                    Offset = offset.Value;
                    if (!UsesPixelsAsUnitSize)
                    {
                        Offset = (new IntVector2(Offset.x * 16, Offset.y * 16));
                    }
                }
            }
            PixelCollider m_CachedCollider = new PixelCollider()
            {
                ColliderGenerationMode = colliderGenerationMode,
                CollisionLayer = collisionLayer,
                IsTrigger = IsTrigger,
                BagleUseFirstFrameOnly = (colliderGenerationMode == PixelCollider.PixelColliderGeneration.Tk2dPolygon),
                SpecifyBagelFrame = string.Empty,
                BagelColliderNumber = 0,
                ManualOffsetX = Offset.x,
                ManualOffsetY = Offset.y,
                ManualWidth = Dimensions.x,
                ManualHeight = Dimensions.y,
                ManualDiameter = 0,
                ManualLeftX = 0,
                ManualLeftY = 0,
                ManualRightX = 0,
                ManualRightY = 0
            };

            if (replaceExistingColliders | m_CachedRigidBody.PixelColliders == null)
            {
                m_CachedRigidBody.PixelColliders = new List<PixelCollider> { m_CachedCollider };
            }
            else
            {
                m_CachedRigidBody.PixelColliders.Add(m_CachedCollider);
            }

            if (m_CachedRigidBody.sprite && colliderGenerationMode == PixelCollider.PixelColliderGeneration.Tk2dPolygon)
            {
                Bounds bounds = m_CachedRigidBody.sprite.GetBounds();
                m_CachedRigidBody.sprite.GetTrueCurrentSpriteDef().colliderVertices = new Vector3[] { bounds.center - bounds.extents, bounds.center + bounds.extents };
                // m_CachedRigidBody.ForceRegenerate();
                // m_CachedRigidBody.RegenerateCache();
            }

            return m_CachedRigidBody;
        }
    }
}
