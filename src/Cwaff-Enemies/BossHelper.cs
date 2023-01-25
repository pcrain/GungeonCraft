using System;
using System.Collections.Generic;
using Gungeon;
using ItemAPI;
using EnemyAPI;
using UnityEngine;
//using DirectionType = DirectionalAnimation.DirectionType;
// using AnimationType = ItemAPI.BossBuilder.AnimationType;
using System.Collections;
using Dungeonator;
using System.Linq;
using Brave.BulletScript;
// using GungeonAPI;

namespace CwaffingTheGungy
{
  public static class BH
  {
    public static List<int> Range(int start, int end)
    {
      return Enumerable.Range(start, end-start+1).ToList();
    }

    public static void CopySaneDefaultBehavior(this BehaviorSpeculator self, BehaviorSpeculator other)
    {
      self.OverrideBehaviors               = other.OverrideBehaviors;
      self.OtherBehaviors                  = other.OtherBehaviors;
      self.InstantFirstTick                = other.InstantFirstTick;
      self.TickInterval                    = other.TickInterval;
      self.PostAwakenDelay                 = other.PostAwakenDelay;
      self.RemoveDelayOnReinforce          = other.RemoveDelayOnReinforce;
      self.OverrideStartingFacingDirection = other.OverrideStartingFacingDirection;
      self.StartingFacingDirection         = other.StartingFacingDirection;
      self.SkipTimingDifferentiator        = other.SkipTimingDifferentiator;
    }

    public static void SetDefaultColliders(this SpeculativeRigidbody self, int width, int height)
    {
      self.PixelColliders.Clear();
      self.PixelColliders.Add(new PixelCollider
        {
          ColliderGenerationMode = PixelCollider.PixelColliderGeneration.Manual,
          CollisionLayer = CollisionLayer.EnemyCollider,
          IsTrigger = false,
          BagleUseFirstFrameOnly = false,
          SpecifyBagelFrame = string.Empty,
          BagelColliderNumber = 0,
          ManualOffsetX = 0,
          ManualOffsetY = 10,
          ManualWidth = 101,
          ManualHeight = 27,
          ManualDiameter = 0,
          ManualLeftX = 0,
          ManualLeftY = 0,
          ManualRightX = 0,
          ManualRightY = 0
        });
      self.PixelColliders.Add(new PixelCollider
        {
          ColliderGenerationMode = PixelCollider.PixelColliderGeneration.Manual,
          CollisionLayer = CollisionLayer.EnemyHitBox,
          IsTrigger = false,
          BagleUseFirstFrameOnly = false,
          SpecifyBagelFrame = string.Empty,
          BagelColliderNumber = 0,
          ManualOffsetX = 0,
          ManualOffsetY = 10,
          ManualWidth = 101,
          ManualHeight = 27,
          ManualDiameter = 0,
          ManualLeftX = 0,
          ManualLeftY = 0,
          ManualRightX = 0,
          ManualRightY = 0,
        });
    }

    public static EnemyBehavior AddSaneDefaultBossBehavior(GameObject prefab, string name, string subtitle, string bossCardPath = "")
    {
      EnemyBehavior companion = prefab.AddComponent<EnemyBehavior>();
        companion.aiActor.healthHaver.PreventAllDamage = false;
        companion.aiActor.HasShadow = false;
        companion.aiActor.IgnoreForRoomClear = false;
        companion.aiActor.specRigidbody.CollideWithOthers = true;
        companion.aiActor.specRigidbody.CollideWithTileMap = true;
        companion.aiActor.PreventFallingInPitsEver = true;
        companion.aiActor.procedurallyOutlined = false;
        companion.aiActor.CanTargetPlayers = true;
        companion.aiActor.PreventBlackPhantom = false;

      // prefab.name = tableId+"_NAME";
      prefab.name = name;
      string tableId = "#"+name.Replace(" ","_").ToUpper();
      ETGMod.Databases.Strings.Enemies.Set(tableId+"_NAME", name);
      ETGMod.Databases.Strings.Enemies.Set(tableId+"_SUBTITLE", subtitle);
      ETGMod.Databases.Strings.Enemies.Set(tableId+"_QUOTE", string.Empty);
      companion.aiActor.healthHaver.overrideBossName = tableId+"_NAME";
      companion.aiActor.OverrideDisplayName = tableId+"_NAME";
      companion.aiActor.ActorName = tableId+"_NAME";
      companion.aiActor.name = tableId+"_NAME";
      GenericIntroDoer miniBossIntroDoer = BH.AddSaneDefaultIntroDoer(prefab);
        if (!String.IsNullOrEmpty(bossCardPath))
        {
          Texture2D bossCardTexture = ResourceExtractor.GetTextureFromResource(bossCardPath);
          miniBossIntroDoer.portraitSlideSettings = new PortraitSlideSettings()
          {
            bossNameString = tableId+"_NAME",
            bossSubtitleString = tableId+"_SUBTITLE",
            bossQuoteString = tableId+"_QUOTE",
            bossSpritePxOffset = IntVector2.Zero,
            topLeftTextPxOffset = IntVector2.Zero,
            bottomRightTextPxOffset = IntVector2.Zero,
            bgColor = Color.cyan
          };
          miniBossIntroDoer.portraitSlideSettings.bossArtSprite = bossCardTexture;
          miniBossIntroDoer.SkipBossCard = false;
          prefab.GetComponent<EnemyBehavior>().aiActor.healthHaver.bossHealthBar = HealthHaver.BossBarType.MainBar;
        }
        else
        {
          miniBossIntroDoer.SkipBossCard = true;
          prefab.GetComponent<EnemyBehavior>().aiActor.healthHaver.bossHealthBar = HealthHaver.BossBarType.SubbossBar;
        }
        miniBossIntroDoer.SkipFinalizeAnimation = true;
        miniBossIntroDoer.RegenerateCache();


      return companion;
    }

