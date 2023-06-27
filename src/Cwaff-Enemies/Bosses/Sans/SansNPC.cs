using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using UnityEngine;

namespace CwaffingTheGungy
{
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
