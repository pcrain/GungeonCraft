using System;
using System.Collections;
using UnityEngine;

namespace GungeonAPI
{
	// Token: 0x02000010 RID: 16
	public class SimpleShrine : SimpleInteractable, IPlayerInteractable
	{
		// Token: 0x06000070 RID: 112 RVA: 0x000065CF File Offset: 0x000047CF
		private void Start()
		{
			SpriteOutlineManager.AddOutlineToSprite(base.sprite, Color.black, 1f, 0f, SpriteOutlineManager.OutlineType.NORMAL);
			this.talkPoint = base.transform.Find("talkpoint");
			this.m_isToggled = false;
		}

		// Token: 0x06000071 RID: 113 RVA: 0x000065F0 File Offset: 0x000047F0
		public void Interact(PlayerController interactor)
		{
			bool flag = TextBoxManager.HasTextBox(this.talkPoint);
			bool flag2 = !flag;
			if (flag2)
			{

				this.m_canUse = ((this.CanUse != null) ? this.CanUse(interactor, base.gameObject) : this.m_canUse);
				base.StartCoroutine(this.HandleConversation(interactor));
			}
		}

		// Token: 0x06000072 RID: 114 RVA: 0x00006671 File Offset: 0x00004871
		private IEnumerator HandleConversation(PlayerController interactor)
		{
			TextBoxManager.ShowStoneTablet(this.talkPoint.position, this.talkPoint, -1f, this.text, true, false);
			int selectedResponse = -1;
			interactor.SetInputOverride("shrineConversation");
			yield return null;
			bool flag = !this.m_canUse;
			bool flag5 = flag;
			if (flag5)
			{
				GameUIRoot.Instance.DisplayPlayerConversationOptions(interactor, null, this.declineText, string.Empty);
			}
			else
			{
				bool isToggle = this.isToggle;
				bool flag6 = isToggle;
				if (flag6)
				{
					bool isToggled = this.m_isToggled;
					bool flag7 = isToggled;
					if (flag7)
					{
						GameUIRoot.Instance.DisplayPlayerConversationOptions(interactor, null, this.declineText, string.Empty);
					}
					else
					{
						GameUIRoot.Instance.DisplayPlayerConversationOptions(interactor, null, this.acceptText, string.Empty);
					}
				}
				else
				{
					GameUIRoot.Instance.DisplayPlayerConversationOptions(interactor, null, this.acceptText, this.declineText);
				}
			}
			while (!GameUIRoot.Instance.GetPlayerConversationResponse(out selectedResponse))
			{
				yield return null;
			}
			interactor.ClearInputOverride("shrineConversation");
			TextBoxManager.ClearTextBox(this.talkPoint);
			bool flag2 = !this.m_canUse;
			bool flag8 = flag2;
			if (flag8)
			{
				yield break;
			}
			bool flag3 = selectedResponse == 0 && this.isToggle;
			bool flag9 = flag3;
			if (flag9)
			{
				Action<PlayerController, GameObject> action = this.m_isToggled ? this.OnDecline : this.OnAccept;
				bool flag10 = action != null;
				if (flag10)
				{
					action(interactor, base.gameObject);
				}
				this.m_isToggled = !this.m_isToggled;
				yield break;
			}
			bool flag4 = selectedResponse == 0;
			bool flag11 = flag4;
			if (flag11)
			{
				Action<PlayerController, GameObject> onAccept = this.OnAccept;
				bool flag12 = onAccept != null;
				if (flag12)
				{
					onAccept(interactor, base.gameObject);
				}
				onAccept = null;
			}
			else
			{
				Action<PlayerController, GameObject> onDecline = this.OnDecline;
				bool flag13 = onDecline != null;
				if (flag13)
				{
					onDecline(interactor, base.gameObject);
				}
				onDecline = null;
			}
			yield break;
		}

		// Token: 0x06000073 RID: 115 RVA: 0x00006687 File Offset: 0x00004887
		public void OnEnteredRange(PlayerController interactor)
		{
			SpriteOutlineManager.AddOutlineToSprite(base.sprite, Color.white, 1f, 0f, SpriteOutlineManager.OutlineType.NORMAL);
			base.sprite.UpdateZDepth();
		}

		// Token: 0x06000074 RID: 116 RVA: 0x000066B2 File Offset: 0x000048B2
		public void OnExitRange(PlayerController interactor)
		{
			SpriteOutlineManager.AddOutlineToSprite(base.sprite, Color.black, 1f, 0f, SpriteOutlineManager.OutlineType.NORMAL);
		}

		// Token: 0x06000075 RID: 117 RVA: 0x000066C4 File Offset: 0x000048C4
		public string GetAnimationState(PlayerController interactor, out bool shouldBeFlipped)
		{
			shouldBeFlipped = false;
			return string.Empty;
		}

		// Token: 0x06000076 RID: 118 RVA: 0x000066E0 File Offset: 0x000048E0
		public float GetDistanceToPoint(Vector2 point)
		{
			bool flag = base.sprite == null;
			bool flag2 = flag;
			float result;
			if (flag2)
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

		// Token: 0x06000077 RID: 119 RVA: 0x00006748 File Offset: 0x00004948
		public float GetOverrideMaxDistance()
		{
			return -1f;
		}
	}
}
