namespace CwaffingTheGungy;


/* NOTES:
    - the OncePerFloor and OnlyOne variables for injection data don't seem to work properly / as expected, so we don't use them
    -
*/

public class FancyShopData
{
  public const int DEFAULT_SHOP_FLOORS = 127;

  public GameObject shop;
  public TalkDoerLite owner;
  public PrototypeDungeonRoom room;
  public GenericLootTable loot;
}

public static class FancyShopBuilder
{
  public delegate bool SpawnCondition(SpawnConditions conds);

  public static Dictionary<string, PrototypeDungeonRoom> FancyShopRooms = new();
  public static Dictionary<GameObject, List<string>> DelayedModdedLootAdditions = new();

  // public static Vector3[] defaultItemPositions = new Vector3[] {
  //   new Vector3(1.125f, 2.125f, 1),
  //   new Vector3(2.625f, 1f, 1),
  //   new Vector3(4.125f, 2.125f, 1) };

  private static List<string> _DefaultLine = new(){"Buy somethin', will ya!"};

  public static FancyShopData MakeFancyShop(string npcName, List<int> shopItems, string roomPath, List<string> moddedItems = null,
    float spawnChance = 1f, Vector2? carpetOffset = null, int? idleFps = null, int? talkFps = null,
    CwaffPrerequisites spawnPrerequisite = CwaffPrerequisites.NONE, SpawnCondition prequisiteValidator = null, string voice = null,
    List<String> genericDialog = null, List<String> stopperDialog = null, List<String> purchaseDialog = null, List<String> stolenDialog = null,
    List<String> noSaleDialog = null, List<String> introDialog = null, List<String> attackedDialog = null, bool allowDupes = false,
    float mainPoolChance = 0.0f, Vector3? talkPointOffset = null, Vector3? npcPosition = null, List<Vector3> itemPositions = null,
    bool exactlyOncePerRun = true, int allowedTilesets = 127, float costModifier = 1f, bool canBeRobbed = true, bool flipTowardsPlayer = true,
    Func<CustomShopController, PlayerController, int, bool> customCanBuy = null,
    Func<CustomShopController, PlayerController, int, int> removeCurrency = null,
    Func<CustomShopController, CustomShopItemController, PickupObject, int> customPrice = null,
    Func<PlayerController, PickupObject, int, bool> onPurchase = null,
    Func<PlayerController, PickupObject, int, bool> onSteal = null
    )
  {
    if (C.DEBUG_BUILD)
      ETGModConsole.Log($"Setting up shop for {npcName}");

    moddedItems ??= new();
    string npcNameUpper = npcName.ToUpper();

    $"#{npcNameUpper}_GENERIC_TALK".SetupDBStrings(genericDialog ?? _DefaultLine);
    $"#{npcNameUpper}_STOPPER_TALK".SetupDBStrings(stopperDialog ?? _DefaultLine);
    $"#{npcNameUpper}_PURCHASE_TALK".SetupDBStrings(purchaseDialog ?? _DefaultLine);
    $"#{npcNameUpper}_NOSALE_TALK".SetupDBStrings(noSaleDialog ?? _DefaultLine);
    $"#{npcNameUpper}_INTRO_TALK".SetupDBStrings(introDialog ?? _DefaultLine);
    $"#{npcNameUpper}_ATTACKED_TALK".SetupDBStrings(attackedDialog ?? _DefaultLine);
    $"#{npcNameUpper}_STOLEN_TALK".SetupDBStrings(stolenDialog ?? _DefaultLine);

    List<DungeonPrerequisite> dungeonPrerequisites = new(){
      new CwaffPrerequisite { prerequisite = spawnPrerequisite.SetupPrerequisite(prequisiteValidator) }
    };

    string currencyIcon = ResMap.Get($"{npcName}_currency", quietFailure: true)?[0];
    if (!string.IsNullOrEmpty(currencyIcon))
      currencyIcon += ".png";

    GenericLootTable lootTable = shopItems.ToLootTable();
    GameObject shop = BetterSetUpShop(
      name                              : npcName,
      prefix                            : C.MOD_PREFIX,
      idleSpritePaths                   : ResMap.Get($"{npcName}_idle"),
      idleFps                           : idleFps ?? 2,
      talkSpritePaths                   : ResMap.Get($"{npcName}_talk"),
      talkFps                           : talkFps ?? 5,
      lootTable                         : lootTable,
      currency                          : (removeCurrency == null) ? CustomShopItemController.ShopCurrencyType.COINS : CustomShopItemController.ShopCurrencyType.CUSTOM,
      runBasedMultilineGenericStringKey : $"#{npcNameUpper}_GENERIC_TALK",
      runBasedMultilineStopperStringKey : $"#{npcNameUpper}_STOPPER_TALK",
      purchaseItemStringKey             : $"#{npcNameUpper}_PURCHASE_TALK",
      purchaseItemFailedStringKey       : $"#{npcNameUpper}_NOSALE_TALK",
      introStringKey                    : $"#{npcNameUpper}_INTRO_TALK",
      attackedStringKey                 : $"#{npcNameUpper}_ATTACKED_TALK",
      stolenFromStringKey               : $"#{npcNameUpper}_STOLEN_TALK",
      talkPointOffset                   : talkPointOffset ?? ShopAPI.defaultTalkPointOffset,
      npcPosition                       : npcPosition ?? ShopAPI.defaultNpcPosition,
      voiceBox                          : ShopAPI.VoiceBoxes.OLD_MAN,
      itemPositions                     : itemPositions?.ToArray() ?? ShopAPI.defaultItemPositions,
      costModifier                      : costModifier,
      giveStatsOnPurchase               : false,
      statsToGiveOnPurchase             : null,
      CustomCanBuy                      : customCanBuy,
      CustomRemoveCurrency              : removeCurrency,
      CustomPrice                       : customPrice,
      OnPurchase                        : onPurchase,
      OnSteal                           : onSteal,
      currencyIconPath                  : currencyIcon,
      currencyName                      : "",
      // currencyName                      : "ui_coin",
      canBeRobbed                       : canBeRobbed,
      hasCarpet                         : ResMap.Get($"{npcName}_carpet", quietFailure: true)?[0] != null,
      carpetSpritePath                  : ResMap.Get($"{npcName}_carpet", quietFailure: true)?[0],
      CarpetOffset                      : carpetOffset,
      hasMinimapIcon                    : ResMap.Get($"{npcName}_icon", quietFailure: true)?[0] != null,
      minimapIconSpritePath             : ResMap.Get($"{npcName}_icon", quietFailure: true)?[0],
      addToMainNpcPool                  : mainPoolChance > 0,
      percentChanceForMainPool          : mainPoolChance,
      prerequisites                     : dungeonPrerequisites.ToArray(), // used by RegisterShopRoom(), but we're manually calling AddInjection() for now
      fortunesFavorRadius               : 2,
      poolType                          : allowDupes ? CustomShopController.ShopItemPoolType.DUPES : CustomShopController.ShopItemPoolType.DEFAULT,
      RainbowModeImmunity               : false,
      // hitboxSize                        : null, // must be null, doesn't work properly
      hitboxSize                        : new IntVector2(13, 19),            // must be null, doesn't work properly
      hitboxOffset                      : new IntVector2(1, -3)            // must be null, doesn't work properly
      );

    // Track how many times this shop room has been spawned in a run for prerequisite tracking purposes
    shop.AddComponent<CwaffPrerequisite.Tracker>().Setup(spawnPrerequisite);

    TalkDoerLite npc = shop.GetComponentInChildren<TalkDoerLite>();
    if (flipTowardsPlayer)
      npc.gameObject.AddComponent<FlipsToFacePlayer>();
    if (!string.IsNullOrEmpty(voice))
      npc.audioCharacterSpeechTag = voice;

    // PrototypeDungeonRoom shopRoom = RoomFactory.BuildNewRoomFromResource(roomPath: roomPath).room;
    PrototypeDungeonRoom shopRoom = BuildNewRoomFromResourceWithoutRegistering(roomPath).room;  // prevents the game from spawning the rooms and disregarding prerequisites

    // ShopAPI.RegisterShopRoom(shop: shop, protoroom: shopRoom, vector: new Vector2((float)(shopRoom.Width / 2), (float)(shopRoom.Height / 2)));
    // RoomFactory.AddInjection(
    InjectRoomIntoUniquePool(
      protoroom            : shopRoom,
      injectionAnnotation  : $"{npcName}'s Shop Room",
      placementRules       : new() { ProceduralFlowModifierData.FlowModifierPlacementType.END_OF_CHAIN },
      chanceToLock         : 0,
      prerequisites        : dungeonPrerequisites,
      injectorName         : $"{npcName}'s Shop Room",
      selectionWeight      : 1,
      chanceToSpawn        : spawnChance,
      addSingularPlaceable : shop,
      XFromCenter          : 0,
      YFromCenter          : 0,
      oncePerRun           : exactlyOncePerRun,
      allowedTilesets      : allowedTilesets
      );

    // Defer initialization of modded items
    DelayedModdedLootAdditions[shop] = moddedItems;

    // Return some useful FancyShopData
    FancyShopRooms[npcName] = shopRoom;
    return new FancyShopData(){
      shop  = shop,
      room  = shopRoom,
      owner = npc,
      loot  = lootTable
    };
  }

