using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ItemAPI
{
    class ExampleActive : PlayerItem
    {
        //Call this method from the Start() method of your ETGModule extension class
        public static void Init()
        {
            //The name of the item
            string itemName = "Sweating Bullets";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it.
            string resourceName = "ItemAPI/Resources/sweating_bullets_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a ActiveItem component to the object
            var item = obj.AddComponent<ExampleActive>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Is it Hot in Here?";
            string longDesc = "While active, triples bullet damage, but reduces health to 1 hit. \n\nDon't get nervous!";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //"kts" here is the item pool. In the console you'd type kts:sweating_bullets
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "kts");

            //Set the cooldown type and duration of the cooldown
            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.Damage, 500);

            //Adds a passive modifier, like curse, coolness, damage, etc. to the item. Works for passives and actives.
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.Curse, 1);

            //Set some other fields
            item.consumable = false;
            item.quality = ItemQuality.C;

            //Create a synergy entry with hot lead as the other mandatory item. 
            List<string> mandatorySynergyItems = new List<string>() { "kts:sweating_bullets", "hot_lead" };
            CustomSynergies.Add("Is it hotter in here?", mandatorySynergyItems);
        }

        //Add the item's functionality down here! I stole most of this from the Stuffed Star active item code!
        float duration = 10f;
        protected override void DoEffect(PlayerController user)
        {
            //Play a sound effect
            AkSoundEngine.PostEvent("Play_OBJ_power_up_01", base.gameObject);

            //Activates the effect
            StartEffect(user);

            //start a coroutine which calls the EndEffect method when the item's effect duration runs out
            StartCoroutine(ItemBuilder.HandleDuration(this, duration, user, EndEffect));
        }

        //Doubles the damage, makes the next shot kill the player, and stores the amount we buffed the player for later
        StatModifier damageMod;
        private void StartEffect(PlayerController user)
        {
            user.healthHaver.NextShotKills = true;

            //This is where we actually implement the synergy with hot lead
            //This can be done anywhere in the class. In this case, if the 
            //player has hot lead, the damage boost will be 4x instead of 3x.
            float damageBoost = 3;
            if (user.HasMTGConsoleID("hot_lead")) 
                damageBoost = 4;

            //Adding a temporary stat boost and forcing the player stats to update
            damageMod = this.AddPassiveStatModifier(PlayerStats.StatType.Damage, damageBoost, StatModifier.ModifyMethod.MULTIPLICATIVE);
            user.stats.RecalculateStats(user, force: true, recursive: true);
        }

        //Resets the player back to their original stats
        private void EndEffect(PlayerController user)
        {
            if (damageMod == null) return;
            user.healthHaver.NextShotKills = false;
            //Removing the temporary stat boost and forcing the player stats to update
            this.RemovePassiveStatModifier(damageMod);
            user.stats.RecalculateStats(user, force: true, recursive: true);
        }

        protected override void OnPreDrop(PlayerController user)
        {
            base.OnPreDrop(user);
            //Forcing the effect to end in case the player drops the item while it is active.
            EndEffect(user);
        }

        //Disable or enable the active whenever you need!
        public override bool CanBeUsed(PlayerController user)
        {
            return base.CanBeUsed(user);
        }
    }
}
