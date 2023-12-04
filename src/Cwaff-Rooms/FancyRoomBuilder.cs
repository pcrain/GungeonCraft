namespace CwaffingTheGungy;

public static class FancyRoomBuilder
{
  public delegate bool SpawnCondition();

  public static Dictionary<string, PrototypeDungeonRoom> FancyShopRooms = new();
  // public static Vector3[] defaultItemPositions = new Vector3[] {
  //   new Vector3(1.125f, 2.125f, 1),
  //   new Vector3(2.625f, 1f, 1),
  //   new Vector3(4.125f, 2.125f, 1) };

  private static List<string> _DefaultLine = new(){"Buy somethin', will ya!"};

  public static PrototypeDungeonRoom MakeFancyShop(string npcName, List<int> shopItems, string roomPath, float spawnChance = 1f, Floors? spawnFloors = null,
    CwaffPrerequisites spawnPrerequisite = CwaffPrerequisites.NONE, SpawnCondition spawnPrequisiteChecker = null, string voice = null,
    List<String> genericDialog = null, List<String> stopperDialog = null, List<String> purchaseDialog = null,
    List<String> noSaleDialog = null, List<String> introDialog = null, List<String> attackedDialog = null, bool allowDupes = false,
    float mainPoolChance = 0.0f, Vector3? talkPointOffset = null, Vector3? npcPosition = null, List<Vector3> itemPositions = null,
    bool oncePerRun = true)
  {
    if (C.DEBUG_BUILD)
      ETGModConsole.Log($"Setting up shop for {npcName}");

    string npcNameUpper = npcName.ToUpper();

    $"#{npcNameUpper}_GENERIC_TALK".SetupDBStrings(genericDialog ?? _DefaultLine);
    $"#{npcNameUpper}_STOPPER_TALK".SetupDBStrings(stopperDialog ?? _DefaultLine);
    $"#{npcNameUpper}_PURCHASE_TALK".SetupDBStrings(purchaseDialog ?? _DefaultLine);
    $"#{npcNameUpper}_NOSALE_TALK".SetupDBStrings(noSaleDialog ?? _DefaultLine);
    $"#{npcNameUpper}_INTRO_TALK".SetupDBStrings(introDialog ?? _DefaultLine);
    $"#{npcNameUpper}_ATTACKED_TALK".SetupDBStrings(attackedDialog ?? _DefaultLine);

    List<DungeonPrerequisite> dungeonPrerequisites = new(){
      new CwaffPrerequisite { prerequisite = spawnPrerequisite.SetupCheck(spawnPrequisiteChecker, oncePerRun) }
    };

    string currencyIcon = ResMap.Get($"{npcName}_icon", quietFailure: true)?[0];
    if (!string.IsNullOrEmpty(currencyIcon))
      currencyIcon += ".png";

    GameObject shop = ShopAPI.SetUpShop(
      name                              : npcName,
      prefix                            : C.MOD_PREFIX,
      idleSpritePaths                   : ResMap.Get($"{npcName}_idle"),
      idleFps                           : 2,
      talkSpritePaths                   : ResMap.Get($"{npcName}_talk"),
      talkFps                           : 2,
      lootTable                         : shopItems.ToLootTable(),
      // currency                          : CustomShopItemController.ShopCurrencyType.CUSTOM,
      currency                          : CustomShopItemController.ShopCurrencyType.COINS,
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
      costModifier                      : 1f,
      giveStatsOnPurchase               : false,
      statsToGiveOnPurchase             : null,
      CustomCanBuy                      : null,
      CustomRemoveCurrency              : null,
      CustomPrice                       : null,
      // CustomPrice                       : HealthTraderCustomPrice,
      OnPurchase                        : null,
      OnSteal                           : null,
      currencyIconPath                  : currencyIcon/*""*/,
      // currencyIconPath                  : "",
      // currencyName                      : "test",
      currencyName                      : "ui_coin",
      canBeRobbed                       : true,
      hasCarpet                         : ResMap.Get($"{npcName}_carpet", quietFailure: true)?[0] != null,
      carpetSpritePath                  : ResMap.Get($"{npcName}_carpet", quietFailure: true)?[0],
      CarpetOffset                      : null,
      hasMinimapIcon                    : ResMap.Get($"{npcName}_icon", quietFailure: true)?[0] != null,
      minimapIconSpritePath             : ResMap.Get($"{npcName}_icon", quietFailure: true)?[0],
      addToMainNpcPool                  : mainPoolChance > 0,
      percentChanceForMainPool          : mainPoolChance,
      prerequisites                     : dungeonPrerequisites.ToArray(), // used by RegisterShopRoom(), but we're manually calling AddInjection() for now
      fortunesFavorRadius               : 2,
      poolType                          : allowDupes ? CustomShopController.ShopItemPoolType.DUPES : CustomShopController.ShopItemPoolType.DEFAULT,
      RainbowModeImmunity               : false,
      hitboxSize                        : null, // must be null, doesn't work properly
      // hitboxSize                        : new IntVector2(0/*13*/, 0/*19*/), // must be null, doesn't work properly
      hitboxOffset                      : null // must be null, doesn't work properly
      );

    // Track how many times this shop room has been spawned in a run for prerequisite tracking purposes
    shop.AddComponent<CwaffPrerequisite.Tracker>().Setup(spawnPrerequisite);

    TalkDoerLite npc = shop.GetComponentInChildren<TalkDoerLite>();
    npc.gameObject.AddComponent<FlipsToFacePlayer>();
    // npc.GetComponent<AIAnimator>().IdleAnimation.Type = DirectionalAnimation.DirectionType.TwoWayHorizontal;
    // npc.GetComponent<AIAnimator>().IdleAnimation.Flipped[0] = DirectionalAnimation.FlipType.Mirror;
    if (!string.IsNullOrEmpty(voice))
      npc.audioCharacterSpeechTag = voice;

    PrototypeDungeonRoom shopRoom = RoomFactory.BuildNewRoomFromResource(roomPath: roomPath).room;

    // ShopAPI.RegisterShopRoom(shop: shop, protoroom: shopRoom, vector: new Vector2((float)(shopRoom.Width / 2), (float)(shopRoom.Height / 2)));
    RoomFactory.AddInjection(
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
      YFromCenter          : 0
      );

    // THIS DOESN'T REALLY WORK TOO WELL. manually handling it the hacky way for now
    // if (oncePerRun) // small hack for now since Alexandria doesn't let us modify this directly as of right now
    // {
    //   SharedInjectionData baseInjection    = LoadHelper.LoadAssetFromAnywhere<SharedInjectionData>("Base Shared Injection Data");

    //   SharedInjectionData injector         = baseInjection.AttachedInjectionData[baseInjection.AttachedInjectionData.Count - 1];
    //   injector.OnlyOne                     = true;
    //   injector.ChanceToSpawnOne            = spawnChance;
    //   // injector.InjectionData[0].OncePerRun = true;  // this doesn't seem to work
    //   baseInjection                        = null;
    // }

    // foreach (Floors floor in Enum.GetValues(typeof(Floors)))
    // {
    //   if ((floor & spawnFloors) == 0)
    //     continue;
    //   // TODO: add shop room to specific floor
    // }

    // _HandleNodeInjectionILHook ??= new ILHook(
    //   typeof(LoopFlowBuilder).GetMethod("HandleNodeInjection", BindingFlags.Instance | BindingFlags.NonPublic, null,
    //     new [] {typeof(BuilderFlowNode), typeof(RuntimeInjectionMetadata), typeof(RuntimeInjectionFlags), typeof(FlowCompositeMetastructure)}, null),
    //   HandleNodeInjectionIL
    //   );

    // _DebugProcessSingleNodeInjectionILHook ??= new ILHook(
    //   typeof(LoopFlowBuilder).GetMethod("ProcessSingleNodeInjection", BindingFlags.Instance | BindingFlags.NonPublic),
    //   DebugProcessSingleNodeInjectionIL
    //   );

    FancyShopRooms[npcName] = shopRoom;
    return shopRoom;
  }

