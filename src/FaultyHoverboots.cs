using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using ItemAPI;

namespace NevernamedsItems
{
    public class FaultyHoverboots : PassiveItem
    {
        public static void Init()
        {
            //The name of the item
            string itemName = "Faulty Hoverboots";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it
            string resourceName = "NevernamedsItems/Resources/workinprogress_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a PassiveItem component to the object
            var item = obj.AddComponent<FaultyHoverboots>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Come Fly With Me";
            string longDesc = "Grants flight, but ceases to function upon dodge rolling. Resets every floor." + "\n\nConceptualised by a Turtle, and created by a lunatic with nothing better to do.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //Do this after ItemBuilder.AddSpriteToObject!
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Adds the actual passive effect to the item

            //Set the rarity of the item
            item.quality = PickupObject.ItemQuality.EXCLUDED;
        }
        public bool isFlying = false;
        private void onDodgeRoll(PlayerController player, Vector2 dirVec)
        {
            ETGModConsole.Log("You dodge rolled.");
            Owner.SetIsFlying(false, "faultyhoverboots", true, false);
            ETGModConsole.Log("Flight was removed.");
        }
        private void OnNewFloor()
        {
            ETGModConsole.Log("A new floor was loaded");
            Owner.SetIsFlying(true, "faultyhoverboots", true, false);
            ETGModConsole.Log("Flight was given.");
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject debrisObject = base.Drop(player);
            player.OnRollStarted -= this.onDodgeRoll;
            GameManager.Instance.OnNewLevelFullyLoaded -= this.OnNewFloor;
            ETGModConsole.Log("The item was dropped.");
            Owner.SetIsFlying(false, "faultyhoverboots", true, false);
            ETGModConsole.Log("Flight was removed");
            return debrisObject;
        }

        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.OnRollStarted += this.onDodgeRoll;
            GameManager.Instance.OnNewLevelFullyLoaded += this.OnNewFloor;
            ETGModConsole.Log("The item was picked up.");
            if (!this.m_pickedUpThisRun)
            {
                ETGModConsole.Log("We passed the pickup check");
                player.SetIsFlying(true, "faultyhoverboots", true, false);
                //player.AdditionalCanDodgeRollWhileFlying = true;
                ETGModConsole.Log("Flight was given.");
            }
        }
        public override void OnDestroy()
        {
            GameManager.Instance.OnNewLevelFullyLoaded -= this.OnNewFloor;
            Owner.OnRollStarted -= this.onDodgeRoll;
            ETGModConsole.Log("The item was destroyed.");
            Owner.SetIsFlying(false, "faultyhoverboots", true, false);
            ETGModConsole.Log("Flight was removed");
            base.OnDestroy();
        }
    }
}
