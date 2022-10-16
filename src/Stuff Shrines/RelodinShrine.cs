

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
	public static class RelodinShrine
	{

		public static void Add()
		{
			OldShrineFactory aa = new OldShrineFactory
			{

				name = "RelodinShrine",
				modID = "omitb",
				text = "A shrine to the lord of chambered rounds Relodin. According to legend, he plucked out his eye as a tribute to Kaliber, and in return recieved great wisdom.",
				spritePath = "NevernamedsItems/Resources/Shrines/relodin_shrine.png",
				room = RoomFactory.BuildFromResource("NevernamedsItems/Resources/EmbeddedRooms/RelodinShrineRoom.room").room,
				RoomWeight = 1f,
				acceptText = "Give an eye for knowledge <Accuracy Down>",
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
		public static string spriteDefinition = "NevernamedsItems/Resources/Shrines/relodin_popup";
		public static bool CanUse(PlayerController player, GameObject shrine)
		{
			if (shrine.GetComponent<CustomShrineController>().numUses == 0)
            {
				return true;
            }
            else
            {
				return false;
            }
		}

		public static void Accept(PlayerController player, GameObject shrine)
		{
			StatModifier SpreadUp = new StatModifier
			{
				statToBoost = PlayerStats.StatType.Accuracy,
				amount = 2f,
				modifyType = StatModifier.ModifyMethod.MULTIPLICATIVE
			};
			StatModifier DamageUp = new StatModifier
			{
				statToBoost = PlayerStats.StatType.Damage,
				amount = 1.3f,
				modifyType = StatModifier.ModifyMethod.MULTIPLICATIVE
			};

			player.ownerlessStatModifiers.Add(SpreadUp);
			player.ownerlessStatModifiers.Add(DamageUp);

			player.stats.RecalculateStats(player);
			shrine.GetComponent<CustomShrineController>().numUses++;
			GameUIRoot.Instance.notificationController.DoCustomNotification(
				   "Blinded",
					"Wisdom of the Gun",
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

