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
    - dog pathfinding gives null reference exceptions in hat room
*/

namespace Alexandria.cAPI
{
  public static class InfiniteRoom
  {
    // private const string BASE_RES_PATH = "Alexandria/cAPI/Resources/"; //NOTE: restore once reintegrated into Alexandria
    // private const string BASE_RES_SUFFIX = ".png";
    private const string BASE_RES_PATH = "CwaffingTheGungy/Resources/";
    private const string BASE_RES_SUFFIX = ".png";

    private const float HALLWAY_X        = 138.2f;
    private const float HALLWAY_Y        = 80.7f;
    private const float FIRST_SEGMENT_X  = HALLWAY_X + 2f;
    private const float SEGMENT_WIDTH    = 2f;
    private const float PEDESTAL_Z       = 10.8f;
    private const float PEDESTAL_SPACING = 2.4f; //NOTE: mildly concerning this isn't the same as SEGMENT_WIDTH
    private const float HAT_Z_OFFSET     = 1f;
    private const int MIN_ROOM_WIDTH     = 10;

    static int roomWidth = 10;
    static GameObject hallwaySegment;
    static GameObject entrance;
    static List<HatPedestal> pedastals = new List<HatPedestal>();
    public static List<GameObject> objects = new List<GameObject>();
    public static bool updateRoom = true;
    public static bool hatRoomNeedsInit = true;

