using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ItemAPI;
using UnityEngine;

namespace NevernamedsItems
{
    class TheShellactery : PlayerItem
    {
        //Call this method from the Start() method of your ETGModule extension class
        public static void Init()
        {
            //The name of the item
            string itemName = "The Shellactery";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it.
            string resourceName = "NevernamedsItems/Resources/theshellactery_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a ActiveItem component to the object
            var item = obj.AddComponent<TheShellactery>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Firearm Immortality";
            string longDesc = "Generates ammunition."+"\n\nThis ancient relic allows you to reach right through the Curtain and pluck ammo directly from the great beyond."+"\n\nTorn from the gut of an ancient Gungeoneer who was ripped back from the jaws of death, despite his best attempts...";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //"kts" here is the item pool. In the console you'd type kts:sweating_bullets
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Set the cooldown type and duration of the cooldown
            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.Damage, 1600);

            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.Curse, 2f, StatModifier.ModifyMethod.ADDITIVE);
            //Adds a passive modifier, like curse, coolness, damage, etc. to the item. Works for passives and actives.

            //Set some other fields
            item.consumable = false;
            item.AddToSubShop(ItemBuilder.ShopType.Cursula);
            item.quality = ItemQuality.S;

            //SYNERGY WITH BLACK HOLE GUN --> "Black Paradox"
            List<string> mandatorySynergyItems = new List<string>() { "nn:the_shellactery", "black_hole_gun" };
            CustomSynergies.Add("Black Paradox", mandatorySynergyItems);
        }

        //Add the item's functionality down here! I stole most of this from the Stuffed Star active item code!

        public override void DoEffect(PlayerController user)
        {
            AkSoundEngine.PostEvent("Play_OBJ_shrine_accept_01", base.gameObject);
            if (user.HasPickupID(169))
            {
                LootEngine.SpawnItem(PickupObjectDatabase.GetById(600).gameObject, LastOwner.specRigidbody.UnitCenter, Vector2.zero, 1f, false, true, false);
                LootEngine.SpawnItem(PickupObjectDatabase.GetById(600).gameObject, LastOwner.specRigidbody.UnitCenter, Vector2.zero, 1f, false, true, false);
            }
            else LootEngine.SpawnItem(PickupObjectDatabase.GetById(78).gameObject, LastOwner.specRigidbody.UnitCenter, Vector2.zero, 1f, false, true, false);
        }
    }
}
