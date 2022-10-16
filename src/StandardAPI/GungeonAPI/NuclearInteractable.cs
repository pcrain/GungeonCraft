using System;
using System.Collections;
using UnityEngine;
using GungeonAPI;


namespace NevernamedsItems
{
	// Token: 0x0200000B RID: 11
	public class JammomasterInteractible : SimpleInteractable, IPlayerInteractable
	{
        private void Start()
		{
			this.talkPoint = base.transform.Find("talkpoint");
			this.m_isToggled = false;
			SpriteOutlineManager.AddOutlineToSprite(base.sprite, Color.black);
			this.m_canUse = true;
			base.spriteAnimator.Play("idle");
		}

		// Token: 0x06000060 RID: 96 RVA: 0x000054A4 File Offset: 0x000036A4
		public void Interact(PlayerController interactor)
		{
			bool flag = TextBoxManager.HasTextBox(this.talkPoint);
			if (!flag)
			{
				this.m_canUse = ((this.CanUse != null) ? this.CanUse(interactor, base.gameObject) : this.m_canUse);
				bool flag2 = !this.m_canUse;
                if (flag2)
                {
                    base.spriteAnimator.PlayForDuration("talk", 2f, "idle", false);
                    TextBoxManager.ShowTextBox(this.talkPoint.position, this.talkPoint, 2f, "No... not this time.", interactor.characterAudioSpeechTag, false, TextBoxManager.BoxSlideOrientation.NO_ADJUSTMENT, false, false);
                }                							
				else
				{
					base.StartCoroutine(this.HandleConversation(interactor));
				}
			}
		}
        private IEnumerator HandleConversation(PlayerController interactor)
        {
            //ETGModConsole.Log("HandleConversation Started");
            SpriteOutlineManager.AddOutlineToSprite(base.sprite, Color.black);
            base.spriteAnimator.PlayForDuration("talk_start", 1, "talk");
            interactor.SetInputOverride("npcConversation");
            Pixelator.Instance.LerpToLetterbox(0.35f, 0.25f);
            yield return null;
            var conversationToUse = AllJammedState.AllJammedActive ? conversation2 : conversation;
            int conversationIndex = 0;
            //ETGModConsole.Log("We made it to the while loop");
            while (conversationIndex < conversationToUse.Count - 1)
            {
                Tools.Print($"Index: {conversationIndex}");
                TextBoxManager.ClearTextBox(this.talkPoint);
                TextBoxManager.ShowTextBox(this.talkPoint.position, this.talkPoint, -1f, conversationToUse[conversationIndex], interactor.characterAudioSpeechTag, instant: false, showContinueText: true);
                float timer = 0;
                while (!BraveInput.GetInstanceForPlayer(interactor.PlayerIDX).ActiveActions.GetActionFromType(GungeonActions.GungeonActionType.Interact).WasPressed || timer < 0.4f)
                {
                    timer += BraveTime.DeltaTime;
                    yield return null;
                }
                conversationIndex++;
            }
            //ETGModConsole.Log("We made it through the while loop");
            m_allowMeToIntroduceMyself = false;
            TextBoxManager.ShowTextBox(this.talkPoint.position, this.talkPoint, -1f, conversationToUse[conversationToUse.Count - 1], interactor.characterAudioSpeechTag, instant: false, showContinueText: true);

            var acceptanceTextToUse = AllJammedState.AllJammedActive ? acceptText2 : acceptText;
            var declineTextToUse = AllJammedState.AllJammedActive ? declineText2 : declineText;
            GameUIRoot.Instance.DisplayPlayerConversationOptions(interactor, null, acceptanceTextToUse, declineTextToUse);
            int selectedResponse = -1;
            while (!GameUIRoot.Instance.GetPlayerConversationResponse(out selectedResponse))
                yield return null;
            //ETGModConsole.Log("We made it to the if statement");
            if (selectedResponse == 0)
            {
                TextBoxManager.ClearTextBox(this.talkPoint);
                base.spriteAnimator.PlayForDuration("do_effect", -1, "talk");
                OnAccept?.Invoke(interactor, this.gameObject);
                base.spriteAnimator.Play("talk");
                TextBoxManager.ShowTextBox(this.talkPoint.position, this.talkPoint, 1f, "It is done...", interactor.characterAudioSpeechTag, instant: false);
                yield return new WaitForSeconds(1f);
            }
            else
            {
                OnDecline?.Invoke(interactor, this.gameObject);
                TextBoxManager.ClearTextBox(this.talkPoint);
            }
            //ETGModConsole.Log("We made it through the if statement");

            // Free player and run OnAccept/OnDecline actions
            interactor.ClearInputOverride("npcConversation");
            Pixelator.Instance.LerpToLetterbox(1, 0.25f);
            base.spriteAnimator.Play("idle");
            //ETGModConsole.Log("We made it");
        }

        // Token: 0x06000062 RID: 98 RVA: 0x0000556A File Offset: 0x0000376A
        public void OnEnteredRange(PlayerController interactor)
		{
			SpriteOutlineManager.AddOutlineToSprite(base.sprite, Color.white, 1f, 0f, SpriteOutlineManager.OutlineType.NORMAL);
			base.sprite.UpdateZDepth();
		}

		// Token: 0x06000063 RID: 99 RVA: 0x00005595 File Offset: 0x00003795
		public void OnExitRange(PlayerController interactor)
		{
			SpriteOutlineManager.AddOutlineToSprite(base.sprite, Color.black, 1f, 0f, SpriteOutlineManager.OutlineType.NORMAL);
		}

		// Token: 0x06000064 RID: 100 RVA: 0x000055B4 File Offset: 0x000037B4
		public string GetAnimationState(PlayerController interactor, out bool shouldBeFlipped)
		{
			shouldBeFlipped = false;
			return string.Empty;
		}

		// Token: 0x06000065 RID: 101 RVA: 0x000055D0 File Offset: 0x000037D0
		public float GetDistanceToPoint(Vector2 point)
		{
			bool flag = base.sprite == null;
			float result;
			if (flag)
			{
				result = 100f;
			}
			else
			{
				Vector3 v = BraveMathCollege.ClosestPointOnRectangle(point, base.specRigidbody.UnitBottomLeft, base.specRigidbody.UnitDimensions);
				result = Vector2.Distance(point, v) / 1.5f;
			}
			return result;
		}

		// Token: 0x06000066 RID: 102 RVA: 0x00005630 File Offset: 0x00003830
		public float GetOverrideMaxDistance()
		{
			return -1f;
		}

		// Token: 0x0400002D RID: 45
		private bool m_allowMeToIntroduceMyself = true;
	}
}
