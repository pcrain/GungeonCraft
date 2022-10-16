using NpcApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LootTableAPI;
using UnityEngine;
using GungeonAPI;
using System.Reflection;

namespace NevernamedsItems
{
    public static class Ironside
    {
        public static GenericLootTable IronsideLootTable;
        public static void Init()
        {
            ETGMod.Databases.Strings.Core.AddComplex("#IRONSIDE_GENERIC_TALK", "The Gungeon is a tough place, it pays to have thick skin.");
            ETGMod.Databases.Strings.Core.AddComplex("#IRONSIDE_GENERIC_TALK", "My pepaw always told me to stay prepared. So that's what I do.");
            ETGMod.Databases.Strings.Core.AddComplex("#IRONSIDE_GENERIC_TALK", "The cost of being heavily armoured is never letting anyone in.");
            ETGMod.Databases.Strings.Core.AddComplex("#IRONSIDE_GENERIC_TALK", "Names Ironside. I specialise in armour and protective supplies.");
            ETGMod.Databases.Strings.Core.AddComplex("#IRONSIDE_GENERIC_TALK", "Like the carpet? I got it custom made.");
            ETGMod.Databases.Strings.Core.AddComplex("#IRONSIDE_GENERIC_TALK", "Blanks are all well and good, but you're not always going to have them. Ask yourself, do you want bullets goin' straight to your heart(s)?");
            ETGMod.Databases.Strings.Core.AddComplex("#IRONSIDE_GENERIC_TALK", "I have done and I have dared, for everything to be prepared.");
            ETGMod.Databases.Strings.Core.AddComplex("#IRONSIDE_GENERIC_TALK", "Did you see that tarnished old slug rooting around in my garbage? What a freak.");
            ETGMod.Databases.Strings.Core.AddComplex("#IRONSIDE_GENERIC_TALK", "Armour up Gungeoneer, the road ahead is long and the bullets don't get any softer.");

            ETGMod.Databases.Strings.Core.AddComplex("#IRONSIDE_STOPPER_TALK", "No more chit-chat.");
            ETGMod.Databases.Strings.Core.AddComplex("#IRONSIDE_STOPPER_TALK", "I don't have time to talk longer.");
            ETGMod.Databases.Strings.Core.AddComplex("#IRONSIDE_STOPPER_TALK", "Got things to be, places to do.");

            ETGMod.Databases.Strings.Core.AddComplex("#IRONSIDE_PURCHASE_TALK", "Pleasure doin' business with you.");
            ETGMod.Databases.Strings.Core.AddComplex("#IRONSIDE_PURCHASE_TALK", "Pays to be prepared.");
            ETGMod.Databases.Strings.Core.AddComplex("#IRONSIDE_PURCHASE_TALK", "Armoured and ready!");

            ETGMod.Databases.Strings.Core.AddComplex("#IRONSIDE_NOSALE_TALK", "You'd better get out there and find some more armour.");
            ETGMod.Databases.Strings.Core.AddComplex("#IRONSIDE_NOSALE_TALK", "Sorry pal, no shields no sale.");

            ETGMod.Databases.Strings.Core.AddComplex("#IRONSIDE_INTRO_TALK", "AVAST FIEND- Oh, no. It's just you.");
            ETGMod.Databases.Strings.Core.AddComplex("#IRONSIDE_INTRO_TALK", "Staying safe out there?");
            ETGMod.Databases.Strings.Core.AddComplex("#IRONSIDE_INTRO_TALK", "Welcome back, you're looking beat up.");
            ETGMod.Databases.Strings.Core.AddComplex("#IRONSIDE_INTRO_TALK", "Good grief... I didn't know someone could have that many bruises.");

            ETGMod.Databases.Strings.Core.AddComplex("#IRONSIDE_ATTACKED_TALK", "Hah, no way you're getting through this armour!");
            ETGMod.Databases.Strings.Core.AddComplex("#IRONSIDE_ATTACKED_TALK", "Nice try, but one of us is squishy and it ain't me.");
            ETGMod.Databases.Strings.Core.AddComplex("#IRONSIDE_ATTACKED_TALK", "You're gonna wish you hadn't done that one day.");

            List<int> LootTable = new List<int>()
            {
                160, //Gunknight Stuff
                161,
                162,
                163,
                450, //Armor Synthesizer
                457, //Armor of Thorns
                64, //Potion of Lead Skin
                65, //Knife Shield
                380, //Betrayers Shield
                447, //Shield of the Maiden
                634, //Crisis Stone
                111, //Heavy Bullets
                256, //Heavy Boots
                564, //Full Metal Jacket
                290, //Sunglasses
                312, //Blast Helmet
                314, //Nanomachines
                454, //Hazmat Suit
                598, //Stone Dome
                545, //AC-15
                178, //Crestfaller
                154, //Trashcannon
                FullArmourJacket.FullArmourJacketID,
                KnightlyBullets.KnightlyBulletsID,
                ArmourBandage.ArmourBandageID,
                GoldenArmour.GoldenArmourID,
                ExoskeletalArmour.MeatShieldID,
                ArmouredArmour.ArmouredArmourID,
                KeyBullwark.KeyBulwarkID,
                TinHeart.TinHeartID,
                HeavyChamber.HeavyChamberID,
                TableTechInvulnerability.TableTechInvulnerabilityID,
                LiquidMetalBody.LiquidMetalBodyID,
                HornedHelmet.HornedHelmetID,
                LeadSoul.LeadSoulID,
                GSwitch.GSwitchID,
                MaidenRifle.MaidenRifleID,
                RapidRiposte.RapidRiposteID,
                Converter.ConverterID,
            };

            IronsideLootTable = LootTableTools.CreateLootTable();
            foreach (int i in LootTable)
            {
                IronsideLootTable.AddItemToPool(i);
            }

            GameObject ironsideObj = ItsDaFuckinShopApi.SetUpShop(
                         "Ironside",
                         "omitb",
                         new List<string>()
                         {
                        "NevernamedsItems/Resources/NPCSprites/Ironside/ironside_idle_001",
                        "NevernamedsItems/Resources/NPCSprites/Ironside/ironside_idle_002",
                        "NevernamedsItems/Resources/NPCSprites/Ironside/ironside_idle_003",
                        "NevernamedsItems/Resources/NPCSprites/Ironside/ironside_idle_004",
                         },
                         8,
                         new List<string>()
                         {
                        "NevernamedsItems/Resources/NPCSprites/Ironside/ironside_talk_001",
                        "NevernamedsItems/Resources/NPCSprites/Ironside/ironside_talk_002",
                        "NevernamedsItems/Resources/NPCSprites/Ironside/ironside_talk_003",
                        "NevernamedsItems/Resources/NPCSprites/Ironside/ironside_talk_004",
                        "NevernamedsItems/Resources/NPCSprites/Ironside/ironside_talk_005",
                        "NevernamedsItems/Resources/NPCSprites/Ironside/ironside_talk_006",
                         },
                         12,
                         IronsideLootTable,
                         CustomShopItemController.ShopCurrencyType.CUSTOM,
                         "#IRONSIDE_GENERIC_TALK",
                         "#IRONSIDE_STOPPER_TALK",
                         "#IRONSIDE_PURCHASE_TALK",
                         "#IRONSIDE_NOSALE_TALK",
                         "#IRONSIDE_INTRO_TALK",
                         "#IRONSIDE_ATTACKED_TALK",
                         new Vector3(1, 2.5f, 0),
                         ItsDaFuckinShopApi.defaultItemPositions,
                         1f,
                         false,
                         null,
                         Ironside.IronsideCustomCanBuy,
                         Ironside.IronsideCustomRemoveCurrency,
                         Ironside.IronsideCustomPrice,
                         IronsideBuy,
                         null,
                         "NevernamedsItems/Resources/NPCSprites/Ironside/armourcurrency_icon.png",
                         "Armor",
                         true,
                         true,
                         "NevernamedsItems/Resources/NPCSprites/Ironside/ironside_carpet",
                         true,
                         "NevernamedsItems/Resources/NPCSprites/Ironside/ironside_mapicon",
                         true,
                         0.1f
                         );

            PrototypeDungeonRoom Mod_Shop_Room = RoomFactory.BuildFromResource("NevernamedsItems/Resources/EmbeddedRooms/IronsideRoom.room").room;
            ItsDaFuckinShopApi.RegisterShopRoom(ironsideObj, Mod_Shop_Room, new UnityEngine.Vector2(7f, 9));
        }
        
