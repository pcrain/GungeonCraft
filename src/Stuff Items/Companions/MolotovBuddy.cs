using System;
using System.Collections;
using System.Collections.Generic;
using Dungeonator;
using Gungeon;
using ItemAPI;
using UnityEngine;

namespace NevernamedsItems
{
    public class MolotovBuddy : PassiveItem
    {
        public static void Init()
        {
            string name = "Molotov Buddy";
            string resourcePath = "NevernamedsItems/Resources/Companions/MolotovBud/molotovbud_icon";
            GameObject gameObject = new GameObject();
            var companionItem = gameObject.AddComponent<MolotovBuddy>();
            ItemBuilder.AddSpriteToObject(name, resourcePath, gameObject);
            string shortDesc = "Hot Headed Friend";
            string longDesc = "This regular molotov was given sentience by the magic of the Gungeon, and with it he gained a firey attitude." + "\n\nIf he could, he would burn the world. What he hates most of all is people calling him cute.";
            companionItem.SetupItem(shortDesc, longDesc, "nn");
            companionItem.quality = PickupObject.ItemQuality.D;

            MolotovBuddy.BuildPrefab();
        }
        float respawnTimer = 0;
        float respawnTimer2 = 0;
        GameObject extantBud = null;
        GameObject extantBud2 = null;
        public override void Update()
        {
            if (Owner)
            {
                if (respawnTimer <= 0)
                {
                    AIActor orLoadByGuid = EnemyDatabase.GetOrLoadByGuid(guid);
                    Vector3 vector = Owner.transform.position;
                    GameObject extantCompanion2 = UnityEngine.Object.Instantiate<GameObject>(orLoadByGuid.gameObject, vector, Quaternion.identity);
                    MolotovCompanionBehaviour orAddComponent = extantCompanion2.GetOrAddComponent<MolotovCompanionBehaviour>();
                    extantBud = extantCompanion2;
                    orAddComponent.Initialize(Owner);
                    if (orAddComponent.specRigidbody)
                    {
                        PhysicsEngine.Instance.RegisterOverlappingGhostCollisionExceptions(orAddComponent.specRigidbody, null, false);
                    }
                    respawnTimer = 7;
                }
                else
                {
                    if (extantBud == null)
                    {
                        respawnTimer -= BraveTime.DeltaTime;
                    }
                }
                #region Bud2
                if (Owner.PlayerHasActiveSynergy("Molly Tov"))
                {
                    if (respawnTimer2 <= 0)
                    {
                        AIActor orLoadByGuid = EnemyDatabase.GetOrLoadByGuid(guid);
                        Vector3 vector = Owner.transform.position;
                        GameObject extantCompanion2 = UnityEngine.Object.Instantiate<GameObject>(orLoadByGuid.gameObject, vector, Quaternion.identity);
                        MolotovCompanionBehaviour orAddComponent = extantCompanion2.GetOrAddComponent<MolotovCompanionBehaviour>();
                        extantBud2 = extantCompanion2;
                        orAddComponent.Initialize(Owner);
                        if (orAddComponent.specRigidbody)
                        {
                            PhysicsEngine.Instance.RegisterOverlappingGhostCollisionExceptions(orAddComponent.specRigidbody, null, false);
                        }
                        respawnTimer2 = 7;
                    }
                    else
                    {
                        if (extantBud2 == null)
                        {
                            respawnTimer2 -= BraveTime.DeltaTime;
                        }
                    }
                }
                #endregion
            }
            base.Update();
        }

