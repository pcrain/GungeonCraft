using Dungeonator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using MonoMod.RuntimeDetour;
using System.Collections;


/* TODO:
    - get fading working again
*/

namespace Alexandria.cAPI
{
  public static class HatRoom
  {
    // private const string BASE_RES_PATH = "Alexandria/cAPI/Resources/"; //NOTE: restore once reintegrated into Alexandria
    private const string BASE_RES_PATH = "CwaffingTheGungy/Resources/";

    private const float HALLWAY_X        = 138.2f;
    private const float HALLWAY_Y        = 80.7f;
    private const float FIRST_SEGMENT_X  = HALLWAY_X + 2f;
    private const float SEGMENT_WIDTH    = 2f;
    private const float PEDESTAL_Z       = 10.8f;
    private const float PEDESTAL_SPACING = 2.4f; //NOTE: mildly concerning this isn't the same as SEGMENT_WIDTH
    private const float HAT_Z_OFFSET     = 1f;
    private const int MIN_SEGMENTS       = 10;

    private static bool hatRoomNeedsInit = true;

    /// <summary>Regenerates the hat room every time the Breach loads</summary>
    [HarmonyPatch(typeof(Foyer), nameof(Foyer.ProcessPlayerEnteredFoyer))]
    private class ProcessPlayerEnteredFoyerPatch
    {
        static void Postfix(Foyer __instance, PlayerController p)
        {
          if (!hatRoomNeedsInit)
            return;
          HatRoom.Init();
          hatRoomNeedsInit = false;
        }
    }

    /// <summary>Prevent Huntress' dog from throwing a zillion null reference exceptions due to being unable to pathfind in the hat room</summary>
    [HarmonyPatch(typeof(AIActor), nameof(AIActor.PathfindToPosition))]
    private class FixBrokenPathfindingPatch
    {
        static bool Prefix(AIActor __instance, Vector2 targetPosition, Vector2? overridePathEnd, bool smooth, Pathfinding.CellValidator cellValidator, Pathfinding.ExtraWeightingFunction extraWeightingFunction, CellTypes? overridePathableTiles, bool canPassOccupied)
        {
            int tilePosition = __instance.PathTile.x + __instance.PathTile.y * Pathfinding.Pathfinder.Instance.m_width;
            if (tilePosition > Pathfinding.Pathfinder.Instance.m_nodes.Length)
              return false; // skip the original method
            if (Pathfinding.Pathfinder.Instance.m_nodes[tilePosition].CellData == null)
              return false; // skip the original method

            return true; // call the original method
        }
    }

    /// <summary>Marks the hat room in need of regeneration every time the Breach is unloaded</summary>
    [HarmonyPatch(typeof(Foyer), nameof(Foyer.OnDepartedFoyer))]
    private class OnDepartedFoyerPatch
    {
        static void Postfix(Foyer __instance)
        {
          hatRoomNeedsInit = true;
        }
    }

    private static void MakeRigidBody(this GameObject g, IntVector2 dimensions, IntVector2 offset)
    {
      g.GenerateOrAddToRigidBody(CollisionLayer.HighObstacle, PixelCollider.PixelColliderGeneration.Manual, UsesPixelsAsUnitSize: true, dimensions: dimensions, offset: offset);
    }

