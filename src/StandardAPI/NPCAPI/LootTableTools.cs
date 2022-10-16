using System.Collections.Generic;
using UnityEngine;

namespace LootTableAPI
{
    static class LootTableTools
    {

        /// <summary>
        /// Creates a new blank loot table
        /// </summary>
        /// <param name="includedLootTables">i think this litterally dose fuck all</param> 
        /// <param name="prerequisites">the prerequisites of the loot table... whatever the fuck that means</param>
        /// <returns></returns>
        public static GenericLootTable CreateLootTable(List<GenericLootTable> includedLootTables = null, DungeonPrerequisite[] prerequisites = null)
        {
            var lootTable = ScriptableObject.CreateInstance<GenericLootTable>();
            lootTable.defaultItemDrops = new WeightedGameObjectCollection()
            {
                elements = new List<WeightedGameObject>()
            };


            if (prerequisites != null)
            {
                lootTable.tablePrerequisites = prerequisites;
            }
            else
            {
                lootTable.tablePrerequisites = new DungeonPrerequisite[0];
            }

            if (includedLootTables != null)
            {
                lootTable.includedLootTables = includedLootTables;
            }
            else
            {
                lootTable.includedLootTables = new List<GenericLootTable>();
            }


            return lootTable;
        }

        /// <summary>
        /// Adds an item to a loot table via PickupObject
        /// </summary>
        /// <param name="lootTable">The loot table you want to add to</param> 
        /// <param name="po">The PickupObject you're adding</param>
        /// <param name="weight">The Weight of the item you're adding (default is 1)</param>
        /// <returns></returns>
        public static void AddItemToPool(this GenericLootTable lootTable, PickupObject po, float weight = 1)
        {
            lootTable.defaultItemDrops.Add(new WeightedGameObject()
            {
                pickupId = po.PickupObjectId,
                weight = weight,
                rawGameObject = po.gameObject,
                forceDuplicatesPossible = false,
                additionalPrerequisites = new DungeonPrerequisite[0]
            });
        }
        /// <summary>
        /// Adds an item to a loot table via PickupObjectId
        /// </summary>
        /// <param name="lootTable">The loot table you want to add to</param> 
        /// <param name="poID">The id of the PickupObject you're adding</param>
        /// <param name="weight">The Weight of the item you're adding (default is 1)</param>
        /// <returns></returns>
        public static void AddItemToPool(this GenericLootTable lootTable, int poID, float weight = 1)
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


    }
}
