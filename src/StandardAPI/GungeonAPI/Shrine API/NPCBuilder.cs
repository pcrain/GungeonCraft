using System;
using System.Collections.Generic;
using System.Linq;
using ItemAPI;
using UnityEngine;

namespace GungeonAPI
{
	// Token: 0x02000011 RID: 17
	public static class NPCBuilder
	{
		// Token: 0x0600006C RID: 108 RVA: 0x00005C58 File Offset: 0x00003E58
		public static tk2dSpriteAnimationClip AddAnimation(this GameObject obj, string name, string spriteDirectory, int fps, NPCBuilder.AnimationType type, DirectionalAnimation.DirectionType directionType = DirectionalAnimation.DirectionType.None, DirectionalAnimation.FlipType flipType = DirectionalAnimation.FlipType.None)
		{
			obj.AddComponent<tk2dSpriteAnimator>();
			AIAnimator aianimator = obj.GetComponent<AIAnimator>();
			bool flag = !aianimator;
			bool flag2 = flag;
			if (flag2)
			{
				aianimator = NPCBuilder.CreateNewAIAnimator(obj);
			}
			DirectionalAnimation directionalAnimation = aianimator.GetDirectionalAnimation(name, directionType, type);
			bool flag3 = directionalAnimation == null;
			bool flag4 = flag3;
			if (flag4)
			{
				directionalAnimation = new DirectionalAnimation
				{
					AnimNames = new string[0],
					Flipped = new DirectionalAnimation.FlipType[0],
					Type = directionType,
					Prefix = string.Empty
				};
			}
			directionalAnimation.AnimNames = directionalAnimation.AnimNames.Concat(new string[]
			{
				name
			}).ToArray<string>();
			directionalAnimation.Flipped = directionalAnimation.Flipped.Concat(new DirectionalAnimation.FlipType[]
			{
				flipType
			}).ToArray<DirectionalAnimation.FlipType>();
			aianimator.AssignDirectionalAnimation(name, directionalAnimation, type);
			return NPCBuilder.BuildAnimation(aianimator, name, spriteDirectory, fps);
		}

		// Token: 0x0600006D RID: 109 RVA: 0x00005D3C File Offset: 0x00003F3C
		private static AIAnimator CreateNewAIAnimator(GameObject obj)
		{
			AIAnimator aianimator = obj.AddComponent<AIAnimator>();
			aianimator.FlightAnimation = NPCBuilder.CreateNewDirectionalAnimation();
			aianimator.HitAnimation = NPCBuilder.CreateNewDirectionalAnimation();
			aianimator.IdleAnimation = NPCBuilder.CreateNewDirectionalAnimation();
			aianimator.TalkAnimation = NPCBuilder.CreateNewDirectionalAnimation();
			aianimator.MoveAnimation = NPCBuilder.CreateNewDirectionalAnimation();
			aianimator.OtherAnimations = new List<AIAnimator.NamedDirectionalAnimation>();
			aianimator.IdleFidgetAnimations = new List<DirectionalAnimation>();
			aianimator.OtherVFX = new List<AIAnimator.NamedVFXPool>();
			return aianimator;
		}

		// Token: 0x0600006E RID: 110 RVA: 0x00005DB0 File Offset: 0x00003FB0
		private static DirectionalAnimation CreateNewDirectionalAnimation()
		{
			return new DirectionalAnimation
			{
				AnimNames = new string[0],
				Flipped = new DirectionalAnimation.FlipType[0],
				Type = DirectionalAnimation.DirectionType.None
			};
		}

