using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using ItemAPI;
using UnityEngine;
using MonoMod.RuntimeDetour;
using System.Reflection;
using SaveAPI;

namespace NevernamedsItems
{
    public class FocalLenses : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Focal Lenses";
            string resourceName = "NevernamedsItems/Resources/focallenses_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<FocalLenses>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Better Beams";
            string longDesc = "Doubles beam damage."+"\n\nA set of lenses capable of increasing the firepower of laserbeams... and the waterpower of the Mega Douser, somehow.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.D;
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.Accuracy, 0.9f, StatModifier.ModifyMethod.MULTIPLICATIVE);
        }
        private void PostProcessBeam(BeamController beam)
        {
            if (beam && beam.projectile)
            {
                beam.projectile.baseData.damage *= 2;
            }
        }

        public override DebrisObject Drop(PlayerController player)
        {
            player.PostProcessBeam -= this.PostProcessBeam;
            DebrisObject debrisObject = base.Drop(player);


            return debrisObject;
        }
        public override void Pickup(PlayerController player)
        {
            player.PostProcessBeam += this.PostProcessBeam;
            base.Pickup(player);
        }
        public override void OnDestroy()
        {
            if (Owner)
            {
                Owner.PostProcessBeam -= this.PostProcessBeam;
            }
            base.OnDestroy();
        }
    }
}
