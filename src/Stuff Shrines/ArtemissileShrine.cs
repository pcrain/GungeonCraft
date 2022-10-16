
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
using Alexandria.Misc;

namespace NevernamedsItems
{
    public static class ArtemissileShrine
    {

        public static void Add()
        {
            OldShrineFactory aa = new OldShrineFactory
            {

                name = "ArtemissileShrine",
                modID = "omitb",
                text = "A shrine to Artemissile, goddess of the eternal hunt. Grants enchanted arms to her most devout followers.",
                spritePath = "NevernamedsItems/Resources/Shrines/artemissile_shrine.png",
                room = RoomFactory.BuildFromResource("NevernamedsItems/Resources/EmbeddedRooms/ArtemissileShrine.room").room,
                RoomWeight = 1f,
                acceptText = "Prove your devotion to the hunt <Lose HP>",
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
        public static string spriteDefinition = "NevernamedsItems/Resources/Shrines/artemissile_popup";
        public static bool CanUse(PlayerController player, GameObject shrine)
        {
            if ((shrine.GetComponent<CustomShrineController>().numUses == 0) || player.HasPickupID(PassiveTestingItem.DebugPassiveID))
            {
                if (player.characterIdentity == OMITBChars.Shade) return true;
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
            else
            {
                return false;
            }
        }
        public static float GetProperShootingSpeed(Gun gun)
        {
            float start = gun.DefaultModule.cooldownTime;
            if (gun.DefaultModule.shootStyle == ProjectileModule.ShootStyle.Charged)
            {
                if (gun.DefaultModule.chargeProjectiles != null)
                {
                    start += gun.DefaultModule.chargeProjectiles[0].ChargeTime;
                }
            }
            if (gun.DefaultModule.numberOfShotsInClip <= 1)
            {
                start += gun.reloadTime;
            }
            return start;
        }
        public static float GetProperShootDuration(Gun gun)
        {
            float start = 1;
            if (gun.DefaultModule != null && gun.DefaultModule.projectiles != null && gun.DefaultModule.projectiles[0] != null && gun.DefaultModule.projectiles[0].GetComponent<BeamController>() != null)
            {
                if (gun.DefaultModule.projectiles[0].GetComponent<BeamController>().usesChargeDelay == true)
                {
                    start += gun.DefaultModule.projectiles[0].GetComponent<BeamController>().chargeDelay;
                }
            }
            return start;
        }
        public static void Accept(PlayerController player, GameObject shrine)
        {
            if (player.ForceZeroHealthState)
            {
                if (player.characterIdentity != OMITBChars.Shade)
                {
                    player.healthHaver.Armor -= 2;
                }
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
            StatModifier statModifier = new StatModifier
            {
                statToBoost = PlayerStats.StatType.Curse,
                amount = 2.5f,
                modifyType = StatModifier.ModifyMethod.ADDITIVE
            };
            player.ownerlessStatModifiers.Add(statModifier);
            player.stats.RecalculateStats(player);

            Gun gun = LootEngine.GetItemOfTypeAndQuality<Gun>(PickupObjectDatabase.Instance.GetRandomQuality(), GameManager.Instance.RewardManager.GunsLootTable, false);

            Debug.Log($"Tryget gun for Artemissile, ID: {gun.PickupObjectId}");

            GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(ResourceCache.Acquire("Global Prefabs/HoveringGun") as GameObject, player.CenterPosition.ToVector3ZisY(0f), Quaternion.identity);
            gameObject.transform.parent = player.transform;
            HoveringGunController hover = gameObject.GetComponent<HoveringGunController>();
            hover.ConsumesTargetGunAmmo = false;
            hover.ChanceToConsumeTargetGunAmmo = 0f;
            hover.Position = HoveringGunController.HoverPosition.CIRCULATE;
            hover.Aim = HoveringGunController.AimType.PLAYER_AIM;
            hover.Trigger = HoveringGunController.FireType.ON_RELOAD;
            hover.CooldownTime = GetProperShootingSpeed(gun);
            hover.ShootDuration = GetProperShootDuration(gun);
            hover.OnlyOnEmptyReload = false;
            hover.Initialize(gun, player);

            player.stats.RecalculateStats(player, false, false);
            shrine.GetComponent<CustomShrineController>().numUses++;
            shrine.GetComponent<CustomShrineController>().GetRidOfMinimapIcon();

            GameUIRoot.Instance.notificationController.DoCustomNotification(
                   "Enchanted Gun",
                    "Blessing Of The Hunt",
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

