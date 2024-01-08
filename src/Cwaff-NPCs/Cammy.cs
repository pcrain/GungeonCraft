namespace CwaffingTheGungy;

// Shop that sells companions :>
public class Cammy
{
    public static void Init()
    {
        List<int> shopItems = new(){
            (int)Items.Dog,
            (int)Items.Wolf,
            (int)Items.Owl,
            (int)Items.Turkey,
            (int)Items.Badge,
            (int)Items.TurtleProblem,
            (int)Items.SuperSpaceTurtle,
            (int)Items.BabyGoodMimic,
            (int)Items.BabyGoodShelleton,
            (int)Items.R2g2,
            (int)Items.BlankCompanionsRing,
            (int)Items.Pig,
            (int)Items.Wingman,
            (int)Items.SpaceFriend,
            (int)Items.ClownMask,
            (int)Items.ChickenFlute,
            (int)Items.Junkan,
            (int)Items.Gunther, // not a true companion, but close enough
        };

        List<string> moddedItems = new(){
            // Once More Into the Breach
            "nn:molotov_buddy",
            "nn:baby_good_chance_kin",
            "nn:potto",
            "nn:peanut",
            "nn:dark_prince",
            "nn:diode",
            "nn:drone",
            "nn:greg_the_egg",
            "nn:fun_guy",
            "nn:baby_good_det",
            "nn:angry_spirit",
            "nn:gusty",
            "nn:scroll_of_exact_knowledge",
            "nn:lil_munchy",
            "nn:hapulon",
            // Frost and Gunfire
            "kp:penguin",
            "kp:blue_balloon",
            "kp:b.f.o.",
            "kp:squire",
            "kp:baby_good_cannon_kin",
            // Expand the Gungeon
            "ex:baby_good_hammer",
            "ex:baby_sitter",
            // Children of Kaliber
            "cel:the_prime_core",
            "cel:pet_rock", // not a true companion, but close enough
            // Little Guy :D
            "lg:strange_root",
            // Bleaker Item Pack
            "bb:baby_good_shellicopter",
            // Planetside of Gunymede
            "psog:baby_good_candle",
            // Knife to a Gunfight
            "ski:baby_good_dodogama",
        };

        // if (C.DEBUG_BUILD)  // test some wonky offsets
        // {
        //     shopItems.Clear();
        //     moddedItems.Clear();
        //     moddedItems.Add("cg:alligator");
        //     moddedItems.Add("cg:platinum_star");
        //     moddedItems.Add("cg:alyx");
        // }

        FancyShopData shop = FancyShopBuilder.MakeFancyShop(
            npcName                : "cammy",
            shopItems              : shopItems,
            moddedItems            : moddedItems,
            roomPath               : "CwaffingTheGungy/Resources/Rooms/petshop.newroom",
            allowDupes             : false,
            costModifier           : 0.5f,
            spawnChance            : 1.0f,
            spawnPrerequisite      : CwaffPrerequisites.WHITE_MAGE_PREREQUISITE,
            // Guaranteed spawn on 1st floor
            allowedTilesets        : (int)( GlobalDungeonData.ValidTilesets.CASTLEGEON ),
            prequisiteValidator    : CwaffPrerequisite.OnFirstFloor,
            idleFps                : 6,
            talkFps                : 4,
            // prequisiteValidator    : null,
            // talkPointOffset        : C.PIXEL_SIZE * new Vector2(7, 22),
            talkPointOffset        : C.PIXEL_SIZE * new Vector2(19, 52),
            // npcPosition            : C.PIXEL_SIZE * new Vector2(10, 60),
            npcPosition            : C.PIXEL_SIZE * new Vector2(-3, 44),
            carpetOffset           : C.PIXEL_SIZE * new Vector2(-23, 0),
            itemPositions          : ShopAPI.defaultItemPositions.ShiftAll(C.PIXEL_SIZE * new Vector2(-25, 0)),
            exactlyOncePerRun      : true,
            canBeRobbed            : false,
            // voice                  : "sans", // will play audio "Play_CHR_<voice>_voice_01"
            genericDialog          : new(){
                "Aren't they just precious!?",
                "Bunch of cuties these ones!",
                "Just look at them!",
                },
            stopperDialog          : new(){
                "Aren't they just precious!?",
                "Bunch of cuties these ones!",
                "Just look at them!",
                },
            purchaseDialog         : new(){
                "I'm sure you'll take great care of them! :D",
                "They look happy to be with you! :D",
                "Thank you so much friend! :D",
                },
            noSaleDialog           : new(){
                "Ah you're a bit short on shells.",
                "I wish I could just give them away, but I have little mouths to feed!",
                "Sorry friend. D:",
                },
            introDialog            : new(){
                "Please consider adopting one of these beautiful babies!",
                "Lots of lovely little companions looking for a home!",
                "Welcome to the companion shop!",
                },
            attackedDialog         : new(){
                "Please be careful!!!",
                "Yikes!!!",
                "Aahhhh!!!",
                }
            );

        shop.AddParentedAnimationToShopFixed(ResMap.Get("cammy_excited"), 10, "purchase");
        shop.AddParentedAnimationToShopFixed(ResMap.Get("cammy_sad"), 4, "denied");
        shop.AddParentedAnimationToShopFixed(ResMap.Get("cammy_sad"), 4, "stolen");
        // shopRoom.ForceSpawnForDebugPurposes();
    }
}
