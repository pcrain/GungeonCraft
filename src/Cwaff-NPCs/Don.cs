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
      if (PizzaTimeController._CurDeliveries == PizzaTimeController._MaxDeliveries)
        return State.PIZZA_TIME_SUCCESS;
      if (PizzaTimeController._CurDeliveries == 0)
        return State.PIZZA_TIME_FAILED;
      return State.PIZZA_TIME_PARTIAL;
    }
    if (interactor.IsGunLocked)
      return State.INCAPABLE_OF_DELIVERY;
    if (PizzaTimeController.AnyRoomsStillOccupied())
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

  protected override IEnumerator NPCTalkingScript()
  {
    PlayerController interactor = Interactor();
    State state = DetermineTalkingState(interactor);
    switch(state)
    {
      case State.READY_FOR_PIZZA_TIME: yield return ScriptREADY_FOR_PIZZA_TIME(); break;
      case State.ENEMIES_ON_FLOOR: yield return ScriptENEMIES_ON_FLOOR(); break;
      case State.INCAPABLE_OF_DELIVERY: yield return ScriptINCAPABLE_OF_DELIVERY(); break;
      case State.PIZZA_TIME_FAILED: yield return ScriptPIZZA_TIME_FAILED(); break;
      case State.PIZZA_TIME_PARTIAL: yield return ScriptPIZZA_TIME_PARTIAL(); break;
      case State.PIZZA_TIME_SUCCESS: yield return ScriptPIZZA_TIME_SUCCESS(); break;
      case State.NO_DELIVERIES_FINISHED: yield return ScriptNO_DELIVERIES_FINISHED(); break;
      case State.SOME_DELIVERIES_FINISHED: yield return ScriptSOME_DELIVERIES_FINISHED(); break;
      case State.ALL_DELIVERIES_FINISHED: yield return ScriptALL_DELIVERIES_FINISHED(); break;
    }

    // yield return Dialogue(new(){
    //   "hey",
    //   "how's it going",
    //   "got the goods",
    // }, "idle");

    // yield return Dialogue(new(){
    //   "aw",
    //   "too bad",
    //   "lorem ipsum dolor sit amet consecutive words i really don't remember how the rest of this goes to be quite honest with you i'm just testing custom voice boxes",
    // }, "cry");

    // yield return Prompt("Nothing", "Something");
    // if (PromptResult() == 1)
    // {
    //   yield return Dialogue(new(){
    //     "amazing",
    //   }, "celebrate");
    // }

    Reset();

    yield break;
  }
}
