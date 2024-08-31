namespace CwaffingTheGungy;

using static Synergy;
using static PlayerStats;
using static StatModifier;

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

    //NOTE: needs to be done early so guns and items can reference synergy ids properly
    public static void InitEnums()
    {
        // Extend the base game's CustomSynergyType enum to make room for our new synergy
        for (int i = 0; i < _SynergyNames.Count; ++i)
            _Synergies[i] = ETGModCompatibility.ExtendEnum<CustomSynergyType>(C.MOD_PREFIX.ToUpper(), _SynergyEnums[i]);
    }

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
        // Bullets no longer have a damage penalty when fired through walls.
        NewSynergy(PROJECTING_MUCH, "Projecting, Much?", new[]{IName(FourDBullets.ItemName), IName(AstralProjector.ItemName)});
        // Aimu Hakurei's charge is instantly set to MAX upon getting hit.
        NewSynergy(LOTUS_LAND_STORY, "Lotus Land Story", new[]{IName(AimuHakurei.ItemName), "laser_lotus"});
        // Enemies stunned by Gorgun's Eye remain stunned for a second after looking away.
        NewSynergy(BLANK_STARE, "Blank Stare", new[]{IName(GorgunEye.ItemName), IName(BlankChecks.ItemName)});
        // Enemies have a 75% chance of having their gun replaced by a Bubble Blaster instead of a 50% chance.
        NewSynergy(DUBBLE_BUBBLE, "Dubble Bubble", new[]{IName(BubbleWand.ItemName), "bubble_blaster"});
        // Enemies drop twice as many souls when killed with Uppskeruvel.
        NewSynergy(SOUL_SEARCHING, "Soul Searching", new[]{IName(Uppskeruvel.ItemName), "gun_soul"});
        // Vacuuming debris occasionally generates casings (up to 20 per floor).
        NewSynergy(CLEANUP_CREW, "Cleanup Crew", new[]{IName(VacuumCleaner.ItemName), IName(CustodiansBadge.ItemName)});
        // Vacuum's chance to restore ammo is increased to 4%.
        NewSynergy(SCAVENGEST, "Scavengest", new[]{IName(VacuumCleaner.ItemName), IName(ScavengingArms.ItemName)});
        // When taking an otherwise fatal hit, if the player has at least 100 casings, damage is negated and the player loses 100 casings instead.
        NewSynergy(DEATH_AND_TAXES, "Death and Taxes", new[]{IName(CreditCard.ItemName), IName(BlankChecks.ItemName)});
        // Spawns a decoy when feigning death.
        NewSynergy(DEAD_MAN_STANDING, "Dead Man Standing", new[]{IName(DeadRinger.ItemName), "decoy"});
        // Spawns an explosive decoy when feigning death.
        NewSynergy(DEAD_MAN_EXPANDING, "Dead Man Expanding", new[]{IName(DeadRinger.ItemName), "explosive_decoy"});
        // Semi-automatic weapons have perfect accuracy.
        NewSynergy(AIM_BOTS, "Aim Bots", new[]{IName(BionicFinger.ItemName), "nanomachines"});
        // Pistol Whip's melee hit deals double damage to Jammed bosses and minibosses, and instantly smites all other Jammed enemies.
        NewSynergy(WICKED_CHILD, "Wicked Child", new[]{IName(PistolWhip.ItemName), IName(HolyWaterGun.ItemName)});
        // Enemies with guns that are charmed by Sub Machine Gun are given a Heroine.
        NewSynergy(I_NEED_A_HERO, "I Need a Hero", new[]{IName(SubMachineGun.ItemName), "heroine"});
        // Digitized chests are automatically unlocked when re-materialized.
        NewSynergy(KEYGEN, "Keygen", new[]{IName(Femtobyte.ItemName), "master_of_unlocking"});
        // Maestro reflects projectiles 50% faster.
        NewSynergy(COMMON_TIME, "Common Time", new[]{IName(Maestro.ItemName), "metronome"})
            .MultFireRate(1.5f);
        // Movement speed is increased by 25% while Breegull is active.
        NewSynergy(TALON_TROT, "Talon Trot", new[]{IName(Breegull.ItemName), "backpack"})
            .MultMoveSpeed(1.25f);
        // [REDACTED]
        NewSynergy(BLASTECH_A1, "BlasTech A-1", new[]{IName(BlasTechF4.ItemName), "laser_sight"});
        // Blackjack fires poker chips on either side of thrown cards.
        NewSynergy(PIT_BOSS, "Pit Boss", new[]{IName(Blackjack.ItemName), "amulet_of_the_pit_lord"});
        // K.A.L.I.'s charge rate is quadrupled.
        NewSynergy(PARTICLE_ACCELERATOR_ACCELERATOR, "Particle Accelerator Accelerator", new[]{IName(KALI.ItemName), "singularity"})
            .MultChargeRate(4f);
        // Stuffed Star recharges twice as quickly.
        NewSynergy(MR_ALLIGATOX, "Mr. Alligatox", new[]{IName(Alligator.ItemName), "stuffed_star"});
        // Adrenaline Shot's critical state lasts for 90 seconds, and taking damage no longer decreases the countdown timer.
        NewSynergy(ADRENALINE_RUSH, "Adrenaline Rush", new[]{IName(AdrenalineShot.ItemName), "shotgun_coffee"});
        // Enemies killed with English's projectiles drop an extra casing.
        NewSynergy(BANK_SHOTS, "Bank Shots", new[]{IName(English.ItemName), "loot_bag"});
        // Grappling Hook's cooldown is removed.
        NewSynergy(BIONIC_COMMANDO, "Bionic Commando", new[]{IName(BionicFinger.ItemName), "grappling_hook"});
        // Every blank used in a shop gives a 10% discount.
        NewSynergy(BLANK_EXPRESSION, "Blank Expression", new[]{IName(BlankChecks.ItemName), "disarming_personality"});
        // Femtobyte instantly kills any enemy that matches the last type of enemy it killed.
        NewSynergy(LOOKUP_TABLE, "Lookup Table", new[]{IName(Femtobyte.ItemName), "portable_table_device"});
        // Spawns 5 turtles upon getting hit
        NewSynergy(TROLLEY_PROBLEM, "Trolley Problem", new[]{IName(DerailGun.ItemName), "turtle_problem"});
        // Ticonderogun's leaves fire goop along drawn lines
        NewSynergy(DRAW_FIRE, "Draw Fire", new[]{IName(Ticonderogun.ItemName), "hot_lead"});
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
        // Spawns two hands that clap enemies, dealing double damage and stunning them for 10 seconds.
        NewMastery<MasteryOfHandCannon>(MASTERY_HAND_CANNON, HandCannon.ItemName);
        // Projectiles immediately observe enemies, effectively giving a 50% chance to instantly kill any enemy and remove all their projectiles.
        NewMastery<MasteryOfSchrodingersGat>(MASTERY_SCHRODINGERS_GAT, SchrodingersGat.ItemName);
        // Chicks spawn jammed and deal contact damage to enemies.
        NewMastery<MasteryOfHatchlingGun>(MASTERY_HATCHLING_GUN, HatchlingGun.ItemName);
        // Die faces no longer resets to 1 when firing, allowing you to hold fire on whatever face you want.
        NewMastery<MasteryOfCrapshooter>(MASTERY_CRAPSHOOTER, Crapshooter.ItemName);
        // Holy Water Gun deals 16x damage to Jammed enemies. Killing Jammed enemies creates pools of holy goop that grant invulnerability and infinite ammo while active.
        NewMastery<MasteryOfHolyWaterGun>(MASTERY_HOLY_WATER_GUN, HolyWaterGun.ItemName);
        // Every Junk vacuumed produces a full ammo box, and every 16 corpses vacuumed produces a piece of armor.
        NewMastery<MasteryOfVacuumCleaner>(MASTERY_VACUUM_CLEANER, VacuumCleaner.ItemName);
        // Projectiles spawn status effect goops corresponding to their color while in flight and upon impact.
        NewMastery<MasteryOfPaintballCannon>(MASTERY_PAINTBALL_CANNON, PaintballCannon.ItemName);
        // Gunbrella fires a constant stream of projectiles at the cursor.
        NewMastery<MasteryOfGunbrella>(MASTERY_GUNBRELLA, Gunbrella.ItemName);
        // Alyx decays four times slower and passively grants poison immunity.
        NewMastery<MasteryOfAlyx>(MASTERY_ALYX, Alyx.ItemName);
        // Pistol Whip can hit enemies closer than its max range, and after killing an enemy, will trigger a mini blank for the next 3 attacks.
        NewMastery<MasteryOfPistolWhip>(MASTERY_PISTOL_WHIP, PistolWhip.ItemName);
        // Femtobyte gains the ability to digitze enemies and respawn them as allies later.
        NewMastery<MasteryOfFemtobyte>(MASTERY_FEMTOBYTE, Femtobyte.ItemName);
        // Enemies drop souls and souls attack enemies even when Uppskeruvel is not the active gun.
        NewMastery<MasteryOfUppskeruvel>(MASTERY_UPPSKERUVEL, Uppskeruvel.ItemName);
        // Card speed is dramatically increased, clip size is doubled, and the last 13 cards in each clip become exploding jokers.
        NewMastery<MasteryOfBlackjack>(MASTERY_BLACKJACK, Blackjack.ItemName)
            .MultSpread(0.5f).MultClipSize(2f).MultFireRate(2f);
        // English can be charged to launch two additional rows of 6 and 7 balls, respectively.
        NewMastery<MasteryOfEnglish>(MASTERY_ENGLISH, English.ItemName);
        // Launches 3 knives at a time for no additional cost.
        NewMastery<MasteryOfIronMaid>(MASTERY_IRON_MAID, IronMaid.ItemName);
        // Alligator's energy production rate decays to its base level much more slowly when removed from an energy source, maintaining high damage output for longer.
        NewMastery<MasteryOfAlligator>(MASTERY_ALLIGATOR, Alligator.ItemName);
        // Touching enemies that have been transmuted to gold causes them to explode in a burst of high damage gold projectiles that trasmute other enemies to gold on kill.
        NewMastery<MasteryOfQuarterPounder>(MASTERY_QUARTER_POUNDER, QuarterPounder.ItemName);
        // Ticonderogun can be reloaded to switch to eraser mode, which reflects encircled enemy bullets.
        NewMastery<MasteryOfTiconderogun>(MASTERY_TICONDEROGUN, Ticonderogun.ItemName);
        // King's Law projectiles phase through walls and home in on enemies after launching.
        NewMastery<MasteryOfKingsLaw>(MASTERY_KINGS_LAW, KingsLaw.ItemName);
        // Enbubbled projectiles gain the ability to enbubble other projectiles on collision.
        NewMastery<MasteryOfBubblebeam>(MASTERY_BUBBLEBEAM, Bubblebeam.ItemName);
        // Deadline lasers act as tripwires and detonate whenever an enemy crosses them.
        NewMastery<MasteryOfDeadline>(MASTERY_DEADLINE, Deadline.ItemName);
      #endregion

        SanityCheckAllSynergiesHaveBeenInitialized();

        _MasteryVFX = VFX.Create("mastery_character_vfx", fps: 16, loops: false, anchor: Anchor.LowerCenter, emissivePower: 100f, emissiveColour: Color.red);
    }

    private static void SanityCheckAllSynergiesHaveBeenInitialized()
    {
        for (int i = 0; i < _SynergyNames.Count; ++i)
            if (_SynergyNames[i] == null)
                ETGModConsole.Log($"<color=#ffff88ff>WARNING: haven't initialized custom synergy {_SynergyEnums[i]}</color>");
    }

    private static AdvancedSynergyEntry NewSynergy(Synergy synergy, string name, string[] mandatory, string[] optional = null, bool ignoreLichEyeBullets = false, int masteryId = -1)
    {
        // Get the enum index of our synergy
        int index = (int)synergy;
        // Register the AdvancedSynergyEntry so that the game knows about it
        AdvancedSynergyEntry ase = RegisterSynergy(_Synergies[index], name, mandatory.ToList(), (optional != null) ? optional.ToList() : null, ignoreLichEyeBullets, masteryId);
        // Index the friendly name of our synergy
        _SynergyNames[index] = name;
        // Get the actual ID of our synergy entry in the AdvancedSynergyDatabase, which doesn't necessarily match the CustomSynergyType enum
        _SynergyIds[index] = GameManager.Instance.SynergyManager.synergies.Length - 1;
        // Return the AdvancedSynergyEntry
        return ase;
    }

    public static AdvancedSynergyEntry RegisterSynergy(CustomSynergyType synergy, string name, List<string> mandatoryConsoleIDs, List<string> optionalConsoleIDs = null, bool ignoreLichEyeBullets = false, int masteryId = -1)
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
            bonusSynergies       = new(){synergy},
            statModifiers        = new List<StatModifier>(),
            IgnoreLichEyeBullets = ignoreLichEyeBullets,
        };

        int oldLength = GameManager.Instance.SynergyManager.synergies.Length;
        Array.Resize(ref GameManager.Instance.SynergyManager.synergies, oldLength + 1);
        GameManager.Instance.SynergyManager.synergies[oldLength] = entry;
        return entry;
    }

    private static AdvancedSynergyEntry NewMastery<T>(Synergy synergy, string gunName) where T : MasteryDummyItem
    {
        if (Lazy.GetModdedItem(IName(gunName)) is not Gun gun)
            return null;

        FakeItem.Create<T>();
        int tokenId = FakeItem.Get<T>().PickupObjectId;
        //NOTE: next line can't begin with NewSynergy or itemtips script gets messed up
        AdvancedSynergyEntry ase = NewSynergy(
            synergy              : synergy,
            name                 : $"{gun.EncounterNameOrDisplayName} Mastery",
            mandatory            : new string[1]{IDs.InternalNames[gun.gunName]},
            masteryId            : tokenId,
            ignoreLichEyeBullets : true);
        _MasteryIds.Add(tokenId);
        _MasteryGuns[gun.PickupObjectId] = tokenId;
        return ase;
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
            //WARNING: if the mastery changes our clip size, the ui doesn't update for some reason (e.g., with Blackjack)...can't track down
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

    public static CustomSynergyType Synergy(this Synergy synergy)
    {
        return _Synergies[(int)synergy];
    }

    public static string SynergyName(this Synergy synergy)
    {
        return _SynergyNames[(int)synergy];
    }

    public static bool HasSynergy(this PlayerController player, Synergy synergy)
    {
        return player.ActiveExtraSynergies.Contains((int)_SynergyIds[(int)synergy]);
    }

    // stat fixer-uppers
    public static StatModifier Mult(this StatType s, float a) => new(){statToBoost = s, modifyType = ModifyMethod.MULTIPLICATIVE, amount = a};
    public static StatModifier Add(this StatType s, float a) => new(){statToBoost = s, modifyType = ModifyMethod.ADDITIVE, amount = a};

    public static AdvancedSynergyEntry MultFireRate(this AdvancedSynergyEntry e, float a)
        { e.statModifiers.Add(StatType.RateOfFire.Mult(a)); return e; }
    public static AdvancedSynergyEntry MultMoveSpeed(this AdvancedSynergyEntry e, float a)
        { e.statModifiers.Add(StatType.MovementSpeed.Mult(a)); return e; }
    public static AdvancedSynergyEntry MultSpread(this AdvancedSynergyEntry e, float a)
        { e.statModifiers.Add(StatType.Accuracy.Mult(a)); return e; }
    public static AdvancedSynergyEntry MultClipSize(this AdvancedSynergyEntry e, float a)
        { e.statModifiers.Add(StatType.AdditionalClipCapacityMultiplier.Mult(a)); return e; }
    public static AdvancedSynergyEntry MultChargeRate(this AdvancedSynergyEntry e, float a)
        { e.statModifiers.Add(StatType.ChargeAmountMultiplier.Mult(a)); return e; }

}

