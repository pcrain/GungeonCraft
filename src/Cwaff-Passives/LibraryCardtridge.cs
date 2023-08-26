using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using UnityEngine;
using MonoMod.RuntimeDetour;

using Gungeon;
using Dungeonator;
using Alexandria.ItemAPI;
using Alexandria.Misc;

namespace CwaffingTheGungy
{
    public class LibraryCardtridge : PassiveItem
    {
        public static string ItemName         = "Library Cardtridge";
        public static string SpritePath       = "CwaffingTheGungy/Resources/ItemSprites/credit_card_icon";
        public static string ShortDescription = "...";
        public static string LongDescription  = "(...)";

        private static HashSet<int> _BookItemIDs = null;
        private static bool         _DidLateInit = false;

        public static void Init()
        {
            PickupObject item  = Lazy.SetupPassive<LibraryCardtridge>(ItemName, SpritePath, ShortDescription, LongDescription);
            item.quality       = PickupObject.ItemQuality.D;

            // Add guids for book items to list
            _BookItemIDs = new();
                _BookItemIDs.Add(ItemHelper.Get(Items.BookOfChestAnatomy).PickupObjectId);
                _BookItemIDs.Add(ItemHelper.Get(Items.MilitaryTraining).PickupObjectId);
                _BookItemIDs.Add(ItemHelper.Get(Items.Map).PickupObjectId);
                _BookItemIDs.Add(ItemHelper.Get(Items.GungeonBlueprint).PickupObjectId);
                _BookItemIDs.Add(ItemHelper.Get(Items.MagazineRack).PickupObjectId);
                _BookItemIDs.Add(ItemHelper.Get(Items.TableTechBlanks).PickupObjectId);
                _BookItemIDs.Add(ItemHelper.Get(Items.TableTechHeat).PickupObjectId);
                _BookItemIDs.Add(ItemHelper.Get(Items.TableTechMoney).PickupObjectId);
                _BookItemIDs.Add(ItemHelper.Get(Items.TableTechRage).PickupObjectId);
                _BookItemIDs.Add(ItemHelper.Get(Items.TableTechRocket).PickupObjectId);
                _BookItemIDs.Add(ItemHelper.Get(Items.TableTechShotgun).PickupObjectId);
                _BookItemIDs.Add(ItemHelper.Get(Items.TableTechSight).PickupObjectId);
                _BookItemIDs.Add(ItemHelper.Get(Items.TableTechStun).PickupObjectId);
        }

        public override void Pickup(PlayerController player)
        {
            if (!_DidLateInit)
            {
                // Add modded items to the book items list
                _BookItemIDs.Add(IDs.Pickups["zoolanders_diary"]);
                _DidLateInit = true;
            }

            base.Pickup(player);
            MakeBooksFree();
            GameManager.Instance.OnNewLevelFullyLoaded += this.OnNewFloor;
        }

        public override DebrisObject Drop(PlayerController player)
        {
            GameManager.Instance.OnNewLevelFullyLoaded -= this.OnNewFloor;
            NoMoreFreeBooks();
            return base.Drop(player);
        }

        private void OnNewFloor() => MakeBooksFree();

        private void MakeBooksFree()
        {
            foreach (BaseShopController shop in StaticReferenceManager.AllShops)
            {
                foreach(ShopItemController shopItem in shop.m_itemControllers)
                {
                    // Only apply discounts to whitelisted items
                    if (!_BookItemIDs.Contains(shopItem.item.PickupObjectId))
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

}
