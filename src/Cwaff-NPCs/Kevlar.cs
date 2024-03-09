namespace CwaffingTheGungy;

public class Kevlar
{
    internal static bool _SpawnedThisRun = false;

    public static void Init()
    {
        // Reset our spawn status at the beginning of the run
        CwaffEvents.OnCleanStart += () => _SpawnedThisRun = false;

        List<int> shopItems = new(){
            IDs.Pickups["insurance_policy"],
        };

        FancyShopData shop = FancyShopBuilder.MakeFancyShop(
            npcName                : "kevlar",
            shopItems              : shopItems,
            moddedItems            : new(),
            roomPath               : $"{C.MOD_INT_NAME}/Resources/Rooms/insurance.newroom",
            allowDupes             : true,
            costModifier           : 3f / 9f, // insurance should cost 3/9 of 90 == 30 casings
            spawnChance            : 1.0f,
            spawnPrerequisite      : CwaffPrerequisites.INSURANCE_PREREQUISITE,
            prequisiteValidator    : PlayerHasGoodItem,
            idleFps                : 2,
            talkFps                : 2,
            // spawnPrequisiteChecker : null,
            flipTowardsPlayer      : false,
            talkPointOffset        : C.PIXEL_SIZE * new Vector2(26, 66),
            npcPosition            : C.PIXEL_SIZE * new Vector2(37, 80),
            itemPositions          : ShopAPI.defaultItemPositions.ShiftAll(C.PIXEL_SIZE * new Vector2(22, 20)),
            exactlyOncePerRun      : false,
            // voice                  : "sans", // will play audio "Play_CHR_<voice>_voice_01"
            genericDialog          : new(){
                "Hath ye no desire to be heir to yourself? For a price, I will make it so.",
                "Ye hath died before, as willt ye soon again. Maketh it, then, into a profitable affair!",
                "I ply ye to sign a contract. Idle talk procures no favours.",
                },
            stopperDialog          : new(){
                "Hath ye no desire to be heir to yourself? For a price, I will make it so.",
                "Ye hath died before, as willt ye soon again. Maketh it, then, into a profitable affair!",
                "I ply ye to sign a contract. Idle talk procures no favours.",
                },
            purchaseDialog         : new(){
                "Suffer me not to wait!",
                "Thankee, ye gentleman... or gentlewoman?",
                "We shall meet again, when ye art a worm's nest.",
                "No need for foppish forms. A deal struck is a deal honoured.",
                "God buy ye! ...Lest I might.",
                "Fare thee well.",
                "Pray remember me.",
                },
            stolenDialog           : new(){
                "A pick-purse!",
                "Begone with thine practices, foul shifter!",
                "Go your ways, caitiff!",
                "Go your ways, thy foul dandy-pratt!",
                "I baffle thine robberies!",
                },
            noSaleDialog           : new(){
                "I cry ye mercy? Ye hath not the balsam for this trade.",
                "Crave not what ye cannot afford.",
                "Be no addle-pot.",
                "Fallen upon ebb-waters, I see?",
                "I accept no fiddler's pay.",
                },
            introDialog            : new(){
                "Greetings, fellow well-met.",
                "Good morrow.",
                "Be at peace!",
                },
            attackedDialog         : new(){
                "Cease this senseless affray!",
                "Returneth to the bedlam from whence thou came!",
                "Caitiff!",
                "Idle hussy!",
                "Cur! Dog-bolt!",
                "Sirrah! Cease this at once!",
                "Hell and devil confound thee!",
                }
            );

        shop.AddParentedAnimationToShopFixed(ResMap.Get("kevlar_bow"), 10, "purchase");
        // shop.AddParentedAnimationToShopFixed(ResMap.Get("cammy_sad"), 4, "denied");
        shop.AddParentedAnimationToShopFixed(ResMap.Get("kevlar_offended"), 8, "stolen");
        shop.SetShotAnimation(paths: ResMap.Get("kevlar_offended"), fps: 8);

        shop.shop.AddComponent<PreventMultipleInsuranceShopSpawns>();
    }

    private static bool PlayerHasGoodItem(SpawnConditions conds)
    {
      if (_SpawnedThisRun)
        return false; // can't spawn if we already spawned this run

      foreach (PickupObject p in GameManager.Instance.PrimaryPlayer.AllItems())
        if (p.quality == ItemQuality.S || p.quality == ItemQuality.A)
            return true;

      if (GameManager.Instance.CurrentGameType != GameManager.GameType.COOP_2_PLAYER)
        return false;

      foreach (PickupObject p in GameManager.Instance.SecondaryPlayer.AllItems())
        if (p.quality == ItemQuality.S || p.quality == ItemQuality.A)
            return true;

      return false;
    }

    private class PreventMultipleInsuranceShopSpawns : MonoBehaviour
    {
        private void Start()
        {
            Kevlar._SpawnedThisRun = true;
        }
    }
}

