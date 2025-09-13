namespace CwaffingTheGungy;

/// <summary>Helper class for integrating new dialog lines into existing TalkDoerLites' FSMs</summary>
public static class CwaffDialog
{
  public static void AddNewDialogState(this TalkDoerLite talker, string stateName, List<string> dialogue, bool replaceExisting = false, string yesPrompt = null, string yesState = null, string noPrompt = null, string noState = null, Action customAction = null)
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

    // determine whether we have a branching dialogue
    bool hasPrompt = (!string.IsNullOrEmpty(yesPrompt) && !string.IsNullOrEmpty(yesState) && !string.IsNullOrEmpty(noPrompt) && !string.IsNullOrEmpty(noState));

    // create the new state
    FsmState newState = new FsmState(fsm.fsm) {
      name        = stateName,
      description = "runtime dialogue state",
      isSequence  = true,
      actions     = new FsmStateAction[0],
    };

    // set up strings for our branching dialogue prompt
    if (hasPrompt)
    {
      newState.transitions = new FsmTransition[]{
        new(){fsmEvent = new FsmEvent($"{stateName}_answer_yes"), toState = yesState},
        new(){fsmEvent = new FsmEvent($"{stateName}_answer_no"), toState = noState},
      };
    }

    // set up strings for the dialogue itself
    List<FsmString> dStrings = newState.actionData.fsmStringParams = new();
    foreach (string s in dialogue)
      dStrings.Add(new(){value = s, useVariable = true, name = s});

    // determine if this state needs to execute custom code
    if (customAction != null)
    {
      Append(ref newState.actions, new PerformAction() {
        action = customAction,
      });
    }

    // set up the dialogue itself
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

    // set the responses if we have them, or kill the conversation otherwise
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

    // initialize all of our new actions with the state info
    foreach (FsmStateAction action in newState.actions)
      action.Init(newState);

    // register our events and states
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
    if (talker.playmakerFsm is not PlayMakerFSM fsm)
    {
      System.Console.WriteLine($"couldn't find FSM for dialog");
      return;
    }
    if (fsm.fsm.activeState is not FsmState curState)
    {
      System.Console.WriteLine($"fsm state is not active");
      return;
    }
    if (fsm.fsm.GetState(stateName) is not FsmState targetState)
    {
      System.Console.WriteLine($"target state does not exist");
      return;
    }

    string eventName = $"{stateName}_event";

    // verify we can transitino to our target state from the current state
    bool hasTransition = false;
    foreach (var t in curState.transitions)
    {
      if (t.fsmEvent.name != eventName)
        continue;
      hasTransition = true;
      break;
    }
    if (!hasTransition) // make sure our current state can transition to our dialogue state
      Append(ref fsm.fsm.activeState.transitions, new(){ fsmEvent = fsm.fsm.GetEvent(eventName), toState = stateName });

    // verify we have a BeginConversation action for our event
    if (targetState.actions[0] is not BeginConversation)
    {
      BeginConversation beginner = new(){ conversationType = BeginConversation.ConversationType.Normal };
      Prepend(ref targetState.actions, beginner);
      beginner.Init(targetState);
    }

    talker.SendPlaymakerEvent(eventName);
  }

  private static void Append<T>(ref T[] arr, T val)
  {
    int oldLength = arr.Length;
    Array.Resize(ref arr, oldLength + 1);
    arr[oldLength] = val;
  }

  private static void Prepend<T>(ref T[] arr, T val)
  {
    int oldLength = arr.Length;
    Array.Resize(ref arr, oldLength + 1);
    for (int i = oldLength; i > 0; --i)
      arr[i] = arr[i - 1];
    arr[0] = val;
  }

  /// <summary>super simple class to execute arbitrary code</summary>
  public class PerformAction : FsmStateAction
  {
    public Action action = null;

    public override void OnEnter()
    {
      if (action != null)
        action();
      Finish();
    }
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
