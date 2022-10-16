using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NpcApi
{
    //im so fucking sorry to anyone looking at this code
    class Hooks
    {
        public static void Init()
        {
            var ModifiedPriceHook = new Hook(
                   typeof(ShopItemController).GetProperty("ModifiedPrice", BindingFlags.Public | BindingFlags.Instance).GetGetMethod(),
                   typeof(Hooks).GetMethod("ModifiedPriceHook"));
            var LockedHook = new Hook(
                   typeof(ShopItemController).GetProperty("Locked", BindingFlags.Public | BindingFlags.Instance).GetGetMethod(),
                   typeof(Hooks).GetMethod("LockedHook"));

            var interactHook = new Hook(
                    typeof(ShopItemController).GetMethod("Interact", BindingFlags.Instance | BindingFlags.Public),
                    typeof(Hooks).GetMethod("InteractHook", BindingFlags.Static | BindingFlags.Public));

            var OnEnteredRangeHook = new Hook(
                typeof(ShopItemController).GetMethod("OnEnteredRange", BindingFlags.Instance | BindingFlags.Public),
                typeof(Hooks).GetMethod("OnEnteredRangeHook", BindingFlags.Static | BindingFlags.Public));

            var InitializeInternalHook = new Hook(
                    typeof(ShopItemController).GetMethod("InitializeInternal", BindingFlags.Instance | BindingFlags.NonPublic),
                    typeof(Hooks).GetMethod("InitializeInternalHook", BindingFlags.Static | BindingFlags.NonPublic));

            var ForceStealHook = new Hook(
                    typeof(ShopItemController).GetMethod("ForceSteal", BindingFlags.Instance | BindingFlags.Public),
                    typeof(Hooks).GetMethod("ForceStealHook", BindingFlags.Static | BindingFlags.Public));

            var OnExitRangeHook = new Hook(
                    typeof(ShopItemController).GetMethod("OnExitRange", BindingFlags.Instance | BindingFlags.Public),
                    typeof(Hooks).GetMethod("OnExitRangeHook", BindingFlags.Static | BindingFlags.Public));

            var LockItemsHook = new Hook(
                    typeof(BaseShopController).GetMethod("LockItems", BindingFlags.Instance | BindingFlags.NonPublic),
                    typeof(Hooks).GetMethod("LockItemsHook", BindingFlags.Static | BindingFlags.NonPublic));

            /*var LockItemsHook = new Hook(
                typeof(BaseShopController).GetMethod("LockItems", BindingFlags.Instance | BindingFlags.NonPublic),
                typeof(Hooks).GetMethod("LockItemsHook", BindingFlags.Static | BindingFlags.NonPublic));*/
        }

        private static void LockItemsHook(Action<BaseShopController> orig, BaseShopController self)
        {
            if (self is CustomShopController)
            {
                (self as CustomShopController).LockItems();
            }
            else
            {
                orig(self);
            }
        }

        public static bool LockedHook(Func<ShopItemController, bool> orig, ShopItemController self)
        {
            if (self is CustomShopItemController)
            {
                return (self as CustomShopItemController).Locked;
            }
            else
            {
                return orig(self);
            }
        }

        public static int ModifiedPriceHook(Func<ShopItemController, int> orig, ShopItemController self)
        {
            if (self is CustomShopItemController)
            {
                return (self as CustomShopItemController).ModifiedPrice;
            }
            else
            {
                return orig(self);
            }
        }

        public static void OnEnteredRangeHook(Action<ShopItemController, PlayerController> orig, ShopItemController self, PlayerController interactor)
        {
            if (!self)
            {
                return;
            }

            if (self is CustomShopItemController)
            {
                (self as CustomShopItemController).OnEnteredRange(interactor);
            }
            else
            {
                orig(self, interactor);
            }

        }

        public static void InteractHook(Action<ShopItemController, PlayerController> orig, ShopItemController self, PlayerController player)
        {
            if (self is CustomShopItemController)
            {
                (self as CustomShopItemController).Interact(player);
            }
            else
            {
                orig(self, player);
            }

        }

        public static void ForceStealHook(Action<ShopItemController, PlayerController> orig, ShopItemController self, PlayerController player)
        {
            if (self is CustomShopItemController)
            {
                (self as CustomShopItemController).ForceSteal(player);
            }
            else
            {
                orig(self, player);
            }
        }

        public static void OnExitRangeHook(Action<ShopItemController, PlayerController> orig, ShopItemController self, PlayerController player)
        {
            if (self is CustomShopItemController)
            {
                (self as CustomShopItemController).ForceSteal(player);
            }
            else
            {
                orig(self, player);
            }
        }

        private static void InitializeInternalHook(Action<ShopItemController, PickupObject> orig, ShopItemController self, PickupObject i)
        {
            if (!self)
            {
                return;
            }

            if (self is CustomShopItemController)
            {
                (self as CustomShopItemController).InitializeInternal(i);
            }
            else
            {
                orig(self, i);
            }

        }


    }
}