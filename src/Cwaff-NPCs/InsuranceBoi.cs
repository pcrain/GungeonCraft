namespace CwaffingTheGungy;

public class InsuranceBoi
{
    public static void Init()
    {
        List<int> shopItems = new(){
            IDs.Pickups["insurance_policy"],
        };

        PrototypeDungeonRoom shopRoom = FancyRoomBuilder.MakeFancyShop(
            npcName                : "insurance_boi",
            shopItems              : shopItems,
            roomPath               : "CwaffingTheGungy/Resources/Rooms/BasicShopRoom2.newroom",
            allowDupes             : true,
            spawnChance            : 0.5f,
            spawnFloors            : Floors.CASTLEGEON,
            spawnPrerequisite      : CwaffPrerequisites.TEST_PREREQUISITE,
            // spawnPrequisiteChecker : CwaffDungeonPrerequisite.OnFirstFloor,
            spawnPrequisiteChecker : null,
            talkPointOffset        : C.PIXEL_SIZE * new Vector2(7, 22),
            npcPosition            : C.PIXEL_SIZE * new Vector2(10, 60),
            itemPositions          : ShopAPI.defaultItemPositions.ShiftAll(C.PIXEL_SIZE * new Vector2(-25, 0)),
            oncePerRun             : true,
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