  /// <summary>Cleaned up version of Alexandria SetupShop method that uses our internal sprites. See Alexandria API for documentation.</summary>
  public static GameObject BetterSetUpShop(string name, string prefix, List<string> idleSpritePaths, int idleFps, List<string> talkSpritePaths, int talkFps, GenericLootTable lootTable, CustomShopItemController.ShopCurrencyType currency, string runBasedMultilineGenericStringKey,
      string runBasedMultilineStopperStringKey, string purchaseItemStringKey, string purchaseItemFailedStringKey, string introStringKey, string attackedStringKey, string stolenFromStringKey, Vector3 talkPointOffset, Vector3 npcPosition, ShopAPI.VoiceBoxes voiceBox = ShopAPI.VoiceBoxes.OLD_MAN, Vector3[] itemPositions = null, float costModifier = 1, bool giveStatsOnPurchase = false,
      StatModifier[] statsToGiveOnPurchase = null, Func<CustomShopController, PlayerController, int, bool> CustomCanBuy = null, Func<CustomShopController, PlayerController, int, int> CustomRemoveCurrency = null, Func<CustomShopController, CustomShopItemController, PickupObject, int> CustomPrice = null,
      Func<PlayerController, PickupObject, int, bool> OnPurchase = null, Func<PlayerController, PickupObject, int, bool> OnSteal = null, string currencyIconPath = "", string currencyName = "", bool canBeRobbed = true, bool hasCarpet = false, string carpetSpritePath = "",
      Vector2? CarpetOffset = null, bool hasMinimapIcon = false, string minimapIconSpritePath = "", bool addToMainNpcPool = false, float percentChanceForMainPool = 0.1f, DungeonPrerequisite[] prerequisites = null, float fortunesFavorRadius = 2,
      CustomShopController.ShopItemPoolType poolType = CustomShopController.ShopItemPoolType.DEFAULT, bool RainbowModeImmunity = false, IntVector2? hitboxSize = null, IntVector2? hitboxOffset = null)
  {

      try
      {
          prerequisites ??= new DungeonPrerequisite[0];

          var shared_auto_001 = ResourceManager.LoadAssetBundle("shared_auto_001");
          var shared_auto_002 = ResourceManager.LoadAssetBundle("shared_auto_002");
          var SpeechPoint = new GameObject("SpeechPoint").RegisterPrefab(activate: true);
          SpeechPoint.transform.position = talkPointOffset;

          var npcObj = new GameObject(prefix + ":" + name).RegisterPrefab();
          npcObj.AddComponent<tk2dSprite>().SetSprite(VFX.Collection, VFX.Collection.GetSpriteIdByName(idleSpritePaths[0]));
          npcObj.layer = 22;

          var collection = npcObj.GetComponent<tk2dSprite>().Collection;
          SpeechPoint.transform.parent = npcObj.transform;

          var idleIdsList = AtlasHelper.AddSpritesToCollection(idleSpritePaths, collection).AsRange();
          var talkIdsList = AtlasHelper.AddSpritesToCollection(talkSpritePaths, collection).AsRange();

          tk2dSpriteAnimator spriteAnimator = npcObj.AddComponent<tk2dSpriteAnimator>();

          SpriteBuilder.AddAnimation(spriteAnimator, collection, idleIdsList, "idle", tk2dSpriteAnimationClip.WrapMode.Loop, idleFps);
          SpriteBuilder.AddAnimation(spriteAnimator, collection, talkIdsList, "talk", tk2dSpriteAnimationClip.WrapMode.Loop, talkFps);

          hitboxSize ??= new IntVector2(20, 18);
          if (hitboxOffset == null) new IntVector2(5, 0); //NOTE: this doesn't do anything, but it's broken in the API as well

          SpeculativeRigidbody rigidbody = ShopAPI.GenerateOrAddToRigidBody(npcObj, CollisionLayer.LowObstacle, PixelCollider.PixelColliderGeneration.Manual, true, true, true, false, false, false, false, true, hitboxSize, hitboxOffset);
          rigidbody.AddCollisionLayerOverride(CollisionMask.LayerToMask(CollisionLayer.BulletBlocker));

          TalkDoerLite talkDoer = npcObj.AddComponent<TalkDoerLite>();
            talkDoer.placeableWidth                = 4;
            talkDoer.placeableHeight               = 3;
            talkDoer.difficulty                    = 0;
            talkDoer.isPassable                    = true;
            talkDoer.usesOverrideInteractionRegion = false;
            talkDoer.overrideRegionOffset          = Vector2.zero;
            talkDoer.overrideRegionDimensions      = Vector2.zero;
            talkDoer.overrideInteractionRadius     = -1;
            talkDoer.PreventInteraction            = false;
            talkDoer.AllowPlayerToPassEventually   = true;
            talkDoer.speakPoint                    = SpeechPoint.transform;
            talkDoer.SpeaksGleepGlorpenese         = false;
            talkDoer.audioCharacterSpeechTag       = ShopAPI.ReturnVoiceBox(voiceBox);
            talkDoer.playerApproachRadius          = 5;
            talkDoer.conversationBreakRadius       = 5;
            talkDoer.echo1                         = null;
            talkDoer.echo2                         = null;
            talkDoer.PreventCoopInteraction        = false;
            talkDoer.IsPaletteSwapped              = false;
            talkDoer.PaletteTexture                = null;
            talkDoer.OutlineDepth                  = 0.5f;
            talkDoer.OutlineLuminanceCutoff        = 0.05f;
            talkDoer.MovementSpeed                 = 3;
            talkDoer.PathableTiles                 = CellTypes.FLOOR;

          UltraFortunesFavor dreamLuck = npcObj.AddComponent<UltraFortunesFavor>();
            dreamLuck.goopRadius          = fortunesFavorRadius;
            dreamLuck.beamRadius          = fortunesFavorRadius;
            dreamLuck.bulletRadius        = fortunesFavorRadius;
            dreamLuck.bulletSpeedModifier = 0.8f;
            dreamLuck.vfxOffset           = 0.625f;
            dreamLuck.sparkOctantVFX      = shared_auto_001.LoadAsset<GameObject>("FortuneFavor_VFX_Spark");

          AIAnimator aIAnimator = ShopAPI.GenerateBlankAIAnimator(npcObj);
          aIAnimator.spriteAnimator = spriteAnimator;
          aIAnimator.IdleAnimation = new DirectionalAnimation
          {
              Type = DirectionalAnimation.DirectionType.Single,
              Prefix = "idle",
              AnimNames = new string[]
              {
                  ""
              },
              Flipped = new DirectionalAnimation.FlipType[]
              {
                  DirectionalAnimation.FlipType.None
              }

          };

          aIAnimator.TalkAnimation = new DirectionalAnimation
          {
              Type = DirectionalAnimation.DirectionType.Single,
              Prefix = "talk",
              AnimNames = new string[]
              {
                  ""
              },
              Flipped = new DirectionalAnimation.FlipType[]
              {
                  DirectionalAnimation.FlipType.None
              }
          };

          var basenpc = ResourceManager.LoadAssetBundle("shared_auto_001").LoadAsset<GameObject>("Merchant_Key").transform.Find("NPC_Key").gameObject;

          PlayMakerFSM finiteStateMachine = npcObj.AddComponent<PlayMakerFSM>();

          UnityEngine.JsonUtility.FromJsonOverwrite(UnityEngine.JsonUtility.ToJson(basenpc.GetComponent<PlayMakerFSM>()), finiteStateMachine);

          FieldInfo fsmStringParams = typeof(ActionData).GetField("fsmStringParams", BindingFlags.NonPublic | BindingFlags.Instance);

          (fsmStringParams.GetValue(finiteStateMachine.FsmStates[1].ActionData) as List<FsmString>)[0].Value = runBasedMultilineGenericStringKey;
          (fsmStringParams.GetValue(finiteStateMachine.FsmStates[1].ActionData) as List<FsmString>)[1].Value = runBasedMultilineStopperStringKey;
          (fsmStringParams.GetValue(finiteStateMachine.FsmStates[4].ActionData) as List<FsmString>)[0].Value = purchaseItemStringKey;
          (fsmStringParams.GetValue(finiteStateMachine.FsmStates[5].ActionData) as List<FsmString>)[0].Value = purchaseItemFailedStringKey;
          (fsmStringParams.GetValue(finiteStateMachine.FsmStates[7].ActionData) as List<FsmString>)[0].Value = introStringKey;
          (fsmStringParams.GetValue(finiteStateMachine.FsmStates[8].ActionData) as List<FsmString>)[0].Value = attackedStringKey;
          (fsmStringParams.GetValue(finiteStateMachine.FsmStates[9].ActionData) as List<FsmString>)[0].Value = stolenFromStringKey;
          (fsmStringParams.GetValue(finiteStateMachine.FsmStates[9].ActionData) as List<FsmString>)[1].Value = stolenFromStringKey;
          (fsmStringParams.GetValue(finiteStateMachine.FsmStates[10].ActionData) as List<FsmString>)[0].Value = "#SHOP_GENERIC_NO_SALE_LABEL";
          (fsmStringParams.GetValue(finiteStateMachine.FsmStates[12].ActionData) as List<FsmString>)[0].Value = "#COOP_REBUKE";

          npcObj.name = prefix + ":" + name;

          var posList = new List<Transform>();
          for (int i = 0; i < itemPositions.Length; i++)
          {
              var ItemPoint = new GameObject("ItemPoint" + i).RegisterPrefab(activate: true);
              ItemPoint.transform.position = itemPositions[i];
              posList.Add(ItemPoint.transform);
          }

          var shopObj = new GameObject(prefix + ":" + name + "_Shop").AddComponent<CustomShopController>();
          shopObj.AllowedToSpawnOnRainbowMode = RainbowModeImmunity;
          shopObj.gameObject.RegisterPrefab();

          shopObj.currencyType = currency;

          shopObj.ActionAndFuncSetUp(CustomCanBuy, CustomRemoveCurrency, CustomPrice, OnPurchase, OnSteal);

          if (currency == CustomShopItemController.ShopCurrencyType.CUSTOM)
          {
              if (!string.IsNullOrEmpty(currencyIconPath))
              {
                  shopObj.customPriceSprite = ShopAPI.AddCustomCurrencyType(currencyIconPath, $"{prefix}:{currencyName}", Assembly.GetCallingAssembly());
              }
              else
              {
                  shopObj.customPriceSprite = currencyName;
              }
          }

          shopObj.canBeRobbed              = canBeRobbed;
          shopObj.placeableHeight          = 5;
          shopObj.placeableWidth           = 5;
          shopObj.difficulty               = 0;
          shopObj.isPassable               = true;
          shopObj.baseShopType             = BaseShopController.AdditionalShopType.TRUCK;//shopType;
          shopObj.FoyerMetaShopForcedTiers = false;
          shopObj.IsBeetleMerchant         = false;
          shopObj.ExampleBlueprintPrefab   = null;
          shopObj.poolType                 = poolType;
          shopObj.shopItems                = lootTable;
          shopObj.spawnPositions           = posList.ToArray();

          foreach (var pos in shopObj.spawnPositions)
              pos.parent = shopObj.gameObject.transform;

          shopObj.shopItemsGroup2          = null;
          shopObj.spawnPositionsGroup2     = null;
          shopObj.spawnGroupTwoItem1Chance = 0.5f;
          shopObj.spawnGroupTwoItem2Chance = 0.5f;
          shopObj.spawnGroupTwoItem3Chance = 0.5f;
          shopObj.shopkeepFSM              = npcObj.GetComponent<PlayMakerFSM>();
          shopObj.shopItemShadowPrefab     = shared_auto_001.LoadAsset<GameObject>("Merchant_Key").GetComponent<BaseShopController>().shopItemShadowPrefab;
          shopObj.prerequisites            = prerequisites;
          shopObj.cat                      = null;

          if (hasMinimapIcon)
          {
              if (!string.IsNullOrEmpty(minimapIconSpritePath))
              {
                  shopObj.OptionalMinimapIcon = new GameObject("minimap_icon_sprite").RegisterPrefab(deactivate: false);
                  shopObj.OptionalMinimapIcon.AddComponent<tk2dSprite>().SetSprite(VFX.Collection, VFX.Collection.GetSpriteIdByName(minimapIconSpritePath));
              }
              else
              {
                  shopObj.OptionalMinimapIcon = ResourceCache.Acquire("Global Prefabs/Minimap_NPC_Icon") as GameObject;
              }
          }

          shopObj.ShopCostModifier     = costModifier;
          shopObj.FlagToSetOnEncounter = GungeonFlags.NONE;
          shopObj.giveStatsOnPurchase  = giveStatsOnPurchase;
          shopObj.statsToGive          = statsToGiveOnPurchase;

          npcObj.transform.parent   = shopObj.gameObject.transform;
          npcObj.transform.position = npcPosition;

          if (hasCarpet)
          {
              GameObject carpetObj = new GameObject(prefix + ":" + name + "_Carpet").RegisterPrefab(activate: true);  // force our new game object to be active immediately
              tk2dSprite carpetSprite = carpetObj.AddComponent<tk2dSprite>();
              carpetSprite.SetSprite(VFX.Collection, VFX.Collection.GetSpriteIdByName(carpetSpritePath));
              carpetSprite.SortingOrder = 2;

              CarpetOffset ??= Vector2.zero;

              carpetObj.transform.position = new Vector3(CarpetOffset.Value.x, CarpetOffset.Value.y, 1.7f);
              carpetObj.transform.parent = shopObj.gameObject.transform;
              carpetObj.layer = 20;
          }
          npcObj.SetActive(true);

          if (addToMainNpcPool)
          {
              shared_auto_002.LoadAsset<DungeonPlaceable>("shopannex_contents_01").variantTiers.Add(new DungeonPlaceableVariant
              {
                  percentChance           = percentChanceForMainPool,
                  unitOffset              = new Vector2(-0.5f, -1.25f),
                  nonDatabasePlaceable    = shopObj.gameObject,
                  enemyPlaceableGuid      = "",
                  pickupObjectPlaceableId = -1,
                  forceBlackPhantom       = false,
                  addDebrisObject         = false,
                  prerequisites           = prerequisites, //shit for unlocks gose here sooner or later
                  materialRequirements    = new DungeonPlaceableRoomMaterialRequirement[0],

              });
          }

          ShopAPI.builtShops.Add(prefix + ":" + name, shopObj.gameObject);
          return shopObj.gameObject;
      }
      catch (Exception message)
      {
          ETGModConsole.Log(message.ToString());
          return null;
      }
  }

