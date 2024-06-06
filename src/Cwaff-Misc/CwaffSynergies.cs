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
        // Gives Drifter's Headgear a 20% longer dash that reflects bullets.
        NewSynergy(HYPE_YOURSELF_UP, "Hype Yourself Up", new[]{IName(DriftersHeadgear.ItemName), "hyper_light_blaster"});
        // Alligator's energy output is tripled while standing on carpet.
        NewSynergy(ELECTRIC_SLIDE, "Electric Slide", new[]{IName(Alligator.ItemName), IName(ComfySlippers.ItemName)});
        // Chekhov's Gun's ammo is fully restored before each boss fight.
        NewSynergy(DEUS_EX_MACHINA, "Deus Ex Machina", new[]{IName(ChekhovsGun.ItemName), IName(PlotArmor.ItemName)});
        // Suncaster replenishes ammo twice as quickly.
        NewSynergy(SOLAR_FLAIR, "Solar Flair", new[]{IName(Suncaster.ItemName), "sunlight_javelin"});
        // 4D Bullets no longer have a damage penalty when firing bullets through walls
        NewSynergy(PROJECTING_MUCH, "Projecting, Much?", new[]{IName(FourDBullets.ItemName), IName(AstralProjector.ItemName)});
        // Aimu Hakurei's charge is instantly set to MAX upon getting hit
        NewSynergy(LOTUS_LAND_STORY, "Lotus Land Story", new[]{IName(AimuHakurei.ItemName), "laser_lotus"});
        // Enemies stunned by Gorgun's Eye remain stunned for a second after looking away
        NewSynergy(BLANK_STARE, "Blank Stare", new[]{IName(GorgunEye.ItemName), IName(BlankChecks.ItemName)});
        // Enemies have a 75% chance of having their gun replaced by a Bubble Blaster instead of a 50% chance
        NewSynergy(DUBBLE_BUBBLE, "Dubble Bubble", new[]{IName(BubbleWand.ItemName), "bubble_blaster"});
        // Enemies drop twice as many souls when killed with Uppskeruvel
        NewSynergy(SOUL_SEARCHING, "Soul Searching", new[]{IName(Uppskeruvel.ItemName), "gun_soul"});
        // Vacuuming debris occasionally generates casing (up to 20 per floor)
        NewSynergy(CLEANUP_CREW, "Cleanup Crew", new[]{IName(VacuumCleaner.ItemName), IName(CustodiansBadge.ItemName)});
        // Vacuum's chance to restore ammo is increased to 4%
        NewSynergy(SCAVENGEST, "Scavengest", new[]{IName(VacuumCleaner.ItemName), IName(ScavengingArms.ItemName)});
      #endregion

      #region Masteries
        // Grandmaster shoots an additional black piece with every shot, no longer shoots pawns, and moves pieces twice as fast.
        NewMastery<MasteryOfGrandmaster>(MASTERY_GRANDMASTER, Grandmaster.ItemName);
        // Chekhov's Gun has no minimum fire time and restores all unfired shots at the end of the room.
        NewMastery<MasteryOfChekhovsGun>(MASTERY_CHEKHOVS_GUN, ChekhovsGun.ItemName);
        // Pincushion's pins phase through minor breakables and the gun itself no longer increases in spread.
        NewMastery<MasteryOfPincushion>(MASTERY_PINCUSHION, Pincushion.ItemName);
        // Enemies and projectiles are completely halted while souls are active.
        NewMastery<MasteryOfPlatinumStar>(MASTERY_PLATINUM_STAR, PlatinumStar.ItemName);
        // Movement penalty is removed.
        NewMastery<MasteryOfNatascha>(MASTERY_NATASCHA, Natascha.ItemName);
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
        //NOTE: next line can't begin with NewSynergy or itemtips script gets messed up
        /**/ NewSynergy(
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


// Dummy classes for masteries
public class MasteryDummyItem : FakeItem { }
internal class MasteryOfGrandmaster : MasteryDummyItem {}
internal class MasteryOfChekhovsGun : MasteryDummyItem {}
internal class MasteryOfPincushion : MasteryDummyItem {}
internal class MasteryOfPlatinumStar : MasteryDummyItem {}
internal class MasteryOfNatascha : MasteryDummyItem {}

public enum Synergy {
    // Synergies
    HYPE_YOURSELF_UP,
    ELECTRIC_SLIDE,
    DEUS_EX_MACHINA,
    SOLAR_FLAIR,
    PROJECTING_MUCH,
    LOTUS_LAND_STORY,
    BLANK_STARE,
    DUBBLE_BUBBLE,
    SOUL_SEARCHING,
    CLEANUP_CREW,
    SCAVENGEST,

    // Masteries
    MASTERY_GRANDMASTER,
    MASTERY_CHEKHOVS_GUN,
    MASTERY_PINCUSHION,
    MASTERY_PLATINUM_STAR,
    MASTERY_NATASCHA,
};
