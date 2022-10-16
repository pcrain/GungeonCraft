using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Alexandria.ItemAPI;

namespace NevernamedsItems
{
    public class CleansingRounds : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Cleansing Rounds";
            string resourceName = "NevernamedsItems/Resources/cleansingrounds_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<CleansingRounds>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Undo What Is Done";
            string longDesc = "These holy shells are capable of saving the Gundead from eternal Jamnation.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            List<string> mandatorySynergyItems = new List<string>() { "nn:cleansing_rounds", "silver_bullets" };
            CustomSynergies.Add("Holy Smackerel", mandatorySynergyItems);
            item.quality = PickupObject.ItemQuality.B;
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
                sourceProjectile.OnHitEnemy += this.OnHitEnemy;
        }
        private void PostProcessBeam(BeamController sourceBeam)
        {
            try
            {
                sourceBeam.projectile.OnHitEnemy += this.OnHitEnemy;
            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message);
            }
        }

        private void OnHitEnemy(Projectile arg1, SpeculativeRigidbody arg2, bool arg3)
        {
            if (arg2 != null && arg2.aiActor != null && Owner != null)
            {
                if (!arg2.aiActor.IsBlackPhantom) return;
                else if (!arg2.healthHaver.IsDead)
                {
                    float procChance = UnityEngine.Random.value;
                    if (Owner.HasPickupID(538))
                    {
                        if (procChance > 0.5)
                        {
                            arg2.aiActor.UnbecomeBlackPhantom();
                        }
                    }
                    else
                    {
                        if (procChance < 0.1)
                        {
                            arg2.aiActor.UnbecomeBlackPhantom();
                        }
                    }
                }
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
