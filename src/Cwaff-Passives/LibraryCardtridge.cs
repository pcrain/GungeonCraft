namespace CwaffingTheGungy;

public class LibraryCardtridge : CwaffPassive
{
    public static string ItemName         = "Library Cardtridge";
    public static string ShortDescription = "Knowledge is Firepower";
    public static string LongDescription  = "Books and paper-based items are free at shops. Bookllets are charmed upon entering a room. Piles of books explode when destroyed.";
    public static string Lore             = "It's pretty safe to assume that most who enter the Gungeon don't come there with the primary goal of reading books, but if you're one of the 4 who do, a library cardtridge is a must-have. Not only does it make reading that much more affordable, but when some unruly Bullet Kin inevitably swing by to destroy your preferred chair and knock over your favorite mug filled with ginger peach green tea, you'll be armed with the knowledge to transform the table you're sitting at into a true bastion of defense.";

    private static HashSet<int>         _BookItemIDs    = null;
    private static HashSet<string>      _BookEnemyGUIDs = null;
    private static bool                 _DidLateInit    = false;
    private static GameActorCharmEffect _CharmEffect    = null;
    private static ExplosionData        _BookExplosion  = null;

    public static void Init()
    {
        PassiveItem item   = Lazy.SetupPassive<LibraryCardtridge>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality       = ItemQuality.D;
        item.AddToSubShop(ItemBuilder.ShopType.Flynt);
        item.AddToShop(ModdedShopType.Talbert);

        // Add guids for book items to set
        _BookItemIDs = new();
            _BookItemIDs.Add((int)Items.BookOfChestAnatomy);
            _BookItemIDs.Add((int)Items.MilitaryTraining);
            _BookItemIDs.Add((int)Items.Map);
            _BookItemIDs.Add((int)Items.GungeonBlueprint);
            _BookItemIDs.Add((int)Items.MagazineRack);
            _BookItemIDs.Add((int)Items.TableTechBlanks);
            _BookItemIDs.Add((int)Items.TableTechHeat);
            _BookItemIDs.Add((int)Items.TableTechMoney);
            _BookItemIDs.Add((int)Items.TableTechRage);
            _BookItemIDs.Add((int)Items.TableTechRocket);
            _BookItemIDs.Add((int)Items.TableTechShotgun);
            _BookItemIDs.Add((int)Items.TableTechSight);
            _BookItemIDs.Add((int)Items.TableTechStun);
            _BookItemIDs.Add((int)Items.Origuni);
            _BookItemIDs.Add((int)Items.Ballot);

        // Add guids for book enemies to set
        _BookEnemyGUIDs = new();
            _BookEnemyGUIDs.Add(Enemies.Bookllet);
            _BookEnemyGUIDs.Add(Enemies.BlueBookllet);
            _BookEnemyGUIDs.Add(Enemies.GreenBookllet);
            _BookEnemyGUIDs.Add(Enemies.TabletBookllett);

        // Initialize our charm effect
        _CharmEffect = (ItemHelper.Get(Items.YellowChamber) as YellowChamberItem).CharmEffect;

        // Initialize our explosion data
        _BookExplosion = Explosions.DefaultLarge.With(damage: 100f, force: 100f, debrisForce: 10f, radius: 3f, preventPlayerForce: true, shake: false);
    }

    private void MakeBooksFriendlyAndExplodey(AIActor enemy)
    {
        if (!enemy.IsHostileAndNotABoss())
            return;
        if (!_BookEnemyGUIDs.Contains(enemy.EnemyGuid))
            return;
        enemy.IgnoreForRoomClear = true;
        enemy.ParentRoom.ResetEnemyHPPercentage();
        enemy.ApplyEffect(_CharmEffect);
        enemy.healthHaver.OnDeath += (_) => Exploder.Explode(enemy.CenterPosition, _BookExplosion, Vector2.zero);
    }

    private void OnEnemySpawn(AIActor enemy)
    {
        MakeBooksFriendlyAndExplodey(enemy);
    }

    private void ExplodingBooks(MinorBreakable mb)
    {
        Exploder.Explode(mb.sprite.WorldCenter, _BookExplosion, Vector2.zero);
    }

