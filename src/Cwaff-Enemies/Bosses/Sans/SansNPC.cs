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
    public bool hasPreFightDialogue = false;
    public bool hasPostFightDialogue = false;
    public bool startedFight = false;
    public bool finishedFight = false;

    private BossController bossController = null;

    protected void StartBossFight()
    {
      AIActor enemy = this.gameObject.GetComponent<AIActor>();

      if (this.bossController != null)
        this.bossController.StartBossFight(enemy);
      else
        ETGModConsole.Log($"BOSS CONTROLLER SHOULD NEVER BE NULL");
      this.gameObject.GetComponent<GenericIntroDoer>().TriggerSequence(GameManager.Instance.BestActivePlayer);

      this.startedFight = true;
    }

    public void SetBossController(BossController bc)
    {
      if (this.bossController != null)
        return;
      this.bossController = bc;
    }

    public void FinishBossFight()
      { StartCoroutine(Defeat_CR()); }

    private IEnumerator Defeat_CR()
    {
      AIActor enemy = this.gameObject.GetComponent<AIActor>();
      enemy.transform.position.GetAbsoluteRoom().RegisterInteractable(this);

      IEnumerator script = DefeatedScript();
      while(script.MoveNext())
        yield return script.Current;

      if (hasPreFightDialogue)
        enemy.transform.position.GetAbsoluteRoom().UnsealRoom();
      if (!hasPostFightDialogue)
        enemy.healthHaver.DeathAnimationComplete(null, null);

      this.finishedFight = true;
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
    protected virtual IEnumerator DefeatedScript() // called by BossController()
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
    protected override IEnumerator DefeatedScript()
    {
      this.ShowText("good stuff kid");
      yield return new WaitForSeconds(1f);
      this.ShowText("take this treasure chest", 1f);
      yield return new WaitForSeconds(1f);
      yield break;
    }
    protected override IEnumerator PostFightScript()
    {
      GameManager.Instance.MainCameraController.OverridePosition = this.sprite.transform.localPosition;
      GameManager.Instance.MainCameraController.SetManualControl(true, true);
      yield return StartCoroutine(Converse(new(){"you really got me kid"},"idle","idle"));
    }
  }
}
