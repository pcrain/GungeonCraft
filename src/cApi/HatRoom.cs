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

    private const float PEDESTAL_Z             = 10.8f;
    private const float HAT_Z_OFFSET           = 1f;
    private const int LIGHT_SPACING            = 8;
    private const int DEBUG_HAT_MULT           = 1; // used for creating 100s of duplicate hats for stress testing hat room, should be 1 on release
    private const float NEW_PEDESTAL_X_SPACING = 3.2f;
    private const float NEW_PEDESTAL_Y_SPACING = 2.5f;

    private static GameObject entrance              = null;
    private static GameObject hatRoomExit           = null;
    private static GameObject plainPedestal         = null;
    private static GameObject goldPedestal          = null;
    private static bool needToGenHatRoom            = true;
    private static bool createdPrefabs              = false;
    private static List<IntVector2> pedestalOffsets = null;
    private static Vector2 hatRoomCenter            = Vector2.zero;

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

    private static void MakeRigidBody(this GameObject g, IntVector2 dimensions, IntVector2 offset)
    {
      g.GenerateOrAddToRigidBody(CollisionLayer.HighObstacle, PixelCollider.PixelColliderGeneration.Manual, UsesPixelsAsUnitSize: true, dimensions: dimensions, offset: offset);
    }

    public static RoomHandler AddRuntimeHatRoom(Dungeon dungeon, PrototypeDungeonRoom prototype, Action<RoomHandler> postProcessCellData = null, DungeonData.LightGenerationStyle lightStyle = DungeonData.LightGenerationStyle.FORCE_COLOR)
    {
      int wallWidth = 3;
      int borderSize = wallWidth * 2;
      IntVector2 roomDimensions = new IntVector2(prototype.Width, prototype.Height);
      IntVector2 newRoomOffset = new IntVector2(dungeon.data.Width + borderSize, borderSize);
      int newWidth = dungeon.data.Width + borderSize * 2 + roomDimensions.x;
      int newHeight = Mathf.Max(dungeon.data.Height, roomDimensions.y + borderSize * 2);
      CellData[][] array = BraveUtility.MultidimensionalArrayResize(dungeon.data.cellData, dungeon.data.Width, dungeon.data.Height, newWidth, newHeight);
      CellArea cellArea = new CellArea(newRoomOffset, roomDimensions);
      cellArea.prototypeRoom = prototype;
      dungeon.data.cellData = array;
      dungeon.data.ClearCachedCellData();
      RoomHandler roomHandler = new RoomHandler(cellArea);
      for (int i = -borderSize; i < roomDimensions.x + borderSize; i++)
      {
        for (int j = -borderSize; j < roomDimensions.y + borderSize; j++)
        {
          IntVector2 p = new IntVector2(i, j) + newRoomOffset;
          CellData cellData = new CellData(p);
          cellData.positionInTilemap = cellData.positionInTilemap - newRoomOffset + new IntVector2(wallWidth, wallWidth);
          cellData.parentArea = cellArea;
          cellData.parentRoom = roomHandler;
          cellData.nearestRoom = roomHandler;
          cellData.distanceFromNearestRoom = 0f;
          array[p.x][p.y] = cellData;
        }
      }
      roomHandler.WriteRoomData(dungeon.data);
      for (int k = -borderSize; k < roomDimensions.x + borderSize; k++)
      {
        for (int l = -borderSize; l < roomDimensions.y + borderSize; l++)
        {
          IntVector2 intVector2 = new IntVector2(k, l) + newRoomOffset;
          array[intVector2.x][intVector2.y].breakable = true;
        }
      }
      dungeon.data.rooms.Add(roomHandler);
      GameObject gameObject = (GameObject)UnityEngine.Object.Instantiate(BraveResources.Load("RuntimeTileMap"));
      tk2dTileMap component = gameObject.GetComponent<tk2dTileMap>();
      component.Editor__SpriteCollection = dungeon.tileIndices.dungeonCollection;
      GameManager.Instance.Dungeon.data.GenerateLightsForRoom(GameManager.Instance.Dungeon.decoSettings, roomHandler, GameObject.Find("_Lights").transform, lightStyle);
      if (postProcessCellData != null)
      {
        postProcessCellData(roomHandler);
      }
      const int HACKY_SCALING_FACTOR = 32; //HACK: making the tilemap extra wide is the only way I've found to avoid rendering issues with black patches in rooms
      TK2DDungeonAssembler.RuntimeResizeTileMap(component, HACKY_SCALING_FACTOR + roomDimensions.x + wallWidth * 2, roomDimensions.y + wallWidth * 2, dungeon.m_tilemap.partitionSizeX, dungeon.m_tilemap.partitionSizeY);
      for (int m = -wallWidth; m < roomDimensions.x + wallWidth; m++)
      {
        for (int n = -wallWidth; n < roomDimensions.y + wallWidth; n++)
        {
          dungeon.assembler.BuildTileIndicesForCell(dungeon, component, newRoomOffset.x + m, newRoomOffset.y + n);
        }
      }
      tk2dRuntime.TileMap.RenderMeshBuilder.CurrentCellXOffset = newRoomOffset.x - wallWidth;
      tk2dRuntime.TileMap.RenderMeshBuilder.CurrentCellYOffset = newRoomOffset.y - wallWidth;
      component.Build(tk2dTileMap.BuildFlags.ForceBuild);
      tk2dRuntime.TileMap.RenderMeshBuilder.CurrentCellXOffset = 0;
      tk2dRuntime.TileMap.RenderMeshBuilder.CurrentCellYOffset = 0;
      component.renderData.transform.position = new Vector3(newRoomOffset.x - wallWidth, newRoomOffset.y - wallWidth, newRoomOffset.y - wallWidth);
      roomHandler.OverrideTilemap = component;
      Pathfinding.Pathfinder.Instance.InitializeRegion(dungeon.data, roomHandler.area.basePosition + new IntVector2(-wallWidth, -wallWidth), roomHandler.area.dimensions + new IntVector2(wallWidth, wallWidth));
      roomHandler.PostGenerationCleanup();
      DeadlyDeadlyGoopManager.ReinitializeData();
      return roomHandler;
    }

    private static PrototypeDungeonRoom CreateEmptyLitRoom(int width, int height)
    {
      PrototypeDungeonRoom room = RoomFactory.GetNewPrototypeDungeonRoom(width, height);
      room.usesProceduralLighting = false;
      room.overrideRoomVisualType = 1; // 0 = stone, 1 = wood, 2 = brick
      room.FullCellData = new PrototypeDungeonRoomCellData[width * height];
      int hradius = width / 2;
      int vradius = height / 2;
      for (int x = 0; x < width; x++)
      {
          for (int y = 0; y < height; y++)
          {
              // fancy math to space out lights evenly without too much overlap or dark spots
              bool shouldBeLit = ((hradius - Math.Min(x, width  - (x + 1))) % LIGHT_SPACING == (LIGHT_SPACING / 2)) &&
                                 ((vradius - Math.Min(y, height - (y + 1))) % LIGHT_SPACING == (LIGHT_SPACING / 2));
              room.FullCellData[x + y * width] = new PrototypeDungeonRoomCellData()
              {
                  containsManuallyPlacedLight = shouldBeLit,
                  state = CellType.FLOOR,
                  appearance = new PrototypeDungeonRoomCellAppearance(),
              };
          }
      }

      // Add walls in the middle for the stairs
      for (int x = hradius - 4; x < hradius + 4; x++)
          for (int y = vradius; y < vradius + 4; y++)
            room.FullCellData[x + y * width].state = CellType.WALL;

      return room;
    }

    private static void CreateRealHatRoom()
    {
      int numHats = Hatabase.Hats.Count;
      if (numHats == 0)
      {
        Debug.Log("No Hats so no hat room!");
        return;
      }

      // Math our way to figuring out the room size
      GetPedestalRingOffsets(DEBUG_HAT_MULT * numHats, out int maxRing);
      int roomXSize = Mathf.CeilToInt(2 * (maxRing + 1) * NEW_PEDESTAL_X_SPACING);
      int roomYSize = Mathf.CeilToInt(2 * (maxRing + 1) * NEW_PEDESTAL_Y_SPACING);

      PrototypeDungeonRoom protoRoom = CreateEmptyLitRoom(roomXSize, roomYSize); //TODO: this doesn't work for sizes smaller than 24x24 without graphical glitches...why?
      Dungeon dungeon = GameManager.Instance.Dungeon;
      RoomHandler newRoom = AddRuntimeHatRoom(dungeon, protoRoom, lightStyle: DungeonData.LightGenerationStyle.FORCE_COLOR);
      TK2DInteriorDecorator decorator = new TK2DInteriorDecorator(dungeon.assembler);
      // decorator.HandleRoomDecoration(newRoom, dungeon, dungeon.m_tilemap);  //TODO: adds janky carpet, fix later
      Pathfinding.Pathfinder.Instance.InitializeRegion(dungeon.data, newRoom.area.basePosition, newRoom.area.dimensions);

      // foreach (IntVector2 cellPos in newRoom.Cells)
      // {
      //   dungeon.assembler.ClearTileIndicesForCell(dungeon, dungeon.m_tilemap, cellPos.x, cellPos.y);
      //   dungeon.assembler.BuildTileIndicesForCell(dungeon, dungeon.m_tilemap, cellPos.x, cellPos.y);
      // }
      // foreach (IntVector2 cellPos in newRoom.Cells)
      // {
      //   IntVector2 roomPos = cellPos - newRoom.Cells[0];
      //   CellData cellData = dungeon.data.cellData[cellPos.x][cellPos.y];

      //   cellData.cellVisualData.containsLight = false;
      //   if (roomPos.x == 0)
      //     cellData.cellVisualData.lightDirection = DungeonData.Direction.WEST;
      //   else if (roomPos.x == ROOM_SIZE - 1)
      //     cellData.cellVisualData.lightDirection = DungeonData.Direction.EAST;
      //   else if (roomPos.y == 0)
      //     cellData.cellVisualData.lightDirection = DungeonData.Direction.SOUTH;
      //   else if (roomPos.y == ROOM_SIZE - 1)
      //     cellData.cellVisualData.lightDirection = DungeonData.Direction.NORTH;
      //   else
      //     cellData.cellVisualData.containsLight = false;

      //   if (cellData.cellVisualData.containsLight)
      //   {
      //     if (roomPos.y == 0 || roomPos.y == ROOM_SIZE - 1)
      //     {
      //       cellData.cellVisualData.facewallLightStampData = dungeon.roomMaterialDefinitions[newRoom.RoomVisualSubtype].sidewallLightStamps[0];
      //       // cellData.cellVisualData.facewallLightStampData = dungeon.roomMaterialDefinitions[newRoom.RoomVisualSubtype].facewallLightStamps[0];
      //       cellData.cellVisualData.sidewallLightStampData = null;
      //     }
      //     else
      //     {
      //       cellData.cellVisualData.facewallLightStampData = null;
      //       cellData.cellVisualData.sidewallLightStampData = dungeon.roomMaterialDefinitions[newRoom.RoomVisualSubtype].sidewallLightStamps[0];
      //     }
      //   }
      //   // TK2DInteriorDecorator.PlaceLightDecorationForCell(dungeon, dungeon.m_tilemap, cellData, cellPos);

      //   cellData.HasCachedPhysicsTile = false;
      //   cellData.CachedPhysicsTile = null;
      //   // newRoom.UpdateCellVisualData(cellPos.x, cellPos.y);
      // }
      // dungeon.RebuildTilemap(dungeon.m_tilemap);
      Pixelator.Instance.MarkOcclusionDirty();
      Pixelator.Instance.ProcessOcclusionChange(newRoom.GetCenterCell(), 1f, newRoom, true);

      CreatePrefabsIfNeeded();
      CreateNewHatPedestals(newRoom);

      // Set up hat room entrance and warp points
      hatRoomCenter = newRoom.area.Center;
      GameObject entranceObj = UnityEngine.Object.Instantiate(entrance, new Vector3(59.75f, 36f, 36.875f), Quaternion.identity);
      entranceObj.GetComponent<SpeculativeRigidbody>().OnCollision += WarpToHatRoom;
      GameObject returner = UnityEngine.Object.Instantiate(hatRoomExit);
      tk2dSprite returnerSprite = returner.GetComponent<tk2dSprite>();
      returnerSprite.PlaceAtPositionByAnchor(hatRoomCenter + new Vector2(0, -1.5f), tk2dBaseSprite.Anchor.LowerCenter);
      returnerSprite.HeightOffGround = -3f;
      returnerSprite.UpdateZDepth();
      returner.GetComponent<SpeculativeRigidbody>().OnCollision += WarpBackFromHatRoom;
    }

    private static void CreatePrefabsIfNeeded()
    {
      if (createdPrefabs)
        return;

      entrance = ItemAPI.ItemBuilder.AddSpriteToObject("Entrance", $"{BASE_RES_PATH}/Entrance.png");
      entrance.MakeRigidBody(dimensions: new IntVector2(30, 30), offset: new IntVector2(20, 10));
      entrance.GetComponent<tk2dSprite>().HeightOffGround = -15;

      plainPedestal = ItemAPI.ItemBuilder.AddSpriteToObject("plainPedestal", $"{BASE_RES_PATH}/pedestal.png");
      plainPedestal.MakeRigidBody(dimensions: new IntVector2(26, 23), offset: new IntVector2(0, 0));
      plainPedestal.GetComponent<tk2dSprite>().HeightOffGround = -3;

      goldPedestal = ItemAPI.ItemBuilder.AddSpriteToObject("goldPedestal", $"{BASE_RES_PATH}/pedestal_gold.png");
      goldPedestal.MakeRigidBody(dimensions: new IntVector2(26, 23), offset: new IntVector2(0, 0));
      goldPedestal.GetComponent<tk2dSprite>().HeightOffGround = -3;

      hatRoomExit = ItemAPI.ItemBuilder.AddSpriteToObject("HatRoomExit", $"{BASE_RES_PATH}/hat_room_exit.png");
      hatRoomExit.MakeRigidBody(dimensions: new IntVector2(22, 16), offset: new IntVector2(12, 8));

      createdPrefabs = true;
    }

    /// <summary>Logic for getting offsets in symmetrical rings around the center of the hat room</summary>
    public static void GetPedestalRingOffsets(int length, out int nextRing)
    {
      pedestalOffsets = new(length);
      int remaining = length;
      nextRing = 1;
      while (remaining > 0)
      {
        nextRing += 1;
        int maxRingSize = nextRing * 8;
        int ringSize = Math.Min(remaining, maxRingSize);
        if ((remaining % 2) == 1 || ringSize == maxRingSize)
          pedestalOffsets.Add(new IntVector2(0, nextRing));
        int halfRing = ringSize / 2;
        int x = 0;
        int y = nextRing;
        for (int i = 1; i <= halfRing; ++i)
        {
          if (i == (maxRingSize / 2))
          {
            pedestalOffsets.Add(new IntVector2(0, -nextRing));
            break;
          }
          if (y == -nextRing)
            --x;
          else if (x < nextRing)
            ++x;
          else
            --y;
          pedestalOffsets.Add(new IntVector2(x, y));
          pedestalOffsets.Add(new IntVector2(-x, y));
        }
        remaining -= ringSize;
      }
    }

    public static void CreateNewHatPedestals(RoomHandler room)
    {
        Vector2 roomCenter = room.area.Center;
        Hat[] allHats = Hatabase.Hats.Values.ToArray();
        for (int i = 0; i < DEBUG_HAT_MULT * allHats.Length; i++)
        {
          Hat hat = allHats[i % allHats.Length];

          float pedX = roomCenter.x + pedestalOffsets[i].x * NEW_PEDESTAL_X_SPACING;
          float pedY = roomCenter.y + pedestalOffsets[i].y * NEW_PEDESTAL_Y_SPACING;

          GameObject pedObj = UnityEngine.Object.Instantiate(hat.goldenPedestal ? goldPedestal : plainPedestal);
          pedObj.GetComponent<tk2dSprite>().PlaceAtPositionByAnchor(new Vector3(pedX, pedY, PEDESTAL_Z), tk2dBaseSprite.Anchor.LowerCenter);

          HatPedestal pedestal = pedObj.AddComponent<HatPedestal>();
          pedestal.hat = hat;

          GameObject pedestalHatObject = new GameObject();
          tk2dSprite sprite = pedestalHatObject.AddComponent<tk2dSprite>();
          sprite.SetSprite(pedestal.hat.sprite.collection, pedestal.hat.sprite.spriteId);
          sprite.PlaceAtLocalPositionByAnchor(
            new Vector2(pedObj.GetComponent<tk2dSprite>().WorldCenter.x, pedY + 1.3f).ToVector3ZisY(0f),
            tk2dBaseSprite.Anchor.LowerCenter);
          sprite.HeightOffGround = HAT_Z_OFFSET;
          sprite.UpdateZDepth();
          SpriteOutlineManager.AddOutlineToSprite(pedestalHatObject.GetComponent<tk2dSprite>(), Color.black, HAT_Z_OFFSET);

          room.RegisterInteractable(pedestal as IPlayerInteractable);
        }
    }

    private static void WarpToHatRoom(CollisionData obj)
    {
      if (obj.OtherRigidbody.gameObject.GetComponent<PlayerController>() is not PlayerController player)
        return;

      GameManager.Instance.StartCoroutine(WarpToPoint(player, hatRoomCenter + new Vector2(-player.SpriteDimensions.x / 2, -2.5f)));
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

    public string GetAnimationState(PlayerController interactor, out bool shouldBeFlipped)
    {
      shouldBeFlipped = false; //Some boilerplate code for determining if the interactable should be flipped
      return string.Empty;
    }

    public float GetOverrideMaxDistance()
    {
      return 1f;
    }

    public float GetDistanceToPoint(Vector2 point)
    {
      return Vector2.Distance(point, gameObject.GetComponent<tk2dSprite>().WorldCenter);
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
