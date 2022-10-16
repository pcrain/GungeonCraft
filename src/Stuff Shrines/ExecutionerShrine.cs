

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
    public static class ExecutionerShrine
    {

        public static void Add()
        {
            OldShrineFactory aa = new OldShrineFactory
            {

                name = "ExecutionerShrine",
                modID = "omitb",
                text = "A shrine to an over-zealous Executioner, fond of gambling and wagers. Those who conjure his spirit are either saved or damned depending on a roll of the dice.",
                spritePath = "NevernamedsItems/Resources/Shrines/executioner_shrine.png",
                room = RoomFactory.BuildFromResource("NevernamedsItems/Resources/EmbeddedRooms/ExecutionerShrine.room").room,
                RoomWeight = 1f,
                acceptText = "Conjure The Spirit of Execution",
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
        public static string spriteDefinition = "NevernamedsItems/Resources/Shrines/executioner_icon";
        public static bool CanUse(PlayerController player, GameObject shrine)
        {
            if (shrine.GetComponent<CustomShrineController>().numUses == 0 || player.HasPickupID(PassiveTestingItem.DebugPassiveID))
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
            shrine.GetComponent<CustomShrineController>().numUses++;
            if (UnityEngine.Random.value <= 0.5f)
            {
                if (player.ForceZeroHealthState)
                {
                    player.healthHaver.Armor += 4;
                }
                else
                {
                    player.healthHaver.ApplyHealing(1000);
                }
                GameUIRoot.Instance.notificationController.DoCustomNotification(
                       "Salvation",
                        "Executioner's Wager",
                        ShrineFactory.ShrineIconCollection,
                    spriteId,
                        UINotificationController.NotificationColor.SILVER,
                        true,
                        false
                        );
                AkSoundEngine.PostEvent("Play_OBJ_shrine_accept_01", shrine);
            }
            else
            {
                if (player.ForceZeroHealthState)
                {
                    player.healthHaver.Armor = 1;
                }
                else
                {
                    player.healthHaver.ForceSetCurrentHealth(0.5f);
                }
                AkSoundEngine.PostEvent("Play_VO_lichA_cackle_01", shrine);
                GameUIRoot.Instance.notificationController.DoCustomNotification(
                       "Damnation",
                        "Executioner's Wager",
                        ShrineFactory.ShrineIconCollection,
                    spriteId,
                        UINotificationController.NotificationColor.SILVER,
                        true,
                        false
                        );

            }
        }
        public static int spriteId;
    }
}
