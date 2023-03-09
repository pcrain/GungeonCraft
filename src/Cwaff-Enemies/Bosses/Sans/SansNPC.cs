using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using UnityEngine;

namespace CwaffingTheGungy
{
  // Class for adding a custom dialogue with a boss leading up to a boss fight
  public class BossNPC : FancyNPC
  {
    private BossController bossController = null;
    private bool startedFight = false;

    protected void StartBossFight()
    {
      AIActor enemy = this.gameObject.GetComponent<AIActor>();

      this.gameObject.transform.position.GetAbsoluteRoom().DeregisterInteractable(this);
      enemy.aiAnimator.EndAnimation();  // make sure our base idle animation plays after our preIntro animation finishes
      this.gameObject.GetComponent<GenericIntroDoer>().TriggerSequence(GameManager.Instance.BestActivePlayer);
      if (this.bossController != null)
        this.bossController.StartBossFight(enemy);
      enemy.aiAnimator.StartCoroutine(RemoveOutlines(enemy)); // hack because trying to remove outlines instantaneously doesn't work for some reason

      this.startedFight = true;
    }

    private IEnumerator RemoveOutlines(AIActor enemy)
    {
      yield return new WaitForSecondsRealtime(1.0f/60.0f);
      SpriteOutlineManager.RemoveOutlineFromSprite(enemy.sprite);
    }

    public void SetBossController(BossController bc)
    {
      if (this.bossController != null)
        return;
      this.bossController = bc;
    }

    protected override IEnumerator NPCTalkingScript()
    {
      IEnumerator script = (startedFight ? PostFightScript() : PreFightScript());
      while(script.MoveNext())
        yield return script.Current;
    }

    protected virtual IEnumerator PreFightScript()
    {
      GameManager.Instance.MainCameraController.OverridePosition = this.sprite.transform.localPosition;
      GameManager.Instance.MainCameraController.SetManualControl(true, true);
      yield return StartCoroutine(Prompt("fight this guy", "don't fight this guy"));
      if (PromptResult() == 0) // accept
        StartBossFight();
    }
    public virtual IEnumerator DefeatedScript() // called by BossController()
      { yield break; }
    protected virtual IEnumerator PostFightScript()
      { yield break; }
  }

  public class SansNPC : BossNPC
  {
    protected override IEnumerator PreFightScript()
    {
      GameManager.Instance.MainCameraController.OverridePosition = this.sprite.transform.localPosition;
      GameManager.Instance.MainCameraController.SetManualControl(true, true);
      yield return StartCoroutine(Converse(new(){"hey pal", "wanna fight?"},"idle_cloak","idle_cloak"));
      yield return StartCoroutine(Prompt("sure", "not really"));
      if (PromptResult() == 0)  StartBossFight(); // accept
      else                      this.ShowText("Alright, suit yourself!", 1f);
    }
    public override IEnumerator DefeatedScript()
    {
      this.ShowText("good stuff kid", 1f);
      yield return new WaitForSeconds(1f);
      this.ShowText("take this treasure chest", 1f);
      yield return new WaitForSeconds(1f);
    }
    protected override IEnumerator PostFightScript()
    {
      GameManager.Instance.MainCameraController.OverridePosition = this.sprite.transform.localPosition;
      GameManager.Instance.MainCameraController.SetManualControl(true, true);
      yield return StartCoroutine(Converse(new(){"you really got me kid"},"idle","idle"));
    }
  }
}
