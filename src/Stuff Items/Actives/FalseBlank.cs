using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using ItemAPI;
using SaveAPI;

namespace NevernamedsItems
{
    class FalseBlank : ReusableBlankitem
    {
        //Call this method from the Start() method of your ETGModule extension class
        public static void Init()
        {
            //The name of the item
            string itemName = "False Blank";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it.
            string resourceName = "NevernamedsItems/Resources/falseblank_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a ActiveItem component to the object
            var item = obj.AddComponent<FalseBlank>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Blanksphemer";
            string longDesc = "This artefact was created by the devilish Shell'tan to trick unwary Gungeoneers. With every use, more of the bearer's soul is stripped away, and exchanged with pure darkness.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //"kts" here is the item pool. In the console you'd type kts:sweating_bullets
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Set the cooldown type and duration of the cooldown
            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.Damage, 50);

            //Adds a passive modifier, like curse, coolness, damage, etc. to the item. Works for passives and actives.
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.Curse, 1f, StatModifier.ModifyMethod.ADDITIVE);

            //Set some other fields
            item.consumable = false;
            item.quality = ItemQuality.D;           

            item.AddToSubShop(ItemBuilder.ShopType.OldRed);
            item.AddToSubShop(ItemBuilder.ShopType.Cursula);
        }
        public override void Pickup(PlayerController player)
        {
            if (!this.m_pickedUpThisRun)
            {
                timesUsed = 0;
            }
            base.Pickup(player);    
        }
        int timesUsed = 0;
        public override void DoEffect(PlayerController user)
        {
            if (!(user.PlayerHasActiveSynergy("False Pretences") && UnityEngine.Random.value < .50f))
            {

                    giveSomeCurse(user);
                
            }
    
            if (user.PlayerHasActiveSynergy("Transparent Lies"))
            {
                PickupObject byId = PickupObjectDatabase.GetById(565);
                user.AcquirePassiveItemPrefabDirectly(byId as PassiveItem);
            }
            timesUsed += 1;
            user.ForceBlank(25f, 0.5f, false, true, null, true, -1f);
            if (timesUsed >= 10)
            {
                if (!SaveAPIManager.GetFlag(CustomDungeonFlags.USED_FALSE_BLANK_TEN_TIMES))
                {
                    SaveAPIManager.SetFlag(CustomDungeonFlags.USED_FALSE_BLANK_TEN_TIMES, true);
                }
            }
        }

        private void giveSomeCurse(PlayerController user)
        {
            float currentCurse = user.stats.GetBaseStatValue(PlayerStats.StatType.Curse);
            user.stats.SetBaseStatValue(PlayerStats.StatType.Curse, currentCurse + 0.5f, user);
        }
        public override bool CanBeUsed(PlayerController user)
        {
            return base.CanBeUsed(user);
        }
    }
}