        private static tk2dSpriteCollectionData MolotovBudAnimationCollection;
        public static void BuildPrefab()
        {
            bool flag = MolotovBuddy.prefab != null || CompanionBuilder.companionDictionary.ContainsKey(MolotovBuddy.guid);
            if (!flag)
            {
                MolotovBuddy.prefab = CompanionBuilder.BuildPrefab("Molotov Bud Companion", MolotovBuddy.guid, "NevernamedsItems/Resources/Companions/MolotovBud/molotovbud_idle_left_001", new IntVector2(12, 1), new IntVector2(5, 5));
                var companionController = MolotovBuddy.prefab.AddComponent<MolotovCompanionBehaviour>();
                companionController.CanInterceptBullets = false;
                companionController.companionID = CompanionController.CompanionIdentifier.NONE;
                companionController.aiActor.MovementSpeed = 6f;
                companionController.aiActor.healthHaver.PreventAllDamage = true;
                companionController.aiActor.CollisionDamage = 0f;
                companionController.aiActor.specRigidbody.CollideWithOthers = false;
                companionController.aiActor.specRigidbody.CollideWithTileMap = false;
                BehaviorSpeculator component = MolotovBuddy.prefab.GetComponent<BehaviorSpeculator>();
                CustomCompanionBehaviours.SimpleCompanionApproach approach = new CustomCompanionBehaviours.SimpleCompanionApproach();
                approach.DesiredDistance = 3;
                component.MovementBehaviors.Add(approach);
                component.MovementBehaviors.Add(new CompanionFollowPlayerBehavior
                {
                    IdleAnimations = new string[]
                    {
                        "idle"
                    }
                });

                //SET UP ANIMATIONS
                AIAnimator aiAnimator = companionController.aiAnimator;
                aiAnimator.MoveAnimation = new DirectionalAnimation
                {
                    Type = DirectionalAnimation.DirectionType.TwoWayHorizontal,
                    Flipped = new DirectionalAnimation.FlipType[2],
                    AnimNames = new string[]
                        {
                        "move_right",
                        "move_left"
                        }
                };
                aiAnimator.IdleAnimation = new DirectionalAnimation
                {
                    Type = DirectionalAnimation.DirectionType.TwoWayHorizontal,
                    Flipped = new DirectionalAnimation.FlipType[2],
                    AnimNames = new string[]
                        {
                        "idle_right",
                        "idle_left"
                        }
                };
                aiAnimator.OtherAnimations = new List<AIAnimator.NamedDirectionalAnimation>
                {
                    new AIAnimator.NamedDirectionalAnimation
                    {
                        name = "burst",
                        anim = new DirectionalAnimation
                        {
                            Type = DirectionalAnimation.DirectionType.Single,
                            Prefix = "burst",
                            AnimNames = new string[1],
                            Flipped = new DirectionalAnimation.FlipType[1]
                        }
                    }, };

                //ADD SPRITES TO THE ANIMATIONS
                bool flag3 = MolotovBuddy.MolotovBudAnimationCollection == null;
                if (flag3)
                {
                    MolotovBuddy.MolotovBudAnimationCollection = SpriteBuilder.ConstructCollection(MolotovBuddy.prefab, "MolotovBudCompanion_Collection");
                    UnityEngine.Object.DontDestroyOnLoad(MolotovBuddy.MolotovBudAnimationCollection);
                    for (int i = 0; i < MolotovBuddy.spritePaths.Length; i++)
                    {
                        SpriteBuilder.AddSpriteToCollection(MolotovBuddy.spritePaths[i], MolotovBuddy.MolotovBudAnimationCollection);
                    }
                    //Idling Animation
                    SpriteBuilder.AddAnimation(companionController.spriteAnimator, MolotovBuddy.MolotovBudAnimationCollection, new List<int>
                    {
                        4,
                        5,
                        6,
                        7,
                    }, "idle_right", tk2dSpriteAnimationClip.WrapMode.Loop).fps = 8f;
                    SpriteBuilder.AddAnimation(companionController.spriteAnimator, MolotovBuddy.MolotovBudAnimationCollection, new List<int>
                    {
                        0,
                        1,
                        2,
                        3,
                    }, "idle_left", tk2dSpriteAnimationClip.WrapMode.Loop).fps = 8f;
                    //Running Animation
                    SpriteBuilder.AddAnimation(companionController.spriteAnimator, MolotovBuddy.MolotovBudAnimationCollection, new List<int>
                    {
                        14,
                        15,
                        16,
                        17,
                        18,
                        19
                    }, "move_right", tk2dSpriteAnimationClip.WrapMode.Loop).fps = 8f;
                    SpriteBuilder.AddAnimation(companionController.spriteAnimator, MolotovBuddy.MolotovBudAnimationCollection, new List<int>
                    {
                        8,
                        9,
                        10,
                        11,
                        12,
                        13
                    }, "move_left", tk2dSpriteAnimationClip.WrapMode.Loop).fps = 8f;
                    SpriteBuilder.AddAnimation(companionController.spriteAnimator, MolotovBuddy.MolotovBudAnimationCollection, new List<int>
                    {
                        20,
                        21,
                        22,
                        23,
                        24,
                        25
                    }, "burst", tk2dSpriteAnimationClip.WrapMode.Once).fps = 12f;

                }
            }
        }
        private static string[] spritePaths = new string[]
        {
            //IdleLeft
            "NevernamedsItems/Resources/Companions/MolotovBud/molotovbud_idle_left_001", //0
            "NevernamedsItems/Resources/Companions/MolotovBud/molotovbud_idle_left_002", //1
            "NevernamedsItems/Resources/Companions/MolotovBud/molotovbud_idle_left_003", //2
            "NevernamedsItems/Resources/Companions/MolotovBud/molotovbud_idle_left_004", //3
            //IdleRight
            "NevernamedsItems/Resources/Companions/MolotovBud/molotovbud_idle_right_001", //4
            "NevernamedsItems/Resources/Companions/MolotovBud/molotovbud_idle_right_002", //5
            "NevernamedsItems/Resources/Companions/MolotovBud/molotovbud_idle_right_003", //6
            "NevernamedsItems/Resources/Companions/MolotovBud/molotovbud_idle_right_004", //7
            //RunLeft
            "NevernamedsItems/Resources/Companions/MolotovBud/molotovbud_move_left_001", //8
            "NevernamedsItems/Resources/Companions/MolotovBud/molotovbud_move_left_002", //9
            "NevernamedsItems/Resources/Companions/MolotovBud/molotovbud_move_left_003", //10
            "NevernamedsItems/Resources/Companions/MolotovBud/molotovbud_move_left_004", //11
            "NevernamedsItems/Resources/Companions/MolotovBud/molotovbud_move_left_005", //12
            "NevernamedsItems/Resources/Companions/MolotovBud/molotovbud_move_left_006", //13
            //RunRight
            "NevernamedsItems/Resources/Companions/MolotovBud/molotovbud_move_right_001", //14
            "NevernamedsItems/Resources/Companions/MolotovBud/molotovbud_move_right_002", //15
            "NevernamedsItems/Resources/Companions/MolotovBud/molotovbud_move_right_003", //16
            "NevernamedsItems/Resources/Companions/MolotovBud/molotovbud_move_right_004", //17
            "NevernamedsItems/Resources/Companions/MolotovBud/molotovbud_move_right_005", //18
            "NevernamedsItems/Resources/Companions/MolotovBud/molotovbud_move_right_006", //19
            //SpawnObject
            "NevernamedsItems/Resources/Companions/MolotovBud/molotovbud_burst_001", //20
            "NevernamedsItems/Resources/Companions/MolotovBud/molotovbud_burst_002", //21
            "NevernamedsItems/Resources/Companions/MolotovBud/molotovbud_burst_003", //22
            "NevernamedsItems/Resources/Companions/MolotovBud/molotovbud_burst_004", //23
            "NevernamedsItems/Resources/Companions/MolotovBud/molotovbud_burst_005", //24
            "NevernamedsItems/Resources/Companions/MolotovBud/molotovbud_burst_006", //25   
        };

