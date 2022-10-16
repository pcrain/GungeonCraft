/*using LootTableAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NpcApi
{
    class Examples
    {
        public static void Init()
        {
            
            NpcTools.Init();

            List<string> npcIdleSprites = new List<string> { "BotsMod/NpcApi/ExampleSprites/Npc/example_npc_idle_001.png", "BotsMod/NpcApi/ExampleSprites/Npc/example_npc_idle_002.png", "BotsMod/NpcApi/ExampleSprites/Npc/example_npc_idle_003.png" };

            List<string> npcTalkSprites = new List<string> { "BotsMod/NpcApi/ExampleSprites/Npc/example_npc_talk_001.png", "BotsMod/NpcApi/ExampleSprites/Npc/example_npc_talk_002.png" };

            

            //loot table api required here. can be downloaded at https://modworkshop.net/mod/33125
            var exampleLootTable = LootTableTools.CreateLootTable();

            exampleLootTable.AddItemToPool(532); //gilded bullets
            exampleLootTable.AddItemToPool(214); //coin crown
            exampleLootTable.AddItemToPool(476); //mircotransaction gun
            exampleLootTable.AddItemToPool(605); //loot bag
            exampleLootTable.AddItemToPool(132); //ring of miserly protection
            exampleLootTable.AddItemToPool(641); //gold junk

            ETGMod.Databases.Strings.Core.AddComplex("#EXAMPLE_RUNBASEDMULTILINE_GENERIC", "words");
            ETGMod.Databases.Strings.Core.AddComplex("#EXAMPLE_RUNBASEDMULTILINE_GENERIC", "more words");
            ETGMod.Databases.Strings.Core.AddComplex("#EXAMPLE_RUNBASEDMULTILINE_GENERIC", "even more words");
            ETGMod.Databases.Strings.Core.AddComplex("#EXAMPLE_RUNBASEDMULTILINE_GENERIC", "to many words");

            ETGMod.Databases.Strings.Core.Set("#EXAMPLE_RUNBASEDMULTILINE_STOPPER", "stop talking to me");

            ETGMod.Databases.Strings.Core.Set("#EXAMPLE_SHOP_PURCHASED", "cool");
            ETGMod.Databases.Strings.Core.Set("#EXAMPLE_PURCHASE_FAILED", "Sorry, Link. I can't give credit. Come back when you're a little... mmmmm... richer!");

            ETGMod.Databases.Strings.Core.Set("#EXAMPLE_INTRO", "Lamp oil? rope? bombs? You want it? It's yours my friend, as long as you have enough rupees.");

            ETGMod.Databases.Strings.Core.Set("#EXAMPLE_TAKEPLAYERDAMAGE", "ouch! thats mean! :(");

            BotsMod.BotsModule.shop2 = ItsDaFuckinShopApi.SetUpShop("examplenpc", "example", npcIdleSprites, 6, npcTalkSprites, 8, exampleLootTable, CustomShopItemController.ShopCurrencyType.CUSTOM, "#EXAMPLE_RUNBASEDMULTILINE_GENERIC", "#EXAMPLE_RUNBASEDMULTILINE_STOPPER", "#EXAMPLE_SHOP_PURCHASED", "#EXAMPLE_PURCHASE_FAILED",
            "#EXAMPLE_INTRO", "#EXAMPLE_TAKEPLAYERDAMAGE", 1, ItsDaFuckinShopApi.defaultItemPositions, true, new StatModifier[] { new StatModifier { amount = 1, modifyType = StatModifier.ModifyMethod.ADDITIVE, statToBoost = PlayerStats.StatType.Coolness } },
            Examples.ExampleCustomCanBuy, Examples.ExampleCustomRemoveCurrency, Examples.ExampleCustomPrice, "BotsMod/NpcApi/ExampleSprites/example_currency_icon.png", "examplecurrencytext", true, false, "").GetComponent<CustomShopController>();
        }

        //sets cost based off item quality
        public static int ExampleCustomPrice(CustomShopController shop, PickupObject item)
        {
            if (item.quality == PickupObject.ItemQuality.A || item.quality == PickupObject.ItemQuality.S)
            {
                return 3;
            }
            return 1;
        }
        //gives a damage down equil to 1/4 of the cost of the item
        public static int ExampleCustomRemoveCurrency(CustomShopController shop, PlayerController player, int cost)
        {
            player.ownerlessStatModifiers.Add(new StatModifier { amount = (-0.25f * cost), modifyType = StatModifier.ModifyMethod.ADDITIVE, statToBoost = PlayerStats.StatType.Damage });
            player.stats.RecalculateStats(player, false, false);
            return 1;//whatevers returned here dosent matter its a remnant of an old system 
        }
        //only allows the player to buy the item if they have enough damage
        public static bool ExampleCustomCanBuy(CustomShopController shop, PlayerController player, int cost)
        {
            if (player.stats.GetStatValue(PlayerStats.StatType.Damage) >= (1 + (0.25f * cost)))
            {
                return true;
            }
            return false;
        }
    }

}*/