    private void OnEnteredCombat()
    {
        // Make piles of books explode on destruction
        RoomHandler curRoom = this.Owner.GetAbsoluteParentRoom();
        foreach (MinorBreakable mb in StaticReferenceManager.AllMinorBreakables)
        {
            if (mb.name == "Pile Of Books" && mb.transform.position.GetAbsoluteRoom() == curRoom)
                mb.OnBreakContext += ExplodingBooks;
        }
    }

    public override void Pickup(PlayerController player)
    {
        if (!_DidLateInit)
        {
            // Add my modded items to the book items list
            _BookItemIDs.Add(IDs.Pickups[AmmoConservationManual.ItemName.InternalName()]);
            _BookItemIDs.Add(IDs.Pickups[MMReloading.ItemName.InternalName()]);
            _BookItemIDs.Add(IDs.Pickups[MMAiming.ItemName.InternalName()]);
            _BookItemIDs.Add(IDs.Pickups[BlankChecks.ItemName.InternalName()]);
            //TODO: Add other modded items to the book items list
            _DidLateInit = true;
        }

        base.Pickup(player);
        MakeBooksFree();
        GameManager.Instance.OnNewLevelFullyLoaded += this.MakeBooksFree;
        player.OnEnteredCombat += this.OnEnteredCombat;
        ETGMod.AIActor.OnPreStart += this.OnEnemySpawn;
    }

    public override void DisableEffect(PlayerController player)
    {
        base.DisableEffect(player);
        ETGMod.AIActor.OnPreStart -= this.OnEnemySpawn;
        GameManager.Instance.OnNewLevelFullyLoaded -= this.MakeBooksFree;
        NoMoreFreeBooks();
        if (!player)
            return;
        player.OnEnteredCombat -= this.OnEnteredCombat;
    }

    private void MakeBooksFree()
    {
        foreach (BaseShopController shop in StaticReferenceManager.AllShops.EmptyIfNull())
        {
            if (!shop || shop.m_itemControllers == null)
                continue;
            foreach(ShopItemController shopItem in shop.m_itemControllers)
            {
                // Only apply discounts to whitelisted items
                if (!shopItem || !shopItem.item || !_BookItemIDs.Contains(shopItem.item.PickupObjectId))
                    continue;

                // Don't apply discount to things with special currency
                if (shopItem.CurrencyType != ShopItemController.ShopCurrencyType.COINS)
                    continue;

                // If we're already discounted, don't do anything
                DiscountBooks discount = shopItem.gameObject.GetOrAddComponent<DiscountBooks>();
                if (discount.isDiscounted)
                    continue;

                // Cache the item's old base custom and override prices
                discount.originalCustomCost = shopItem.item.UsesCustomCost ? shopItem.item.CustomCost : null;
                discount.originalCurrentCost = shopItem.CurrentPrice;
                discount.originalOverrideCost = shopItem.OverridePrice;

                // Set our custom prices
                shopItem.item.UsesCustomCost = true;
                shopItem.item.CustomCost     = 0;
                shopItem.OverridePrice       = 0;
                discount.isDiscounted        = true;
            }
        }
    }

    private void NoMoreFreeBooks()
    {
        foreach (BaseShopController shop in StaticReferenceManager.AllShops)
        {
            foreach(ShopItemController shopItem in shop.m_itemControllers)
            {
                if (shopItem.gameObject.GetComponent<DiscountBooks>() is not DiscountBooks discount)
                    continue;

                if (!discount.isDiscounted)
                    continue;

                if (discount.originalCustomCost.HasValue)
                    shopItem.item.CustomCost = discount.originalCustomCost.Value;
                else
                    shopItem.item.UsesCustomCost = false;
                shopItem.OverridePrice = discount.originalOverrideCost;
                shopItem.CurrentPrice = discount.originalCurrentCost;
                discount.isDiscounted = false;
            }
        }
    }

    private class DiscountBooks : MonoBehaviour
    {
        public bool isDiscounted = false;
        public int? originalCustomCost = null;
        public int? originalOverrideCost = null;
        public int originalCurrentCost = -1;
    }
}
