using UnityEngine;
using ItemAPI;
using Dungeonator;
using System.Collections;
using System;
using MonoMod.RuntimeDetour;
using System.Reflection;
using Gungeon;
using System.Collections.Generic;
using SaveAPI;

namespace NevernamedsItems
{
    public class KinAmmolet : BlankModificationItem
    {
        public static void Init()
        {
            string itemName = "Kin Ammolet";
            string resourceName = "NevernamedsItems/Resources/kinammolet_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<KinAmmolet>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Blanks Reinforce";
            string longDesc = "Blanks summon reinforcements to aid you in combat!" + "\n\nThe little pendant is sentient, and very, very confused.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");


            item.quality = PickupObject.ItemQuality.D;
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.AdditionalBlanksPerFloor, 1f, StatModifier.ModifyMethod.ADDITIVE);
            item.AddToSubShop(ItemBuilder.ShopType.OldRed);
            item.AddToSubShop(ItemBuilder.ShopType.Cursula);
            KinAmmoletID = item.PickupObjectId;

            item.SetupUnlockOnCustomFlag(CustomDungeonFlags.PURCHASED_KINAMMOLET, true);
            item.AddItemToDougMetaShop(15);
        }

        private static int KinAmmoletID;

        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
        }

        private static Hook BlankHook = new Hook(
    typeof(SilencerInstance).GetMethod("ProcessBlankModificationItemAdditionalEffects", BindingFlags.Instance | BindingFlags.NonPublic),
    typeof(KinAmmolet).GetMethod("BlankModHook", BindingFlags.Instance | BindingFlags.Public),
    typeof(SilencerInstance)
);

        public void BlankModHook(Action<SilencerInstance, BlankModificationItem, Vector2, PlayerController> orig, SilencerInstance silencer, BlankModificationItem bmi, Vector2 centerPoint, PlayerController user)
        {
            orig(silencer, bmi, centerPoint, user);

            if (user.HasPickupID(KinAmmoletID))
            {
                RoomHandler currentRoom = user.CurrentRoom;
                if (currentRoom.IsSealed)
                {
                    string enemyToSpawnGUID = "01972dee89fc4404a5c408d50007dad5";
                    bool shouldJam = false;
                    if (user.PlayerHasActiveSynergy("Shotgun Club") && UnityEngine.Random.value <= 0.5f) enemyToSpawnGUID = "128db2f0781141bcb505d8f00f9e4d47";
                    if (user.PlayerHasActiveSynergy("Friends On The Other Side")) shouldJam = true;

                    CompanionisedEnemyUtility.SpawnCompanionisedEnemy(user, enemyToSpawnGUID, centerPoint.ToIntVector2(), false, ExtendedColours.brown, 15, 2, shouldJam, true);
                    if ((user.PlayerHasActiveSynergy("Aim Twice, Shoot Once")))
                    {
                        CompanionisedEnemyUtility.SpawnCompanionisedEnemy(user, enemyToSpawnGUID, centerPoint.ToIntVector2(), false, ExtendedColours.brown, 15, 2, shouldJam, true);
                    }
                }
            }
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject debrisObject = base.Drop(player);
            return debrisObject;
        }
    }
}