		// Token: 0x0600006F RID: 111 RVA: 0x00005DE8 File Offset: 0x00003FE8
		public static tk2dSpriteAnimationClip BuildAnimation(AIAnimator aiAnimator, string name, string spriteDirectory, int fps)
		{
			tk2dSpriteCollectionData tk2dSpriteCollectionData = aiAnimator.GetComponent<tk2dSpriteCollectionData>();
			bool flag = !tk2dSpriteCollectionData;
			bool flag2 = flag;
			if (flag2)
			{
				tk2dSpriteCollectionData = SpriteBuilder.ConstructCollection(aiAnimator.gameObject, aiAnimator.name + "_collection");
			}
			string[] resourceNames = ResourceExtractor.GetResourceNames();
			List<int> list = new List<int>();
			for (int i = 0; i < resourceNames.Length; i++)
			{
				bool flag3 = resourceNames[i].StartsWith(spriteDirectory.Replace('/', '.'), StringComparison.OrdinalIgnoreCase);
				bool flag4 = flag3;
				if (flag4)
				{
					list.Add(SpriteBuilder.AddSpriteToCollection(resourceNames[i], tk2dSpriteCollectionData));
				}
			}
			bool flag5 = list.Count == 0;
			bool flag6 = flag5;
			if (flag6)
			{
				Tools.PrintError<string>("No sprites found for animation " + name, "FF0000");
			}
			tk2dSpriteAnimationClip tk2dSpriteAnimationClip = SpriteBuilder.AddAnimation(aiAnimator.spriteAnimator, tk2dSpriteCollectionData, list, name, tk2dSpriteAnimationClip.WrapMode.Loop);
			tk2dSpriteAnimationClip.fps = (float)fps;
			return tk2dSpriteAnimationClip;
		}

		// Token: 0x06000070 RID: 112 RVA: 0x00005ED0 File Offset: 0x000040D0
		public static DirectionalAnimation GetDirectionalAnimation(this AIAnimator aiAnimator, string name, DirectionalAnimation.DirectionType directionType, NPCBuilder.AnimationType type)
		{
			DirectionalAnimation directionalAnimation = null;
			switch (type)
			{
				case NPCBuilder.AnimationType.Move:
					directionalAnimation = aiAnimator.MoveAnimation;
					break;
				case NPCBuilder.AnimationType.Idle:
					directionalAnimation = aiAnimator.IdleAnimation;
					break;
				case NPCBuilder.AnimationType.Flight:
					directionalAnimation = aiAnimator.FlightAnimation;
					break;
				case NPCBuilder.AnimationType.Hit:
					directionalAnimation = aiAnimator.HitAnimation;
					break;
				case NPCBuilder.AnimationType.Talk:
					directionalAnimation = aiAnimator.TalkAnimation;
					break;
			}
			bool flag = directionalAnimation != null;
			bool flag2 = flag;
			DirectionalAnimation result;
			if (flag2)
			{
				result = directionalAnimation;
			}
			else
			{
				result = null;
			}
			return result;
		}

		// Token: 0x06000071 RID: 113 RVA: 0x00005F50 File Offset: 0x00004150
		public static void AssignDirectionalAnimation(this AIAnimator aiAnimator, string name, DirectionalAnimation animation, NPCBuilder.AnimationType type)
		{
			switch (type)
			{
				case NPCBuilder.AnimationType.Move:
					aiAnimator.MoveAnimation = animation;
					break;
				case NPCBuilder.AnimationType.Idle:
					aiAnimator.IdleAnimation = animation;
					break;
				case NPCBuilder.AnimationType.Fidget:
					aiAnimator.IdleFidgetAnimations.Add(animation);
					break;
				case NPCBuilder.AnimationType.Flight:
					aiAnimator.FlightAnimation = animation;
					break;
				case NPCBuilder.AnimationType.Hit:
					aiAnimator.HitAnimation = animation;
					break;
				case NPCBuilder.AnimationType.Talk:
					aiAnimator.TalkAnimation = animation;
					break;
				default:
					aiAnimator.OtherAnimations.Add(new AIAnimator.NamedDirectionalAnimation
					{
						anim = animation,
						name = name
					});
					break;
			}
		}

		// Token: 0x020000D6 RID: 214
		public enum AnimationType
		{
			// Token: 0x04000187 RID: 391
			Move,
			// Token: 0x04000188 RID: 392
			Idle,
			// Token: 0x04000189 RID: 393
			Fidget,
			// Token: 0x0400018A RID: 394
			Flight,
			// Token: 0x0400018B RID: 395
			Hit,
			// Token: 0x0400018C RID: 396
			Talk,
			// Token: 0x0400018D RID: 397
			Other
		}
	}
}
