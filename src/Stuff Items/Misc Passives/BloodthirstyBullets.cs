using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Alexandria.ItemAPI;
using SaveAPI;
using Alexandria.Misc;

namespace NevernamedsItems
{
    public class BloodthirstyBullets : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Bloodthirsty Bullets";
            string resourceName = "NevernamedsItems/Resources/bloodthirstybullets_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<BloodthirstyBullets>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Born in the Fray";
            string longDesc = "Bullets either deal massive bonus damage to enemies, or enjam them." + "\n\nThese bullets were designed, cast, and shaped in the middle of combat to train them for the battlefield.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.Curse, 1f, StatModifier.ModifyMethod.ADDITIVE);
            item.quality = PickupObject.ItemQuality.B;
            item.SetTag("bullet_modifier");
            item.SetupUnlockOnCustomFlag(CustomDungeonFlags.ALLJAMMED_BEATEN_MINES, true);
            BloodthirstyBulletsID = item.PickupObjectId;
        }
        public static int BloodthirstyBulletsID;
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.PostProcessProjectile += this.PostProcessProjectile;
            player.PostProcessBeam += this.PostProcessBeam;
        }
        private void PostProcessProjectile(Projectile sourceProjectile, float effectChanceScalar)
        {
            try
            {
                if (AllJammedState.AllJammedActive == true)
                {
                    sourceProjectile.baseData.damage *= 1.45f;
                }
                else
                {
                    BloodthirstyBulletsComp comp = sourceProjectile.gameObject.GetOrAddComponent<BloodthirstyBulletsComp>();
                    if (sourceProjectile.ProjectilePlayerOwner() && sourceProjectile.ProjectilePlayerOwner().PlayerHasActiveSynergy("[todo: Add funny synergy name]"))
                    {
                        comp.nonJamDamageMult = 30;
                        comp.jamChance = 0.2f;
                    }
                }
            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message);
            }
        }
        private void PostProcessBeam(BeamController sourceBeam)
        {
            if (sourceBeam && sourceBeam.projectile)
            {
                PostProcessProjectile(sourceBeam.projectile, 1);
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

    }
}