// Dummy classes for masteries
public   class MasteryDummyItem         : FakeItem { }
internal class MasteryOfGrandmaster     : MasteryDummyItem {}
internal class MasteryOfChekhovsGun     : MasteryDummyItem {}
internal class MasteryOfPincushion      : MasteryDummyItem {}
internal class MasteryOfPlatinumStar    : MasteryDummyItem {}
internal class MasteryOfNatascha        : MasteryDummyItem {}
internal class MasteryOfHandCannon      : MasteryDummyItem {}
internal class MasteryOfSchrodingersGat : MasteryDummyItem {}
internal class MasteryOfHatchlingGun    : MasteryDummyItem {}
internal class MasteryOfCrapshooter     : MasteryDummyItem {}
internal class MasteryOfHolyWaterGun    : MasteryDummyItem {}
internal class MasteryOfVacuumCleaner   : MasteryDummyItem {}
internal class MasteryOfPaintballCannon : MasteryDummyItem {}
internal class MasteryOfGunbrella       : MasteryDummyItem {}
internal class MasteryOfAlyx            : MasteryDummyItem {}
internal class MasteryOfPistolWhip      : MasteryDummyItem {}
internal class MasteryOfFemtobyte       : MasteryDummyItem {}
internal class MasteryOfUppskeruvel     : MasteryDummyItem {}
internal class MasteryOfBlackjack       : MasteryDummyItem {}
internal class MasteryOfEnglish         : MasteryDummyItem {}
internal class MasteryOfIronMaid        : MasteryDummyItem {}
internal class MasteryOfAlligator       : MasteryDummyItem {}
internal class MasteryOfQuarterPounder  : MasteryDummyItem {}
internal class MasteryOfTiconderogun    : MasteryDummyItem {}
internal class MasteryOfKingsLaw        : MasteryDummyItem {}
internal class MasteryOfBubblebeam      : MasteryDummyItem {}
internal class MasteryOfDeadline        : MasteryDummyItem {}

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
    DEATH_AND_TAXES,
    DEAD_MAN_STANDING,
    DEAD_MAN_EXPANDING,
    AIM_BOTS,
    WICKED_CHILD,
    I_NEED_A_HERO,
    KEYGEN,
    COMMON_TIME,
    TALON_TROT,
    BLASTECH_A1,
    PIT_BOSS,
    PARTICLE_ACCELERATOR_ACCELERATOR,
    MR_ALLIGATOX,
    ADRENALINE_RUSH,
    BANK_SHOTS,
    BIONIC_COMMANDO,
    BLANK_EXPRESSION,
    LOOKUP_TABLE,
    TROLLEY_PROBLEM,
    DRAW_FIRE,

    // Masteries
    MASTERY_GRANDMASTER,
    MASTERY_CHEKHOVS_GUN,
    MASTERY_PINCUSHION,
    MASTERY_PLATINUM_STAR,
    MASTERY_NATASCHA,
    MASTERY_HAND_CANNON,
    MASTERY_SCHRODINGERS_GAT,
    MASTERY_HATCHLING_GUN,
    MASTERY_CRAPSHOOTER,
    MASTERY_HOLY_WATER_GUN,
    MASTERY_VACUUM_CLEANER,
    MASTERY_PAINTBALL_CANNON,
    MASTERY_GUNBRELLA,
    MASTERY_ALYX,
    MASTERY_PISTOL_WHIP,
    MASTERY_FEMTOBYTE,
    MASTERY_UPPSKERUVEL,
    MASTERY_BLACKJACK,
    MASTERY_ENGLISH,
    MASTERY_IRON_MAID,
    MASTERY_ALLIGATOR,
    MASTERY_QUARTER_POUNDER,
    MASTERY_TICONDEROGUN,
    MASTERY_KINGS_LAW,
    MASTERY_BUBBLEBEAM,
    MASTERY_DEADLINE,
};
