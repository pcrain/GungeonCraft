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
    public class WidowsRing : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Widow's Ring";
            string resourceName = "NevernamedsItems/Resources/widowsring_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<WidowsRing>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Deadly Charms";
            string longDesc = "Charmed enemies can be instantly slain." + "\n\nThe previous owner of this ring was banished to the Gungeon for murdering five of his ex-husbands."+"\nThere's even a small compartment inside containing poison.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.C;
        }
        private void OnHitEnemy(Projectile self, SpeculativeRigidbody enemy, bool fatal)
        {
            if (enemy && enemy.healthHaver && enemy.aiActor && enemy.aiActor.CanTargetEnemies &&!enemy.aiActor.CanTargetPlayers && !enemy.healthHaver.IsBoss && enemy.GetComponent<CompanionController>() == null)
            {
                enemy.healthHaver.ApplyDamage(1E+07f, Vector2.zero, "Erasure", CoreDamageTypes.None, DamageCategory.Unstoppable, true, null, false);
            }
        }
        private void PostProcessProjectile(Projectile sourceProjectile, float effectChanceScalar)
        {
            sourceProjectile.OnHitEnemy += this.OnHitEnemy;
        }
        private void PostProcessBeam(BeamController beam)
        {
            if (beam.GetComponent<Projectile>())
            {
                PostProcessProjectile(beam.GetComponent<Projectile>(), 1);
            }
        }

        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject debrisObject = base.Drop(player);
            player.PostProcessProjectile -= this.PostProcessProjectile;
            player.PostProcessBeam -= this.PostProcessBeam;


            return debrisObject;
        }
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.PostProcessProjectile += this.PostProcessProjectile;
            player.PostProcessBeam += this.PostProcessBeam;
        }
        public override void OnDestroy()
        {
            if (Owner)
            {
                Owner.PostProcessBeam -= this.PostProcessBeam;
                Owner.PostProcessProjectile -= this.PostProcessProjectile;
            }
            base.OnDestroy();
        }
    }
}
