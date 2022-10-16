using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Gungeon;
using MonoMod.RuntimeDetour;
using UnityEngine;

namespace ItemAPI
{
    // Token: 0x02000016 RID: 22
    public static class CompanionBuilder
    {
        // Token: 0x060000BB RID: 187 RVA: 0x00007FC8 File Offset: 0x000061C8
        public static void Init()
        {
            string companionGuid = Game.Items["dog"].GetComponent<CompanionItem>().CompanionGuid;
            AIActor orLoadByGuid = EnemyDatabase.GetOrLoadByGuid(companionGuid);
            CompanionBuilder.behaviorSpeculatorPrefab = UnityEngine.Object.Instantiate<GameObject>(orLoadByGuid.gameObject);
            foreach (object obj in CompanionBuilder.behaviorSpeculatorPrefab.transform)
            {
                Transform transform = (Transform)obj;
                bool flag = transform != CompanionBuilder.behaviorSpeculatorPrefab.transform;
                if (flag)
                {
                    UnityEngine.Object.DestroyImmediate(transform);
                }
            }
            foreach (Component component in CompanionBuilder.behaviorSpeculatorPrefab.GetComponents<Component>())
            {
                bool flag2 = component.GetType() != typeof(BehaviorSpeculator);
                if (flag2)
                {
                    UnityEngine.Object.DestroyImmediate(component);
                }
            }
            UnityEngine.Object.DontDestroyOnLoad(CompanionBuilder.behaviorSpeculatorPrefab);
            FakePrefab.MarkAsFakePrefab(CompanionBuilder.behaviorSpeculatorPrefab);
            CompanionBuilder.behaviorSpeculatorPrefab.SetActive(false);
            Hook hook = new Hook(typeof(EnemyDatabase).GetMethod("GetOrLoadByGuid", BindingFlags.Static | BindingFlags.Public), typeof(CompanionBuilder).GetMethod("GetOrLoadByGuid"));
        }

        // Token: 0x060000BC RID: 188 RVA: 0x00008118 File Offset: 0x00006318
        public static AIActor GetOrLoadByGuid(Func<string, AIActor> orig, string guid)
        {
            foreach (string text in CompanionBuilder.companionDictionary.Keys)
            {
                bool flag = text == guid;
                if (flag)
                {
                    return CompanionBuilder.companionDictionary[text].GetComponent<AIActor>();
                }
            }
            return orig(guid);
        }

        // Token: 0x060000BD RID: 189 RVA: 0x00008198 File Offset: 0x00006398
        public static GameObject BuildPrefab(string name, string guid, string defaultSpritePath, IntVector2 hitboxOffset, IntVector2 hitBoxSize)
        {
            bool flag = CompanionBuilder.companionDictionary.ContainsKey(guid);
            GameObject result;
            if (flag)
            {
                ETGModConsole.Log("CompanionBuilder: Tried to create two companion prefabs with the same GUID!", false);
                result = null;
            }
            else
            {
                GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(CompanionBuilder.behaviorSpeculatorPrefab);
                gameObject.name = name;
                tk2dSprite component = SpriteBuilder.SpriteFromResource(defaultSpritePath, gameObject).GetComponent<tk2dSprite>();
                component.SetUpSpeculativeRigidbody(hitboxOffset, hitBoxSize).CollideWithOthers = false;
                gameObject.AddComponent<tk2dSpriteAnimator>();
                gameObject.AddComponent<AIAnimator>();
                HealthHaver healthHaver = gameObject.AddComponent<HealthHaver>();
                healthHaver.RegisterBodySprite(component, false, 0);
                healthHaver.PreventAllDamage = true;
                healthHaver.SetHealthMaximum(15000f, null, false);
                healthHaver.FullHeal();
                AIActor aiactor = gameObject.AddComponent<AIActor>();
                aiactor.State = AIActor.ActorState.Normal;
                aiactor.EnemyGuid = guid;
                BehaviorSpeculator component2 = gameObject.GetComponent<BehaviorSpeculator>();
                component2.MovementBehaviors = new List<MovementBehaviorBase>();
                component2.AttackBehaviors = new List<AttackBehaviorBase>();
                component2.TargetBehaviors = new List<TargetBehaviorBase>();
                component2.OverrideBehaviors = new List<OverrideBehaviorBase>();
                component2.OtherBehaviors = new List<BehaviorBase>();
                EnemyDatabaseEntry item = new EnemyDatabaseEntry
                {
                    myGuid = guid,
                    placeableWidth = 2,
                    placeableHeight = 2,
                    isNormalEnemy = false
                };
                EnemyDatabase.Instance.Entries.Add(item);
                CompanionBuilder.companionDictionary.Add(guid, gameObject);
                UnityEngine.Object.DontDestroyOnLoad(gameObject);
                FakePrefab.MarkAsFakePrefab(gameObject);
                gameObject.SetActive(false);
                result = gameObject;
            }
            return result;
        }

