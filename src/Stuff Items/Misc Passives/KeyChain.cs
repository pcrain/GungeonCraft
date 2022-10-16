using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ItemAPI;
using UnityEngine;
using MonoMod.RuntimeDetour;
using System.Reflection;

namespace NevernamedsItems
{
    public class KeyChain : PassiveItem
    {
        public static void Init()
        {
            //The name of the item
            string itemName = "Keychain";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it
            string resourceName = "NevernamedsItems/Resources/keychain_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a PassiveItem component to the object
            var item = obj.AddComponent<KeyChain>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Chain Reaction";
            string longDesc = "25% chance to double any keys picked up."+ "\n\nNobody quite understands how keys work in the Gungeon, but this thing apparently gives you more of them so who cares.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //Do this after ItemBuilder.AddSpriteToObject!
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Adds the actual passive effect to the item


            //Set the rarity of the item
            item.quality = PickupObject.ItemQuality.B;
            item.AddToSubShop(ItemBuilder.ShopType.Flynt);
        }

        Hook keyPickupHook = new Hook(
                typeof(KeyBulletPickup).GetMethod("Pickup", BindingFlags.Instance | BindingFlags.Public),
                typeof(KeyChain).GetMethod("keyPickupHookMethod")
            );
        public static void keyPickupHookMethod(Action<KeyBulletPickup, PlayerController> orig, KeyBulletPickup self, PlayerController player)
        {
            orig(self, player);
            if (player.HasPickupID(Gungeon.Game.Items["nn:keychain"].PickupObjectId))
            {
                if (!self.IsRatKey && UnityEngine.Random.value < .25f) player.carriedConsumables.KeyBullets += 1;
                else if (self.IsRatKey && UnityEngine.Random.value < .1f) player.carriedConsumables.ResourcefulRatKeys += 1;
            }
        }
    }
}
