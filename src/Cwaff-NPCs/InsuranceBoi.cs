namespace CwaffingTheGungy;

public class InsuranceBoi
{
    public static void Init()
    {
        List<int> shopItems = new(){
            IDs.Pickups["insurance_policy"],
        };

        List<string> moddedItems = new(){
            // "nn:grandfather_glock",
            // "nn:arc_tactical",
        };

        FancyShopData shop = FancyRoomBuilder.MakeFancyShop(
            npcName                : "insurance_boi",
            shopItems              : shopItems,
            moddedItems            : moddedItems,
            roomPath               : "CwaffingTheGungy/Resources/Rooms/BasicShopRoom2.newroom",
            allowDupes             : true,
            costModifier           : 2f / 9f, // insurance should cost 2/9 of 90 == 20 casings
            spawnChance            : 1.0f,
            spawnPrerequisite      : CwaffPrerequisites.INSURANCE_PREREQUISITE,
            prequisiteValidator    : CwaffPrerequisite.OnFirstFloor,
            // spawnPrequisiteChecker : null,
            talkPointOffset        : C.PIXEL_SIZE * new Vector2(7, 22),
            npcPosition            : C.PIXEL_SIZE * new Vector2(10, 60),
            itemPositions          : ShopAPI.defaultItemPositions.ShiftAll(C.PIXEL_SIZE * new Vector2(-25, 0)),
            exactlyOncePerRun      : true,
            // voice                  : "sans", // will play audio "Play_CHR_<voice>_voice_01"
            genericDialog          : new(){
                "BUY SOMETHING PLEASE",
                },
            stopperDialog          : new(){
                "BUY SOMETHING PLEASE",
                },
            purchaseDialog         : new(){
                "BUY SOMETHING PLEASE",
                },
            noSaleDialog           : new(){
                "BUY SOMETHING PLEASE",
                },
            introDialog            : new(){
                "BUY SOMETHING PLEASE",
                },
            attackedDialog         : new(){
                "BUY SOMETHING PLEASE",
                }
            );
        // shopRoom.ForceSpawnForDebugPurposes();
    }
}