  public static RoomFactory.RoomData BuildNewRoomFromResourceWithoutRegistering(string roomPath)
  {
      RoomFactory.RoomData roomData = RoomFactory.ExtractRoomDataFromResource(roomPath, Assembly.GetCallingAssembly());
      roomData.name = Path.GetFileName(roomPath);
      roomData.room = RoomFactory.Build(roomData);
      RoomFactory.PostProcessCells(roomData);

      // if (!rooms.ContainsKey(roomData.room.name))
      // {
      //     rooms.Add(roomData.room.name, roomData);
      // }
      // DungeonHandler.Register(roomData);
      return roomData;
  }

  // Extension for checking shop activation conditions using CwaffPrerequisite and returning CwaffPrerequisite for inline setup
  public static CwaffPrerequisites SetupPrerequisite(this CwaffPrerequisites prereq, FancyShopBuilder.SpawnCondition validator)
  {
    if (prereq != CwaffPrerequisites.NONE)
      CwaffPrerequisite.AddPrequisiteValidator(prereq, validator);
    else if (validator != null)
      ETGModConsole.Log($"ATTEMPTED TO SET UP VALIDATOR FOR PREREQUISITE == NONE");
    return prereq;
  }

  // Stolen from NN for now
  public static GenericLootTable CreateLootTable(List<GenericLootTable> includedLootTables = null, DungeonPrerequisite[] prerequisites = null)
  {
      GenericLootTable lootTable = ScriptableObject.CreateInstance<GenericLootTable>();
      lootTable.defaultItemDrops = new WeightedGameObjectCollection()
      {
          elements = new List<WeightedGameObject>()
      };
      lootTable.tablePrerequisites = prerequisites ?? new DungeonPrerequisite[0];
      lootTable.includedLootTables = includedLootTables ?? new List<GenericLootTable>();
      return lootTable;
  }

