using SaveAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NevernamedsItems
{
    class CheckUnlocks
    {
        public static void Init()
        {
            ETGModConsole.Commands.GetGroup("nn").AddUnit("checkunlocks", delegate (string[] args)
            {
                ETGModConsole.Log("<color=#00d6e6>    ---------------   Item Unlocks in Once More Into The Breach    --------------    </color>");
                ETGModConsole.Log("Unlocks requiring the player to do X amount of things can be done across multiple runs unless otherwise specified.");

                PrintUnlock(CustomDungeonFlags.PLAYERHELDMORETHANFIVEARMOUR, "Armoured Armour", "Hold 5 pieces of armour or more at once (11 as Robot).");
                PrintUnlock(CustomDungeonFlags.KILLEDJAMMEDKEYBULLETKIN, "Keybullet Effigy", "Kill a Jammed Keybullet Kin.");
                PrintUnlock(CustomDungeonFlags.KILLEDJAMMEDCHANCEKIN, "Chance Effigy", "Kill a Jammed Chance Kin.");
                PrintUnlock(CustomDungeonFlags.KILLEDJAMMEDMIMIC, "Bloody Box", "Kill a Jammed Mimic.");
                PrintUnlock(CustomDungeonFlags.HASBEENDAMAGEDBYRISKRIFLE, "Risky Ring", "Take damage to the Risk Rifle.");
                PrintUnlock(CustomDungeonFlags.FAILEDRATMAZE, "Cheese Heart", "Take too many wrong turns in the Rat's maze.");
                PrintUnlock(CustomDungeonFlags.USEDFALLENANGELSHRINE, "Dagger of the Aimgels", "Use a fallen angel shrine.");
                PrintUnlock(CustomDungeonFlags.KILLEDENEMYWITHTHROWNGUN, "Diamond Bracelet", "Kill an enemy with a thrown gun.");
                PrintUnlock(CustomDungeonFlags.USED_FALSE_BLANK_TEN_TIMES, "True Blank", "Use the False Blank ten times in one run.");
                PrintUnlock(CustomTrackedStats.TITAN_KIN_KILLED, "Titan Bullets", "Kill 5 Titan Bullet Kin;", 5);
                PrintUnlock(CustomDungeonFlags.HAS_BEATEN_BOSS_BY_SKIN_OF_TEETH, "Mutagen", "Kill a boss while one hit from death.");
                PrintUnlock(CustomDungeonFlags.ANGERED_BELLO, "Justice", "Make the Shopkeep very, very mad.");
                PrintUnlock(SaveAPIManager.GetPlayerMaximum(CustomTrackedMaximums.MAX_HEART_CONTAINERS_EVER) >= 8, "Orgun", "Have more than 8 heart containers at once.");
                PrintUnlock(CustomDungeonFlags.ROBOT_HELD_FIVE_JUNK, "Junkllets", "Hold more than five junk at once as the Robot.");
                PrintUnlock(CustomTrackedStats.CHARMED_ENEMIES_KILLED, "Kalibers Eye", "Kill 100 Charmed Enemies;", 100);
                PrintUnlock(CustomDungeonFlags.HURT_BY_SHROOMER, "Shroomed Gun", "Take damage to a Shroomer.");
                PrintUnlock(CustomDungeonFlags.UNLOCKED_MISSINGUNO, "Missinguno", "Open a Glitched Chest OR kill the Lich as the Paradox.");
                PrintUnlock(CustomDungeonFlags.FLOOR_CLEARED_WITH_CURSE, "Holey Water", "Clear a floor with an active... special curse.");
                PrintUnlock(CustomTrackedStats.JAMMED_CHESTS_OPENED, "Cursed Tumbler", "Open a Jammed Chest in All-Jammed mode.", 1, false);
                PrintUnlock(CustomTrackedStats.RUSTY_ITEMS_PURCHASED, "Rusty Casing", "Purchase 3 items from Rusty;", 3);
                PrintUnlock(CustomTrackedStats.RUSTY_ITEMS_STOLEN, "Rusty Shotgun", "Steal an item from Rusty.", 1, false);
                PrintUnlock(CustomDungeonFlags.CHEATED_DEATH_SHADE, "Shade's Eye", "Take damage as the Shade... and live!");

                //----------------------------------------------------------Specific Boss Kills
                ETGModConsole.Log("<color=#00d6e6>Boss Kill Tally Unlocks:</color>");//------------------------------------------------------------

                PrintUnlock(CustomTrackedStats.BEHOLSTER_KILLS, "The Beholster", "Kill 15 Beholsters;", 15);
                PrintUnlock(CustomTrackedStats.MINEFLAYER_KILLS, "Flayed Revolver", "Kill 10 Mine Flayers;", 10);

                //----------------------------------------------------------Dragun Unlocks
                ETGModConsole.Log("<color=#00d6e6>Dragun Unlocks:</color>");//------------------------------------------------------------

                PrintUnlock(CustomDungeonFlags.DRAGUN_KILLED_HUNTER, "Doggun", "Kill the Dragun as the Hunter.");
                PrintUnlock(CustomDungeonFlags.DRAGUN_KILLED_SHADE, "Shade Shot", "Kill the Dragun as the Shade.");

                //----------------------------------------------------------Lich Unlocks
                ETGModConsole.Log("<color=#00d6e6>Lich Unlocks:</color>");//------------------------------------------------------------

                PrintUnlock(CustomDungeonFlags.LICH_BEATEN_SHADE, "Lead Soul", "Kill the Lich as the Shade.");

                //----------------------------------------------------------BOSSRUSH
                ETGModConsole.Log("<color=#00d6e6>Bossrush Unlocks:</color>");//------------------------------------------------------------

                PrintUnlock(CustomDungeonFlags.BOSSRUSH_PILOT, "Keygen", "Beat Bossrush as the Pilot.");
                PrintUnlock(CustomDungeonFlags.BOSSRUSH_MARINE, "Shot in the Arm", "Beat Bossrush as the Marine.");
                PrintUnlock(CustomDungeonFlags.BOSSRUSH_CONVICT, "Lvl. 2 Molotov", "Beat Bossrush as the Convict.");
                PrintUnlock(CustomDungeonFlags.BOSSRUSH_HUNTER, "Boltcaster", "Beat Bossrush as the Hunter.");
                PrintUnlock(CustomDungeonFlags.BOSSRUSH_BULLET, "Carnwennan", "Beat Bossrush as the Bullet.");
                PrintUnlock(CustomDungeonFlags.BOSSRUSH_ROBOT, "Magnet", "Beat Bossrush as the Robot.");
                PrintUnlock(CustomDungeonFlags.BOSSRUSH_PARADOX, "Paraglocks", "Beat Bossrush as the Paradox.");
                PrintUnlock(CustomDungeonFlags.BOSSRUSH_GUNSLINGER, "Bullet Shuffle", "Beat Bossrush as the Gunslinger.");
                PrintUnlock(CustomDungeonFlags.BOSSRUSH_SHADE, "Jaws of Defeat", "Beat Bossrush as the Shade.");

                //----------------------------------------------------------RAINBOW MODE
                ETGModConsole.Log("<color=#00d6e6>Rainbow Mode Unlocks:</color>");//------------------------------------------------------------

                PrintUnlock(CustomDungeonFlags.RAINBOW_KILLED_LICH, "Rainbow Guon Stone", "Kill the Lich in Rainbow Mode.");

                //----------------------------------------------------------TURBO MODE
                ETGModConsole.Log("<color=#00d6e6>Turbo Mode Unlocks:</color>"); //------------------------------------------------------------

                PrintUnlock(CustomDungeonFlags.BEATEN_KEEP_TURBO_MODE, "Chaos Ruby", "Beat the Keep of the Lead Lord on Turbo Mode.");
                PrintUnlock(CustomDungeonFlags.BEATEN_MINES_BOSS_TURBO_MODE, "Ringer", "Beat the Black Powder Mine on Turbo Mode.");
                PrintUnlock(CustomDungeonFlags.BEATEN_HOLLOW_BOSS_TURBO_MODE, "Supersonic Shots", "Beat the Hollow on Turbo Mode.");

                //----------------------------------------------------------ADVANCED DRAGUN
                ETGModConsole.Log("<color=#00d6e6>Advanced Dragun Unlocks:</color>"); //------------------------------------------------------------

                PrintUnlock(CustomDungeonFlags.ADVDRAGUN_KILLED_ROBOT, "Electrum Rounds", "Kill the Advanced Dragun as the Robot.");
                PrintUnlock(CustomDungeonFlags.ADVDRAGUN_KILLED_SHADE, "Redhawk", "Kill the Advanced Dragun as the Shade.");

                //----------------------------------------------------------ALL JAMMED MODE
                ETGModConsole.Log("<color=#00d6e6>All-Jammed Mode Unlocks:</color>"); //------------------------------------------------------------

                PrintUnlock(CustomDungeonFlags.ALLJAMMED_BEATEN_KEEP, "Hallowed Bullets", "Beat the Keep on All-Jammed Mode.");
                PrintUnlock(CustomDungeonFlags.ALLJAMMED_BEATEN_OUB, "Chemical Burn", "Beat the Oubliette on All-Jammed Mode.");
                PrintUnlock(CustomDungeonFlags.ALLJAMMED_BEATEN_PROPER, "Silver Guon Stone", "Beat the Gungeon Proper on All-Jammed Mode.");
                PrintUnlock(CustomDungeonFlags.ALLJAMMED_BEATEN_ABBEY, "Gunidae solvit Haatelis", "Beat the Abbey of the True Gun on All-Jammed Mode.");
                PrintUnlock(CustomDungeonFlags.ALLJAMMED_BEATEN_MINES, "Bloodthirsty Bullets", "Beat the Black Powder Mines on All-Jammed Mode.");
                PrintUnlock(CustomDungeonFlags.ALLJAMMED_BEATEN_RAT, "Ammo Trap", "Beat the Resourceful Rat on All-Jammed Mode.");
                PrintUnlock(CustomDungeonFlags.ALLJAMMED_BEATEN_HOLLOW, "Mirror Bullets", "Beat the Hollow on All-Jammed Mode.");
                PrintUnlock(CustomDungeonFlags.ALLJAMMED_BEATEN_OFFICE, "Scroll of Exact Knowledge", "Beat the R&G Dept on All-Jammed Mode.");
                PrintUnlock(CustomDungeonFlags.ALLJAMMED_BEATEN_FORGE, "Cloak of Darkness", "Beat the Forge on All-Jammed Mode.");
                PrintUnlock(CustomDungeonFlags.ALLJAMMED_BEATEN_HELL, "Gun of a Thousand Sins", "Beat Bullet Hell on All-Jammed Mode.");

                //----------------------------------------------------------CHALLENGE MODES
                ETGModConsole.Log("<color=#00d6e6>Challenge Run Unlocks:</color>"); //------------------------------------------------------------
                ETGModConsole.Log("View all challenges and how to activate them by entering 'nnchallenges' into the console.");

                PrintUnlock(CustomDungeonFlags.CHALLENGE_WHATARMY_BEATEN, "Tablet of Order", "Kill the Dragun on the 'What Army?' Challenge.");
                PrintUnlock(CustomDungeonFlags.CHALLENGE_INVISIBLEO_BEATEN, "Ring of Invisibility", "Kill the Dragun on the 'Invisible-O' Challenge.");
                PrintUnlock(CustomDungeonFlags.CHALLENGE_TOILANDTROUBLE_BEATEN, "Witches Brew", "Kill the Dragun on the 'Toil And Trouble' Challenge.");
                PrintUnlock(CustomDungeonFlags.CHALLENGE_KEEPITCOOL_BEATEN, "Blizzkrieg", "Beat the Hollow on the 'Keep It Cool' Challenge.");

                //----------------------------------------------------------HUNTING QUESTS
                ETGModConsole.Log("<color=#00d6e6>Hunting Quest Unlocks:</color>"); //------------------------------------------------------------
                ETGModConsole.Log("See Frifle and Mauser for details.");

                PrintUnlock(CustomDungeonFlags.MISFIREBEAST_QUEST_REWARDED, "Beastclaw", null);
                PrintUnlock(CustomDungeonFlags.NITRA_QUEST_REWARDED, "Dynamite Launcher", null);
                PrintUnlock(CustomDungeonFlags.GUNCULTIST_QUEST_REWARDED, "Kalibers Prayer", null);
                PrintUnlock(CustomDungeonFlags.PHASERSPIDER_QUEST_REWARDED, "Phaser Spiderling", null);
                PrintUnlock(CustomDungeonFlags.JAMMEDBULLETKIN_QUEST_REWARDED, "Maroon Guon Stone", null);
                PrintUnlock(CustomDungeonFlags.JAMMEDSHOTGUNKIN_QUEST_REWARDED, "Blight Shell", null);
                PrintUnlock(CustomDungeonFlags.JAMMEDLEADMAIDEN_QUEST_REWARDED, "Maiden-Shaped Box", null);
                PrintUnlock(CustomDungeonFlags.JAMMEDBULLETSHARK_QUEST_REWARDED, "Gunshark", null);
                PrintUnlock(CustomDungeonFlags.JAMMEDGUNNUT_QUEST_REWARDED, "Bullet Blade", null);

            });
        }
        public static void PrintUnlock(CustomDungeonFlags flag, string itemname, string unlockPrereqs)
        {
            if (SaveAPIManager.GetFlag(flag))
            {
                ETGModConsole.Log($"{itemname} <color=#04eb00>[Unlocked]</color>!");
            }
            else { ETGModConsole.Log($"{itemname}  <color=#ff0000ff>[Locked]</color> {(string.IsNullOrEmpty(unlockPrereqs) ? "" : "-")} {unlockPrereqs}"); }
        }
        public static void PrintUnlock(CustomTrackedStats flag, string itemname, string unlockPrereqs, int req, bool showCounter = true)
        {
            if (SaveAPIManager.GetPlayerStatValue(flag) >= req)
            {
                ETGModConsole.Log($"{itemname} <color=#04eb00>[Unlocked]</color>!");
            }
            else
            {
                string counter = $"[{SaveAPIManager.GetPlayerStatValue(flag)}/{req}]";
                if (!showCounter) counter = "";
                ETGModConsole.Log($"{itemname} <color=#ff0000ff>[Locked]</color> - {unlockPrereqs} {counter}");
            }
        }
        public static void PrintUnlock(bool customOverride, string itemname, string unlockPrereqs)
        {
            if (customOverride)
            {
                ETGModConsole.Log($"{itemname} <color=#04eb00>[Unlocked]</color>!");
            }
            else { ETGModConsole.Log($"{itemname}  <color=#ff0000ff>[Locked]</color> {(string.IsNullOrEmpty(unlockPrereqs) ? "" : "-")} {unlockPrereqs}"); }
        }
    }
}
