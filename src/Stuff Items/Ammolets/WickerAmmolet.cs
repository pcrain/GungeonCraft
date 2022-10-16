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
    public class WickerAmmolet : BlankModificationItem
    {

        //Call this method from the Start() method of your ETGModule extension
        public static void Init()
        {
            //The name of the item
            string itemName = "Wicker Ammolet";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it
            string resourceName = "NevernamedsItems/Resources/wickerammolet_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a PassiveItem component to the object
            var item = obj.AddComponent<WickerAmmolet>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Blanks Terrify";
            string longDesc = "Modifies the elegant sigh of your blanks into a horrifying screech, sure to terrify all who hear it.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //Do this after ItemBuilder.AddSpriteToObject!
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Set the rarity of the item
            item.quality = PickupObject.ItemQuality.B;

            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.AdditionalBlanksPerFloor, 1f, StatModifier.ModifyMethod.ADDITIVE);

            item.AddToSubShop(ItemBuilder.ShopType.OldRed);

            //ID of the item if you need it to be used in other methods
            WickerAmmoletID = item.PickupObjectId;
        }

        private static int WickerAmmoletID;

        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            if (fleeData == null || fleeData.Player != player)
            {
                fleeData = new FleePlayerData();
                fleeData.Player = player;
                fleeData.StartDistance = 100f;
            }
        }

        private static Hook BlankHook = new Hook(
    typeof(SilencerInstance).GetMethod("ProcessBlankModificationItemAdditionalEffects", BindingFlags.Instance | BindingFlags.NonPublic),
    typeof(WickerAmmolet).GetMethod("BlankModHook", BindingFlags.Instance | BindingFlags.Public),
    typeof(SilencerInstance)
);

        public void BlankModHook(Action<SilencerInstance, BlankModificationItem, Vector2, PlayerController> orig, SilencerInstance silencer, BlankModificationItem bmi, Vector2 centerPoint, PlayerController user)
        {
            orig(silencer, bmi, centerPoint, user);

            if (user.HasPickupID(WickerAmmoletID))
            {
                AkSoundEngine.PostEvent("Play_ENM_bombshee_scream_01", user.gameObject);
                RoomHandler currentRoom = user.CurrentRoom;
                if (currentRoom.HasActiveEnemies(RoomHandler.ActiveEnemyType.All))
                {

                    foreach (AIActor aiactor in currentRoom.GetActiveEnemies(RoomHandler.ActiveEnemyType.All))
                    {                    
                        if (aiactor.behaviorSpeculator != null)
                        {

                            aiactor.behaviorSpeculator.FleePlayerData = fleeData;
                            FleePlayerData fleePlayerData = new FleePlayerData();
                            GameManager.Instance.StartCoroutine(WickerAmmolet.RemoveFear(aiactor));

                        }
                    }
                }
            }
        }
        private static IEnumerator RemoveFear(AIActor aiactor)
        {
            yield return new WaitForSeconds(7f);
            aiactor.behaviorSpeculator.FleePlayerData = null;
            yield break;
        }
        private static FleePlayerData fleeData;

        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject debrisObject = base.Drop(player);
            return debrisObject;
        }
    }
}

