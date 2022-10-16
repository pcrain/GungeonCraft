using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using ItemAPI;
using SaveAPI;

namespace NevernamedsItems
{
    class TrueBlank : ReusableBlankitem
    {
        //Call this method from the Start() method of your ETGModule extension class
        public static void Init()
        {
            //The name of the item
            string itemName = "True Blank";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it.
            string resourceName = "NevernamedsItems/Resources/trueblank_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a ActiveItem component to the object
            var item = obj.AddComponent<TrueBlank>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Blanktism";
            string longDesc = "Triggers a blank with no cooldown, but costs 0.5 curse to use.\n\n" + "Of all the blanks that have ever shaken the Gungeon's halls, this one is rumoured to be the very first. Torn from the casing of Kaliber and moulded with her blessing.\n\n" + "Gives 2.5 curse to get the ball rolling.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //"kts" here is the item pool. In the console you'd type kts:sweating_bullets
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Set the cooldown type and duration of the cooldown
            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.None, 5);

            //Adds a passive modifier, like curse, coolness, damage, etc. to the item. Works for passives and actives.
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.Curse, 2.5f, StatModifier.ModifyMethod.ADDITIVE);

            //Set some other fields
            item.consumable = false;
            item.quality = ItemQuality.S;
            item.AddToSubShop(ItemBuilder.ShopType.OldRed);
            item.SetupUnlockOnCustomFlag(CustomDungeonFlags.USED_FALSE_BLANK_TEN_TIMES, true);

        }


        //Add the item's functionality down here! I stole most of this from the Stuffed Star active item code!

        public override void DoEffect(PlayerController user)
        {
            //Activates the effect
            float currentCurseNotForOperation = user.stats.GetStatValue(PlayerStats.StatType.Curse);
            //ETGModConsole.Log("Upon activating the item, before doing anything to your curse, the item thinks it is: " + currentCurseNotForOperation);
            if (currentCurseNotForOperation > 0.5f)
            {
                float currentCurse = user.stats.GetBaseStatValue(PlayerStats.StatType.Curse);
                //ETGModConsole.Log("The item has determined that your current curse value has above 0.5");
                user.ForceBlank(25f, 0.5f, false, true, null, true, -1f);
                //ETGModConsole.Log("The blank has been triggered");
                currentCurse -= 0.5f;
                //ETGModConsole.Log("Your current curse SHOULD have 0.5 subtracted from it. It is now: " + currentCurse);
                user.stats.SetBaseStatValue(PlayerStats.StatType.Curse, currentCurse, user);
                //ETGModConsole.Log("This new curse is put back into you: " + currentCurse);
            }
            else
            {
                ETGModConsole.Log("ERROR: \"The True Blank doesn't think you have any curse. You should never see this message unless you've encountered a bug.\" - NN");
            }
        }
        public override bool CanBeUsed(PlayerController user)
        {
            /*float currentCurse = user.stats.GetBaseStatValue(PlayerStats.StatType.Curse);
            if (currentCurse > 0.5f)
            {
                return true;
            }
            else
            {
                return false;
            }
            */
            return user.stats.GetStatValue(PlayerStats.StatType.Curse) > 0.5;
        }
    }
}
