using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using UnityEngine;
using Alexandria.ItemAPI;

namespace NevernamedsItems
{
    public class OsteoporosisBullets : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Osteoporosis Bullets";
            string resourceName = "NevernamedsItems/Resources/osteoporosisbullets_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<OsteoporosisBullets>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Bad To The Bone";
            string longDesc = "These bullets are a skele-TON of trouble for the various BONEheads found throughout the Gungeon, leaving them well and truly BONED";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.Curse, 1f, StatModifier.ModifyMethod.ADDITIVE);
            item.quality = PickupObject.ItemQuality.A;
            item.SetTag("bullet_modifier");
        }
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.PostProcessProjectile += this.PostProcessProjectile;
            player.PostProcessBeam += this.PostProcessBeam;
        }
        private void PostProcessProjectile(Projectile sourceProjectile, float effectChanceScalar)
        {
            ProjectileInstakillBehaviour instakill = sourceProjectile.gameObject.GetOrAddComponent<ProjectileInstakillBehaviour>();
            instakill.tagsToKill.Add("skeleton");
            instakill.bossBonusDMG = 7;
        }
        private void PostProcessBeam(BeamController sourceBeam)
        {
            if (sourceBeam.projectile)
            {
                ProjectileInstakillBehaviour instakill = sourceBeam.projectile.gameObject.GetOrAddComponent<ProjectileInstakillBehaviour>();
                instakill.tagsToKill.Add("skeleton");
                instakill.bossBonusDMG = 1;
            }
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject debrisObject = base.Drop(player);
            player.PostProcessProjectile -= this.PostProcessProjectile;
            player.PostProcessBeam -= this.PostProcessBeam;
            return debrisObject;
        }
        public override void OnDestroy()
        {
            if (Owner)
            {
                Owner.PostProcessProjectile -= this.PostProcessProjectile;
                Owner.PostProcessBeam -= this.PostProcessBeam;
            }
            base.OnDestroy();
        }

        public static List<string> Skellebosses = new List<string>()
        {
            EnemyGuidDatabase.Entries["lich"],
            EnemyGuidDatabase.Entries["megalich"],
            EnemyGuidDatabase.Entries["infinilich"],
            EnemyGuidDatabase.Entries["cannonbalrog"],
        };

    }
}
