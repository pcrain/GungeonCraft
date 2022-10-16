using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Alexandria.ItemAPI;

namespace NevernamedsItems
{
    public class BackwardsBullets : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Backwards Bullets";
            string resourceName = "NevernamedsItems/Resources/backwardsbullets_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<BackwardsBullets>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "gnaB gnaB ytoohS";
            string longDesc = "...thgin ymrots dna dloc a no tnemirepxe cifirroh a fo tluser eht era stellub esehT" + "\n\n!sdrawkcab levart meht sekam osla tub, lufrewop erom stellub ruoy sekaM";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.Accuracy, 0.5f, StatModifier.ModifyMethod.MULTIPLICATIVE);
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.Damage, 2f, StatModifier.ModifyMethod.MULTIPLICATIVE);

            item.quality = PickupObject.ItemQuality.C;
            item.SetTag("bullet_modifier");
        }
        public void ModifyVolley(ProjectileVolleyData volleyToModify)
        {
            int count = volleyToModify.projectiles.Count;
            for (int i = 0; i < count; i++)
            {
                ProjectileModule projectileModule = volleyToModify.projectiles[i];
                projectileModule.angleFromAim += 180;
            }
        }
        public override void Pickup(PlayerController player)
        {
            player.stats.AdditionalVolleyModifiers += this.ModifyVolley;
            player.stats.RecalculateStats(player, false, false);
            base.Pickup(player);
        }
        public override DebrisObject Drop(PlayerController player)
        {
            player.stats.AdditionalVolleyModifiers -= this.ModifyVolley;
            player.stats.RecalculateStats(player, false, false);
            return base.Drop(player);
        }
        public override void OnDestroy()
        {
            if (Owner)
            {
                Owner.stats.AdditionalVolleyModifiers -= this.ModifyVolley;
                Owner.stats.RecalculateStats(Owner, false, false);
            }
            base.OnDestroy();
        }
    }
}
