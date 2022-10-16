using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Alexandria.ItemAPI ;
using Alexandria.Misc;

namespace NevernamedsItems
{
    public class MistakeBullets : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Mistake Bullets";
            string resourceName = "NevernamedsItems/Resources/mistakebullets_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<MistakeBullets>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Your Bullets Suck!";
            string longDesc = "Gain a firerate and reload speed bonus, in exchange for negative knockback.\n\n" + "No relation to the actual Mistake though. These bullets were made by a hunchbacked hermit living in space Albania or something.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.KnockbackMultiplier, -3, StatModifier.ModifyMethod.MULTIPLICATIVE);
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.RateOfFire, 1.2f, StatModifier.ModifyMethod.MULTIPLICATIVE);
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.ReloadSpeed, 0.7f, StatModifier.ModifyMethod.MULTIPLICATIVE);
            item.SetTag("bullet_modifier");

            //Set the rarity of the item
            item.quality = PickupObject.ItemQuality.D;
            MistakeBulletsID = item.PickupObjectId;
        }
        public static int MistakeBulletsID;
        private void PostProcess(Projectile bullet, float bleh)
        {
            if (bullet && bullet.ProjectilePlayerOwner() && bullet.ProjectilePlayerOwner().PlayerHasActiveSynergy("For Big Mistakes"))
            {
                bullet.baseData.force *= -1f;
            }
        }
        private void ProcessBeam(BeamController beam)
        {
            if (beam)
            {
                Projectile projectile = beam.projectile;
                if (projectile)
                {
                    this.PostProcess(projectile, 1f);
                }
            }
        }

        public override void Pickup(PlayerController player)
        {
            bool flag = !this.m_pickedUpThisRun;
            if (flag)
            {
                PickupObject byId = PickupObjectDatabase.GetById(565);
                player.AcquirePassiveItemPrefabDirectly(byId as PassiveItem);
                PickupObject byId2 = PickupObjectDatabase.GetById(127);
                player.AcquirePassiveItemPrefabDirectly(byId2 as PassiveItem);
            }
            player.PostProcessProjectile += this.PostProcess;
            player.PostProcessBeam += this.ProcessBeam;
            base.Pickup(player);
        }
        public override DebrisObject Drop(PlayerController player)
        {
            player.PostProcessProjectile -= this.PostProcess;
            player.PostProcessBeam -= this.ProcessBeam;
            return base.Drop(player);
        }
        public override void OnDestroy()
        {
            if (Owner)
            {
                Owner.PostProcessProjectile -= this.PostProcess;
                Owner.PostProcessBeam -= this.ProcessBeam;
            }
            base.OnDestroy();
        }
    }
}