    public static GenericIntroDoer AddSaneDefaultIntroDoer(GameObject prefab)
    {
      GenericIntroDoer miniBossIntroDoer = prefab.AddComponent<GenericIntroDoer>();
        miniBossIntroDoer.triggerType = GenericIntroDoer.TriggerType.PlayerEnteredRoom;
        miniBossIntroDoer.specifyIntroAiAnimator = null;
        miniBossIntroDoer.initialDelay = 0.15f;
        miniBossIntroDoer.cameraMoveSpeed = 14;
        miniBossIntroDoer.introAnim = string.Empty;
        // miniBossIntroDoer.introAnim = "intro"; //TODO: check if this actually exists
        miniBossIntroDoer.introDirectionalAnim = string.Empty;
        miniBossIntroDoer.continueAnimDuringOutro = false;
        miniBossIntroDoer.BossMusicEvent = "Play_MUS_Boss_Theme_Beholster";
        miniBossIntroDoer.PreventBossMusic = false;
        miniBossIntroDoer.InvisibleBeforeIntroAnim = true;
        miniBossIntroDoer.preIntroAnim = string.Empty;
        miniBossIntroDoer.preIntroDirectionalAnim = string.Empty;
        miniBossIntroDoer.cameraFocus = null;
        miniBossIntroDoer.roomPositionCameraFocus = Vector2.zero;
        miniBossIntroDoer.restrictPlayerMotionToRoom = false;
        miniBossIntroDoer.fusebombLock = false;
        miniBossIntroDoer.AdditionalHeightOffset = 0;
      return miniBossIntroDoer;
    }

    public static void AddAnimation(this EnemyBehavior self, tk2dSpriteCollectionData collection, List<int> ids, string name, float fps, bool loop, DirectionalAnimation.DirectionType direction = DirectionalAnimation.DirectionType.None)
    {
      tk2dSpriteAnimationClip.WrapMode loopMode = loop
        ? tk2dSpriteAnimationClip.WrapMode.Loop
        : tk2dSpriteAnimationClip.WrapMode.Once;
      SpriteBuilder.AddAnimation(self.spriteAnimator, collection, ids, name, loopMode).fps = fps;
      if (direction != DirectionalAnimation.DirectionType.None)
      {
        self.aiAnimator.IdleAnimation = new DirectionalAnimation
        {
          Type = direction,
          Prefix = name,
          AnimNames = new string[1], // TODO: this might not be one if our directional type is not single
          Flipped = new DirectionalAnimation.FlipType[1]
        };
      }
    }

    public static tk2dSpriteCollectionData LoadSpriteCollection(GameObject prefab, string[] spritePaths)
    {
      tk2dSpriteCollectionData bossSprites = SpriteBuilder.ConstructCollection(prefab, (prefab.name+" Collection").Replace(" ","_"));
      UnityEngine.Object.DontDestroyOnLoad(bossSprites);
      for (int i = 0; i < spritePaths.Length; i++)
        SpriteBuilder.AddSpriteToCollection(spritePaths[i], bossSprites);
      return bossSprites;
    }

    public static IEnumerator WaitForSecondsInvariant(float time)
    {
      for (float elapsed = 0f; elapsed < time; elapsed += GameManager.INVARIANT_DELTA_TIME) { yield return null; }
      yield break;
    }
  }
}
