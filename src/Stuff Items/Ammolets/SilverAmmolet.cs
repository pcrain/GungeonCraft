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
    public class SilverAmmolet : BlankModificationItem
    {

        //Call this method from the Start() method of your ETGModule extension
        public static void Init()
        {
            //The name of the item
            string itemName = "Silver Ammolet";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it
            string resourceName = "NevernamedsItems/Resources/silverammolet_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a PassiveItem component to the object
            var item = obj.AddComponent<SilverAmmolet>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Blanks Cleanse";
            string longDesc = "A holy artefact from The Order of The True Gun's archives." + "\n\nMade of 200% Silver, and capable of bestowing a holy cleanse upon the Jammed.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //Do this after ItemBuilder.AddSpriteToObject!
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Set the rarity of the item
            item.quality = PickupObject.ItemQuality.C; //C

            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.AdditionalBlanksPerFloor, 1f, StatModifier.ModifyMethod.ADDITIVE);

            item.AddToSubShop(ItemBuilder.ShopType.OldRed);
            SilverAmmoletID = item.PickupObjectId;

            //ID of the item if you need it to be used in other methods
        }
        private static int SilverAmmoletID;

        private static Hook BlankHook = new Hook(
    typeof(SilencerInstance).GetMethod("ProcessBlankModificationItemAdditionalEffects", BindingFlags.Instance | BindingFlags.NonPublic),
    typeof(SilverAmmolet).GetMethod("BlankModHook", BindingFlags.Instance | BindingFlags.Public),
    typeof(SilencerInstance)
);

        public void BlankModHook(Action<SilencerInstance, BlankModificationItem, Vector2, PlayerController> orig, SilencerInstance silencer, BlankModificationItem bmi, Vector2 centerPoint, PlayerController user)
        {
            orig(silencer, bmi, centerPoint, user);
            try
            {
                if (user.HasPickupID(SilverAmmoletID))
                {
                    RoomHandler currentRoom = user.CurrentRoom;
                    if (currentRoom.HasActiveEnemies(RoomHandler.ActiveEnemyType.All))
                    {

                        foreach (AIActor aiactor in currentRoom.GetActiveEnemies(RoomHandler.ActiveEnemyType.All))
                        {
                            if (aiactor.behaviorSpeculator != null)
                            {
                                if (aiactor.IsBlackPhantom)
                                {
                                    float procChance = 0.5f;
                                    if (user.HasPickupID(538)) procChance = 0.7f;
                                    if (UnityEngine.Random.value <= procChance)
                                    {
                                        aiactor.UnbecomeBlackPhantom();
                                    }
                                }
                            }
                        }
                    }
                }

            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message);
                ETGModConsole.Log(e.StackTrace);
            }
        }
    }
}