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
    public class RatPoison : PassiveItem
    {
        public static string ItemName         = "Rat Poison";
        public static string SpritePath       = "rat_poison_icon";
        public static string ShortDescription = "Swiper no Swiping";
        public static string LongDescription  = "Completely prevents the Resourceful Rat from stealing items.\n\nThe Hegemony has invested hundreds of thousands of credits into researching both diplomatic and military means of discouraging the Resourceful Rat's thievery. It turns out that splashing some pickle juice on your items is enough to keep the rodent at bay indefinitely, though the lingering odor is far from pleasant.";

        private static int ratPoisonId;
        private static Hook ratPoisonHook;

        public static void Init()
        {
            PickupObject item                  = Lazy.SetupPassive<RatPoison>(ItemName, SpritePath, ShortDescription, LongDescription);
            item.quality                       = PickupObject.ItemQuality.C;
            item.IgnoredByRat                  = true;
            item.ClearIgnoredByRatFlagOnPickup = false;
            item.AddToSubShop(ItemBuilder.ShopType.Cursula);

            ratPoisonId   = item.PickupObjectId;
            ratPoisonHook = new Hook(
                typeof(PickupObject).GetMethod("ShouldBeTakenByRat", BindingFlags.Instance | BindingFlags.NonPublic),
                typeof(RatPoison).GetMethod("ShouldBeTakenByRat", BindingFlags.Static | BindingFlags.Public)
                );
        }

        public static bool ShouldBeTakenByRat(Func<PickupObject, Vector2, bool> orig, PickupObject pickup, Vector2 point)
        {
            if (GameManager.Instance.AnyPlayerHasPickupID(ratPoisonId))
                return false;
            return orig(pickup, point);
        }
    }
}