  public static void ForceSpawnForDebugPurposes(this PrototypeDungeonRoom room)
  {
      SharedInjectionData injector                   = ScriptableObject.CreateInstance<SharedInjectionData>();
      injector.UseInvalidWeightAsNoInjection         = true;
      injector.PreventInjectionOfFailedPrerequisites = false/*true*/;
      injector.IsNPCCell                             = false;
      injector.IgnoreUnmetPrerequisiteEntries        = false;
      injector.OnlyOne                               = false;
      injector.ChanceToSpawnOne                      = 1f;
      injector.AttachedInjectionData                 = new List<SharedInjectionData>();
      injector.InjectionData                         = new List<ProceduralFlowModifierData>
      {
        GenerateNewProcData(room, GlobalDungeonData.ValidTilesets.CASTLEGEON),
      };
      injector.name = $"Debug Force Spawn Room {Guid.NewGuid()}";
      SharedInjectionData BaseInjection = LoadHelper.LoadAssetFromAnywhere<SharedInjectionData>("Base Shared Injection Data");
      BaseInjection.AttachedInjectionData ??= new List<SharedInjectionData>();
      BaseInjection.AttachedInjectionData.Add(injector);
  }

  public static ProceduralFlowModifierData GenerateNewProcData(PrototypeDungeonRoom RequiredRoom, GlobalDungeonData.ValidTilesets Tileset)
  {
    string name = RequiredRoom.name.ToString()+Tileset.ToString();
    if (RequiredRoom.name.ToString() == null)
      name = "EmergencyAnnotationName";

    Vector2 offset = new Vector2(-2.25f, -1.25f);
    Vector2 vector = new Vector2((float)(RequiredRoom.Width / 2) + offset.x, (float)(RequiredRoom.Height / 2) + offset.y);

    RequiredRoom.placedObjectPositions.Add(vector);

    ProceduralFlowModifierData SpecProcData = new ProceduralFlowModifierData()
    {
      annotation = name,
      DEBUG_FORCE_SPAWN = false/*true*/,
      OncePerRun = false,
      placementRules = new List<ProceduralFlowModifierData.FlowModifierPlacementType>()
      {
        ProceduralFlowModifierData.FlowModifierPlacementType.END_OF_CHAIN
      },
      roomTable = null,
      exactRoom = RequiredRoom,
      IsWarpWing = false,
      RequiresMasteryToken = false,
      chanceToLock = 0,
      selectionWeight = 2,
      chanceToSpawn = 1,
      RequiredValidPlaceable = null,
      prerequisites = new DungeonPrerequisite[]
      {
        new CwaffPrerequisite { prerequisite = CwaffPrerequisites.TEST_PREREQUISITE.SetupPrerequisite(validator: null) }
      },
      CanBeForcedSecret = false,
      RandomNodeChildMinDistanceFromEntrance = 0,
      exactSecondaryRoom = null,
      framedCombatNodes = 0,
    };
    return SpecProcData;
  }

