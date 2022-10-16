
using GungeonAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static GungeonAPI.OldShrineFactory;
using Gungeon;
using Alexandria.ItemAPI;
using Alexandria.ChestAPI;
using Dungeonator;
using System.Reflection;
using MonoMod.RuntimeDetour;
using Alexandria.ChestAPI;
using Alexandria.Misc;

namespace NevernamedsItems
{
    public static class KliklokShrine
    {
        public static void Add()
        {
            OldShrineFactory aa = new OldShrineFactory
            {

                name = "KliklokShrine",
                modID = "omitb",
                text = "A shrine to Kliklok, patron god of chests. Giving a blood sacrifice to his effigy may bolster his disciples.",
                spritePath = "NevernamedsItems/Resources/Shrines/kliklok_shrine.png",
                room = RoomFactory.BuildFromResource("NevernamedsItems/Resources/EmbeddedRooms/KliklokRoom.room").room,
                RoomWeight = 1f,
                acceptText = "Pray <Lose HP>",
                declineText = "Leave",
                OnAccept = Accept,
                OnDecline = null,
                CanUse = CanUse,
                offset = new Vector3(-1.5f, -1, 0),
                talkPointOffset = new Vector3(0, 3, 0),
                isToggle = false,
                isBreachShrine = false,


            };
            aa.Build();
            spriteId = SpriteBuilder.AddSpriteToCollection(spriteDefinition, ShrineFactory.ShrineIconCollection);
        }
        public static string spriteDefinition = "NevernamedsItems/Resources/Shrines/kliklok_icon";
        public static bool CanUse(PlayerController player, GameObject shrine)
        {
            if (player.ForceZeroHealthState)
            {
                if (player.healthHaver.Armor > 2)
                {
                    if (GetAllChests().Count > 0) return true;
                    else return false;
                }
                else return false;
            }
            else
            {
                if (player.healthHaver.GetMaxHealth() > 1)
                {
                    if (GetAllChests().Count > 0) return true;
                    else return false;
                }
                else return false;
            }
        }
        public static List<Chest> GetAllChests()
        {
            List<Chest> Validchests = new List<Chest>();
            foreach (Chest chest in StaticReferenceManager.AllChests)
            {
                if (chest && !chest.IsBroken && !chest.IsOpen && !chest.IsGlitched && !chest.IsLockBroken)
                {
                    List<ChestUtility.ChestTier> BannedTiers = new List<ChestUtility.ChestTier>()
                    {
                           ChestUtility.ChestTier.OTHER,
                           ChestUtility.ChestTier.GLITCHED,
                           ChestUtility.ChestTier.RAINBOW,
                           ChestUtility.ChestTier.RAT,
                           ChestUtility.ChestTier.SECRETRAINBOW,
                           ChestUtility.ChestTier.TRUTH,
                    };
                    if (!BannedTiers.Contains (chest.GetChestTier())) Validchests.Add(chest);
                }
            }
            return Validchests;
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
            foreach (Chest chest in GetAllChests())
            {
                ChestUtility.ChestTier targetTier = ChestUtility.ChestTier.OTHER;
                ChestUtility.ChestTier curTier = chest.GetChestTier();
                switch (curTier)
                {
                    case ChestUtility.ChestTier.BROWN:
                        targetTier = ChestUtility.ChestTier.BLUE;
                        break;
                    case ChestUtility.ChestTier.BLUE:
                        targetTier = ChestUtility.ChestTier.GREEN;
                        break;
                    case ChestUtility.ChestTier.GREEN:
                        if (UnityEngine.Random.value <= 0.75f) targetTier = ChestUtility.ChestTier.RED;
                        else targetTier = ChestUtility.ChestTier.SYNERGY;
                        break;
                    case ChestUtility.ChestTier.RED:
                        targetTier = ChestUtility.ChestTier.BLACK;
                        break;
                    case ChestUtility.ChestTier.BLACK:
                        if (UnityEngine.Random.value <= 0.05f) targetTier = ChestUtility.ChestTier.RAINBOW;
                        else chest.ForceUnlock();
                        break;
                    case ChestUtility.ChestTier.SYNERGY:
                        if (UnityEngine.Random.value <= 0.5f) targetTier = ChestUtility.ChestTier.RED;
                        else targetTier = ChestUtility.ChestTier.BLACK;
                        break;
                }

                ThreeStateValue isMimic = ThreeStateValue.UNSPECIFIED;
                if (chest.IsMimic) isMimic = ThreeStateValue.FORCEYES;
                else isMimic = ThreeStateValue.FORCENO;

                if (targetTier != ChestUtility.ChestTier.OTHER)
                {
                    Chest newChest = ChestUtility.SpawnChestEasy(chest.sprite.WorldBottomLeft.ToIntVector2(), targetTier, chest.IsLocked, chest.ChestType, isMimic, ThreeStateValue.FORCENO);
                    if (chest.GetComponent<JammedChestBehav>()) newChest.gameObject.AddComponent<JammedChestBehav>();

                    player.CurrentRoom.DeregisterInteractable(chest);
                    chest.DeregisterChestOnMinimap();
                    UnityEngine.Object.Destroy(chest.gameObject);
                }

                shrine.GetComponent<CustomShrineController>().numUses++;
                GameUIRoot.Instance.notificationController.DoCustomNotification(
                       "Kliklok's Blessing",
                        "Chests Upgraded",
                        ShrineFactory.ShrineIconCollection,
                    spriteId,
                        UINotificationController.NotificationColor.SILVER,
                        true,
                        false
                        );
                AkSoundEngine.PostEvent("Play_OBJ_shrine_accept_01", shrine);
            }
        }
        public static int spriteId;
    }
}

