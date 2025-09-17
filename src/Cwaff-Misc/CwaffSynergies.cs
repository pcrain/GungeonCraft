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
            _Synergies[i] = _SynergyEnums[i].ExtendEnum<CustomSynergyType>();
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
        // Vacuum's chance to restore ammo is increased to 20%.
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
        // Sub Machine Gun replaces any guns held by charmed enemies with Heroine.
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
        // Spawns 5 turtles upon getting hit.
        NewSynergy(TROLLEY_PROBLEM, "Trolley Problem", new[]{IName(DerailGun.ItemName), "turtle_problem"});
        // Ticonderogun leaves fire goop along drawn lines.
        NewSynergy(DRAW_FIRE, "Draw Fire", new[]{IName(Ticonderogun.ItemName), "hot_lead"});
        // Gorgun Eye's effect pierces walls and enemies, stunning all enemies in the direction the player is facing.
        NewSynergy(PIERCING_GAZE, "Piercing Gaze", new[]{IName(GorgunEye.ItemName), "ghost_bullets"});
        // Breegull's normal eggs can be fired for free.
        NewSynergy(CHEATO_PAGE, "Cheato Page", new[]{IName(Breegull.ItemName), "book_of_chest_anatomy"});
        // Standing over healthy goops restore Camera's ammo as well as Gunflower's.
        NewSynergy(PHOTOSYNTHESIS, "Photosynthesis", new[]{IName(Gunflower.ItemName), "camera"});
        // Grants immunity to contact damage while holding Alien Engine and immunity to most other forms of damage while firing Alien Engine.
        NewSynergy(TANK_ENGINE, "Tank Engine", new[]{IName(DerailGun.ItemName), "alien_engine"});
        // Projectiles detonated by blanks create larger explosions.
        NewSynergy(DEMOLITION_MAN, "Demolition, Man!", new[]{IName(VolcanicAmmolet.ItemName), IName(Scotsman.ItemName)});
        // Warrior's Gi activates when 2 hits from death as well as 1 hit from death.
        NewSynergy(SAIYAN_PRIDE, "Saiyan Pride", new[]{IName(WarriorsGi.ItemName), IName(KiBlast.ItemName)});
        // Flakseed sprouts grow 3x faster while planted in water.
        NewSynergy(LAWN_CARE, "Lawn Care", new[]{IName(Flakseed.ItemName), "starpew"});
        // Ring of Defenestration's rewards have a 33% chance of being doubled.
        NewSynergy(THE_ABYSS_STARES_BACK, "The Abyss Stares Back", new[]{IName(RingOfDefenestration.ItemName), "amulet_of_the_pit_lord"});
        // Tables reflect projectiles back at enemies.
        NewSynergy(FURNITURE_POLISH, "Furniture Polish", new[]{IName(BulletproofTablecloth.ItemName), "potion_of_lead_skin"});
        // Paintball Cannon's projectiles reflect enemy projectiles while Potion of Lead Skin is active.
        NewSynergy(LEAD_PAINT, "Lead Paint", new[]{IName(PaintballCannon.ItemName), "potion_of_lead_skin"});
        // Camping Supplies' damage boost builds twice as quickly.
        NewSynergy(COZY_CAMPER, "Cozy Camper", new[]{IName(CampingSupplies.ItemName), IName(ComfySlippers.ItemName)});
        // Treasure spots uncovered by Itemfinder also spawn 3-9 casings.
        NewSynergy(TREASURE_HUNTER, "Treasure Hunter", new[]{IName(Itemfinder.ItemName), "sense_of_direction"});
        // Taking damage no longer causes juggled guns to be dropped.
        NewSynergy(SOLID_FOOTING, "Solid Footing", new[]{IName(Jugglernaut.ItemName), "heavy_boots"});
        // Sextant locks on 30% faster.
        NewSynergy(YOU_MAY_USE_A_CALCULATOR, "You May Use a Calculator", new[]{IName(Sextant.ItemName), IName(Calculator.ItemName)});
        // Wayfarer's drones leave a trail of poison
        NewSynergy(STRAGGLER, "Straggler", new[]{IName(Wayfarer.ItemName), "gas_mask"});
        // Wayfarer's drones leave a trail of fire
        NewSynergy(TRAILBLAZER, "Trailblazer", new[]{IName(Wayfarer.ItemName), "ring_of_fire_resistance"});
        // Hallaeribut always fires 10 piranhas per shot at all hunger levels.
        NewSynergy(STAY_HUNGRY, "Stay Hungry", new[]{IName(Hallaeribut.ItemName), "hungry_bullets"});
        // Upon taking damage, time freezes for everything but the player for 5 seconds.
        NewSynergy(SEGALS_LAW, "Segal's Law", new[]{IName(DeadRinger.ItemName), "super_hot_watch"});
        // Allays are twice as likely to find items upon clearing a room with at least one torch placed in it.
        NewSynergy(SPAWNPROOFING, "Spawnproofing", new[]{IName(AmethystShard.ItemName), IName(StackOfTorches.ItemName)});
        // Cuppajoe's stat boost duration is reduced to 9 seconds, but crash time is reduced to 3 seconds.
        NewSynergy(CAFFEINE_ADDICTION, "Caffeine Addiction", new[]{IName(MacchiAuto.ItemName), IName(Cuppajoe.ItemName)});
        // Kaliber's Justice can no longer take items from the player.
        NewSynergy(KALIBERS_FAVOR, "Kaliber's Favor", new[]{IName(KalibersJustice.ItemName), "seven_leaf_clover"});
        // Tryhard Snacks stay active for twice as long after an enemy spawns.
        NewSynergy(GAMER_REFLEXES, "Gamer Reflexes", new[]{IName(TryhardSnacks.ItemName), "3rd_party_controller"});
        // Derail Gun reloads 35% faster.
        NewSynergy(AHEAD_OF_SCHEDULE, "Ahead of Schedule", new[]{IName(DerailGun.ItemName), "sense_of_direction"})
            .MultReload(0.65f);
        // Sextant reloads 70% faster.
        NewSynergy(WRONG_KIND_OF_COMPASS, "Wrong Kind of Compass", new[]{IName(Sextant.ItemName), "sense_of_direction"})
            .MultReload(0.3f);
        // Custodian's Badge can no longer incur strikes.
        NewSynergy(JOB_SECURITY, "Job Security", new[]{IName(CustodiansBadge.ItemName), IName(RatPoison.ItemName)});
        // Vacuum Cleaner and Leafblower are dual wielded.
        NewSynergy(FULL_CIRCULATION, "Full Circulation", new[]{IName(VacuumCleaner.ItemName), IName(Leafblower.ItemName)});
      #endregion

      #region Masteries
        // Grandmaster shoots an additional black piece with every shot, no longer shoots pawns, and moves pieces twice as fast.
        NewMastery<MasteryOfGrandmaster>(MASTERY_GRANDMASTER, Grandmaster.ItemName);
        // Chekhov's Gun has no minimum fire time and restores all unfired shots at the end of the room.
        NewMastery<MasteryOfChekhovsGun>(MASTERY_CHEKHOVS_GUN, ChekhovsGun.ItemName);
        // Pincushion's pins phase through decor, and the gun itself no longer increases in spread.
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
        // Crapshooter's die face does not reset to 1 upon firing and repeatedly shoots the same-numbered die as long as fire as held.
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
        // Pistol Whip deals double damage to jammed enemies, and after killing any enemy, will trigger a mini blank for the next 3 attacks.
        NewMastery<MasteryOfPistolWhip>(MASTERY_PISTOL_WHIP, PistolWhip.ItemName);
        // Femtobyte gains the ability to digitze enemies and respawn them as allies later.
        NewMastery<MasteryOfFemtobyte>(MASTERY_FEMTOBYTE, Femtobyte.ItemName);
        // Enemies drop soul fragments and Aimless Souls attack enemies even when Uppskeruvel is not the active gun.
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
        // Starmageddon fires meteors that deal splash damage and set the ground ablaze, and passively grants fire immunity.
        NewMastery<MasteryOfStarmageddon>(MASTERY_STARMAGEDDON, Starmageddon.ItemName);
        // Subtractor Beam shots that only hit one enemy use that enemy as the damage source for Subtractor Beam's next shot.
        NewMastery<MasteryOfSubtractorBeam>(MASTERY_SUBTRACTOR_BEAM, SubtractorBeam.ItemName);
        // Shots ignore the invulnerable phases of most enemies and no longer prevent enemies from dropping casings or other rewards.
        NewMastery<MasteryOfKALI>(MASTERY_KALI, KALI.ItemName);
        // Reloading detonates any decor or explosive enemies in a large cone in front of the player. Exploding decor will not damage the player, but explosive enemies will.
        NewMastery<MasteryOfScotsman>(MASTERY_SCOTSMAN, Scotsman.ItemName);
        // Carpet Bomber's charge rate is quadrupled and projectiles travel twice as quickly
        NewMastery<MasteryOfCarpetBomber>(MASTERY_CARPET_BOMBER, CarpetBomber.ItemName)
            .MultChargeRate(4f).MultProjSpeed(2f);
        // Soul-linked enemies may take damage instead of the player whenever the player gets hit. The chance per enemy is equal to 25% + (5% * Curse), capping at 75% per enemy.
        NewMastery<MasteryOfSoulKaliber>(MASTERY_SOUL_KALIBER, SoulKaliber.ItemName);
        // Birds can phase through inner walls and collect up to three projectiles on the way back from an enemy.
        NewMastery<MasteryOfLightwing>(MASTERY_LIGHTWING, Lightwing.ItemName);
        // Debris is launched faster and with less spread, and pierces through enemies.
        NewMastery<MasteryOfMagunet>(MASTERY_MAGUNET, Magunet.ItemName)
            .MultSpread(0.5f).MultProjSpeed(2f);
        // Derail Gun continuously leaks oil onto the ground while held. When ignited, the oil produces green fire that doesn't harm the player.
        NewMastery<MasteryOfDerailGun>(MASTERY_DERAIL_GUN, DerailGun.ItemName);
        // Killing any enemy instantly spawns a replicant of that enemy if Alien Nailgun has previously registered its DNA, regardless of active gun.
        NewMastery<MasteryOfAlienNailgun>(MASTERY_ALIEN_NAILGUN, AlienNailgun.ItemName);
        // Vladimir increases curse by 1 for every 10 enemies killed, gains power for every point of curse the player has, and passively prevents Lord of the Jammed from spawning.
        NewMastery<MasteryOfVladimir>(MASTERY_VLADIMIR, Vladimir.ItemName);
        // Maestro reflects projectiles twice as quickly and does not consume ammo unless a projectile is reflected.
        NewMastery<MasteryOfMaestro>(MASTERY_MAESTRO, Maestro.ItemName)
            .MultFireRate(2.0f);
        // Ki Blast can be charged to fire a Kamehameha capable of breaking boss damage caps. Charging a Kamehameha slows the player down, and firing the Kamehameha prevents the player from moving entirely.
        NewMastery<MasteryOfKiBlast>(MASTERY_KI_BLAST, KiBlast.ItemName);
        // Hallaeribut becomes permanently Ravenous and can be fed items on the ground by reloading with a full clip. Items grant ammo proportional to their quality. If Hallaeribut runs out of ammo, it will automatically consume the least valuable item in the player's inventory for ammo. Hallaeribut will not feed on the player until no more items are available.
        NewMastery<MasteryOfHallaeribut>(MASTERY_HALLAERIBUT, Hallaeribut.ItemName);
        // Gunflower passively regenerates up to 10% of its max ammo while active and can gain ammo from all goops.
        NewMastery<MasteryOfGunflower>(MASTERY_GUNFLOWER, Gunflower.ItemName);
        // Omnidirectional Laser fires lasers in 5 directions, with the laser aimed towards the reticle being 50% stronger.
        NewMastery<MasteryOfOmnidirectionalLaser>(MASTERY_OMNIDIRECTIONAL_LASER, OmnidirectionalLaser.ItemName)
            .MultDamage(1.5f);
        // Blamethrower fires projectiles radially in all directions and permanently stuns scapegoats.
        NewMastery<MasteryOfBlamethrower>(MASTERY_BLAMETHROWER, Blamethrower.ItemName);
        // Projectiles that hit walls now split into two projectiles that follow the wall in both directions.
        NewMastery<MasteryOfZag>(MASTERY_ZAG, Zag.ItemName);
        // Projectiles move twice as fast and home onto enemies.
        NewMastery<MasteryOfOutbreak>(MASTERY_OUTBREAK, Outbreak.ItemName)
            .MultProjSpeed(2f);
        // Telefragger can be reloaded while firing to instantly teleport the player to the end of the beam. This teleport cannot be triggered again until killing another enemy with Telefragger.
        NewMastery<MasteryOfTelefragger>(MASTERY_TELEFRAGGER, Telefragger.ItemName);
        // Reload time is halved, and tranquilized enemies with guns are guaranteed to drop their guns and some ammo.
        NewMastery<MasteryOfTranquilizer>(MASTERY_TRANQUILIZER, Tranquilizer.ItemName)
            .MultReload(0.5f);
        // Graze range is increased, focus mode slows down time even further, and projectiles grazed while not in focus mode are reflected back at enemies.
        NewMastery<MasteryOfAimuHakurei>(MASTERY_AIMU_HAKUREI, AimuHakurei.ItemName);
        // Racket Launcher gains increased reflect range, and can serve additional projectiles when fired while no balls are within reflect range.
        NewMastery<MasteryOfRacketLauncher>(MASTERY_RACKET_LAUNCHER, RacketLauncher.ItemName);
        // Each juggled gun automatically fires an additional ball projectile when tossed.
        NewMastery<MasteryOfJugglernaut>(MASTERY_JUGGLERNAUT, Jugglernaut.ItemName);
        // [REDACTED]
        NewMastery<MasteryOfBlasTechF4>(MASTERY_BLASTECH_F4, BlasTechF4.ItemName);
        // Yggdrashell can target up to 3 enemies simultaneously.
        NewMastery<MasteryOfYggdrashell>(MASTERY_YGGDRASHELL, Yggdrashell.ItemName);
        // Spider drones fire lasers that pierce small obstacles and have increased speed, damage, and fire rate.
        NewMastery<MasteryOfWidowmaker>(MASTERY_WIDOWMAKER, Widowmaker.ItemName);
        // Oddjob fires additional sawblade projectiles outward radially while in flight.
        NewMastery<MasteryOfOddjob>(MASTERY_ODDJOB, Oddjob.ItemName);
        // Sunderbuss produces a shockwave that travels along the ground in the direction of aim, heavily damaging all enemies in its path.
        NewMastery<MasteryOfSunderbuss>(MASTERY_SUNDERBUSS, Sunderbuss.ItemName);
        // Wavefront's projectiles become ionized, intermittently zapping nearby enemies and each other.
        NewMastery<MasteryOfWavefront>(MASTERY_WAVEFRONT, Wavefront.ItemName);
        // Breegull transforms into its dragon form, granting infinite fire eggs that never need reloading.
        NewMastery<MasteryOfBreegull>(MASTERY_BREEGULL, Breegull.ItemName);
        // Overflow siphons goop from barrels faster and can be overfilled far past its max ammo, causing it to constantly autofire extra goop until it is no longer overfilled.
        NewMastery<MasteryOfOverflow>(MASTERY_OVERFLOW, Overflow.ItemName);
        // Every 5-15 enemies killed with Missiletoe triggers a gift exchange, which randomly replaces a wrapped gift with another item of equal quality.
        NewMastery<MasteryOfMissiletoe>(MASTERY_MISSILETOE, Missiletoe.ItemName);
        // Flow of time is further slowed down when standing in coffee goop.
        NewMastery<MasteryOfMacchiAuto>(MASTERY_MACCHI_AUTO, MacchiAuto.ItemName);
        // Chroma gains a tribeam mode that uses additional ammo, but deals vastly increased damage. The tribeam's damage scales even further based on the lowest of extracted red, green, and blue pigment levels.
        NewMastery<MasteryOfChroma>(MASTERY_CHROMA, Chroma.ItemName);
        // Flakseed's sprouts can grow in hostile terrain and can no longer be trampled. Fully grown sprouts become larger flak flowers that attack and stun nearby enemies with their roots.
        NewMastery<MasteryOfFlakseed>(MASTERY_FLAKSEED, Flakseed.ItemName);
        // Cars have much better handling, deal 25% more damage, and can crash into walls up to 3 times before disappearing.
        NewMastery<MasteryOfRCLauncher>(MASTERY_R_C_LAUNCHER, RCLauncher.ItemName)
            .MultDamage(1.25f);
        // B. B. Gun projectiles deflect all enemy projectiles in their path, transforming them to pins in the process.
        NewMastery<MasteryOfBBGun>(MASTERY_B_B_GUN, BBGun.ItemName);
        // Bat echoes have a 100% chance of distracting enemies at any distance, and gain the ability to distract bosses.
        NewMastery<MasteryOfNycterian>(MASTERY_NYCTERIAN, Nycterian.ItemName);
        // Reload time is decreased by 35%, and seltzer water now inflicts hiccups on enemies. Hiccups have a chance to erratically stun enemies for a brief period and causes them to emit a ring of bullets that damage other nearby enemies.
        NewMastery<MasteryOfSeltzerPelter>(MASTERY_SELTZER_PELTER, SeltzerPelter.ItemName)
            .MultReload(0.65f);
        // Firing uncharged shots no longer consumes ammo, and every active prism fires a parallel beam of light in unison with Suncaster.
        NewMastery<MasteryOfSuncaster>(MASTERY_SUNCASTER, Suncaster.ItemName);
        // Projectiles home towards nearby enemies on each bounce and gain unlimited piercing with no damage loss.
        NewMastery<MasteryOfBouncer>(MASTERY_BOUNCER, Bouncer.ItemName);
        // Sub Machine Gun restores all hearts when consumed, and is automatically consumed upon taking otherwise fatal damage.
        NewMastery<MasteryOfSubMachineGun>(MASTERY_SUB_MACHINE_GUN, SubMachineGun.ItemName);
        // Reloading now deploys a stereo that continuously emits sound matching Stereoscope's pitch at time of deployment, acting as a secondary source of stun and damage.
        NewMastery<MasteryOfStereoscope>(MASTERY_STEREOSCOPE, Stereoscope.ItemName);
        // Groundhog charges twice as quickly and creates a mini-tremor when plunged into the ground, triggering a mini-blank effect.
        NewMastery<MasteryOfGroundhog>(MASTERY_GROUNDHOG, Groundhog.ItemName)
            .MultChargeRate(2f);
        // Glockarina gains infinite ammo and fires 3 rings of notes when reloading an empty clip.
        NewMastery<MasteryOfGlockarina>(MASTERY_GLOCKARINA, Glockarina.ItemName);
        // Macheening's charge-up time is halved, and its projectiles destroy enemy projectiles upon collision.
        NewMastery<MasteryOfMacheening>(MASTERY_MACHEENING, Macheening.ItemName);
        // Plasmarble projectiles bounce two additional times before shattering and emit 4 electric bolts per bounce.
        NewMastery<MasteryOfPlasmarble>(MASTERY_PLASMARBLE, Plasmarble.ItemName);
        // Reloading toggles autotarget mode, allowing Xelsior's pistols to autotarget enemies at a reduced fire rate.
        NewMastery<MasteryOfXelsior>(MASTERY_XELSIOR, Xelsior.ItemName);
        // Projectiles can now hit enemies, and destroy up to 5 of their projectiles on contact.
        NewMastery<MasteryOfEmpath>(MASTERY_EMPATH, Empath.ItemName);
        // Sextant automatically fires as soon as it will kill its target or deal a critical hit.
        NewMastery<MasteryOfSextant>(MASTERY_SEXTANT, Sextant.ItemName);
        // Leafblower gains increased knockback and the ability to blow around projectiles.
        NewMastery<MasteryOfLeafblower>(MASTERY_LEAFBLOWER, Leafblower.ItemName);
        // Reloading while a drone is active disconnects the drone and makes it autonomous, causing it to automatically seek out enemies in its line of sight.
        NewMastery<MasteryOfWayfarer>(MASTERY_WAYFARER, Wayfarer.ItemName);
        // Forks explode upon multiplying.
        NewMastery<MasteryOfForkbomb>(MASTERY_FORKBOMB, Forkbomb.ItemName);
        // Zealot gains infinite ammo and fires even while dodge rolling.
        NewMastery<MasteryOfZealot>(MASTERY_ZEALOT, Zealot.ItemName);
        // Toothpaste projectiles create suds in a much larger radius, and toothbrush can be swung much faster.
        NewMastery<MasteryOfToothpaste>(MASTERY_TOOTHPASTE, Toothpaste.ItemName);
        // Gradius deploys 2 extra blue, orange, and pink ships.
        NewMastery<MasteryOfGradius>(MASTERY_GRADIUS, Gradius.ItemName);
        // While Heartbreaker is active, each of the player's empty heart containers orbits them to block bullets.
        NewMastery<MasteryOfHeartbreaker>(MASTERY_HEARTBREAKER, Heartbreaker.ItemName);
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
            mandatory            : [IName(gun.gunName)],
            masteryId            : tokenId,
            ignoreLichEyeBullets : true);
        _MasteryIds.Add(tokenId);
        _MasteryGuns[gun.PickupObjectId] = tokenId;
        return ase;
    }

    public static int MasteryTokenId(this Gun gun)
    {
        return _MasteryGuns.TryGetValue(gun.PickupObjectId, out int id) ? id : -1;
    }

    public static void AcquireMastery(this PlayerController player, Gun gun)
    {
        if (gun && gun.MasteryTokenId() is int id && id >= 0)
        {
            //WARNING: if the mastery changes our clip size, the ui doesn't update for some reason (e.g., with Blackjack)...can't track down
            player.AcquireFakeItem(id);
            if (gun.gameObject.GetComponent<CwaffGun>() is CwaffGun cg)
                cg.DoMasteryChecks(player);
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
            cursor.CallPrivate(typeof(RecalculateMasteriesPatch), nameof(CheckForNewMasteries));
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

    private static string IName(string itemName) => C.MOD_PREFIX+":"+itemName.InternalName();
    public static CustomSynergyType Synergy(this Synergy synergy) => _Synergies[(int)synergy];
    public static string SynergyName(this Synergy synergy) => _SynergyNames[(int)synergy];
    public static bool HasSynergy(this PlayerController player, Synergy synergy) => player.ActiveExtraSynergies.Contains((int)_SynergyIds[(int)synergy]);

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
    public static AdvancedSynergyEntry MultProjSpeed(this AdvancedSynergyEntry e, float a)
        { e.statModifiers.Add(StatType.ProjectileSpeed.Mult(a)); return e; }
    public static AdvancedSynergyEntry MultDamage(this AdvancedSynergyEntry e, float a)
        { e.statModifiers.Add(StatType.Damage.Mult(a)); return e; }
    public static AdvancedSynergyEntry MultReload(this AdvancedSynergyEntry e, float a)
        { e.statModifiers.Add(StatType.ReloadSpeed.Mult(a)); return e; }
    public static AdvancedSynergyEntry MultAmmo(this AdvancedSynergyEntry e, float a)
        { e.statModifiers.Add(StatType.AmmoCapacityMultiplier.Mult(a)); return e; }

}

// Dummy classes for masteries
public   class MasteryDummyItem              : FakeItem { }
internal class MasteryOfGrandmaster          : MasteryDummyItem {}
internal class MasteryOfChekhovsGun          : MasteryDummyItem {}
internal class MasteryOfPincushion           : MasteryDummyItem {}
internal class MasteryOfPlatinumStar         : MasteryDummyItem {}
internal class MasteryOfNatascha             : MasteryDummyItem {}
internal class MasteryOfHandCannon           : MasteryDummyItem {}
internal class MasteryOfSchrodingersGat      : MasteryDummyItem {}
internal class MasteryOfHatchlingGun         : MasteryDummyItem {}
internal class MasteryOfCrapshooter          : MasteryDummyItem {}
internal class MasteryOfHolyWaterGun         : MasteryDummyItem {}
internal class MasteryOfVacuumCleaner        : MasteryDummyItem {}
internal class MasteryOfPaintballCannon      : MasteryDummyItem {}
internal class MasteryOfGunbrella            : MasteryDummyItem {}
internal class MasteryOfAlyx                 : MasteryDummyItem {}
internal class MasteryOfPistolWhip           : MasteryDummyItem {}
internal class MasteryOfFemtobyte            : MasteryDummyItem {}
internal class MasteryOfUppskeruvel          : MasteryDummyItem {}
internal class MasteryOfBlackjack            : MasteryDummyItem {}
internal class MasteryOfEnglish              : MasteryDummyItem {}
internal class MasteryOfIronMaid             : MasteryDummyItem {}
internal class MasteryOfAlligator            : MasteryDummyItem {}
internal class MasteryOfQuarterPounder       : MasteryDummyItem {}
internal class MasteryOfTiconderogun         : MasteryDummyItem {}
internal class MasteryOfKingsLaw             : MasteryDummyItem {}
internal class MasteryOfBubblebeam           : MasteryDummyItem {}
internal class MasteryOfDeadline             : MasteryDummyItem {}
internal class MasteryOfStarmageddon         : MasteryDummyItem {}
internal class MasteryOfSubtractorBeam       : MasteryDummyItem {}
internal class MasteryOfKALI                 : MasteryDummyItem {}
internal class MasteryOfScotsman             : MasteryDummyItem {}
internal class MasteryOfCarpetBomber         : MasteryDummyItem {}
internal class MasteryOfSoulKaliber          : MasteryDummyItem {}
internal class MasteryOfLightwing            : MasteryDummyItem {}
internal class MasteryOfMagunet              : MasteryDummyItem {}
internal class MasteryOfDerailGun            : MasteryDummyItem {}
internal class MasteryOfAlienNailgun         : MasteryDummyItem {}
internal class MasteryOfVladimir             : MasteryDummyItem {}
internal class MasteryOfMaestro              : MasteryDummyItem {}
internal class MasteryOfKiBlast              : MasteryDummyItem {}
internal class MasteryOfHallaeribut          : MasteryDummyItem {}
internal class MasteryOfGunflower            : MasteryDummyItem {}
internal class MasteryOfOmnidirectionalLaser : MasteryDummyItem {}
internal class MasteryOfBlamethrower         : MasteryDummyItem {}
internal class MasteryOfZag                  : MasteryDummyItem {}
internal class MasteryOfOutbreak             : MasteryDummyItem {}
internal class MasteryOfTelefragger          : MasteryDummyItem {}
internal class MasteryOfTranquilizer         : MasteryDummyItem {}
internal class MasteryOfAimuHakurei          : MasteryDummyItem {}
internal class MasteryOfRacketLauncher       : MasteryDummyItem {}
internal class MasteryOfJugglernaut          : MasteryDummyItem {}
internal class MasteryOfBlasTechF4           : MasteryDummyItem {}
internal class MasteryOfYggdrashell          : MasteryDummyItem {}
internal class MasteryOfWidowmaker           : MasteryDummyItem {}
internal class MasteryOfOddjob               : MasteryDummyItem {}
internal class MasteryOfSunderbuss           : MasteryDummyItem {}
internal class MasteryOfWavefront            : MasteryDummyItem {}
internal class MasteryOfBreegull             : MasteryDummyItem {}
internal class MasteryOfOverflow             : MasteryDummyItem {}
internal class MasteryOfMissiletoe           : MasteryDummyItem {}
internal class MasteryOfMacchiAuto           : MasteryDummyItem {}
internal class MasteryOfChroma               : MasteryDummyItem {}
internal class MasteryOfFlakseed             : MasteryDummyItem {}
internal class MasteryOfRCLauncher           : MasteryDummyItem {}
internal class MasteryOfBBGun                : MasteryDummyItem {}
internal class MasteryOfNycterian            : MasteryDummyItem {}
internal class MasteryOfSeltzerPelter        : MasteryDummyItem {}
internal class MasteryOfSuncaster            : MasteryDummyItem {}
internal class MasteryOfBouncer              : MasteryDummyItem {}
internal class MasteryOfSubMachineGun        : MasteryDummyItem {}
internal class MasteryOfStereoscope          : MasteryDummyItem {}
internal class MasteryOfGroundhog            : MasteryDummyItem {}
internal class MasteryOfGlockarina           : MasteryDummyItem {}
internal class MasteryOfMacheening           : MasteryDummyItem {}
internal class MasteryOfPlasmarble           : MasteryDummyItem {}
internal class MasteryOfXelsior              : MasteryDummyItem {}
internal class MasteryOfEmpath               : MasteryDummyItem {}
internal class MasteryOfSextant              : MasteryDummyItem {}
internal class MasteryOfLeafblower           : MasteryDummyItem {}
internal class MasteryOfWayfarer             : MasteryDummyItem {}
internal class MasteryOfForkbomb             : MasteryDummyItem {}
internal class MasteryOfZealot               : MasteryDummyItem {}
internal class MasteryOfToothpaste           : MasteryDummyItem {}
internal class MasteryOfGradius              : MasteryDummyItem {}
internal class MasteryOfHeartbreaker         : MasteryDummyItem {}

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
    PIERCING_GAZE,
    CHEATO_PAGE,
    PHOTOSYNTHESIS,
    TANK_ENGINE,
    DEMOLITION_MAN,
    SAIYAN_PRIDE,
    LAWN_CARE,
    THE_ABYSS_STARES_BACK,
    FURNITURE_POLISH,
    LEAD_PAINT,
    COZY_CAMPER,
    TREASURE_HUNTER,
    SOLID_FOOTING,
    YOU_MAY_USE_A_CALCULATOR,
    STRAGGLER,
    TRAILBLAZER,
    STAY_HUNGRY,
    SEGALS_LAW,
    SPAWNPROOFING,
    CAFFEINE_ADDICTION,
    KALIBERS_FAVOR,
    GAMER_REFLEXES,
    AHEAD_OF_SCHEDULE,
    WRONG_KIND_OF_COMPASS,
    JOB_SECURITY,
    FULL_CIRCULATION,

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
    MASTERY_STARMAGEDDON,
    MASTERY_SUBTRACTOR_BEAM,
    MASTERY_KALI,
    MASTERY_SCOTSMAN,
    MASTERY_CARPET_BOMBER,
    MASTERY_SOUL_KALIBER,
    MASTERY_LIGHTWING,
    MASTERY_MAGUNET,
    MASTERY_DERAIL_GUN,
    MASTERY_ALIEN_NAILGUN,
    MASTERY_VLADIMIR,
    MASTERY_MAESTRO,
    MASTERY_KI_BLAST,
    MASTERY_HALLAERIBUT,
    MASTERY_GUNFLOWER,
    MASTERY_OMNIDIRECTIONAL_LASER,
    MASTERY_BLAMETHROWER,
    MASTERY_ZAG,
    MASTERY_OUTBREAK,
    MASTERY_TELEFRAGGER,
    MASTERY_TRANQUILIZER,
    MASTERY_AIMU_HAKUREI,
    MASTERY_RACKET_LAUNCHER,
    MASTERY_JUGGLERNAUT,
    MASTERY_BLASTECH_F4,
    MASTERY_YGGDRASHELL,
    MASTERY_WIDOWMAKER,
    MASTERY_ODDJOB,
    MASTERY_SUNDERBUSS,
    MASTERY_WAVEFRONT,
    MASTERY_BREEGULL,
    MASTERY_OVERFLOW,
    MASTERY_MISSILETOE,
    MASTERY_MACCHI_AUTO,
    MASTERY_CHROMA,
    MASTERY_FLAKSEED,
    MASTERY_R_C_LAUNCHER,
    MASTERY_B_B_GUN,
    MASTERY_NYCTERIAN,
    MASTERY_SELTZER_PELTER,
    MASTERY_SUNCASTER,
    MASTERY_BOUNCER,
    MASTERY_SUB_MACHINE_GUN,
    MASTERY_STEREOSCOPE,
    MASTERY_GROUNDHOG,
    MASTERY_GLOCKARINA,
    MASTERY_MACHEENING,
    MASTERY_PLASMARBLE,
    MASTERY_XELSIOR,
    MASTERY_EMPATH,
    MASTERY_SEXTANT,
    MASTERY_LEAFBLOWER,
    MASTERY_WAYFARER,
    MASTERY_FORKBOMB,
    MASTERY_ZEALOT,
    MASTERY_TOOTHPASTE,
    MASTERY_GRADIUS,
    MASTERY_HEARTBREAKER,
};