        public class MolotovCompanionBehaviour : CompanionController
        {
            private void Start()
            {
                this.Owner = this.m_owner;
            }
            public override void Update()
            {
                if (this && Owner && !Dungeon.IsGenerating)
                {
                    if (this.aiActor && this.aiActor.OverrideTarget && !isAttacking)
                    {
                        if (Vector2.Distance(this.specRigidbody.UnitCenter, this.aiActor.OverrideTarget.UnitCenter) < 5)
                        {
                            StartCoroutine(DoBurst());
                            isAttacking = true;
                        }
                    }
                }
                base.Update();
            }
            bool isAttacking = false;
            private IEnumerator DoBurst()
            {
                this.aiActor.MovementSpeed = 0;
                this.aiAnimator.PlayUntilFinished("burst", false, null, -1f, false);
                yield return new WaitForSeconds(0.25f);
                if (this.aiActor.OverrideTarget)
                {
                    DeadlyDeadlyGoopManager goopManagerForGoopType = DeadlyDeadlyGoopManager.GetGoopManagerForGoopType(EasyGoopDefinitions.FireDef);
                    AkSoundEngine.PostEvent("Play_OBJ_glassbottle_shatter_01", this.gameObject);
                    Vector2 vector = this.specRigidbody.UnitCenter;
                    Vector2 normalized = (this.aiActor.OverrideTarget.UnitCenter - vector).normalized;
                    goopManagerForGoopType.TimedAddGoopLine(this.specRigidbody.UnitCenter, this.specRigidbody.UnitCenter + normalized * 7, 1f, 0.5f);
                }
                yield return new WaitForSeconds(0.25f);
                this.sprite.renderer.enabled = false;
                this.aiActor.EraseFromExistence();
                yield break;
            }
            public PlayerController Owner;
        }
        public static GameObject prefab;
        private static readonly string guid = "moltovbuddy723839ehufhwifweugfsfgskd";
    }
}
