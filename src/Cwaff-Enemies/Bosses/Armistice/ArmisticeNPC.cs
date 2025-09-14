namespace CwaffingTheGungy;

public class ArmisticeNPC : BossNPC
{
  private bool _talkedOnce = false;

  protected override IEnumerator PreFightScript()
  {
    // this.defaultAudioEvent = "sans_laugh";
    this.audioTag = "Lady";
    this.defaultTalkAnimation = "talk";
    this.defaultPauseAnimation = "calm";

    GameManager.Instance.MainCameraController.OverridePosition = this.sprite.transform.localPosition + new Vector3(0f, 2f, 0f);
    GameManager.Instance.MainCameraController.SetManualControl(true, true);

    bool firstEncounter = (int)CustomTrackedStats.ENCOUNTERED_ARMI.Get() == 0;
    bool firstTalkThisEncounter = !this._talkedOnce;

    if (firstTalkThisEncounter)
    {
      this._talkedOnce = true;
      CustomTrackedStats.ENCOUNTERED_ARMI.Increment();
      #if DEBUG
      System.Console.WriteLine($"handling encounter number {(int)CustomTrackedStats.ENCOUNTERED_ARMI.Get()}");
      #endif
    }

    if (firstEncounter)
    {
      yield return Converse("Hunh...hello there.");
      yield return Converse("I don't see a lot of people down here.");
      yield return Converse("Actually, I don't think I've ever seen anyone else down here.");
      yield return Prompt("Where are we?");

      yield return Converse("Technically, we're in the Abbey of the True Gun.");
      yield return Converse("A more interesting question would be \"when are we?\"");
      yield return Prompt("Okay... *when* are we?");

      yield return Converse("We are suspended in time, at the exact moment the Gungeon was created.");
      yield return Prompt("How do you know this?");

      yield return Converse("I've had ample time to reflect upon the existence of this place, believe me.");
      yield return Prompt("How did I end up here?", "How did you end up here?");
      if (PromptResult() == 0)
        yield return Converse("Presumably, the same way I did.");
      else
        yield return Converse("Presumably, the same way you did.");

      yield return Converse("You entered the Gungeon to kill your past, and succeeded.");
      yield return Converse("You discovered a greater evil lurked within the Gungeon, and banished it.");
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
      yield return Converse("It's a lot more crowded there now than it is in the present day.");
      yield return Converse("A lot harder to fight through as well.");
      yield return Prompt("...and you mentioned dying?");

      yield return Converse("It's as temporary as ever, and very effective for leaving this place.");
      yield return Converse("Though there isn't much here that can kill you, besides me.");
      yield return Prompt("Is that a threat?", "You couldn't kill me if you tried!");
      if (PromptResult() == 0)
      {
        yield return Converse("No, just an observation.");
        yield return Converse("On the contrary, I come here to get away from all of the fighting the rest of the Gungeon usually demands.");
      }
      else
      {
        yield return Converse("I'm very confident I could. I have more than enough equipment and experience for the task.");
        yield return Converse("But that's besides the point. I come here to get away from all of the fighting the rest of the Gungeon usually demands.");
      }

      yield return Converse("And I'm more than happy to share the space with you, should you desire to stay for a while.");
      yield return Converse("If I'm being quite honest, I'd prefer not to fight.");
      yield return Converse("Admittedly...dueling sounds a lot more entertaining for both of us than fighting through Bullet Hell.");
      yield return Converse("It's not like dying here is permanent for either of us.");
      yield return Converse("So...why not, we can fight if you really want to.");
      yield return Prompt("Not right now.", "Let's do it!");

      if (PromptResult() == 0)
        yield return Converse("Alright. I'm going back to meditating then. Feel free to stay as long as you'd like.");
      else
        yield return DoFightDialogue();
      yield break;
    }

    if (firstTalkThisEncounter)
      yield return Converse("Oh hey, you're back. What's up?");
    else
      yield return Converse("Hello again. What's up?");

    yield return Prompt("Is there anything to do here?", "Let's fight!");
    if (PromptResult() == 0)
    {
      yield return Converse("Not much. Time doesn't pass here, so I like coming here to meditate and relax.");
      yield return Converse("The walls don't seem to shift in here either. It's looked like this ever since I first came down here.");
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
    base.aiActor.gameObject.transform.position.GetAbsoluteRoom().m_hasGivenReward = true; // prevent abbey chest and pedestal from spawning
    base.aiActor.StealthDeath = true; // prevent corpse from spawning
    yield return new WaitForSeconds(1.5f);

    this.talkPointAdjustment = new Vector2(-0.375f, 0.125f);
    this.ShowText("Damn. See you around I guess.", autoContinueTimer: 1f);
    yield return new WaitForSeconds(1.25f);

    base.gameObject.Play("armistice_defeat_teleport_sound");
    base.aiActor.aiAnimator.PlayUntilFinished("death");
    while (base.aiActor.aiAnimator.IsPlaying("death"))
      yield return null;

    base.aiActor.healthHaver.DeathAnimationComplete(null, null);
  }

  protected override IEnumerator PostFightScript()
  {
    // GameManager.Instance.MainCameraController.OverridePosition = this.sprite.transform.localPosition;
    // GameManager.Instance.MainCameraController.SetManualControl(true, true);
    this.ShowText("Ugh.", autoContinueTimer: 1f);
    yield break;
    // yield return Converse("the item in that chest is all yours",
    //   "idle", "idle", "armistice_laugh");
    // SetAnimation("idle");
  }
}
