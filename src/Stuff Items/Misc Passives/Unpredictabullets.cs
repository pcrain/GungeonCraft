using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Alexandria.ItemAPI;
using UnityEngine;
using MonoMod.RuntimeDetour;
using System.Reflection;

namespace NevernamedsItems
{
    public class Unpredictabullets : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Unpredictabullets";
            string resourceName = "NevernamedsItems/Resources/unpredictabullets_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<Unpredictabullets>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Who? What? When? Where?";
            string longDesc = "Unpredictably modifies bullet stats."+"\n\nCreated by firing enough bullets at the wall and seeing what stuck.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.B;
            item.SetTag("bullet_modifier");
        }

        private void PostProcessProjectile(Projectile sourceProjectile, float effectChanceScalar)
        {
            try
            {
                sourceProjectile.baseData.damage *= GetModifierAmount(Owner);
                sourceProjectile.baseData.range *= GetModifierAmount(Owner);
                sourceProjectile.baseData.speed *= GetModifierAmount(Owner);
                sourceProjectile.baseData.force *= GetModifierAmount(Owner);
                sourceProjectile.AdditionalScaleMultiplier *= GetModifierAmount(Owner);
                sourceProjectile.UpdateSpeed();
            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message);
            }
        }      
        private float GetModifierAmount(PlayerController owner)
        {
            int max = 200;
            int min = 10;
            if (owner.PlayerHasActiveSynergy("Cause And Effect")) max = 250;
            float initialPick = UnityEngine.Random.Range(min, max + 1);
            float finalmod = initialPick /= 100f;
            return finalmod;
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject debrisObject = base.Drop(player);
            player.PostProcessProjectile -= this.PostProcessProjectile;
            return debrisObject;
        }
        public override void OnDestroy()
        {
            if (Owner) Owner.PostProcessProjectile -= this.PostProcessProjectile;
            base.OnDestroy();
        }
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.PostProcessProjectile += this.PostProcessProjectile;
        }
    }
}
