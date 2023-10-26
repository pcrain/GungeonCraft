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
    public class BubbleWand : PassiveItem
    {
        public static string ItemName         = "Bubble Wand";
        public static string SpritePath       = "bubble_wand_icon";
        public static string ShortDescription = "TBD";
        public static string LongDescription  = "TBD";

        private static int _BubbleWandId;
        private static Hook _BubbleWandHook;

        public static void Init()
        {
            PickupObject item = Lazy.SetupPassive<BubbleWand>(ItemName, SpritePath, ShortDescription, LongDescription);
            item.quality      = PickupObject.ItemQuality.B;

            _BubbleWandId   = item.PickupObjectId;
            _BubbleWandHook = new Hook(
                typeof(AIActor).GetMethod("Start", BindingFlags.Public | BindingFlags.Instance),
                typeof(BubbleWand).GetMethod("OnEnemyPreStart"));
        }

        public static void OnEnemyPreStart(Action<AIActor> action, AIActor enemy)
        {
            if (GameManager.Instance.AnyPlayerHasPickupID(_BubbleWandId) && Lazy.CoinFlip())
                enemy.ReplaceGun(Items.BubbleBlaster);
            action(enemy);
        }
    }
}
