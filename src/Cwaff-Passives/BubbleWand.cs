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
        public static string ShortDescription = "Bring It Around Town";
        public static string LongDescription  = "Upon entering combat, each enemy has a 50% chance of having their held gun replaced with a short-ranged Bubble Blaster.\n\nBubble blowing is a surprisingly popular pastime among the Gundead -- at least for those who have hands -- yet it is rare for Gungeoneers to actually encounter any Gundead enjoying their bubbles. It is believed that they are rather self-conscious about their below-average bubble-blowing abilities, and that showing a shared interest in their passion might be enough to get some of them to open up a bit more.";

        private static int _BubbleWandId;
        private static Hook _BubbleWandHook;

        public static void Init()
        {
            PickupObject item = Lazy.SetupPassive<BubbleWand>(ItemName, SpritePath, ShortDescription, LongDescription);
            item.quality      = PickupObject.ItemQuality.B;
            item.AddToSubShop(ItemBuilder.ShopType.Goopton);

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
