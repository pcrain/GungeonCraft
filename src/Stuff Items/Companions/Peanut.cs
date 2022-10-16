using System;
using System.Collections;
using System.Collections.Generic;
using Dungeonator;
using Gungeon;
using ItemAPI;
using UnityEngine;

namespace NevernamedsItems
{
    public class Peanut : PassiveItem
    {
        public static void Init()
        {
            string name = "Peanut";
            string resourcePath = "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_icon";
            GameObject gameObject = new GameObject();
            var companionItem = gameObject.AddComponent<CompanionItem>();
            ItemBuilder.AddSpriteToObject(name, resourcePath, gameObject);
            string shortDesc = "Prince of the Pea";
            string longDesc = "This young, soft-shelled Gun Nut sought the teachings of Ser Manuel, but was lost in the labyrinthine and confusing Halls of Knowledge for many years."+"\n\nHe now wields the mighty Peablade, made from a Peashooter that he found in a chest.";
            companionItem.SetupItem(shortDesc, longDesc, "nn");
            companionItem.quality = PickupObject.ItemQuality.A;
            companionItem.CompanionGuid = Peanut.guid;

            Peanut.BuildPrefab();
        }
        private static tk2dSpriteCollectionData PeanutAnimationCollection;
        public static void BuildPrefab()
        {
            bool flag = Peanut.prefab != null || CompanionBuilder.companionDictionary.ContainsKey(Peanut.guid);
            if (!flag)
            {
                Peanut.prefab = CompanionBuilder.BuildPrefab("Peanut Companion", Peanut.guid, "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_idlesouth_001", new IntVector2(16, 11), new IntVector2(8, 9));
                var companionController = Peanut.prefab.AddComponent<PeanutCompanionBehaviour>();
                companionController.companionID = CompanionController.CompanionIdentifier.NONE;
                companionController.aiActor.MovementSpeed = 5f;                
                companionController.aiActor.healthHaver.PreventAllDamage = true;
                companionController.aiActor.CollisionDamage = 0f;
                companionController.aiActor.specRigidbody.CollideWithOthers = false;
                companionController.aiActor.specRigidbody.CollideWithTileMap = false;

                BehaviorSpeculator component = Peanut.prefab.GetComponent<BehaviorSpeculator>();
                component.AttackBehaviors.Add(new CustomCompanionBehaviours.PeanutAttackBehaviour());
                component.MovementBehaviors.Add(new CustomCompanionBehaviours.SimpleCompanionApproach());
                component.MovementBehaviors.Add(new CompanionFollowPlayerBehavior
                {
                    IdleAnimations = new string[]
                    {
                        "idle"
                    }
                });
                //SET UP ANIMATIONS
                AIAnimator aiAnimator = companionController.aiAnimator;
                aiAnimator.
                aiAnimator.MoveAnimation = new DirectionalAnimation
                {
                    Type = DirectionalAnimation.DirectionType.FourWayCardinal,
                    Flipped = new DirectionalAnimation.FlipType[4],
                    AnimNames = new string[]
                        {
                        "move_north",
                        "move_east",
                        "move_south",
                        "move_west",
                        }
                };
                aiAnimator.IdleAnimation = new DirectionalAnimation
                {
                    Type = DirectionalAnimation.DirectionType.FourWayCardinal,
                    Flipped = new DirectionalAnimation.FlipType[4],
                    AnimNames = new string[]
                        {
                        "idle_north",
                        "idle_east",
                        "idle_south",
                        "idle_west",
                        }
                    
                };
                aiAnimator.OtherAnimations = new List<AIAnimator.NamedDirectionalAnimation>
                {
                    new AIAnimator.NamedDirectionalAnimation
                    {
                        name = "attack",
                        anim = new DirectionalAnimation
                        {
                            Prefix = "attack",
                            Type = DirectionalAnimation.DirectionType.FourWayCardinal,
                             Flipped = new DirectionalAnimation.FlipType[4],
                            AnimNames = new string[]
                            {
                                 "attack_west",
                                  "attack_east",
                                  "attack_north",
                                  "attack_south"
                             }
                        }
                    }, };

                //ADD SPRITES TO THE ANIMATIONS
                if (Peanut.PeanutAnimationCollection == null)
                {
                    Peanut.PeanutAnimationCollection = SpriteBuilder.ConstructCollection(Peanut.prefab, "PeanutCompanion_Collection");
                    UnityEngine.Object.DontDestroyOnLoad(Peanut.PeanutAnimationCollection);
                    for (int i = 0; i < Peanut.spritePaths.Length; i++)
                    {
                        SpriteBuilder.AddSpriteToCollection(Peanut.spritePaths[i], Peanut.PeanutAnimationCollection);
                    }
                    //Idling Animation
                    SpriteBuilder.AddAnimation(companionController.spriteAnimator, Peanut.PeanutAnimationCollection, new List<int>
                    {
                        0,
                        1,
                        2,
                        3
                    }, "idle_west", tk2dSpriteAnimationClip.WrapMode.Loop).fps = 4f;
                    SpriteBuilder.AddAnimation(companionController.spriteAnimator, Peanut.PeanutAnimationCollection, new List<int>
                    {
                        4,
                        5,
                        6,
                        7,
                    }, "idle_east", tk2dSpriteAnimationClip.WrapMode.Loop).fps = 4f;
                    SpriteBuilder.AddAnimation(companionController.spriteAnimator, Peanut.PeanutAnimationCollection, new List<int>
                    {
                        8,
                        9,
                        10,
                        11,
                    }, "idle_north", tk2dSpriteAnimationClip.WrapMode.Loop).fps = 4f;
                    SpriteBuilder.AddAnimation(companionController.spriteAnimator, Peanut.PeanutAnimationCollection, new List<int>
                    {
                        12,
                        13,
                        14,
                        15,
                    }, "idle_south", tk2dSpriteAnimationClip.WrapMode.Loop).fps = 4f;
                    //Running Animation
                    SpriteBuilder.AddAnimation(companionController.spriteAnimator, Peanut.PeanutAnimationCollection, new List<int>
                    {
                        16,
                        17,
                        18,
                        19,
                        20,
                        21
                    }, "move_west", tk2dSpriteAnimationClip.WrapMode.Loop).fps = 8f;
                    SpriteBuilder.AddAnimation(companionController.spriteAnimator, Peanut.PeanutAnimationCollection, new List<int>
                    {
                        22,
                        23,
                        24,
                       25,
                        26,
                        27
                    }, "move_east", tk2dSpriteAnimationClip.WrapMode.Loop).fps = 8f;
                    SpriteBuilder.AddAnimation(companionController.spriteAnimator, Peanut.PeanutAnimationCollection, new List<int>
                    {
                        28,
                       29,
                        30,
                        31,
                        32,
                        33
                    }, "move_north", tk2dSpriteAnimationClip.WrapMode.Loop).fps = 8f;
                    SpriteBuilder.AddAnimation(companionController.spriteAnimator, Peanut.PeanutAnimationCollection, new List<int>
                    {
                        34,
                        35,
                        36,
                        37,
                        38,
                        39
                    }, "move_south", tk2dSpriteAnimationClip.WrapMode.Loop).fps = 8f;
                    //ATTACKING ANIMATIONS
                    SpriteBuilder.AddAnimation(companionController.spriteAnimator, Peanut.PeanutAnimationCollection, new List<int>
                    {
                        40,
                        41,
                        42,
                        43,
                        44,
                        45,
                        46,
                        47
                    }, "attack_east", tk2dSpriteAnimationClip.WrapMode.Once).fps = 8f;
                    SpriteBuilder.AddAnimation(companionController.spriteAnimator, Peanut.PeanutAnimationCollection, new List<int>
                    {
                        48,
                        49,
                        50,
                        51,
                        52,
                        53,
                        54,
                        55
                    }, "attack_west", tk2dSpriteAnimationClip.WrapMode.Once).fps = 8f;
                    SpriteBuilder.AddAnimation(companionController.spriteAnimator, Peanut.PeanutAnimationCollection, new List<int>
                    {
                        56,
                        57,
                        58,
                        59,
                        60,
                        61,
                        62
                    }, "attack_north", tk2dSpriteAnimationClip.WrapMode.Once).fps = 8f;
                    SpriteBuilder.AddAnimation(companionController.spriteAnimator, Peanut.PeanutAnimationCollection, new List<int>
                    {
                        63,
                        64,
                       65,
                        66,
                        67,
                        68,
                        69,
                        70
                    }, "attack_south", tk2dSpriteAnimationClip.WrapMode.Once).fps = 8f;
                }
            }
        }
        private static string[] spritePaths = new string[]
        {
            //Idle West
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_idlewest_001", //0     
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_idlewest_002", //1     
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_idlewest_003", //2     
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_idlewest_004", //3
            //Idle East
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_idleeast_001", //4  
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_idleeast_002", //5    
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_idleeast_003", //6     
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_idleeast_004", //7
            //Idle North
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_idlenorth_001", //8  
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_idlenorth_002", //9 
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_idlenorth_003", //10  
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_idlenorth_004", //11
            //Idle South
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_idlesouth_001", //12
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_idlesouth_002", //13
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_idlesouth_003", //14
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_idlesouth_004", //15
            //Move West
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_movewest_001", //16
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_movewest_002", //17
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_movewest_003", //18
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_movewest_004", //19
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_movewest_005", //20
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_movewest_006", //21
            //Move East
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_moveeast_001", //22
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_moveeast_002", //23
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_moveeast_003", //24
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_moveeast_004", //25
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_moveeast_005", //26
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_moveeast_006", //27
            //Move North
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_movenorth_001", //28
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_movenorth_002", //29
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_movenorth_003", //30
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_movenorth_004", //31
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_movenorth_005", //32
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_movenorth_006", //33
            //Move South
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_movesouth_001", //34
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_movesouth_002", //35
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_movesouth_003", //36
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_movesouth_004", //37
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_movesouth_005", //38
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_movesouth_006", //39
            //Attack East
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_attackeast_001", //40
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_attackeast_002", // 41
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_attackeast_003", // 42
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_attackeast_004", // 43
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_attackeast_005", // 44
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_attackeast_006", // 45
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_attackeast_007", // 46
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_attackeast_008", // 47
            //Attack West
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_attackwest_001", //48
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_attackwest_002", //49
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_attackwest_003", //50
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_attackwest_004", //51
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_attackwest_005", //52
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_attackwest_006", //53
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_attackwest_007", //54
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_attackwest_008", //55
            //Attack North
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_attacknorth_001", //56
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_attacknorth_002", //57
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_attacknorth_003", //58
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_attacknorth_004", //59
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_attacknorth_005", //60
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_attacknorth_006", //61
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_attacknorth_007", //62
            //Attack South
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_attacksouth_001", //63
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_attacksouth_002", //64
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_attacksouth_003", //65
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_attacksouth_004", //66
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_attacksouth_005", //67
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_attacksouth_006", //68
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_attacksouth_007", //69
            "NevernamedsItems/Resources/Companions/PeanutCompanion/peanut_attacksouth_008", //70
        };
        public class PeanutCompanionBehaviour : CompanionController
        {
            private void Start()
            {
                this.Owner = this.m_owner;
            }

            public PlayerController Owner;
        }
        public static GameObject prefab;
        private static readonly string guid = "peanut_companion83279843696946";
    }
}