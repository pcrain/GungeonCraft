
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
    public static class DagunShrine
    {

        public static void Add()
        {
            OldShrineFactory aa = new OldShrineFactory
            {

                name = "DagunShrine",
                modID = "omitb",
                text = "A shrine to Dagun, god of plenty. His amphora of shells running never dry.",
                spritePath = "NevernamedsItems/Resources/Shrines/dagun_shrine.png",
                room = RoomFactory.BuildFromResource("NevernamedsItems/Resources/EmbeddedRooms/DagunShrine.room").room,
                RoomWeight = 1f,
                acceptText = "Pray <Lose HP>",
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
        public static string spriteDefinition = "NevernamedsItems/Resources/Shrines/dagun_popup";
        public static bool CanUse(PlayerController player, GameObject shrine)
        {
            if (player.ForceZeroHealthState)
            {
                if (player.healthHaver.Armor > 2)
                {
                    return true;
                }
                else return false;
            }
            else
            {
                if (player.healthHaver.GetMaxHealth() > 1)
                {
                    return true;
                }
                else return false;
            }
        }

        public static void Accept(PlayerController player, GameObject shrine)
        {
            if (player.ForceZeroHealthState)
            {
                player.healthHaver.Armor -= 2;
            }
            else
            {
                StatModifier HP = new StatModifier
                {
                    statToBoost = PlayerStats.StatType.Health,
                    amount = -1f,
                    modifyType = StatModifier.ModifyMethod.ADDITIVE
                };
                player.ownerlessStatModifiers.Add(HP);
                player.stats.RecalculateStats(player);
            }

            int amt = UnityEngine.Random.Range(5, 11);
            for (int i = 0; i < amt; i++)
            {
                IntVector2 pos = player.CurrentRoom.GetRandomVisibleClearSpot(2, 2);
                LootEngine.SpawnItem(PickupObjectDatabase.GetById(BraveUtility.RandomElement(BabyGoodChanceKin.lootIDlist)).gameObject, pos.ToVector2(), Vector2.zero, 0);
            }
            shrine.GetComponent<CustomShrineController>().numUses++;
            GameUIRoot.Instance.notificationController.DoCustomNotification(
                   "Bounty",
                    "Dungeon of Plenty",
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

