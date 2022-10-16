using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using ItemAPI;

namespace NevernamedsItems
{
    class BlankKey : PassiveItem
    {
        public static void Init()
        {
            //The name of the item
            string itemName = "Blank Key";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it
            string resourceName = "NevernamedsItems/Resources/blankkey_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a PassiveItem component to the object
            var item = obj.AddComponent<BlankKey>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Implosive Openings";
            string longDesc = "Spending a key triggers a blank effect." + "\n\nFlynt and Old Red don't often see eye to eye, but there are some... exceptions.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //Do this after ItemBuilder.AddSpriteToObject!
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Adds the actual passive effect to the item            
            //Set the rarity of the item
            item.quality = PickupObject.ItemQuality.C;
        }
        private float currentKeys, lastKeys;
        public override void Update()
        {
            if (Owner)
            {
                CalculateKeys(Owner);
            }

            else { return; }
        }

        private void CalculateKeys(PlayerController player)
        {
            currentKeys = player.carriedConsumables.KeyBullets;
            if (currentKeys < lastKeys)
            {
                if (Owner.HasPickupID(Gungeon.Game.Items["nn:spare_blank"].PickupObjectId))
                {
                    LootEngine.GivePrefabToPlayer(PickupObjectDatabase.GetById(224).gameObject, player);
                }
                else
                {
                    player.ForceBlank(25f, 0.5f, false, true, null, true, -1f);
                }
            }
            lastKeys = currentKeys;

        }
        public override void Pickup(PlayerController player)
        {
            //player.SetIsFlying(true, "shade", true, false);
            bool hasntAlreadyBeenCollected = !this.m_pickedUpThisRun;
            if (hasntAlreadyBeenCollected)
            {
                LootEngine.GivePrefabToPlayer(PickupObjectDatabase.GetById(67).gameObject, player);
                LootEngine.GivePrefabToPlayer(PickupObjectDatabase.GetById(224).gameObject, player);
            }
            base.Pickup(player);
        }
        public override DebrisObject Drop(PlayerController player)
        {
            //player.SetIsFlying(false, "shade", true, false);
            DebrisObject debrisObject = base.Drop(player);
            return debrisObject;
        }
        public override void OnDestroy()
        {
            //Owner.SetIsFlying(false, "shade", true, false);
            base.OnDestroy();
        }
    }
}