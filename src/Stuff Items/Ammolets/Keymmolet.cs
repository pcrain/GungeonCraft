using UnityEngine;
using ItemAPI;
using Dungeonator;
using System.Collections;
using System;
using MonoMod.RuntimeDetour;
using System.Reflection;
using Gungeon;
using System.Collections.Generic;


namespace NevernamedsItems
{
    public class Keymmolet : BlankModificationItem
    {

        //Call this method from the Start() method of your ETGModule extension
        public static void Init()
        {
            //The name of the item
            string itemName = "Keymmolet";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it
            string resourceName = "NevernamedsItems/Resources/keymmolet_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a PassiveItem component to the object
            var item = obj.AddComponent<Keymmolet>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Blanks are Key";
            string longDesc = "Using a blank unlocks all chests in the room!" + "\n\nA brilliant evolution in ammolet technology, combining the radial power of a blank with the opening power of a key.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //Do this after ItemBuilder.AddSpriteToObject!
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Set the rarity of the item
            item.quality = PickupObject.ItemQuality.A;

            item.AddToSubShop(ItemBuilder.ShopType.OldRed);

            //ID of the item if you need it to be used in other methods
            KeymmoletID = item.PickupObjectId;
        }

        private static int KeymmoletID;

        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
        }

        private static Hook BlankHook = new Hook(
    typeof(SilencerInstance).GetMethod("ProcessBlankModificationItemAdditionalEffects", BindingFlags.Instance | BindingFlags.NonPublic),
    typeof(Keymmolet).GetMethod("BlankModHook", BindingFlags.Instance | BindingFlags.Public),
    typeof(SilencerInstance)
);

        public void BlankModHook(Action<SilencerInstance, BlankModificationItem, Vector2, PlayerController> orig, SilencerInstance silencer, BlankModificationItem bmi, Vector2 centerPoint, PlayerController user)
        {
            orig(silencer, bmi, centerPoint, user);

            if (user.HasPickupID(KeymmoletID))
            {
                RoomHandler currentRoom = user.CurrentRoom;
                Chest[] allChests = FindObjectsOfType<Chest>();
                foreach (Chest chest in allChests)
                {
                    if (chest.transform.position.GetAbsoluteRoom() == currentRoom)
                    {
                        if (chest.ChestIdentifier == Chest.SpecialChestIdentifier.RAT && chest.IsLocked)
                        {
                            TextBubble.DoAmbientTalk(chest.transform, new Vector3(1, 2, 0), "Nice try", 4f);
                        }
                        else
                        {
                            chest.ForceUnlock();
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
