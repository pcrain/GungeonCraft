

using GungeonAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static GungeonAPI.OldShrineFactory;
using Gungeon;
using ItemAPI;
using Dungeonator;
using System.Reflection;
using MonoMod.RuntimeDetour;


namespace NevernamedsItems
{
	public static class InvestmentShrine
	{

		public static void Add()
		{
			OldShrineFactory aa = new OldShrineFactory
			{

				name = "InvestmentShrine",
				modID = "omitb",
				text = "A shrine to the darkest of demonic hordes- the board of investors. Giving up your money seems like a great investment opportunity.",
				spritePath = "NevernamedsItems/Resources/Shrines/investment.png",
				room = RoomFactory.BuildFromResource("NevernamedsItems/Resources/EmbeddedRooms/InvestmentShrineRoom.room").room,
				RoomWeight = 1f,
				acceptText = "Invest!",
				declineText = "Leave",
				OnAccept = Accept,
				OnDecline = null,
				CanUse = CanUse,
				offset = new Vector3(-1, -1, 0),
				talkPointOffset = new Vector3(0, 3, 0),
				isToggle = false,
				isBreachShrine = false,
				

			};
			aa.Build();
			spriteId = SpriteBuilder.AddSpriteToCollection(spriteDefinition, ShrineFactory.ShrineIconCollection);
		}
		public static string spriteDefinition = "NevernamedsItems/Resources/Shrines/investment_icon";
		public static bool CanUse(PlayerController player, GameObject shrine)
		{
			bool canuse = (player.carriedConsumables.Currency >= (10 * (shrine.GetComponent<CustomShrineController>().numUses + 1)));
			if (canuse)
			{
				return true;
			}
			else
			{
				return false;
			}
		}
		/*
		 string.Concat(new string[]
				{
					text2,
					" (",
					(this.Costs[0].cost * PlayerStats.GetTotalCurse()).ToString(),
					" ",
					StringTableManager.GetString("#COINS"),
					")"
				});*/

		public static void Accept(PlayerController player, GameObject shrine)
		{
			player.carriedConsumables.Currency -=  (10 * (shrine.GetComponent<CustomShrineController>().numUses + 1));
			shrine.GetComponent<CustomShrineController>().numUses++;
			StatModifier PriceReduction = new StatModifier
			{
				statToBoost = PlayerStats.StatType.GlobalPriceMultiplier,
				amount = .9f,
				modifyType = StatModifier.ModifyMethod.MULTIPLICATIVE
			};
			player.ownerlessStatModifiers.Add(PriceReduction);
			player.stats.RecalculateStats(player);
			GameUIRoot.Instance.notificationController.DoCustomNotification(
				   "Invested!",
					"Shop Prices Reduced",
					ShrineFactory.ShrineIconCollection,
				spriteId,
					UINotificationController.NotificationColor.SILVER,
					true,
					false
					);
			AkSoundEngine.PostEvent("Play_OBJ_shrine_accept_01", shrine);
		}
		public static int spriteId;		
	}
}