  public static void InjectRoomIntoUniquePool(PrototypeDungeonRoom protoroom, string injectionAnnotation, List<ProceduralFlowModifierData.FlowModifierPlacementType> placementRules, float chanceToLock, List<DungeonPrerequisite> prerequisites,
     string injectorName, float selectionWeight = 1, float chanceToSpawn = 1, GameObject addSingularPlaceable = null, float XFromCenter = 0, float YFromCenter = 0, bool oncePerRun = false, int allowedTilesets = 127)
  {
      if (addSingularPlaceable != null)
      {
          Vector2 offset = new Vector2(-0.75f, -0.75f);
          Vector2 vector = new Vector2((float)(protoroom.Width / 2) + offset.x, (float)(protoroom.Height / 2) + offset.y);

          protoroom.placedObjectPositions.Add(vector);
          DungeonPrerequisite[] prereqArray = new DungeonPrerequisite[0];

          GameObject original = addSingularPlaceable;
          DungeonPlaceable placeableContents = ScriptableObject.CreateInstance<DungeonPlaceable>();
          placeableContents.width = 2;
          placeableContents.height = 2;
          placeableContents.respectsEncounterableDifferentiator = true;
          placeableContents.variantTiers = new List<DungeonPlaceableVariant> {
              new DungeonPlaceableVariant
              {
                  percentChance = 1f,
                  nonDatabasePlaceable = original,
                  prerequisites = prereqArray,
                  materialRequirements = new DungeonPlaceableRoomMaterialRequirement[0]
              }
          };

          protoroom.placedObjects.Add(new PrototypePlacedObjectData
          {
              contentsBasePosition  = vector,
              fieldData             = new List<PrototypePlacedObjectFieldData>(),
              instancePrerequisites = prereqArray,
              linkedTriggerAreaIDs  = new List<int>(),
              placeableContents     = placeableContents

          });
      }

      ProceduralFlowModifierData injection = new ProceduralFlowModifierData()
      {
          annotation                             = injectionAnnotation,
          DEBUG_FORCE_SPAWN                      = false,
          OncePerRun                             = oncePerRun,
          placementRules                         = new List<ProceduralFlowModifierData.FlowModifierPlacementType>(placementRules),
          roomTable                              = null,
          exactRoom                              = protoroom,
          IsWarpWing                             = false,
          RequiresMasteryToken                   = false,
          chanceToLock                           = chanceToLock,
          selectionWeight                        = selectionWeight,
          chanceToSpawn                          = chanceToSpawn,  // chance this paricular room will spawn from the current SharedInjectionData room pool
          RequiredValidPlaceable                 = null,
          prerequisites                          = prerequisites.ToArray(),
          CanBeForcedSecret                      = true,
          RandomNodeChildMinDistanceFromEntrance = 0,
          exactSecondaryRoom                     = null,
          framedCombatNodes                      = 0,
      };

      int numValidTilesets = 0;
      GlobalDungeonData.ValidTilesets[] array = Enum.GetValues(typeof(GlobalDungeonData.ValidTilesets)) as GlobalDungeonData.ValidTilesets[];
      foreach (GlobalDungeonData.ValidTilesets tileset in array)
      {
        if (((int)tileset & allowedTilesets) == (int)tileset)
        {
          // Lazy.DebugLog($"  have tileset {Enum.GetName(typeof(GlobalDungeonData.ValidTilesets), tileset)}");
          ++numValidTilesets;
        }
      }

      SharedInjectionData injector = new SharedInjectionData(){
        name                                  = injectorName,
        UseInvalidWeightAsNoInjection         = true,
        PreventInjectionOfFailedPrerequisites = false,
        IsNPCCell                             = false,
        IgnoreUnmetPrerequisiteEntries        = false,
        OnlyOne                               = oncePerRun,
        ChanceToSpawnOne                      = 1.0f /* / numValidTilesets*/,  // chance the MetaInjection will spawn this sharedInjection data on the current floor
        AttachedInjectionData                 = new List<SharedInjectionData>(),
        InjectionData                         = new List<ProceduralFlowModifierData> {
            injection
        }
      };
      // Lazy.DebugLog($"  there are {numValidTilesets} valid tilesets for this shop");

      GameManager.Instance.GlobalInjectionData.entries.Add(new MetaInjectionDataEntry{
        injectionData                    = injector,
        MinToAppearPerRun                = oncePerRun ? 1 : numValidTilesets, // corresponds to how many floors this room will try to spawn on, not how many instances of the room will spawn on the floor
        MaxToAppearPerRun                = oncePerRun ? 1 : numValidTilesets, // if this number is lower than the number of enabled tilesets in validTilesets, everything will break
        OverallChanceToTrigger           = 1f, // chance this particular MetaInjectionDataEntry is present in the current run at all
        UsesUnlockedChanceToTrigger      = false,
        UnlockedChancesToTrigger         = new MetaInjectionUnlockedChanceEntry[0]{},
        UsesWeightedNumberToAppearPerRun = false,
        WeightedNumberToAppear           = new(),
        AllowBonusSecret                 = false,
        IsPartOfExcludedCastleSet        = false,
        validTilesets                    = (GlobalDungeonData.ValidTilesets)allowedTilesets // this is virtually useless since Gungeon uses GenerationShuffle(), which will never shuffle 2-element lists
        // validTilesets                    = (GlobalDungeonData.ValidTilesets)127 // everything before Bullet Hell
      });
  }