        public static bool IronsideBuy(PlayerController player, PickupObject item, int idfk)
        {

            return false;
        }
        public static int IronsideCustomPrice(CustomShopController shop, CustomShopItemController itemCont, PickupObject item)
        {
            int price = 1;
            switch (item.quality)
            {
                case PickupObject.ItemQuality.S:
                    price = 4;
                    break;
                case PickupObject.ItemQuality.A:
                    price = 3;
                    break;
                case PickupObject.ItemQuality.B:
                    price = 2;
                    break;
                case PickupObject.ItemQuality.C:
                    price = 2;
                    break;
            }
            if (item is PassiveItem && (item as PassiveItem).ArmorToGainOnInitialPickup > 0) price += (item as PassiveItem).ArmorToGainOnInitialPickup;
            else if (item is Gun && (item as Gun).ArmorToGainOnPickup > 0) price += (item as Gun).ArmorToGainOnPickup;

            List<int> AdditionalArmorIDs = new List<int>() { ArmouredArmour.ArmouredArmourID };

            if (AdditionalArmorIDs.Contains(item.PickupObjectId)) price++;
            price = Mathf.Min(price, 5);
            return price;
        }
        public static int IronsideCustomRemoveCurrency(CustomShopController shop, PlayerController player, int cost)
        {
            if (player.characterIdentity != OMITBChars.Shade) player.healthHaver.Armor -= cost;
            else
            {
                FieldInfo _itemControllers = typeof(CustomShopController).GetField("m_itemControllers", BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (CustomShopItemController item in _itemControllers.GetValue(shop) as List<ShopItemController>)
                {
                    item.ForceOutOfStock();
                }
            }
            return 1;
        }
        public static bool IronsideCustomCanBuy(CustomShopController shop, PlayerController player, int cost)
        {
            if (player.characterIdentity == OMITBChars.Shade) return true;
            if (player.healthHaver.Armor >= cost && player.healthHaver.GetCurrentHealth() > 0)
            {
                return true;
            }
            else if (player.healthHaver.Armor > cost)
            {
                return true;
            }

            return false;
        }
    }
}

