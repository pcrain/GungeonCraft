// namespace CwaffingTheGungy;

// // Shop that sells items that tend to be most useful when acquired early and upgraded throughout the course of a run
// // (Couldn't figure out enough items to sell here for this to be interesting)
// public class WhiteMageScrapped
// {
//     public static void Init()
//     {
//         List<int> shopItems = new(){
//             Items.BookOfChestAnatomy, // if your run will involve breaking chests, might as well start earlier
//             Items.LamentConfigurum, // if you're gonna be spamming it as much as possible, might as well start earlier
//             Items.GungeonBlueprint, // getting a blueprint late in the run is pretty useless
//             Items.SenseOfDirection, // compasses are more useful earlier for speedrunning purposes
//             Items.GildedBullets, // effects compound with money, so if you save money all run these are incredibly nice
//             Items.Backpack, // extra active item slots are always useful early
//             Items.ShelletonKey, // keys are always useful early
//             Items.Evolver, // the gun grows with you throughout the run, obviously useful early
//             Items.Spice, // :3

//             // possibly TOO good early?
//             Items.GunderfuryLv10, // at higher levels it's extremely good
//             Items.PlatinumBullets, // effects compound with every enemy killed, so this is extremely good early on
//             Items.Akey47, // keys are always useful early

//             // IDs.Pickups["insurance_policy"],
//         };

//         List<string> moddedItems = new(){
//             "nn:bag_of_holding", // +10 active item slots are mostly useful early
//             "nn:pocket_chest",   // levels up with damage
//         };

//         PrototypeDungeonRoom shopRoom = FancyRoomBuilder.MakeFancyShop(
//             npcName                : "white_mage",
//             shopItems              : shopItems,
//             moddedItems            : moddedItems,
//             roomPath               : $"{C.MOD_INT_NAME}/Resources/Rooms/BasicShopRoom2.newroom",
//             allowDupes             : false,
//             costModifier           : 2f / 9f, // insurance should cost 2/9 of 90 == 20 casings
//             spawnChance            : 1.0f,
//             spawnFloors            : Floors.CASTLEGEON,
//             spawnPrerequisite      : CwaffPrerequisites.INSURANCE_PREREQUISITE,
//             prequisiteValidator    : CwaffPrerequisite.OnFirstFloor,
//             // spawnPrequisiteChecker : null,
//             talkPointOffset        : C.PIXEL_SIZE * new Vector2(7, 22),
//             npcPosition            : C.PIXEL_SIZE * new Vector2(10, 60),
//             itemPositions          : ShopAPI.defaultItemPositions.ShiftAll(C.PIXEL_SIZE * new Vector2(-25, 0)),
//             oncePerRun             : true,
//             // voice                  : "sans", // will play audio "Play_CHR_<voice>_voice_01"
//             genericDialog          : new(){
//                 "BUY SOMETHING PLEASE",
//                 },
//             stopperDialog          : new(){
//                 "BUY SOMETHING PLEASE",
//                 },
//             purchaseDialog         : new(){
//                 "BUY SOMETHING PLEASE",
//                 },
//             noSaleDialog           : new(){
//                 "BUY SOMETHING PLEASE",
//                 },
//             introDialog            : new(){
//                 "BUY SOMETHING PLEASE",
//                 },
//             attackedDialog         : new(){
//                 "BUY SOMETHING PLEASE",
//                 }
//             );
//         // shopRoom.ForceSpawnForDebugPurposes();
//     }
// }
