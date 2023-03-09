using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using UnityEngine;

namespace CwaffingTheGungy
{
    public class BossNPC : FancyNPC
    {
        private BossController bossController = null;

        protected void StartBossFight()
        {
            AIActor enemy = this.gameObject.GetComponent<AIActor>();

            this.gameObject.transform.position.GetAbsoluteRoom().DeregisterInteractable(this);
            enemy.aiAnimator.EndAnimation();  // make sure our base idle animation plays after our preIntro animation finishes
            this.gameObject.GetComponent<GenericIntroDoer>().TriggerSequence(GameManager.Instance.BestActivePlayer);
            if (this.bossController != null)
                this.bossController.StartBossFight(enemy);
            enemy.aiAnimator.StartCoroutine(RemoveOutlines(enemy)); // hack because trying to remove outlines instantaneously doesn't work for some reason
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
    }

    public class SansNPC : BossNPC
    {
        protected override IEnumerator NPCTalkingScript()
        {
            List<string> conversation = new List<string> {
                "Hey buddy, what's good!",
                "Listen, I've got a *real* nice item for you.",
                };

            yield return StartCoroutine(Converse(conversation,"idle_cloak","idle_cloak"));
            StartBossFight();
            yield break;
        }
    }
}
