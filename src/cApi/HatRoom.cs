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
  public static class HatRoom
  {
    // private const string BASE_RES_PATH = "Alexandria/cAPI/Resources"; //NOTE: restore once reintegrated into Alexandria
    private const string BASE_RES_PATH = "CwaffingTheGungy/Resources";

    private const float HALLWAY_X        = 138.2f;
    private const float HALLWAY_Y        = 80.7f;
    private const float FIRST_SEGMENT_X  = HALLWAY_X + 2f;
    private const float SEGMENT_WIDTH    = 2f;
    private const float PEDESTAL_Z       = 10.8f;
    private const float PEDESTAL_SPACING = 2.4f;
    private const float HAT_Z_OFFSET     = 1f;
    private const int MIN_SEGMENTS       = 10;

    private static GameObject hallwaySegment   = null;
    private static GameObject entrance         = null;
    private static GameObject hallwayEnd       = null;
    private static GameObject plainPedestal    = null;
    private static GameObject goldPedestal     = null;
    private static GameObject hallwaySegBottom = null;
    private static bool needToGenHatRoom       = true;
    private static bool createdPrefabs         = false;

    /// <summary>Regenerates the hat room every time the Breach loads</summary>
    [HarmonyPatch(typeof(Foyer), nameof(Foyer.ProcessPlayerEnteredFoyer))]
    private class ProcessPlayerEnteredFoyerPatch
    {
        static void Postfix(Foyer __instance, PlayerController p)
        {
          if (!needToGenHatRoom)
            return;
          HatRoom.CreateRealHatRoom();
          // HatRoom.Generate();
          needToGenHatRoom = false;
        }
    }

    /// <summary>Marks the hat room in need of regeneration every time the Breach is unloaded</summary>
    [HarmonyPatch(typeof(Foyer), nameof(Foyer.OnDepartedFoyer))]
    private class OnDepartedFoyerPatch
    {
        static void Postfix(Foyer __instance)
        {
          needToGenHatRoom = true;
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

    private static void MakeRigidBody(this GameObject g, IntVector2 dimensions, IntVector2 offset)
    {
      g.GenerateOrAddToRigidBody(CollisionLayer.HighObstacle, PixelCollider.PixelColliderGeneration.Manual, UsesPixelsAsUnitSize: true, dimensions: dimensions, offset: offset);
    }

    private static PrototypeDungeonRoom CreateEmptyRoom(int width = 12, int height = 12)
    {
        try
        {
            PrototypeDungeonRoom room = RoomFactory.GetNewPrototypeDungeonRoom(width, height);
            room.usesProceduralLighting = false;
            // AddExit(room, new Vector2(width / 2, height), DungeonData.Direction.NORTH);
            // AddExit(room, new Vector2(width / 2, 0), DungeonData.Direction.SOUTH);
            // AddExit(room, new Vector2(width, height / 2), DungeonData.Direction.EAST);
            // AddExit(room, new Vector2(0, height / 2), DungeonData.Direction.WEST);

            // room.overrideRoomVisualType = 0; // stone flooring
            room.overrideRoomVisualType = 1; // wood flooring
            // room.overrideRoomVisualType = 2; // brick flooring
            room.FullCellData = new PrototypeDungeonRoomCellData[width * height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    room.FullCellData[x + y * width] = new PrototypeDungeonRoomCellData()
                    {
                        containsManuallyPlacedLight = false,
                        state = /*(x == 0 || x == width - 1 || y == 0 || y == height - 1) ? CellType.WALL :*/ CellType.FLOOR,
                        appearance = new PrototypeDungeonRoomCellAppearance()
                        {
                            OverrideFloorType = CellVisualData.CellFloorType.Carpet,
                        },
                    };
                }
            }

            // room.OnBeforeSerialize();
            // room.OnAfterDeserialize();
            // room.UpdatePrecalculatedData();
            return room;
        }
        catch (Exception)
        {
            return null;
        }
    }

    const int ROOM_SIZE = 24;
    private static void CreateRealHatRoom()
    {
      ETGModConsole.Log($"creating real hat room");
      PrototypeDungeonRoom protoRoom = /*RoomFactory.*/CreateEmptyRoom(ROOM_SIZE, ROOM_SIZE); //TODO: this doesn't work for sizes smaller than 24x24 without graphical glitches...why?
      // PrototypeDungeonRoom protoRoom = /*RoomFactory.*/CreateEmptyRoom(8, 8); //TODO: this doesn't work for sizes smaller than 24x24 without graphical glitches...why?
      // PrototypeDungeonRoom protoRoom = RoomFactory.GetNewPrototypeDungeonRoom(12, 12);
      Dungeon dungeon = GameManager.Instance.Dungeon;
      RoomHandler newRoom = dungeon.AddRuntimeRoom(protoRoom, lightStyle: DungeonData.LightGenerationStyle.STANDARD);
      TK2DInteriorDecorator decorator = new TK2DInteriorDecorator(dungeon.assembler);
      // decorator.HandleRoomDecoration(newRoom, dungeon, dungeon.m_tilemap);  //TODO: adds janky carpet, fix later
      // decorator.UpholsterRoom(newRoom, dungeon, dungeon.m_tilemap);
      Pathfinding.Pathfinder.Instance.InitializeRegion(dungeon.data, newRoom.area.basePosition, newRoom.area.dimensions);
      int i = 0;
      IntVector2 cornerPos = newRoom.Cells[0];
      foreach (IntVector2 cellPos in newRoom.Cells)
      {
        dungeon.assembler.ClearTileIndicesForCell(dungeon, dungeon.m_tilemap, cellPos.x, cellPos.y);
        dungeon.assembler.BuildTileIndicesForCell(dungeon, dungeon.m_tilemap, cellPos.x, cellPos.y);
      }
      foreach (IntVector2 cellPos in newRoom.Cells)
      {
        IntVector2 roomPos = cellPos - cornerPos;
        CellData cellData = dungeon.data.cellData[cellPos.x][cellPos.y];

        cellData.cellVisualData.containsLight = false;
        if (roomPos.x == 0)
          cellData.cellVisualData.lightDirection = DungeonData.Direction.WEST;
        else if (roomPos.x == ROOM_SIZE - 1)
          cellData.cellVisualData.lightDirection = DungeonData.Direction.EAST;
        else if (roomPos.y == 0)
          cellData.cellVisualData.lightDirection = DungeonData.Direction.SOUTH;
        else if (roomPos.y == ROOM_SIZE - 1)
          cellData.cellVisualData.lightDirection = DungeonData.Direction.NORTH;
        else
          cellData.cellVisualData.containsLight = false;

        if (cellData.cellVisualData.containsLight)
        {
          if (roomPos.y == 0 || roomPos.y == ROOM_SIZE - 1)
          {
            cellData.cellVisualData.facewallLightStampData = dungeon.roomMaterialDefinitions[newRoom.RoomVisualSubtype].sidewallLightStamps[0];
            // cellData.cellVisualData.facewallLightStampData = dungeon.roomMaterialDefinitions[newRoom.RoomVisualSubtype].facewallLightStamps[0];
            cellData.cellVisualData.sidewallLightStampData = null;
          }
          else
          {
            cellData.cellVisualData.facewallLightStampData = null;
            cellData.cellVisualData.sidewallLightStampData = dungeon.roomMaterialDefinitions[newRoom.RoomVisualSubtype].sidewallLightStamps[0];
          }
        }
        // TK2DInteriorDecorator.PlaceLightDecorationForCell(dungeon, dungeon.m_tilemap, cellData, cellPos);

        // cellData.hasBeenGenerated = false;
        cellData.HasCachedPhysicsTile = false;
        cellData.CachedPhysicsTile = null;
        // newRoom.UpdateCellVisualData(cellPos.x, cellPos.y);

        ++i;
      }
      ETGModConsole.Log($"processed {i} cells");
      // dungeon.RebuildTilemap(dungeon.m_tilemap);
      Pixelator.Instance.MarkOcclusionDirty();
      Pixelator.Instance.ProcessOcclusionChange(newRoom.GetCenterCell(), 100f, newRoom, true);

      Vector2 newRoomPos = /*newRoom.GetCenterCell()*/newRoom.Epicenter.ToVector2();
      ETGModConsole.Log($"created new room at {newRoomPos}");
      Vector2 basePos = newRoom.area.basePosition.ToVector2();
      ETGModConsole.Log($"new room basePos at {basePos}");
      PlayerController pc = GameManager.Instance.PrimaryPlayer;
      ETGModConsole.Log($"player is at {pc.CenterPosition}");
      pc.WarpToPointAndBringCoopPartner(newRoomPos /*basePos*/, doFollowers: true);
      pc.ForceChangeRoom(newRoom);
      Vector2 oldPos = GameManager.Instance.MainCameraController.transform.position.XY() - pc.transform.position.XY();
      GameManager.Instance.MainCameraController.SetManualControl(true, false);
      GameManager.Instance.MainCameraController.transform.position =
        (pc.transform.position.XY() + oldPos).ToVector3ZUp(GameManager.Instance.MainCameraController.transform.position.z);
      GameManager.Instance.MainCameraController.SetManualControl(false, false);
    }

    private static void CreatePrefabsIfNeeded()
    {
      if (createdPrefabs)
        return;

      hallwaySegment = ItemAPI.ItemBuilder.AddSpriteToObject("Hallway", $"{BASE_RES_PATH}/hallway-seg.png");
      hallwaySegment.MakeRigidBody(dimensions: new IntVector2(32, 25), offset: new IntVector2(0, 100));
      hallwaySegment.GetComponent<tk2dSprite>().HeightOffGround = -15;

      entrance = ItemAPI.ItemBuilder.AddSpriteToObject("Entrance", $"{BASE_RES_PATH}/Entrance.png");
      entrance.MakeRigidBody(dimensions: new IntVector2(30, 30), offset: new IntVector2(20, 10));
      entrance.GetComponent<tk2dSprite>().HeightOffGround = -15;

      hallwayEnd = ItemAPI.ItemBuilder.AddSpriteToObject("HallwayEnd", $"{BASE_RES_PATH}/hallwayEnd.png");
      hallwayEnd.MakeRigidBody(dimensions: new IntVector2(32, 25), offset: new IntVector2(0, 100));
      hallwayEnd.MakeRigidBody(dimensions: new IntVector2(8, 142), offset: new IntVector2(23, 0));
      hallwayEnd.GetComponent<tk2dSprite>().HeightOffGround = -15;

      plainPedestal = ItemAPI.ItemBuilder.AddSpriteToObject("plainPedestal", $"{BASE_RES_PATH}/pedestal.png");
      plainPedestal.MakeRigidBody(dimensions: new IntVector2(26, 23), offset: new IntVector2(0, 0));
      plainPedestal.GetComponent<tk2dSprite>().HeightOffGround = -3;

      goldPedestal = ItemAPI.ItemBuilder.AddSpriteToObject("goldPedestal", $"{BASE_RES_PATH}/pedestal_gold.png");
      goldPedestal.MakeRigidBody(dimensions: new IntVector2(26, 23), offset: new IntVector2(0, 0));
      goldPedestal.GetComponent<tk2dSprite>().HeightOffGround = -3;

      hallwaySegBottom = ItemAPI.ItemBuilder.AddSpriteToObject("HallwayBot", $"{BASE_RES_PATH}/hallway-seg-bot.png");
      hallwaySegBottom.MakeRigidBody(dimensions: new IntVector2(32, 9), offset: new IntVector2(0, -8));

      createdPrefabs = true;
    }

    public static void Generate()
    {
      if (Hatabase.Hats.Count == 0)
      {
        Debug.Log("No Hats so no hat room!");
        return;
      }

      try
      {
        CreatePrefabsIfNeeded();

        RoomHandler foyerRoom = GameManager.Instance.Dungeon.data.Entrance;
        //NOTE: funky math since segment width and pedestal spacing are not the same
        int numRoomSegments = Mathf.Max(MIN_SEGMENTS, Mathf.CeilToInt((Mathf.Ceil(0.5f * Hatabase.Hats.Count) * PEDESTAL_SPACING) / SEGMENT_WIDTH));

        // Set up hat room entrance and warp points
        GameObject entranceObj = UnityEngine.Object.Instantiate(entrance, new Vector3(59.75f, 36f, 36.875f), Quaternion.identity);
        entranceObj.GetComponent<SpeculativeRigidbody>().OnCollision += WarpToHatRoom;
        GameObject returner = UnityEngine.Object.Instantiate(new GameObject(), new Vector3(HALLWAY_X, HALLWAY_Y, PEDESTAL_Z), Quaternion.identity);
        returner.MakeRigidBody(dimensions: new IntVector2(2, 142), offset: new IntVector2(0, 0));
        returner.GetComponent<SpeculativeRigidbody>().OnCollision += WarpBackFromHatRoom;

        // Place down segments of the hat room hallway
        UnityEngine.Object.Instantiate(hallwaySegment, // first top segment
          new Vector3(HALLWAY_X, HALLWAY_Y, PEDESTAL_Z), Quaternion.identity);
        UnityEngine.Object.Instantiate(hallwaySegBottom, // first bottom segment
          new Vector3(HALLWAY_X, HALLWAY_Y, PEDESTAL_Z), Quaternion.identity);
        for (int i = 0; i < numRoomSegments; i++)
        {
            Vector3 pos = new Vector3(FIRST_SEGMENT_X + (SEGMENT_WIDTH * i), HALLWAY_Y, PEDESTAL_Z);
            UnityEngine.Object.Instantiate(hallwaySegment, pos, Quaternion.identity);
            UnityEngine.Object.Instantiate(hallwaySegBottom, pos, Quaternion.identity);
        }
        UnityEngine.Object.Instantiate(hallwayEnd, // last top segment
          new Vector3(FIRST_SEGMENT_X + (SEGMENT_WIDTH * numRoomSegments), HALLWAY_Y, PEDESTAL_Z), Quaternion.identity);
        UnityEngine.Object.Instantiate(hallwaySegBottom, // last bottom segment
          new Vector3(FIRST_SEGMENT_X + (SEGMENT_WIDTH * numRoomSegments), HALLWAY_Y, PEDESTAL_Z), Quaternion.identity);

        // Create the pedestals with hats on them
        Hat[] allHats = Hatabase.Hats.Values.ToArray();
        for (int i = 0; i < allHats.Length; i++)
        {
          Hat hat = allHats[i];
          int segmentId = i / 2;
          bool onTop = (i % 2) == 0;
          float pedY = onTop ? 85.7f : 81f;

          GameObject pedObj = UnityEngine.Object.Instantiate(
            hat.goldenPedestal ? goldPedestal : plainPedestal,
            new Vector3(FIRST_SEGMENT_X + (PEDESTAL_SPACING * segmentId), pedY, PEDESTAL_Z),
            Quaternion.identity);

          HatPedestal pedestal = pedObj.AddComponent<HatPedestal>();
          pedestal.hat = hat;
          pedestal.lowerPedestal = !onTop;

          GameObject hat1 = new GameObject();
          tk2dSprite sprite = hat1.AddComponent<tk2dSprite>();
          sprite.SetSprite(pedestal.hat.sprite.collection, pedestal.hat.sprite.spriteId);
          sprite.PlaceAtLocalPositionByAnchor(
            new Vector2(pedObj.GetComponent<tk2dSprite>().WorldCenter.x, pedY + 1.3f).ToVector3ZisY(0f),
            tk2dBaseSprite.Anchor.LowerCenter);
          sprite.HeightOffGround = HAT_Z_OFFSET;
          sprite.UpdateZDepth();
          SpriteOutlineManager.AddOutlineToSprite(hat1.GetComponent<tk2dSprite>(), Color.black, HAT_Z_OFFSET);

          foyerRoom.RegisterInteractable(pedestal as IPlayerInteractable);
        }
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
