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
        public static string SpritePath       = "CwaffingTheGungy/Resources/ItemSprites/library_cardtridge_icon";
        public static string ShortDescription = "Knowledge is Firepower";
        public static string LongDescription  = "(informational items are free at shops; Bookllets are charmed upon entering a room; books explode on destruction)";

        private static HashSet<int>         _BookItemIDs    = null;
        private static HashSet<string>      _BookEnemyGUIDs = null;
        private static bool                 _DidLateInit    = false;
        private static GameActorCharmEffect _CharmEffect    = null;
        private static ExplosionData        _BookExplosion  = null;

        public static void Init()
        {
            PickupObject item  = Lazy.SetupPassive<LibraryCardtridge>(ItemName, SpritePath, ShortDescription, LongDescription);
            item.quality       = PickupObject.ItemQuality.D;

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

            // Add guids for book enemies to set
            _BookEnemyGUIDs = new();
                _BookEnemyGUIDs.Add(Enemies.Bookllet);
                _BookEnemyGUIDs.Add(Enemies.BlueBookllet);
                _BookEnemyGUIDs.Add(Enemies.GreenBookllet);
                _BookEnemyGUIDs.Add(Enemies.TabletBookllett);

            // Initialize our charm effect
            _CharmEffect = (ItemHelper.Get(Items.YellowChamber) as YellowChamberItem).CharmEffect;

            // Initialize our explosion data
            ExplosionData defaultExplosion = GameManager.Instance.Dungeon.sharedSettingsPrefab.DefaultExplosionData;
            _BookExplosion = new ExplosionData()
            {
                forceUseThisRadius     = true,
                pushRadius             = 3f,
                damageRadius           = 3f,
                damageToPlayer         = 0f,
                doDamage               = true,
                damage                 = 100,
                doDestroyProjectiles   = false,
                doForce                = true,
                debrisForce            = 10f,
                preventPlayerForce     = true,
                explosionDelay         = 0.01f,
                usesComprehensiveDelay = false,
                doScreenShake          = false,
                playDefaultSFX         = true,
                effect                 = defaultExplosion.effect,
                ignoreList             = defaultExplosion.ignoreList,
                ss                     = defaultExplosion.ss,
            };

            // Get our book pile assets
            AssetBundle sharedAssets = ResourceManager.LoadAssetBundle("shared_auto_001");
            DungeonPlaceable pile = sharedAssets.LoadAsset<DungeonPlaceable>("PileOrStackOfBooks");
            ETGModConsole.Log($"books: {pile.name}");
            sharedAssets = null;
        }

        private void MakeBooksFriendlyAndExplodey(AIActor enemy)
        {
            if (!enemy.IsNormalEnemy || !enemy.healthHaver || enemy.healthHaver.IsBoss || enemy.IsHarmlessEnemy)
                return;
            if (!_BookEnemyGUIDs.Contains(enemy.EnemyGuid))
                return;
            enemy.IgnoreForRoomClear = true;
            enemy.ParentRoom.ResetEnemyHPPercentage();
            enemy.ApplyEffect(_CharmEffect);
            enemy.healthHaver.OnDeath += (_) => Exploder.Explode(enemy.sprite.WorldCenter, _BookExplosion, Vector2.zero);
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
                // Add modded items to the book items list
                _BookItemIDs.Add(IDs.Pickups["zoolanders_diary"]);
                _DidLateInit = true;
            }

            base.Pickup(player);
            MakeBooksFree();
            GameManager.Instance.OnNewLevelFullyLoaded += this.OnNewFloor;
            player.OnEnteredCombat += this.OnEnteredCombat;
            ETGMod.AIActor.OnPreStart += this.OnEnemySpawn;
        }

        public override DebrisObject Drop(PlayerController player)
        {
            ETGMod.AIActor.OnPreStart -= this.OnEnemySpawn;
            player.OnEnteredCombat -= this.OnEnteredCombat;
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