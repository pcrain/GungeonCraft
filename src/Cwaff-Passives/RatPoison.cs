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
        public static string SpritePath       = "CwaffingTheGungy/Resources/ItemSprites/rat_poison_icon";
        public static string ShortDescription = "Swiper no Swiping";
        public static string LongDescription  = "(Resourceful rat no longer steals items)";

        private static int ratPoisonId;
        private static Hook ratPoisonHook;

        public static void Init()
        {
            PickupObject item                  = Lazy.SetupPassive<RatPoison>(ItemName, SpritePath, ShortDescription, LongDescription);
            item.quality                       = PickupObject.ItemQuality.C;
            item.IgnoredByRat                  = true;
            item.ClearIgnoredByRatFlagOnPickup = false;

            ratPoisonId   = IDs.Passives["rat_poison"];
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