    public static void Init()
    {
      if (Hatabase.Hats.Count == 0)
      {
        Debug.Log("No Hats so no hat room!");
        return;
      }

      try
      {
        RoomHandler foyerRoom = GameManager.Instance.Dungeon.data.Entrance;
        //NOTE: funky math since segment width and pedestal spacing are not the same
        int numRoomSegments = Mathf.Max(MIN_SEGMENTS, Mathf.CeilToInt((Mathf.Ceil(0.5f * Hatabase.Hats.Count) * PEDESTAL_SPACING) / SEGMENT_WIDTH));

        GameObject hallwaySegment = ItemAPI.ItemBuilder.AddSpriteToObject("Hallway", $"{BASE_RES_PATH}hallway-seg.png");
        hallwaySegment.MakeRigidBody(dimensions: new IntVector2(32, 25), offset: new IntVector2(0, 100));
        hallwaySegment.GetComponent<tk2dSprite>().HeightOffGround = -15;

        GameObject entrance = ItemAPI.ItemBuilder.AddSpriteToObject("Entrance", $"{BASE_RES_PATH}Entrance.png");
        entrance.MakeRigidBody(dimensions: new IntVector2(30, 30), offset: new IntVector2(20, 10));
        entrance.GetComponent<tk2dSprite>().HeightOffGround = -15;

        GameObject hallwayEnd = ItemAPI.ItemBuilder.AddSpriteToObject("HallwayEnd", $"{BASE_RES_PATH}hallwayEnd.png");
        hallwayEnd.MakeRigidBody(dimensions: new IntVector2(32, 25), offset: new IntVector2(0, 100));
        hallwayEnd.MakeRigidBody(dimensions: new IntVector2(8, 142), offset: new IntVector2(23, 0));
        hallwayEnd.GetComponent<tk2dSprite>().HeightOffGround = -15;

        GameObject plainPedestal = ItemAPI.ItemBuilder.AddSpriteToObject("plainPedestal", $"{BASE_RES_PATH}pedestal.png");
        plainPedestal.MakeRigidBody(dimensions: new IntVector2(26, 23), offset: new IntVector2(0, 0));
        plainPedestal.GetComponent<tk2dSprite>().HeightOffGround = -3;

        GameObject goldPedestal = ItemAPI.ItemBuilder.AddSpriteToObject("goldPedestal", $"{BASE_RES_PATH}pedestal_gold.png");
        goldPedestal.MakeRigidBody(dimensions: new IntVector2(26, 23), offset: new IntVector2(0, 0));
        goldPedestal.GetComponent<tk2dSprite>().HeightOffGround = -3;

        GameObject hallwaySegBottom = ItemAPI.ItemBuilder.AddSpriteToObject("HallwayBot", $"{BASE_RES_PATH}hallway-seg-bot.png");
        hallwaySegBottom.MakeRigidBody(dimensions: new IntVector2(32, 9), offset: new IntVector2(0, -8));

        GameObject entranceObj = UnityEngine.Object.Instantiate(entrance, new Vector3(59.75f, 36f, 36.875f), Quaternion.identity);
        entranceObj.GetComponent<SpeculativeRigidbody>().OnCollision += WarpToHatRoom;

        UnityEngine.Object.Instantiate(hallwaySegment, new Vector3(HALLWAY_X, HALLWAY_Y, PEDESTAL_Z), Quaternion.identity); // first top segment

        UnityEngine.Object.Instantiate(hallwaySegBottom, new Vector3(HALLWAY_X, HALLWAY_Y, PEDESTAL_Z), Quaternion.identity); // first bottom segment

        GameObject returner = UnityEngine.Object.Instantiate(new GameObject(), new Vector3(HALLWAY_X, HALLWAY_Y, PEDESTAL_Z), Quaternion.identity);
        returner.MakeRigidBody(dimensions: new IntVector2(2, 142), offset: new IntVector2(0, 0));
        returner.GetComponent<SpeculativeRigidbody>().OnCollision += WarpBackFromHatRoom;

        for (int i = 0; i < numRoomSegments; i++)
        {
            Vector3 pos = new Vector3(FIRST_SEGMENT_X + (SEGMENT_WIDTH * i), HALLWAY_Y, PEDESTAL_Z);
            UnityEngine.Object.Instantiate(hallwaySegment, pos, Quaternion.identity);
            UnityEngine.Object.Instantiate(hallwaySegBottom, pos, Quaternion.identity);
        }

        Hat[] allHats = Hatabase.Hats.Values.ToArray();
        for (int i = 0; i < Hatabase.Hats.Count; i++)
        {
          Hat hat = allHats[i];
          int segmentId = i / 2;
          bool onTop = (i % 2) == 0;

          GameObject pedObj = UnityEngine.Object.Instantiate(
            hat.goldenPedestal ? goldPedestal : plainPedestal,
            new Vector3(FIRST_SEGMENT_X + (PEDESTAL_SPACING * segmentId), onTop ? 85.7f : 81f, PEDESTAL_Z),
            Quaternion.identity);

          HatPedestal pedestal = pedObj.AddComponent<HatPedestal>();
          pedestal.hat = hat;
          pedestal.lowerPedestal = !onTop;

          GameObject hat1 = new GameObject();
          tk2dSprite sprite = hat1.AddComponent<tk2dSprite>();
          sprite.SetSprite(pedestal.hat.sprite.collection, pedestal.hat.sprite.spriteId);
          sprite.PlaceAtLocalPositionByAnchor(
            new Vector2(pedObj.GetComponent<tk2dSprite>().WorldCenter.x, onTop ? 87.2f : 82.3f).ToVector3ZisY(0f),
            tk2dBaseSprite.Anchor.LowerCenter);
          sprite.HeightOffGround = HAT_Z_OFFSET;
          sprite.UpdateZDepth();
          SpriteOutlineManager.AddOutlineToSprite(hat1.GetComponent<tk2dSprite>(), Color.black, HAT_Z_OFFSET);

          foyerRoom.RegisterInteractable(pedestal as IPlayerInteractable);
        }

        UnityEngine.Object.Instantiate(hallwayEnd, // last top segment
          new Vector3(FIRST_SEGMENT_X + (SEGMENT_WIDTH * numRoomSegments), HALLWAY_Y, PEDESTAL_Z), Quaternion.identity);

        UnityEngine.Object.Instantiate(hallwaySegBottom, // last bottom segment
          new Vector3(FIRST_SEGMENT_X + (SEGMENT_WIDTH * numRoomSegments), HALLWAY_Y, PEDESTAL_Z), Quaternion.identity);
      }
      catch(Exception e)
      {
        ETGModConsole.Log("Error setting up hat room: " + e);
      }
    }

