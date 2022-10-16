using System;
using System.Collections.Generic;
using UnityEngine;

namespace GungeonAPI
{
	// Token: 0x02000019 RID: 25
	public abstract class NPCInteractable : BraveBehaviour
	{
		// Token: 0x04000040 RID: 64
		public Action<PlayerController, GameObject> OnAccept;

		// Token: 0x04000041 RID: 65
		public Action<PlayerController, GameObject> OnDecline;

		// Token: 0x04000042 RID: 66
		public List<string> conversation;

		// Token: 0x04000043 RID: 67
		public List<string> conversation2;

		// Token: 0x04000044 RID: 68
		public Func<PlayerController, GameObject, bool> CanUse;

		// Token: 0x04000045 RID: 69
		public Transform talkPoint;

		// Token: 0x04000046 RID: 70
		public string text;

		// Token: 0x04000047 RID: 71
		public string acceptText;

		// Token: 0x04000048 RID: 72
		public string acceptText2;

		// Token: 0x04000049 RID: 73
		public string declineText;

		// Token: 0x0400004A RID: 74
		public string declineText2;

		// Token: 0x0400004B RID: 75
		public bool isToggle;

		// Token: 0x0400004C RID: 76
		protected bool m_isToggled;

		// Token: 0x0400004D RID: 77
		protected bool m_canUse = true;
	}
}