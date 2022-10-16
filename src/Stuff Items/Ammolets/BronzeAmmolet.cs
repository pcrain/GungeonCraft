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
    public class BronzeAmmolet : BlankModificationItem
    {
        public static void Init()
        {
            string itemName = "Bronze Ammolet";
            string resourceName = "NevernamedsItems/Resources/bronzeammolet_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<BronzeAmmolet>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Blanks Diminish";
            string longDesc = "This ammolet appears to have shrunk in the wash, and is eager to take out it's vengeance against any home appliances or Gundead fiends that get in it's way by shrinking them as well!"+"\n\nShrunken enemies can be stomped on to kill them.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");


            item.quality = PickupObject.ItemQuality.C;
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.AdditionalBlanksPerFloor, 1f, StatModifier.ModifyMethod.ADDITIVE);
            item.AddToSubShop(ItemBuilder.ShopType.OldRed);
            BronzeAmmoletID = item.PickupObjectId;

            item.SetupUnlockOnCustomFlag(CustomDungeonFlags.PURCHASED_BRONZEAMMOLET, true);
            item.AddItemToDougMetaShop(41);
        }

        private static int BronzeAmmoletID;

        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
        }

        private static Hook BlankHook = new Hook(
    typeof(SilencerInstance).GetMethod("ProcessBlankModificationItemAdditionalEffects", BindingFlags.Instance | BindingFlags.NonPublic),
    typeof(BronzeAmmolet).GetMethod("BlankModHook", BindingFlags.Instance | BindingFlags.Public),
    typeof(SilencerInstance)
);

        public void BlankModHook(Action<SilencerInstance, BlankModificationItem, Vector2, PlayerController> orig, SilencerInstance silencer, BlankModificationItem bmi, Vector2 centerPoint, PlayerController user)
        {
            orig(silencer, bmi, centerPoint, user);

            if (user.HasPickupID(BronzeAmmoletID))
            {

                RoomHandler currentRoom = user.CurrentRoom;
                List<AIActor> activeEnemies = currentRoom.GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
                if (activeEnemies != null)
                {
                    for (int i = 0; i < activeEnemies.Count; i++)
                    {
                        if (UnityEngine.Random.value <= 0.5f)
                        {
                            activeEnemies[i].ApplyEffect(StatusEffectHelper.GenerateSizeEffect(10, new Vector2(0.4f, 0.4f)));
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