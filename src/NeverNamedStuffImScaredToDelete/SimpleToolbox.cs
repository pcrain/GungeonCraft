using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Alexandria.ItemAPI;
using Dungeonator;
using System.Collections;
using System.Diagnostics;
using Alexandria.Misc;

namespace CwaffingTheGungy
{
    public class SpawnObjectManager : MonoBehaviour //----------------------------------------------------------------------------------------------------------------------------
    {
        public static void SpawnObject(GameObject thingToSpawn, Vector3 convertedVector, GameObject SpawnVFX, bool correctForWalls = false)
        {
            Vector2 Vector2Position = convertedVector;

            GameObject newObject = Instantiate(thingToSpawn, convertedVector, Quaternion.identity);

            SpeculativeRigidbody ObjectSpecRigidBody = newObject.GetComponentInChildren<SpeculativeRigidbody>();
            Component[] componentsInChildren = newObject.GetComponentsInChildren(typeof(IPlayerInteractable));
            for (int i = 0; i < componentsInChildren.Length; i++)
            {
                IPlayerInteractable interactable = componentsInChildren[i] as IPlayerInteractable;
                if (interactable != null)
                {
                    newObject.transform.position.GetAbsoluteRoom().RegisterInteractable(interactable);
                }
            }
            Component[] componentsInChildren2 = newObject.GetComponentsInChildren(typeof(IPlaceConfigurable));
            for (int i = 0; i < componentsInChildren2.Length; i++)
            {
                IPlaceConfigurable placeConfigurable = componentsInChildren2[i] as IPlaceConfigurable;
                if (placeConfigurable != null)
                {
                    placeConfigurable.ConfigureOnPlacement(GameManager.Instance.Dungeon.data.GetAbsoluteRoomFromPosition(Vector2Position.ToIntVector2()));
                }
            }
            /* FlippableCover component7 = newObject.GetComponentInChildren<FlippableCover>();
             component7.transform.position.XY().GetAbsoluteRoom().RegisterInteractable(component7);
             component7.ConfigureOnPlacement(component7.transform.position.XY().GetAbsoluteRoom());*/

            ObjectSpecRigidBody.Initialize();
            PhysicsEngine.Instance.RegisterOverlappingGhostCollisionExceptions(ObjectSpecRigidBody, null, false);

            if (SpawnVFX != null)
            {
                UnityEngine.Object.Instantiate<GameObject>(SpawnVFX, ObjectSpecRigidBody.sprite.WorldCenter, Quaternion.identity);
            }
            if (correctForWalls) CorrectForWalls(newObject);
        }
        private static void CorrectForWalls(GameObject portal)
        {
            SpeculativeRigidbody rigidbody = portal.GetComponent<SpeculativeRigidbody>();
            if (rigidbody)
            {
                bool flag = PhysicsEngine.Instance.OverlapCast(rigidbody, null, true, false, null, null, false, null, null, new SpeculativeRigidbody[0]);
                if (flag)
                {
                    Vector2 vector = portal.transform.position.XY();
                    IntVector2[] cardinalsAndOrdinals = IntVector2.CardinalsAndOrdinals;
                    int num = 0;
                    int num2 = 1;
                    for (; ; )
                    {
                        for (int i = 0; i < cardinalsAndOrdinals.Length; i++)
                        {
                            portal.transform.position = vector + PhysicsEngine.PixelToUnit(cardinalsAndOrdinals[i] * num2);
                            rigidbody.Reinitialize();
                            if (!PhysicsEngine.Instance.OverlapCast(rigidbody, null, true, false, null, null, false, null, null, new SpeculativeRigidbody[0]))
                            {
                                return;
                            }
                        }
                        num2++;
                        num++;
                        if (num > 200)
                        {
                            goto Block_4;
                        }
                    }
                //return;
                Block_4:
                    UnityEngine.Debug.LogError("FREEZE AVERTED!  TELL RUBEL!  (you're welcome) 147");
                    return;
                }
            }
        }
    }
    public static class AnimateBullet//----------------------------------------------------------------------------------------------
    {
        public static tk2dSpriteAnimationClip CreateProjectileAnimation(List<string> names, int fps, bool loops, List<IntVector2> pixelSizes, List<bool> lighteneds, List<tk2dBaseSprite.Anchor> anchors, List<bool> anchorsChangeColliders,
            List<bool> fixesScales, List<Vector3?> manualOffsets, List<IntVector2?> overrideColliderPixelSizes, List<IntVector2?> overrideColliderOffsets, List<Projectile> overrideProjectilesToCopyFrom)
        {
            tk2dSpriteAnimationClip clip = new tk2dSpriteAnimationClip();
            clip.name = names[0]+"_clip";
            clip.fps = fps;
            List<tk2dSpriteAnimationFrame> frames = new List<tk2dSpriteAnimationFrame>();
            for (int i = 0; i < names.Count; i++)
            {
                string name = names[i];
                IntVector2 pixelSize = pixelSizes[i];
                IntVector2? overrideColliderPixelSize = overrideColliderPixelSizes[i];
                IntVector2? overrideColliderOffset = overrideColliderOffsets[i];
                Vector3? manualOffset = manualOffsets[i];
                bool anchorChangesCollider = anchorsChangeColliders[i];
                bool fixesScale = fixesScales[i];
                if (!manualOffset.HasValue)
                {
                    manualOffset = new Vector2?(Vector2.zero);
                }
                tk2dBaseSprite.Anchor anchor = anchors[i];
                bool lightened = lighteneds[i];
                Projectile overrideProjectileToCopyFrom = overrideProjectilesToCopyFrom[i];
                tk2dSpriteAnimationFrame frame = new tk2dSpriteAnimationFrame();
                frame.spriteId = ETGMod.Databases.Items.ProjectileCollection.inst.GetSpriteIdByName(name);
                frame.spriteCollection = ETGMod.Databases.Items.ProjectileCollection;
                frames.Add(frame);
                int? overrideColliderPixelWidth = null;
                int? overrideColliderPixelHeight = null;
                if (overrideColliderPixelSize.HasValue)
                {
                    overrideColliderPixelWidth = overrideColliderPixelSize.Value.x;
                    overrideColliderPixelHeight = overrideColliderPixelSize.Value.y;
                }
                int? overrideColliderOffsetX = null;
                int? overrideColliderOffsetY = null;
                if (overrideColliderOffset.HasValue)
                {
                    overrideColliderOffsetX = overrideColliderOffset.Value.x;
                    overrideColliderOffsetY = overrideColliderOffset.Value.y;
                }
                tk2dSpriteDefinition def = GunTools.SetupDefinitionForProjectileSprite(name, frame.spriteId, pixelSize.x, pixelSize.y, lightened, overrideColliderPixelWidth, overrideColliderPixelHeight, overrideColliderOffsetX, overrideColliderOffsetY,
                    overrideProjectileToCopyFrom);
                def.ConstructOffsetsFromAnchor(anchor, def.position3, fixesScale, anchorChangesCollider);
                def.position0 += manualOffset.Value;
                def.position1 += manualOffset.Value;
                def.position2 += manualOffset.Value;
                def.position3 += manualOffset.Value;
            }
            clip.wrapMode = loops ? tk2dSpriteAnimationClip.WrapMode.Loop : tk2dSpriteAnimationClip.WrapMode.Once;
            clip.frames = frames.ToArray();
            return clip;
        }
        public static tk2dSpriteAnimationClip CreateProjectileAnimation(List<string> names, int fps, bool loops, IntVector2 pixelSizes, bool lighteneds, tk2dBaseSprite.Anchor anchors, bool anchorsChangeColliders,
            bool fixesScales, Vector3? manualOffsets = null, IntVector2? overrideColliderPixelSizes = null, IntVector2? overrideColliderOffsets = null, Projectile overrideProjectilesToCopyFrom = null)
        {
            int n = names.Count;
            return CreateProjectileAnimation(
                names,fps,loops,
                Enumerable.Repeat(pixelSizes,n).ToList(),
                Enumerable.Repeat(lighteneds,n).ToList(),
                Enumerable.Repeat(anchors,n).ToList(),
                Enumerable.Repeat(anchorsChangeColliders,n).ToList(),
                Enumerable.Repeat(fixesScales,n).ToList(),
                Enumerable.Repeat<Vector3?>(manualOffsets,n).ToList(),
                Enumerable.Repeat<IntVector2?>(overrideColliderPixelSizes,n).ToList(),
                Enumerable.Repeat<IntVector2?>(overrideColliderOffsets,n).ToList(),
                Enumerable.Repeat<Projectile>(overrideProjectilesToCopyFrom,n).ToList());
        }
        public static void SetAnimation(this Projectile proj, tk2dSpriteAnimationClip clip, int frame = -1)
        {
            proj.sprite.spriteAnimator.currentClip = clip;
            if (frame >= 0)
                proj.sprite.spriteAnimator.SetFrame(frame);
        }
        public static void AddAnimation(this Projectile proj, tk2dSpriteAnimationClip clip)
        {
            if (proj.sprite.spriteAnimator == null)
            {
                proj.sprite.spriteAnimator = proj.sprite.gameObject.AddComponent<tk2dSpriteAnimator>();
            }
            proj.sprite.spriteAnimator.playAutomatically = true;
            if (proj.sprite.spriteAnimator.Library == null)
            {
                proj.sprite.spriteAnimator.Library = proj.sprite.spriteAnimator.gameObject.AddComponent<tk2dSpriteAnimation>();
                proj.sprite.spriteAnimator.Library.clips = new tk2dSpriteAnimationClip[0];
                proj.sprite.spriteAnimator.Library.enabled = true;
            }

            proj.sprite.spriteAnimator.Library.clips = proj.sprite.spriteAnimator.Library.clips.Concat(new tk2dSpriteAnimationClip[] { clip }).ToArray();
            proj.sprite.spriteAnimator.deferNextStartClip = false;
        }
        public static void AnimateProjectile(this Projectile proj, List<string> names, int fps, bool loops, List<IntVector2> pixelSizes, List<bool> lighteneds, List<tk2dBaseSprite.Anchor> anchors, List<bool> anchorsChangeColliders,
            List<bool> fixesScales, List<Vector3?> manualOffsets, List<IntVector2?> overrideColliderPixelSizes, List<IntVector2?> overrideColliderOffsets, List<Projectile> overrideProjectilesToCopyFrom)
        {
            tk2dSpriteAnimationClip clip = CreateProjectileAnimation(
                names, fps, loops, pixelSizes, lighteneds, anchors, anchorsChangeColliders,
                fixesScales, manualOffsets, overrideColliderPixelSizes, overrideColliderOffsets,
                overrideProjectilesToCopyFrom);
            proj.AddAnimation(clip);
            proj.SetAnimation(clip);
        }
        // Simpler version of the above method assuming most elements are repeated
        public static void AnimateProjectile(this Projectile proj, List<string> names, int fps, bool loops, IntVector2 pixelSizes, bool lighteneds, tk2dBaseSprite.Anchor anchors, bool anchorsChangeColliders,
            bool fixesScales, Vector3? manualOffsets = null, IntVector2? overrideColliderPixelSizes = null, IntVector2? overrideColliderOffsets = null, Projectile overrideProjectilesToCopyFrom = null)
        {
            int n = names.Count;
            proj.AnimateProjectile(
                names,fps,loops,
                Enumerable.Repeat(pixelSizes,n).ToList(),
                Enumerable.Repeat(lighteneds,n).ToList(),
                Enumerable.Repeat(anchors,n).ToList(),
                Enumerable.Repeat(anchorsChangeColliders,n).ToList(),
                Enumerable.Repeat(fixesScales,n).ToList(),
                Enumerable.Repeat<Vector3?>(manualOffsets,n).ToList(),
                Enumerable.Repeat<IntVector2?>(overrideColliderPixelSizes,n).ToList(),
                Enumerable.Repeat<IntVector2?>(overrideColliderOffsets,n).ToList(),
                Enumerable.Repeat<Projectile>(overrideProjectilesToCopyFrom,n).ToList()
                );
        }
    }
}
