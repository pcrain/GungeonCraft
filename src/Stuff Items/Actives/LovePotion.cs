using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using ItemAPI;
using SaveAPI;

namespace NevernamedsItems
{
    class LovePotion : PlayerItem
    {
        //Call this method from the Start() method of your ETGModule extension class
        public static void Init()
        {
            //The name of the item
            string itemName = "Love Potion";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it.
            string resourceName = "NevernamedsItems/Resources/lovepotion_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a ActiveItem component to the object
            var item = obj.AddComponent<LovePotion>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "The Sausage Principle";
            string longDesc = "This potent potion of love was made by the Three Witches as part of a dashing romantic plot that was doomed to fail"+"\n\nIf you like something, never learn how it was made";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //"kts" here is the item pool. In the console you'd type kts:sweating_bullets
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Set the cooldown type and duration of the cooldown
            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.Damage, 150);

            //Set some other fields
            item.consumable = false;
            item.quality = ItemQuality.D;
            item.AddToSubShop(ItemBuilder.ShopType.Goopton);
            item.AddToSubShop(ItemBuilder.ShopType.Cursula);
            item.SetupUnlockOnCustomFlag(CustomDungeonFlags.PURCHASED_LOVEPOTION, true);
            item.AddItemToGooptonMetaShop(10);
        }

        //Add the item's functionality down here! I stole most of this from the Stuffed Star active item code!

        public override void DoEffect(PlayerController user)
        {
            float length = 13;
            float width = 2.5f;
            if (user.PlayerHasActiveSynergy("Ooh Eee Ooh Ah Ah!"))
            {
                length = 20;
                width = 4;
            }
                DeadlyDeadlyGoopManager goopManagerForGoopType = DeadlyDeadlyGoopManager.GetGoopManagerForGoopType(EasyGoopDefinitions.CharmGoopDef);
            Vector2 vector = user.CenterPosition;
            Vector2 normalized = (user.unadjustedAimPoint.XY() - vector).normalized;
            goopManagerForGoopType.TimedAddGoopLine(user.CenterPosition, user.CenterPosition + normalized * length, width, 0.5f);
            if (user.PlayerHasActiveSynergy("Number 9")) goopManagerForGoopType.TimedAddGoopLine(user.CenterPosition, user.CenterPosition + (normalized * -1) * length, width, 0.5f);
            //goopManagerForGoopType.gameObject.AddComponent<PurifiedWaterGoop>();
        }

        public override bool CanBeUsed(PlayerController user)
        {
            return true;
        }
    }
}

