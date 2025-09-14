namespace CwaffingTheGungy;

/* Available Talking Animations:
  idle
  idle_calm     [eyes closed]
  idle_empty    [eyes angry]
  idle_glance   [eyes smug]
  idle_sad      [eyes sad]
  shocked       [eyes shocked]
  shrug         [eyes winking]
  shrug_calm    [eyes closed]
  shrug_glance  [eyes smug]
*/

public class ArmisticeNPC : BossNPC
{
  private bool _talkedOnce = false;

  protected override IEnumerator PreFightScript()
  {
    // this.defaultAudioEvent = "sans_laugh";
    this.audioTag = "Lady";

    GameManager.Instance.MainCameraController.OverridePosition = this.sprite.transform.localPosition + new Vector3(0f, 2f, 0f);
    GameManager.Instance.MainCameraController.SetManualControl(true, true);

    if (!this._talkedOnce)
    {
      this._talkedOnce = true;
      CustomTrackedStats.ENCOUNTERED_ARMI.Increment();
      #if DEBUG
      System.Console.WriteLine($"handling encounter number {(int)CustomTrackedStats.ENCOUNTERED_ARMI.Get()}");
      #endif

      yield return Converse("Hunh...well then...");
      yield return Converse("I don't see a lot of people down here.");
      yield return Converse("I don't see anyone else down here, really.");
      yield return Prompt("Where are we?");

      yield return Converse("Technically, we're in the Abbey of the True Gun.");
      yield return Converse("A better question is \"when are we\"?");
      yield return Prompt("Okay...*when* are we?");

      yield return Converse("We are suspended in time, at the exact moment the Gungeon was created.");
      yield return Prompt("How do you know this?");

      yield return Converse("Believe me, I've had ample time to investigate and reflect upon the existence of this place.");
      yield return Prompt("How did I end up here?", "How did you end up here?");
      if (PromptResult() == 0)
        yield return Converse("Presumably, the same way I did.");
      else
        yield return Converse("Presumably, the same way you did.");

      yield return Converse("You entered the Gungeon to kill your past, and succeeded.");
      yield return Converse("You discovered the greater evil within the Gungeon, and banished it.");
      yield return Converse("Something inexplicable drew you back to the Gungeon.");
      yield return Converse("You loaded and fired the Gun That Can Kill the Past once more, with no particular purpose in mind.");
      yield return Converse("...and now you're here, at the birth of the Gungeon.");
      yield return Converse("It's rather peaceful here, considering none of the Gundead have migrated beyond Bullet Hell yet.");
      yield return Converse("In my opinion, it's a very nice place to relax and reflect for a while.");
      yield return Prompt("...I'm sorry, but who are you?");

      yield return Converse("Armistice. You can call me Armi if you'd like.");
      yield return Prompt("Am I stuck here?", "Are you stuck here?");

      yield return Converse("Oh, no, we can leave at any time via the usual methods...");
      yield return Converse("...taking the elevator in the room behind us.........or dying.");
      yield return Prompt("Where does the elevator lead?");

      yield return Converse("Straight to Bullet Hell, the birthplace of the Gundead.");
      yield return Converse("It probably looks exactly how you're accustomed to seeing it.");
      yield return Prompt("...and you mentioned dying?");

      yield return Converse("It's as temporary as ever, and very effective for leaving this place.");
      yield return Converse("Though there isn't much here that can kill you, besides me.");
      yield return Prompt("Is that a threat?", "You couldn't kill me if you tried!");
      if (PromptResult() == 0)
      {
        yield return Converse("No. On the contrary, I come here to get away from all of the fighting the rest of the Gungeon usually demands.");
        yield return Converse("And I'm more than happy to share the space with you, should you desire to stay for a while.");
      }
      else
      {
        yield return Converse("I'm very confident I could. I have more than enough equipment and experience for the task.");
        yield return Converse("But that's besides the point.");
      }

      yield return Converse("If I'm being quite honest, I'd prefer not to fight.");
      yield return Converse("...");
      yield return Converse("...although...");
      yield return Converse("A duel to the death sounds a lot more fun for both of us than the alternative ways you have out of here.");
      yield return Converse("And it's not like I can't find my way back here just fine should you somehow emerge victorious.");
      yield return Converse("So...why not, we can fight if you really want.");
      yield return Prompt("Maybe later.", "Let's do it!");

      if (PromptResult() == 0)
        yield return Converse("Alright. I'll be here for a bit.");
      else
        yield return DoFightDialogue();
      yield break;
    }

    yield return Converse("Hey. What's up?");
    yield return Prompt("What else is there to do here?", "Let's fight!");
    if (PromptResult() == 0)
    {
      yield return Converse("Not much.");
      yield return Converse("Time doesn't pass here, so I like coming here to meditate and relax.");
      yield return Converse("The walls don't seem to shift in here either. It's looked like this ever since I first came down here.");
      yield return Converse("...");
      yield return Converse("...truthfully, this place could really use some decorating.");
      yield return Converse("Maybe later.");
    }
    else
      yield return DoFightDialogue();
    yield break;
  }

  private IEnumerator DoFightDialogue()
  {
      yield return Converse("Alright. Guess relaxation time's over.");
      yield return Converse("Good luck, you'll need it.");
      StartBossFight(); // accept
  }

  protected override IEnumerator DefeatedScript()
  {
    this.ShowText("Damn. See you around I guess.", autoContinueTimer: 1f);
    yield return new WaitForSeconds(1f);
    // this.ShowText("take this treasure chest", autoContinueTimer: 1f);
    // yield return new WaitForSeconds(1f);
    yield break;
  }

  protected override IEnumerator PostFightScript()
  {
    // GameManager.Instance.MainCameraController.OverridePosition = this.sprite.transform.localPosition;
    // GameManager.Instance.MainCameraController.SetManualControl(true, true);
    this.ShowText("Ugh.", autoContinueTimer: 1f);
    yield break;
    // yield return Converse("the item in that chest is all yours",
    //   "idle", "idle", "armistice_laugh");
    // yield return Converse("as for that secret...",
    //     "idle", "idle", "armistice_laugh");
    // yield return Converse("have you ever come across a {wj}Normal{w} gun?",
    //     "idle", "idle", "armistice_laugh");
    // yield return Converse("i don't mean a standard or ordinary gun, but a {wj}Normal{w} gun",
    //     "shrug_calm", "shrug_calm", "armistice_laugh");
    // yield return Converse("i've heard if you drop a {wj}Normal{w} gun inside a triangle of 3 other guns...",
    //     "idle", "idle", "armistice_laugh");
    // yield return Converse("something cool might happen depending on the combined strength of the guns",
    //     "idle_glance", "idle_glance", "armistice_laugh");
    // yield return Converse("if something cool DOES happen, try using a blank",
    //     "idle", "idle", "armistice_laugh");
    // yield return Converse("and if nothing happens, maybe try dropping some stronger guns",
    //     "shrug", "shrug", "armistice_laugh");
    // yield return Converse("what, you think i'm making all of this up?",
    //     "idle", "idle", "armistice_laugh");
    // yield return Converse("eh, could be",
    //     "idle_glance", "idle_glance", "armistice_laugh");
    // yield return Converse("let's do this again some time",
    //   "shrug", "shrug", "armistice_laugh");
    // SetAnimation("idle");
  }
}
