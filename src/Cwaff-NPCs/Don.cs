namespace CwaffingTheGungy;

public class Don : FancyNPC
{
  public static FancyNPC _NPC;

  private enum State
  {
    // FIRST_MEETING_EVER,       // first ever encounter with Don
    // FIRST_MEETING_RUN,        // first encounter with Don this run
    // LATER_MEETING_RUN,        // second or higher encounter with Don this run (only possible with Domino item)
    READY_FOR_PIZZA_TIME,     // all prerequisites for starting pizza event fulfilled
    ENEMIES_ON_FLOOR,         // can't start pizza event due to enemies being on floor
    INCAPABLE_OF_DELIVERY,    // can't start pizza event due to being gun locked / other reasons
    PIZZA_TIME_FAILED,        // pizza event happened this floor and was failed
    PIZZA_TIME_PARTIAL,       // pizza event happened this floor and was partially successful
    PIZZA_TIME_SUCCESS,       // pizza event happened this floor and was completely successful
    PIZZA_TIME_TIMEOUT,       // pizza event happened this floor and was not completed in time
    NO_DELIVERIES_FINISHED,   // no pizzas have been delivered, and you are wasting your time talking to Don
    SOME_DELIVERIES_FINISHED, // at least one pizza has been delivered, but not all of them
    ALL_DELIVERIES_FINISHED,  // all pizzas have been delivered
  }

  public static void Init()
  {
    // _NPC = CwaffNPCBuilder.Build("don");
    _NPC = FancyNPC.Setup<Don>(
      name: "don",
      animNames: new(){
        "don_idle",
        "don_talk",
        "don_shout",
        "don_cry",
        "don_celebrate",
      }
    ).GetComponent<FancyNPC>();

    _NPC.lockCamera = true;
    // _NPC.defaultAudioEvent = "don_voice_1";
    _NPC.defaultAudioEvents = ["don_voice_1", "don_voice_2"];
    _NPC.voiceRate = 0.1f;

    List<DungeonPrerequisite> dungeonPrerequisites = new(){
      new CwaffPrerequisite { prerequisite = CwaffPrerequisites.NONE }
    };

    VFX.Create("pizza_box_decor", anchor: Anchor.LowerCenter).RegisterEasyRATPlaceable("pizza_box");

    string roomPath = $"{C.MOD_INT_NAME}/Resources/Rooms/pizza.newroom";
    PrototypeDungeonRoom donRoom = FancyShopBuilder.BuildNewRoomFromResourceWithoutRegistering(roomPath).room;  // prevents the game from spawning the rooms and disregarding prerequisites
    FancyShopBuilder.InjectRoomIntoUniquePool(
      protoroom            : donRoom,
      injectionAnnotation  : "Don's Pizza Shop Room",
      placementRules       : new() { ProceduralFlowModifierData.FlowModifierPlacementType.END_OF_CHAIN },
      chanceToLock         : 0,
      prerequisites        : dungeonPrerequisites,
      injectorName         : "Don's Pizza Shop Room",
      selectionWeight      : 1,
      chanceToSpawnEachRun : C.DEBUG_BUILD ? 1.0f : 0.1f, //TODO: tweak later
      addSingularPlaceable : _NPC.gameObject,
      XFromCenter          : 0.0f,
      YFromCenter          : 3.25f,
      oncePerRun           : true, //NOTE: necessary to make sure the validator doesn't have to do any heavy lifting (possibly makes validator redundant?)
      allowedTilesets      : (int)(GlobalDungeonData.ValidTilesets.CASTLEGEON/* | GlobalDungeonData.ValidTilesets.GUNGEON*/)
      );

  }

  protected override void Start()
  {
    base.Start();
    RoomHandler room = base.transform.position.GetAbsoluteRoom();
    List<MinorBreakable> breakables = room.GetComponentsInRoom<MinorBreakable>();
    for (int i = breakables.Count - 1; i >= 0; --i)
    {
      MinorBreakable breakable = breakables[i];
      GameObject go = breakable.gameObject;
      if (go.name != "Kitchen_Counter(Clone)")
        continue;
      breakable.OnlyBrokenByCode = true; // make the counters indestructible
    }
    PizzaTimeController._DonNPC = this;
  }