  // private static ILHook _HandleNodeInjectionILHook = null;
  // private static void HandleNodeInjectionIL(ILContext il)
  // {
  //   ILCursor cursor = new ILCursor(il);
  //   // cursor.DumpILOnce("HandleNodeInjectionIL" );

  //   // MetaInjectionData.InjectionSetsUsedThisRun faultily adds nodes to its list before actually confirming they were successfully added
  //   //   so we need to undo that if we failed to actually add anything
  //   if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchCall<LoopFlowBuilder>("ProcessSingleNodeInjection")))
  //         return; // failed to find what we need
  //   if (!cursor.TryGotoNext(MoveType.Before, instr => instr.MatchLdnull()))
  //         return; // failed to find what we need

  //   cursor.Emit(OpCodes.Ldloc_2); // Get our old proceduralFlowModifierData
  //   cursor.Emit(OpCodes.Ldarg_2); // Get sourceMetadata
  //   cursor.Emit(OpCodes.Call, typeof(FancyRoomBuilder).GetMethod("RemoveProceduralFlowModifierDataFromSetsUsedThisRun", BindingFlags.Static | BindingFlags.NonPublic));
  // }

  // private static void RemoveProceduralFlowModifierDataFromSetsUsedThisRun(ProceduralFlowModifierData data, RuntimeInjectionMetadata source)
  // {
  //   // ETGModConsole.Log($"  RETRY {data.exactRoom?.name ?? "null"}");
  //   if (MetaInjectionData.InjectionSetsUsedThisRun.Contains(data))
  //   {
  //     MetaInjectionData.InjectionSetsUsedThisRun.Remove(data);
  //     source.HasAssignedModDataExactRoom = false;
  //   }
  // }

