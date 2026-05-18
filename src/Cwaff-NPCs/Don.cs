namespace CwaffingTheGungy;

public class Don : FancyNPC
{
  private const float _DON_CHANCE_PER_RUN = 0.06f;

  private static int _EncountersThisRun = 0;

  private bool _talked;
  private bool _didCheckin;
  private RoomHandler _room;

  public static FancyNPC _NPC;

  private enum State
  {
    FIRST_MEETING_EVER,       // first ever encounter with Don
    FIRST_MEETING_RUN,        // first encounter with Don this run
    LATER_MEETING_RUN,        // second or higher encounter with Don this run (only possible with Domino item)
    READY_FOR_PIZZA_TIME,     // all prerequisites for starting pizza event fulfilled
    ENEMIES_ON_FLOOR,         // can't start pizza event due to enemies being on floor
    INCAPABLE_OF_DELIVERY,    // can't start pizza event due to being gun locked / other reasons
    COOP_MODE,                // can't start pizza event due to being in co-op mode
    NEED_FULL_MAP,            // can't start pizza event due to not having the floor fully mapped
    PIZZA_TIME_FAILED,        // pizza event happened this floor and was failed
    PIZZA_TIME_PARTIAL,       // pizza event happened this floor and was partially successful
    PIZZA_TIME_SUCCESS,       // pizza event happened this floor and was completely successful
    PIZZA_TIME_TIMEOUT,       // pizza event happened this floor and was not completed in time
    PIZZA_TIME_RUINED,        // pizza event happened this floor and was cut short due to taking damage
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
        "don_gesture",
        "don_shout",
        "don_cry",
        "don_celebrate",
      }
    ).GetComponent<FancyNPC>();

    _NPC.SetAnimationFPS("talk", 6);
    _NPC.SetAnimationFPS("gesture", 6);
    _NPC.SetAnimationFPS("shout", 9);

    _NPC.lockCamera = true;
    // _NPC.defaultAudioEvent = "don_voice_1";
    _NPC.defaultAudioEvents = ["don_voice_1", "don_voice_2", "don_voice_3"];
    _NPC.voiceRate = 0.1f;
    _NPC.alwaysReturnToIdle = false;

    VFX.Create("pizza_box_decor", anchor: Anchor.LowerCenter).RegisterEasyRATPlaceable("pizza_box");

    PrototypeDungeonRoom donRoom = FancyShopBuilder.BuildNewRoomFromResourceWithoutRegistering(
      $"{C.MOD_INT_NAME}/Resources/Rooms/pizza.newroom").room; // prevents the game from spawning the rooms and disregarding prerequisites
    SetUpDonsRoom(donRoom, forDomino: false); // set up normal room variant that spawns randomly up to once per run
    SetUpDonsRoom(donRoom, forDomino: true); // set up special room variant that has a guaranteed spawn each floor when the player has Domino
  }

  private static void SetUpDonsRoom(PrototypeDungeonRoom donRoom, bool forDomino)
  {
    string roomName = forDomino ? "Don's Other Pizza Shop Room" : "Don's Pizza Shop Room";
    FancyShopBuilder.InjectRoomIntoUniquePool(
      protoroom            : donRoom,
      injectionAnnotation  : roomName,
      placementRules       : new() { ProceduralFlowModifierData.FlowModifierPlacementType.END_OF_CHAIN },
      chanceToLock         : 0,
      prerequisites        : [new CwaffPrerequisite(){ prerequisite = forDomino
        ? CwaffPrerequisites.HAVE_DOMINO.SetupPrerequisite(CwaffPrerequisite.HaveDomino)
        : CwaffPrerequisites.NO_DOMINO.SetupPrerequisite(CwaffPrerequisite.NoHaveDomino)
      }],
      injectorName         : roomName,
      selectionWeight      : 1,
      chanceToSpawnEachRun : forDomino ? 1.0f : (C.DEBUG_BUILD ? 1.0f : _DON_CHANCE_PER_RUN),
      addSingularPlaceable : forDomino ? null : _NPC.gameObject, // prevent double-adding
      XFromCenter          : 0.0f,
      YFromCenter          : 3.25f,
      oncePerRun           : !forDomino, //NOTE: necessary to make sure the validator doesn't have to do any heavy lifting (possibly makes validator redundant?)
      allowedTilesets      : (forDomino || !C.DEBUG_BUILD) ? 127 : (int)GlobalDungeonData.ValidTilesets.CASTLEGEON
      );
  }

  protected override void Start()
  {
    base.Start();
    base.transform.position = base.transform.position.Quantize(0.0625f);
    RoomHandler room = this._room = base.transform.position.GetAbsoluteRoom();
    // Lazy.DebugConsoleLog($"spawned room {room.area.prototypeRoom.name}");
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

    CwaffEvents.OnChangedRooms += this.OnChangedRooms;
  }

  private static readonly List<Tuple<string, string>> _EntryStrings = [
    new("Exactly 7 pepperonis per quadrant...good, good.", "idle"),
    new("Mushrooms sliced to precisely 35 millimeters thick...good, good.", "idle"),
    new("Pizza sauce heated in a medium saucepan for 8 minutes at 91 degrees centigrade...good, good.", "idle"),
    new("Cheese melted until hue is fulvous with a 0.1% margin of error...good, good.", "idle"),
    new("This cheese is AMBER, it's supposed to be FULVOUS!", "shout"),
    new("These acute folds will never do. They're called pizza BOXES not pizza SQUARE FRUSTUMS!", "shout"),
  ];

  private void OnChangedRooms(PlayerController controller, RoomHandler oldRoom, RoomHandler newRoom)
  {
    PizzaTimeController.CheckAnyRoomsStillOccupied();
    if (newRoom != this._room)
      return;
    var dialogue = _EntryStrings.ChooseRandom();
    ShowText(dialogue.First, 3f);
    SetAnimation(dialogue.Second);
  }

  public override void OnDestroy()
  {
    CwaffEvents.OnChangedRooms -= this.OnChangedRooms;
    base.OnDestroy();
  }

  private static void ClearEncountersThisRun()
  {
    CwaffEvents.OnCleanStart -= ClearEncountersThisRun;
    _EncountersThisRun = 0;
  }

  private State DetermineTalkingState(PlayerController interactor)
  {
    if (GameManager.Instance.CurrentGameType == GameManager.GameType.COOP_2_PLAYER)
      return State.COOP_MODE;

    bool firstEncounterEver = (int)CustomTrackedStats.ENCOUNTERED_DON.Get() == 0;
    bool firstEncounterThisRun = _EncountersThisRun == 0;
    bool firstEncounterThisFloor = !this._talked;
    if (firstEncounterThisFloor)
      CustomTrackedStats.ENCOUNTERED_DON.Increment();
    if ((++_EncountersThisRun) == 1)
    {
        CwaffEvents.OnCleanStart -= ClearEncountersThisRun;
        CwaffEvents.OnCleanStart += ClearEncountersThisRun;
    }
    this._talked = true;

    if (firstEncounterEver)
      return State.FIRST_MEETING_EVER;
    if (firstEncounterThisRun)
      return State.FIRST_MEETING_RUN;
    if (firstEncounterThisFloor)
      return State.LATER_MEETING_RUN;
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
      if (PizzaTimeController._RuinedEquipment)
        return State.PIZZA_TIME_RUINED;
      if (PizzaTimeController._CurDeliveries == 0)
        return State.PIZZA_TIME_FAILED;
      if (PizzaTimeController._TimerExpired)
        return State.PIZZA_TIME_TIMEOUT;
      if (PizzaTimeController._CurDeliveries == PizzaTimeController._MaxDeliveries)
        return State.PIZZA_TIME_SUCCESS;
      return State.PIZZA_TIME_PARTIAL;
    }
    if (interactor.IsGunLocked)
      return State.INCAPABLE_OF_DELIVERY;
    if (!GameManager.Instance.Dungeon.AllRoomsVisited)
      return State.NEED_FULL_MAP;
    if (PizzaTimeController.CheckAnyRoomsStillOccupied()/* && !C.DEBUG_BUILD*/)
      return State.ENEMIES_ON_FLOOR;
    return State.READY_FOR_PIZZA_TIME;
  }

  private IEnumerator ScriptFIRST_MEETING_EVER()
  {
    yield return Converse("UNACCEPTABLE!", "shout");
    yield return Converse("9 PEPPERONIS IN ONE QUADRANT?! THAT'S THE DEVIL'S WORK!", "shout");
    yield return Converse("...", "idle");
    yield return Converse("...*sniff*...why can't anyone follow a simple pizza recipe?", "cry");
    yield return Converse("Oh...oh a customer!", "cry");
    yield return Converse("Wait...no...you're not here to order are you?", "idle");
    yield return Converse("Actually, i don't believe we've met before.", "idle");
    yield return Converse("Mi name is Don Mino, but most people around here call mi Papa Don.", "idle");
    yield return Converse("And this...this is mi pizza kitchen.", "idle");
    yield return Converse("Mi beautiful pizza kitchen.", "cry");
    yield return Converse("...*sniff*...", "cry");
    yield return Converse("...ahem, excuse mi for getting a touch emotional.", "cry");
    yield return Converse("If you'll allow an old man to tell a story.", "cry");
    yield return Converse("Ever since i was a little boi, i've always dreamed of opening mi own pizza kitchen.", "talk", "gesture");
    yield return Converse("Mi father owned a pizza kitchen,", "talk", "gesture");
    yield return Converse("and his father owned a pizza kitchen,", "talk", "gesture");
    yield return Converse("and HIS father owned a pizza kitchen,", "talk", "gesture");
    yield return Converse("and HIS father...", "talk", "gesture");
    yield return Converse("...well, he was actually a leatherworker, but that's besides the point...", "talk", "gesture");
    yield return Converse("...because HIS father owned a pizza kitchen!", "shout");
    yield return Converse("Pizza making is in mi blood, don't you see?", "talk", "gesture");
    yield return Converse("Alas...nobody here seems to share mi passion for the pizzas.", "cry");
    yield return Converse("Not a single soul I've hired has shown the art of pizza crafting the care and respect it deserves.", "cry");
    yield return Converse("Just the other day, i had to fire someone for grating cheese incorrectly.", "talk", "gesture");
    yield return Converse("They grated the mozzarella cheese with the parmasean cheese grater.", "talk", "gesture");
    yield return Converse("CAN YOU BELIEVE IT?! THE PARMASEAN GRATER.", "shout");
    yield return Converse("Getting trace shreds of parmasean on a pristine mozzarella brick prior to baking, why...", "talk", "gesture");
    yield return Converse("...it ruins the entire integrity of the cheese foundation.", "cry");
    yield return Converse("...*sniff*...anyway...", "cry");
    yield return Converse("As you can see I'm a little short-staffed at the moment.", "idle");
    yield return Converse("I can manage the pizza-making mi self, but the deliveries...oh, the Gungeon's a dangerous place for a simple pizza maker like mi.", "idle");
    yield return Converse("I've never held any weapon strong than a pizza peel in my life, and the Gundead are swarming everywhere!", "idle");
    yield return Converse("Although...", "idle");
    yield return Converse("...", "idle");
    yield return Converse("...between you and mi, the Bullet Kin are mi best customers!", "idle");
    yield return Converse("They won't come anywhere near mi shop while their cohorts are around, but they love to order delivery while off duty.", "idle");
    yield return Converse("Since I'm needed in here making the pizzas, I rely on help from adventurers like yourself to make sure mi pizzas make their way to stomachs in need.", "idle");
    yield return Converse("BUT!", "talk", "gesture");

    yield return GiveInstructions();

    yield break;
  }

  private IEnumerator ScriptFIRST_MEETING_RUN()
  {
    yield return Converse("Oh, welcome back, mi slightly inept protege.", "idle");
    yield return Converse("Do you need a refresher on the art of pizza delivery?", "idle");
    yield return Prompt("Nope.", "Yes please.");
    if (PromptResult() == 0)
      yield return PrepareForPizzaTime();
    else
      yield return GiveInstructions();
    yield break;
  }

  private IEnumerator ScriptLATER_MEETING_RUN()
  {
    yield return Converse("Ah, mi protege", "idle");
    yield return Converse("The Bullet Kin are unusually hungry today. Which means there is much pizza to be made and delivered!", "idle");
    yield return PrepareForPizzaTime();
    yield break;
  }

  private IEnumerator ScriptREADY_FOR_PIZZA_TIME()
  {
    yield return Converse("Well then....", "idle");
    yield return Converse("There are poor, hungry Bullet Kin out there, counting on us...", "idle");
    yield return Converse("...counting on YOU...", "talk", "gesture");
    yield return Converse("...to deliver them fresh, hot, immaculately crafted pizza at a reasonable price.", "idle");
    yield return Converse("Can I count on you to perform this sacred duty, mi protege?", "idle");
    yield return Prompt("Not right now.", "Of course!");
    if (PromptResult() == 1)
    {
      yield return Converse("Wonderful!", "idle");
      yield return Converse("Your shift starts...", "idle");
      yield return Converse("10 SECONDS AGO...GET MOVING!", "shout");
      PizzaTimeController.StartPizzaTime(Interactor());
    }
    else
      yield return Converse("THEN GET OUTTA MI KITCHEN! I HAVE PIZZAS TO MAKE!", "shout");

    Reset();

    yield break;
  }

  private IEnumerator ScriptENEMIES_ON_FLOOR()
  {
    yield return Converse("I'm afraid the Gundead are still on duty, which means there's nobody to order mi pizzas." ,"idle");
    yield return Converse("Come back once they've all been dealt with!" ,"talk", "gesture");
    Reset();
    yield break;
  }

  private IEnumerator ScriptINCAPABLE_OF_DELIVERY()
  {
    yield return Converse("It doesn't seem like you're capable of holding a pizza peel at the moment." ,"idle");
    yield return Converse("Come back once you're prepared to wield the sacred instrument!" ,"talk", "gesture");
    Reset();
    yield break;
  }

  private IEnumerator ScriptNEED_FULL_MAP()
  {
    yield return Converse("That look in your eyes tells me you have incomplete knowledge of your delivery route." ,"idle");
    yield return Converse("Come back once you've learned the lay of the land!" ,"talk", "gesture");
    Reset();
    yield break;
  }

  private IEnumerator ScriptCOOP_MODE()
  {
    yield return Converse("I'm not looking for multiple pizza deliverers right now." ,"idle");
    yield return Converse("Come back once you're by yourself!" ,"talk", "gesture");
    Reset();
    yield break;
  }

  private IEnumerator ScriptPIZZA_TIME_FAILED()
  {
    if (!this._didCheckin)
    {
      yield return Converse("...", "idle");
      yield return Converse("...", "idle");
      yield return Converse("YOU THINK PIZZA DELIVERY IS SOME KIND OF GAME!", "shout");
      yield return Converse("Every day I labor tirelessly over a hot oven to make the finest pizzas this Gungeon has ever seen.", "cry");
      yield return Converse("And YOU!", "shout");
      yield return Converse("YOU let EVERY SINGLE ONE of them go to waste!", "shout");
      yield return Converse("Get out of my kitchen! NOW!", "shout");
      Interactor().IncreaseCurse(3);
      this._didCheckin = true;
    }
    else
      yield return Converse("Get out of my kitchen! NOW!", "shout");

    yield break;
  }

  private IEnumerator ScriptPIZZA_TIME_PARTIAL()
  {
    float completion = (float)PizzaTimeController._CurDeliveries / (float)PizzaTimeController._MaxDeliveries;
    if (completion >= 0.5f)
      yield return BonusDialogue();
    else
      yield return Converse("Please...leave me be....", "cry");
    yield break;
  }

  private IEnumerator ScriptPIZZA_TIME_SUCCESS()
  {
    yield return BonusDialogue();
    yield break;
  }

  private IEnumerator ScriptPIZZA_TIME_TIMEOUT()
  {
    if (!this._didCheckin)
    {
      yield return Converse("Well well well, if it isn't mi sluggish protege. You certainly took your time delivering all of those pizzas.", "talk", "gesture");
      yield return Converse("And you couldn't even be bothered to return for equipment polishing and inspection?", "cry");
      yield return Converse("The labor for refurbishing that equipment is coming out of your wages!", "shout");
      this._didCheckin = true;
    }

    yield return Converse("Until next time, mi slothlike protege.", "talk", "gesture");

    yield break;
  }

  private IEnumerator ScriptPIZZA_TIME_RUINED()
  {
    if (!this._didCheckin)
    {
      yield return Converse("YOU FOOL! YOU'VE SOILED MI PIZZA DELIVERY EQUIPMENT!", "talk", "shout");
      yield return Converse("It's going to take ages to get those scuff marks out of the peel.", "cry");
      yield return Converse("The labor for refurbishing that equipment is coming out of your wages!", "shout");
      this._didCheckin = true;
    }

    yield return Converse("Until next time, mi careless protege.", "talk", "gesture");
    yield break;
  }

  private IEnumerator ScriptNO_DELIVERIES_FINISHED()
  {
    yield return Converse("WHY ARE YOU STILL HERE?! PERFORM YOUR SACRED DUTY!", "shout");
    Reset();
    yield break;
  }

  private IEnumerator ScriptSOME_DELIVERIES_FINISHED()
  {
    yield return Converse("Have you finished your route?");
    yield return Prompt("Not yet.", "I'm done!");
    if (PromptResult() == 0)
    {
      yield return Converse("THEN GET BACK OUT THERE AND PERFORM YOUR SACRED DUTY!", "shout");
      Reset();
    }
    else
      yield return ReturnToPost();

    yield break;
  }

  private IEnumerator ScriptALL_DELIVERIES_FINISHED()
  {
    PizzaTimeController.EndPizzaTime();
    yield return Converse("Wonderful, mi protege!", "celebrate");
    yield return Converse("All of mi customers are satisfied.", "celebrate");
    yield return Converse("I will sleep well tonight knowing all of mi customers have full bellies.", "celebrate");
    yield return Converse("Take your daily wages and tips, plus this extra bonus as a personal token of thanks.", "celebrate");
    PizzaTimeController.HandleDeliverySuccess();
    yield return Converse("Until next time, mi protege.", "celebrate");
    yield break;
  }

  private IEnumerator ScriptUNKNOWN()
  {
    yield return BonusDialogue();
    yield break;
  }

  private IEnumerator GiveInstructions()
  {
    while (true)
    {
      yield return Converse("Delivering pizzas is an art that takes a lifetime to master.", "talk", "gesture");
      yield return Converse("First, the pizzas must be delivered fresh out of the oven.", "talk", "gesture");
      yield return Converse("\"Thirty minutes or less\"?! Even five minutes is far too long!", "shout");
      yield return Converse("Second, the pizzas must not be touched once they are baked.", "talk", "gesture");
      yield return Converse("They must be gently scooped onto a peel and flung directly to the customer's hands.", "talk", "gesture");
      yield return Converse("The only one handling the pizza after it's cooked must be the one eating it!", "talk", "gesture");
      yield return Converse("Finally, pizza delivery is a sacred task. It must be performed without hesitation or distraction.", "talk", "gesture");
      yield return Converse("One must know the delivery route. One must commit to the delivery route. And most importantly...", "talk", "gesture");
      yield return Converse("...one must RETURN from the delivery route in a timely matter for equipment inspection and polishing.", "talk", "gesture");
      yield return Converse("Did you get all that?", "idle");
      yield return Prompt("One more time please.", "Understood!");
      if (PromptResult() == 1)
        break;
    }

    yield return PrepareForPizzaTime();

    yield break;
  }

  private IEnumerator ReturnToPost()
  {
    PizzaTimeController.EndPizzaTime();
    float completion = (float)PizzaTimeController._CurDeliveries / (float)PizzaTimeController._MaxDeliveries;
    if (completion >= 0.9f)
    {
      yield return Converse("Well...", "idle");
      yield return Converse("You missed a few customers, But at least it seems most of mi pizzas found themselves a loving stomach to call home.", "talk", "gesture");
      yield return Converse("Take your daily wages and tips.", "idle");
      PizzaTimeController.HandleDeliverySuccess();
      yield return Converse("Until next time, mi relatively ineffective protege.", "talk", "gesture");
      Reset();
    }
    else if (completion >= 0.5f)
    {
      yield return Converse("Well...", "idle");
      yield return Converse("You missed far too many customers, which is completely unacceptable!", "shout");
      yield return Converse("So many wasted pizzas with nowhere to call home.", "cry");
      yield return Converse("Take your daily wages and be off!", "shout");
      PizzaTimeController.HandleDeliverySuccess();
      yield return Converse("Until next time, mi highly ineffective protege.", "talk", "gesture");
      Reset();
    }
    else
    {
      yield return Converse("*sniff*", "cry");
      yield return Converse("You've sullied the entire profession of pizza delivery.", "cry");
      yield return Converse("Mi pizzas have no home. Mi disappointment has no words.", "cry");
      yield return Converse("Please...leave me be....", "cry");
      PizzaTimeController.HandleDeliverySuccess();
      // Reset(); // NOTE: no reset when crying
    }

    yield break;
  }

  private IEnumerator PrepareForPizzaTime()
  {
    PlayerController interactor = Interactor();
    if (interactor.IsGunLocked)
      yield return ScriptINCAPABLE_OF_DELIVERY();
    else if (!Lazy.AllRoomsVisited())
      yield return ScriptNEED_FULL_MAP();
    else if (PizzaTimeController.CheckAnyRoomsStillOccupied()/* && !C.DEBUG_BUILD*/)
      yield return ScriptENEMIES_ON_FLOOR();
    else
      yield return ScriptREADY_FOR_PIZZA_TIME();

    yield break;
  }

  private IEnumerator BonusDialogue()
  {
    int rng = UnityEngine.Random.Range(0, 3);

    if (rng == 0)
    {
      yield return Converse("Sometimes I think the Gungeon may not have been the best place to open mi pizza kitchen.", "idle");
      yield return Converse("But it was very affordable, and at the very least, it's much better than the last place I rented.", "idle");
      yield return Converse("The owner was never there, the lighting was horrendous, and there were these scary animatronics that I swear moved around when you weren't looking.", "idle");
      yield return Converse("Truly terrifying.", "cry");
    }
    else if (rng == 1)
    {
      yield return Converse("Anyone can come up with a pizza recipe, but do you know what the real secret ingredients are to a perfect pizza?", "talk", "gesture");
      yield return Converse("Blood, sweat, and tears!", "shout");
      yield return Converse("Specificially...", "idle");
      yield return Converse("Three milliliters of blood in the pizza sauce from lightly knicking one's thumb slicing the tomatoes.", "talk", "gesture");
      yield return Converse("Eight droplets of sweat to give the mozzarella that extra salty taste.", "talk", "gesture");
      yield return Converse("And tears from 30 seconds of light sobbing to moisten the pizza dough to the perfect consistency.", "talk", "gesture");
    }
    else
    {
      yield return Converse("I have many a fond memory of workin in mi father Don's pizza kitchen as a child.", "idle");
      yield return Converse("...huh? Yes, mi father was also named Don. As was his father, and HIS father, and HIS father.", "idle");
      yield return Converse("We were a poor family you see. We could only afford one name.", "cry");
    }

    Reset();

    yield break;
  }

  protected override IEnumerator NPCTalkingScript()
  {
    yield return DetermineTalkingState(Interactor()) switch
    {
      State.FIRST_MEETING_EVER       => ScriptFIRST_MEETING_EVER(),
      State.FIRST_MEETING_RUN        => ScriptFIRST_MEETING_RUN(),
      State.LATER_MEETING_RUN        => ScriptLATER_MEETING_RUN(),
      State.READY_FOR_PIZZA_TIME     => ScriptREADY_FOR_PIZZA_TIME(),
      State.ENEMIES_ON_FLOOR         => ScriptENEMIES_ON_FLOOR(),
      State.INCAPABLE_OF_DELIVERY    => ScriptINCAPABLE_OF_DELIVERY(),
      State.NEED_FULL_MAP            => ScriptNEED_FULL_MAP(),
      State.COOP_MODE                => ScriptCOOP_MODE(),
      State.PIZZA_TIME_FAILED        => ScriptPIZZA_TIME_FAILED(),
      State.PIZZA_TIME_PARTIAL       => ScriptPIZZA_TIME_PARTIAL(),
      State.PIZZA_TIME_SUCCESS       => ScriptPIZZA_TIME_SUCCESS(),
      State.PIZZA_TIME_TIMEOUT       => ScriptPIZZA_TIME_TIMEOUT(),
      State.PIZZA_TIME_RUINED        => ScriptPIZZA_TIME_RUINED(),
      State.NO_DELIVERIES_FINISHED   => ScriptNO_DELIVERIES_FINISHED(),
      State.SOME_DELIVERIES_FINISHED => ScriptSOME_DELIVERIES_FINISHED(),
      State.ALL_DELIVERIES_FINISHED  => ScriptALL_DELIVERIES_FINISHED(),
      _                              => ScriptUNKNOWN(),
    };
    yield break;
  }

  public override float GetOverrideMaxDistance()
  {
    return 2.0f; // so we can interact from across a counter
  }
}