    private static void WarpToHatRoom(CollisionData obj)
    {
      if (obj.OtherRigidbody.gameObject.GetComponent<PlayerController>() is not PlayerController player)
        return;

      GameManager.Instance.StartCoroutine(WarpToPoint(player, new Vector2(140.5f, 84f)));
      Pixelator.Instance.DoOcclusionLayer = false;
    }

    private static void WarpBackFromHatRoom(CollisionData obj)
    {
      if (obj.OtherRigidbody.gameObject.GetComponent<PlayerController>() is not PlayerController player)
        return;

      GameManager.Instance.StartCoroutine(WarpToPoint(player, new Vector2(57.75f, 36.75f)));
      Pixelator.Instance.DoOcclusionLayer = true;
    }

    private static IEnumerator WarpToPoint(PlayerController p, Vector2 position)
    {
      p.usingForcedInput = true;
      Pixelator.Instance.FadeToBlack(0.1f, false);
      yield return new WaitForSeconds(0.15f);
      p.WarpToPointAndBringCoopPartner(position, doFollowers: true);
      yield return new WaitForSeconds(0.05f);
      Pixelator.Instance.FadeToBlack(0.1f, true);
      p.usingForcedInput = false;
    }
  }

  class HatPedestal : BraveBehaviour, IPlayerInteractable
  {
    public Hat hat;
    public bool lowerPedestal;

    public string GetAnimationState(PlayerController interactor, out bool shouldBeFlipped)
    {
      //Some boilerplate code for determining if the interactable should be flipped
      shouldBeFlipped = false;
      return string.Empty;
    }

    public float GetOverrideMaxDistance()
    {
      return 1f;
    }

    public float GetDistanceToPoint(Vector2 point)
    {
      return Vector2.Distance(point, lowerPedestal ? gameObject.GetComponent<tk2dSprite>().WorldTopCenter : gameObject.GetComponent<tk2dSprite>().WorldCenter);
    }

    public void Interact(PlayerController interactor)
    {
      interactor.GetComponent<HatController>().SetHat(hat);
    }

    public void OnEnteredRange(PlayerController interactor)
    {
      //A method that runs whenever the player enters the interaction range of the interactable. This is what outlines it in white to show that it can be interacted with
      SpriteOutlineManager.RemoveOutlineFromSprite(base.sprite, true);
      SpriteOutlineManager.AddOutlineToSprite(base.sprite, Color.white);
      TextBoxManager.ShowInfoBox(new Vector2(transform.position.x + 0.75f ,transform.position.y + 2), transform, 3600f, hat.hatName); // 1 hour duration so it persists
    }

    public void OnExitRange(PlayerController interactor)
    {
      // A method that runs whenever the player exits the interaction range of the interactable.This is what removed the white outline to show that it cannot be currently interacted with
      SpriteOutlineManager.RemoveOutlineFromSprite(base.sprite, true);
      SpriteOutlineManager.AddOutlineToSprite(base.sprite, Color.black);
      TextBoxManager.ShowInfoBox(new Vector2(transform.position.x + 0.75f ,transform.position.y + 2), transform, 0f, hat.hatName); // 0 duration = disappears instantly
    }
  }

  public static class cAPIToolBox
  {
      public static SpeculativeRigidbody GenerateOrAddToRigidBody(this GameObject targetObject, CollisionLayer collisionLayer, PixelCollider.PixelColliderGeneration colliderGenerationMode = PixelCollider.PixelColliderGeneration.Tk2dPolygon, bool collideWithTileMap = false, bool CollideWithOthers = true, bool CanBeCarried = true, bool CanBePushed = false, bool RecheckTriggers = false, bool IsTrigger = false, bool replaceExistingColliders = false, bool UsesPixelsAsUnitSize = false, IntVector2? dimensions = null, IntVector2? offset = null)
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
