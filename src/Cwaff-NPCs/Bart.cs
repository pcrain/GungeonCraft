namespace CwaffingTheGungy;

// Bartering Shop NPC
public class Bart
{
    internal static GenericLootTable _BarterTable = null;

    internal const string _BarterSpriteS = $"{C.MOD_PREFIX}:S_TIER_ITEM";  // need the strings immediately for preloading in Main()
    internal const string _BarterSpriteA = $"{C.MOD_PREFIX}:A_TIER_ITEM";
    internal const string _BarterSpriteB = $"{C.MOD_PREFIX}:B_TIER_ITEM";
    internal const string _BarterSpriteC = $"{C.MOD_PREFIX}:C_TIER_ITEM";

    public static void Init()
    {
        // We need to find all loaded items, so defer initialization for now
        CwaffEvents.OnAllModsLoaded += SetupBarterTable;
        List<int> shopItems      = new();
        List<string> moddedItems = new();

        $"#BARTER_SHOP_SIGN".SetupDBStrings(new(){"HOW TO BARTER:\n\ndrop an item whose quality is\nat least the quality shown on\nthe item you wish to trade for."});

        FancyShopData shop = FancyRoomBuilder.MakeFancyShop(
            npcName                : "bart",
            shopItems              : shopItems,
            moddedItems            : moddedItems,
            roomPath               : "CwaffingTheGungy/Resources/Rooms/barter.newroom",
            allowDupes             : false,
            costModifier           : 1f,
            spawnChance            : 1.0f,
            spawnPrerequisite      : CwaffPrerequisites.BARTER_SHOP_PREREQUISITE,
            // Guaranteed spawn on 2nd or 3rd floor
            allowedTilesets        : (int)( GlobalDungeonData.ValidTilesets.GUNGEON | GlobalDungeonData.ValidTilesets.MINEGEON ),
            prequisiteValidator    : OnSecondOrThirdFloor,
            talkPointOffset        : C.PIXEL_SIZE * new Vector2(32, 51),
            npcPosition            : C.PIXEL_SIZE * new Vector2(-15, 76),
            itemPositions          : ShopAPI.defaultItemPositions.ShiftAll(C.PIXEL_SIZE * new Vector2(-25, 0 + 16)),
            exactlyOncePerRun      : true,
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
        shop.owner.gameObject.AddComponent<BarteringPriceFixer>();
        shop.owner.gameObject.AddComponent<AddShotAnimation>();  // need to add at runtime since Reset() is called on the DialogueBox FsmStateAction
        shop.shop.AddComponent<ForceOutOfStockOnFailedSteal>();

        // NOTE: can't use ShopAPI version because it relies on GetCallingAssembly() for embedded resources, which doesn't work with nested function calls
        FancyRoomBuilder.AddParentedAnimationToShopFixed(shop.shop, ResMap.Get("bart_shot"), 1, "shot");
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
        foreach (DebrisObject debris in StaticReferenceManager.AllDebris)
        {
            if (!debris.IsPickupObject)
                continue; // not a pickup
            if (debris.GetComponentInChildren<PickupObject>() is not PickupObject pickup)
                continue; // not a pickup
            if (!pickup.CanBeSold)
                continue; // not sellable
            if (pickup.sprite.WorldCenter.GetAbsoluteRoom() != player.CurrentRoom)
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

public class AddShotAnimation : MonoBehaviour
{
    private void Start()
    {
        foreach (FsmStateAction action in base.GetComponent<PlayMakerFSM>().FsmStates[8].Actions)
        {
            if (action is not DialogueBox dialogue)
                continue;
            dialogue.SuppressDefaultAnims = false;
            dialogue.OverrideTalkAnim     = "shot";
        }
    }
}

public class BarteringPriceFixer : MonoBehaviour
{
    private void Start()
    {
        if (base.gameObject?.transform?.parent is not Transform shopTransform)
            return;
        foreach (Transform child in shopTransform)
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

public class ForceOutOfStockOnFailedSteal : MonoBehaviour
{
    private CustomShopController _shop = null;
    private bool _didOutOfStock = false;

    private void Start()
    {
        this._shop = base.GetComponent<CustomShopController>();
    }

    private void Update()
    {
        if (this._didOutOfStock)
            return;
        if (!this._shop.m_wasCaughtStealing)
            return;

        foreach (Transform child in base.transform)
        {
            CustomShopItemController[] shopItems =child?.gameObject?.GetComponentsInChildren<CustomShopItemController>();
            if ((shopItems?.Length ?? 0) == 0)
                continue;
            if (shopItems[0] is not CustomShopItemController shopItem)
                continue;
            if (!shopItem.item)
                continue;
            if (!shopItem.pickedUp)
                shopItem.ForceOutOfStock();
        }

        this._didOutOfStock = true;
    }
}
