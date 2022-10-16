using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SaveAPI
{
	[Serializable]
	public class CustomHuntQuest : MonsterHuntQuest
	{
		public new bool IsQuestComplete()
		{
			if (this.CustomQuestFlag != CustomDungeonFlags.NONE && AdvancedGameStatsManager.Instance.GetFlag(this.CustomQuestFlag))
            {
				return true;
            }
			return GameStatsManager.Instance.GetFlag(this.QuestFlag);
		}

		public bool IsEnemyValid(AIActor enemy, MonsterHuntProgress progress)
        {
			if(this.ValidTargetCheck != null && !this.ValidTargetCheck(enemy, progress))
            {
				return false;
            }
			return SaveTools.IsEnemyStateValid(enemy, this.RequiredEnemyState);
        }

		public void Complete()
        {
			if(this.QuestFlag != GungeonFlags.NONE)
            {
				GameStatsManager.Instance.SetFlag(this.QuestFlag, true);
			}
			if(this.CustomQuestFlag != CustomDungeonFlags.NONE)
            {
				AdvancedGameStatsManager.Instance.SetFlag(this.CustomQuestFlag, true);
			}
        }

		public new void UnlockRewards()
		{
			for (int i = 0; i < this.FlagsToSetUponReward.Count; i++)
			{
				GameStatsManager.Instance.SetFlag(this.FlagsToSetUponReward[i], true);
			}
			for (int i = 0; i < this.CustomFlagsToSetUponReward.Count; i++)
			{
				AdvancedGameStatsManager.Instance.SetFlag(this.CustomFlagsToSetUponReward[i], true);
			}
		}

		[LongEnum]
		[SerializeField]
		public CustomDungeonFlags CustomQuestFlag;
		[LongEnum]
		[SerializeField]
		public List<CustomDungeonFlags> CustomFlagsToSetUponReward;
		[SerializeField]
		public Func<AIActor, MonsterHuntProgress, bool> ValidTargetCheck;
		[SerializeField]
		public JammedEnemyState RequiredEnemyState;
	}
}
