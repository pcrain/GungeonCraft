namespace CwaffingTheGungy;

// Bartering Shop NPC
public class Bart
{
    internal static GenericLootTable _BarterTable = null;

    internal static string _BarterSpriteS = null;
    internal static string _BarterSpriteA = null;
    internal static string _BarterSpriteB = null;
    internal static string _BarterSpriteC = null;

    public static void Init()
    {
        // We need to find all loaded items, so defer initialization for now
        CwaffEvents.OnAllModsLoaded += SetupBarterTable;

        System.Diagnostics.Stopwatch tempWatch = System.Diagnostics.Stopwatch.StartNew();
        // ETGModConsole.Log($"Timing old atlas adjusment code");
        // _BarterSpriteS = ShopAPI.AddCustomCurrencyType(ResMap.Get("barter_s_icon")[0]+".png", $"{C.MOD_PREFIX}:S_TIER_ITEM", Assembly.GetCallingAssembly());
        // _BarterSpriteA = ShopAPI.AddCustomCurrencyType(ResMap.Get("barter_a_icon")[0]+".png", $"{C.MOD_PREFIX}:A_TIER_ITEM", Assembly.GetCallingAssembly());
        // _BarterSpriteB = ShopAPI.AddCustomCurrencyType(ResMap.Get("barter_b_icon")[0]+".png", $"{C.MOD_PREFIX}:B_TIER_ITEM", Assembly.GetCallingAssembly());
        // _BarterSpriteC = ShopAPI.AddCustomCurrencyType(ResMap.Get("barter_c_icon")[0]+".png", $"{C.MOD_PREFIX}:C_TIER_ITEM", Assembly.GetCallingAssembly());
        // tempWatch.Stop(); ETGModConsole.Log($"  finished in "+(tempWatch.ElapsedMilliseconds/1000.0f)+" seconds");

        // tempWatch = System.Diagnostics.Stopwatch.StartNew();
        ETGModConsole.Log($"Timing new atlas adjusment code");
        _BarterSpriteS = AtlasFixer.BetterAddCustomCurrencyType(ResMap.Get("barter_s_icon")[0]+".png", $"{C.MOD_PREFIX}:S_TIER_ITEM", Assembly.GetCallingAssembly());
        _BarterSpriteA = AtlasFixer.BetterAddCustomCurrencyType(ResMap.Get("barter_a_icon")[0]+".png", $"{C.MOD_PREFIX}:A_TIER_ITEM", Assembly.GetCallingAssembly());
        _BarterSpriteB = AtlasFixer.BetterAddCustomCurrencyType(ResMap.Get("barter_b_icon")[0]+".png", $"{C.MOD_PREFIX}:B_TIER_ITEM", Assembly.GetCallingAssembly());
        _BarterSpriteC = AtlasFixer.BetterAddCustomCurrencyType(ResMap.Get("barter_c_icon")[0]+".png", $"{C.MOD_PREFIX}:C_TIER_ITEM", Assembly.GetCallingAssembly());
        tempWatch.Stop(); ETGModConsole.Log($"  finished in "+(tempWatch.ElapsedMilliseconds/1000.0f)+" seconds");

        List<int> shopItems      = new();
        List<string> moddedItems = new();

        FancyShopData shop = FancyRoomBuilder.MakeFancyShop(
            npcName                : "insurance_boi",
            shopItems              : shopItems,
            moddedItems            : moddedItems,
            roomPath               : "CwaffingTheGungy/Resources/Rooms/BasicShopRoom2.newroom",
            allowDupes             : true,
            costModifier           : 1f,
            spawnChance            : 1.0f,
            spawnFloors            : Floors.CASTLEGEON,
            spawnPrerequisite      : CwaffPrerequisites.INSURANCE_PREREQUISITE,
            prequisiteValidator    : CwaffPrerequisite.OnFirstFloor,
            // spawnPrequisiteChecker : null,
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
                },
            customCanBuy           : CanBarterWithItemOnGround,
            removeCurrency         : DestroyBarteredItem,
            customPrice            : GetPriceFromQuality,
            onPurchase             : OnPurchase,
            onSteal                : OnSteal
            );

