namespace CwaffingTheGungy;
using static Synergy;

public static class CwaffSynergies
{
    private static int                   _NUM_SYNERGIES = Enum.GetNames(typeof(Synergy)).Length;                            // number of synergies our mod adds
    public static List<CustomSynergyType>    _Synergies = Enumerable.Repeat<CustomSynergyType>(0, _NUM_SYNERGIES).ToList(); // list of the actual new synergies
    public static List<string>            _SynergyNames = Enumerable.Repeat<string>(null, _NUM_SYNERGIES).ToList();         // list of friendly names of synergies
    public static List<string>            _SynergyEnums = new(Enum.GetNames(typeof(Synergy)));                              // list of enum names of synergies
    public static List<int>               _SynergyIds   = Enumerable.Repeat<int>(0, _NUM_SYNERGIES).ToList();               // list of ids of synergies in the AdvancedSynergyEntry database

    public static void Init()
    {
        // Makes Hyper Light Dasher 20% longer and reflect bullets
        NewSynergy(HYPE_YOURSELF_UP, "Hype Yourself Up", new[]{IName(DriftersHeadgear.ItemName), "hyper_light_blaster"});
    }

    private static void NewSynergy(Synergy synergy, string name, string[] mandatory, string[] optional = null)
    {
        // ETGModConsole.Log($"number of total synergies was {GameManager.Instance.SynergyManager.synergies.Length}");
        int index = (int)synergy;
        // ETGModConsole.Log($"adding synergy {index}");
        _Synergies[index]    = ETGModCompatibility.ExtendEnum<CustomSynergyType>(C.MOD_PREFIX.ToUpper(), _SynergyEnums[index]);
        // ETGModConsole.Log($"got synergy with id {(int)_Synergies[index]} == {_Synergies[index]}");
        _SynergyNames[index] = name;
        CustomSynergies.Add(name, mandatory.ToList(), optional?.ToList());
        _SynergyIds[index] = GameManager.Instance.SynergyManager.synergies.Length - 1;
        // ETGModConsole.Log($"number of total synergies is now {GameManager.Instance.SynergyManager.synergies.Length}");
        // for (int i = 0; i < GameManager.Instance.SynergyManager.synergies.Length; ++i)
        //     ETGModConsole.Log($"{i} -> {GameManager.Instance.SynergyManager.synergies[i].NameKey}");
    }

    private static string IName(string itemName)
    {
        return IDs.InternalNames[itemName];
    }

    public static string SynergyName(this Synergy synergy)
    {
        return _SynergyNames[(int)synergy];
    }

    private static bool first = true;
    public static bool PlayerHasActiveSynergy(this PlayerController player, Synergy synergy)
    {
        // if (first)
        // {
        //     // first = false;
        //     // foreach (string s in Enum.GetNames(typeof(CustomSynergyType)))
        //     //     ETGModConsole.Log($"{s}");
        //     ETGModConsole.Log($"printing all active synergies");
        //     foreach (CustomSynergyType s in player.ActiveExtraSynergies)
        //         ETGModConsole.Log($" {s}");
        //     foreach (CustomSynergyType s in player.stats.ActiveCustomSynergies)
        //         ETGModConsole.Log($" {s}");
        //     foreach (CustomSynergyType s in player.CustomEventSynergies)
        //         ETGModConsole.Log($" {s}");
        // }
        // ETGModConsole.Log($"checking if we have {(int)_Synergies[(int)synergy]}");

        return player.ActiveExtraSynergies.Contains((int)_SynergyIds[(int)synergy]);

        // ETGModConsole.Log($"checking for synergy with id {_Synergies[(int)synergy]}");
        // ETGModConsole.Log($"  found {player.CountActiveBonusSynergies(_Synergies[(int)synergy])}");
        // return player.HasActiveBonusSynergy(_Synergies[(int)synergy]);

        // return player.PlayerHasActiveSynergy(synergy.SynergyName());
    }
}

public enum Synergy {
    HYPE_YOURSELF_UP,
};
