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
    public class HepatizonAmmolet : BlankModificationItem
    {
        public static void Init()
        {
            string itemName = "Hepatizon Ammolet";
            string resourceName = "NevernamedsItems/Resources/hepatizonammolet_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<HepatizonAmmolet>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Blanks Infect";
            string longDesc = "Blanks blast out microparticles of infected fluid, spreading the plague to enemies." + "\n\nSome say the original plague virus was brought to the Gungeon by the Resourceful Rat.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");


            item.quality = PickupObject.ItemQuality.B;
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.AdditionalBlanksPerFloor, 1f, StatModifier.ModifyMethod.ADDITIVE);
            item.AddToSubShop(ItemBuilder.ShopType.OldRed);
            HepatizonAmmoletID = item.PickupObjectId;

            item.SetupUnlockOnCustomFlag(CustomDungeonFlags.PURCHASED_HEPATIZONAMMOLET, true);
            item.AddItemToDougMetaShop(47);
        }

        private static int HepatizonAmmoletID;

        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
        }

        private static Hook BlankHook = new Hook(
    typeof(SilencerInstance).GetMethod("ProcessBlankModificationItemAdditionalEffects", BindingFlags.Instance | BindingFlags.NonPublic),
    typeof(HepatizonAmmolet).GetMethod("BlankModHook", BindingFlags.Instance | BindingFlags.Public),
    typeof(SilencerInstance)
);

        public void BlankModHook(Action<SilencerInstance, BlankModificationItem, Vector2, PlayerController> orig, SilencerInstance silencer, BlankModificationItem bmi, Vector2 centerPoint, PlayerController user)
        {
            orig(silencer, bmi, centerPoint, user);

            if (user.HasPickupID(HepatizonAmmoletID))
            {
                
                RoomHandler currentRoom = user.CurrentRoom;
                List<AIActor> activeEnemies = currentRoom.GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
                if (activeEnemies != null)
                {
                    for (int i = 0; i < activeEnemies.Count; i++)
                    {
                        if (UnityEngine.Random.value <= 0.33f)
                        {
                            activeEnemies[i].ApplyEffect(StaticStatusEffects.StandardPlagueEffect);
                        }
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