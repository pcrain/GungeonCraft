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
    GameManager.Instance.MainCameraController.OverridePosition = this.sprite.transform.localPosition;
    GameManager.Instance.MainCameraController.SetManualControl(true, true);
    if (!this._talkedOnce)
    {
      yield return Converse("hey kid, how'd you manage to find me?",
        "idle", "idle", "armistice_laugh");
      yield return Converse("this place ain't exactly easy to get to",
        "idle_glance", "idle_glance", "armistice_laugh");
      yield return Converse("that bello guy tip you off, or you just get here by luck?",
        "idle", "idle", "armistice_laugh");
      yield return Converse("...eh, doesn't matter",
        "shrug_glance", "shrug_glance", "armistice_laugh");
      yield return Converse("seems like you have some cool toys on you",
        "idle", "idle", "armistice_laugh");
      yield return Converse("since you're here, wanna try my little test?",
        "idle_glance", "idle_glance", "armistice_laugh");
      yield return Converse("if you can manage to land a few hits on me, i'll tell you a secret",
        "shrug", "shrug", "armistice_laugh");
      yield return Converse("i don't have any guns on me, but i'll try to make it interesting",
        "idle", "idle", "armistice_laugh");
      yield return Converse("and don't feel the need to hold back, i can handle myself",
        "shrug", "shrug", "armistice_laugh");
      yield return Converse("whaddaya say, kid?",
        "idle_glance", "idle_glance", "armistice_laugh");
      this._talkedOnce = true;
    }
    else
    {
      yield return Converse("hey kid, change your mind?",
        "idle", "idle", "armistice_laugh");
    }
    SetAnimation("idle");
    yield return Prompt("let's go!", "not right now");
    if (PromptResult() == 0)
    {
      yield return Converse("alright, show me what you got kid",
        "idle", "idle", "armistice_laugh");
      yield return Converse("and remember, don't hold back",
        "shrug", "shrug", "armistice_laugh");
      yield return Converse("...cause I won't be",
        "idle_empty", "idle_empty", "armistice_laugh");
      base.aiActor.gameObject.Play("armistice_stop_all");
      StartBossFight(); // accept
    }
    else
    {
      yield return Converse("alright, suit yourself",
        "shrug_calm", "shrug_calm", "armistice_laugh");
      yield return Converse("i'll be here if you change your mind",
        "idle", "idle", "armistice_laugh");
      SetAnimation("idle");
    }
  }

  protected override IEnumerator DefeatedScript()
  {
    this.ShowText("good stuff, kid. come here", autoContinueTimer: 1f);
    yield return new WaitForSeconds(1f);
    // this.ShowText("take this treasure chest", autoContinueTimer: 1f);
    // yield return new WaitForSeconds(1f);
    yield break;
  }

  protected override IEnumerator PostFightScript()
  {
    GameManager.Instance.MainCameraController.OverridePosition = this.sprite.transform.localPosition;
    GameManager.Instance.MainCameraController.SetManualControl(true, true);
    yield return Converse("you really got me, kid",
      "shrug", "shrug", "armistice_laugh");
    yield return Converse("the item in that chest is all yours",
      "idle", "idle", "armistice_laugh");
    yield return Converse("as for that secret...",
        "idle", "idle", "armistice_laugh");
    yield return Converse("have you ever come across a {wj}Normal{w} gun?",
        "idle", "idle", "armistice_laugh");
    yield return Converse("i don't mean a standard or ordinary gun, but a {wj}Normal{w} gun",
        "shrug_calm", "shrug_calm", "armistice_laugh");
    yield return Converse("i've heard if you drop a {wj}Normal{w} gun inside a triangle of 3 other guns...",
        "idle", "idle", "armistice_laugh");
    yield return Converse("something cool might happen depending on the combined strength of the guns",
        "idle_glance", "idle_glance", "armistice_laugh");
    yield return Converse("if something cool DOES happen, try using a blank",
        "idle", "idle", "armistice_laugh");
    yield return Converse("and if nothing happens, maybe try dropping some stronger guns",
        "shrug", "shrug", "armistice_laugh");
    yield return Converse("what, you think i'm making all of this up?",
        "idle", "idle", "armistice_laugh");
    yield return Converse("eh, could be",
        "idle_glance", "idle_glance", "armistice_laugh");
    yield return Converse("let's do this again some time",
      "shrug", "shrug", "armistice_laugh");
    SetAnimation("idle");
  }
}
