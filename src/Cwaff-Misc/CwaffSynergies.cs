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
        /* NOTE:
            - Each synergy entry below must have a comment before the line and the name of the synergy as the first quoted string in the following line
              This is to ensure that our automatic item tips generation script parses it correctly.
        */

        // Makes Drifter's Headgear dash 20% longer and reflect bullets
        NewSynergy(HYPE_YOURSELF_UP, "Hype Yourself Up", new[]{IName(DriftersHeadgear.ItemName), "hyper_light_blaster"});

        SanityCheckAllSynergiesHaveBeenInitialized();
    }

    private static void SanityCheckAllSynergiesHaveBeenInitialized()
    {
        for (int i = 0; i < _SynergyNames.Count(); ++i)
            if (_SynergyNames[i] == null)
                ETGModConsole.Log($"<color=#ffff88ff>WARNING: haven't initialized custom synergy {_SynergyEnums[i]}</color>");
    }

    private static void NewSynergy(Synergy synergy, string name, string[] mandatory, string[] optional = null)
    {
        // Register the AdvancedSynergyEntry so that the game knows about it
        CustomSynergies.Add(name, mandatory.ToList(), optional?.ToList());
        // Get the enum index of our synergy
        int index            = (int)synergy;
        // Extend the base game's CustomSynergyType enum to make room for our new synergy
        _Synergies[index]    = ETGModCompatibility.ExtendEnum<CustomSynergyType>(C.MOD_PREFIX.ToUpper(), _SynergyEnums[index]);
        // Index the friendly name of our synergy
        _SynergyNames[index] = name;
        // Get the actual ID of our synergy entry in the AdvancedSynergyDatabase, which doesn't necessarily match the CustomSynergyType enum
        _SynergyIds[index]   = GameManager.Instance.SynergyManager.synergies.Length - 1;
    }

    private static string IName(string itemName)
    {
        return IDs.InternalNames[itemName];
    }

    public static string SynergyName(this Synergy synergy)
    {
        return _SynergyNames[(int)synergy];
    }

    public static bool PlayerHasActiveSynergy(this PlayerController player, Synergy synergy)
    {
        return player.ActiveExtraSynergies.Contains((int)_SynergyIds[(int)synergy]);
    }
}

public enum Synergy {
    HYPE_YOURSELF_UP,
};