    /// <summary>Regenerates the hat room every time the Breach loads</summary>
    [HarmonyPatch(typeof(Foyer), nameof(Foyer.ProcessPlayerEnteredFoyer))]
    private class ProcessPlayerEnteredFoyerPatch
    {
        static void Postfix(Foyer __instance, PlayerController p)
        {
          if (!hatRoomNeedsInit)
            return;
          RegenHatRoom();
          hatRoomNeedsInit = false;
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

    public static void RegenHatRoom()
    {
        foreach (var obj in InfiniteRoom.objects)
            UnityEngine.GameObject.Destroy(obj);
        InfiniteRoom.objects.Clear();
        InfiniteRoom.Init();
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

        roomWidth = Mathf.Max(MIN_ROOM_WIDTH, Mathf.CeilToInt(Hatabase.Hats.Count / 2));
        hallwaySegment = ItemAPI.ItemBuilder.AddSpriteToObject("Hallway", $"{BASE_RES_PATH}hallway-seg{BASE_RES_SUFFIX}");
        var hallwayEnd = ItemAPI.ItemBuilder.AddSpriteToObject("HallwayEnd", $"{BASE_RES_PATH}hallwayEnd{BASE_RES_SUFFIX}");
        entrance = ItemAPI.ItemBuilder.AddSpriteToObject("Entrance", $"{BASE_RES_PATH}Entrance{BASE_RES_SUFFIX}");
        var plainPedestal = ItemAPI.ItemBuilder.AddSpriteToObject("plainPedastal", $"{BASE_RES_PATH}pedestal{BASE_RES_SUFFIX}");
        var goldPedestal = ItemAPI.ItemBuilder.AddSpriteToObject("plainPedastal", $"{BASE_RES_PATH}pedestal_gold{BASE_RES_SUFFIX}");
        var hallwaySegBottom = ItemAPI.ItemBuilder.AddSpriteToObject("HallwayBot", $"{BASE_RES_PATH}hallway-seg-bot{BASE_RES_SUFFIX}");
        cAPIToolBox.GenerateOrAddToRigidBody(goldPedestal, CollisionLayer.HighObstacle, PixelCollider.PixelColliderGeneration.Manual, UsesPixelsAsUnitSize: true, dimensions: new IntVector2(26, 23), offset: new IntVector2(0, 0));
        goldPedestal.GetComponent<tk2dSprite>().HeightOffGround = -3;

        cAPIToolBox.GenerateOrAddToRigidBody(plainPedestal, CollisionLayer.HighObstacle, PixelCollider.PixelColliderGeneration.Manual, UsesPixelsAsUnitSize: true, dimensions: new IntVector2(26, 23), offset: new IntVector2(0, 0));
        plainPedestal.GetComponent<tk2dSprite>().HeightOffGround = -3;

        cAPIToolBox.GenerateOrAddToRigidBody(hallwaySegment, CollisionLayer.HighObstacle, PixelCollider.PixelColliderGeneration.Manual, UsesPixelsAsUnitSize: true, dimensions: new IntVector2(32, 25), offset: new IntVector2(0, 100));
        cAPIToolBox.GenerateOrAddToRigidBody(hallwaySegBottom, CollisionLayer.HighObstacle, PixelCollider.PixelColliderGeneration.Manual, UsesPixelsAsUnitSize: true, dimensions: new IntVector2(32, 9), offset: new IntVector2(0, -8));

        cAPIToolBox.GenerateOrAddToRigidBody(hallwayEnd, CollisionLayer.HighObstacle, PixelCollider.PixelColliderGeneration.Manual, UsesPixelsAsUnitSize: true, dimensions: new IntVector2(32, 25), offset: new IntVector2(0, 100));
        cAPIToolBox.GenerateOrAddToRigidBody(hallwayEnd, CollisionLayer.HighObstacle, PixelCollider.PixelColliderGeneration.Manual, UsesPixelsAsUnitSize: true, dimensions: new IntVector2(8, 142), offset: new IntVector2(23, 0));
        var segmentSprite = hallwaySegment.GetComponent<tk2dSprite>();
        segmentSprite.HeightOffGround = -15;
        hallwayEnd.GetComponent<tk2dSprite>().HeightOffGround = -15;
        entrance.GetComponent<tk2dSprite>().HeightOffGround = -4;
        cAPIToolBox.GenerateOrAddToRigidBody(entrance, CollisionLayer.HighObstacle, PixelCollider.PixelColliderGeneration.Manual, UsesPixelsAsUnitSize: true, dimensions: new IntVector2(30, 30), offset: new IntVector2(20, 10));
        entrance.GetComponent<tk2dSprite>().HeightOffGround = -15;

        var entranceObj = UnityEngine.Object.Instantiate(entrance, new Vector3(59.75f, 36f, 36.875f), Quaternion.identity);
        entranceObj.GetComponent<SpeculativeRigidbody>().OnCollision += WarpToHatRoom;
        objects.Add(entranceObj);

        var firstSeg = UnityEngine.Object.Instantiate(hallwaySegment, new Vector3(HALLWAY_X, HALLWAY_Y, PEDESTAL_Z), Quaternion.identity);
        objects.Add(firstSeg);

        var firstBot = UnityEngine.Object.Instantiate(hallwaySegBottom, new Vector3(HALLWAY_X, HALLWAY_Y, PEDESTAL_Z), Quaternion.identity);
        objects.Add(firstBot);

        var returner = UnityEngine.Object.Instantiate(new GameObject(), new Vector3(HALLWAY_X, HALLWAY_Y, PEDESTAL_Z), Quaternion.identity);
        cAPIToolBox.GenerateOrAddToRigidBody(returner, CollisionLayer.HighObstacle, PixelCollider.PixelColliderGeneration.Manual, UsesPixelsAsUnitSize: true, dimensions: new IntVector2(2, 142), offset: new IntVector2(0, 0));
        returner.GetComponent<SpeculativeRigidbody>().OnCollision += WarpBackFromHatRoom;
        objects.Add(returner);

        for (int i = 0; i < roomWidth; i++)
        {
            objects.Add(UnityEngine.Object.Instantiate(hallwaySegment, new Vector3(FIRST_SEGMENT_X + (SEGMENT_WIDTH * i), HALLWAY_Y, PEDESTAL_Z), Quaternion.identity));
            objects.Add(UnityEngine.Object.Instantiate(hallwaySegBottom, new Vector3(FIRST_SEGMENT_X + (SEGMENT_WIDTH * i), HALLWAY_Y, PEDESTAL_Z), Quaternion.identity));
        }

        Hat[] allHats = Hatabase.Hats.Values.ToArray();
        for (int i = 0; i < Hatabase.Hats.Count; i++)
        {
          Hat hat = allHats[i];
          int segmentId = i / 2;
          bool onTop = (i % 2) == 0;

          GameObject ped1 = UnityEngine.Object.Instantiate(
            hat.goldenPedastal ? goldPedestal : plainPedestal, new Vector3(FIRST_SEGMENT_X + (PEDESTAL_SPACING * segmentId), onTop ? 85.7f : 81f, PEDESTAL_Z), Quaternion.identity);
          objects.Add(ped1);

          var comp1 = ped1.AddComponent<HatPedestal>();
          comp1.hat = hat;
          comp1.lowerPedastal = !onTop;

          var hat1 = new GameObject();
          tk2dSprite sprite = hat1.AddComponent<tk2dSprite>();
          sprite.SetSprite(comp1.hat.sprite.collection, comp1.hat.sprite.spriteId);
          sprite.PlaceAtLocalPositionByAnchor(new Vector2(ped1.GetComponent<tk2dSprite>().WorldCenter.x, onTop ? 87.2f : 82.3f).ToVector3ZisY(0f), tk2dBaseSprite.Anchor.LowerCenter);
          sprite.HeightOffGround = HAT_Z_OFFSET;
          sprite.UpdateZDepth();
          SpriteOutlineManager.AddOutlineToSprite(hat1.GetComponent<tk2dSprite>(), Color.black, HAT_Z_OFFSET);
          pedastals.Add(comp1);

          foyerRoom.RegisterInteractable(comp1 as IPlayerInteractable);
        }

        var end = UnityEngine.Object.Instantiate(hallwayEnd, new Vector3(FIRST_SEGMENT_X + (SEGMENT_WIDTH * roomWidth), HALLWAY_Y, PEDESTAL_Z), Quaternion.identity);
        var finalBot = UnityEngine.Object.Instantiate(hallwaySegBottom, new Vector3(FIRST_SEGMENT_X + (SEGMENT_WIDTH * roomWidth), HALLWAY_Y, PEDESTAL_Z), Quaternion.identity);
        objects.Add(end);
        objects.Add(finalBot);

        updateRoom = false;
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

      WarpToPoint(player, new Vector2(140.5f, 84.7f));
      Pixelator.Instance.DoOcclusionLayer = false;
    }

    private static void WarpBackFromHatRoom(CollisionData obj)
    {
      if (obj.OtherRigidbody.gameObject.GetComponent<PlayerController>() is not PlayerController player)
        return;

      WarpToPoint(player, new Vector2(57.75f, 36.75f));
      Pixelator.Instance.DoOcclusionLayer = true;
    }

    private static void WarpToPoint(PlayerController p, Vector2 position)
    {
      p.usingForcedInput = true;
      if (updateRoom)
      {
        // Pixelator.Instance.FadeToBlack(0.5f, false);
        foreach(var obj in objects)
        {
          UnityEngine.GameObject.Destroy(obj);
        }
        objects.Clear();
        Init();
        // yield return new WaitUntil(() => updateRoom == false);
        // Pixelator.Instance.FadeToBlack(0.5f, true);
      }
      else
      {
        // Pixelator.Instance.FadeToBlack(0.5f, false, 0.3f);
      }
      p.WarpToPointAndBringCoopPartner(position, doFollowers: true);
      p.usingForcedInput = false;
    }
  }

