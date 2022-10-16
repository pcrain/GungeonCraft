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
    public class Tabullets : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Tabullets";
            string resourceName = "NevernamedsItems/Resources/tabullets_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<Tabullets>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Surface Level";
            string longDesc = "Your bullets no longer damage tables, and are allowed to pass right through. Passing through a table increases a bullet's damage." + "\n\nAn initiation gift among the Knights of the Octagonal Table.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.C;
            item.SetTag("bullet_modifier");
        }

        private void PostProcessProjectile(Projectile sourceProjectile, float effectChanceScalar)
        {
            try
            {
                sourceProjectile.specRigidbody.OnPreRigidbodyCollision = (SpeculativeRigidbody.OnPreRigidbodyCollisionDelegate)Delegate.Combine(sourceProjectile.specRigidbody.OnPreRigidbodyCollision, new SpeculativeRigidbody.OnPreRigidbodyCollisionDelegate(this.HandlePreCollision));
            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message);
            }
        }
        private void HandlePreCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
        {
            if (otherRigidbody.gameObject.name != null)
            {
                if (otherRigidbody.gameObject.name == "Table_Vertical" || otherRigidbody.gameObject.name == "Table_Horizontal")
                {
                    if (!tablesHitAlready.Contains(otherRigidbody))
                    {
                        tablesHitAlready.Add(otherRigidbody);
                        myRigidbody.projectile.baseData.damage *= 1.2f;
                        myRigidbody.projectile.RuntimeUpdateScale(1.2f);
                    }
                    PhysicsEngine.SkipCollision = true;
                }
            }
        }
        private List<SpeculativeRigidbody> tablesHitAlready = new List<SpeculativeRigidbody>();
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