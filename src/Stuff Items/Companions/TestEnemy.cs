using Gungeon;
using ItemAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NevernamedsItems
{
    public class TestEnemyClass : AIActor
    {
        public static GameObject prefab;
        public static readonly string guid = "TESTENEMYGUID832974628653498";
        private static tk2dSpriteCollectionData TestEnemyCollection;
        public static GameObject shootpoint;
        public static void Init()
        {
            TestEnemyClass.BuildPrefab();
        }
        public static void BuildPrefab()
        {
            if (!(prefab != null || AdvEnemyBuilder.Dictionary.ContainsKey(guid)))
            {
                prefab = AdvEnemyBuilder.BuildPrefab("Test Enemy", guid, spritePaths[0], new IntVector2(14, 5), new IntVector2(5, 5), false);
                var companion = prefab.AddComponent<EnemyBehavior>();
                //Actor Variables
                companion.aiActor.MovementSpeed = 7f;
                companion.aiActor.CollisionDamage = 1f;
                companion.aiActor.HasShadow = true;
                companion.aiActor.IgnoreForRoomClear = false;
                companion.aiActor.CanTargetPlayers = true;
                companion.aiActor.CanTargetEnemies = false;
                companion.aiActor.PreventFallingInPitsEver = false;
                companion.aiActor.CollisionKnockbackStrength = 10f;
                companion.aiActor.procedurallyOutlined = true;
                companion.aiActor.PreventBlackPhantom = false;
                //Body Variables
                companion.aiActor.specRigidbody.CollideWithOthers = true;
                companion.aiActor.specRigidbody.CollideWithTileMap = true;

                //Health Variables
                companion.aiActor.healthHaver.PreventAllDamage = false;
                companion.aiActor.healthHaver.SetHealthMaximum(15f, null, false);
                companion.aiActor.healthHaver.ForceSetCurrentHealth(15f);
                //Other Variables
                companion.aiActor.knockbackDoer.weight = 10f;
                //AnimatorVariables
                companion.aiAnimator.HitReactChance = 1f;
                companion.aiAnimator.faceSouthWhenStopped = false;
                companion.aiAnimator.faceTargetWhenStopped = true;
                companion.aiActor.SetIsFlying(true, "Flying Entity");
                AdvEnemyBuilder.Strings.Enemies.Set("#TEST_ENEMY_NAME_SMALL", "Test Enemy");
                companion.aiActor.OverrideDisplayName = "#TEST_ENEMY_NAME_SMALL";
                companion.aiActor.specRigidbody.PixelColliders.Clear();
                companion.aiActor.gameObject.AddComponent<tk2dSpriteAttachPoint>();
                companion.aiActor.gameObject.AddComponent<ObjectVisibilityManager>();
                companion.aiActor.specRigidbody.PixelColliders.Add(new PixelCollider
                {
                    ColliderGenerationMode = PixelCollider.PixelColliderGeneration.Manual,
                    CollisionLayer = CollisionLayer.EnemyCollider,
                    IsTrigger = false,
                    BagleUseFirstFrameOnly = false,
                    SpecifyBagelFrame = string.Empty,
                    BagelColliderNumber = 0,
                    ManualOffsetX = 14,
                    ManualOffsetY = 5,
                    ManualWidth = 5,
                    ManualHeight = 5,
                    ManualDiameter = 0,
                    ManualLeftX = 0,
                    ManualLeftY = 0,
                    ManualRightX = 0,
                    ManualRightY = 0
                });
                companion.aiActor.specRigidbody.PixelColliders.Add(new PixelCollider
                {
                    ColliderGenerationMode = PixelCollider.PixelColliderGeneration.Manual,
                    CollisionLayer = CollisionLayer.EnemyHitBox,
                    IsTrigger = false,
                    BagleUseFirstFrameOnly = false,
                    SpecifyBagelFrame = string.Empty,
                    BagelColliderNumber = 0,
                    ManualOffsetX = 14,
                    ManualOffsetY = 5,
                    ManualWidth = 5,
                    ManualHeight = 5,
                    ManualDiameter = 0,
                    ManualLeftX = 0,
                    ManualLeftY = 0,
                    ManualRightX = 0,
                    ManualRightY = 0,
                });
                AIAnimator aiAnimator = companion.aiAnimator;
                aiAnimator.IdleAnimation = new DirectionalAnimation { Type = DirectionalAnimation.DirectionType.TwoWayHorizontal, Flipped = new DirectionalAnimation.FlipType[0], AnimNames = new string[] { "idle_right", "idle_left", } };
                aiAnimator.MoveAnimation = new DirectionalAnimation { Type = DirectionalAnimation.DirectionType.TwoWayHorizontal, Flipped = new DirectionalAnimation.FlipType[0], AnimNames = new string[] { "move_right", "move_left", } };




                aiAnimator.OtherAnimations = new List<AIAnimator.NamedDirectionalAnimation> { new AIAnimator.NamedDirectionalAnimation { name = "sex", anim = new DirectionalAnimation { Prefix = "sex", Type = DirectionalAnimation.DirectionType.TwoWayHorizontal, Flipped = new DirectionalAnimation.FlipType[0], AnimNames = new string[] { "sex_right", "sex_left", } } }, };
                if (TestEnemyCollection == null)
                {
                    TestEnemyCollection = SpriteBuilder.ConstructCollection(prefab, "TestEnemyCollection");
                    UnityEngine.Object.DontDestroyOnLoad(TestEnemyCollection);
                    for (int i = 0; i < spritePaths.Length; i++)
                    {
                        SpriteBuilder.AddSpriteToCollection(spritePaths[i], TestEnemyCollection);
                    }
                    SpriteBuilder.AddAnimation(companion.spriteAnimator, TestEnemyCollection, new List<int> { 0, 1, 2, 3, }, "idle_right", tk2dSpriteAnimationClip.WrapMode.Once).fps = 15
                    ; SpriteBuilder.AddAnimation(companion.spriteAnimator, TestEnemyCollection, new List<int> { 4, 5, 6, 7, }, "idle_left", tk2dSpriteAnimationClip.WrapMode.Once).fps = 15
                     ; SpriteBuilder.AddAnimation(companion.spriteAnimator, TestEnemyCollection, new List<int> { 8, 9, 10, 11, }, "move_right", tk2dSpriteAnimationClip.WrapMode.Once).fps = 15;
                    SpriteBuilder.AddAnimation(companion.spriteAnimator, TestEnemyCollection, new List<int> { 12, 13, 14, 15, }, "move_left", tk2dSpriteAnimationClip.WrapMode.Once).fps = 15;
                    SpriteBuilder.AddAnimation(companion.spriteAnimator, TestEnemyCollection, new List<int> { 16, 17, 18, }, "sex_right", tk2dSpriteAnimationClip.WrapMode.Once).fps = 20;
                    SpriteBuilder.AddAnimation(companion.spriteAnimator, TestEnemyCollection, new List<int> { 19, 20, 21, }, "sex_left", tk2dSpriteAnimationClip.WrapMode.Once).fps = 20;

                }
                var bs = prefab.GetComponent<BehaviorSpeculator>();
                prefab.GetComponent<ObjectVisibilityManager>();
                BehaviorSpeculator behaviorSpeculator = EnemyDatabase.GetOrLoadByGuid("01972dee89fc4404a5c408d50007dad5").behaviorSpeculator;
                bs.OverrideBehaviors = behaviorSpeculator.OverrideBehaviors;
                bs.OtherBehaviors = behaviorSpeculator.OtherBehaviors;

                //ATTACK BEHAVIOUR SETUP (Must be done BY HAND)
                shootpoint = new GameObject("fuck");
                shootpoint.transform.parent = companion.transform;
                shootpoint.transform.position = (companion.sprite.WorldCenter + new Vector2(0, 0));
                GameObject m_CachedGunAttachPoint = companion.transform.Find("fuck").gameObject;
                bs.TargetBehaviors = new List<TargetBehaviorBase>{ //Add your target behaviours here!
new TargetPlayerBehavior{Radius = 1000,LineOfSight = false,ObjectPermanence = true,SearchInterval = 0.25f,PauseOnTargetSwitch = false,PauseTime = 0.25f,},
};

                bs.MovementBehaviors = new List<MovementBehaviorBase>() { //Add your movement behaviours here!
new SeekTargetBehavior() { StopWhenInRange = true,CustomRange = 5,LineOfSight = false,ReturnToSpawn = true,SpawnTetherDistance = -1,PathInterval = 0.25f,SpecifyRange = false,MinActiveRange = 0,MaxActiveRange = 0},
};

                bs.AttackBehaviors = new List<AttackBehaviorBase>()
                {
                    //Attack behaviours must be added here MANUALLY
                };

                bs.InstantFirstTick = behaviorSpeculator.InstantFirstTick;
                bs.TickInterval = behaviorSpeculator.TickInterval;
                bs.StartingFacingDirection = behaviorSpeculator.StartingFacingDirection;
                bs.PostAwakenDelay = behaviorSpeculator.PostAwakenDelay;
                bs.RemoveDelayOnReinforce = behaviorSpeculator.RemoveDelayOnReinforce;
                bs.OverrideStartingFacingDirection = behaviorSpeculator.OverrideStartingFacingDirection;
                bs.SkipTimingDifferentiator = behaviorSpeculator.SkipTimingDifferentiator;



                if (companion.GetComponent<EncounterTrackable>() != null)
                {
                    UnityEngine.Object.Destroy(companion.GetComponent<EncounterTrackable>());
                }
                Game.Enemies.Add("nn:Test_Enemy".ToLower(), companion.aiActor);
                SpriteBuilder.AddSpriteToCollection("AmmonomiconSprite", AdvEnemyBuilder.ammonomiconCollection);
                companion.encounterTrackable = companion.gameObject.AddComponent<EncounterTrackable>();
                companion.encounterTrackable.journalData = new JournalEntry();
                companion.encounterTrackable.EncounterGuid = "nn:Test Enemy".ToLower();
                companion.encounterTrackable.prerequisites = new DungeonPrerequisite[0];
                companion.encounterTrackable.journalData.SuppressKnownState = false;
                companion.encounterTrackable.journalData.IsEnemy = true;
                companion.encounterTrackable.journalData.SuppressInAmmonomicon = false;
                companion.encounterTrackable.ProxyEncounterGuid = "";
                companion.encounterTrackable.journalData.AmmonomiconSprite = "Bingly Bungly Boo";
                companion.encounterTrackable.journalData.enemyPortraitSprite = ItemAPI.ResourceExtractor.GetTextureFromResource("Bungle Bish bash bosh");
                AdvEnemyBuilder.Strings.Enemies.Set("#TEST_ENEMY", "Test Enemy");
                AdvEnemyBuilder.Strings.Enemies.Set("#TEST_ENEMY_SHORTDESC", "Quote");
                AdvEnemyBuilder.Strings.Enemies.Set("#TEST_ENEMY_LONGDESC", "Description");
                companion.encounterTrackable.journalData.PrimaryDisplayName = "#TEST_ENEMY";
                companion.encounterTrackable.journalData.NotificationPanelDescription = "#TEST_ENEMY_SHORTDESC";
                companion.encounterTrackable.journalData.AmmonomiconFullEntry = "#TEST_ENEMY_LONGDESC";
                AdvEnemyBuilder.AddEnemyToDatabase(companion.gameObject, "nn:Test_Enemy".ToLower());
                EnemyDatabase.GetEntry("nn:Test_Enemy".ToLower()).ForcedPositionInAmmonomicon = 15;
                EnemyDatabase.GetEntry("nn:Test_Enemy".ToLower()).isInBossTab = false;
                EnemyDatabase.GetEntry("nn:Test_Enemy".ToLower()).isNormalEnemy = true;

            }
        }
        private static string[] spritePaths = new string[]{
"Sex/Penis/Cock/Idle_right_001",
"Sex/Penis/Cock/Idle_right_002",
"Sex/Penis/Cock/Idle_right_003",
"Sex/Penis/Cock/Idle_right_004",
"Sex/Penis/Cock/Idle_left_001",
"Sex/Penis/Cock/Idle_left_002",
"Sex/Penis/Cock/Idle_left_003",
"Sex/Penis/Cock/Idle_left_004",
"Sex/Penis/Cock/Walk_right_001",
"Sex/Penis/Cock/Walk_right_002",
"Sex/Penis/Cock/Walk_right_003",
"Sex/Penis/Cock/Walk_right_004",
"Sex/Penis/Cock/Walk_left_001",
"Sex/Penis/Cock/Walk_left_002",
"Sex/Penis/Cock/Walk_left_003",
"Sex/Penis/Cock/Walk_left_004",
"Sex/Penis/Cock/Fuck_right_001",
"Sex/Penis/Cock/Fuck_right_002",
"Sex/Penis/Cock/Fuck_right_003",
"Sex/Penis/Cock/Fuck_left_001",
"Sex/Penis/Cock/Fuck_left_002",
"Sex/Penis/Cock/Fuck_left_003",

 };
    }

}


