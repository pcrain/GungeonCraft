namespace SaveAPI;

public enum CustomTrackedStats
{
    //Add your custom tracked stats here
    //You can remove any stats here
    ENCOUNTERED_ARMI,
    DEFEATED_ARMI,
    DIED_TO_ARMI,
    ENCOUNTERED_SKEL,
    DEFEATED_SKEL,
    DIED_TO_SKEL,
}

internal static class CustomTrackedStatsExtensions
{
    public static void Increment(this CustomTrackedStats stat, float val = 1f) => AdvancedGameStatsManager.Instance.RegisterStatChange(stat, val);
    public static void Set(this CustomTrackedStats stat, float val) => AdvancedGameStatsManager.Instance.SetStat(stat, val);
    public static void Reset(this CustomTrackedStats stat) => AdvancedGameStatsManager.Instance.SetStat(stat, 0f);
    public static float Get(this CustomTrackedStats stat) => AdvancedGameStatsManager.Instance.GetPlayerStatValue(stat);
    public static float GetForChar(this CustomTrackedStats stat) => AdvancedGameStatsManager.Instance.GetCharacterStatValue(stat);
}