  private State DetermineTalkingState(PlayerController interactor)
  {
    if (PizzaTimeController._PizzaTimeHappening)
    {
      if (PizzaTimeController._CurDeliveries == 0)
        return State.NO_DELIVERIES_FINISHED;
      if (PizzaTimeController._CurDeliveries == PizzaTimeController._MaxDeliveries)
        return State.ALL_DELIVERIES_FINISHED;
      return State.SOME_DELIVERIES_FINISHED;
    }
    if (PizzaTimeController._PizzaTimeAttemptedThisFloor)
    {
      if (PizzaTimeController._TimerExpired)
        return State.PIZZA_TIME_TIMEOUT;
      if (PizzaTimeController._CurDeliveries == PizzaTimeController._MaxDeliveries)
        return State.PIZZA_TIME_SUCCESS;
      if (PizzaTimeController._CurDeliveries == 0)
        return State.PIZZA_TIME_FAILED;
      return State.PIZZA_TIME_PARTIAL;
    }
    if (interactor.IsGunLocked)
      return State.INCAPABLE_OF_DELIVERY;
    if (PizzaTimeController.AnyRoomsStillOccupied() && !C.DEBUG_BUILD)
      return State.ENEMIES_ON_FLOOR;
    return State.READY_FOR_PIZZA_TIME;
  }

  private IEnumerator ScriptREADY_FOR_PIZZA_TIME()
  {
    yield return Converse("Are you ready to deliver some pizzas?");
    yield return Prompt("Not yet.", "I'm ready!");
    if (PromptResult() == 1)
    {
      PizzaTimeController.StartPizzaTime(Interactor());
    }
    yield break;
  }

  private IEnumerator ScriptENEMIES_ON_FLOOR()
  {
    yield return Converse("ScriptENEMIES_ON_FLOOR");
    yield break;
  }

  private IEnumerator ScriptINCAPABLE_OF_DELIVERY()
  {
    yield return Converse("ScriptINCAPABLE_OF_DELIVERY");
    yield break;
  }

  private IEnumerator ScriptPIZZA_TIME_FAILED()
  {
    yield return Converse("ScriptPIZZA_TIME_FAILED");
    yield break;
  }

  private IEnumerator ScriptPIZZA_TIME_PARTIAL()
  {
    yield return Converse("ScriptPIZZA_TIME_PARTIAL");
    yield break;
  }

  private IEnumerator ScriptPIZZA_TIME_SUCCESS()
  {
    yield return Converse("ScriptPIZZA_TIME_SUCCESS");
    yield break;
  }

  private IEnumerator ScriptPIZZA_TIME_TIMEOUT()
  {
    yield return Converse("ScriptPIZZA_TIME_TIMEOUT");
    yield break;
  }

  private IEnumerator ScriptNO_DELIVERIES_FINISHED()
  {
    yield return Converse("ScriptNO_DELIVERIES_FINISHED");
    yield break;
  }

  private IEnumerator ScriptSOME_DELIVERIES_FINISHED()
  {
    yield return Converse("Are you done?");
    yield return Prompt("Not yet.", "I'm done!");
    if (PromptResult() == 1)
    {
      PizzaTimeController.HandleDeliverySuccess();
    }
    yield break;
  }

  private IEnumerator ScriptALL_DELIVERIES_FINISHED()
  {
    PizzaTimeController.HandleDeliverySuccess();
    yield return Converse("Beautiful!", "cry");
    yield break;
  }

  private IEnumerator ScriptUNKNOWN()
  {
    yield break;
  }

  protected override IEnumerator NPCTalkingScript()
  {
    yield return DetermineTalkingState(Interactor()) switch
    {
      State.READY_FOR_PIZZA_TIME     => ScriptREADY_FOR_PIZZA_TIME(),
      State.ENEMIES_ON_FLOOR         => ScriptENEMIES_ON_FLOOR(),
      State.INCAPABLE_OF_DELIVERY    => ScriptINCAPABLE_OF_DELIVERY(),
      State.PIZZA_TIME_FAILED        => ScriptPIZZA_TIME_FAILED(),
      State.PIZZA_TIME_PARTIAL       => ScriptPIZZA_TIME_PARTIAL(),
      State.PIZZA_TIME_SUCCESS       => ScriptPIZZA_TIME_SUCCESS(),
      State.PIZZA_TIME_TIMEOUT       => ScriptPIZZA_TIME_TIMEOUT(),
      State.NO_DELIVERIES_FINISHED   => ScriptNO_DELIVERIES_FINISHED(),
      State.SOME_DELIVERIES_FINISHED => ScriptSOME_DELIVERIES_FINISHED(),
      State.ALL_DELIVERIES_FINISHED  => ScriptALL_DELIVERIES_FINISHED(),
      _                              => ScriptUNKNOWN(),
    };
    Reset();
    yield break;
  }

  public override float GetOverrideMaxDistance()
  {
    return 2.0f; // so we can interact from across a counter
  }
}
