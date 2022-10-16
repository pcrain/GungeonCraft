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
    public class Ammolock : BlankModificationItem
    {
        public static void Init()
        {
            string itemName = "Ammolock";
            string resourceName = "NevernamedsItems/Resources/ammolock_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<Ammolock>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Blanks Clamp";
            string longDesc = "Blanks lock enemies in place, unable to move!" + "\n\nForged out of impossible Neutronium Alloy, this Ammolet saps Gundead of all their energy.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");


            item.quality = PickupObject.ItemQuality.C;
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.AdditionalBlanksPerFloor, 1f, StatModifier.ModifyMethod.ADDITIVE);
            item.AddToSubShop(ItemBuilder.ShopType.OldRed);
            AmmolockID = item.PickupObjectId;
        }

        private static int AmmolockID;

        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
        }

        private static Hook BlankHook = new Hook(
    typeof(SilencerInstance).GetMethod("ProcessBlankModificationItemAdditionalEffects", BindingFlags.Instance | BindingFlags.NonPublic),
    typeof(Ammolock).GetMethod("BlankModHook", BindingFlags.Instance | BindingFlags.Public),
    typeof(SilencerInstance)
);

        public void BlankModHook(Action<SilencerInstance, BlankModificationItem, Vector2, PlayerController> orig, SilencerInstance silencer, BlankModificationItem bmi, Vector2 centerPoint, PlayerController user)
        {
            orig(silencer, bmi, centerPoint, user);

            if (user.HasPickupID(AmmolockID))
            {
                GameActorSpeedEffect Lockdown = StatusEffectHelper.GenerateLockdown(10);
                RoomHandler currentRoom = user.CurrentRoom;
                List<AIActor> activeEnemies = currentRoom.GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
                if (activeEnemies != null)
                {
                    for (int i = 0; i < activeEnemies.Count; i++)
                    {
                        AIActor aiactor = activeEnemies[i];
                        aiactor.gameActor.ApplyEffect(Lockdown, 1f, null);
                        if (user.PlayerHasActiveSynergy("Under Lock And Key") && aiactor.healthHaver) aiactor.healthHaver.ApplyDamage(7 * user.carriedConsumables.KeyBullets, Vector2.zero, "Under Lock And Key"); 
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