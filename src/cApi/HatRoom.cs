using Dungeonator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using MonoMod.RuntimeDetour;
using System.Collections;

namespace Alexandria.cAPI
{
  public static class InfiniteRoom
  {
    // private const string BASE_RES_PATH = "Alexandria/cAPI/Resources/"; //NOTE: restore once reintegrated into Alexandria
    // private const string BASE_RES_SUFFIX = ".png";
    private const string BASE_RES_PATH = "";
    private const string BASE_RES_SUFFIX = "";

    static int roomWidth = 10;
    static GameObject hallwaySegment;
    static GameObject entrance;
    static List<hatPedastal> pedastals = new List<hatPedastal>();
    public static List<GameObject> objects = new List<GameObject>();
    public static bool updateRoom = true;
    public static void Init()
    {
      try
      {
        ETGModConsole.Log(0);
        if (Hatabase.Hats.Count == 0)
        {
          Debug.Log("No Hats so no hat room!");
          return;
        }
        ETGModConsole.Log(1);
        roomWidth = Mathf.Max(10, Mathf.CeilToInt(Hatabase.Hats.Count / 2));
        hallwaySegment = ItemAPI.ItemBuilder.AddSpriteToObject("Hallway", $"{BASE_RES_PATH}hallway-seg{BASE_RES_SUFFIX}");
        var hallwayEnd = ItemAPI.ItemBuilder.AddSpriteToObject("HallwayEnd", $"{BASE_RES_PATH}hallwayEnd{BASE_RES_SUFFIX}");
        entrance = ItemAPI.ItemBuilder.AddSpriteToObject("Entrance", $"{BASE_RES_PATH}Entrance{BASE_RES_SUFFIX}");
        var plainPedestal = ItemAPI.ItemBuilder.AddSpriteToObject("plainPedastal", $"{BASE_RES_PATH}pedestal{BASE_RES_SUFFIX}");
        var goldPedestal = ItemAPI.ItemBuilder.AddSpriteToObject("plainPedastal", $"{BASE_RES_PATH}pedestal_gold{BASE_RES_SUFFIX}");
        var hallwaySegBottom = ItemAPI.ItemBuilder.AddSpriteToObject("HallwayBot", $"{BASE_RES_PATH}hallway-seg-bot{BASE_RES_SUFFIX}");
        ETGModConsole.Log(2);
        cAPIToolBox.GenerateOrAddToRigidBody(goldPedestal, CollisionLayer.HighObstacle, PixelCollider.PixelColliderGeneration.Manual, UsesPixelsAsUnitSize: true, dimensions: new IntVector2(26, 23), offset: new IntVector2(0, 0));
        goldPedestal.GetComponent<tk2dSprite>().HeightOffGround = -3;

        cAPIToolBox.GenerateOrAddToRigidBody(plainPedestal, CollisionLayer.HighObstacle, PixelCollider.PixelColliderGeneration.Manual, UsesPixelsAsUnitSize: true, dimensions: new IntVector2(26, 23), offset: new IntVector2(0, 0));
        plainPedestal.GetComponent<tk2dSprite>().HeightOffGround = -3;

        cAPIToolBox.GenerateOrAddToRigidBody(hallwaySegment, CollisionLayer.HighObstacle, PixelCollider.PixelColliderGeneration.Manual, UsesPixelsAsUnitSize: true, dimensions: new IntVector2(32, 25), offset: new IntVector2(0, 100));
        cAPIToolBox.GenerateOrAddToRigidBody(hallwaySegBottom, CollisionLayer.HighObstacle, PixelCollider.PixelColliderGeneration.Manual, UsesPixelsAsUnitSize: true, dimensions: new IntVector2(32, 9), offset: new IntVector2(0, -8));

        cAPIToolBox.GenerateOrAddToRigidBody(hallwayEnd, CollisionLayer.HighObstacle, PixelCollider.PixelColliderGeneration.Manual, UsesPixelsAsUnitSize: true, dimensions: new IntVector2(32, 25), offset: new IntVector2(0, 100));
        cAPIToolBox.GenerateOrAddToRigidBody(hallwayEnd, CollisionLayer.HighObstacle, PixelCollider.PixelColliderGeneration.Manual, UsesPixelsAsUnitSize: true, dimensions: new IntVector2(8, 142), offset: new IntVector2(23, 0));
        ETGModConsole.Log(3);
        var sprite = hallwaySegment.GetComponent<tk2dSprite>();
        sprite.HeightOffGround = -15;
        hallwayEnd.GetComponent<tk2dSprite>().HeightOffGround = -15;
        entrance.GetComponent<tk2dSprite>().HeightOffGround = -4;
        cAPIToolBox.GenerateOrAddToRigidBody(entrance, CollisionLayer.HighObstacle, PixelCollider.PixelColliderGeneration.Manual, UsesPixelsAsUnitSize: true, dimensions: new IntVector2(30, 30), offset: new IntVector2(20, 10));
        entrance.GetComponent<tk2dSprite>().HeightOffGround = -15;

        var entranceObj = UnityEngine.Object.Instantiate(entrance, new Vector3(59.75f, 36f, 36.875f), Quaternion.identity);
        var firstSeg = UnityEngine.Object.Instantiate(hallwaySegment, new Vector3(138.2f, 80.7f, 10.8f), Quaternion.identity);
        var firstBot = UnityEngine.Object.Instantiate(hallwaySegBottom, new Vector3(138.2f, 80.7f, 10.8f), Quaternion.identity);
        var returner = UnityEngine.Object.Instantiate(new GameObject(), new Vector3(138.2f, 80.7f, 10.8f), Quaternion.identity);

        objects.Add(entranceObj);
        objects.Add(firstSeg);
        objects.Add(firstBot);
        objects.Add(returner);
        ETGModConsole.Log(4);
        cAPIToolBox.GenerateOrAddToRigidBody(returner, CollisionLayer.HighObstacle, PixelCollider.PixelColliderGeneration.Manual, UsesPixelsAsUnitSize: true, dimensions: new IntVector2(2, 142), offset: new IntVector2(0, 0));
        returner.GetComponent<SpeculativeRigidbody>().OnCollision += warpBack;
        int newestAddedHat = 0;
        ETGModConsole.Log(5);
        for (int i = 0; i < roomWidth; i++)
        {
          var newestSeg = UnityEngine.Object.Instantiate(hallwaySegment, new Vector3(140.2f + (2 * i), 80.7f, 10.8f), Quaternion.identity);
          var newestBot = UnityEngine.Object.Instantiate(hallwaySegBottom, new Vector3(140.2f + (2 * i), 80.7f, 10.8f), Quaternion.identity);
          objects.Add(newestSeg);
          objects.Add(newestBot);

          if (Hatabase.Hats.Count > newestAddedHat + 1)
          {
            GameObject ped1;
            GameObject ped2;
            if (Hatabase.Hats.Values.ToArray()[newestAddedHat].goldenPedastal)
            {
              ped1 = UnityEngine.Object.Instantiate(goldPedestal, new Vector3(140.2f + (2.4f * i), 85.7f, 10.8f), Quaternion.identity);
            }
            else
            {
              ped1 = UnityEngine.Object.Instantiate(plainPedestal, new Vector3(140.2f + (2.4f * i), 85.7f, 10.8f), Quaternion.identity);
            }
            if (Hatabase.Hats.Values.ToArray()[newestAddedHat + 1].goldenPedastal)
            {
              ped2 = UnityEngine.Object.Instantiate(plainPedestal, new Vector3(140.2f + (2.4f * i), 81f, 10.8f), Quaternion.identity);
            }
            else
            {
              ped2 = UnityEngine.Object.Instantiate(plainPedestal, new Vector3(140.2f + (2.4f * i), 81f, 10.8f), Quaternion.identity);
            }
            cAPIToolBox.GenerateOrAddToRigidBody(ped2, CollisionLayer.HighObstacle, PixelCollider.PixelColliderGeneration.Manual, UsesPixelsAsUnitSize: true, dimensions: new IntVector2(26, 6), offset: new IntVector2(0, -6));

            objects.Add(ped1);
            objects.Add(ped2);
            var comp1 = ped1.AddComponent<hatPedastal>();
            comp1.hat = Hatabase.Hats.Values.ToArray()[newestAddedHat];
            comp1.lowerPedastal = false;

            var hat1 = ItemAPI.ItemBuilder.AddSpriteToObject("placeholder", $"{BASE_RES_PATH}empty{BASE_RES_SUFFIX}");
            ItemAPI.SpriteBuilder.CopyFrom<tk2dBaseSprite>(hat1.GetComponent<tk2dBaseSprite>(), Hatabase.Hats.Values.ToArray()[newestAddedHat].sprite);
            hat1.GetComponent<tk2dSprite>().PlaceAtLocalPositionByAnchor(new Vector3(ped2.GetComponent<tk2dSprite>().WorldCenter.x, 87.2f, 10.8f), tk2dBaseSprite.Anchor.LowerCenter);
            newestAddedHat++;

            var comp2 = ped2.AddComponent<hatPedastal>();
            comp2.hat = Hatabase.Hats.Values.ToArray()[newestAddedHat];
            comp2.lowerPedastal = true;
            SpriteOutlineManager.AddOutlineToSprite(hat1.GetComponent<tk2dSprite>(), Color.black);

            var hat2 = ItemAPI.ItemBuilder.AddSpriteToObject("placeholder", $"{BASE_RES_PATH}empty{BASE_RES_SUFFIX}");
            ItemAPI.SpriteBuilder.CopyFrom<tk2dBaseSprite>(hat2.GetComponent<tk2dBaseSprite>(), Hatabase.Hats.Values.ToArray()[newestAddedHat].sprite);
            hat2.GetComponent<tk2dSprite>().PlaceAtLocalPositionByAnchor(new Vector3(ped2.GetComponent<tk2dSprite>().WorldCenter.x, 82.3f, 10.8f), tk2dBaseSprite.Anchor.LowerCenter);

            SpriteOutlineManager.AddOutlineToSprite(hat2.GetComponent<tk2dSprite>(), Color.black);

            pedastals.Add(ped1.GetComponent<hatPedastal>());
            pedastals.Add(ped2.GetComponent<hatPedastal>());
            newestAddedHat++;
            ETGModConsole.Log(6);
          }
          else if (Hatabase.Hats.Count > newestAddedHat)
          {
            var ped = UnityEngine.Object.Instantiate(plainPedestal, new Vector3(140.2f + (2.4f * i), 85.7f, 10.8f), Quaternion.identity);
            var comp1 = ped.AddComponent<hatPedastal>();
            comp1.hat = Hatabase.Hats.Values.ToArray()[newestAddedHat];
            comp1.lowerPedastal = false;
            objects.Add(ped);
            var hat1 = ItemAPI.ItemBuilder.AddSpriteToObject("placeholder", $"{BASE_RES_PATH}empty{BASE_RES_SUFFIX}");
            ItemAPI.SpriteBuilder.CopyFrom<tk2dBaseSprite>(hat1.GetComponent<tk2dBaseSprite>(), Hatabase.Hats.Values.ToArray()[newestAddedHat].sprite);
            hat1.GetComponent<tk2dSprite>().PlaceAtLocalPositionByAnchor(new Vector3(ped.GetComponent<tk2dSprite>().WorldCenter.x, 87.2f, 10.8f), tk2dBaseSprite.Anchor.LowerCenter);

            SpriteOutlineManager.AddOutlineToSprite(hat1.GetComponent<tk2dSprite>(), Color.black);
            pedastals.Add(ped.GetComponent<hatPedastal>());
            newestAddedHat++;
            ETGModConsole.Log(7);
          }
        }

        var end = UnityEngine.Object.Instantiate(hallwayEnd, new Vector3(140.2f + (2 * (roomWidth)), 80.7f, 10.8f), Quaternion.identity);
        var finalBot = UnityEngine.Object.Instantiate(hallwaySegBottom, new Vector3(140.2f + (2 * (roomWidth)), 80.7f, 10.8f), Quaternion.identity);
        objects.Add(end);
        objects.Add(finalBot);

        entranceObj.GetComponent<SpeculativeRigidbody>().OnCollision += warp;
        updateRoom = false;
        ETGModConsole.Log(8);
      }
      catch(Exception e)
            {
        ETGModConsole.Log("Error setting up hat room: " + e);
            }
    }

