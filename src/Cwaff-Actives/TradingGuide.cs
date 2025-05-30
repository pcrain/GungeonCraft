namespace CwaffingTheGungy;

public class TradingGuide : CwaffActive
{
    public static string ItemName         = "Trading Guide";
    public static string ShortDescription = "TBD";
    public static string LongDescription  = "TBD";
    public static string Lore             = "An illustrated compendium listing the values of the various commodities found within the Gungeon, as assessed by a panel of 'independent experts.' Both the independence and expertise of these panel members are up for dispute, but what's certain is the exorbitant prices are only taken at face value by the shopowners referencing them to sell Gungeoneers a Klobbe for 56 casings.";

    private IPlayerInteractable _barterTargetIx = null;
    private int _barterTargetId = -1;
    private PickupObject _barterTarget = null;
    private PickupObject _barterOffer = null;

    public static void Init()
    {
        PlayerItem item = Lazy.SetupActive<TradingGuide>(ItemName, ShortDescription, LongDescription, Lore);
        item.quality    = ItemQuality.D;
        item.consumable = false;
        ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.Timed, 1f);
    }

    public override void Pickup(PlayerController player)
    {
        base.Pickup(player);
        BarterShopController.UpdateBarterShopPrices();
    }

    public override void OnPreDrop(PlayerController player)
    {
        base.OnPreDrop(player);
        BarterShopController.UpdateBarterShopPrices();
    }

    public override void OnDestroy()
    {
        BarterShopController.UpdateBarterShopPrices();
        base.OnDestroy();
    }

    public override bool CanBeUsed(PlayerController user)
    {
        if (user.m_lastInteractionTarget is not IPlayerInteractable ixTarget)
            return false;

        if (ixTarget is ShopItemController shopItem && shopItem.item)
            this._barterTarget = shopItem.item;
        else if (ixTarget is CustomShopItemController customShopItem && customShopItem.item)
            this._barterTarget = customShopItem.item;
        else
            return false;

        if (Bart.ExactlyOneBarterableItemNearby(user) is not PickupObject pickup)
            return false;
        if (pickup.QualityGrade() < this._barterTarget.QualityGrade())
            return false;

        this._barterTargetId = this._barterTarget.PickupObjectId;
        this._barterTargetIx = ixTarget;
        this._barterOffer = pickup;
        return base.CanBeUsed(user);
    }

    public override void DoEffect(PlayerController player)
    {
        if (!this._barterOffer)
            return;

        // need to check CustomShopItemController first since all of them are ShopItemControllers by definition
        if (this._barterTargetIx is CustomShopItemController customShopItem)
        {
            customShopItem.pickedUp = !customShopItem.item.PersistsOnPurchase;
            LootEngine.GivePrefabToPlayer(customShopItem.item.gameObject, player);
            if (customShopItem.OnPurchase != null)
                customShopItem.OnPurchase(player, customShopItem.item, 0); // NOTE: modified price is 0 when bartering, change later if needed
            // if (customShopItem.m_parentShop != null) //NOTE: this is never checked in Alexandria...is it a bug? who knows
            //     customShopItem.m_parentShop.PurchaseItem(customShopItem, true);
            if (customShopItem.m_baseParentShop != null)
                customShopItem.m_baseParentShop.PurchaseItem(customShopItem, true);
            player.HandleItemPurchased(customShopItem);
            if (!customShopItem.item.PersistsOnPurchase)
                GameUIRoot.Instance.DeregisterDefaultLabel(customShopItem.transform);
            AkSoundEngine.PostEvent("Play_OBJ_item_purchase_01", customShopItem.gameObject);
        }
        else if (this._barterTargetIx is ShopItemController shopItem)
        {
            shopItem.pickedUp = !shopItem.item.PersistsOnPurchase;
            LootEngine.GivePrefabToPlayer(shopItem.item.gameObject, player);
            if (shopItem.m_parentShop != null)
                shopItem.m_parentShop.PurchaseItem(shopItem, true);
            if (shopItem.m_baseParentShop != null)
                shopItem.m_baseParentShop.PurchaseItem(shopItem, true);
            player.HandleItemPurchased(shopItem);
            if (!shopItem.item.PersistsOnPurchase)
                GameUIRoot.Instance.DeregisterDefaultLabel(shopItem.transform);
            AkSoundEngine.PostEvent("Play_OBJ_item_purchase_01", shopItem.gameObject);
        }

        RoomHandler.unassignedInteractableObjects.TryRemove(this._barterOffer as IPlayerInteractable);
        Lazy.DoSmokeAt(this._barterOffer.sprite.WorldCenter);
        UnityEngine.Object.Destroy(this._barterOffer.gameObject);
    }
}
