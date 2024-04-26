namespace CwaffingTheGungy;
using static Synergy;

public static class CwaffSynergies
{
    private static int                   _NUM_SYNERGIES = Enum.GetNames(typeof(Synergy)).Length;                            // number of synergies our mod adds
    public static List<CustomSynergyType>    _Synergies = Enumerable.Repeat<CustomSynergyType>(0, _NUM_SYNERGIES).ToList(); // list of the actual new synergies
    public static List<string>            _SynergyNames = Enumerable.Repeat<string>(null, _NUM_SYNERGIES).ToList();         // list of friendly names of synergies
    public static List<string>            _SynergyEnums = new(Enum.GetNames(typeof(Synergy)));                              // list of enum names of synergies
    public static List<int>               _SynergyIds   = Enumerable.Repeat<int>(0, _NUM_SYNERGIES).ToList();               // list of ids of synergies in the AdvancedSynergyEntry database
    public static HashSet<int>            _MasteryIds   = new();                                                            // Set of fake item ids corresponding to mastery tokens
    public static Dictionary<int,int>     _MasteryGuns  = new();                                                            // Dictionary of gun pickup ids to their mastery token ids

    internal static GameObject _MasteryVFX = null;

    public static void Init()
    {
        /* NOTE:
            - Each synergy entry below must have a comment before the line and the name of the synergy as the first quoted string in the following line
              This is to ensure that our automatic item tips generation script parses it correctly.
        */

      #region Synergies
        // Makes Drifter's Headgear dash 20% longer and reflect bullets
        NewSynergy(HYPE_YOURSELF_UP, "Hype Yourself Up", new[]{IName(DriftersHeadgear.ItemName), "hyper_light_blaster"});
      #endregion

      #region Masteries
        // Grandmaster shoots an additional black piece with every shot, no longer shoots pawns, and moves pieces twice as fast
        NewMastery<MasteryOfGrandmaster>(MASTERY_GRANDMASTER, Grandmaster.ItemName);
      #endregion

        SanityCheckAllSynergiesHaveBeenInitialized();

        _MasteryVFX = VFX.Create("mastery_character_vfx", fps: 16, loops: false, anchor: Anchor.LowerCenter, emissivePower: 100f, emissiveColour: Color.red);
    }

    private static void SanityCheckAllSynergiesHaveBeenInitialized()
    {
        for (int i = 0; i < _SynergyNames.Count(); ++i)
            if (_SynergyNames[i] == null)
                ETGModConsole.Log($"<color=#ffff88ff>WARNING: haven't initialized custom synergy {_SynergyEnums[i]}</color>");
    }

    private static void NewSynergy(Synergy synergy, string name, string[] mandatory, string[] optional = null, bool ignoreLichEyeBullets = false, int masteryId = -1)
    {
        // Register the AdvancedSynergyEntry so that the game knows about it
        RegisterSynergy(name, mandatory.ToList(), optional?.ToList(), ignoreLichEyeBullets, masteryId);
        // Get the enum index of our synergy
        int index            = (int)synergy;
        // Extend the base game's CustomSynergyType enum to make room for our new synergy
        _Synergies[index]    = ETGModCompatibility.ExtendEnum<CustomSynergyType>(C.MOD_PREFIX.ToUpper(), _SynergyEnums[index]);
        // Index the friendly name of our synergy
        _SynergyNames[index] = name;
        // Get the actual ID of our synergy entry in the AdvancedSynergyDatabase, which doesn't necessarily match the CustomSynergyType enum
        _SynergyIds[index]   = GameManager.Instance.SynergyManager.synergies.Length - 1;
    }

    public static AdvancedSynergyEntry RegisterSynergy(string name, List<string> mandatoryConsoleIDs, List<string> optionalConsoleIDs = null, bool ignoreLichEyeBullets = false, int masteryId = -1)
    {
        List<int> itemIDs    = new();
        List<int> gunIDs     = new();
        List<int> optItemIDs = new();
        List<int> optGunIDs  = new();
        foreach (var id in mandatoryConsoleIDs)
        {
            PickupObject pickup = Gungeon.Game.Items[id];
            if (pickup && pickup.GetComponent<Gun>())
                gunIDs.Add(pickup.PickupObjectId);
            else if (pickup && (pickup.GetComponent<PlayerItem>() || pickup.GetComponent<PassiveItem>()))
                itemIDs.Add(pickup.PickupObjectId);
        }
        if (masteryId >= 0)
            itemIDs.Add(masteryId);

        if (optionalConsoleIDs != null)
        {
            foreach (var id in optionalConsoleIDs)
            {
                PickupObject pickup = Gungeon.Game.Items[id];
                if (pickup && pickup.GetComponent<Gun>())
                    optGunIDs.Add(pickup.PickupObjectId);
                else if (pickup && (pickup.GetComponent<PlayerItem>() || pickup.GetComponent<PassiveItem>()))
                    optItemIDs.Add(pickup.PickupObjectId);
            }
        }

        // Add our synergy's name to the string manager so it displays properly when activated
        string nameKey = $"#{name.ToID().ToUpperInvariant()}";
        ETGMod.Databases.Strings.Synergy.Set(nameKey, name);

        AdvancedSynergyEntry entry = new AdvancedSynergyEntry()
        {
            NameKey              = nameKey,
            MandatoryItemIDs     = itemIDs,
            MandatoryGunIDs      = gunIDs,
            OptionalItemIDs      = optItemIDs,
            OptionalGunIDs       = optGunIDs,
            bonusSynergies       = new List<CustomSynergyType>(),
            statModifiers        = new List<StatModifier>(),
            IgnoreLichEyeBullets = ignoreLichEyeBullets,
        };


        int oldLength = GameManager.Instance.SynergyManager.synergies.Length;
        Array.Resize(ref GameManager.Instance.SynergyManager.synergies, oldLength + 1);
        GameManager.Instance.SynergyManager.synergies[oldLength] = entry;
        return entry;
    }

