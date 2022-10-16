using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

using System.Reflection;
using UnityEngine;
using ItemAPI;
using Dungeonator;

namespace NevernamedsItems
{
    class GunjurersBelt : TargetedAttackPlayerItem
    {
        //Call this method from the Start() method of your ETGModule extension class
        public static void Init()
        {
            //The name of the item
            string itemName = "Gunjurers Belt";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it.
            string resourceName = "NevernamedsItems/Resources/gunjurersbelt_icon";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a ActiveItem component to the object
            var item = obj.AddComponent<GunjurersBelt>();

            //Adds a tk2dSprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Poof!";
            string longDesc = "Knitted by an Apprentice Gunjurer as part of his ammomantic exams, it allows the bearer to slip beyond the curtain.";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //"kts" here is the item pool. In the console you'd type kts:sweating_bullets
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            //Set the cooldown type and duration of the cooldown
            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.Timed, 5);

            //Adds a passive modifier, like curse, coolness, damage, etc. to the item. Works for passives and actives.

            //Set some other fields
            item.consumable = false;
            item.quality = ItemQuality.D;

            item.doesStrike = false;
            item.doesGoop = false;
            item.DoScreenFlash = false;
            item.reticleQuad = (PickupObjectDatabase.GetById(443) as TargetedAttackPlayerItem).reticleQuad;
        }
        public override void DoActiveEffect(PlayerController user)
        {
            tk2dBaseSprite cursor = OMITBReflectionHelpers.ReflectGetField<tk2dBaseSprite>(typeof(TargetedAttackPlayerItem), "m_extantReticleQuad", this);
            Vector2 overridePos = cursor.WorldCenter;
            TeleportPlayerToCursorPosition.StartTeleport(user, overridePos);            
            base.DoActiveEffect(user);
        }
        public override bool CanBeUsed(PlayerController user)
        {
            return base.CanBeUsed(user);
        }             
    }
}
