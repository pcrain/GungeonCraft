using NpcApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LootTableAPI;
using UnityEngine;
using GungeonAPI;

namespace CwaffingTheGungy
{
    public static class Boomhildr
    {
        public static PrototypeDungeonRoom shopRoom;
        public static GenericLootTable BoomhildrLootTable;
        public static void Init()
        {
            ETGMod.Databases.Strings.Core.AddComplex("#BOOMHILDR_GENERIC_TALK", "Explosions are the spice of life! ...and death.");
            ETGMod.Databases.Strings.Core.AddComplex("#BOOMHILDR_GENERIC_TALK", "Name's Boomhildr, demolitions expert, self taught, at yer service.");
            ETGMod.Databases.Strings.Core.AddComplex("#BOOMHILDR_GENERIC_TALK", "My legs? Got taken clean off by a dynamite explosion! It's what made me want to go into pyrotechnics!");
            ETGMod.Databases.Strings.Core.AddComplex("#BOOMHILDR_GENERIC_TALK", "My arm? ...it was bitten off by a gator, why do you ask.");
            ETGMod.Databases.Strings.Core.AddComplex("#BOOMHILDR_GENERIC_TALK", "My eye? It was shot out. By a bomb. With a gun.");
            ETGMod.Databases.Strings.Core.AddComplex("#BOOMHILDR_GENERIC_TALK", "What am I here for?... none of your business.");
            ETGMod.Databases.Strings.Core.AddComplex("#BOOMHILDR_GENERIC_TALK", "There was a little claymore running around here a while ago, screaming something about 'Booms'. Pretty cool guy.");
            ETGMod.Databases.Strings.Core.AddComplex("#BOOMHILDR_GENERIC_TALK", "Some weirdo keeps coming by and calling me an 'Angel of Death'. What's up with that?");
            ETGMod.Databases.Strings.Core.AddComplex("#BOOMHILDR_GENERIC_TALK", "I've got all the boom you could ask for. Boom comin' out the ears.");
            ETGMod.Databases.Strings.Core.AddComplex("#BOOMHILDR_GENERIC_TALK", "Me and Cursula knew each other in college... before she got into mumbo jumbo.");
            ETGMod.Databases.Strings.Core.AddComplex("#BOOMHILDR_GENERIC_TALK", "My mama always used to say that frag grenades are the fireworks of explosive warfare.");

            ETGMod.Databases.Strings.Core.AddComplex("#BOOMHILDR_STOPPER_TALK", "Not now, got powder to mix, fuses to braid, you know the deal.");
            ETGMod.Databases.Strings.Core.AddComplex("#BOOMHILDR_STOPPER_TALK", "I'm busy riggin' up for a big blast.");
            ETGMod.Databases.Strings.Core.AddComplex("#BOOMHILDR_STOPPER_TALK", "Listen pal, I've only got so much patience for blabbermouths.");

            ETGMod.Databases.Strings.Core.AddComplex("#BOOMHILDR_PURCHASE_TALK", "Thanks for the cash.");
            ETGMod.Databases.Strings.Core.AddComplex("#BOOMHILDR_PURCHASE_TALK", "Have fun blowing &%*! up.");
            ETGMod.Databases.Strings.Core.AddComplex("#BOOMHILDR_PURCHASE_TALK", "Send 'em to kingdom come for me!");
            ETGMod.Databases.Strings.Core.AddComplex("#BOOMHILDR_PURCHASE_TALK", "Boom-boom-boom-boom, amirite?");

            ETGMod.Databases.Strings.Core.AddComplex("#BOOMHILDR_NOSALE_TALK", "Sorry mate, nothin' in this world is free.");
            ETGMod.Databases.Strings.Core.AddComplex("#BOOMHILDR_NOSALE_TALK", "Good explosives aren't cheap.");
            ETGMod.Databases.Strings.Core.AddComplex("#BOOMHILDR_NOSALE_TALK", "I'm not running a charity here.");

            ETGMod.Databases.Strings.Core.AddComplex("#BOOMHILDR_INTRO_TALK", "You're back. And with all your limbs too.");
            ETGMod.Databases.Strings.Core.AddComplex("#BOOMHILDR_INTRO_TALK", "Blow up anyone special lately?");
            ETGMod.Databases.Strings.Core.AddComplex("#BOOMHILDR_INTRO_TALK", "See any good blasts out there in the Gungeon?");
            ETGMod.Databases.Strings.Core.AddComplex("#BOOMHILDR_INTRO_TALK", "Want a bomb? I've got bombs. Let's talk.");
            ETGMod.Databases.Strings.Core.AddComplex("#BOOMHILDR_INTRO_TALK", "Bombs? Bombs? Bombs? You want it? It's yours, my friend- as long as you have enough cash.");

            ETGMod.Databases.Strings.Core.AddComplex("#BOOMHILDR_ATTACKED_TALK", "Watch it buster.");
            ETGMod.Databases.Strings.Core.AddComplex("#BOOMHILDR_ATTACKED_TALK", "No need to be jealous.");
            ETGMod.Databases.Strings.Core.AddComplex("#BOOMHILDR_ATTACKED_TALK", "You're gonna blow your chances.");
            ETGMod.Databases.Strings.Core.AddComplex("#BOOMHILDR_ATTACKED_TALK", "I can bring this whole chamber down on our heads, and you're attacking me?");

            List<int> LootTable = new List<int>()
            {
                108, //Bomb
                109, //Ice Bomb
                460, //Chaff Grenade
                66, //Proximity Mine
                308, //Cluster Mine
                136, //C4
                252, //Air Strike
                443, //Big Boy
                567, //Roll Bomb
                234, //iBomb Companion App
                438, //Explosive Decoy
                403, //Melted Rock
                304, //Explosive Rounds
                312, //Blast Helmet
                398, //Table Tech Rocket
                440, //Ruby Bracelet
                601, //Big Shotgun
                4, //Sticky Crossbow
                542, //Strafe Gun
                96, //M16
                6, //Zorgun
                81, //Deck4rd
                274, //Dark Marker
                39, //RPG
                19, //Grenade Launcher
                92, //Stinger
                563, //The Exotic
                129, //Com4nd0
                372, //RC Rocket
                16, //Yari Launcher
                332, //Lil Bomber
                180, //Grasschopper
                593, //Void Core Cannon
                362, //Bullet Bore
                186, //Machine Fist
                28, //Mailbox
                339, //Mahoguny
                478, //Banana

                // NitroBullets.NitroBulletsID,
                // AntimatterBullets.AntimatterBulletsID,
                // BombardierShells.BombardierShellsID,
                // Blombk.BlombkID,
                // Nitroglycylinder.NitroglycylinderID,
                // GunpowderPheromones.GunpowderPheromonesID,
                // RocketMan.RocketManID,
                // ChemGrenade.ChemGrenadeID,
                // InfantryGrenade.InfantryGrenadeID,
                // BomberJacket.ID,
                // Bombinomicon.ID,
                // MagicMissile.ID,
                // GrenadeShotgun.GrenadeShotgunID,
                // Felissile.ID,
                // TheThinLine.ID,
                // RocketPistol.ID,
                // Demolitionist.DemolitionistID,
                // HandMortar.ID,
                // FireLance.FireLanceID,
                // DynamiteLauncher.DynamiteLauncherID,
                // BottleRocket.ID,
                // NNBazooka.BazookaID,
                // BoomBeam.ID,
                // Pillarocket.ID,
                // BlastingCap.ID,
            };

            BoomhildrLootTable = LootTableTools.CreateLootTable();
            foreach (int i in LootTable)
            {
                BoomhildrLootTable.AddItemToPool(i);
            }

            GameObject boomhildrObj = ItsDaFuckinShopApi.SetUpShop(
                         "Boomhildr",
                         "cg",
                         new List<string>()
                         {
                        "CwaffingTheGungy/Resources/NPCSprites/Boomhildr/boomhildr_idle_001",
                        "CwaffingTheGungy/Resources/NPCSprites/Boomhildr/boomhildr_idle_002",
                        "CwaffingTheGungy/Resources/NPCSprites/Boomhildr/boomhildr_idle_003",
                        "CwaffingTheGungy/Resources/NPCSprites/Boomhildr/boomhildr_idle_004",
                        "CwaffingTheGungy/Resources/NPCSprites/Boomhildr/boomhildr_idle_005",
                         },
                         7,
                         new List<string>()
                         {
                        "CwaffingTheGungy/Resources/NPCSprites/Boomhildr/boomhildr_talk_001",
                        "CwaffingTheGungy/Resources/NPCSprites/Boomhildr/boomhildr_talk_002",
                         },
                         3,
                         BoomhildrLootTable,
                         CustomShopItemController.ShopCurrencyType.COINS,
                         "#BOOMHILDR_GENERIC_TALK",
                         "#BOOMHILDR_STOPPER_TALK",
                         "#BOOMHILDR_PURCHASE_TALK",
                         "#BOOMHILDR_NOSALE_TALK",
                         "#BOOMHILDR_INTRO_TALK",
                         "#BOOMHILDR_ATTACKED_TALK",
                         new Vector3(0.5f, 4, 0),
                         ItsDaFuckinShopApi.defaultItemPositions,
                         1f,
                         false,
                         null,
                         null,
                         null,
                         null,
                         null,
                         null,
                         null,
                         null,
                         true,
                         false,
                         null,
                         true,
                         "CwaffingTheGungy/Resources/NPCSprites/Boomhildr/boomhildr_mapicon",
                         true,
                         0.1f
                         );
            shopRoom = RoomFactory.BuildFromResource("CwaffingTheGungy/Resources/EmbeddedRooms/BoomhildrRoom.room").room;
            ItsDaFuckinShopApi.RegisterShopRoom(boomhildrObj, shopRoom, new UnityEngine.Vector2(9f, 11));
        }
    }
}
