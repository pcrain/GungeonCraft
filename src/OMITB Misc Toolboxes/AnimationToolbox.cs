using Alexandria.ItemAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NevernamedsItems
{
    class AnimationToolbox
    {
    }
    public static class AnimationUtilityExtensions
    {
        public static void PlayUntilFinished(this tk2dSpriteAnimator animator, string clipName, string revertClip)
        {
            tk2dSpriteAnimationClip clip = animator.GetClipByName(clipName);
            if (clip == null) { Debug.LogError($"Selected clip '{clipName}' does not exist."); return; }
            if (clip.frames.Length <= 0) { Debug.LogError($"Selected clip '{clipName}' has no frames."); return; }
            if (clip.fps <= 0) { Debug.LogError($"Selected clip '{clipName}' has a framerate of 0 or lower, and as such cannot be played until finished."); return; }
            float duration = (clip.frames.Length / clip.fps);
            ETGModConsole.Log(duration.ToString());
            animator.PlayForDuration(clipName, duration, revertClip);
        }
        public static void AddAnimationToObject(this GameObject target, tk2dSpriteCollectionData spriteCollection, string animationName, List<string> spritePaths, int fps, Vector2 colliderDimensions, Vector2 colliderOffsets, tk2dBaseSprite.Anchor anchor, tk2dSpriteAnimationClip.WrapMode wrapMode, bool isDefaultAnimation = false)
        {
            tk2dSpriteAnimator animator = target.GetOrAddComponent<tk2dSpriteAnimator>();
            tk2dSpriteAnimation animation = target.GetOrAddComponent<tk2dSpriteAnimation>();
            animation.clips = new tk2dSpriteAnimationClip[0];
            animator.Library = animation;

            tk2dSpriteAnimationClip clip = new tk2dSpriteAnimationClip() { name = animationName, frames = new tk2dSpriteAnimationFrame[0], fps = fps };

            List<tk2dSpriteAnimationFrame> frames = new List<tk2dSpriteAnimationFrame>();
            foreach (string path in spritePaths)
            {
                tk2dSpriteCollectionData collection = spriteCollection;
                int frameSpriteId = SpriteBuilder.AddSpriteToCollection(path, collection);
                tk2dSpriteDefinition frameDef = collection.spriteDefinitions[frameSpriteId];

                frameDef.colliderVertices = new Vector3[]{
                    new Vector3(colliderOffsets.x/16, colliderOffsets.y/16, 0f),
                    new Vector3((colliderDimensions.x / 16), (colliderDimensions.y / 16), 0f)
                };

                frameDef.ConstructOffsetsFromAnchor(anchor);
                frames.Add(new tk2dSpriteAnimationFrame { spriteId = frameSpriteId, spriteCollection = collection });
            }
            clip.frames = frames.ToArray();
            clip.wrapMode = wrapMode;
            animation.clips = animation.clips.Concat(new tk2dSpriteAnimationClip[] { clip }).ToArray();

            if (isDefaultAnimation)
            {
                animator.DefaultClipId = animation.GetClipIdByName(animationName);
                animator.playAutomatically = true;
            }
        }
        public static void AdvAddAnimation(this AIAnimator targetAnimator, string animationName, DirectionalAnimation.DirectionType directionality, CompanionBuilder.AnimationType AnimationType, List<DirectionalAnimationData> AnimData)
        {

            List<string> subAnimNames = new List<string>();
            foreach (DirectionalAnimationData data in AnimData)
            {
                subAnimNames.Add(data.subAnimationName);

                tk2dSpriteCollectionData tk2dSpriteCollectionData = targetAnimator.GetComponent<tk2dSpriteCollectionData>();
                if (!tk2dSpriteCollectionData)
                {
                    tk2dSpriteCollectionData = SpriteBuilder.ConstructCollection(targetAnimator.gameObject, targetAnimator.name + "_collection");
                }

                string[] resourceNames = ResourceExtractor.GetResourceNames();
                List<int> list = new List<int>();
                for (int i = 0; i < resourceNames.Length; i++)
                {
                    if (resourceNames[i].StartsWith(data.pathDirectory.Replace('/', '.'), StringComparison.OrdinalIgnoreCase))
                    {
                      // ETGModConsole.Log($"Resource Found: {resourceNames[i]}.");
                        list.Add(SpriteBuilder.AddSpriteToCollection(resourceNames[i], tk2dSpriteCollectionData));
                    }
                }
                //ETGModConsole.Log($"Adding animation {data.subAnimationName} with list length {list.Count}.");

                tk2dSpriteAnimationClip tk2dSpriteAnimationClip = SpriteBuilder.AddAnimation(targetAnimator.spriteAnimator, tk2dSpriteCollectionData, list, data.subAnimationName, data.wrapMode);
                tk2dSpriteAnimationClip.fps = (float)data.fps;
            }

            if (AnimationType != CompanionBuilder.AnimationType.Other)
            {
                DirectionalAnimation newDirectionalAnimation = new DirectionalAnimation
                {
                    Type = directionality,
                    Flipped = new DirectionalAnimation.FlipType[subAnimNames.Count],
                    AnimNames = subAnimNames.ToArray(),
                    Prefix = string.Empty,
                };

                switch (AnimationType)
                {
                    case CompanionBuilder.AnimationType.Idle:
                        targetAnimator.IdleAnimation = newDirectionalAnimation;
                        break;
                    case CompanionBuilder.AnimationType.Move:
                        targetAnimator.MoveAnimation = newDirectionalAnimation;
                        break;
                    case CompanionBuilder.AnimationType.Hit:
                        targetAnimator.HitAnimation = newDirectionalAnimation;
                        break;
                    case CompanionBuilder.AnimationType.Talk:
                        targetAnimator.TalkAnimation = newDirectionalAnimation;
                        break;
                    case CompanionBuilder.AnimationType.Flight:
                        targetAnimator.FlightAnimation = newDirectionalAnimation;
                        break;
                    case CompanionBuilder.AnimationType.Fidget:
                        if (targetAnimator.IdleFidgetAnimations == null) targetAnimator.IdleFidgetAnimations = new List<DirectionalAnimation>();
                        targetAnimator.IdleFidgetAnimations.Add(newDirectionalAnimation);
                        break;
                }
            }
            else
            {
                AIAnimator.NamedDirectionalAnimation newOtheranim = new AIAnimator.NamedDirectionalAnimation
                {
                    name = animationName,
                    anim = new DirectionalAnimation
                    {
                        Prefix = animationName,
                        Type = directionality,
                        Flipped = new DirectionalAnimation.FlipType[subAnimNames.Count],
                        AnimNames = subAnimNames.ToArray(),
                    }
                };

                if (targetAnimator.OtherAnimations == null) targetAnimator.OtherAnimations = new List<AIAnimator.NamedDirectionalAnimation>();
                targetAnimator.OtherAnimations.Add(newOtheranim);
            }
        }
        public class DirectionalAnimationData
        {
            public string subAnimationName;
            public tk2dSpriteAnimationClip.WrapMode wrapMode = tk2dSpriteAnimationClip.WrapMode.Loop;
            public int fps;
            public string pathDirectory;
        }       
    }
}