  // need to call this locally because the Alexandria version of PackerHelper.AddSpriteToCollection uses it's own AssemblyName due to how it's implemented
  public static void AddParentedAnimationToShopFixed(this FancyShopData shop, List<string> yourPaths, float YourAnimFPS, string AnimationName)
  {
      GameObject self = shop.shop;
      var collection = self.GetComponentInChildren<tk2dSprite>().Collection;
      tk2dSpriteAnimator spriteAnimator = self.GetComponentInChildren<tk2dSpriteAnimator>();
      AIAnimator aianimator = self.GetComponentInChildren<AIAnimator>();
      if (yourPaths != null)
      {
          List<int> stealIdsList = AtlasHelper.AddSpritesToCollection(yourPaths, collection).AsRange();
          // ShopAPI.CreateDirectionalAnimation(spriteAnimator, collection, aianimator, stealIdsList, AnimationName, YourAnimFPS);
          CreateDirectionalAnimation(spriteAnimator, collection, aianimator, stealIdsList, AnimationName, YourAnimFPS);
      }
  }

  private static void CreateDirectionalAnimation(tk2dSpriteAnimator spriteAnimator, tk2dSpriteCollectionData collection, AIAnimator aianimator, List<int> IdsList, string animationName, float FPS)
  {
      SpriteBuilder.AddAnimation(spriteAnimator, collection, IdsList, animationName, tk2dSpriteAnimationClip.WrapMode.Once, FPS);
      DirectionalAnimation aa = new DirectionalAnimation
      {
          Type = DirectionalAnimation.DirectionType.Single,
          Prefix = animationName,
          AnimNames = new string[1],
          Flipped = new DirectionalAnimation.FlipType[1]
      };
      if (aianimator.OtherAnimations != null)
      {
          aianimator.OtherAnimations.Add(
          new AIAnimator.NamedDirectionalAnimation
          {
              name = animationName,
              anim = aa
          });
      }
      else
      {
          aianimator.OtherAnimations = new List<AIAnimator.NamedDirectionalAnimation>
          {
              new AIAnimator.NamedDirectionalAnimation
              {
                  name = animationName,
                  anim = aa
              }
          };
      }
  }

