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

public class SansNPC : BossNPC
{
  private bool _talkedOnce = false;

  protected override IEnumerator PreFightScript()
  {
    GameManager.Instance.MainCameraController.OverridePosition = this.sprite.transform.localPosition;
    GameManager.Instance.MainCameraController.SetManualControl(true, true);
    if (!this._talkedOnce)
    {
      yield return Converse("hey kid, how'd you manage to find me?",
        "idle", "idle", "sans_laugh");
      yield return Converse("this place ain't exactly easy to get to",
        "idle_glance", "idle_glance", "sans_laugh");
      yield return Converse("that bello guy tip you off, or you just get here by luck?",
        "idle", "idle", "sans_laugh");
      yield return Converse("...eh, doesn't matter",
        "shrug_glance", "shrug_glance", "sans_laugh");
      yield return Converse("seems like you have some cool toys on you",
        "idle", "idle", "sans_laugh");
      yield return Converse("since you're here, wanna try my little test?",
        "idle_glance", "idle_glance", "sans_laugh");
      yield return Converse("if you can manage to land a few hits on me, i'll give you something cool",
        "shrug", "shrug", "sans_laugh");
      yield return Converse("i don't have any guns on me, but i'll try to make it interesting",
        "idle", "idle", "sans_laugh");
      yield return Converse("and don't feel the need to hold back, i can handle myself",
        "shrug", "shrug", "sans_laugh");
      yield return Converse("whaddaya say, kid?",
        "idle_glance", "idle_glance", "sans_laugh");
      this._talkedOnce = true;
    }
    else
    {
      yield return Converse("hey kid, change your mind?",
        "idle", "idle", "sans_laugh");
    }
    SetAnimation("idle");
    yield return Prompt("let's go!", "not right now");
    if (PromptResult() == 0)
    {
      yield return Converse("alright, show me what you got kid",
        "idle", "idle", "sans_laugh");
      yield return Converse("and remember, don't hold back",
        "shrug", "shrug", "sans_laugh");
      yield return Converse("...cause I won't be",
        "idle_empty", "idle_empty", "sans_laugh");
      AkSoundEngine.PostEvent("sans_stop_all", base.aiActor.gameObject);
      StartBossFight(); // accept
    }
    else
    {
      this.ShowText("alright, suit yourself!", autoContinueTimer: 1f);
      yield return Converse("alright, suit yourself",
        "shrug_calm", "shrug_calm", "sans_laugh");
      yield return Converse("i'll be here if you change your mind",
        "idle", "idle", "sans_laugh");
      SetAnimation("idle");
    }
  }

  protected override IEnumerator DefeatedScript()
  {
    this.ShowText("good stuff, kid", autoContinueTimer: 1f);
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
      "shrug", "shrug", "sans_laugh");
    yield return Converse("the item in that chest is all yours",
      "idle", "idle", "sans_laugh");
    yield return Converse("let's do this again some time",
      "shrug", "shrug", "sans_laugh");
    SetAnimation("idle");
  }
}
