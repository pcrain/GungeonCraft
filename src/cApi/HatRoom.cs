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

    private static readonly Vector3 ENTRANCE_POSITION = new Vector3(59.75f, 36f, 36.875f);

    private static GameObject entrance              = null;
    private static GameObject hatRoomExit           = null;
    private static GameObject plainPedestal         = null;
    private static GameObject goldPedestal          = null;
    private static bool needToGenHatRoom            = true;
    private static bool createdPrefabs              = false;
    private static List<IntVector2> pedestalOffsets = null;
    private static Vector2 hatRoomCenter            = Vector2.zero;
    private static PrototypeDungeonRoom protoRoom   = null;
    private static RoomHandler runtimeRoom          = null;

    /// <summary>Regenerates the hat room every time the Breach loads</summary>
    [HarmonyPatch(typeof(Foyer), nameof(Foyer.ProcessPlayerEnteredFoyer))]
    private class ProcessPlayerEnteredFoyerPatch
    {
        static void Postfix(Foyer __instance, PlayerController p)
        {
          if (!needToGenHatRoom || Hatabase.Hats.Count == 0)
            return;
          CreatePrefabsIfNeeded();
          CreateHatRoomPrototypeIfNeeded();
          // Set up hat room entrance and warp points
          UnityEngine.Object.Instantiate(entrance, ENTRANCE_POSITION, Quaternion.identity)
            .GetComponent<SpeculativeRigidbody>().OnCollision += WarpToHatRoom;
          runtimeRoom = null;
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

    private static void CreateHatRoomPrototypeIfNeeded()
    {
      if (protoRoom != null)
        return;

      // Math our way to figuring out the room size
      int numHats = Hatabase.Hats.Count;
      GetPedestalRingOffsets(DEBUG_HAT_MULT * numHats, out int maxRing);
      int roomXSize = Mathf.CeilToInt(2 * (maxRing + 1) * NEW_PEDESTAL_X_SPACING);
      int roomYSize = Mathf.CeilToInt(2 * (maxRing + 1) * NEW_PEDESTAL_Y_SPACING);

      protoRoom = CreateEmptyLitHatRoom(roomXSize, roomYSize);
    }

    private static PrototypeDungeonRoom CreateEmptyLitHatRoom(int width, int height)
    {
      PrototypeDungeonRoom room = RoomFactory.GetNewPrototypeDungeonRoom(width, height);
      room.usesProceduralLighting = false;
      room.overrideRoomVisualType = 1; // 0 = stone, 1 = wood, 2 = brick
      room.FullCellData = new PrototypeDungeonRoomCellData[width * height];
      int hradius = width / 2;
      int vradius = height / 2;
      for (int y = 0; y < height; y++)
      {
          for (int x = 0; x < width; x++)
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
      for (int y = vradius; y < vradius + 4; y++)
          for (int x = hradius - 4; x < hradius + 4; x++)
              room.FullCellData[x + y * width].state = CellType.WALL;

      return room;
    }

    private static void MakeRigidBody(this GameObject g, IntVector2 dimensions, IntVector2 offset)
    {
      g.GenerateOrAddToRigidBody(CollisionLayer.HighObstacle, PixelCollider.PixelColliderGeneration.Manual, UsesPixelsAsUnitSize: true, dimensions: dimensions, offset: offset);
    }

    /// <summary>Mostly identical to the base game AddRuntimeRoom() function, with a hack to work around tiles failing to render when adding the room to the Breach</summary>
    private static RoomHandler AddRuntimeHatRoom(Dungeon dungeon, PrototypeDungeonRoom prototype, Action<RoomHandler> postProcessCellData = null, DungeonData.LightGenerationStyle lightStyle = DungeonData.LightGenerationStyle.FORCE_COLOR)
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

    private static void InstantiateHatRoomIfNeeded()
    {
      if (runtimeRoom != null)
        return;

      // Create the hat room itself
      Dungeon dungeon = GameManager.Instance.Dungeon;
      RoomHandler newRoom = AddRuntimeHatRoom(dungeon, protoRoom, lightStyle: DungeonData.LightGenerationStyle.FORCE_COLOR);
      Pixelator.Instance.MarkOcclusionDirty();
      Pixelator.Instance.ProcessOcclusionChange(newRoom.GetCenterCell(), 1f, newRoom, true);

      // Set up the hat room exit
      hatRoomCenter = newRoom.area.Center;
      GameObject returner = UnityEngine.Object.Instantiate(hatRoomExit);
      tk2dSprite returnerSprite = returner.GetComponent<tk2dSprite>();
      returnerSprite.PlaceAtPositionByAnchor(hatRoomCenter + new Vector2(0, -1.5f), tk2dBaseSprite.Anchor.LowerCenter);
      returnerSprite.HeightOffGround = -3f;
      returnerSprite.UpdateZDepth();
      returner.GetComponent<SpeculativeRigidbody>().OnPreRigidbodyCollision += PreWarpBackFromHatRoom;
      returner.GetComponent<SpeculativeRigidbody>().OnCollision += WarpBackFromHatRoom;

      // Set up the hat pedestals
      CreateHatPedestals(newRoom);

      runtimeRoom = newRoom;
    }

    /// <summary>Logic for getting offsets in symmetrical rings around the center of the hat room</summary>
    private static void GetPedestalRingOffsets(int length, out int nextRing)
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

    private static void CreateHatPedestals(RoomHandler room)
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

          if (hat.HasBeenUnlocked || hat.showSilhouetteWhenLocked)
          {
            GameObject pedestalHatObject = new GameObject();
            tk2dSprite sprite = pedestalHatObject.AddComponent<tk2dSprite>();
            sprite.SetSprite(pedestal.hat.sprite.collection, pedestal.hat.sprite.spriteId);
            if (!hat.HasBeenUnlocked)
            {
              sprite.usesOverrideMaterial = true;
              sprite.renderer.material.shader = ShaderCache.Acquire("Brave/Internal/Black");
            }
            sprite.PlaceAtLocalPositionByAnchor(
              new Vector2(pedObj.GetComponent<tk2dSprite>().WorldCenter.x, pedY + 1.3f).ToVector3ZisY(0f),
              tk2dBaseSprite.Anchor.LowerCenter);
            sprite.HeightOffGround = HAT_Z_OFFSET;
            sprite.UpdateZDepth();
            SpriteOutlineManager.AddOutlineToSprite(pedestalHatObject.GetComponent<tk2dSprite>(), Color.black, HAT_Z_OFFSET);
          }
          room.RegisterInteractable(pedestal as IPlayerInteractable);
        }
    }

    private static Vector2 HatRoomWarpPoint() => hatRoomCenter + new Vector2(-GameManager.Instance.PrimaryPlayer.SpriteDimensions.x / 2, -2f);
    private static Vector2 HatRoomReturnPoint() => new Vector2(58.5f, 37.25f);

    private static void WarpToHatRoom(CollisionData obj)
    {
      if (obj.OtherRigidbody.gameObject.GetComponent<PlayerController>() is not PlayerController player)
        return;

      GameManager.Instance.StartCoroutine(WarpToPoint(player, HatRoomWarpPoint, Vector2.down, InstantiateHatRoomIfNeeded));
      Pixelator.Instance.DoOcclusionLayer = false;
    }

    private static void PreWarpBackFromHatRoom(SpeculativeRigidbody me, PixelCollider myPixelCollider, SpeculativeRigidbody other, PixelCollider otherPixelCollider)
    {
      if (other.gameObject.GetComponent<PlayerController>() is not PlayerController player)
        return;
      if (player.usingForcedInput || player.m_activeActions.Move.Vector.y <= 0f)
        PhysicsEngine.SkipCollision = true;
    }

    private static void WarpBackFromHatRoom(CollisionData obj)
    {
      if (obj.OtherRigidbody.gameObject.GetComponent<PlayerController>() is not PlayerController player)
        return;
      if (player.m_activeActions.Move.Vector.y <= 0f)
        return; // only enter if we're facing up

      GameManager.Instance.StartCoroutine(WarpToPoint(player, HatRoomReturnPoint, Vector2.left));
      Pixelator.Instance.DoOcclusionLayer = true;
    }

    private static IEnumerator WarpToPoint(PlayerController p, Func<Vector2> positionFunc, Vector2? forceDirection = null, Action preWarpSetup = null)
    {
      p.usingForcedInput = true;
      Pixelator.Instance.FadeToBlack(0.1f, false);
      yield return new WaitForSeconds(0.15f);
      if (preWarpSetup != null)
        preWarpSetup();
      p.WarpToPointAndBringCoopPartner(positionFunc(), doFollowers: true);
      GameManager.Instance.MainCameraController.ForceToPlayerPosition(p);
      if (forceDirection.HasValue)
        p.ForceIdleFacePoint(forceDirection.Value);
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
      return 1.5f;
    }

    public float GetDistanceToPoint(Vector2 point)
    {
      return Vector2.Distance(point, gameObject.GetComponent<tk2dSprite>().WorldCenter);
    }

    public void Interact(PlayerController interactor)
    {
      if (hat.HasBeenUnlocked)
        interactor.GetComponent<HatController>().SetHat(hat);
      else
        AkSoundEngine.PostEvent("Play_OBJ_purchase_unable_01", base.gameObject);
    }

    public void OnEnteredRange(PlayerController interactor)
    {
      SpriteOutlineManager.RemoveOutlineFromSprite(base.sprite, true);
      SpriteOutlineManager.AddOutlineToSprite(base.sprite, Color.white);
      TextBoxManager.ShowInfoBox(new Vector2(transform.position.x + 0.75f ,transform.position.y + 2), transform, 3600f, hat.HasBeenUnlocked ? hat.hatName : hat.UnlockText); // 1 hour duration so it persists
    }

    public void OnExitRange(PlayerController interactor)
    {
      SpriteOutlineManager.RemoveOutlineFromSprite(base.sprite, true);
      SpriteOutlineManager.AddOutlineToSprite(base.sprite, Color.black);
      TextBoxManager.ShowInfoBox(new Vector2(transform.position.x + 0.75f ,transform.position.y + 2), transform, 0f, hat.HasBeenUnlocked ? hat.hatName : hat.UnlockText); // 0 duration = disappears instantly
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