    private static void NewMastery<T>(Synergy synergy, string gunName) where T : MasteryDummyItem
    {
        if (Lazy.GetModdedItem(IName(gunName)) is not Gun gun)
            return;

        FakeItem.Create<T>();
        int tokenId = FakeItem.Acquire<T>().PickupObjectId;
        NewSynergy(
            synergy              : synergy,
            name                 : $"{gun.EncounterNameOrDisplayName} Mastery",
            mandatory            : new string[1]{IDs.InternalNames[gun.gunName]},
            masteryId            : tokenId,
            ignoreLichEyeBullets : true);
        _MasteryIds.Add(tokenId);
        _MasteryGuns[gun.PickupObjectId] = tokenId;
    }

    public static bool IsMasterable(this Gun gun)
    {
        return _MasteryGuns.ContainsKey(gun.PickupObjectId);
    }

    public static int MasteryTokenId(this Gun gun)
    {
        return _MasteryGuns[gun.PickupObjectId];
    }

    public static void AcquireMastery(this PlayerController player, Gun gun)
    {
        if (gun && gun.IsMasterable())
        {
            player.AcquireFakeItem(_MasteryGuns[gun.PickupObjectId]);
            player.gameObject.Play("mastery_ritual_complete_sound");
        }
        else if (gun)
            Lazy.DebugWarn($"Trying to acquire mastery for {gun.EncounterNameOrDisplayName}, which doesn't have a mastery!");
        else
            Lazy.DebugWarn($"Trying to acquire mastery for null gun!");
    }

    /// <summary>Adds some code after calling RebuildSynergies() to check if any of the new synergies are masteries, and if so, plays the appropriate VFX / skips the remaining notifications</summary>
    [HarmonyPatch(typeof(PlayerStats), nameof(PlayerStats.RecalculateSynergies))]
    private class RecalculateMasteriesPatch
    {
        [HarmonyILManipulator]
        private static void RecalculateMasteriesIL(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt<AdvancedSynergyDatabase>("RebuildSynergies")))
                return;
            cursor.Emit(OpCodes.Ldarg_0); // PlayerStats instance
            cursor.Emit(OpCodes.Ldarg_1); // PlayerController owner
            cursor.Emit(OpCodes.Call, typeof(RecalculateMasteriesPatch).GetMethod("CheckForNewMasteries", BindingFlags.Static | BindingFlags.NonPublic));
            ILLabel continueAsNormalLabel = cursor.DefineLabel();
            cursor.Emit(OpCodes.Brfalse, continueAsNormalLabel);
            cursor.Emit(OpCodes.Ret);
            cursor.MarkLabel(continueAsNormalLabel);
        }

        private static bool CheckForNewMasteries(PlayerStats stats, PlayerController owner)
        {
            // if (!owner.AcceptingAnyInput)  //NOTE: this is a vanilla bug with synergies, so disabling for now
            //     return false; // probably reloading a save or something, don't play any vfx

            for (int i = 0; i < owner.ActiveExtraSynergies.Count; i++)
            {
                int synId = owner.ActiveExtraSynergies[i];
                if (!(!GameManager.Instance.SynergyManager.synergies[synId].SuppressVFX && GameManager.Instance.SynergyManager.synergies[synId].ActivationStatus != SynergyEntry.SynergyActivation.INACTIVE && !stats.PreviouslyActiveSynergies.Contains(synId)))
                    continue;

                AdvancedSynergyEntry advancedSynergyEntry = GameManager.Instance.SynergyManager.synergies[synId];
                if (advancedSynergyEntry.MandatoryItemIDs == null || advancedSynergyEntry.MandatoryItemIDs.Count == 0 || !_MasteryIds.Contains(advancedSynergyEntry.MandatoryItemIDs[0]))
                    continue; // definitely not a mastery

                owner.gameObject.Play("the_sound_of_mastering_a_weapon");
                owner.PlayEffectOnActor(_MasteryVFX, new Vector3(0f, 0.5f, 0f));
                // if (advancedSynergyEntry.ActivationStatus != SynergyEntry.SynergyActivation.INACTIVE && !string.IsNullOrEmpty(advancedSynergyEntry.NameKey))
                //     GameUIRoot.Instance.notificationController.AttemptSynergyAttachment(advancedSynergyEntry);
                GameStatsManager.Instance.HandleEncounteredSynergy(synId);

                stats.PreviouslyActiveSynergies.Clear();
                stats.PreviouslyActiveSynergies.AddRange(owner.ActiveExtraSynergies);
                return true; // skip remainder of original function
            }

            return false; // continue with remainder of original function
        }
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

public class MasteryDummyItem : FakeItem
{

}

public enum Synergy {
    // Synergies
    HYPE_YOURSELF_UP,

    // Masteries
    MASTERY_GRANDMASTER,
};
