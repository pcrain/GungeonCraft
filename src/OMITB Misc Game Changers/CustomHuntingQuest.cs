using SaveAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NevernamedsItems
{
    class CustomHuntingQuest
    {
        public static void Init()
        {
            //--------------------------------------------Beastclaw
            CustomHuntQuests.AddQuest(
                CustomDungeonFlags.MISFIREBEAST_QUEST_COMPLETE,
                new List<string>() //Opening Dialogue
                {
                    "Times have changed, and new creatures emerge from the shadows!",
                    "Hunt 10 of the fickle feline Misfire Beasts, and return to us",
                },
                "Misfire Beasts",
                new List<string>() //Required GUIDs
                {
                    "45192ff6d6cb43ed8f1a874ab6bef316",
                },
                10, //Number of Kills
                null, //Reward Flags
                new List<CustomDungeonFlags>() //Custom Reward Flags
                {
                    CustomDungeonFlags.MISFIREBEAST_QUEST_REWARDED,
                },
                JammedEnemyState.NoCheck,
                delegate (AIActor aiactor, MonsterHuntProgress progress) //Extra Logic Check
                {
                    if (aiactor.GetComponent<DisplacedImageController>() == null) return true;
                    else return false;
                },
                null //Index
                );
           
            //--------------------------------------------Dynamite Launcher
            CustomHuntQuests.AddQuest(
                CustomDungeonFlags.NITRA_QUEST_COMPLETE,
                new List<string>() //Opening Dialogue
                {
                    "The rumblings of explosions echo from the chambers below constantly, possibly loosening the very foundation of the Gungeon itself!",
                    "Hunt 35 Nitra to thin their numbers, just in case..."
                },
                "Nitra",
                new List<string>() //Required GUIDs
                {
                    "c0260c286c8d4538a697c5bf24976ccf",
                },
                35, //Number of Kills
                null, //Reward Flags
                new List<CustomDungeonFlags>() //Custom Reward Flags
                {
                    CustomDungeonFlags.NITRA_QUEST_REWARDED,
                },
                JammedEnemyState.NoCheck,
                delegate (AIActor aiactor, MonsterHuntProgress progress) //Extra Logic Check
                {
                    return true;
                },
                null //Index
                );

            //-----------------------------------------------Kaliber's Prayer
            CustomHuntQuests.AddQuest(
                CustomDungeonFlags.GUNCULTIST_QUEST_COMPLETE,
                new List<string>() //Opening Dialogue
                {
                    "Gungeoneers lose many things in the halls below... some of us even lose our minds.",
                    "Put 20 of the maddened Gun Cultists out of their misery, to slow the spread of the cult."
                },
                "Gun Cultists",
                new List<string>() //Required GUIDs
                {
                     "57255ed50ee24794b7aac1ac3cfb8a95",
                },
                20, //Number of Kills
                null, //Reward Flags
                new List<CustomDungeonFlags>() //Custom Reward Flags
                {
                    CustomDungeonFlags.GUNCULTIST_QUEST_REWARDED,
                },
                JammedEnemyState.NoCheck,
                delegate (AIActor aiactor, MonsterHuntProgress progress) //Extra Logic Check
                {
                    return true;
                },
                null //Index
                );

            //------------------------------------------------Phaser Spiderling
            CustomHuntQuests.AddQuest(
                CustomDungeonFlags.PHASERSPIDER_QUEST_COMPLETE,
                new List<string>() //Opening Dialogue
                {
                    "Creeping, crawling, legged things... they make me shudder.",
                    "Exterminate 20 of the disgusting Phaser Spiders and send them beyond the curtain once and for all!",
                    "...eugh...",
                },
                "Phaser Spiders",
                new List<string>() //Required GUIDs
                {
                     "98ca70157c364750a60f5e0084f9d3e2",
                },
                20, //Number of Kills
                null, //Reward Flags
                new List<CustomDungeonFlags>() //Custom Reward Flags
                {
                    CustomDungeonFlags.PHASERSPIDER_QUEST_REWARDED,
                },
                JammedEnemyState.NoCheck,
                delegate (AIActor aiactor, MonsterHuntProgress progress) //Extra Logic Check
                {
                    return true;
                },
                null //Index
                );

            //--------------------------------------------------Maroon Guon Stone           
            CustomHuntQuests.AddQuest(
                CustomDungeonFlags.JAMMEDBULLETKIN_QUEST_COMPLETE,
                new List<string>() //Opening Dialogue
                {
                    "An even more dangerous game awaits",
                    "New curses creep through the Gungeon's halls, and the numbers of the Jammed have risen!",
                    "Banish 100 Jammed Bullet Kin back to Bullet Hell!",
                    "...perhaps that strange fellow in red up by the railing can help you."
                },
                "Jammed Bullet Kin",
                new List<string>() //Required GUIDs
                {
                      "01972dee89fc4404a5c408d50007dad5",
                    "db35531e66ce41cbb81d507a34366dfe",
                    "88b6b6a93d4b4234a67844ef4728382c",
                    "70216cae6c1346309d86d4a0b4603045",
                    "df7fb62405dc4697b7721862c7b6b3cd",
                    "3cadf10c489b461f9fb8814abc1a09c1",
                    "8bb5578fba374e8aae8e10b754e61d62",
                    "e5cffcfabfae489da61062ea20539887",
                    "1a78cfb776f54641b832e92c44021cf2",
                    "d4a9836f8ab14f3fadd0f597438b1f1f",
                    "5f3abc2d561b4b9c9e72b879c6f10c7e",
                    "844657ad68894a4facb1b8e1aef1abf9",
                    "906d71ccc1934c02a6f4ff2e9c07c9ec",
                    "9eba44a0ea6c4ea386ff02286dd0e6bd",
                    "05cb719e0178478685dc610f8b3e8bfc",
                    "5861e5a077244905a8c25c2b7b4d6ebb",
                    "6f818f482a5c47fd8f38cce101f6566c",
                    "39e6f47a16ab4c86bec4b12984aece4c",
                    "699cd24270af4cd183d671090d8323a1",
                },
                100, //Number of Kills
                null, //Reward Flags
                new List<CustomDungeonFlags>() //Custom Reward Flags
                {
                    CustomDungeonFlags.JAMMEDBULLETKIN_QUEST_REWARDED,
                },
                JammedEnemyState.Jammed,
                delegate (AIActor aiactor, MonsterHuntProgress progress) //Extra Logic Check
                {
                    return true;
                },
                null //Index
                );

            //---------------------------------------------------Blight Shell
            CustomHuntQuests.AddQuest(
                CustomDungeonFlags.JAMMEDSHOTGUNKIN_QUEST_COMPLETE,
                new List<string>() //Opening Dialogue
                {
                    "The Jammed have marched onwards, and now the towering shells of the Shotgundead glow red with dark power!",
                    "Reduce 45 Jammed Shotgun Kin to ash and return to us, Jamslayer!",
                },
                "Jammed Shotgun Kin",
                new List<string>() //Required GUIDs
                {
                     "128db2f0781141bcb505d8f00f9e4d47",
                    "b54d89f9e802455cbb2b8a96a31e8259",
                    "2752019b770f473193b08b4005dc781f",
                    "7f665bd7151347e298e4d366f8818284",
                    "b1770e0f1c744d9d887cc16122882b4f",
                    "1bd8e49f93614e76b140077ff2e33f2b",
                    "044a9f39712f456597b9762893fbc19c",
                    "37340393f97f41b2822bc02d14654172",
                     "ddf12a4881eb43cfba04f36dd6377abb",
                     "86dfc13486ee4f559189de53cfb84107",
                },
                45, //Number of Kills
                null, //Reward Flags
                new List<CustomDungeonFlags>() //Custom Reward Flags
                {
                    CustomDungeonFlags.JAMMEDSHOTGUNKIN_QUEST_REWARDED,
                },
                JammedEnemyState.Jammed,
                delegate (AIActor aiactor, MonsterHuntProgress progress) //Extra Logic Check
                {
                    return true;
                },
                null //Index
                );

            //---------------------------------------------------Gunshark
            CustomHuntQuests.AddQuest(
                CustomDungeonFlags.JAMMEDBULLETSHARK_QUEST_COMPLETE,
                new List<string>() //Opening Dialogue
                {
                    "Something lurks in the waters...",
                    "Something... cursed.",
                    "Send 15 Jammed Bullet Sharks back to Davey Jones, and we shall reward you handsomely!"
                },
                "Jammed Bullet Shark",
                new List<string>() //Required GUIDs
                {
                     "72d2f44431da43b8a3bae7d8a114a46d",
                    "b70cbd875fea498aa7fd14b970248920",
                },
                15, //Number of Kills
                null, //Reward Flags
                new List<CustomDungeonFlags>() //Custom Reward Flags
                {
                    CustomDungeonFlags.JAMMEDBULLETSHARK_QUEST_REWARDED,
                },
                JammedEnemyState.Jammed,
                delegate (AIActor aiactor, MonsterHuntProgress progress) //Extra Logic Check
                {
                    return true;
                },
                null //Index
                );

            //----------------------------------------------------Maiden-Shaped Box
            CustomHuntQuests.AddQuest(
                CustomDungeonFlags.JAMMEDLEADMAIDEN_QUEST_COMPLETE,
                new List<string>() //Opening Dialogue
                {
                    "Your deadliest foe yet clanks just out of sight!",
                    "Singlehandedly ruining this Gungeon...",
                    "Disassemble 10 Jammed Lead Maidens, and save your fellow Gungeoneers a gruesome fate!"
                },
                "Jammed Lead Maiden",
                new List<string>() //Required GUIDs
                {
                     "cd4a4b7f612a4ba9a720b9f97c52f38c",
                    "9215d1a221904c7386b481a171e52859",
                },
                10, //Number of Kills
                null, //Reward Flags
                new List<CustomDungeonFlags>() //Custom Reward Flags
                {
                    CustomDungeonFlags.JAMMEDLEADMAIDEN_QUEST_REWARDED,
                },
                JammedEnemyState.Jammed,
                delegate (AIActor aiactor, MonsterHuntProgress progress) //Extra Logic Check
                {
                    return true;
                },
                null //Index
                );

            //--------------------------------------------------Bullet Blade
            CustomHuntQuests.AddQuest(
                CustomDungeonFlags.JAMMEDGUNNUT_QUEST_COMPLETE,
                new List<string>() //Opening Dialogue
                {
                    "The numbers of the Jammed lay decimated!",
                    "Congratulations, Jamslayer! I shall compose a ballad to your prowess in disposing demons!",
                    "... but one final accursed quarry remains.",
                    "Slay 20 Jammed Gun Nuts to truly claim the title of Hell Walker!",
                },
                "Jammed Gun Nut",
                new List<string>() //Required GUIDs
                {
                     "ec8ea75b557d4e7b8ceeaacdf6f8238c",
                    "463d16121f884984abe759de38418e48",
                    "383175a55879441d90933b5c4e60cf6f",
                },
                20, //Number of Kills
                null, //Reward Flags
                new List<CustomDungeonFlags>() //Custom Reward Flags
                {
                    CustomDungeonFlags.JAMMEDGUNNUT_QUEST_REWARDED,
                },
                JammedEnemyState.Jammed,
                delegate (AIActor aiactor, MonsterHuntProgress progress) //Extra Logic Check
                {
                    return true;
                },
                null //Index
                );
        }
    }
}
