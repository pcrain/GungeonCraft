using System;
using System.Collections.Generic;
using UnityEngine;

namespace GungeonAPI
{
	// Token: 0x0200000F RID: 15
	public abstract class SimpleInteractable : BraveBehaviour
	{
		// Token: 0x0400002F RID: 47
		public Action<PlayerController, GameObject> OnAccept;

		// Token: 0x04000030 RID: 48
		public Action<PlayerController, GameObject> OnDecline;

		// Token: 0x04000031 RID: 49
		public List<string> conversation;

		// Token: 0x04000032 RID: 50
		public List<string> conversation2;

		public List<string> conversation3;

		public List<string> conversation4;

		// Token: 0x04000033 RID: 51
		public Func<PlayerController, GameObject, bool> CanUse;

		// Token: 0x04000034 RID: 52
		public Transform talkPoint;

		// Token: 0x04000035 RID: 53
		public string text;

		// Token: 0x04000036 RID: 54
		public string acceptText;

		// Token: 0x04000037 RID: 55
		public string acceptText2;

		// Token: 0x04000038 RID: 56
		public string declineText;

		// Token: 0x04000039 RID: 57
		public string declineText2;

		// Token: 0x0400003A RID: 58
		public bool isToggle;

		// Token: 0x0400003B RID: 59
		protected bool m_isToggled;

		// Token: 0x0400003C RID: 60
		protected bool m_canUse = true;
	}
}