    private static void warpBack(CollisionData obj)
    {
      var player = obj.OtherRigidbody.gameObject.GetComponent<PlayerController>();
      if (player != null)
      {
        ETGMod.StartGlobalCoroutine(WarpToPoint(player, new Vector2(57.75f, 36.75f)));
        Pixelator.Instance.DoOcclusionLayer = true;
      }
    }

    private static IEnumerator WarpToPoint(PlayerController p, Vector2 position)
    {
      p.usingForcedInput = true;
      if (updateRoom)
      {

        Pixelator.Instance.FadeToBlack(0.5f, false);
        foreach(var obj in objects)
        {
          UnityEngine.GameObject.Destroy(obj);
        }
        objects.Clear();
        Init();
        yield return new WaitUntil(() => updateRoom == false);
        Pixelator.Instance.FadeToBlack(0.5f, true);
      }
      else
      {
        Pixelator.Instance.FadeToBlack(0.5f, false, 0.3f);
      }
      p.WarpToPointAndBringCoopPartner(position);
      p.usingForcedInput = false;
    }

    private static void warp(CollisionData obj)
    {
      var player = obj.OtherRigidbody.gameObject.GetComponent<PlayerController>();
      if (player != null)
      {
        ETGMod.StartGlobalCoroutine(WarpToPoint(player, new Vector2(140.5f, 84.7f)));
        Pixelator.Instance.DoOcclusionLayer = false;
      }
    }
  }

