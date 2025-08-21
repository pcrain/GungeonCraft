namespace CwaffingTheGungy;

// Bartering Shop NPC
public class Bart
{
    internal static GenericLootTable _BarterTable = null;

    public static void Init()
    {
        // We need to find all loaded items, so defer initialization for now
        CwaffEvents.OnAllModsLoaded += SetupBarterTable;
        List<int> shopItems      = new();
        List<string> moddedItems = new();

        $"#BARTER_SHOP_SIGN".SetupDBStrings(new(){"HOW TO BARTER:\n\ndrop an item whose quality is\nat least the quality shown on\nthe item you wish to trade for."});

        bool fixedSpawn = CwaffConfig._Gunfig.Value(CwaffConfig._SHOP_KEY) == "Classic";

        FancyShopData shop = FancyShopBuilder.MakeFancyShop(
            npcName                : "bart",
            shopItems              : shopItems,
            moddedItems            : moddedItems,
            roomPath               : $"{C.MOD_INT_NAME}/Resources/Rooms/barter.newroom",
            allowDupes             : false,
            costModifier           : 1f,
            spawnChanceEachRun     : fixedSpawn ? 1.0f : 0.33f,
            spawnPrerequisite      : CwaffPrerequisites.BARTER_SHOP_PREREQUISITE,
            // Guaranteed spawn on 2nd or 3rd floor in classic mode, any floor otherwise
            allowedTilesets        : fixedSpawn ? ((int)( GlobalDungeonData.ValidTilesets.GUNGEON | GlobalDungeonData.ValidTilesets.MINEGEON )) : 127,
            prequisiteValidator    : fixedSpawn ? OnSecondOrThirdFloor : null,
            idleFps                : 6,
            talkFps                : 6,
            flipTowardsPlayer      : false,
            talkPointOffset        : C.PIXEL_SIZE * new Vector2(32, 51),
            npcPosition            : C.PIXEL_SIZE * new Vector2(-15, 76),
            itemPositions          : ShopAPI.defaultItemPositions.ShiftAll(C.PIXEL_SIZE * new Vector2(-25, 0 + 16)),
            exactlyOncePerRun      : true, //NOTE: necessary to make sure the validator doesn't have to do any heavy lifting (possibly makes validator redundant?)
            // voice                  : "sans", // will play audio "Play_CHR_<voice>_voice_01"
            genericDialog          : new(){
                "My trash is your treasure.",
                "Finders keepers.",
                "Not one for small talk.",
                },
            stopperDialog          : new(){
                "My trash is your treasure.",
                "Finders keepers.",
                "Not one for small talk.",
                },
            purchaseDialog         : new(){
                "Been a pleasure.",
                "Have a good one.",
                "Done deal.",
                },
            stolenDialog           : new(){
                "I'll remember that.",
                "Hope you're happy.",
                "That's not yours.",
                },
            noSaleDialog           : new(){
                "Give me something better.",
                "Not interested.",
                "For that?",
                },
            introDialog            : new(){
                "I'll take that off your hands.",
                "Let's make a deal.",
                "What do you have for me?",
                },
            attackedDialog         : new(){
                "Rude.",
                "Please stop.",
                "Enough.",
                },
            customCanBuy           : CanBarterWithItemOnGround,
            removeCurrency         : DestroyBarteredItem,
            customPrice            : GetPriceFromQuality,
            onPurchase             : OnPurchase,
            onSteal                : OnSteal
            );

        _BarterTable = shop.loot;
        shop.SetShotAnimation(paths: ResMap.Get("bart_shot"), fps: 1);
        shop.shop.AddComponent<ForceOutOfStockOnFailedSteal>();
        shop.shop.AddComponent<BarterShopController>();
    }

    public static bool OnSecondOrThirdFloor(SpawnConditions conds)
    {
      string levelBeingLoaded = GameManager.Instance.GetLastLoadedLevelDefinition().dungeonSceneName;
      if (levelBeingLoaded == "tt5")
        return true; // conds.randomNumberForThisRun < 0.5f;
      if (levelBeingLoaded == "tt_mines")
        return true; // conds.randomNumberForThisRun >= 0.5f;
      return false;
    }

    internal static void SetupBarterTable()
    {
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
        foreach (IPlayerInteractable ix in RoomHandler.unassignedInteractableObjects)
        {
            if (ix is ShopItemController)
                continue; // not ours to sell
            if (ix is not PickupObject pickup || !pickup || !pickup.isActiveAndEnabled || !pickup.gameObject || !pickup.gameObject.activeInHierarchy)
                continue; // not a pickup
            if (!pickup.CanBeSold)
                continue; // not sellable
            if (!pickup.sprite || pickup.sprite.WorldCenter.GetAbsoluteRoom() != player.CurrentRoom)
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
        if (player.HasActive<TradingGuide>())
            return (barterItem.QualityGrade() >= shopItem.item.QualityGrade()); // better deals with Trading Guide
        return (barterItem.QualityGrade() > shopItem.item.QualityGrade());
    }

    internal static int DestroyBarteredItem(CustomShopController shop, PlayerController player, int price)
    {
        if (ExactlyOneBarterableItemNearby(player) is not PickupObject pickup)
            return 0;
        RoomHandler.unassignedInteractableObjects.TryRemove(pickup as IPlayerInteractable);
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

public class BarterShopController : MonoBehaviour
{
    private static readonly List<BarterShopController> _ExtantShops = new();

    private CustomShopController _shop = null;
    private void Start()
    {
        this._shop = base.GetComponent<CustomShopController>();
        // make the sign explaining how bartering works indestructible
        RoomHandler room = this._shop.transform.position.GetAbsoluteRoom();
        foreach (var ix in room.interactableObjects)
        {
            if (ix is not Component c)
                continue;
            if (!c.gameObject.name.Contains("Sign"))
                continue;
            MajorBreakable mb = c.gameObject.GetComponent<MajorBreakable>();
            mb.TemporarilyInvulnerable = true;
            break;
        }
        _ExtantShops.Add(this);
        UpdatePrices();
    }

    private void OnDestroy()
    {
        _ExtantShops.TryRemove(this);
    }

    public static void UpdateBarterShopPrices()
    {
        foreach (BarterShopController shop in _ExtantShops)
            shop.UpdatePrices();
    }

    private void UpdatePrices()
    {
        bool isDiscounted = Lazy.AnyoneHasActive<TradingGuide>();
        Lazy.DebugLog($"updating prices for {this._shop.m_shopItems.Count} barter shop items with discount {isDiscounted}");
        foreach (ShopItemController baseController in this._shop.m_itemControllers)
        {
            if (baseController is not CustomShopItemController customItemController || !customItemController.item)
                continue;
            switch (customItemController.item.quality)
            { // all trades are one tier down unless we have the Trading Guide
                case ItemQuality.A: customItemController.customPriceSprite = isDiscounted ? "barter_a_icon" : "barter_s_icon"; break;
                case ItemQuality.B: customItemController.customPriceSprite = isDiscounted ? "barter_b_icon" : "barter_a_icon"; break;
                case ItemQuality.C: customItemController.customPriceSprite = isDiscounted ? "barter_c_icon" : "barter_b_icon"; break;
                case ItemQuality.D: customItemController.customPriceSprite = isDiscounted ? "barter_d_icon" : "barter_c_icon"; break;
            }
        }
    }
}
