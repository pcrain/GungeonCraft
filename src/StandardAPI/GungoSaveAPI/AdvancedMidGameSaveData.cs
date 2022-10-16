using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FullSerializer;

namespace SaveAPI
{
	/// <summary>
	/// Class to store prior session stats from <see cref="AdvancedGameStatsManager"/> when the game is saved
	/// </summary>
	public class AdvancedMidGameSaveData
	{
		public AdvancedMidGameSaveData(string midGameSaveGuid)
		{
			this.midGameSaveGuid = midGameSaveGuid;
			this.PriorSessionStats = AdvancedGameStatsManager.Instance.MoveSessionStatsToSavedSessionStats();
		}

		/// <summary>
		/// Returns <see langword="true"/> if this <see cref="AdvancedMidGameSaveData"/> isn't invalidated
		/// </summary>
		/// <returns><see langword="true"/> if this <see cref="AdvancedMidGameSaveData"/> isn't invalidated</returns>
		public bool IsValid()
		{
			return !this.invalidated;
		}

		/// <summary>
		/// Invalidates this <see cref="AdvancedMidGameSaveData"/>
		/// </summary>
		public void Invalidate()
		{
			this.invalidated = true;
		}

		/// <summary>
		/// Revalidates this <see cref="AdvancedMidGameSaveData"/>
		/// </summary>
		public void Revalidate()
		{
			this.invalidated = false;
		}

		/// <summary>
		/// Adds saved session stats from this <see cref="AdvancedMidGameSaveData"/> to <see cref="AdvancedGameStatsManager"/>'s saved session stats
		/// </summary>
		public void LoadDataFromMidGameSave()
		{
			AdvancedGameStatsManager.Instance.AssignMidGameSavedSessionStats(this.PriorSessionStats);
		}

		/// <summary>
		/// Stored session stats from the saved session
		/// </summary>
		[fsProperty]
		public AdvancedGameStats PriorSessionStats;
		/// <summary>
		/// This <see cref="AdvancedMidGameSaveData"/>'s guid
		/// </summary>
		[fsProperty]
		public string midGameSaveGuid;
		/// <summary>
		/// Is this <see cref="AdvancedMidGameSaveData"/> invalidated?
		/// </summary>
		[fsProperty]
		public bool invalidated;
	}
}
