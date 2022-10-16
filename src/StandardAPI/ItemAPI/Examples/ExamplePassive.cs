using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ItemAPI;

namespace ItemAPI
{
    public class ExamplePassive
    {
        //Call this method from the Start() method of your ETGModule extension
        public static void Init()
        {
            //The name of the item
            string itemName = "Boss Bullets"; 

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it
            string resourceName = "ItemAPI/Resources/boss_bullets_icon"; 

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a PassiveItem component to the object
            var item = obj.AddComponent<PassiveItem>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Show 'em Who's Boss";
            string longDesc = "Greatly increases damage dealt to bosses.\n\n" +
                "This item was created by a union of Gungeoneers who became fed up with low wages and poor benefits.\n" +
                "Viva la Revolverlucion!";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //Do this after ItemBuilder.AddSpriteToObject!
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "kts");

            //Adds the actual passive effect to the item
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.DamageToBosses, 2, StatModifier.ModifyMethod.MULTIPLICATIVE);
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.Curse, 1);

            //Set the rarity of the item
            item.quality = PickupObject.ItemQuality.S;
            
            //Add to the cursula shop
            item.AddToSubShop(ItemBuilder.ShopType.Cursula);
        }
    }
}
