using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.ComponentModel;  //debug
using System.IO;
using System.Runtime.InteropServices; // audio loading
using System.Text.RegularExpressions;

using UnityEngine;
using MonoMod.RuntimeDetour;

using Gungeon;
using Dungeonator;
using Alexandria.ItemAPI;
using NpcApi;

namespace CwaffingTheGungy
{
    /*
        Currently known modded shop IDs:
            psog:tabletechshop        // table and mimic related items
            psog:timedshop            // "rare" items (we add time and space related items)
            psog:gregthly             // not sure we have anything to contribute here
            psog:masteryRewardTrader  // we don't need to muck with this

            omitb:Rusty               // poor quality items (not necessarily D tier)
            omitb:Ironside            // armour and defense themed items
            omitb:Boomhildr           // explosive themed items
    */

    public enum ModdedShopType {
        TimeTrader, //Planetside
        Talbert,    //Planetside
        Rusty,      //OMITB
        Boomhildr,  //OMITB
        Ironside,   //OMITB
    };

    public static class ModdedShopItemAdder // global custom events we can listen for
    {

        internal static Dictionary<string, ModdedShopType> _ModdedShopNameMap = new();
        internal static Dictionary<ModdedShopType,List<int>> _ModdedShopItems = new();

        private static bool _Initialized = false;

        public static void Init()
        {
            // Create a delayed initalizer to add items to modded shops once all mods are actually loaded
            CwaffEvents.OnRunStart += AddItemsToModdedShops;

            // Create lists for items to add to each modded shop's loot table
            foreach (ModdedShopType moddedShopType in (ModdedShopType[]) Enum.GetValues(typeof(ModdedShopType)))
                _ModdedShopItems[moddedShopType] = new List<int>();

            // Populate our map of modded shop ids to a proper ModdedShopType
            _ModdedShopNameMap["psog:timedshop"]     = ModdedShopType.TimeTrader;
            _ModdedShopNameMap["psog:tabletechshop"] = ModdedShopType.Talbert;
            _ModdedShopNameMap["omitb:Rusty"]        = ModdedShopType.Rusty;
            _ModdedShopNameMap["omitb:Boomhildr"]    = ModdedShopType.Boomhildr;
            _ModdedShopNameMap["omitb:Ironside"]     = ModdedShopType.Ironside;
        }

        // Extension method for adding items to modded subshops (delayed until first level is loaded, then used by AddItemsToModdedShops())
        public static void AddToSubShop(this PickupObject po, ModdedShopType type/*, float weight = 1*/)
        {
            _ModdedShopItems[type].Add(po.PickupObjectId);
        }

        // Yoinked from NN / Bunny
        /// <summary>
        /// Adds an item to a loot table via PickupObjectId
        /// </summary>
        /// <param name="lootTable">The loot table you want to add to</param>
        /// <param name="poID">The id of the PickupObject you're adding</param>
        /// <param name="weight">The Weight of the item you're adding (default is 1)</param>
        /// <returns></returns>
        private static void AddItemToPool(this GenericLootTable lootTable, int poID, float weight = 1)
        {
            var po = PickupObjectDatabase.GetById(poID);
            lootTable.defaultItemDrops.Add(new WeightedGameObject()
            {
                pickupId = po.PickupObjectId,
                weight = weight,
                rawGameObject = po.gameObject,
                forceDuplicatesPossible = false,
                additionalPrerequisites = new DungeonPrerequisite[0]
            });
        }

        private static void AddItemsToModdedShops(PlayerController p1, PlayerController p2, GameManager.GameMode gameMode)
        {
            if (_Initialized)
                return;
            _Initialized = true;

            if (C.DEBUG_BUILD)
                ETGModConsole.Log($"scanning custom shops: ");
            var watch = System.Diagnostics.Stopwatch.StartNew();
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                // See if the assembly contains a reference to the shop api
                Type perModShopApi        = assembly.GetType("NpcApi.ItsDaFuckinShopApi");
                Type perModShopController = assembly.GetType("NpcApi.CustomShopController");
                if (perModShopApi == null || perModShopController == null)
                    continue;
                if (C.DEBUG_BUILD)
                    ETGModConsole.Log($"  found assembly: {assembly.GetName().Name}");

                // See if the assembly has actually defined builtShops
                FieldInfo builtShopsInfo = perModShopApi.GetField("builtShops", BindingFlags.Public | BindingFlags.Static);
                if (builtShopsInfo == null || builtShopsInfo.FieldType != typeof(Dictionary<string, GameObject>))
                {
                    ETGModConsole.Log($"    failed to retrieve built shops");
                    continue;
                }

                // See if we can retrieve builtShops and that its non-null
                Dictionary<string, GameObject> builtShops = builtShopsInfo.GetValue(null) as Dictionary<string, GameObject>;
                if (builtShops == null)
                {
                    ETGModConsole.Log($"    failed to convert shop dictionary");
                    continue;
                }

                // Look through all of the mod's shops, see if we know about any of them, and add our items to each of them as needed
                foreach(KeyValuePair<string, GameObject> entry in builtShops)
                {
                    if (entry.Value.GetComponent<BaseShopController>()?.shopItems is not GenericLootTable shopItems)
                        continue;
                    if (C.DEBUG_BUILD)
                        ETGModConsole.Log($"    found shop {entry.Key}");
                    if (!_ModdedShopNameMap.ContainsKey(entry.Key))
                        continue;

                    // if (C.DEBUG_BUILD)
                    // {
                    //     foreach(WeightedGameObject item in shopItems.GetCompiledRawItems())
                    //         ETGModConsole.Log($"      contains {item.pickupId} == {PickupObjectDatabase.GetById(item.pickupId).EncounterNameOrDisplayName} with weight {item.weight}");
                    // }

                    foreach(int itemToAdd in _ModdedShopItems[_ModdedShopNameMap[entry.Key]])
                    {
                        if (C.DEBUG_BUILD)
                            ETGModConsole.Log($"      adding {itemToAdd} == {PickupObjectDatabase.GetById(itemToAdd).EncounterNameOrDisplayName} with weight 1");
                        shopItems.AddItemToPool(itemToAdd);
                    }
                }
            }
            watch.Stop();
            if (C.DEBUG_BUILD)
                ETGModConsole.Log($"  initialized modded shop items in "+(watch.ElapsedMilliseconds/1000.0f)+" seconds");
        }
    }
}
