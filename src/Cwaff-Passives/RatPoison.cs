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
        public static string ShortDescription = "Ratty no Ratting";
        public static string LongDescription  = "(Resourceful rat no longer steals items)";

        private static int ratPoisonId;
        private static Hook ratPoisonSpawnHook;

        public static void Init()
        {
            PickupObject item  = Lazy.SetupPassive<RatPoison>(ItemName, SpritePath, ShortDescription, LongDescription);
            item.quality       = PickupObject.ItemQuality.C;

            ratPoisonId        = IDs.Passives["rat_poison"];
            ratPoisonSpawnHook = new Hook(
                typeof(LootEngine).GetMethod("PostprocessItemSpawn", BindingFlags.Static | BindingFlags.NonPublic),
                typeof(RatPoison).GetMethod("OnItemSpawn", BindingFlags.Static | BindingFlags.Public)
                );
        }

        public static void OnItemSpawn(Action<DebrisObject> orig, DebrisObject spawnedItem)
        {
            orig(spawnedItem);
            if (!GameManager.Instance.AnyPlayerHasPickupID(ratPoisonId))
                return;
            spawnedItem.GetComponent<PickupObject>().IgnoredByRat = true;
            spawnedItem.GetComponent<PickupObject>().ClearIgnoredByRatFlagOnPickup = false;
        }
    }
}
