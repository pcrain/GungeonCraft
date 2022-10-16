using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ItemAPI;

namespace NevernamedsItems
{
    class InitialiseSynergies
    {
        public static void DoInitialisation()
        {
            #region PreBigUpdateSynergies
            //HANDLE SYNERGIES
            //Keybullet Effigy / Spare Key
            List<string> mandatorySynergyItems = new List<string>() { "nn:keybullet_effigy", "nn:spare_key" };
            CustomSynergies.Add("Spare Kin", mandatorySynergyItems);
            //GUN GREASE SYNERGIES
            List<string> mandatorySynergyItemsWorldOnFire = new List<string>() { "nn:gun_grease" };
            List<string> optionalSynergyItemsWorldOnFire = new List<string>() { "napalm_strike", "big_boy", "hot_lead", "ring_of_fire_resistance", "gungeon_pepper" };
            CustomSynergies.Add("Set The World On Fire", mandatorySynergyItemsWorldOnFire, optionalSynergyItemsWorldOnFire);
            List<string> mandatorySynergyItemsOilSlick = new List<string>() { "nn:gun_grease" };
            List<string> optionalSynergyItemsOilSlick = new List<string>() { "nn:sanctified_oil", "oiled_cylinder" };
            CustomSynergies.Add("Oil Slick", mandatorySynergyItemsOilSlick, optionalSynergyItemsOilSlick);
            //BOMBER JACKET / LIL BOMBER SYNERGY
            List<string> mandatorySynergyItemsBigBomber = new List<string>() { "nn:bomber_jacket", "lil_bomber" };
            CustomSynergies.Add("Big Bomber", mandatorySynergyItemsBigBomber);
            //DRAGUN SCALE / TABLE TECH HEAT SYNERGY
            List<string> mandatorySynergyItemsHotHotTable = new List<string>() { "nn:dragun_scale", "table_tech_heat" };
            CustomSynergies.Add("Hot Hot Table", mandatorySynergyItemsHotHotTable);
            //ELEVATOR BUTTON / TIME FUDDLER'S ROBE
            List<string> mandatorySynergyItemsTurnBackTime = new List<string>() { "nn:elevator_button", "space_friend" };
            CustomSynergies.Add("If We Could Turn Back Time", mandatorySynergyItemsTurnBackTime);
            //G-SWITCH / BRICK BREAKER
            List<string> mandatorySynergyItemsGIsForGain = new List<string>() { "nn:g-switch", "brick_breaker" };
            CustomSynergies.Add("G is for Gain", mandatorySynergyItemsGIsForGain);
            //RUSTY CASING / ORGAN DONOR CARD
            List<string> mandatorySynergyItemsHeartSoWetAndCold = new List<string>() { "nn:organ_donor_card", "nn:rusty_casing" };
            CustomSynergies.Add("Heart so wet and cold...", mandatorySynergyItemsHeartSoWetAndCold);
            //JUNKLLETS / FULL ARMOUR JACKET
            List<string> mandatorySynergyItemsManOrMachine = new List<string>() { "nn:junkllets", "nn:full_armour_jacket" };
            CustomSynergies.Add("Man... or Machine?", mandatorySynergyItemsManOrMachine);
            //GOLDEN ARMOUR / SILVER BULLETS
            List<string> mandatorySynergyItemsZilveredUp = new List<string>() { "nn:golden_armour", "silver_bullets" };
            CustomSynergies.Add("Zilvered Up", mandatorySynergyItemsZilveredUp);
            //GOLDEN ARMOUR / OLD GOLDIE / GOLD JUNK / GOLD AMMOLET / AU GUN / GILDED HYDRA
            List<string> mandatorySynergyItemsGoldReserves = new List<string>() { "nn:golden_armour" };
            List<string> optionalSynergyItemsGoldReserves = new List<string>() { "old_goldie", "gold_junk", "gold_ammolet", "au_gun", "gilded_hydra" };
            CustomSynergies.Add("Gold Reserves", mandatorySynergyItemsGoldReserves, optionalSynergyItemsGoldReserves);
            //BASHFUL SHOT / CROWDED CLIPS
            List<string> mandatorySynergyItemsHighsAndLows = new List<string>() { "nn:bashful_shot", "nn:crowded_clips" };
            CustomSynergies.Add("Highs and Lows", mandatorySynergyItemsHighsAndLows);
            //BULLET BULLETS / EASY RELOAD BULLETS
            List<string> mandatorySynergyItemsEasyPeasy = new List<string>() { "nn:bullet_bullets", "easy_reload_bullets" };
            CustomSynergies.Add("Easy Peasy", mandatorySynergyItemsEasyPeasy);
            //EYE OF THE BEHOLSTER / THE BEHOLSTER
            List<string> mandatorySynergyItemsTHEEYES = new List<string>() { "nn:the_beholster", "eye_of_the_beholster" };
            CustomSynergies.Add("THE EYES", mandatorySynergyItemsTHEEYES);
            //HIVE HOLSTER / BEE HIVE / JAR OF BEES / HONEYCOMB / BUMBULLETS
            List<string> mandatorySynergyItemsHiveMind = new List<string>() { "nn:hive_holster" };
            List<string> optionalSynergyItemsHiveMind = new List<string>() { "bee_hive", "jar_of_bees", "bumbullets", "honeycomb" };
            CustomSynergies.Add("Hive Mind", mandatorySynergyItemsHiveMind, optionalSynergyItemsHiveMind);
            //BLANK BOOTS / BLANK BULLETS
            List<string> mandatorySynergyItemsMinorConfusion = new List<string>() { "nn:blank_boots", "blank_bullets" };
            CustomSynergies.Add("Minor Confusion", mandatorySynergyItemsMinorConfusion);
            //BLANK BOOTS / FULL METAL JACKET / TRUE BLANK / FALSE BLANK / SPARE BLANK / BLANK STARE
            List<string> mandatorySynergyItemsBlankItStatement = new List<string>() { "nn:blank_boots" };
            List<string> optionalSynergyItemsBlankItStatement = new List<string>() { "full_metal_jacket", "nn:true_blank", "nn:false_blank", "nn:spare_blank", "nn:blank_stare" };
            CustomSynergies.Add("Blank-It Statement", mandatorySynergyItemsBlankItStatement, optionalSynergyItemsBlankItStatement);
            //BLANK STARE / SPARE BLANK
            List<string> mandatorySynergyItemsBuy1Get1Free = new List<string>() { "nn:blank_stare", "nn:spare_blank" };
            CustomSynergies.Add("Buy 1 Get 1 Free", mandatorySynergyItemsBuy1Get1Free);
            //ROCKET MAN / ROCKET LAUNCHERS
            List<string> mandatorySynergyItemsHighAsAKite = new List<string>() { "nn:rocket_man" };
            List<string> optionalSynergyItemsHighAsAKite = new List<string>() { "stinger", "rpg", "the_exotic", "com4nd0", "rc_rocket", "yari_launcher", "void_core_cannon", "bullet_bore", "gungeon_ant" };
            CustomSynergies.Add("High as a Kite", mandatorySynergyItemsHighAsAKite, optionalSynergyItemsHighAsAKite);
            //ROCKET MAN / JETPACK
            List<string> mandatorySynergyItemsTimelessFlight = new List<string>() { "nn:rocket_man", "jetpack" };
            CustomSynergies.Add("Timeless Flight", mandatorySynergyItemsTimelessFlight);
            //HEART OF GOLD / ORGAN DONOR CARD
            List<string> mandatorySynergyItemsDoGooder = new List<string>() { "nn:heart_of_gold", "nn:organ_donor_card" };
            CustomSynergies.Add("Do-Gooder", mandatorySynergyItemsDoGooder);
            //CHANCE KIN EFFIGY / BABY GOOD CHANCE KIN
            List<string> mandatorySynergyItemsWorship = new List<string>() { "nn:baby_good_chance_kin", "nn:chance_effigy" };
            CustomSynergies.Add("Worship", mandatorySynergyItemsWorship);
            //BABY GOOD CHANCE KIN / BABY GOOD MIMIC / BABY GOOD SHELLETON
            List<string> mandatorySynergyItemsGoodLads = new List<string>() { "nn:baby_good_chance_kin" };
            List<string> optionalSynergyItemsGoodLads = new List<string>() { "baby_good_mimic", "baby_good_shelleton" };
            CustomSynergies.Add("Good Lads", mandatorySynergyItemsGoodLads, optionalSynergyItemsGoodLads);
            //GILDED LEAD / GILDED BULLETS
            List<string> mandatorySynergyItemsGuildoftheGilded = new List<string>() { "nn:gilded_lead", "gilded_bullets" };
            CustomSynergies.Add("Guild of the Gilded", mandatorySynergyItemsGuildoftheGilded);
            //GILDED LEAD / MEGA DOUSER
            List<string> mandatorySynergyItemsGoldenShower = new List<string>() { "nn:gilded_lead", "mega_douser" };
            CustomSynergies.Add("Golden Shower", mandatorySynergyItemsGoldenShower);
            //CHANCE KIN EFFIGY / KEYBULLET EFFIGY
            List<string> mandatorySynergyItemsLuckOfTheQuickdraw = new List<string>() { "nn:keybullet_effigy", "nn:chance_effigy" };
            CustomSynergies.Add("Luck of the Quickdraw", mandatorySynergyItemsLuckOfTheQuickdraw);
            //MAGICKE CAULDRON / SIXTH CHAMBER / YELLOW CHAMBER / HIGH KALIBER / KALIBER'S EYE
            List<string> mandatorySynergyItemsAMosteAccursedBrew = new List<string>() { "nn:alchemy_crucible" };
            List<string> optionalSynergyItemsAMosteAccursedBrew = new List<string>() { "sixth_chamber", "yellow_chamber", "high_kaliber", "nn:kalibers_eye" };
            CustomSynergies.Add("A Moste Accursed Brew", mandatorySynergyItemsAMosteAccursedBrew, optionalSynergyItemsAMosteAccursedBrew);
            //MAGICKE CAULDRON / CHAOS AMMOLET / CHAOS BULLETS
            List<string> mandatorySynergyItemsICANDOANYTHING = new List<string>() { "nn:alchemy_crucible" };
            List<string> optionalSynergyItemsICANDOANYTHING = new List<string>() { "chaos_bullets", "chaos_ammolet" };
            CustomSynergies.Add("I CAN DO ANYTHING", mandatorySynergyItemsICANDOANYTHING, optionalSynergyItemsICANDOANYTHING);
            //GUNPOWDER PHEROMONES
            List<string> mandatorySynergyItemsShotgunPheromones = new List<string>() { "nn:gunpowder_pheromones", "nn:shutdown_shells" };
            CustomSynergies.Add("Shotgun Pheromones", mandatorySynergyItemsShotgunPheromones);
            //CLAY SCULPTURE / GUNDROMEDA STRAIN
            List<string> mandatorySynergyItemsLynks = new List<string>() { "nn:clay_sculpture", "gundromeda_strain" };
            CustomSynergies.Add("Lynks", mandatorySynergyItemsLynks);
            //BOMBINOMICON / ICE BOMB
            List<string> mandatorySynergyItemsBlueBomber = new List<string>() { "nn:bombinomicon" };
            List<string> optionalSynergyItemsBlueBomber = new List<string>() { "ice_bomb", "ice_cube", "heart_of_ice" };
            CustomSynergies.Add("Blue Bomber", mandatorySynergyItemsBlueBomber, optionalSynergyItemsBlueBomber);
            //BOMBINOMICON / GRENADE LAUNCHER / LIL BOMBER
            List<string> mandatorySynergyItemsDemoman = new List<string>() { "nn:bombinomicon" };
            List<string> optionalSynergyItemsDemoman = new List<string>() { "grenade_launcher", "lil_bomber" };
            CustomSynergies.Add("A Bomby Nation", mandatorySynergyItemsDemoman, optionalSynergyItemsDemoman);
            //WALKIN AROUND WITH YER HEADS FULL O' EYEBALLS
            List<string> mandatorySynergyItemsDemoscreech = new List<string>() { "nn:bombinomicon", "eyepatch" };
            CustomSynergies.Add("HEADS FULL O' EYEBALLS", mandatorySynergyItemsDemoscreech);
            //SPRINGLOADED CHAMBER / WIND UP GUN
            List<string> mandatorySynergyItemsTinkerToys = new List<string>() { "nn:springloaded_chamber", "wind_up_gun" };
            CustomSynergies.Add("Tinker Toys", mandatorySynergyItemsTinkerToys);
            //SPRINGLOADED CHAMBER / BULLET TIME
            List<string> mandatorySynergyItemsClockpunk = new List<string>() { "nn:springloaded_chamber", "bullet_time" };
            CustomSynergies.Add("Clockpunk", mandatorySynergyItemsClockpunk);
            //TRACER ROUNDS / ORBITAL BULLETS
            List<string> mandatorySynergyItemsRingOfFire = new List<string>() { "nn:tracer_rounds", "orbital_bullets" };
            CustomSynergies.Add("Ring of Fire", mandatorySynergyItemsRingOfFire);
            //TRACER ROUNDS / CAMERA
            List<string> mandatorySynergyItemsHotPics = new List<string>() { "nn:tracer_rounds", "camera" };
            CustomSynergies.Add("Hot Pics", mandatorySynergyItemsHotPics);
            //TRACER ROUNDS / GRACEFUL GOOP
            List<string> mandatorySynergyItemsHotTempered = new List<string>() { "nn:tracer_rounds", "nn:graceful_goop" };
            CustomSynergies.Add("Hot Tempered", mandatorySynergyItemsHotTempered);
            //TRACER ROUNDS / FLARE GUN
            List<string> mandatorySynergyItemsEvenMoreVisible = new List<string>() { "nn:tracer_rounds", "flare_gun" };
            CustomSynergies.Add("Even More Visible!", mandatorySynergyItemsEvenMoreVisible);
            //GRACEFUL GOOP / CAMERA
            List<string> mandatorySynergyItemsBehindTheGoops = new List<string>() { "nn:graceful_goop", "camera" };
            CustomSynergies.Add("Behind the Goops", mandatorySynergyItemsBehindTheGoops);
            //GRACEFUL GOOP / TEAR JERKER
            List<string> mandatorySynergyItemsDoomedDescent = new List<string>() { "nn:graceful_goop", "tear_jerker" };
            CustomSynergies.Add("The Doomed Descent Is Over", mandatorySynergyItemsDoomedDescent);
            //GRACEFUL GOOP / GOLDEN ITEMS
            List<string> mandatorySynergyItemsComedyGold = new List<string>() { "nn:graceful_goop" };
            List<string> optionalSynergyItemsComedyGold = new List<string>() { "au_gun", "gilded_hydra", "nn:golden_armour", "nn:gold_guon_stone", "old_goldie", "gilded_bullets", "gold_junk", "gold_ammolet" };
            CustomSynergies.Add("Comedy Gold", mandatorySynergyItemsComedyGold, optionalSynergyItemsComedyGold);
            //VOODOOLLETS / BADGE
            List<string> mandatorySynergyItemsSoiree = new List<string>() { "nn:voodoollets", "badge" };
            CustomSynergies.Add("Soiree", mandatorySynergyItemsSoiree);
            //VOODOOLLETS / CHARMING ROUNDS
            List<string> mandatorySynergyItemsTheLoveYouCannotShowMe = new List<string>() { "nn:voodoollets", "charming_rounds" };
            CustomSynergies.Add("The Love You Cannot Show Me", mandatorySynergyItemsTheLoveYouCannotShowMe);
            //VOODOOLLETS / SPICE / FACE MELTER / LUTE / GUNZHENG / GHOST BULLETS / GUNSLINGERS ASHES / SPECTRE BULLETS
            List<string> mandatorySynergyItemsInTheMovement = new List<string>() { "nn:voodoollets" };
            List<string> optionalSynergyItemsInTheMovement = new List<string>() { "spice", "face_melter", "really_special_lute", "gunzheng", "ghost_bullets", "gunslingers_ashes", "nn:spectre_bullets" };
            CustomSynergies.Add("In The Movement...", mandatorySynergyItemsInTheMovement, optionalSynergyItemsInTheMovement);
            //VOODOOLLETS / DECOY
            List<string> mandatorySynergyItemsElysianNight = new List<string>() { "nn:voodoollets", "decoy" };
            CustomSynergies.Add("Elysian Night...", mandatorySynergyItemsElysianNight);
            //FROST KEY / SNOWBALLER
            List<string> mandatorySynergyItemsFrostKeyTheSnowman = new List<string>() { "nn:frost_key", "snowballer" };
            CustomSynergies.Add("Frost-Key The Snowman", mandatorySynergyItemsFrostKeyTheSnowman);
            //FROST KEY / DRAGUNFIRE / RING OF FIRE RESISTANCE / STAFF OF FIREPOWER / HIGH DRAGUNFIRE
            List<string> mandatorySynergyItemsFrostAndGunfire = new List<string>() { "nn:frost_key" };
            List<string> optionalSynergyItemsFrostAndGunfire = new List<string>() { "high_dragunfire", "dragunfire", "ring_of_fire_resistance", "staff_of_firepower" };
            CustomSynergies.Add("Frost and Gunfire", mandatorySynergyItemsFrostAndGunfire, optionalSynergyItemsFrostAndGunfire);
            //SUPERSONIC SHOTS / FACE MELTER
            List<string> mandatorySynergyItemsUnderground = new List<string>() { "nn:supersonic_shots", "face_melter" };
            CustomSynergies.Add("Underground", mandatorySynergyItemsUnderground);
            //ACCELERANT / FLAME HAND / PITCHFORK / PHOENIX
            List<string> mandatorySynergyItemsBurnBabyBurn = new List<string>() { "nn:accelerant" };
            List<string> optionalSynergyItemsBurnBabyBurn = new List<string>() { "pitchfork", "flame_hand", "phoenix", };
            CustomSynergies.Add("Burn, Baby Burn!", mandatorySynergyItemsBurnBabyBurn, optionalSynergyItemsBurnBabyBurn);
            //MR. FAHRENHEIT / MR ACCRETION JR
            List<string> mandatorySynergyItemsMercury = new List<string>() { "mr_accretion_jr" };
            List<string> optionalSynergyItemsMercury = new List<string>() { "nn:mr_fahrenheit", };
            CustomSynergies.Add("Mercury", mandatorySynergyItemsMercury, optionalSynergyItemsMercury);
            //MR. FAHRENHEIT / SUPERSONIC SHOTS
            List<string> mandatorySynergyItemsSupersonicMan = new List<string>() { "nn:supersonic_shots", "nn:mr_fahrenheit" };
            CustomSynergies.Add("Supersonic Man", mandatorySynergyItemsSupersonicMan);
            //SILVER AMMOLET / SILVER BULLETS
            List<string> mandatorySynergyItemsBlessedAreTheShriek = new List<string>() { "nn:silver_ammolet", "silver_bullets" };
            CustomSynergies.Add("Blessed are The Shriek", mandatorySynergyItemsBlessedAreTheShriek);
            //GUNSHARK / FAT BULLETS / TITAN BULLETS
            List<string> mandatorySynergyItemsMegashark = new List<string>() { "nn:gunshark" };
            List<string> optionalSynergyItemsMegashark = new List<string>() { "fat_bullets", "nn:titan_bullets", "megahand" };
            CustomSynergies.Add("Megashark", mandatorySynergyItemsMegashark, optionalSynergyItemsMegashark);
            //GUNSHARK / COMPRESSED AIR TANK
            List<string> mandatorySynergyItemsBloodInTheWater = new List<string>() { "nn:gunshark", "compressed_air_tank" };
            CustomSynergies.Add("Blood In The Water", mandatorySynergyItemsBloodInTheWater);
            //LOVE PISTOL / LOVE ITEMS
            List<string> mandatorySynergyItemsEverlastingLove = new List<string>() { "nn:love_pistol" };
            List<string> optionalSynergyItemsEverlastingLove = new List<string>() { "charming_rounds", "shotgun_full_of_love", "charm_horn", "charmed_bow" };
            CustomSynergies.Add("Everlasting Love", mandatorySynergyItemsEverlastingLove, optionalSynergyItemsEverlastingLove);
            //LOVE PISTOL / TOXIC ITEMS
            List<string> mandatorySynergyItemsToxicLove = new List<string>() { "nn:love_pistol" };
            List<string> optionalSynergyItemsToxicLove = new List<string>() { "trashcannon", "shotgun_full_of_hate" };
            CustomSynergies.Add("Toxic Love", mandatorySynergyItemsToxicLove, optionalSynergyItemsToxicLove);
            //SUNLIGHT JAVELIN / GUN GREASE
            List<string> mandatorySynergyItemsGreaseLightning = new List<string>() { "nn:gun_grease", "sunlight_javelin" };
            CustomSynergies.Add("Grease Lightning", mandatorySynergyItemsGreaseLightning);
            //SUNLIGHT JAVELIN / MAMA
            List<string> mandatorySynergyItemsGunderboltsAndLightning = new List<string>() { "nn:mama", "sunlight_javelin" };
            CustomSynergies.Add("Gunderbolts and Lightning", mandatorySynergyItemsGunderboltsAndLightning);
            //DISC GUN / CIGARETTES / BOMBER JACKET / DOUBLE VISION
            List<string> mandatorySynergyItemsEvenWorseChoices = new List<string>() { "nn:disc_gun" };
            List<string> optionalSynergyItemsEvenWorseChoices = new List<string>() { "cigarettes", "nn:bomber_jacket", "double_vision" };
            CustomSynergies.Add("Even Worse Choices", mandatorySynergyItemsEvenWorseChoices, optionalSynergyItemsEvenWorseChoices);
            //JUSTICE / MOUSTACHE
            List<string> mandatorySynergyItemsShotkeeper = new List<string>() { "nn:justice", "mustache" };
            CustomSynergies.Add("Shotkeeper", mandatorySynergyItemsShotkeeper);
            //FINGER GUNS / LICHY TRIGGER FINGER / +1 BULLETS
            List<string> mandatorySynergyItemsPolydactyly = new List<string>() { "nn:finger_guns" };
            List<string> optionalSynergyItemsPolydactyly = new List<string>() { "+1_bullets", "lichy_trigger_finger" };
            CustomSynergies.Add("Polydactyly", mandatorySynergyItemsPolydactyly, optionalSynergyItemsPolydactyly);
            //DISC GUN / GONNE
            List<string> mandatorySynergyItemsDiscworld = new List<string>() { "nn:disc_gun", "nn:gonne" };
            CustomSynergies.Add("Discworld", mandatorySynergyItemsDiscworld);
            //DISC GUN SUPER DISC FORME
            List<string> mandatorySynergyItemsSuperDisc = new List<string>() { "nn:disc_gun" };
            List<string> optionalSynergyItemsSuperDisc = new List<string>() { "+1_bullets", "buzzkill" };
            CustomSynergies.Add("Super Disc", mandatorySynergyItemsSuperDisc, optionalSynergyItemsSuperDisc);
            //ORGUN / HEART ITEMS
            List<string> mandatorySynergyItemsHeartAttack = new List<string>() { "nn:orgun" };
            List<string> optionalSynergyItemsHeartAttack = new List<string>() { "heart_holster", "heart_purse", "heart_bottle", "heart_lunchbox", "heart_locket", "heart_synthesizer", "heart_of_ice", "nn:cheese_heart", "nn:forsaken_heart", "nn:heart_of_gold", "nn:heart_padlock" };
            CustomSynergies.Add("Heart Attack", mandatorySynergyItemsHeartAttack, optionalSynergyItemsHeartAttack);
            //ORGUN / HELMET ITEMS - HEADACHE
            List<string> mandatorySynergyItemsHeadache = new List<string>() { "nn:orgun" };
            List<string> optionalSynergyItemsHeadache = new List<string>() { "gunknight_helmet", "blast_helmet", "crown_of_guns", "stone_dome", "clown_mask", "old_knights_helm", "nn:horned_helmet" };
            CustomSynergies.Add("Headache", mandatorySynergyItemsHeadache, optionalSynergyItemsHeadache);
            //OCTAGUN / MUSICAL WEAPONS
            List<string> mandatorySynergyItemsShapesNBeats = new List<string>() { "nn:octagun" };
            List<string> optionalSynergyItemsShapesNBeats = new List<string>() { "gunzheng", "really_special_lute", "face_melter", "metronome" };
            CustomSynergies.Add("Shapes N' Beats", mandatorySynergyItemsShapesNBeats, optionalSynergyItemsShapesNBeats);
            //MAGIC MISSILE / MAGIC ITEMS
            List<string> mandatorySynergyItemsMagicerMissile = new List<string>() { "nn:magic_missile" };
            List<string> optionalSynergyItemsMagicerMissile = new List<string>() { "nn:magic_quiver", "magic_bullets", "witch_pistol", "bundle_of_wands" };
            CustomSynergies.Add("Magic-er Missile", mandatorySynergyItemsMagicerMissile, optionalSynergyItemsMagicerMissile);
            //GREY GUON STONE
            List<string> mandatorySynergyItemsGreyerGuonStone = new List<string>() { "nn:grey_guon_stone" };
            List<string> optionalSynergyItemsGreyerGuonStone = new List<string>() { "+1_bullets", "amulet_of_the_pit_lord", "bullet_idol" };
            CustomSynergies.Add("Greyer Guon Stone", mandatorySynergyItemsGreyerGuonStone, optionalSynergyItemsGreyerGuonStone);
            //GOLF RIFLE / PURPLER
            List<string> mandatorySynergyItemsBirdie = new List<string>() { "nn:golf_rifle", "nn:purpler" };
            CustomSynergies.Add("Birdie!", mandatorySynergyItemsBirdie);
            //PURPLER / PURPLE PROSE
            List<string> mandatorySynergyItemsPurplest = new List<string>() { "nn:purple_prose", "nn:purpler" };
            CustomSynergies.Add("Purplest", mandatorySynergyItemsPurplest);
            //PURPLE PROSE / REALLY SPECIAL LUTE
            List<string> mandatorySynergyItemsEternalProse = new List<string>() { "nn:purple_prose", "really_special_lute" };
            CustomSynergies.Add("Eternal Prose", mandatorySynergyItemsEternalProse);
            //MINI GUN / SHOTGUNS
            List<string> mandatorySynergyItemsMiniShotgun = new List<string>() { "nn:mini_gun" };
            List<string> optionalSynergyItemsMiniShotgun = new List<string>() { "regular_shotgun", "old_goldie", "huntsman" };
            CustomSynergies.Add("Mini Shotgun", mandatorySynergyItemsMiniShotgun, optionalSynergyItemsMiniShotgun);
            //HAND MORTAR / HAND CANNON
            List<string> mandatorySynergyItemsGoodOldGuns = new List<string>() { "nn:hand_cannon", "nn:hand_mortar" };
            CustomSynergies.Add("Good Old Guns", mandatorySynergyItemsGoodOldGuns);
            //HAND MORTAR / OLD GOLDIE
            List<string> mandatorySynergyItemsTheClassics = new List<string>() { "nn:hand_mortar", "old_goldie" };
            CustomSynergies.Add("The Classics", mandatorySynergyItemsTheClassics);
            //SPEAR OF JUSTICE / CLONE / PIG / GUN SOUL
            List<string> mandatorySynergyItemsUndying = new List<string>() { "nn:spear_of_justice" };
            List<string> optionalSynergyItemsUndying = new List<string>() { "clone", "gun_soul", "pig" };
            CustomSynergies.Add("Undying", mandatorySynergyItemsUndying, optionalSynergyItemsUndying);
            //SPEAR OF JUSTICE / HEAVY BOOTS / SHIELD OF THE MAIDEN
            List<string> mandatorySynergyItemsNoRunningAway = new List<string>() { "nn:spear_of_justice" };
            List<string> optionalSynergyItemsNoRunningAway = new List<string>() { "heavy_boots", "shield_of_the_maiden" };
            CustomSynergies.Add("No Running Away!", mandatorySynergyItemsNoRunningAway, optionalSynergyItemsNoRunningAway);
            //STUN GUN / TRANK GUN
            List<string> mandatorySynergyItemsNonLethalSolutions = new List<string>() { "nn:stun_gun", "trank_gun" };
            CustomSynergies.Add("Non Lethal Solutions", mandatorySynergyItemsNonLethalSolutions);
            //DIAMOND GUN - BANE OF ARTHROPODS
            List<string> mandatorySynergyItemsBaneOfArthropods = new List<string>() { "nn:diamond_gun" };
            List<string> optionalSynergyItemsBaneOfArthropods = new List<string>() { "shotgrub", "grappling_hook", "chaos_bullets", "bumbullets", "bee_hive", "jar_of_bees", "nn:hive_holster", "honeycomb" };
            CustomSynergies.Add("Bane of Arthropods", mandatorySynergyItemsBaneOfArthropods, optionalSynergyItemsBaneOfArthropods);
            //DIAMOND GUN - SMITE
            List<string> mandatorySynergyItemsSmite = new List<string>() { "nn:diamond_gun" };
            List<string> optionalSynergyItemsSmite = new List<string>() { "ghost_bullets", "zombie_bullets", "skull_spitter", "vertebraek47" };
            CustomSynergies.Add("Smite", mandatorySynergyItemsSmite, optionalSynergyItemsSmite);
            //DIAMOND GUN - FIRE ASPECT
            List<string> mandatorySynergyItemsFireAspect = new List<string>() { "nn:diamond_gun" };
            List<string> optionalSynergyItemsFireAspect = new List<string>() { "hot_lead", "nn:tracer_rounds", "copper_ammolet", "phoenix", "nn:mr_fahrenheit" };
            CustomSynergies.Add("Fire Aspect", mandatorySynergyItemsFireAspect, optionalSynergyItemsFireAspect);
            //DIAMOND GUN - KNOCKBACK
            List<string> mandatorySynergyItemsKnockback = new List<string>() { "nn:diamond_gun" };
            List<string> optionalSynergyItemsKnockback = new List<string>() { "casey", "nn:diamond_bracelet", "boxing_glove", "ruby_bracelet", "nn:pearl_bracelet" };
            CustomSynergies.Add("Knockback", mandatorySynergyItemsKnockback, optionalSynergyItemsKnockback);
            //DIAMOND GUN - SHARPNESS
            List<string> mandatorySynergyItemsSharpness = new List<string>() { "nn:diamond_gun" };
            List<string> optionalSynergyItemsSharpness = new List<string>() { "knife_shield", "katana_bullets", "vorpal_bullets", "excaliber", "nn:longsword_shot" };
            CustomSynergies.Add("Sharpness", mandatorySynergyItemsSharpness, optionalSynergyItemsSharpness);
            //COPPER AMMOLET / LANTAKA - BROTHERS IN COPPER
            List<string> mandatorySynergyItemsBrothersInCopper = new List<string>() { "copper_ammolet" };
            List<string> optionalSynergyItemsBrothersInCopper = new List<string>() { "nn:lantaka" };
            CustomSynergies.Add("Brothers in Copper", mandatorySynergyItemsBrothersInCopper, optionalSynergyItemsBrothersInCopper);
            //BLOWGUN / POISON DART FROG
            List<string> mandatorySynergyItemsDartistry = new List<string>() { "nn:blowgun", "nn:poison_dart_frog" };
            CustomSynergies.Add("Dartistry", mandatorySynergyItemsDartistry);
            //BLOWGUN / DART RIFLE
            List<string> mandatorySynergyItemsOldAndNew = new List<string>() { "nn:blowgun", "nn:dart_rifle" };
            CustomSynergies.Add("Old and New", mandatorySynergyItemsOldAndNew);
            //KALASHNIRANG / BOOMERANG
            List<string> mandatorySynergyItemsRangaround = new List<string>() { "boomerang", "nn:kalashnirang" };
            CustomSynergies.Add("Rangaround", mandatorySynergyItemsRangaround);
            //BLASMASTER / GRAPPLING HOOK / BULLET TIME
            List<string> mandatorySynergyItemsPiratesLife = new List<string>() { "nn:blasmaster" };
            List<string> optionalSynergyItemsPiratesLife = new List<string>() { "grappling_hook", "bullet_time" };
            CustomSynergies.Add("Pirate's Life", mandatorySynergyItemsPiratesLife, optionalSynergyItemsPiratesLife);
            //DOGGUN / WOLF
            List<string> mandatorySynergyItemsDiscordAndRhyme = new List<string>() { "nn:doggun", "wolf" };
            CustomSynergies.Add("Discord and Rhyme", mandatorySynergyItemsDiscordAndRhyme);
            //DEMOLITIONIST / BOMB ITEMS
            List<string> mandatorySynergyItemsDemolitionMan = new List<string>() { "nn:demolitionist" };
            List<string> optionalSynergyItemsDemolitionMan = new List<string>() { "proximity_mine", "cluster_mine", "bomb", "nn:bombinomicon", "nn:bomber_jacket", "c4" };
            CustomSynergies.Add("Demolition Man", mandatorySynergyItemsDemolitionMan, optionalSynergyItemsDemolitionMan);
            //VISCERIFLE / OLD CREST
            List<string> mandatorySynergyItemsElderShot = new List<string>() { "nn:viscerifle", "old_crest" };
            CustomSynergies.Add("Elder Shot", mandatorySynergyItemsElderShot);
            //VISCERIFLE / MEAT SHIELD
            List<string> mandatorySynergyItemsBackMuscle = new List<string>() { "nn:viscerifle", "nn:meat_shield" };
            CustomSynergies.Add("Back Muscle", mandatorySynergyItemsBackMuscle);
            //VISCERIFLE / GREEN GUON STONE
            List<string> mandatorySynergyItemsDangerIsMyMiddleName = new List<string>() { "nn:viscerifle", "green_guon_stone" };
            CustomSynergies.Add("Danger Is My Middle Name", mandatorySynergyItemsDangerIsMyMiddleName);
            //CLOWN SHOTGUN / CLOWN MASK
            List<string> mandatorySynergyItemsClownTown = new List<string>() { "nn:clown_shotgun" };
            List<string> optionalSynergyItemsClownTown = new List<string>() { "clown_mask" };
            CustomSynergies.Add("Clown Town", mandatorySynergyItemsClownTown, optionalSynergyItemsClownTown);
            //LOVE POTION / VOODOOLLETS
            List<string> mandatorySynergyItemsOohEeeOohAhAh = new List<string>() { "nn:love_potion" };
            List<string> optionalSynergyItemsOohEeeOohAhAh = new List<string>() { "nn:voodoollets" };
            CustomSynergies.Add("Ooh Eee Ooh Ah Ah!", mandatorySynergyItemsOohEeeOohAhAh, optionalSynergyItemsOohEeeOohAhAh);
            //LOVE POTION / CHARM ITEMS
            List<string> mandatorySynergyItemsNumber9 = new List<string>() { "nn:love_potion" };
            List<string> optionalSynergyItemsNumber9 = new List<string>() { "charm_horn", "charming_rounds", "charmed_bow", "nn:purple_prose", "nn:love_pistol" };
            CustomSynergies.Add("Number 9", mandatorySynergyItemsNumber9, optionalSynergyItemsNumber9);
            //PISTA / BOW
            List<string> mandatorySynergyItemsPistolsRequiem = new List<string>() { "nn:pista", "bow" };
            CustomSynergies.Add("Pistols Requiem", mandatorySynergyItemsPistolsRequiem);
            //RISK RIFLE / MEDKIT / RATION / ORANGE
            List<string> mandatorySynergyItemsZeroRisk = new List<string>() { "nn:risk_rifle" };
            List<string> optionalSynergyItemsZeroRisk = new List<string>() { "medkit", "ration", "orange" };
            CustomSynergies.Add("Zero Risk", mandatorySynergyItemsZeroRisk, optionalSynergyItemsZeroRisk);
            //ARMOURED ARMOUR / ARMOR SYNTHESIZER / MEAT SHIELD / GOLDEN ARMOUR / GUNKNIGHT ARMOUR / POWER ARMOUR / ARMOR OF THORNS
            List<string> mandatorySynergyItemsArmouredArmouredArmour = new List<string>() { "nn:armoured_armour" };
            List<string> optionalSynergyItemsArmouredArmouredArmour = new List<string>() { "armor_synthesizer", "nn:meat_shield", "nn:golden_armour", "gunknight_armor", "nn:power_armour", "armor_of_thorns" };
            CustomSynergies.Add("Armoured Armoured Armour", mandatorySynergyItemsArmouredArmouredArmour, optionalSynergyItemsArmouredArmouredArmour);
            //MULTIPLICATOR / TABLE TECHS
            List<string> mandatorySynergyItemsTimesTables = new List<string>() { "nn:multiplicator" };
            List<string> optionalSynergyItemsTimesTables = new List<string>() { "portable_table_device", "table_tech_sight", "table_tech_money", "table_tech_rocket", "table_tech_shotgun", "table_tech_heat", "table_tech_rage", "table_tech_blanks", "table_tech_stun", "nn:table_tech_table", "nn:table_tech_invulnerability", "nn:table_tech_speed", "nn:table_tech_ammo", "nn:table_tech_guon", "nn:tabullets" };
            CustomSynergies.Add("Times Tables", mandatorySynergyItemsTimesTables, optionalSynergyItemsTimesTables);
            #endregion

            #region SynergisingUpdateSynergies
            //SYNERGIES ADDED IN THE SYNERGISING
            //BOXING GLOVE / BASHING BULLETS
            List<string> mandatorySynergyItemsGunPunchMan = new List<string>() { "nn:bashing_bullets", "boxing_glove" };
            CustomSynergies.Add("Gun Punch Man", mandatorySynergyItemsGunPunchMan);
            //BLANK STARE / SPARE KEY
            List<string> mandatorySynergyItemsItsAKeyper = new List<string>() { "nn:blank_stare", "nn:spare_key" };
            CustomSynergies.Add("It's a Key-per!", mandatorySynergyItemsItsAKeyper);
            //BLANK KEY / SPARE BLANK
            List<string> mandatorySynergyItemsAnotherTime = new List<string>() { "nn:blank_key", "nn:spare_blank" };
            CustomSynergies.Add("...Another Time", mandatorySynergyItemsAnotherTime);
            //GLASS CHAMBER / GLASS ROUNDS
            List<string> mandatorySynergyItemsGlassworks = new List<string>() { "nn:glass_chamber", "nn:glass_rounds" };
            CustomSynergies.Add("Glassworks", mandatorySynergyItemsGlassworks);
            //HIVE HOLSTER / STINGER
            List<string> mandatorySynergyItemsStingInTheTail = new List<string>() { "nn:hive_holster", "stinger" };
            CustomSynergies.Add("Sting in the Tail", mandatorySynergyItemsStingInTheTail);
            //MUTAGEN / NANOMACHINES
            List<string> mandatorySynergyItemsNanobiology = new List<string>() { "nn:mutagen", "nanomachines" };
            CustomSynergies.Add("Nanobiology", mandatorySynergyItemsNanobiology);
            //MUTAGEN / ANTIBODY
            List<string> mandatorySynergyItemsUltraserum = new List<string>() { "nn:mutagen", "antibody" };
            CustomSynergies.Add("Ultraserum", mandatorySynergyItemsUltraserum);
            //SPARE KEY / SPARE BLANK
            List<string> mandatorySynergyItemsSpareProtection = new List<string>() { "nn:spare_blank", "nn:spare_key" };
            CustomSynergies.Add("Spare Protection", mandatorySynergyItemsSpareProtection);
            //LOOSE CHANGE / RUSTY CASING
            List<string> mandatorySynergyItemsLostNeverFound = new List<string>() { "nn:loose_change", "nn:rusty_casing" };
            CustomSynergies.Add("Lost, Never Found", mandatorySynergyItemsLostNeverFound);
            //BULLET KIN PLUSHIE / MICROTRANSACTION GUN / TSHIRT CANNON
            List<string> mandatorySynergyItemsMerchandising = new List<string>() { "nn:bullet_kin_plushie" };
            List<string> optionalSynergyItemsMerchandising = new List<string>() { "microtransaction_gun", "t_shirt_cannon" };
            CustomSynergies.Add("Merchandising", mandatorySynergyItemsMerchandising, optionalSynergyItemsMerchandising);
            #endregion

            #region OldSynergiesMigratedHere
            // OLD SYNERGIES MIGRATED HERE
            List<string> mandatorySynergyItemsSoundBarrier = new List<string>() { "nn:table_tech_speed", "nn:speed_potion" };
            CustomSynergies.Add("Sound Barrier", mandatorySynergyItemsSoundBarrier);
            //YELLOW CHAMBER / KALIBERS EYE
            List<string> mandatorySynergyItemsAllSeeing = new List<string>() { "nn:kalibers_eye", "yellow_chamber" };
            CustomSynergies.Add("All Seeing", mandatorySynergyItemsAllSeeing);
            //MINERS BULLETS / EYE ITEMS
            List<string> mandatorySynergyItemsEyeOfTheSpider = new List<string>() { "nn:miners_bullets" };
            List<string> optionalSynergyItemsEyeOfTheSpider = new List<string>() { "nn:kalibers_eye", "bloody_eye", "rolling_eye", "eye_of_the_beholster" };
            CustomSynergies.Add("Eye of the Spider", mandatorySynergyItemsEyeOfTheSpider, optionalSynergyItemsEyeOfTheSpider);
            //LUCKY COIN / LOOT ITEMS
            List<string> mandatorySynergyItemsProsperity = new List<string>() { "nn:lucky_coin" };
            List<string> optionalSynergyItemsProsperity = new List<string>() { "nn:lump_of_space_metal", "nn:loose_change", "coin_crown", "iron_coin", "gold_junk", "table_tech_money" };
            CustomSynergies.Add("Prosperity", mandatorySynergyItemsProsperity, optionalSynergyItemsProsperity);
            //MINERS BULLETS / LOOT ITEMS
            List<string> mandatorySynergyItemsMiningAway = new List<string>() { "nn:miners_bullets" };
            List<string> optionalSynergyItemsMiningAway = new List<string>() { "nn:lump_of_space_metal", "mine_cutter" };
            CustomSynergies.Add("Miiiining Away~", mandatorySynergyItemsMiningAway, optionalSynergyItemsMiningAway);
            //BLOODTHIRSTY BULLETS / SILVER BULLETS
            List<string> mandatorySynergyItemsFunnySyngeryName = new List<string>() { "nn:bloodthirsty_bullets", "silver_bullets" };
            CustomSynergies.Add("[todo: Add funny synergy name]", mandatorySynergyItemsFunnySyngeryName);
            //IDENTITY CRISIS / WINGMAN / WOLF /UNITY
            List<string> mandatorySynergyItemsThoseWeLeftBehind = new List<string>() { "nn:identity_crisis" };
            List<string> optionalSynergyItemsThoseWeLeftBehind = new List<string>() { "wingman", "wolf", "unity" };
            CustomSynergies.Add("Those We Left Behind", mandatorySynergyItemsThoseWeLeftBehind, optionalSynergyItemsThoseWeLeftBehind);
            //IDENTITY CRISIS / GUN SOUL / CLONE / SHADOW CLONE
            List<string> mandatorySynergyItemsNewRunNewMe = new List<string>() { "nn:identity_crisis" };
            List<string> optionalSynergyItemsNewRunNewMe = new List<string>() { "clone", "shadow_clone", "gun_soul" };
            CustomSynergies.Add("New run, New me", mandatorySynergyItemsNewRunNewMe, optionalSynergyItemsNewRunNewMe);
            //FALSE BLANK / FORSAKEN HEART
            List<string> mandatorySynergyItemsFalsePretences = new List<string>() { "nn:false_blank", "nn:forsaken_heart" };
            CustomSynergies.Add("False Pretences", mandatorySynergyItemsFalsePretences);
            //LEWIS / BATTLE STANDARD
            List<string> mandatorySynergyItemsRallyTheSlacker = new List<string>() { "nn:lewis", "battle_standard" };
            CustomSynergies.Add("Rally The Slacker", mandatorySynergyItemsRallyTheSlacker);
            //HEART PADLOCK / HEART LOCKET
            List<string> mandatorySynergyItemsAllLockedUp = new List<string>() { "nn:heart_padlock", "heart_locket" };
            CustomSynergies.Add("All Locked Up", mandatorySynergyItemsAllLockedUp);
            //HEART PADLOCK / SHELLETON KEY
            List<string> mandatorySynergyItemsKeyDeath = new List<string>() { "nn:heart_padlock", "shelleton_key" };
            CustomSynergies.Add("Key Death", mandatorySynergyItemsKeyDeath);
            #endregion

            #region BigUpdate (1.14) Synergies
            //PENCIL / MIGHTIER THAN THE GUN
            List<string> mandatorySynergyItemsMightierThanTheGun = new List<string>() { "nn:pencil" };
            List<string> optionalSynergyItemsMightierThanTheGun = new List<string>() { "nn:gun_grease", "nn:paintball_gun", "ballot" };
            CustomSynergies.Add("Mightier Than The Gun", mandatorySynergyItemsMightierThanTheGun, optionalSynergyItemsMightierThanTheGun);
            //SHOWDOWN / CHARM ITEMS
            List<string> mandatorySynergyItemsFrenemies = new List<string>() { "nn:showdown" };
            List<string> optionalSynergyItemsFrenemies = new List<string>() { "charming_rounds", "charm_horn", "charmed_bow", "shotgun_full_of_love", "nn:love_pistol", "nn:purple_prose", "nn:love_potion" };
            CustomSynergies.Add("Frenemies", mandatorySynergyItemsFrenemies, optionalSynergyItemsFrenemies);
            //SHOWDOWN / SPITEFUL ITEMS
            List<string> mandatorySynergyItemsDirtyTricks = new List<string>() { "nn:showdown" };
            List<string> optionalSynergyItemsDirtyTricks = new List<string>() { "shotgun_full_of_hate", "poison_vial", "nn:mistake_bullets" };
            CustomSynergies.Add("Dirty Tricks", mandatorySynergyItemsDirtyTricks, optionalSynergyItemsDirtyTricks);
            //VACUUM GUN / TRANSMOG ITEMS
            List<string> mandatorySynergyItemsChickadoo = new List<string>() { "nn:vacuum_gun" };
            List<string> optionalSynergyItemsChickadoo = new List<string>() { "magic_bullets", "bundle_of_wands", "witch_pistol", "hexagun", "nn:fowl_ring" };
            CustomSynergies.Add("Chickadoo", mandatorySynergyItemsChickadoo, optionalSynergyItemsChickadoo);
            //BOOKLLET / ANTIMAGIC ROUNDS / MAGIC BULLETS / +1 BULLETS / ALCHEMY CRUCIBLE
            List<string> mandatorySynergyItemsAdvancedAmmomancy = new List<string>() { "nn:bookllet" };
            List<string> optionalSynergyItemsAdvancedAmmomancy = new List<string>() { "magic_bullets", "nn:antimagic_rounds", "+1_bullets", "nn:alchemy_crucible" };
            CustomSynergies.Add("Advanced Ammomancy", mandatorySynergyItemsAdvancedAmmomancy, optionalSynergyItemsAdvancedAmmomancy);
            //REKEYTER / SHELLETON KEY
            List<string> mandatorySynergyItemsReShelletonKeyter = new List<string>() { "nn:rekeyter", "shelleton_key" };
            CustomSynergies.Add("ReShelletonKeyter", mandatorySynergyItemsReShelletonKeyter);
            //AM0 / Spread Items
            List<string> mandatorySynergyItemsSpreadshot = new List<string>() { "nn:am0" };
            List<string> optionalSynergyItemsSpreadshot = new List<string>() { "unity", "flak_bullets", "eyepatch", "sense_of_direction", "nn:clown_shotgun" };
            CustomSynergies.Add("Spreadshot", mandatorySynergyItemsSpreadshot, optionalSynergyItemsSpreadshot);
            //AM0 / MENGER AMMO BOX
            List<string> mandatorySynergyItemsMengerClip = new List<string>() { "nn:am0", "nn:menger_ammo_box" };
            CustomSynergies.Add("Menger Clip", mandatorySynergyItemsMengerClip);
            //BULLET BLADE / GHOST ITEMS
            List<string> mandatorySynergyItemsGhostSword = new List<string>() { "nn:bullet_blade" };
            List<string> optionalSynergyItemsGhostSword = new List<string>() { "ghost_bullets", "nn:spectre_bullets", "zombie_bullets", "nn:bombinomicon", "gunslingers_ashes" };
            CustomSynergies.Add("GHOST SWORD!!!", mandatorySynergyItemsGhostSword, optionalSynergyItemsGhostSword);
            //CARDINALS  MITRE / BOOTS
            List<string> mandatorySynergyItemsHolySocks = new List<string>() { "nn:cardinals_mitre" };
            List<string> optionalSynergyItemsHolySocks = new List<string>() { "ballistic_boots", "heavy_boots", "gunboots", "springheel_boots", "rat_boots", "nn:cyclone_boots", "nn:legboot", "nn:blank_boots" };
            CustomSynergies.Add("Holy Socks", mandatorySynergyItemsHolySocks, optionalSynergyItemsHolySocks);
            //DISPLACER CANNON  / DELETE THIS
            List<string> mandatorySynergyItemsDeleteThis = new List<string>() { "nn:displacer_cannon" };
            List<string> optionalSynergyItemsDeleteThis = new List<string>() { "chest_teleporter", "teleporter_prototype", "railgun", "prototype_railgun" };
            CustomSynergies.Add("Delete This", mandatorySynergyItemsDeleteThis, optionalSynergyItemsDeleteThis);
            //DISPLACER CANNON / LOOT VORTEX
            List<string> mandatorySynergyItemsLootVortex = new List<string>() { "nn:displacer_cannon" };
            List<string> optionalSynergyItemsLootVortex = new List<string>() { "nn:menger_ammo_box", "nn:bloody_ammo", "nn:spare_blank", "nn:spare_key", "nn:rusty_casing", "nn:loose_change" };
            CustomSynergies.Add("Loot Vortex", mandatorySynergyItemsLootVortex, optionalSynergyItemsLootVortex);
            //DESK FAN / BALLOON GUN
            List<string> mandatorySynergyItemsHighSetting = new List<string>() { "nn:desk_fan" };
            List<string> optionalSynergyItemsHighSetting = new List<string>() { "balloon_gun" };
            CustomSynergies.Add("Fresh Air", mandatorySynergyItemsHighSetting, optionalSynergyItemsHighSetting);
            //FALSE BLANK / GLASS AMMOLET
            List<string> mandatorySynergyItemsTransparentLies = new List<string>() { "nn:false_blank", "nn:glass_ammolet" };
            CustomSynergies.Add("Transparent Lies", mandatorySynergyItemsTransparentLies);
            //CHEM GRENADE / TOXIC ITEMS
            List<string> mandatorySynergyItemsToxicShock = new List<string>() { "nn:chem_grenade" };
            List<string> optionalSynergyItemsToxicShock = new List<string>() { "poison_vial", "irradiated_lead", "gamma_ray", "nn:graceful_goop", "gas_mask", "hazmat_suit", "monster_blood" };
            CustomSynergies.Add("Toxic Shock", mandatorySynergyItemsToxicShock, optionalSynergyItemsToxicShock);
            //BLOMBK / BLANK ITEMS
            List<string> mandatorySynergyItemsAtomicBlombk = new List<string>() { "nn:blombk" };
            List<string> optionalSynergyItemsAtomicBlombk = new List<string>() { "elder_blank", "nn:true_blank", "nn:false_blank", "blank_bullets", "full_metal_jacket", "nn:blank_boots", "big_boy" };
            CustomSynergies.Add("Atomic Blombk", mandatorySynergyItemsAtomicBlombk, optionalSynergyItemsAtomicBlombk);
            //TOOLBOX / RELIGIOUS ITEMS
            List<string> mandatorySynergyItemsHisGrace = new List<string>() { "nn:toolbox" };
            List<string> optionalSynergyItemsHisGrace = new List<string>() { "nn:cardinals_mitre", "holey_grail", "blood_brooch", "yellow_chamber" };
            CustomSynergies.Add("His Grace", mandatorySynergyItemsHisGrace, optionalSynergyItemsHisGrace);
            //TOOLBOX / TOOL ITEMS
            List<string> mandatorySynergyItemsSharpestToolInTheShed = new List<string>() { "nn:toolbox" };
            List<string> optionalSynergyItemsSharpestToolInTheShed = new List<string>() { "drill", "h4mmer", "cobalt_hammer", "anvillain", "wood_beam", "nn:pencil" };
            CustomSynergies.Add("Sharpest Tool In The Shed", mandatorySynergyItemsSharpestToolInTheShed, optionalSynergyItemsSharpestToolInTheShed);
            //UNPREDICTABULLETS - CAUSE AND EFFECT
            List<string> mandatorySynergyItemsCauseAndEffect = new List<string>() { "nn:unpredictabullets" };
            List<string> optionalSynergyItemsCauseAndEffect = new List<string>() { "mass_shotgun", "chaos_bullets", "chaos_ammolet", "nn:protean" };
            CustomSynergies.Add("Cause And Effect", mandatorySynergyItemsCauseAndEffect, optionalSynergyItemsCauseAndEffect);
            //FUNGO CANNON / MYSHELLIUM
            List<string> mandatorySynergyItemsMyshellium = new List<string>() { "nn:fungo_cannon" };
            List<string> optionalSynergyItemsMyshellium = new List<string>() { "nn:error_shells", "nn:rando_rounds", "nn:shutdown_shells", "nn:blight_shell", "shell" };
            CustomSynergies.Add("Myshellium", mandatorySynergyItemsMyshellium, optionalSynergyItemsMyshellium);
            //FUNGO CANNON / HUNTER SPORES
            List<string> mandatorySynergyItemsHunterSpores = new List<string>() { "nn:fungo_cannon" };
            List<string> optionalSynergyItemsHunterSpores = new List<string>() { "wolf", "nn:doggun", "rattler", "trick_gun", "colt_1851" };
            CustomSynergies.Add("Hunter Spores", mandatorySynergyItemsHunterSpores, optionalSynergyItemsHunterSpores);
            //FUNGO CANNON / SPORE LAUNCHER
            List<string> mandatorySynergyItemsEnspore = new List<string>() { "nn:fungo_cannon", "nn:spore_launcher" };
            CustomSynergies.Add("Enspore!", mandatorySynergyItemsEnspore);
            //G20 / MULTIPLICATOR
            List<string> mandatorySynergyItemsRerollinRollinRollin = new List<string>() { "nn:g20" };
            List<string> optionalSynergyItemsRerollinRollinRollin = new List<string>() { "nn:multiplicator", "scouter" };
            CustomSynergies.Add("Rerollin Rollin Rollin", mandatorySynergyItemsRerollinRollinRollin, optionalSynergyItemsRerollinRollinRollin);
            //VOID MARSHAL / JUDGE - COURT MARSHAL
            List<string> mandatorySynergyItemsCourtMarshal = new List<string>() { "the_judge", "void_marshal" };
            CustomSynergies.Add("Court Marshal", mandatorySynergyItemsCourtMarshal);
            //GRANDFATHER GLOCK / SIX ITEMS
            List<string> mandatorySynergyItemsSixSharp = new List<string>() { "nn:grandfather_glock" };
            List<string> optionalSynergyItemsSixSharp = new List<string>() { "sixth_chamber", "high_kaliber" };
            CustomSynergies.Add("Six Sharp", mandatorySynergyItemsSixSharp, optionalSynergyItemsSixSharp);
            //GRANDFATHER GLOCK / ONE ITEMS
            List<string> mandatorySynergyItemsKellysEye = new List<string>() { "nn:grandfather_glock" };
            List<string> optionalSynergyItemsKellysEye = new List<string>() { "nn:1_shot", "+1_bullets" };
            CustomSynergies.Add("Kellys Eye", mandatorySynergyItemsKellysEye, optionalSynergyItemsKellysEye);
            //LOCKDOWN BULLETS / ALPHA BULLETS
            List<string> mandatorySynergyItemsAddedEffectLockdown = new List<string>() { "nn:lockdown_bullets", "alpha_bullets" };
            CustomSynergies.Add("Added Effect - Lockdown", mandatorySynergyItemsAddedEffectLockdown);
            //LOCKDOWN BULLETS / OMEGA BULLETS
            List<string> mandatorySynergyItemsMoonstoneWeapon = new List<string>() { "nn:lockdown_bullets", "omega_bullets" };
            CustomSynergies.Add("Moonstone Weapon", mandatorySynergyItemsMoonstoneWeapon);
            //BOOKLLET / LOREBOOK
            List<string> mandatorySynergyItemsLibrarian = new List<string>() { "nn:lorebook", "nn:bookllet" };
            CustomSynergies.Add("Librarian", mandatorySynergyItemsLibrarian);
            //LOREBOOK / KNIGHTLY ITEMS
            List<string> mandatorySynergyItemsLevel20Fighter = new List<string>() { "nn:lorebook" };
            List<string> optionalSynergyItemsLevel20Fighter = new List<string>() { "excaliber", "vorpal_gun", "vorpal_bullets", "knights_gun", "old_knights_flask", "nn:knightly_bullets" };
            CustomSynergies.Add("Level 20 Fighter", mandatorySynergyItemsLevel20Fighter, optionalSynergyItemsLevel20Fighter);
            //LOREBOOK / WIZARD ITEMS
            List<string> mandatorySynergyItemsLevel20Wizard = new List<string>() { "nn:lorebook" };
            List<string> optionalSynergyItemsLevel20Wizard = new List<string>() { "nn:magic_missile", "witch_pistol", "magic_bullets", "hexagun", "bundle_of_wands" };
            CustomSynergies.Add("Level 20 Wizard", mandatorySynergyItemsLevel20Wizard, optionalSynergyItemsLevel20Wizard);
            //LOREBOOK / BARD ITEMS
            List<string> mandatorySynergyItemsLevel20Bard = new List<string>() { "nn:lorebook" };
            List<string> optionalSynergyItemsLevel20Bard = new List<string>() { "really_special_lute", "face_melter", "metronome", "gunzheng" };
            CustomSynergies.Add("Level 20 Bard", mandatorySynergyItemsLevel20Bard, optionalSynergyItemsLevel20Bard);
            //LOREBOOK / ROGUE ITEMS
            List<string> mandatorySynergyItemsLevel20Rogue = new List<string>() { "nn:lorebook" };
            List<string> optionalSynergyItemsLevel20Rogue = new List<string>() { "smoke_bomb", "knife_shield", "kruller_glaive", "grappling_hook", "shadow_bullets", "box" };
            CustomSynergies.Add("Level 20 Rogue", mandatorySynergyItemsLevel20Rogue, optionalSynergyItemsLevel20Rogue);
            //RING OF AMMO REDEMTION / AMMO BELT / DRUM CLIP
            List<string> mandatorySynergyItemsAmmoEconomyInflation = new List<string>() { "nn:ring_of_ammo_redemption" };
            List<string> optionalSynergyItemsAmmoEconomyInflation = new List<string>() { "ammo_belt", "drum_clip", "utility_belt" };
            CustomSynergies.Add("Ammo Economy Inflation", mandatorySynergyItemsAmmoEconomyInflation, optionalSynergyItemsAmmoEconomyInflation);
            //RISKY RING / RISK RIFLE
            List<string> mandatorySynergyItemsDoubleRiskDoubleReward = new List<string>() { "nn:risky_ring", "nn:risk_rifle" };
            CustomSynergies.Add("Double Risk, Double Reward", mandatorySynergyItemsDoubleRiskDoubleReward);
            //RISKY RING / CROWN ITEMS
            List<string> mandatorySynergyItemsUltraMutation = new List<string>() { "nn:risky_ring" };
            List<string> optionalSynergyItemsUltraMutation = new List<string>() { "crown_of_guns", "nn:goomperors_crown" };
            CustomSynergies.Add("Ultra Mutation", mandatorySynergyItemsUltraMutation, optionalSynergyItemsUltraMutation);
            //WARP BULLETS / JOLTER / DISPLACER CANNON / TELEPORTER PROTOTYPE / CHEST TELEPORTER
            List<string> mandatorySynergyItemsLostInTheWarp = new List<string>() { "nn:warp_bullets" };
            List<string> optionalSynergyItemsLostInTheWarp = new List<string>() { "jolter", "nn:displacer_cannon", "teleporter_prototype", "chest_teleporter" };
            CustomSynergies.Add("Corrupted By Warp", mandatorySynergyItemsLostInTheWarp, optionalSynergyItemsLostInTheWarp);
            //ICICLE / COLD ITEMS
            List<string> mandatorySynergyItemsYouNeedToChill = new List<string>() { "nn:icicle" };
            List<string> optionalSynergyItemsYouNeedToChill = new List<string>() { "glacier", "frost_bullets", "nn:frost_key", "ice_cube", "snowballer" };
            CustomSynergies.Add("You Need To Chill", mandatorySynergyItemsYouNeedToChill, optionalSynergyItemsYouNeedToChill);
            //HOT GLUE GUN / STICK IN THE MUD
            List<string> mandatorySynergyItemsStickInTheMud = new List<string>() { "nn:hot_glue_gun" };
            List<string> optionalSynergyItemsStickInTheMud = new List<string>() { "pig", "nn:lockdown_bullets" };
            CustomSynergies.Add("Stick In The Mud", mandatorySynergyItemsStickInTheMud, optionalSynergyItemsStickInTheMud);
            //HOT GLUE GUN / HEAT STRESS
            List<string> mandatorySynergyItemsHeatStress = new List<string>() { "nn:hot_glue_gun" };
            List<string> optionalSynergyItemsHeatStress = new List<string>() { "hot_lead", "copper_ammolet", "nn:dragun_scale", "nn:flame_chamber", "phoenix" };
            CustomSynergies.Add("Heat Stress", mandatorySynergyItemsHeatStress, optionalSynergyItemsHeatStress);
            //HOT GLUE GUN / BALLOON GUN
            List<string> mandatorySynergyItemsGlueGunner = new List<string>() { "nn:hot_glue_gun" };
            List<string> optionalSynergyItemsGlueGunner = new List<string>() { "balloon_gun" };
            CustomSynergies.Add("Glue Gunner", mandatorySynergyItemsGlueGunner, optionalSynergyItemsGlueGunner);
            //FLAMECHAMBER / FLAME ITEMS
            List<string> mandatorySynergyItemsPyromaniac = new List<string>() { "nn:flame_chamber" };
            List<string> optionalSynergyItemsPyromaniac = new List<string>() { "hot_lead", "copper_ammolet", "phoenix" };
            CustomSynergies.Add("Pyromaniac", mandatorySynergyItemsPyromaniac, optionalSynergyItemsPyromaniac);
            //HONEY POT
            List<string> mandatorySynergyItemsHoneyImHome = new List<string>() { "nn:honey_pot" };
            List<string> optionalSynergyItemsHoneyImHome = new List<string>() { "bee_hive", "honeycomb", "nn:hive_holster" };
            CustomSynergies.Add("Honey, I'm Home!", mandatorySynergyItemsHoneyImHome, optionalSynergyItemsHoneyImHome);
            //G-SWITCH / PROTOTYPE ITEMS
            List<string> mandatorySynergyItemsPrototypeForm = new List<string>() { "nn:g-switch" };
            List<string> optionalSynergyItemsPrototypeForm = new List<string>() { "prototype_railgun", "teleporter_prototype" };
            CustomSynergies.Add("Prototype Form", mandatorySynergyItemsPrototypeForm, optionalSynergyItemsPrototypeForm);
            //MAIDEN RIFLE / SHIELD OF THE MAIDEN / MAIDEN SHAPED BOX
            List<string> mandatorySynergyItemsDoubleMaiden = new List<string>() { "nn:maiden_rifle" };
            List<string> optionalSynergyItemsDoubleMaiden = new List<string>() { "nn:maiden-shaped_box", "shield_of_the_maiden" };
            CustomSynergies.Add("Double Maiden", mandatorySynergyItemsDoubleMaiden, optionalSynergyItemsDoubleMaiden);
            //BULLATTERER / GHOST ITEMS
            List<string> mandatorySynergyItemsSpiratterer = new List<string>() { "nn:bullatterer" };
            List<string> optionalSynergyItemsSpiratterer = new List<string>() { "ghost_bullets", "nn:spectre_bullets", "thompson", "gunslingers_ashes" };
            CustomSynergies.Add("Spiratterer", mandatorySynergyItemsSpiratterer, optionalSynergyItemsSpiratterer);
            //BULLATTERER / SHOTGUNS
            List<string> mandatorySynergyItemsShotgatterer = new List<string>() { "nn:bullatterer" };
            List<string> optionalSynergyItemsShotgatterer = new List<string>() { "regular_shotgun", "nn:justice", "nn:ranger", "nn:clown_shotgun", "blooper", "blunderbuss", "elephant_gun", "huntsman", "old_goldie", "pulse_cannon", "siren", "tangler", "void_shotgun", "zilla_shotgun" };
            CustomSynergies.Add("Shotgatterer", mandatorySynergyItemsShotgatterer, optionalSynergyItemsShotgatterer);
            //BULLATTERER / GRENADES / BOMBS
            List<string> mandatorySynergyItemsGrenatterer = new List<string>() { "nn:bullatterer" };
            List<string> optionalSynergyItemsGrenatterer = new List<string>() { "flak_bullets", "grenade_launcher", "bomb", "lil_bomber", "rpg" };
            CustomSynergies.Add("Grenatterer", mandatorySynergyItemsGrenatterer, optionalSynergyItemsGrenatterer);
            //BULLATTERER / CROWN OF GUNS / GOOMPERORS CROWN
            List<string> mandatorySynergyItemsKingBullatterer = new List<string>() { "nn:bullatterer" };
            List<string> optionalSynergyItemsKingBullatterer = new List<string>() { "crown_of_guns", "coin_crown", "nn:goomperors_crown" };
            CustomSynergies.Add("King Bullatterer", mandatorySynergyItemsKingBullatterer, optionalSynergyItemsKingBullatterer);
            //SHROOMED GUN / FINGER ITEMS
            List<string> mandatorySynergyItemsBallisticFingers = new List<string>() { "nn:shroomed_gun" };
            List<string> optionalSynergyItemsBallisticFingers = new List<string>() { "lichy_trigger_finger", "nn:finger_guns" };
            CustomSynergies.Add("Ballistic Fingers", mandatorySynergyItemsBallisticFingers, optionalSynergyItemsBallisticFingers);
            //DYNAMITE LAUNCHER / EXPLOSIVE ITEMS
            List<string> mandatorySynergyItemsNobelEffort = new List<string>() { "nn:dynamite_launcher" };
            List<string> optionalSynergyItemsNobelEffort = new List<string>() { "nn:gunpowder_pheromones", "nn:nitroglycylinder" };
            CustomSynergies.Add("Nobel Effort", mandatorySynergyItemsNobelEffort, optionalSynergyItemsNobelEffort);
            //RIOT GUN / CROWD SUPRESSION
            List<string> mandatorySynergyItemsCrowdSupression = new List<string>() { "nn:riot_gun" };
            List<string> optionalSynergyItemsCrowdSupression = new List<string>() { "chaff_grenade", "mega_douser", "nn:wicker_ammolet" };
            CustomSynergies.Add("Crowd Supression", mandatorySynergyItemsCrowdSupression, optionalSynergyItemsCrowdSupression);
            //BEASTCLAW / DISPLACER CANNON
            List<string> mandatorySynergyItemsMisfireCannon = new List<string>() { "nn:displacer_cannon", "nn:beastclaw" };
            CustomSynergies.Add("Misfire Cannon", mandatorySynergyItemsMisfireCannon);
            //BEASTCLAW / PENCIL
            List<string> mandatorySynergyItemsFreehand = new List<string>() { "nn:pencil", "nn:beastclaw" };
            CustomSynergies.Add("Freehand", mandatorySynergyItemsFreehand);
            //TABLE TECH GUON / MAHOGUNY
            List<string> mandatorySynergyItemsMahogunyGuonStones = new List<string>() { "nn:table_tech_guon", "mahoguny" };
            CustomSynergies.Add("Mahoguny Guon Stones", mandatorySynergyItemsMahogunyGuonStones);
            //WRENCH / GLITCH ITEMS
            List<string> mandatorySynergyItemsNullReferenceException = new List<string>() { "nn:wrench" };
            List<string> optionalSynergyItemsnullReferenceException = new List<string>() { "nn:error_shells" };
            CustomSynergies.Add("NullReferenceException", mandatorySynergyItemsNullReferenceException, optionalSynergyItemsnullReferenceException);
            //PESTILENCE / GUNDROMEDA STRAIN
            List<string> mandatorySynergyItemsMultimorbidities = new List<string>() { "nn:pestilence", "gundromeda_strain" };
            CustomSynergies.Add("Multimorbidities", mandatorySynergyItemsMultimorbidities);
            //KEVIN / GRAVITY GUN
            List<string> mandatorySynergyItemsOnceMoreIntoTheBreach = new List<string>() { "nn:kevin", "nn:gravity_gun" };
            CustomSynergies.Add("Once More Into The Breach", mandatorySynergyItemsOnceMoreIntoTheBreach);
            //THICKER THAN WATER
            List<string> mandatorySynergyItemsThickerThanWater = new List<string>() { "nn:blood_thinner" };
            List<string> optionalSynergyItemsThickerThanWater = new List<string>() { "heart_purse", "heart_locket", "heart_bottle", "heart_holster", "heart_lunchbox" };
            CustomSynergies.Add("Thicker Than Water", mandatorySynergyItemsThickerThanWater, optionalSynergyItemsThickerThanWater);
            //PUNISHMENT RAY / WOOD BEAM
            List<string> mandatorySynergyItemsSpareTheRod = new List<string>() { "nn:punishment_ray" };
            List<string> optionalSynergyItemsSpareTheRod = new List<string>() { "wood_beam" };
            CustomSynergies.Add("Spare The Rod", mandatorySynergyItemsSpareTheRod, optionalSynergyItemsSpareTheRod);
            //PUNISHMENT RAY / COBALT HAMMER
            List<string> mandatorySynergyItemsCobaltStreak = new List<string>() { "nn:punishment_ray" };
            List<string> optionalSynergyItemsCobaltStreak = new List<string>() { "cobalt_hammer" };
            CustomSynergies.Add("Cobalt Streak", mandatorySynergyItemsCobaltStreak, optionalSynergyItemsCobaltStreak);
            //CRESCENDO BLASTER / FAST ITEMS
            List<string> mandatorySynergyItemsAllegro = new List<string>() { "nn:crescendo_blaster" };
            List<string> optionalSynergyItemsAllegro = new List<string>() { "ballistic_boots", "rocket_powered_bullets", "shotga_cola", "shotgun_coffee" };
            CustomSynergies.Add("Allegro", mandatorySynergyItemsAllegro, optionalSynergyItemsAllegro);
            //CRESCENDO BLASTER / LOUD ITEMS
            List<string> mandatorySynergyItemsFortissimo = new List<string>() { "nn:crescendo_blaster" };
            List<string> optionalSynergyItemsFortissimo = new List<string>() { "bomb", "ice_bomb", "lil_bomber", "screecher" };
            CustomSynergies.Add("Fortissimo", mandatorySynergyItemsFortissimo, optionalSynergyItemsFortissimo);
            //VARIABLE / BACKWARDS ITEMS
            List<string> mandatorySynergyItemsBackwardsCompatibility = new List<string>() { "nn:variable" };
            List<string> optionalSynergyItemsBackwardsCompatibility = new List<string>() { "backup_gun", "nn:backwards_bullets", "nn:back_warder" };
            CustomSynergies.Add("Backwards Compatible", mandatorySynergyItemsBackwardsCompatibility, optionalSynergyItemsBackwardsCompatibility);
            //AUTOLLET / COMPUTER ITEMS
            List<string> mandatorySynergyItemsCodeBanks = new List<string>() { "nn:autollet" };
            List<string> optionalSynergyItemsCodeBanks = new List<string>() { "ibomb_companion_app", "nn:aimbot", "remote_bullets" };
            CustomSynergies.Add("Code Blanks", mandatorySynergyItemsCodeBanks, optionalSynergyItemsCodeBanks);
            //KIN AMMOLET / CURSED BULLETS / YELLOW CHAMBER / SIXTH CHAMBER / VOODOOLLETS
            List<string> mandatorySynergyItemsFriendsOnTheOtherSide = new List<string>() { "nn:kin_ammolet" };
            List<string> optionalSynergyItemsFriendsOnTheOtherSide = new List<string>() { "cursed_bullets", "nn:voodoollets", "yellow_chamber", "sixth_chamber" };
            CustomSynergies.Add("Friends On The Other Side", mandatorySynergyItemsFriendsOnTheOtherSide, optionalSynergyItemsFriendsOnTheOtherSide);
            //KIN AMMOLET / +1 BULLETS / MULTIPLICATOR / DOUBLE VISION / HELIX BULLETS / SCATTERSHOT
            List<string> mandatorySynergyItemsAimTwiceShootOnce = new List<string>() { "nn:kin_ammolet" };
            List<string> optionalSynergyItemsAimTwiceShootOnce = new List<string>() { "+1_bullets", "nn:multiplicator", "double_vision", "helix_bullets", "scattershot" };
            CustomSynergies.Add("Aim Twice, Shoot Once", mandatorySynergyItemsAimTwiceShootOnce, optionalSynergyItemsAimTwiceShootOnce);
            //KIN AMMOLET / REGULAR SHOTGUN / WINCHESTER / BIG SHOTGUN / CLOWN SHOTGUN 
            List<string> mandatorySynergyItemsShotgunClub = new List<string>() { "nn:kin_ammolet" };
            List<string> optionalSynergyItemsShotgunClub = new List<string>() { "regular_shotgun", "nn:clown_shotgun", "big_shotgun", "winchester" };
            CustomSynergies.Add("Shotgun Club", mandatorySynergyItemsShotgunClub, optionalSynergyItemsShotgunClub);
            //THE FAT LINE / THE THIN LINE
            List<string> mandatorySynergyItemsParallelLines = new List<string>() { "the_fat_line", "nn:the_thin_line" };
            CustomSynergies.Add("Parallel Lines", mandatorySynergyItemsParallelLines);
            //CYANER GUON STONE
            List<string> mandatorySynergyItemsCyanerGuonStone = new List<string>() { "nn:cyan_guon_stone" };
            List<string> optionalSynergyItemsCyanerGuonStone = new List<string>() { "+1_bullets", "amulet_of_the_pit_lord", "heavy_boots", "nn:bullet_boots" };
            CustomSynergies.Add("Cyaner Guon Stone", mandatorySynergyItemsCyanerGuonStone, optionalSynergyItemsCyanerGuonStone);
            //RAINBOWER GUON STONE
            List<string> mandatorySynergyItemsRainbowerGuonStone = new List<string>() { "nn:rainbow_guon_stone" };
            List<string> optionalSynergyItemsRainbowerGuonStone = new List<string>() { "+1_bullets", "amulet_of_the_pit_lord", "nn:ammolite" }; //Add Ammolite when you port this
            CustomSynergies.Add("Rainbower Guon Stone", mandatorySynergyItemsRainbowerGuonStone, optionalSynergyItemsRainbowerGuonStone);
            //INDIGOER GUON STONE
            List<string> mandatorySynergyItemsIndigoerGuonStone = new List<string>() { "nn:indigo_guon_stone" };
            List<string> optionalSynergyItemsIndigoerGuonStone = new List<string>() { "+1_bullets", "amulet_of_the_pit_lord", "blank_bullets" };
            CustomSynergies.Add("Indigoer Guon Stone", mandatorySynergyItemsIndigoerGuonStone, optionalSynergyItemsIndigoerGuonStone);
            //KALIBERS PRAYER + CHANCE BULLETS
            List<string> mandatorySynergyItemsGunsignor = new List<string>() { "chance_bullets", "nn:kalibers_prayer" };
            CustomSynergies.Add("Gunsignor", mandatorySynergyItemsGunsignor);
            //KALIBERS PRAYER + YELLOW GUON STONE / LIQUID METAL BODY / BIG IRON
            List<string> mandatorySynergyItemsRodOfIron = new List<string>() { "nn:kalibers_prayer" };
            List<string> optionalSynergyItemsRodOfIron = new List<string>() { "nn:yellow_guon_stone", "nn:liquid-metal_body", "big_iron" };
            CustomSynergies.Add("Rod of Iron", mandatorySynergyItemsRodOfIron, optionalSynergyItemsRodOfIron);
            //KALIBERS PRAYER + BOMBINOMICON
            List<string> mandatorySynergyItemsLiturgist = new List<string>() { "nn:kalibers_prayer" };
            List<string> optionalSynergyItemsLiturgist = new List<string>() { "nn:bombinomicon", "nn:bookllet", "nn:lorebook" }; 
            CustomSynergies.Add("Liturgist", mandatorySynergyItemsLiturgist, optionalSynergyItemsLiturgist);
            //KALIBERS PRAYER + SAA
            List<string> mandatorySynergyItemsGunvana = new List<string>() { "saa", "nn:kalibers_prayer" };
            CustomSynergies.Add("Gunvana", mandatorySynergyItemsGunvana);
            //SILVERER GUON STONE
            List<string> mandatorySynergyItemsSilvererGuonStone = new List<string>() { "nn:silver_guon_stone" };
            List<string> optionalSynergyItemsSilvererGuonStone = new List<string>() { "+1_bullets", "amulet_of_the_pit_lord", "nn:silver_ammolet" };
            CustomSynergies.Add("Silverer Guon Stone", mandatorySynergyItemsSilvererGuonStone, optionalSynergyItemsSilvererGuonStone);
            //MAROONER GUON STONE
            List<string> mandatorySynergyItemsMaroonerGuonStone = new List<string>() { "nn:maroon_guon_stone" };
            List<string> optionalSynergyItemsMaroonerGuonStone = new List<string>() { "+1_bullets", "amulet_of_the_pit_lord", "nn:bloodthirsty_bullets" };
            CustomSynergies.Add("Marooner Guon Stone", mandatorySynergyItemsMaroonerGuonStone, optionalSynergyItemsMaroonerGuonStone);
            //SILVER GUON STONE
            List<string> mandatorySynergyItemsTurnGundead = new List<string>() { "nn:silver_guon_stone" };
            List<string> optionalSynergyItemsTurnGundead = new List<string>() { "silver_bullets", "nn:cleansing_rounds", "nn:hallowed_bullets" };
            CustomSynergies.Add("Turn Gundead", mandatorySynergyItemsTurnGundead, optionalSynergyItemsTurnGundead);
            //MAROON GUON STONE / IRRADIATED LEAD
            List<string> mandatorySynergyItemsToxicCore = new List<string>() { "nn:maroon_guon_stone", "irradiated_lead" };
            CustomSynergies.Add("Toxic Core", mandatorySynergyItemsToxicCore);
            //MAROON GUON STONE / CHARMING ROUNDS
            List<string> mandatorySynergyItemsCharmingCore = new List<string>() { "nn:maroon_guon_stone", "charming_rounds" };
            CustomSynergies.Add("Charming Core", mandatorySynergyItemsCharmingCore);
            //MAROON GUON STONE / HOT LEAD
            List<string> mandatorySynergyItemsBurningCore = new List<string>() { "nn:maroon_guon_stone", "hot_lead" };
            CustomSynergies.Add("Burning Core", mandatorySynergyItemsBurningCore);
            //MAROON GUON STONE / EXPLOSIVE ROUNDS
            List<string> mandatorySynergyItemsExplosiveCore = new List<string>() { "nn:maroon_guon_stone", "explosive_rounds" };
            CustomSynergies.Add("Explosive Core", mandatorySynergyItemsExplosiveCore);
            //MAROON GUON STONE / HUNGRY BULLETS
            List<string> mandatorySynergyItemsHungryCore = new List<string>() { "nn:maroon_guon_stone", "hungry_bullets" };
            CustomSynergies.Add("Hungry Core", mandatorySynergyItemsHungryCore);
            //MAROON GUON STONE / HOMING BULLETS
            List<string> mandatorySynergyItemsSmartCore = new List<string>() { "nn:maroon_guon_stone", "homing_bullets" };
            CustomSynergies.Add("Smart Core", mandatorySynergyItemsSmartCore);
            //GLASSTER / GLASS CANNON
            List<string> mandatorySynergyItemsNoPaneNoGain = new List<string>() { "nn:glasster", "glass_cannon" };
            CustomSynergies.Add("No Pane, No Gain", mandatorySynergyItemsNoPaneNoGain);
            //GREG THE EGG
            List<string> mandatorySynergyItemsFreeRangeGregs = new List<string>() { "nn:greg_the_egg" };
            List<string> optionalSynergyItemsFreeRangeGregs = new List<string>() { "chance_bullets", "nn:chaos_ruby", "nn:rando_rounds", "chaos_bullets" };
            CustomSynergies.Add("Free Range Gregs", mandatorySynergyItemsFreeRangeGregs, optionalSynergyItemsFreeRangeGregs);
            //GREG THE EGG / SCRAMBLER
            List<string> mandatorySynergyItemsScrambledGregs = new List<string>() { "nn:greg_the_egg", "the_scrambler" };
            CustomSynergies.Add("Scrambled Gregs", mandatorySynergyItemsScrambledGregs);
            //GREG THE EGG / EGG SALAD
            List<string> mandatorySynergyItemsGregSalad = new List<string>() { "nn:greg_the_egg", "nn:egg_salad" };
            CustomSynergies.Add("Greg Salad", mandatorySynergyItemsGregSalad);
            //GREG THE EGG / PITCHFORK / LAMENT CONFIGURUM / SIXTH CHAMBER / FLAME HAND
            List<string> mandatorySynergyItemsDeviledGregs = new List<string>() { "nn:greg_the_egg" };
            List<string> optionalSynergyItemsDeviledGregs = new List<string>() { "pitchfork", "lament_configurum", "sixth_chamber", "flame_hand" };
            CustomSynergies.Add("Deviled Gregs", mandatorySynergyItemsDeviledGregs, optionalSynergyItemsDeviledGregs);
            //GREG THE EGG / POCKET CHEST / AGED BELL / BULLET TIME
            List<string> mandatorySynergyItemsCenturyGreg = new List<string>() { "nn:greg_the_egg" };
            List<string> optionalSynergyItemsCenturyGreg = new List<string>() { "nn:pocket_chest", "aged_bell", "bullet_time" };
            CustomSynergies.Add("Century Greg", mandatorySynergyItemsCenturyGreg, optionalSynergyItemsCenturyGreg);
            //GLASS SHARD / GLASSTER
            List<string> mandatorySynergyItemsMasterglass = new List<string>() { "nn:glass_shard", "nn:glasster" };
            CustomSynergies.Add("Masterglass", mandatorySynergyItemsMasterglass);
            //GLASS SHARD / SHATTERSHOT
            List<string> mandatorySynergyItemsShattershot = new List<string>() { "nn:glass_shard", "scattershot" };
            CustomSynergies.Add("Shattershot", mandatorySynergyItemsShattershot);
            //GLASS SHARD / EGG SALAD / ORANGE / RATION / MEATBUN / WEIRD EGG / GREG THE EGG / SCRAMBLER
            List<string> mandatorySynergyItemsBreakFast = new List<string>() { "nn:glass_shard" };
            List<string> optionalSynergyItemsBreakFast = new List<string>() { "orange", "ration", "meatbun", "weird_egg", "nn:egg_salad", "nn:greg_the_egg", "the_scrambler" };
            CustomSynergies.Add("Break Fast!", mandatorySynergyItemsBreakFast, optionalSynergyItemsBreakFast);
            //GREG THE EGG / YELLOW GUON STONE / 
            List<string> mandatorySynergyItemsHardBoiledGregs = new List<string>() { "nn:greg_the_egg" };
            List<string> optionalSynergyItemsHardBoiledGregs = new List<string>() { "nn:yellow_guon_stone", "potion_of_lead_skin", "teapot", "nn:mirror_bullets" };
            CustomSynergies.Add("Hard Boiled Gregs", mandatorySynergyItemsHardBoiledGregs, optionalSynergyItemsHardBoiledGregs);
            //DRONE / BEE ITEMS
            List<string> mandatorySynergyItemsWrongKindOfDrone = new List<string>() { "nn:drone" };
            List<string> optionalSynergyItemsWrongKindOfDrone = new List<string>() { "bee_hive", "honeycomb", "bumbullets", "stinger", "nn:hive_holster", "jar_of_bees" };
            CustomSynergies.Add("Wrong Kind Of Drone", mandatorySynergyItemsWrongKindOfDrone, optionalSynergyItemsWrongKindOfDrone);
            //BULLETS WITH GUNS / EXCALIBER
            List<string> mandatorySynergyItemsBulletsWithKnives = new List<string>() { "nn:bullets_with_guns" };
            List<string> optionalSynergyItemsBulletsWithKnives = new List<string>() { "excaliber", "huntsman", "fightsabre" };
            CustomSynergies.Add("Bullets With Knives", mandatorySynergyItemsBulletsWithKnives, optionalSynergyItemsBulletsWithKnives);
            //HAND GUN / HEART ITEMS
            List<string> mandatorySynergyItemsHaveAHeart = new List<string>() { "nn:hand_gun" };
            List<string> optionalSynergyItemsHaveAHeart = new List<string>() { "nn:orgun", "heart_of_ice", "heart_locket", "heart_purse", "heart_bottle", "heart_synthesizer", "heart_holster", "heart_lunchbox" };
            CustomSynergies.Add("Have A Heart", mandatorySynergyItemsHaveAHeart, optionalSynergyItemsHaveAHeart);
            //HAND GUN / DIGGING / KEY ITEMS
            List<string> mandatorySynergyItemsDigDeep = new List<string>() { "nn:hand_gun" }; //Add portable hole
            List<string> optionalSynergyItemsDigDeep = new List<string>() { "knights_gun", "drill", "amulet_of_the_pit_lord", "nn:portable_hole" };
            CustomSynergies.Add("Dig Deep", mandatorySynergyItemsDigDeep, optionalSynergyItemsDigDeep);
            //HAND GUN / DIAMOND ITEMS
            List<string> mandatorySynergyItemsGirlsBestFriend = new List<string>() { "nn:hand_gun" };
            List<string> optionalSynergyItemsGirlsBestFriend = new List<string>() { "nn:diamond_gun", "nn:chaos_ruby", "coin_crown", "nn:diamond_bracelet" };
            CustomSynergies.Add("Girls Best Friend", mandatorySynergyItemsGirlsBestFriend, optionalSynergyItemsGirlsBestFriend);
            //HAND GUN / CLUB ITEMS
            List<string> mandatorySynergyItemsGoingClubbing = new List<string>() { "nn:hand_gun" };
            List<string> optionalSynergyItemsGoingClubbing = new List<string>() { "nn:pearl_bracelet", "klobbe", "nn:titan_bullets", "anvillain", "casey", "boxing_glove" };
            CustomSynergies.Add("Going Clubbing", mandatorySynergyItemsGoingClubbing, optionalSynergyItemsGoingClubbing);
            //HAND GUN / CAT BULLET KING THRONE / CROWN OF GUNS 
            List<string> mandatorySynergyItemsSuicideKing = new List<string>() { "nn:hand_gun" };
            List<string> optionalSynergyItemsSuicideKing = new List<string>() { "nn:bullut", "nn:viscerifle", "nn:disc_gun", "cigarettes" };
            CustomSynergies.Add("Suicide King", mandatorySynergyItemsSuicideKing, optionalSynergyItemsSuicideKing);
            //HAND GUN / 
            List<string> mandatorySynergyItemsRoyalFlush = new List<string>() { "nn:hand_gun" };
            List<string> optionalSynergyItemsRoyalFlush = new List<string>() { "crown_of_guns", "cat_bullet_king_throne" };
            CustomSynergies.Add("Royal Flush", mandatorySynergyItemsRoyalFlush, optionalSynergyItemsRoyalFlush);
            //POTTY / TEAPOT
            List<string> mandatorySynergyItemsTeapotty = new List<string>() { "nn:potto", "teapot" };
            CustomSynergies.Add("Teapotto", mandatorySynergyItemsTeapotty);
            //POTTY / LUCKY ITEMS
            List<string> mandatorySynergyItemsPotOGold = new List<string>() { "nn:potto" };
            List<string> optionalSynergyItemsPotOGold = new List<string>() { "seven_leaf_clover", "nn:lucky_coin", "unicorn_horn" };
            CustomSynergies.Add("Pot O' Gold", mandatorySynergyItemsPotOGold, optionalSynergyItemsPotOGold);
            //POTTY / CARD ITEMS
            List<string> mandatorySynergyItemsWhatDoesItDo = new List<string>() { "nn:potto" };
            List<string> optionalSynergyItemsWhatDoesItDo = new List<string>() { "nn:hand_gun" };
            CustomSynergies.Add("What does it do?", mandatorySynergyItemsWhatDoesItDo, optionalSynergyItemsWhatDoesItDo);
            //POTTY / MAGIC ITEMS
            List<string> mandatorySynergyItemsThePotterBoy = new List<string>() { "nn:potto" };
            List<string> optionalSynergyItemsThePotterBoy = new List<string>() { "magic_bullets", "witch_pistol", "bundle_of_wands", "owl" };
            CustomSynergies.Add("The Potter Boy", mandatorySynergyItemsThePotterBoy, optionalSynergyItemsThePotterBoy);
            //POTTY / SHOTGRUB
            List<string> mandatorySynergyItemsTheyGrowInside = new List<string>() { "nn:potto", "shotgrub" };
            CustomSynergies.Add("They Grow Inside", mandatorySynergyItemsTheyGrowInside);
            //POTTY / CURSED ITEMS
            List<string> mandatorySynergyItemsCursedCeramics = new List<string>() { "nn:potto" };
            List<string> optionalSynergyItemsCursedCeramics = new List<string>() { "cursed_bullets", "sixth_chamber", "yellow_chamber", "riddle_of_lead" };
            CustomSynergies.Add("Cursed Ceramics", mandatorySynergyItemsCursedCeramics, optionalSynergyItemsCursedCeramics);
            //GUNGER / HUNGRY BULLETS
            List<string> mandatorySynergyItemsFamished = new List<string>() { "nn:gunger", "hungry_bullets" };
            CustomSynergies.Add("Famished", mandatorySynergyItemsFamished);
            //GUMGUN / BIRD ITEMS
            List<string> mandatorySynergyItemsWEH = new List<string>() { "nn:gumgun" }; //Add birdshot on port
            List<string> optionalSynergyItemsWEH = new List<string>() { "weird_egg", "nn:greg_the_egg", "the_scrambler", "wax_wings" };
            CustomSynergies.Add("WEH", mandatorySynergyItemsWEH, optionalSynergyItemsWEH);
            //GUNSMOKE PERFUME / HOT LEAD / FLAMECHAMBER 
            List<string> mandatorySynergyItemsRegularHottie = new List<string>() { "nn:gunsmoke_perfume" };
            List<string> optionalSynergyItemsRegularHottie = new List<string>() { "hot_lead", "nn:flame_chamber" };
            CustomSynergies.Add("Regular Hottie", mandatorySynergyItemsRegularHottie, optionalSynergyItemsRegularHottie);
            //GUNSMOKE PERFUME / CHARMING ROUNDS / SHOTGUN FULL OF LOVE / LOVE PISTOL
            List<string> mandatorySynergyItemsPracticallyPungent = new List<string>() { "nn:gunsmoke_perfume" };
            List<string> optionalSynergyItemsPracticallyPungent = new List<string>() { "charming_rounds", "nn:love_pistol", "shotgun_full_of_love", "nn:gunpowder_pheromones" };
            CustomSynergies.Add("Practically Pungent", mandatorySynergyItemsPracticallyPungent, optionalSynergyItemsPracticallyPungent);
            //TIN HEART / OIL ITEMS
            List<string> mandatorySynergyItemsOilCanWhat = new List<string>() { "nn:tin_heart" };
            List<string> optionalSynergyItemsOilCanWhat = new List<string>() { "fossilized_gun", "nn:gun_grease", "gungeon_ant", "oiled_cylinder", "nn:sanctified_oil" };
            CustomSynergies.Add("Oil Can What?", mandatorySynergyItemsOilCanWhat, optionalSynergyItemsOilCanWhat);
            //TIN HEART / HUNTSMAN
            List<string> mandatorySynergyItemsWoodcutter = new List<string>() { "nn:tin_heart", "huntsman" };
            CustomSynergies.Add("Woodcutter", mandatorySynergyItemsWoodcutter);
            //GLOCK 42 / GUNSMOKE PERUME
            List<string> mandatorySynergyItemsSongOfMyPeople = new List<string>() { "nn:glock_42", "nn:gunsmoke_perfume" };
            CustomSynergies.Add("Song of my people", mandatorySynergyItemsSongOfMyPeople);
            //GLOCK 42 / STEALTH ITEMS
            List<string> mandatorySynergyItemsConcealedCarry = new List<string>() { "nn:glock_42" };
            List<string> optionalSynergyItemsConcealedCarry = new List<string>() { "smoke_bomb", "shadow_bullets", "shadow_clone", "backpack", "nn:hiking_pack" };
            CustomSynergies.Add("Concealed Carry", mandatorySynergyItemsConcealedCarry, optionalSynergyItemsConcealedCarry);
            //GLOCK 42 / LIFE THE UNIVERSE AND EVERYTHING
            List<string> mandatorySynergyItemsLifeTheUniverseAndEverything = new List<string>() { "nn:glock_42" };
            List<string> optionalSynergyItemsLifeTheUniverseAndEverything = new List<string>() { "mr_accretion_jr", "evolver" };
            CustomSynergies.Add("Life, The Universe, and Everything", mandatorySynergyItemsLifeTheUniverseAndEverything, optionalSynergyItemsLifeTheUniverseAndEverything);
            //RAPID RIPOSTE / SUPER SPACE TURTLE / TURTLE PROBLEM
            List<string> mandatorySynergyItemsWouldntYouAgree = new List<string>() { "nn:rapid_riposte" };
            List<string> optionalSynergyItemsWouldntYouAgree = new List<string>() { "super_space_turtle", "turtle_problem" };
            CustomSynergies.Add("Wouldn't You Agree?", mandatorySynergyItemsWouldntYouAgree, optionalSynergyItemsWouldntYouAgree);
            //HEAD OF THE ORDER / KALIBERS PRAYER
            List<string> mandatorySynergyItemsNonDesistasNonExieris = new List<string>() { "nn:head_of_the_order" }; //Add Cardinal's Mitre
            List<string> optionalSynergyItemsNonDesistasNonExieris = new List<string>() { "nn:kalibers_prayer", "nn:cardinals_mitre" };
            CustomSynergies.Add("Non Desistas Non Exieris", mandatorySynergyItemsNonDesistasNonExieris, optionalSynergyItemsNonDesistasNonExieris);
            //PEANUT / PEA SHOOTER
            List<string> mandatorySynergyItemsPealadin = new List<string>() { "nn:peanut", "pea_shooter" };
            CustomSynergies.Add("Pealadin", mandatorySynergyItemsPealadin);
            //PEA SHOOTER / REPEATOVOLVER / SHADOW BULLETS
            List<string> mandatorySynergyItemsRepeater = new List<string>() { "pea_shooter" };
            List<string> optionalSynergyItemsRepeater = new List<string>() { "nn:repeatovolver", "shadow_bullets" };
            CustomSynergies.Add("Repeater", mandatorySynergyItemsRepeater, optionalSynergyItemsRepeater);
            //VIPER / ELECTRICITY ITEMS
            List<string> mandatorySynergyItemsLightningFastStrike = new List<string>() { "nn:viper" };
            List<string> optionalSynergyItemsLightningFastStrike = new List<string>() { "shock_rounds", "shock_rifle", "the_emperor", "nn:diode" };
            CustomSynergies.Add("Lightning Fast Strike", mandatorySynergyItemsLightningFastStrike, optionalSynergyItemsLightningFastStrike);
            //VIPER / SNIPER RIFLE / AWP / 50 CALIBER ROUNDS / TRACER ROUND
            List<string> mandatorySynergyItemsSniperViper = new List<string>() { "nn:viper" };
            List<string> optionalSynergyItemsSniperViper = new List<string>() { "sniper_rifle", "awp", "nn:50._cal_rounds", "nn:tracer_rounds" };
            CustomSynergies.Add("Sniper Viper", mandatorySynergyItemsSniperViper, optionalSynergyItemsSniperViper);
            //SNAKER / SNAKE GUNS
            List<string> mandatorySynergyItemsSnakeWorldChampion = new List<string>() { "nn:snaker" };
            List<string> optionalSynergyItemsSnakeWorldChampion = new List<string>() { "nn:viper", "rattler", "snakemaker", "ring_of_ethereal_form" };
            CustomSynergies.Add("Snake World Champion", mandatorySynergyItemsSnakeWorldChampion, optionalSynergyItemsSnakeWorldChampion);
            //SNAKER / HUNGRY BULLETS
            List<string> mandatorySynergyItemsHighScore = new List<string>() { "nn:snaker", "hungry_bullets" };
            CustomSynergies.Add("High Score", mandatorySynergyItemsHighScore);
            //GUNKNIGHT AMULET / OTHER GUNKNIGHT PIECES
            List<string> mandatorySynergyItemsReuknighted = new List<string>() { "nn:gunknight_amulet" };
            List<string> optionalSynergyItemsReuknighted = new List<string>() { "gunknight_armor", "gunknight_helmet", "gunknight_gauntlet", "gunknight_greaves" };
            CustomSynergies.Add("Reuknighted", mandatorySynergyItemsReuknighted, optionalSynergyItemsReuknighted);
            //COMBAT KNIFE / MIRROR BULLETS
            List<string> mandatorySynergyItemsMirrorBlade = new List<string>() { "nn:combat_knife", "nn:mirror_bullets" };
            CustomSynergies.Add("Mirror Blade", mandatorySynergyItemsMirrorBlade);
            //COMBAT KNIFE / SCATTERSHOT
            List<string> mandatorySynergyItemsTriTipDagger = new List<string>() { "nn:combat_knife", "scattershot" };
            CustomSynergies.Add("Tri-Tip Dagger", mandatorySynergyItemsTriTipDagger);
            //COMBAT KNIFE / SWORD BULLETS
            List<string> mandatorySynergyItemsWhirlingBlade = new List<string>() { "nn:combat_knife", "nn:longsword_shot" };
            CustomSynergies.Add("Whirling Blade", mandatorySynergyItemsWhirlingBlade);
            //COMBAT KNIFE / HOT ITEMS
            List<string> mandatorySynergyItems1000DegreeKnife = new List<string>() { "nn:combat_knife" };
            List<string> optionalSynergyItems1000DegreeKnife = new List<string>() { "hot_lead", "nn:flame_chamber", "nn:mr_fahrenheit" };
            CustomSynergies.Add("1000 Degree Knife", mandatorySynergyItems1000DegreeKnife, optionalSynergyItems1000DegreeKnife);
            //LASER BULLETS / HEAVILY VECTOR BASED ITEMS
            List<string> mandatorySynergyItemsNewVector = new List<string>() { "nn:laser_bullets" };
            List<string> optionalSynergyItemsNewVector = new List<string>() { "nn:diode", "nn:baby_good_det" };
            CustomSynergies.Add("new Vector2(x, y)", mandatorySynergyItemsNewVector, optionalSynergyItemsNewVector);
            //LASER BULLETS / BEAM WEAPONs
            List<string> mandatorySynergyItemsBeamMeUp = new List<string>() { "nn:laser_bullets" };
            List<string> optionalSynergyItemsBeamMeUp = new List<string>() { "mega_douser", "unicorn_horn", "demon_head", "mutation", "fossilized_gun", "gamma_ray", "freeze_ray", "science_cannon", "disintegrator", "proton_backpack", "plunger", "raiden_coil", "moonscraper", "wood_beam", "abyssal_tentacle", "life_orb" };
            CustomSynergies.Add("Beam Me Up!", mandatorySynergyItemsBeamMeUp, optionalSynergyItemsBeamMeUp);
            //PAPER BADGE / MAILBOX
            List<string> mandatorySynergyItemsLuckyDay = new List<string>() { "nn:paper_badge", "mailbox" };
            CustomSynergies.Add("Lucky Day", mandatorySynergyItemsLuckyDay);
            //MANTID AUGMENT / HUNGRY BULLETS / BLOODTHIRSTY BULLETS
            List<string> mandatorySynergyItemsBloodthirstyBlades = new List<string>() { "nn:mantid_augment" };
            List<string> optionalSynergyItemsBloodthirstyBlades = new List<string>() { "hungry_bullets", "nn:bloodthirsty_bullets" };
            CustomSynergies.Add("Bloodthirsty Blades", mandatorySynergyItemsBloodthirstyBlades, optionalSynergyItemsBloodthirstyBlades);
            //LONGSWORD SHOT / FIGHTSABRE
            List<string> mandatorySynergyItemsSabreThrow = new List<string>() { "nn:longsword_shot", "fightsabre" };
            CustomSynergies.Add("Sabre Throw", mandatorySynergyItemsSabreThrow);
            //LONGSWORD SHOT / KNIFE SHIELD / STAFF OF FIREPOWER / MAGIC BULLETS
            List<string> mandatorySynergyItemsSwordMage = new List<string>() { "nn:longsword_shot" };
            List<string> optionalSynergyItemsSwordMage = new List<string>() { "knife_shield", "staff_of_firepower", "magic_bullets" };
            CustomSynergies.Add("Sword Mage", mandatorySynergyItemsSwordMage, optionalSynergyItemsSwordMage);
            //LONGSWORD SHOT / KATANA BULLETS
            List<string> mandatorySynergyItemsLiveByTheSword = new List<string>() { "nn:longsword_shot", "katana_bullets" };
            CustomSynergies.Add("Live By The Sword", mandatorySynergyItemsLiveByTheSword);
            //PUMP CHARGE SHOTGUN / BLOOD BROOCH / BLOODY EYE
            List<string> mandatorySynergyItemsBloodForTheBloodGod = new List<string>() { "nn:pump_charge_shotgun" };
            List<string> optionalSynergyItemsBloodForTheBloodGod = new List<string>() { "blood_brooch", "bloody_eye", "nn:blood_thinner", "monster_blood" };
            CustomSynergies.Add("Blood For The Blood God", mandatorySynergyItemsBloodForTheBloodGod, optionalSynergyItemsBloodForTheBloodGod);
            //PUMP CHARGE SHOTGUN / CHARGE SHOT / 
            List<string> mandatorySynergyItemsBLOODISFUEL = new List<string>() { "nn:pump_charge_shotgun" };
            List<string> optionalSynergyItemsBLOODISFUEL = new List<string>() { "charge_shot", "nn:bloodthirsty_bullets", "holey_grail" };
            CustomSynergies.Add("BLOOD IS FUEL", mandatorySynergyItemsBLOODISFUEL, optionalSynergyItemsBLOODISFUEL);
            //GRAVITY GUN PROPS
            List<string> mandatorySynergyItemsPropFly = new List<string>() { "nn:gravity_gun" }; //Add toolbox
            List<string> optionalSynergyItemsPropFly = new List<string>() { "portable_table_device", "nn:toolbox" };
            CustomSynergies.Add("Prop Fly", mandatorySynergyItemsPropFly, optionalSynergyItemsPropFly);
            //GRAVITY GUN / TABLE TECH ROCKET
            List<string> mandatorySynergyItemsHiddenTechNitro = new List<string>() { "nn:gravity_gun", "table_tech_rocket" };
            CustomSynergies.Add("Hidden Tech Nitro", mandatorySynergyItemsHiddenTechNitro);
            //GRAVITY GUN / MATTER AND ANTIMATTER ITEMS
            List<string> mandatorySynergyItemsNegativeMatter = new List<string>() { "nn:gravity_gun" };
            List<string> optionalSynergyItemsNegativeMatter = new List<string>() { "dark_marker", "nn:antimatter_bullets", "mass_shotgun" };
            CustomSynergies.Add("Negative Matter", mandatorySynergyItemsNegativeMatter, optionalSynergyItemsNegativeMatter);
            //GRAVITY GUN / ALKALI BULLETS
            List<string> mandatorySynergyItemsXenobiology = new List<string>() { "nn:gravity_gun", "nn:alkali_bullets" };
            CustomSynergies.Add("Xenobiology", mandatorySynergyItemsXenobiology);
            //GRAVITY GUN / MAILBOX
            List<string> mandatorySynergyItemsRedLetterDay = new List<string>() { "nn:gravity_gun", "mailbox" };
            CustomSynergies.Add("Red Letter Day", mandatorySynergyItemsRedLetterDay);
            //APPLE / GRAVITY ITEMS
            List<string> mandatorySynergyItemsNewton = new List<string>() { "nn:apple" };
            List<string> optionalSynergyItemsNewton = new List<string>() { "nn:gravity_gun", "nn:gravitron", "orbital_bullets", "mr_accretion_jr" };
            CustomSynergies.Add("Newton", mandatorySynergyItemsNewton, optionalSynergyItemsNewton);
            //APPLE / GOLDEN APPLE
            List<string> mandatorySynergyItemsGoldenApple = new List<string>() { "nn:apple" };
            List<string> optionalSynergyItemsGoldenApple = new List<string>() { "nn:diamond_gun", "au_gun", "nn:miners_bullets", "old_goldie" };
            CustomSynergies.Add("Golden Apple", mandatorySynergyItemsGoldenApple, optionalSynergyItemsGoldenApple);
            //APPLE / MEDICAL ITEMS
            List<string> mandatorySynergyItemsAppleADay = new List<string>() { "nn:apple" };
            List<string> optionalSynergyItemsAppleADay = new List<string>() { "medkit", "plunger", "muscle_relaxant", "nn:mutagen", "antibody" };
            CustomSynergies.Add("Apple A Day", mandatorySynergyItemsAppleADay, optionalSynergyItemsAppleADay);
            //CREDITOR / MICROTRANSACTION GUN
            List<string> mandatorySynergyItemsFullyFunded = new List<string>() { "nn:creditor", "microtransaction_gun" };
            CustomSynergies.Add("Fully Funded", mandatorySynergyItemsFullyFunded);
            //GATLING GUN / ROBOT ITEMS
            List<string> mandatorySynergyItemsGatterUp = new List<string>() { "nn:gatling_gun" };
            List<string> optionalSynergyItemsGatterUp = new List<string>() { "casey", "remote_bullets", "vulcan_cannon", "ibomb_companion_app" };
            CustomSynergies.Add("Gatter Up", mandatorySynergyItemsGatterUp, optionalSynergyItemsGatterUp);
            //GATLING GUN / SNAKE ITEMS
            List<string> mandatorySynergyItemsGattlesnake = new List<string>() { "nn:gatling_gun" };
            List<string> optionalSynergyItemsGattlesnake = new List<string>() { "snakemaker", "rattler", "nn:viper", "nn:snaker", "nn:guneonate" };
            CustomSynergies.Add("Gattlesnake", mandatorySynergyItemsGattlesnake, optionalSynergyItemsGattlesnake);
            //BAYONET / KNIVES
            List<string> mandatorySynergyItemsHackNSlash = new List<string>() { "nn:bayonet" };
            List<string> optionalSynergyItemsHackNSlash = new List<string>() { "nn:combat_knife", "excaliber" };
            CustomSynergies.Add("Hack n' Slash", mandatorySynergyItemsHackNSlash, optionalSynergyItemsHackNSlash);
            //LEFTHANDEDNESS / ROBOTS LEFT HAND
            List<string> mandatorySynergyItemsLeftyLoosey = new List<string>() { "nn:lefthandedness", "robots_left_hand" };
            CustomSynergies.Add("Lefty Loosey", mandatorySynergyItemsLeftyLoosey);
            //NEUTRINO / QUAD LASER / MINING LASER
            List<string> mandatorySynergyItemsSoftnose = new List<string>() { "nn:neutrino" };
            List<string> optionalSynergyItemsSoftnose = new List<string>() { "mine_cutter", "quad_laser" };
            CustomSynergies.Add("Softnose", mandatorySynergyItemsSoftnose, optionalSynergyItemsSoftnose);
            //LEFTHANDEDNESS / WITCH PISTOL / BUNDLE OF WANDS
            List<string> mandatorySynergyItemsSinisterHanded = new List<string>() { "nn:lefthandedness" };
            List<string> optionalSynergyItemsSinisterHanded = new List<string>() { "witch_pistol", "bundle_of_wands" };
            CustomSynergies.Add("Sinister Handed", mandatorySynergyItemsSinisterHanded, optionalSynergyItemsSinisterHanded);
            //PENCIL / ERASER
            List<string> mandatorySynergyItemsStationery = new List<string>() { "nn:pencil", "nn:eraser" };
            CustomSynergies.Add("Stationary", mandatorySynergyItemsStationery);
            //MISTAKE BULLETS / ERASER
            List<string> mandatorySynergyItemsForBigMistakes = new List<string>() { "nn:mistake_bullets", "nn:eraser" };
            CustomSynergies.Add("For Big Mistakes", mandatorySynergyItemsForBigMistakes);
            //GRANDFATHER GLOCK / FINGER GUNS / LEFTHANDEDNESS
            List<string> mandatorySynergyItemsMechanicalHands = new List<string>() { "nn:grandfather_glock" };
            List<string> optionalSynergyItemsMechanicalHands = new List<string>() { "nn:lefthandedness", "nn:finger_guns" };
            CustomSynergies.Add("Mechanical Hands", mandatorySynergyItemsMechanicalHands, optionalSynergyItemsMechanicalHands);
            //GUSTY / CYCLONE BOOTS
            List<string> mandatorySynergyItemsGaleForce = new List<string>() { "nn:gusty", "nn:cyclone_boots" };
            CustomSynergies.Add("Gale Force", mandatorySynergyItemsGaleForce);
            //SHATTERBLANK / RING OF TRIGGERS / CHANCE BULLETS / FLAK BULLETS
            List<string> mandatorySynergyItemsFragMental = new List<string>() { "nn:shatterblank" };
            List<string> optionalSynergyItemsFragMental = new List<string>() { "ring_of_triggers", "chance_bullets", "flak_bullets" };
            CustomSynergies.Add("Frag Mental", mandatorySynergyItemsFragMental, optionalSynergyItemsFragMental);
            //BLANKANNON / GLASS AMMOLET
            List<string> mandatorySynergyItemsPanedExpression = new List<string>() { "nn:blankannon", "nn:glass_ammolet" };
            CustomSynergies.Add("Paned Expression", mandatorySynergyItemsPanedExpression);
            //BLANKANNON / ELDER BLANK
            List<string> mandatorySynergyItemsSecretsOfTheAncients = new List<string>() { "nn:blankannon", "elder_blank" };
            CustomSynergies.Add("Secrets of the Ancients", mandatorySynergyItemsSecretsOfTheAncients);
            //MAP FRAGMENT / TATTERED MAP / SCROLL OF EXACT KNOWLEDGE
            List<string> mandatorySynergyItemsRestoration = new List<string>() { "nn:map_fragment" };
            List<string> optionalSynergyItemsRestoration = new List<string>() { "nn:scroll_of_exact_knowledge", "nn:tattered_map" };
            CustomSynergies.Add("Restoration", mandatorySynergyItemsRestoration, optionalSynergyItemsRestoration);
            //MAP FRAGMENT / BRICK OF CASH / KALIBER'S EYE / VENUSIAN BARS
            List<string> mandatorySynergyItemsTrustInTheAllSeeing = new List<string>() { "nn:map_fragment" };
            List<string> optionalSynergyItemsTrustInTheAllSeeing = new List<string>() { "brick_of_cash", "nn:venusian_bars", "nn:kalibers_eye" };
            CustomSynergies.Add("Trust In The All-Seeing", mandatorySynergyItemsTrustInTheAllSeeing, optionalSynergyItemsTrustInTheAllSeeing);
            //CLOAK OF DARKNESS / SILVER ITEMS
            List<string> mandatorySynergyItemsCloakAndMirrors = new List<string>() { "nn:cloak_of_darkness" };
            List<string> optionalSynergyItemsCloakAndMirrors = new List<string>() { "nn:mirror_bullets", "nn:silver_ammolet", "silver_bullets" };
            CustomSynergies.Add("Cloak and Mirrors", mandatorySynergyItemsCloakAndMirrors, optionalSynergyItemsCloakAndMirrors);
            //SYRINGES
            List<string> mandatorySynergyItemsSpun = new List<string>() { "antibody", "nn:blood_thinner", "nn:booster_shot" };
            CustomSynergies.Add("Spun", mandatorySynergyItemsSpun);
            //MINUTE GUN / BULLET TIME / GRANDFATHERGLOCK
            List<string> mandatorySynergyItemsNickOfTime = new List<string>() { "nn:minute_gun" };
            List<string> optionalSynergyItemsNickOfTime = new List<string>() { "nn:grandfather_glock", "bullet_time" };
            CustomSynergies.Add("Nick Of Time", mandatorySynergyItemsNickOfTime, optionalSynergyItemsNickOfTime);
            //BOMBARDIER SHELLS / MISTAKE BULLETS
            List<string> mandatorySynergyItemsForwardThinking = new List<string>() { "nn:bombardier_shells", "nn:mistake_bullets" };
            CustomSynergies.Add("Forward Thinking", mandatorySynergyItemsForwardThinking);
            //SPLATTERSHOT/+1BULLETS
            List<string> mandatorySynergyItemsThreePlusOneEqualsFour = new List<string>() { "nn:splattershot", "+1_bullets" };
            CustomSynergies.Add("3 + 1 = 4", mandatorySynergyItemsThreePlusOneEqualsFour);
            //KEYMOLLET + AMMOLOCK
            List<string> mandatorySynergyItemsUnderLockAndKey = new List<string>() { "nn:keymmolet", "nn:ammolock" };
            CustomSynergies.Add("Under Lock And Key", mandatorySynergyItemsUnderLockAndKey);
            //PESTIFEROUS LEAD + ALPHA BULLETS
            List<string> mandatorySynergyItemsAddedEffectPlague = new List<string>() { "nn:pestiferous_lead", "alpha_bullets" };
            CustomSynergies.Add("Added Effect - Plague", mandatorySynergyItemsAddedEffectPlague);
            //PESTIFEROUS LEAD + OMEGA BULLETS
            List<string> mandatorySynergyItemsAmethystWeapon = new List<string>() { "nn:pestiferous_lead", "omega_bullets" };
            CustomSynergies.Add("Amethyst Weapon", mandatorySynergyItemsAmethystWeapon);
            #endregion

            #region ShadowsAndSorcerySynergies
            //Y BEAM / SHIT
            List<string> mandatorySynergyItemsCenterfold = new List<string>() { "nn:y_beam" };
            List<string> optionalSynergyItemsCenterfold = new List<string>() { "laser_sight", "origuni", "book_of_chest_anatomy" };
            CustomSynergies.Add("Centerfold", mandatorySynergyItemsCenterfold, optionalSynergyItemsCenterfold);
            //WELROD / WELGUN
            List<string> mandatorySynergyItemsWelWelWel = new List<string>() { "nn:welrod", "nn:welgun" };
            CustomSynergies.Add("Wel Wel Wel", mandatorySynergyItemsWelWelWel);
            //ACCELERATOR / ACCELERANT
            List<string> mandatorySynergyItemsAccelent = new List<string>() { "nn:accelerant", "nn:accelerator" };
            CustomSynergies.Add("Accelent", mandatorySynergyItemsAccelent);
            //SHOULDER HOLSTER / HIP HOLSTER
            List<string> mandatorySynergyItemsHeadsShouldersKneesAndToes = new List<string>() { "nn:shoulder_holster", "hip_holster" };
            CustomSynergies.Add("Heads, Shoulders, Knees, and Toes", mandatorySynergyItemsHeadsShouldersKneesAndToes);
            //UTERINE POLYP / GROSS ITEMS
            List<string> mandatorySynergyItemsWombular = new List<string>() { "nn:uterine_polyp" };
            List<string> optionalSynergyItemsWombular = new List<string>() { "nn:orgun", "tear_jerker", "mutation" };
            CustomSynergies.Add("Wombular", mandatorySynergyItemsWombular, optionalSynergyItemsWombular);
            //WITHERING CHAMBER / SEVEN LEAF CLOVER
            List<string> mandatorySynergyItemsChamrock = new List<string>() { "nn:withering_chamber" };
            List<string> optionalSynergyItemsChamrock = new List<string>() { "seven_leaf_clover" };
            CustomSynergies.Add("Chamrock", mandatorySynergyItemsChamrock, optionalSynergyItemsChamrock);
            //RC360 / AIMBOT
            List<string> mandatorySynergyItemsICallHacks = new List<string>() { "nn:rc_360", "nn:aimbot" };
            CustomSynergies.Add("I Call Hacks", mandatorySynergyItemsICallHacks);
            //NECROMANCERS RIGHT HAND / THE SPRINTING DEAD
            List<string> mandatorySynergyItemsTheSprintingDead = new List<string>() { "nn:necromancers_right_hand" };
            List<string> optionalSynergyItemsTheSprintingDead = new List<string>() { "ballistic_boots", "nn:speed_potion", "shotga_cola", "shotgun_coffee" };
            CustomSynergies.Add("The Sprinting Dead", mandatorySynergyItemsTheSprintingDead, optionalSynergyItemsTheSprintingDead);
            //NECROMANCERS RIGHT HAND / SKELETON ITEMS
            List<string> mandatorySynergyItemsRollDemBones = new List<string>() { "nn:necromancers_right_hand" };
            List<string> optionalSynergyItemsRollDemBones = new List<string>() { "nn:grim_blanks", "shellegun", "vertebraek47", "skull_spitter", "nn:uzi_spine_mm" };
            CustomSynergies.Add("Roll Dem Bones", mandatorySynergyItemsRollDemBones, optionalSynergyItemsRollDemBones);
            //ALBEDO / SPEED
            List<string> mandatorySynergyItemsWhiteEthesia = new List<string>() { "nn:albedo" };
            List<string> optionalSynergyItemsWhiteEthesia = new List<string>() { "nn:speed_potion", "potion_of_gun_friendship" };
            CustomSynergies.Add("White Ethesia", mandatorySynergyItemsWhiteEthesia, optionalSynergyItemsWhiteEthesia);
            //MAGNUM OPUS
            List<string> mandatorySynergyItemsMagnumOpus = new List<string>() { "nn:nigredo", "nn:albedo", "nn:citrinitas", "nn:rubedo" };
            CustomSynergies.Add("Magnum Opus", mandatorySynergyItemsMagnumOpus);
            //Ultravioleter Guon Stone
            List<string> mandatorySynergyItemsUltravioleterGuonStone = new List<string>() { "nn:ultraviolet_guon_stone" };
            List<string> optionalSynergyItemsUltravioleterGuonStone = new List<string>() { "nn:speed_potion", "+1_bullets", "amulet_of_the_pit_lord" };
            CustomSynergies.Add("Ultravioleter Guon Stone", mandatorySynergyItemsUltravioleterGuonStone, optionalSynergyItemsUltravioleterGuonStone);
            //Lead Soul / Steely Stoic Items
            List<string> mandatorySynergyItemsNoWillToBreak = new List<string>() { "nn:lead_soul" };
            List<string> optionalSynergyItemsNoWillToBreak = new List<string>() { "nn:tablet_of_order", "nn:key_bulwark", "knife_shield" };
            CustomSynergies.Add("No Will To Break", mandatorySynergyItemsNoWillToBreak, optionalSynergyItemsNoWillToBreak);
            //STARTER PISTOL / FLARE GUN
            List<string> mandatorySynergyItemsMixedSignals = new List<string>() { "nn:starter_pistol", "flare_gun" };
            CustomSynergies.Add("Mixed Signals", mandatorySynergyItemsMixedSignals);
            //Infraredder Guon Stone
            List<string> mandatorySynergyItemsInfraredderGuonStone = new List<string>() { "nn:infrared_guon_stone" };
            List<string> optionalSynergyItemsInfraredderGuonStone = new List<string>() { "nn:aimbot", "+1_bullets", "amulet_of_the_pit_lord" };
            CustomSynergies.Add("Infraredder Guon Stone", mandatorySynergyItemsInfraredderGuonStone, optionalSynergyItemsInfraredderGuonStone);
            //INFRARED GUON STONE/ ULTRAVIOLET GUON STONE
            List<string> mandatorySynergyItemsXenochrome = new List<string>() { "nn:infrared_guon_stone", "nn:ultraviolet_guon_stone" };
            CustomSynergies.Add("Xenochrome", mandatorySynergyItemsXenochrome);
            //INFRARED GUON STONE / CAMERA
            List<string> mandatorySynergyItemsInfraredCamera = new List<string>() { "nn:infrared_guon_stone", "camera" };
            CustomSynergies.Add("Infrared Camera", mandatorySynergyItemsInfraredCamera);
            //TEAR JERKER / GARBAGE
            List<string> mandatorySynergyItemsCowOnATrashFarm = new List<string>() { "tear_jerker" };
            List<string> optionalSynergyItemsCowOnATrashFarm = new List<string>() { "junk", "trashcannon", "klobbe" };
            CustomSynergies.Add("Cow, on a Trash Farm", mandatorySynergyItemsCowOnATrashFarm, optionalSynergyItemsCowOnATrashFarm);
            //IDENTITY CRISIS / PARAGLOCKS
            List<string> mandatorySynergyItemsAssociatedDissasociations = new List<string>() { "nn:identity_crisis", "nn:paraglocks" };
            CustomSynergies.Add("Associated Disassociations", mandatorySynergyItemsAssociatedDissasociations);
            //SALT GUN / PILLARS
            List<string> mandatorySynergyItemsPillarsOfSalt = new List<string>() { "nn:salt_gun" };
            List<string> optionalSynergyItemsPillarsOfSalt = new List<string>() { "wood_beam", "nn:pillarocket", "brick_breaker" };
            CustomSynergies.Add("Pillars Of Salt", mandatorySynergyItemsPillarsOfSalt, optionalSynergyItemsPillarsOfSalt);
            //SALT GUN / TABLE TECHS
            List<string> mandatorySynergyItemsTableSalt = new List<string>() { "nn:salt_gun" };
            List<string> optionalSynergyItemsTableSalt = new List<string>() {
                "table_tech_heat",
                "table_tech_blanks",
                "table_tech_rocket",
                "table_tech_sight",
                "table_tech_shotgun",
                "table_tech_money",
                "table_tech_stun",
                "table_tech_rage",
                "nn:table_tech_table",
                "nn:table_tech_speed",
                "nn:table_tech_invulnerability",
                "nn:table_tech_ammo",
                "nn:table_tech_guon",
                "nn:uns-table_tech",
                "portable_table_device",
                "nn:tabullets"
            };
            CustomSynergies.Add("Table Salt", mandatorySynergyItemsTableSalt, optionalSynergyItemsTableSalt);
            //BACK WARDER / KNIVES
            List<string> mandatorySynergyItemsBackstabber = new List<string>() { "nn:back_warder" };
            List<string> optionalSynergyItemsBackstabber = new List<string>() { "knife_shield", "excaliber", "huntsman", "nn:butchers_knife", "katana_bullets", "nn:combat_knife" };
            CustomSynergies.Add("Backstabber!", mandatorySynergyItemsBackstabber, optionalSynergyItemsBackstabber);
            // THE BRIDE / THE GROOM
            List<string> mandatorySynergyItemsShotgunWedding = new List<string>() { "nn:the_bride", "nn:the_groom" };
            CustomSynergies.Add("Shotgun Wedding", mandatorySynergyItemsShotgunWedding);
            //THE BRIDE / CHARMING
            List<string> mandatorySynergyItemsWuvTwueWuv = new List<string>() { "nn:the_bride" };
            List<string> optionalSynergyItemsWuvTwueWuv = new List<string>() { "nn:love_pistol", "shotgun_full_of_love" };
            CustomSynergies.Add("Wuv... twue wuv...", mandatorySynergyItemsWuvTwueWuv, optionalSynergyItemsWuvTwueWuv);
            //BIGSHOT / BOW / BOMB
            List<string> mandatorySynergyItemsHyperlinkBlocked = new List<string>() { "nn:big_shot" };
            List<string> optionalSynergyItemsHyperlinkBlocked = new List<string>() { "bow", "bomb", "bottle", "chicken_flute", "grappling_hook" };
            CustomSynergies.Add("Hyperlink Blocked", mandatorySynergyItemsHyperlinkBlocked, optionalSynergyItemsHyperlinkBlocked);
            //BIGSHOT / BULLET SIZE ITEMS
            List<string> mandatorySynergyItemsBiggestShot = new List<string>() { "nn:big_shot" };
            List<string> optionalSynergyItemsBiggestShot = new List<string>() { "fat_bullets", "stout_bullets", "nn:titan_bullets" };
            CustomSynergies.Add("BIGGEST SHOT", mandatorySynergyItemsBiggestShot, optionalSynergyItemsBiggestShot);
            //BIGSHOT / MONEY
            List<string> mandatorySynergyItemsDealOfALifetime = new List<string>() { "nn:big_shot" };
            List<string> optionalSynergyItemsDealOfALifetime = new List<string>() { "gilded_bullets", "brick_of_cash", "briefcase_of_cash", "nn:loose_change", "nn:rusty_casing" };
            CustomSynergies.Add("De4l 0f 4 Lif3tim3", mandatorySynergyItemsDealOfALifetime, optionalSynergyItemsDealOfALifetime);
            //GAYK-47 / MACHINE PISTOL
            List<string> mandatorySynergyItemsLoveExcitingAndNew = new List<string>() { "nn:gayk47", "machine_pistol" };
            AdvancedSynergyEntry LoveExcitingAndNew = CustomSynergies.Add("Love, Exciting and New", mandatorySynergyItemsLoveExcitingAndNew);
            LoveExcitingAndNew.ActiveWhenGunUnequipped = false;
            StatModifier loveExcitingAndNewShotSpeed = new StatModifier()
            {
                amount = 1.25f,
                statToBoost = PlayerStats.StatType.ProjectileSpeed,
                modifyType = StatModifier.ModifyMethod.MULTIPLICATIVE,
            };
            StatModifier loveExcitingAndNewFireRate = new StatModifier()
            {
                amount = 1.25f,
                statToBoost = PlayerStats.StatType.RateOfFire,
                modifyType = StatModifier.ModifyMethod.MULTIPLICATIVE,
            };
            LoveExcitingAndNew.statModifiers.Add(loveExcitingAndNewShotSpeed);
            LoveExcitingAndNew.statModifiers.Add(loveExcitingAndNewFireRate);
            //GAYK-47 / 
            List<string> mandatorySynergyItemsCantShootStraight = new List<string>() { "nn:gayk47" };
            List<string> optionalSynergyItemsCantShootStraight = new List<string>() { "eyepatch", "nn:rainbow_guon_stone" };
            AdvancedSynergyEntry CantShootStraight = CustomSynergies.Add("Can't Shoot Straight", mandatorySynergyItemsCantShootStraight, optionalSynergyItemsCantShootStraight);
            CantShootStraight.ActiveWhenGunUnequipped = false;
            StatModifier CantShootStraightAccuracy = new StatModifier()
            {
                amount = 3f,
                statToBoost = PlayerStats.StatType.Accuracy,
                modifyType = StatModifier.ModifyMethod.MULTIPLICATIVE,
            };
            StatModifier CantShootStraightBounce = new StatModifier()
            {
                amount = 1f,
                statToBoost = PlayerStats.StatType.AdditionalShotBounces,
                modifyType = StatModifier.ModifyMethod.ADDITIVE,
            };
            CantShootStraight.statModifiers.Add(CantShootStraightAccuracy);
            CantShootStraight.statModifiers.Add(CantShootStraightBounce);
            //SHRINK SHOT / ALPHA
            List<string> mandatorySynergyItemsAddedEffectShrink = new List<string>() { "nn:shrinkshot", "alpha_bullets" };
            CustomSynergies.Add("Added Effect - Shrink", mandatorySynergyItemsAddedEffectShrink);
            //SHRINK SHOT / OMEGA
            List<string> mandatorySynergyItemsTopazWeapon = new List<string>() { "nn:shrinkshot", "omega_bullets" };
            CustomSynergies.Add("Topaz Weapon", mandatorySynergyItemsTopazWeapon);
            //MINI GUN / SMALL
            List<string> mandatorySynergyItemsMicroAggressions = new List<string>() { "nn:mini_gun" };
            List<string> optionalSynergyItemsMicroAggressions = new List<string>() { "nanomachines" };
            CustomSynergies.Add("Micro Aggressions", mandatorySynergyItemsMicroAggressions, optionalSynergyItemsMicroAggressions);
            //FIRE LANCE / MAGIC
            List<string> mandatorySynergyItemsThereAreSomeWhoCallMe = new List<string>() { "nn:fire_lance" };
            List<string> optionalSynergyItemsThereAreSomeWhoCallMe = new List<string>() { "magic_bullets", "bundle_of_wands","hexagun","witch_pistol" };
            CustomSynergies.Add("There are some who call me...", mandatorySynergyItemsThereAreSomeWhoCallMe, optionalSynergyItemsThereAreSomeWhoCallMe);
            //ALPHA BEAM / ALPHA BULLETS
            List<string> mandatorySynergyItemsDualAlpha = new List<string>() { "nn:alpha_beam", "alpha_bullets" };
            AdvancedSynergyEntry DualAlpha = CustomSynergies.Add("Dual Alpha", mandatorySynergyItemsDualAlpha);
            LoveExcitingAndNew.ActiveWhenGunUnequipped = false;
            StatModifier DualAlphaDMG = new StatModifier()
            {
                amount = 1.5f,
                statToBoost = PlayerStats.StatType.Damage,
                modifyType = StatModifier.ModifyMethod.MULTIPLICATIVE,
            };
            LoveExcitingAndNew.statModifiers.Add(DualAlphaDMG);
            //ALPHA BEAM / LIGHT
            List<string> mandatorySynergyItemsAbsoluteRadiance = new List<string>() { "nn:alpha_beam" };
            List<string> optionalSynergyItemsAbsoluteRadiance = new List<string>() { "nn:lead_soul", "flash_ray", "light_gun", "sprun" };
            CustomSynergies.Add("Absolute Radiance", mandatorySynergyItemsAbsoluteRadiance, optionalSynergyItemsAbsoluteRadiance);
            //THE SHELL / SHELL
            List<string> mandatorySynergyItemsShellllehSllehSShell = new List<string>() { "nn:the_shell", "shell" };
            CustomSynergies.Add("ShellllehS llehSShell", mandatorySynergyItemsShellllehSllehSShell);
            //THE SHELL / SCATTERSHOT
            List<string> mandatorySynergyItemsShootYourShot = new List<string>() { "nn:the_shell", "scattershot" };
            CustomSynergies.Add("Shoot Your Shot", mandatorySynergyItemsShootYourShot);
            //HEMATIC ROUNDS
            List<string> mandatorySynergyItemsBloodTransfusion = new List<string>() { "nn:hematic_rounds" };
            List<string> optionalSynergyItemsBloodTransfusion = new List<string>() { "nn:bloodthirsty_bullets", "nn:organ_donor_card", "nn:blood_thinner", "nn:bloody_box" };
            CustomSynergies.Add("Blood Transfusion", mandatorySynergyItemsBloodTransfusion, optionalSynergyItemsBloodTransfusion);
            //MOLOTOV BUD / MOLOTOV LAUNCHER
            List<string> mandatorySynergyItemsMollyTov = new List<string>() { "nn:molotov_buddy", "molotov_launcher" };
            CustomSynergies.Add("Molly Tov", mandatorySynergyItemsMollyTov);
            //VACUUM GUN / POISON
            List<string> mandatorySynergyItemsPoisbulonial = new List<string>() { "nn:vacuum_gun" };
            List<string> optionalSynergyItemsPoisbulonial = new List<string>() { "irradiated_lead", "plague_pistol", "uranium_ammolet", "poison_vial" };
            CustomSynergies.Add("Poisbulonial", mandatorySynergyItemsPoisbulonial, optionalSynergyItemsPoisbulonial);
            //VACUUM GUN / ICE
            List<string> mandatorySynergyItemsBlizzbulonial = new List<string>() { "nn:vacuum_gun" };
            List<string> optionalSynergyItemsBlizzbulonial = new List<string>() { "frost_bullets", "freeze_ray", "ice_bomb", "glacier" };
            CustomSynergies.Add("Blizzbulonial", mandatorySynergyItemsBlizzbulonial, optionalSynergyItemsBlizzbulonial);
            //VACUUM GUN / FIRE
            List<string> mandatorySynergyItemsLeadbulonial = new List<string>() { "nn:vacuum_gun" };
            List<string> optionalSynergyItemsLeadbulonial = new List<string>() { "hot_lead", "nn:accelerant", "potion_of_lead_skin", "phoenix" };
            CustomSynergies.Add("Leadbulonial", mandatorySynergyItemsLeadbulonial, optionalSynergyItemsLeadbulonial);
            //VACUUM GUN / POOP
            List<string> mandatorySynergyItemsPoopulonial = new List<string>() { "nn:vacuum_gun" };
            List<string> optionalSynergyItemsPoopulonial = new List<string>() { "klobbe", "weird_egg", "nn:egg_salad","meatbun" };
            CustomSynergies.Add("Poopulonial", mandatorySynergyItemsPoopulonial, optionalSynergyItemsPoopulonial);
            //VACUUM GUN / BLOOD
            List<string> mandatorySynergyItemsBloodbulonial = new List<string>() { "nn:vacuum_gun" };
            List<string> optionalSynergyItemsBloodbulonial = new List<string>() { "bloody_9mm", "blood_brooch", "nn:hematic_rounds", "bloodied_scarf" };
            CustomSynergies.Add("Bloodbulonial", mandatorySynergyItemsBloodbulonial, optionalSynergyItemsBloodbulonial);
            //DOUBLE GUN / DOUBLE VISION
            List<string> mandatorySynergyItemsDoubleOrNothing = new List<string>() { "nn:double_gun", "double_vision" };
            CustomSynergies.Add("Double Or Nothing", mandatorySynergyItemsDoubleOrNothing);
            //HOLEY WATER / HOLEY GRAIL
            List<string> mandatorySynergyItemsTheLastCrusade = new List<string>() { "nn:holey_water", "holey_grail" };
            CustomSynergies.Add("The Last Crusade", mandatorySynergyItemsTheLastCrusade);
            //BOTTLE ROCKET
            List<string> mandatorySynergyItemsToxicSolutions = new List<string>() { "nn:bottle_rocket" };
            List<string> optionalSynergyItemsToxicSolutions = new List<string>() { "plunger", "poison_vial" };
            CustomSynergies.Add("Toxic Solutions", mandatorySynergyItemsToxicSolutions, optionalSynergyItemsToxicSolutions);
            //KALEIDOSCOPIC GUON STONE
            List<string> mandatorySynergyItemsKaleidoscopicerGuonStone = new List<string>() { "nn:kaleidoscopic_guon_stone" };
            List<string> optionalSynergyItemsKaleidoscopicerGuonStone = new List<string>() { "+1_bullets", "amulet_of_the_pit_lord", "scope" };
            CustomSynergies.Add("Kaleidoscopicer Guon Stone", mandatorySynergyItemsKaleidoscopicerGuonStone, optionalSynergyItemsKaleidoscopicerGuonStone);
            //red stone synergy
            List<string> mandatorySynergyItemsReddySteady = new List<string>() { "nn:kaleidoscopic_guon_stone" };
            List<string> optionalSynergyItemsReddySteady = new List<string>() { "red_guon_stone", "nn:chaos_ruby" };
            CustomSynergies.Add("Reddy Steady", mandatorySynergyItemsReddySteady, optionalSynergyItemsReddySteady);
            //orange stone synergy
            List<string> mandatorySynergyItemsOrangeUGlad = new List<string>() { "nn:kaleidoscopic_guon_stone" };
            List<string> optionalSynergyItemsOrangeUGlad = new List<string>() { "orange_guon_stone", "orange" };
            CustomSynergies.Add("Orange U Glad", mandatorySynergyItemsOrangeUGlad, optionalSynergyItemsOrangeUGlad);
            //yellow stone synergy
            List<string> mandatorySynergyItemsYellowThere = new List<string>() { "nn:kaleidoscopic_guon_stone" };
            List<string> optionalSynergyItemsYellowThere = new List<string>() { "nn:yellow_guon_stone", "yellow_chamber" };
            CustomSynergies.Add("Yellow There", mandatorySynergyItemsYellowThere, optionalSynergyItemsYellowThere);
            //green stone synergy
            List<string> mandatorySynergyItemsGreenBehindTheEars = new List<string>() { "nn:kaleidoscopic_guon_stone" };
            List<string> optionalSynergyItemsGreenBehindTheEars = new List<string>() { "green_guon_stone", "gamma_ray" };
            CustomSynergies.Add("Green Behind The Ears", mandatorySynergyItemsGreenBehindTheEars, optionalSynergyItemsGreenBehindTheEars);
            //blue stone synergy
            List<string> mandatorySynergyItemsDaBaDeeDaBaDie = new List<string>() { "nn:kaleidoscopic_guon_stone" };
            List<string> optionalSynergyItemsDaBaDeeDaBaDie = new List<string>() { "blue_guon_stone", "gungeon_blueprint" };
            CustomSynergies.Add("da ba dee da ba die", mandatorySynergyItemsDaBaDeeDaBaDie, optionalSynergyItemsDaBaDeeDaBaDie);
            //purple stone synergy
            List<string> mandatorySynergyItemsTomorrowOrJustTheEndOfTime = new List<string>() { "nn:kaleidoscopic_guon_stone" };
            List<string> optionalSynergyItemsTomorrowOrJustTheEndOfTime = new List<string>() { "nn:ultraviolet_guon_stone", "nn:purple_prose" };
            CustomSynergies.Add("Tomorrow, or just the end of time?", mandatorySynergyItemsTomorrowOrJustTheEndOfTime, optionalSynergyItemsTomorrowOrJustTheEndOfTime);
            //MARCH GUN / SHOES
            List<string> mandatorySynergyItemsTappyToes = new List<string>() { "nn:march_gun" };
            List<string> optionalSynergyItemsTappyToes = new List<string>() { "ballistic_boots", "heavy_boots", "nn:bullet_boots" ,"gunboots", "bug_boots", "springheel_boots", "rat_boots", "nn:cyclone_boots", "nn:legboot", "nn:blank_boots" };
            CustomSynergies.Add("Tappy Toes", mandatorySynergyItemsTappyToes, optionalSynergyItemsTappyToes);
            //MARCH GUN / MUSIC
            List<string> mandatorySynergyItemsStepToTheBeat = new List<string>() { "nn:march_gun" };
            List<string> optionalSynergyItemsStepToTheBeat = new List<string>() { "face_melter", "metronome", "gunzheng", "really_special_lute" };
            CustomSynergies.Add("Step To The Beat", mandatorySynergyItemsStepToTheBeat, optionalSynergyItemsStepToTheBeat);
            //Heat Ray / Heat Items
            List<string> mandatorySynergyItemsReheat = new List<string>() { "nn:heat_ray" };
            List<string> optionalSynergyItemsReheat = new List<string>() { "table_tech_heat", "phoenix", "nn:hot_glue_gun", "hot_lead" };
            CustomSynergies.Add("Re-Heat", mandatorySynergyItemsReheat, optionalSynergyItemsReheat);
            //Heat Ray Bullshit
            List<string> mandatorySynergyItemsHeatRayBullshit = new List<string>() { "nn:heat_ray" };
            List<string> optionalSynergyHeatRayBullshit = new List<string>() { "nn:greek_fire", "nn:lvl._2_molotov", "nn:ring_of_oddly_specific_benefits" };
            CustomSynergies.Add("It won't actually show up on screen, but there's no hard limit on how long you can make synergy names.", mandatorySynergyItemsHeatRayBullshit, optionalSynergyHeatRayBullshit);
            // Heat ray / Don't trust the toaster
            List<string> mandatorySynergyItemsDontTrustTheToaster = new List<string>() { "nn:heat_ray" };
            List<string> optionalSynergyItemsDontTrustTheToaster = new List<string>() { "nn:molotov_buddy", "nn:#1_boss_mug" };
            CustomSynergies.Add("Don't Trust The Toaster", mandatorySynergyItemsDontTrustTheToaster, optionalSynergyItemsDontTrustTheToaster);
            //GAXE / DIAMOND ITEMS
            List<string> mandatorySynergyItemsDiamondGaxe = new List<string>() { "nn:gaxe" };
            List<string> optionalSynergyItemsDiamondGaxe = new List<string>() { "nn:diamond_gun", "nn:miners_bullets", "nn:diamond_bracelet" };
            CustomSynergies.Add("Diamond Gaxe", mandatorySynergyItemsDiamondGaxe, optionalSynergyItemsDiamondGaxe);
            //BIG SHOT / pipis
            List<string> mandatorySynergyItemspipis = new List<string>() { "nn:big_shot" };
            List<string> optionalSynergyItemspipis = new List<string>() { "megahand", "sunglasses", "pea_shooter" };
            CustomSynergies.Add("pipis", mandatorySynergyItemspipis, optionalSynergyItemspipis);
            //PANIC PENDANT / ADRENALINE
            List<string> mandatorySynergyItemsPlanB = new List<string>() { "nn:panic_pendant" };
            List<string> optionalSynergyItemsPlanB = new List<string>() { "muscle_relaxant", "shotgun_coffee" };
            CustomSynergies.Add("Plan B", mandatorySynergyItemsPlanB, optionalSynergyItemsPlanB);
            //REBONDIR / BOUNCE ITEMS
            List<string> mandatorySynergyItemsRebondissement = new List<string>() { "nn:rebondir" };
            List<string> optionalSynergyItemsRebondissement = new List<string>() { "bouncy_bullets", "saa" };
            CustomSynergies.Add("Rebondissement", mandatorySynergyItemsRebondissement, optionalSynergyItemsRebondissement);
            //REBONDIR / RICO
            List<string> mandatorySynergyItemsSuperBounceBros = new List<string>() { "nn:rebondir", "nn:rico" };
            CustomSynergies.Add("Super Bounce Bros", mandatorySynergyItemsSuperBounceBros);
            //POP GUN / SHOTGA COLA
            List<string> mandatorySynergyItemsSodaPop = new List<string>() { "nn:pop_gun", "shotga_cola" };
            AdvancedSynergyEntry SodaPop = CustomSynergies.Add("Soda Pop", mandatorySynergyItemsSodaPop);
            SodaPop.ActiveWhenGunUnequipped = false;
            StatModifier SodaPopDMG = new StatModifier()
            {
                amount = 2f,
                statToBoost = PlayerStats.StatType.ProjectileSpeed,
                modifyType = StatModifier.ModifyMethod.MULTIPLICATIVE,
            };
            SodaPop.statModifiers.Add(SodaPopDMG);
            //DIAMOND CUTTER / RANGER ITEMS
            List<string> mandatorySynergyItemsRangerClass = new List<string>() { "nn:diamond_cutter" };
            List<string> optionalSynergyItemsRangerClass = new List<string>() { "bow", "nn:magic_quiver", "nn:ranger" };
            CustomSynergies.Add("Ranger Class", mandatorySynergyItemsRangerClass, optionalSynergyItemsRangerClass);
            //STICK GUN / QUICK, DRAW!
            List<string> mandatorySynergyItemsQuickDraw = new List<string>() { "nn:stick_gun" };
            List<string> optionalSynergyItemsQuickDraw = new List<string>() { "nn:pencil", "hip_holster" };
            CustomSynergies.Add("Quick, Draw!", mandatorySynergyItemsQuickDraw, optionalSynergyItemsQuickDraw);
            //BOXING GLOVE / JOKES
            List<string> mandatorySynergyItemsPunchLine = new List<string>() { "boxing_glove" };
            List<string> optionalSynergyItemsPunchLine = new List<string>() { "nn:clown_shotgun", "clown_mask", "jk47" };
            CustomSynergies.Add("Punch Line", mandatorySynergyItemsPunchLine, optionalSynergyItemsPunchLine);
            //BOXING GLOVE / ICE
            List<string> mandatorySynergyItemsNorthStar = new List<string>() { "boxing_glove" };
            List<string> optionalSynergyItemsNorthStar = new List<string>() { "frost_bullets" };
            CustomSynergies.Add("North Star", mandatorySynergyItemsNorthStar, optionalSynergyItemsNorthStar);
            //COPPER SIDEARM / COPPER AMMOLET
            List<string> mandatorySynergyItemsCopOut = new List<string>() { "nn:copper_sidearm", "copper_ammolet" };
            CustomSynergies.Add("Cop-Out", mandatorySynergyItemsCopOut);
            //MARBLED UZI / EYES
            List<string> mandatorySynergyItemsGorgunsGaze = new List<string>() { "nn:marbled_uzi" };
            List<string> optionalSynergyItemsGorgunsGaze = new List<string>() { "nn:kalibers_eye", "nn:bloodshot_eye", "bloody_eye", "nn:shades_eye", "nn:cartographers_eye", "eye_of_the_beholster" };
            CustomSynergies.Add("Gorgun's Gaze", mandatorySynergyItemsGorgunsGaze, optionalSynergyItemsGorgunsGaze);
            //LIGHTNING ROD / ELECTRIC ITEMS
            List<string> mandatorySynergyItemsStormRod = new List<string>() { "nn:lightning_rod" };
            List<string> optionalSynergyItemsStormRod = new List<string>() { "shock_rounds", "thunderclap", "sunlight_javelin" };
            CustomSynergies.Add("Storm Rod", mandatorySynergyItemsStormRod, optionalSynergyItemsStormRod);
            //RUSTY SHOTGUN
            List<string> mandatorySynergyItemsProperCareAndMaintenance = new List<string>() { "nn:rusty_shotgun" };
            List<string> optionalSynergyItemsProperCareAndMaintenance = new List<string>() { "nn:gun_grease", "sponge", "nn:toolbox" };
            CustomSynergies.Add("Proper Care & Maintenance", mandatorySynergyItemsProperCareAndMaintenance, optionalSynergyItemsProperCareAndMaintenance);
            //RUSTY SHOTGUN / RUST IN PEACE
            List<string> mandatorySynergyItemsRustInPeace = new List<string>() { "nn:rusty_shotgun" };
            List<string> optionalSynergyItemsRustInPeace = new List<string>() { "nn:rusty_casing" };
            AdvancedSynergyEntry RustInPeace = CustomSynergies.Add("Rust In Peace", mandatorySynergyItemsRustInPeace, optionalSynergyItemsRustInPeace);
            RustInPeace.ActiveWhenGunUnequipped = false;
            RustInPeace.statModifiers.Add(new StatModifier()
            {
                amount = 1.2f,
                statToBoost = PlayerStats.StatType.RateOfFire,
                modifyType = StatModifier.ModifyMethod.MULTIPLICATIVE,
            });
            RustInPeace.statModifiers.Add(new StatModifier()
            {
                amount = 2f,
                statToBoost = PlayerStats.StatType.AdditionalClipCapacityMultiplier,
                modifyType = StatModifier.ModifyMethod.MULTIPLICATIVE,
            });
            //CLICKER / ONE CLICK AWAY
            List<string> mandatorySynergyItemsOneClickAway = new List<string>() { "nn:clicker" };
            List<string> optionalSynergyItemsOneClickAway = new List<string>() { "nn:big_shot", "mailbox" };
            CustomSynergies.Add("One Click Away!", mandatorySynergyItemsOneClickAway, optionalSynergyItemsOneClickAway);
            //Uzi Spinemm / Bones
            List<string> mandatorySynergyItemsTheBoneZone = new List<string>() { "nn:uzi_spine_mm" };
            List<string> optionalSynergyItemsTheBoneZone = new List<string>() { "skull_spitter", "vertebraek47" };
            CustomSynergies.Add("The Bone Zone", mandatorySynergyItemsTheBoneZone, optionalSynergyItemsTheBoneZone);
            //Obrien Fist / Green Items
            List<string> mandatorySynergyItemsTheGreenRoomPale = new List<string>() { "nn:obrien_fist" };
            List<string> optionalSynergyItemsTheGreenRoomPale = new List<string>() { "green_guon_stone", "the_membrane" };
            CustomSynergies.Add("The Green Room Pale", mandatorySynergyItemsTheGreenRoomPale, optionalSynergyItemsTheGreenRoomPale);
            //Barrel Chamber / The Barrel
            List<string> mandatorySynergyItemsDoubleBarrelled = new List<string>() { "nn:barrel_chamber", "barrel" };
            CustomSynergies.Add("Double Barrelled", mandatorySynergyItemsDoubleBarrelled);
            //Teapot / Space items
            List<string> mandatorySynergyItemsRusselsTeapot = new List<string>() { "teapot" };
            List<string> optionalSynergyItemsRusselsTeapot = new List<string>() { "mr_accretion_jr", "nn:moonrock" };
            CustomSynergies.Add("Russel's Teapot", mandatorySynergyItemsRusselsTeapot, optionalSynergyItemsRusselsTeapot);
            //Fun Guy / Pointy
            List<string> mandatorySynergyItemsPointiestOne = new List<string>() { "nn:fun_guy" };
            List<string> optionalSynergyItemsPointiestOne = new List<string>() { "katana_bullets", "excaliber", "unicorn_horn", "kruller_glaive" };
            CustomSynergies.Add("Pointiest One", mandatorySynergyItemsPointiestOne, optionalSynergyItemsPointiestOne);
            //Big Shot / Keygen
            List<string> mandatorySynergyItemsKEYGEN = new List<string>() { "nn:keygen", "nn:big_shot" };
            CustomSynergies.Add("KEYGEN", mandatorySynergyItemsKEYGEN);
            #endregion

            #region OMITBGoBrrrSynergies
            //RED ROBIN / BAT ITEMS
            List<string> mandatorySynergyItemsManbatAndRobin = new List<string>() { "nn:red_robin" };
            List<string> optionalSynergyItemsManbatAndRobin = new List<string>() { "nn:bullatterer" };
            CustomSynergies.Add("Manbat and Robin", mandatorySynergyItemsManbatAndRobin, optionalSynergyItemsManbatAndRobin);
            //RED ROBIN / BIRD ITEMS
            List<string> mandatorySynergyItemsScarletTanager = new List<string>() { "nn:red_robin" };
            List<string> optionalSynergyItemsScarletTanager = new List<string>() { "owl", "nn:birdshot", "nn:purpler", "chicken_flute", "nn:fowl_ring", "turkey" };
            CustomSynergies.Add("Scarlet Tanager", mandatorySynergyItemsScarletTanager, optionalSynergyItemsScarletTanager);
            //SKORPION
            List<string> mandatorySynergyItemsSkorpionSting = new List<string>() { "nn:skorpion" };
            List<string> optionalSynergyItemsSkorpionSting = new List<string>() { "rattler", "stinger" };
            CustomSynergies.Add("Skorpion Sting", mandatorySynergyItemsSkorpionSting, optionalSynergyItemsSkorpionSting);
            //DICE GRENADE / CHAOS ITEMS
            List<string> mandatorySynergyItemsRollWithInitiative = new List<string>() { "nn:dice_grenade" };
            List<string> optionalSynergyItemsRollWithInitiative = new List<string>() { "chaos_bullets", "nn:gayk47", "nn:bullet_shuffle" };
            CustomSynergies.Add("Roll With Advantage", mandatorySynergyItemsRollWithInitiative, optionalSynergyItemsRollWithInitiative);
            //PEARL AMMOLET / BUBBLE BLASTER
            List<string> mandatorySynergyItemsBubbleBlowingBaby = new List<string>() { "nn:pearl_ammolet" };
            List<string> optionalSynergyItemsBubbleBlowingBaby = new List<string>() { "bubble_blaster", "nn:soap_gun" };
            CustomSynergies.Add("Bubble Blowing, Baby!", mandatorySynergyItemsBubbleBlowingBaby, optionalSynergyItemsBubbleBlowingBaby);
            //REDHAWK / REDROBIN
            List<string> mandatorySynergyItemsRedbirds = new List<string>() { "nn:red_robin", "nn:redhawk" };
           AdvancedSynergyEntry RedBirds = CustomSynergies.Add("Redbirds", mandatorySynergyItemsRedbirds);
            RedBirds.ActiveWhenGunUnequipped = false;
            RedBirds.statModifiers.Add(new StatModifier()
            {
                amount = 1.3f,
                statToBoost = PlayerStats.StatType.RateOfFire,
                modifyType = StatModifier.ModifyMethod.MULTIPLICATIVE,
            });
            RedBirds.statModifiers.Add(new StatModifier()
            {
                amount = 0.8f,
                statToBoost = PlayerStats.StatType.Accuracy,
                modifyType = StatModifier.ModifyMethod.MULTIPLICATIVE,
            });
            //PAINTBALL GUN / BLACK ITEMS
            List<string> mandatorySynergyItemsPaintItBlack = new List<string>() { "nn:paintball_gun" };
            List<string> optionalSynergyItemsPaintItBlack = new List<string>() { "nn:black_holster", "black_hole_gun" };
            AdvancedSynergyEntry PaintItBlack = CustomSynergies.Add("Paint It Black", mandatorySynergyItemsPaintItBlack, optionalSynergyItemsPaintItBlack);
            //LIBRAM OF THE CHAMBER / CHAMBER MASTERS
            List<string> mandatorySynergyItemsChamberMasters = new List<string>() { "nn:libram_of_the_chambers" };
            List<string> optionalSynergyItemsChamberMasters = new List<string>() { "yellow_chamber", "sixth_chamber", "oiled_cylinder", 
                "nn:nitroglycylinder", "nn:glass_chamber", "nn:flame_chamber", "nn:springloaded_chamber", "nn:withering_chamber", "nn:heavy_chamber", "nn:cyclopean_cylinder", "nn:recyclinder", "nn:barrel_chamber" };
            CustomSynergies.Add("Chamber Masters", mandatorySynergyItemsChamberMasters, optionalSynergyItemsChamberMasters);
            #endregion

            #region TheIntermediaryFrontier
            //BLACKER GUON STONE
            List<string> mandatorySynergyItemsBlackerGuonStone = new List<string>() { "nn:black_guon_stone" };
            List<string> optionalSynergyItemsBlackerGuonStone = new List<string>() { "+1_bullets", "amulet_of_the_pit_lord", "nn:black_holster" };
            CustomSynergies.Add("Blacker Guon Stone", mandatorySynergyItemsBlackerGuonStone, optionalSynergyItemsBlackerGuonStone);
            //BLACK GUON STONE + SHRINKSHOT
            List<string> mandatorySynergyItemsSchwarzschildRadius = new List<string>() { "nn:black_guon_stone", "nn:shrinkshot" };
            CustomSynergies.Add("Schwarzschild Radius", mandatorySynergyItemsSchwarzschildRadius);
            //Pepper Poppers + Gungeon Pepper
            List<string> mandatorySynergyItemsPepperX = new List<string>() { "nn:pepper_poppers", "gungeon_pepper" };
            CustomSynergies.Add("Pepper X", mandatorySynergyItemsPepperX);
            //Pepper Poppers + Pickled Pepper
            List<string> mandatorySynergyItemsPickledPoppers = new List<string>() { "nn:pepper_poppers", "nn:pickled_pepper" };
            CustomSynergies.Add("Pickled Poppers", mandatorySynergyItemsPickledPoppers);
            //Schwarzlose / Water items
            List<string> mandatorySynergyItemsWaterJacket = new List<string>() { "nn:schwarzlose" };
            List<string> optionalSynergyItemsWaterJacket = new List<string>() { "mega_douser", "bottle", "heart_bottle", "nn:holey_water", "nn:bottle_rocket" };
            CustomSynergies.Add("Water Jacket", mandatorySynergyItemsWaterJacket, optionalSynergyItemsWaterJacket);
            //RECTANGULAR MIRROR + TABLE TECH SPEED
            List<string> mandatorySynergyItemsFastPass = new List<string>() { "nn:rectangular_mirror", "nn:table_tech_speed" };
            CustomSynergies.Add("Fast Pass", mandatorySynergyItemsFastPass);
            //RECTANGULAR MIRROR + SCULPTORS CHISEL
            List<string> mandatorySynergyItemsSterling = new List<string>() { "nn:rectangular_mirror", "nn:sculptors_chisel" };
            CustomSynergies.Add("Sterling", mandatorySynergyItemsSterling);
            //BLASTING CAP / PERCUSSION CAP
            List<string> mandatorySynergyItemsScreamosynthesis = new List<string>() { "nn:blasting_cap", "nn:percussion_cap" };
            CustomSynergies.Add("Screamosynthesis", mandatorySynergyItemsScreamosynthesis);
            //PERCUSSION CAP / FUN GUY
            List<string> mandatorySynergyItemsMushAdoAboutNothing = new List<string>() { "nn:fun_guy", "nn:percussion_cap" };
            CustomSynergies.Add("Mush Ado About Nothing", mandatorySynergyItemsMushAdoAboutNothing);
            //DARC PISTOL
            List<string> mandatorySynergyItemsDARCPistol = new List<string>() { "nn:arc_pistol" };
            List<string> optionalSynergyItemsDARCPistol = new List<string>() { "riddle_of_lead", "nn:cloak_of_darkness", "dark_marker" };
            CustomSynergies.Add("DARC Pistol", mandatorySynergyItemsDARCPistol, optionalSynergyItemsDARCPistol);
            #endregion
        }

    }
}
