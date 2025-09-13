namespace SaveAPI;

public enum CustomDungeonFlags
{
    //Add your custom flags here
    //You can remove any flags here (except NONE, don't remove it)
    NONE,
    HAS_DEFEATED_ARMI,
    HAS_DEFEATED_SKEL,
}

internal static class CustomDungeonFlagsExtensions
{
    public static void Set(this CustomDungeonFlags flag) => AdvancedGameStatsManager.Instance.SetFlag(flag, true);
    public static void Unset(this CustomDungeonFlags flag) => AdvancedGameStatsManager.Instance.SetFlag(flag, false);
    public static bool Get(this CustomDungeonFlags flag) => AdvancedGameStatsManager.Instance.GetFlag(flag);
}