  class hatPedastal : BraveBehaviour, IPlayerInteractable
  {
    public Hat hat;
    public bool lowerPedastal;
    void Update()
    {
      foreach (var player in GameManager.Instance.AllPlayers)
      {
          if (GetDistanceToPoint(player.transform.position) < GetOverrideMaxDistance() && BraveInput.GetInstanceForPlayer(player.PlayerIDX).ActiveActions.InteractAction.WasPressed)
          {
            Interact(player);
          }
          else if (GetDistanceToPoint(player.transform.position) < GetOverrideMaxDistance())
          {
            OnEnteredRange(player);
          }
          else
          {
            OnExitRange(player);
          }
      }
    }

    public string GetAnimationState(PlayerController interactor, out bool shouldBeFlipped)
    {
      //Some boilerplate code for determining if the interactable should be flipped
      shouldBeFlipped = false;
      return string.Empty;
    }

    public float GetOverrideMaxDistance()
    {
      if(lowerPedastal)
        return 1.7f;

      return 2.5f;
    }

    public override void OnDestroy()
    {
      base.OnDestroy();
    }
    public float GetDistanceToPoint(Vector2 point)
    {
      if (lowerPedastal)
      {
        return Vector2.Distance(point, gameObject.GetComponent<tk2dSprite>().WorldCenter
          .WithY(gameObject.GetComponent<tk2dSprite>().WorldBottomCenter.y)
          .WithX(gameObject.GetComponent<tk2dSprite>().WorldBottomCenter.x - 1));
      }
      return Vector2.Distance(point, gameObject.GetComponent<tk2dSprite>().WorldCenter
        .WithY(gameObject.GetComponent<tk2dSprite>().WorldTopCenter.y)
        .WithX(gameObject.GetComponent<tk2dSprite>().WorldTopCenter.x - 1));
    }

    public void Interact(PlayerController interactor)
    {
      interactor.GetComponent<HatController>().SetHat(hat);
      TextBoxManager.ShowInfoBox(new Vector2(transform.position.x + 0.75f, transform.position.y + 2), transform, 0, hat.name);
    }

    public void OnEnteredRange(PlayerController interactor)
    {
      //A method that runs whenever the player enters the interaction range of the interactable. This is what outlines it in white to show that it can be interacted with
      SpriteOutlineManager.RemoveOutlineFromSprite(base.sprite, true);
      SpriteOutlineManager.AddOutlineToSprite(base.sprite, Color.white);
      TextBoxManager.ShowInfoBox(new Vector2(transform.position.x + 0.75f ,transform.position.y + 2), transform, 0, hat.name);
    }

    public void OnExitRange(PlayerController interactor)
    {
      // A method that runs whenever the player exits the interaction range of the interactable.This is what removed the white outline to show that it cannot be currently interacted with
      SpriteOutlineManager.RemoveOutlineFromSprite(base.sprite, true);
      SpriteOutlineManager.AddOutlineToSprite(base.sprite, Color.black);
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