  public static void SetShotAnimation(this FancyShopData shop, List<string> paths, float fps)
  {
      shop.AddParentedAnimationToShopFixed(paths, fps, "shot");
      // need to update when instantiated since Reset() is called on the DialogueBox FsmStateAction
      shop.owner.gameObject.AddComponent<AddShotAnimation>();
  }

  private class AddShotAnimation : MonoBehaviour
  {
      private void Start()
      {
          foreach (FsmStateAction action in base.GetComponent<PlayMakerFSM>().FsmStates[8].Actions)
          {
              if (action is not DialogueBox dialogue)
                  continue;
              dialogue.SuppressDefaultAnims = false;
              dialogue.OverrideTalkAnim     = "shot";
          }
      }
  }
}

public class ForceOutOfStockOnFailedSteal : MonoBehaviour
{
    private CustomShopController _shop = null;
    private bool _didOutOfStock = false;

    private void Start()
    {
        this._shop = base.GetComponent<CustomShopController>();
    }

    private void Update()
    {
        if (this._didOutOfStock)
            return;
        if (!this._shop.m_wasCaughtStealing)
            return;

        foreach (Transform child in base.transform)
        {
            CustomShopItemController[] shopItems =child?.gameObject?.GetComponentsInChildren<CustomShopItemController>();
            if ((shopItems?.Length ?? 0) == 0)
                continue;
            if (shopItems[0] is not CustomShopItemController shopItem)
                continue;
            if (!shopItem.item)
                continue;
            if (!shopItem.pickedUp)
                shopItem.ForceOutOfStock();
        }

        this._didOutOfStock = true;
    }
}
