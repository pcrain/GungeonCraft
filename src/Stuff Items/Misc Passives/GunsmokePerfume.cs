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
    public class GunsmokePerfume : AffectEnemiesInProximityTickItem
    {
        public static void Init()
        {
            string itemName = "Gunsmoke Perfume";
            string resourceName = "NevernamedsItems/Resources/gunsmokeperfume_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<GunsmokePerfume>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Ode To Glock 42";
            string longDesc = "Charms enemies who get too close." + "\n\nThe enticing aroma of a battle hardened gunslinger!";

            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            item.range = 2;
            item.rangeMultSynergy = "Practically Pungent";
            item.synergyRangeMult = 2;

            item.CanBeDropped = true;
            item.quality = PickupObject.ItemQuality.B;
        }
        public override void AffectEnemy(AIActor aiactor)
        {
            aiactor.ApplyEffect(StaticStatusEffects.charmingRoundsEffect);
            if (Owner.PlayerHasActiveSynergy("Regular Hottie")) aiactor.ApplyEffect(StaticStatusEffects.hotLeadEffect);
            base.AffectEnemy(aiactor);
        }
    }
}