        _BarterTable = shop.loot;
        shop.owner.gameObject.AddComponent<BarteringPriceFixer>();
    }

    internal static void SetupBarterTable()
    {
        Lazy.DebugLog($"setting up bartering table");
        // System.Diagnostics.Stopwatch tempWatch = System.Diagnostics.Stopwatch.StartNew();
        foreach (PickupObject item in PickupObjectDatabase.Instance.Objects)
        {
            if (!item)
                continue; // skip null items removed from base game
            int grade = item.QualityGrade();
            if (grade < 1 || grade == 5 /* S */)
                continue;  // we don't care about items that aren't A, B, C, or D quality
            if (item.ShouldBeExcludedFromShops)
                continue;  // we don't care about excluded objects
            _BarterTable.AddItemToPool(item.PickupObjectId);
        }
        // tempWatch.Stop(); ETGModConsole.Log($"  finished in "+(tempWatch.ElapsedMilliseconds/1000.0f)+" seconds");
    }

    internal const float _BARTER_RADIUS = 6f;
    internal const float _BARTER_RADIUS_SQR = _BARTER_RADIUS * _BARTER_RADIUS;
    internal static PickupObject ExactlyOneBarterableItemNearby(PlayerController player)
    {
        PickupObject bestCandidate = null;
        foreach (DebrisObject debris in StaticReferenceManager.AllDebris)
        {
            if (!debris.IsPickupObject)
                continue; // not a pickup
            if (debris.GetComponentInChildren<PickupObject>() is not PickupObject pickup)
                continue; // not a pickup
            if (!pickup.CanBeSold)
                continue; // not sellable
            if ((pickup.sprite.WorldCenter - player.sprite.WorldCenter).sqrMagnitude >= _BARTER_RADIUS_SQR)
                continue; // too far
            if (bestCandidate != null)
                return null; // more than one item nearby
            bestCandidate = pickup;
        }
        return bestCandidate;
    }

    internal static bool CanBarterWithItemOnGround(CustomShopController shop, PlayerController player, int price)
    {
        if (ExactlyOneBarterableItemNearby(player) is not PickupObject barterItem)
            return false;
        if (shop.GetTargetedItemByPlayer(player) is not CustomShopItemController shopItem)
            return false;
        return (barterItem.QualityGrade() > shopItem.item.QualityGrade());
    }

    internal static int DestroyBarteredItem(CustomShopController shop, PlayerController player, int price)
    {
        if (ExactlyOneBarterableItemNearby(player) is not PickupObject pickup)
            return 0;
        Lazy.DoSmokeAt(pickup.sprite.WorldCenter);
        UnityEngine.Object.Destroy(pickup.gameObject);
        return 0;
    }

    internal static int GetPriceFromQuality(CustomShopController shop, CustomShopItemController item, PickupObject pickup)
    {
        return 1;
    }

    internal static bool OnPurchase(PlayerController player, PickupObject pickup, int price)
    {
        return false;
    }

    internal static bool OnSteal(PlayerController player, PickupObject pickup, int price)
    {
        return false;
    }
}

public class BarteringPriceFixer : MonoBehaviour
{
    private void Start()
    {
        if (!base.gameObject?.transform?.parent)
            return;
        ETGModConsole.Log($"Fixing barter prices!");
        foreach (Transform child in base.gameObject.transform.parent)
        {
            CustomShopItemController[] shopItems =child?.gameObject?.GetComponentsInChildren<CustomShopItemController>();
            if ((shopItems?.Length ?? 0) == 0)
                continue;
            if (shopItems[0] is not CustomShopItemController shopItem)
                continue;
            if (!shopItem.item)
                continue;
            switch (shopItem.item.quality)
            {
                case ItemQuality.A: shopItem.customPriceSprite = Bart._BarterSpriteS; break;
                case ItemQuality.B: shopItem.customPriceSprite = Bart._BarterSpriteA; break;
                case ItemQuality.C: shopItem.customPriceSprite = Bart._BarterSpriteB; break;
                case ItemQuality.D: shopItem.customPriceSprite = Bart._BarterSpriteC; break;
            }
        }
    }
}
