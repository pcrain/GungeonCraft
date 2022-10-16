using System.Text;
using UnityEngine;
using Alexandria.ItemAPI;
using MonoMod.RuntimeDetour;
using System.Reflection;
using System.Collections.Generic;
using Dungeonator;
using Alexandria.Misc;

namespace NevernamedsItems
{
    public class PaperBadge : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Paper Badge";
            string resourceName = "NevernamedsItems/Resources/paperbadge_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<PaperBadge>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "All or Nothing";
            string longDesc = "Randomly either doubles or negates your bullet damage!"+"\n\nThis paper badge looks far too flimsy to be of any use, but you'd be surprised.";

            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");

            item.CanBeDropped = true;
            item.quality = PickupObject.ItemQuality.C;
        }

        public void PostProcess(Projectile bullet, float chanceScaler)
        {
            if (bullet && bullet.ProjectilePlayerOwner())
            {
            float procChance = 0.5f;
                procChance += bullet.ProjectilePlayerOwner().stats.GetStatValue(PlayerStats.StatType.Coolness) * 0.05f;
            if (Owner.PlayerHasActiveSynergy("Lucky Day")) procChance += 0.3f;
            if (UnityEngine.Random.value <= procChance)
            {
                bullet.baseData.damage *= 2;
                bullet.RuntimeUpdateScale(1.5f);
            }
            else
            {
                bullet.baseData.damage *= 0.01f;
                bullet.RuntimeUpdateScale(0.5f);
            }
            }
        }
        public override void Pickup(PlayerController player)
        {
            player.PostProcessProjectile += this.PostProcess;
            base.Pickup(player);
        }
        public override void DisableEffect(PlayerController player)
        {
            player.PostProcessProjectile -= this.PostProcess;
            base.DisableEffect(player);
        }
    }
}