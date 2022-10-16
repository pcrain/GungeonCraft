using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dungeonator;
using UnityEngine;
using ItemAPI;

namespace NevernamedsItems
{
    class ClaySculpture : PassiveItem
    {
        public static void Init()
        {
            //The name of the item
            string itemName = "Clay Sculpture";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it
            string resourceName = "NevernamedsItems/Resources/claysculpture_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a PassiveItem component to the object
            var item = obj.AddComponent<ClaySculpture>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "This Gungeon Has Gundead In It";
            string longDesc = "Upon taking damage, Gundead are consumed by the floor beneath them."+"\n\nThe foundations of the Gungeon are built on a special type of 12-gauge granular clay, which is the ABSOLUTE best for sculptures such as these."+"\nJust be careful... you don't want to catch anything...";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //Do this after ItemBuilder.AddSpriteToObject!
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Adds the actual passive effect to the item

            //Set the rarity of the item
            item.quality = PickupObject.ItemQuality.C;
        }
        private void charmAll(PlayerController user)
        {
            AIActor randomEnemy = user.CurrentRoom.GetRandomActiveEnemy();
            if (randomEnemy != null && !randomEnemy.healthHaver.IsBoss) randomEnemy.ForceFall();
            if (Owner.HasPickupID(159))
            {
                AIActor randomEnemy2 = user.CurrentRoom.GetRandomActiveEnemy();
                if (randomEnemy2 != null && !randomEnemy2.healthHaver.IsBoss) randomEnemy2.ForceFall();
            }
        }
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.OnReceivedDamage += this.charmAll;
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject debrisObject = base.Drop(player);
            player.OnReceivedDamage -= this.charmAll;
            return debrisObject;
        }
        public override void OnDestroy()
        {
            if (Owner) Owner.OnReceivedDamage -= this.charmAll;
            base.OnDestroy();
        }
    }
}