        // Token: 0x060000BE RID: 190 RVA: 0x000082FC File Offset: 0x000064FC
        public static tk2dSpriteAnimationClip AddAnimation(this GameObject obj, string name, string spriteDirectory, int fps, CompanionBuilder.AnimationType type, DirectionalAnimation.DirectionType directionType = DirectionalAnimation.DirectionType.None, DirectionalAnimation.FlipType flipType = DirectionalAnimation.FlipType.None)
        {
            AIAnimator orAddComponent = obj.GetOrAddComponent<AIAnimator>();
            DirectionalAnimation directionalAnimation = orAddComponent.GetDirectionalAnimation(name, directionType, type);
            bool flag = directionalAnimation == null;
            if (flag)
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
            orAddComponent.AssignDirectionalAnimation(name, directionalAnimation, type);
            return CompanionBuilder.BuildAnimation(orAddComponent, name, spriteDirectory, fps);
        }

        // Token: 0x060000BF RID: 191 RVA: 0x000083B8 File Offset: 0x000065B8
        public static tk2dSpriteAnimationClip BuildAnimation(AIAnimator aiAnimator, string name, string spriteDirectory, int fps)
        {
            tk2dSpriteCollectionData tk2dSpriteCollectionData = aiAnimator.GetComponent<tk2dSpriteCollectionData>();
            bool flag = !tk2dSpriteCollectionData;
            if (flag)
            {
                tk2dSpriteCollectionData = SpriteBuilder.ConstructCollection(aiAnimator.gameObject, aiAnimator.name + "_collection");
            }
            string[] resourceNames = ResourceExtractor.GetResourceNames();
            List<int> list = new List<int>();
            for (int i = 0; i < resourceNames.Length; i++)
            {
                bool flag2 = resourceNames[i].StartsWith(spriteDirectory.Replace('/', '.'), StringComparison.OrdinalIgnoreCase);
                if (flag2)
                {
                    list.Add(SpriteBuilder.AddSpriteToCollection(resourceNames[i], tk2dSpriteCollectionData));
                }
            }
            tk2dSpriteAnimationClip tk2dSpriteAnimationClip = SpriteBuilder.AddAnimation(aiAnimator.spriteAnimator, tk2dSpriteCollectionData, list, name, tk2dSpriteAnimationClip.WrapMode.Loop);
            tk2dSpriteAnimationClip.fps = (float)fps;
            return tk2dSpriteAnimationClip;
        }

        // Token: 0x060000C0 RID: 192 RVA: 0x0000846C File Offset: 0x0000666C
        public static DirectionalAnimation GetDirectionalAnimation(this AIAnimator aiAnimator, string name, DirectionalAnimation.DirectionType directionType, CompanionBuilder.AnimationType type)
        {
            DirectionalAnimation directionalAnimation = null;
            switch (type)
            {
                case CompanionBuilder.AnimationType.Move:
                    directionalAnimation = aiAnimator.MoveAnimation;
                    break;
                case CompanionBuilder.AnimationType.Idle:
                    directionalAnimation = aiAnimator.IdleAnimation;
                    break;
                case CompanionBuilder.AnimationType.Flight:
                    directionalAnimation = aiAnimator.FlightAnimation;
                    break;
                case CompanionBuilder.AnimationType.Hit:
                    directionalAnimation = aiAnimator.HitAnimation;
                    break;
                case CompanionBuilder.AnimationType.Talk:
                    directionalAnimation = aiAnimator.TalkAnimation;
                    break;
            }
            bool flag = directionalAnimation != null;
            DirectionalAnimation result;
            if (flag)
            {
                result = directionalAnimation;
            }
            else
            {
                result = null;
            }
            return result;
        }

        // Token: 0x060000C1 RID: 193 RVA: 0x000084E4 File Offset: 0x000066E4
        public static void AssignDirectionalAnimation(this AIAnimator aiAnimator, string name, DirectionalAnimation animation, CompanionBuilder.AnimationType type)
        {
            switch (type)
            {
                case CompanionBuilder.AnimationType.Move:
                    aiAnimator.MoveAnimation = animation;
                    break;
                case CompanionBuilder.AnimationType.Idle:
                    aiAnimator.IdleAnimation = animation;
                    break;
                case CompanionBuilder.AnimationType.Fidget:
                    aiAnimator.IdleFidgetAnimations.Add(animation);
                    break;
                case CompanionBuilder.AnimationType.Flight:
                    aiAnimator.FlightAnimation = animation;
                    break;
                case CompanionBuilder.AnimationType.Hit:
                    aiAnimator.HitAnimation = animation;
                    break;
                case CompanionBuilder.AnimationType.Talk:
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

        // Token: 0x04000054 RID: 84
        private static GameObject behaviorSpeculatorPrefab;

        // Token: 0x04000055 RID: 85
        public static Dictionary<string, GameObject> companionDictionary = new Dictionary<string, GameObject>();

        // Token: 0x02000063 RID: 99
        public enum AnimationType
        {
            // Token: 0x0400012F RID: 303
            Move,
            // Token: 0x04000130 RID: 304
            Idle,
            // Token: 0x04000131 RID: 305
            Fidget,
            // Token: 0x04000132 RID: 306
            Flight,
            // Token: 0x04000133 RID: 307
            Hit,
            // Token: 0x04000134 RID: 308
            Talk,
            // Token: 0x04000135 RID: 309
            Other
        }
    }
}