  class HatPedestal : BraveBehaviour, IPlayerInteractable
  {
    public Hat hat;
    public bool lowerPedastal;

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
      return Vector2.Distance(point, lowerPedastal ? gameObject.GetComponent<tk2dSprite>().WorldTopCenter : gameObject.GetComponent<tk2dSprite>().WorldCenter);
    }

    public void Interact(PlayerController interactor)
    {
      interactor.GetComponent<HatController>().SetHat(hat);
      // TextBoxManager.ShowInfoBox(new Vector2(transform.position.x + 0.75f, transform.position.y + 2), transform, 0.25f, hat.name);
    }

    public void OnEnteredRange(PlayerController interactor)
    {
      //A method that runs whenever the player enters the interaction range of the interactable. This is what outlines it in white to show that it can be interacted with
      SpriteOutlineManager.RemoveOutlineFromSprite(base.sprite, true);
      SpriteOutlineManager.AddOutlineToSprite(base.sprite, Color.white);
      TextBoxManager.ShowInfoBox(new Vector2(transform.position.x + 0.75f ,transform.position.y + 2), transform, 3600f, hat.name);  // 1 hour duration so it persists
    }

    public void OnExitRange(PlayerController interactor)
    {
      // A method that runs whenever the player exits the interaction range of the interactable.This is what removed the white outline to show that it cannot be currently interacted with
      SpriteOutlineManager.RemoveOutlineFromSprite(base.sprite, true);
      SpriteOutlineManager.AddOutlineToSprite(base.sprite, Color.black);
      TextBoxManager.ShowInfoBox(new Vector2(transform.position.x + 0.75f ,transform.position.y + 2), transform, 0f, hat.name);  // 0 duration = disappears instantly
    }
  }

  public static class cAPIToolBox
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