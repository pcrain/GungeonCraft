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

public static class FancyRoomBuilder
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
    float spawnChance = 1f, Vector2? carpetOffset = null,
    CwaffPrerequisites spawnPrerequisite = CwaffPrerequisites.NONE, SpawnCondition prequisiteValidator = null, string voice = null,
    List<String> genericDialog = null, List<String> stopperDialog = null, List<String> purchaseDialog = null,
    List<String> noSaleDialog = null, List<String> introDialog = null, List<String> attackedDialog = null, bool allowDupes = false,
    float mainPoolChance = 0.0f, Vector3? talkPointOffset = null, Vector3? npcPosition = null, List<Vector3> itemPositions = null,
    bool exactlyOncePerRun = true, int allowedTilesets = 127, float costModifier = 1f,
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

    List<DungeonPrerequisite> dungeonPrerequisites = new(){
      new CwaffPrerequisite { prerequisite = spawnPrerequisite.SetupPrerequisite(prequisiteValidator) }
    };

    string currencyIcon = ResMap.Get($"{npcName}_currency", quietFailure: true)?[0];
    if (!string.IsNullOrEmpty(currencyIcon))
      currencyIcon += ".png";

    GenericLootTable lootTable = shopItems.ToLootTable();
    GameObject shop = ShopAPI.SetUpShop(
      name                              : npcName,
      prefix                            : C.MOD_PREFIX,
      idleSpritePaths                   : ResMap.Get($"{npcName}_idle"),
      idleFps                           : 2,
      talkSpritePaths                   : ResMap.Get($"{npcName}_talk"),
      talkFps                           : 5,
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
      canBeRobbed                       : true,
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
  public static CwaffPrerequisites SetupPrerequisite(this CwaffPrerequisites prereq, FancyRoomBuilder.SpawnCondition validator)
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
}
