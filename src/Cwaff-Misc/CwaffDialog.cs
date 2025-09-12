namespace CwaffingTheGungy;

public static class CwaffDialog
{
  public static void AddNewDialogState(this TalkDoerLite talker, string stateName, List<string> dialogue, bool isStartState = false, bool replaceExisting = false, string yesPrompt = null, string yesState = null, string noPrompt = null, string noState = null)
  {
    if (talker.playmakerFsm is not PlayMakerFSM fsm)
    {
      System.Console.WriteLine($"no fsm found");
      return;
    }

    // check if the state already exists and bail out if so
    foreach (FsmState state in fsm.fsm.states)
    {
      if (state.name == stateName)
      {
        if (!replaceExisting)
        {
          System.Console.WriteLine($"state {stateName} already found, doing nothing");
          return;
        }
        //TODO: handle replacement
      }
    }

    bool hasPrompt = (!string.IsNullOrEmpty(yesPrompt) && !string.IsNullOrEmpty(yesState) && !string.IsNullOrEmpty(noPrompt) && !string.IsNullOrEmpty(noState));

    // add the new state
    FsmState newState = new FsmState(fsm.fsm) {
      name        = stateName,
      description = "runtime dialogue state",
      isSequence  = true,
      actions     = new FsmStateAction[0],
    };

    if (hasPrompt)
    {
      newState.transitions = new FsmTransition[]{
        new(){fsmEvent = new FsmEvent($"{stateName}_answer_yes"), toState = yesState},
        new(){fsmEvent = new FsmEvent($"{stateName}_answer_no"), toState = noState},
      };
    }

    List<FsmString> dStrings = newState.actionData.fsmStringParams = new();
    foreach (string s in dialogue)
      dStrings.Add(new(){value = s, useVariable = true, name = s});

    if (isStartState)
    {
      Append(ref newState.actions, new BeginConversation() {
        conversationType = BeginConversation.ConversationType.Normal,
      });
      if (fsm.fsm.GetState("Idle") is FsmState idleState) //TODO: don't hardcode base state
        Append(ref idleState.transitions, new(){ fsmEvent = new FsmEvent($"{stateName}_event"), toState = stateName });
      else
        System.Console.WriteLine($"could not find idle state");
    }

    var dialogueBox = new DialogueBox() {
      sequence                   = DialogueBox.DialogueSequence.Default,
      dialogue                   = dStrings.ToArray(),
      responses                  = new FsmString[0],
      skipWalkAwayEvent          = false,
      PlayBoxOnInteractingPlayer = false,
      IsThoughtBubble            = false,
      SuppressDefaultAnims       = false,
      forceCloseTime             = 0f,
      zombieTime                 = 0f,
      OverrideTalkAnim           = string.Empty,
      events                     = (hasPrompt ? (new FsmEvent[2]{newState.transitions[0].fsmEvent, newState.transitions[1].fsmEvent}) : new FsmEvent[0]),
    };
    Append(ref newState.actions, dialogueBox);

    if (hasPrompt)
    {
        dialogueBox.responses = new FsmString[2]{
          yesPrompt.AutoCoreKey(),
          noPrompt.AutoCoreKey(),
        };
    }
    else
    {
      Append(ref newState.actions, new EndConversation() {
        killZombieTextBoxes        = true,
        doNotLerpCamera            = false,
        suppressReinteractDelay    = false,
        suppressFurtherInteraction = false,
      });
    }

    foreach (FsmStateAction action in newState.actions)
      action.Init(newState);

    Append(ref fsm.fsm.events, new FsmEvent($"{stateName}_event"));
    Append(ref fsm.fsm.states, newState);
  }

  private static string AutoCoreKey(this string s)
  {
    string k = $"#{s}_prompt";
    if (!ETGMod.Databases.Strings.Core.ContainsKey(k))
      ETGMod.Databases.Strings.Core.Set(k, s);
    return k;
  }

  public static void StartDialog(this TalkDoerLite talker, string stateName)
  {
    talker.SendPlaymakerEvent($"{stateName}_event");
  }

  private static void Append<T>(ref T[] arr, T val)
  {
    int oldLength = arr.Length;
    Array.Resize(ref arr, oldLength + 1);
    arr[oldLength] = val;
  }
}

// [HarmonyPatch]
// internal static class FSMDebugPatches
// {
//   [HarmonyPatch(typeof(Fsm), nameof(Fsm.Event), typeof(string))]
//   [HarmonyPrefix]
//   private static void FSMEventPatch(Fsm __instance, string fsmEventName)
//   {
//     // System.Console.WriteLine($"calling event {fsmEventName} on {__instance.Name} for target {__instance.EventTarget}");
//   }

//   [HarmonyPatch(typeof(DialogueBox), nameof(DialogueBox.OnUpdate))]
//   [HarmonyPostfix]
//   private static void DialogueBoxOnUpdatePatch(DialogueBox __instance)
//   {
//     // System.Console.WriteLine($"m_dialogueState == {__instance.m_dialogueState}, m_talkDoer.State == {__instance.m_talkDoer.State}, finished == {__instance.finished}");
//   }
// }