  // private static ILHook _DebugProcessSingleNodeInjectionILHook = null;
  // private static void DebugProcessSingleNodeInjectionIL(ILContext il)
  // {
  //   ILCursor cursor = new ILCursor(il);
  //   // cursor.DumpILOnce("DebugProcessSingleNodeInjectionIL" );

  //   // swap the debug print statements from false to true
  //   cursor.Remove();
  //   cursor.Emit(OpCodes.Ldc_I4_1);
  // }

  // public static int HealthTraderCustomPrice(CustomShopController shop, CustomShopItemController itemCont, PickupObject item)
  // {
  //   return 5;
  // }

  private static PrototypeDungeonRoom _BasicShopRoom = null;
  public static PrototypeDungeonRoom GetBasicShopRoom()
  {
    return RoomFactory.BuildNewRoomFromResource(roomPath: "CwaffingTheGungy/Resources/Rooms/BasicShopRoom2.newroom").room; // rebuild every time for now
    // if (_BasicShopRoom == null)
    //   _BasicShopRoom = RoomFactory.BuildFromResource(roomPath: "CwaffingTheGungy/Resources/Rooms/BasicShopRoom.room").room;
    // return _BasicShopRoom;
  }

  // Extension for checking shop activation conditions using CwaffPrerequisite and returning CwaffPrerequisite for inline setup
  public static CwaffPrerequisites SetupCheck(this CwaffPrerequisites prereq, FancyRoomBuilder.SpawnCondition check, bool oncePerRun)
  {
    CwaffPrerequisite.AddPrequisiteCheck(prereq, check, oncePerRun);
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
        new CwaffPrerequisite { prerequisite = CwaffPrerequisites.TEST_PREREQUISITE.SetupCheck(check: null, oncePerRun: false) }
      },
      CanBeForcedSecret = false,
      RandomNodeChildMinDistanceFromEntrance = 0,
      exactSecondaryRoom = null,
      framedCombatNodes = 0,
    };
    return SpecProcData;
  }
}
