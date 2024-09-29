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

        bool fixedSpawn = CwaffConfig._Gunfig.Value(CwaffConfig._SHOP_KEY) == "Classic";

        FancyShopData shop = FancyShopBuilder.MakeFancyShop(
            npcName                : "kevlar",
            shopItems              : shopItems,
            moddedItems            : new(),
            roomPath               : $"{C.MOD_INT_NAME}/Resources/Rooms/insurance.newroom",
            allowDupes             : true, // allow multiple insurance policies
            allowExcluded          : true, // insurance policies are excluded items
            spawnChanceEachRun     : fixedSpawn ? 1.0f : 0.33f,
            spawnPrerequisite      : CwaffPrerequisites.INSURANCE_PREREQUISITE,
            prequisiteValidator    : fixedSpawn ? PlayerHasGoodItem : null,
            idleFps                : 2,
            talkFps                : 8,
            loopTalk               : false,
            // spawnPrequisiteChecker : null,
            flipTowardsPlayer      : false,
            talkPointOffset        : C.PIXEL_SIZE * new Vector2(26, 66),
            npcPosition            : C.PIXEL_SIZE * new Vector2(37, 80),
            itemPositions          : ShopAPI.defaultItemPositions.ShiftAll(C.PIXEL_SIZE * new Vector2(22, 20)),
            exactlyOncePerRun      : fixedSpawn ? false : true, //NOTE: must be false in classic mode to spawn as soon as the prequisiteValidator is fulfilled
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

        shop.AddParentedAnimationToShopFixed(ResMap.Get("kevlar_bow"), 8, "purchase");
        shop.AddParentedAnimationToShopFixed(ResMap.Get("kevlar_offended"), 8, "stolen");
        shop.SetShotAnimation(paths: ResMap.Get("kevlar_offended"), fps: 8);

        shop.shop.AddComponent<PreventMultipleInsuranceShopSpawns>();
        shop.shop.AddComponent<UpdateInsuranceSpritesToMatchCharacter>();
    }

    private static bool PlayerHasGoodItem(SpawnConditions conds)
    {
      if (_SpawnedThisRun)
        return false; // can't spawn if we already spawned this run

      //TODO: figure out why Kevlar was spawning on first floor without this check, since player should never have an S or A tier item otherwise
      if (CwaffPrerequisite.OnFirstFloor(conds))
        return false; // can't spawn on first floor no matter what

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

    private class UpdateInsuranceSpritesToMatchCharacter : MonoBehaviour
    {
        private void Start()
        {
            if (!base.gameObject || !base.gameObject.transform || base.gameObject.transform.parent is not Transform shopTransform)
                return;
            foreach (Transform child in shopTransform)
            {
                if (!child || !child.gameObject)
                    continue;
                CustomShopItemController[] shopItems = child.gameObject.GetComponentsInChildren<CustomShopItemController>();
                if (shopItems == null || shopItems.Length == 0)
                    continue;
                foreach (CustomShopItemController shopItem in shopItems)
                    if (shopItem.item is InsurancePolicy)
                        shopItem.sprite.SetSprite(InsurancePolicy.GetSpriteIdForCharacter());
            }
        }
    }


}
