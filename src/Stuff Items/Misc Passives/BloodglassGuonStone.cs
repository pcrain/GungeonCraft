using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dungeonator;
using UnityEngine;
using ItemAPI;
using Alexandria.Misc;

namespace NevernamedsItems
{
    class BloodglassGuonStone : PassiveItem
    {
        public static int BloodGlassGuonStoneID;
        public static void Init()
        {
            //The name of the item
            string itemName = "Bloodglass Guon Stone";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it
            string resourceName = "NevernamedsItems/Resources/bloodglassguonstone_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a PassiveItem component to the object
            var item = obj.AddComponent<BloodglassGuonStone>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "We are the crystal haems";
            string longDesc = "An ancient glass blessing, perverted by Blobulonian technology." + "\n\nCrystallises spilt blood into glass guon stones.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //Do this after ItemBuilder.AddSpriteToObject!
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Adds the actual passive effect to the item

            //Set the rarity of the item
            item.quality = PickupObject.ItemQuality.C;

            BloodGlassGuonStoneID = item.PickupObjectId;
            //item.AddAsChamberGunMastery("OnceMoreIntoTheBreach", 4);
            item.RemovePickupFromLootTables();

        }

        private void SpawnGuons(PlayerController player)
        {
            if (UnityEngine.Random.value < 0.4f)
            {
                LootEngine.GivePrefabToPlayer(PickupObjectDatabase.GetById(565).gameObject, player);
                LootEngine.GivePrefabToPlayer(PickupObjectDatabase.GetById(565).gameObject, player);
                LootEngine.GivePrefabToPlayer(PickupObjectDatabase.GetById(565).gameObject, player);
            }
        }
        public override void Pickup(PlayerController player)
        {
            bool hasntAlreadyBeenCollected = !this.m_pickedUpThisRun;
            if (hasntAlreadyBeenCollected)
            {
                LootEngine.GivePrefabToPlayer(PickupObjectDatabase.GetById(565).gameObject, player);
                LootEngine.GivePrefabToPlayer(PickupObjectDatabase.GetById(565).gameObject, player);
                LootEngine.GivePrefabToPlayer(PickupObjectDatabase.GetById(565).gameObject, player);
            }
            player.OnReceivedDamage += this.SpawnGuons;
            base.Pickup(player);
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject debrisObject = base.Drop(player);
            player.OnReceivedDamage -= this.SpawnGuons;
            return debrisObject;
        }
        public override void OnDestroy()
        {
          if (Owner)  Owner.OnReceivedDamage -= this.SpawnGuons;
            base.OnDestroy();
        }
    }
}