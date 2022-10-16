using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ItemAPI;
using MonoMod.RuntimeDetour;
using System.Reflection;
using Dungeonator;

namespace NevernamedsItems
{
    public class PickledPepper : AffectEnemiesInProximityTickItem
    {
        public static void Init()
        {
            string itemName = "Pickled Pepper";
            string resourceName = "NevernamedsItems/Resources/NeoItemSprites/pickledpepper_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<PickledPepper>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Picked In Pecks";
            string longDesc = "Poisons enemies who come too close. \n\nA Gungeon Pepper soaked in pickling brine until it turns a bright green colour. \n\nThe noxious vapour from this delicacy is enough to dissolve your foes from the inside out.";

            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            item.range = 3.5f;

            item.CanBeDropped = true;
            item.quality = PickupObject.ItemQuality.B;
        }
        public override void AffectEnemy(AIActor aiactor)
        {
            aiactor.ApplyEffect(StaticStatusEffects.irradiatedLeadEffect);
            base.AffectEnemy(aiactor);
        }
    }
}
