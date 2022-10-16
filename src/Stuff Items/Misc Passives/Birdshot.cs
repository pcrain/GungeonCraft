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
    public class Birdshot : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Birdshot";
            string resourceName = "NevernamedsItems/Resources/birdshot_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<Birdshot>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Angry";
            string longDesc = "+40% damage to flying enemies." + "\n\nA collection of tiny pellets enchanted with an inherent hatred for anything that dares to leave the ground." + "\nQuarantined in the Gungeon after several unfortunate incidents in an airport.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.B;
            item.SetTag("bullet_modifier");
            BirdshotID = item.PickupObjectId;
        }
        public static int BirdshotID;
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
            if (otherRigidbody && otherRigidbody.healthHaver && otherRigidbody.aiActor)
            {
                if (otherRigidbody.aiActor.IsFlying)
                {
                    float originalDamage = myRigidbody.projectile.baseData.damage;
                    myRigidbody.projectile.baseData.damage *= 1.4f;
                    myRigidbody.StartCoroutine(this.ChangeProjectileDamage(myRigidbody.projectile, originalDamage));
                }
            }
        }
        private IEnumerator ChangeProjectileDamage(Projectile bullet, float oldDamage)
        {
            yield return new WaitForSeconds(0.1f);
            if (bullet != null)
            {
                bullet.baseData.damage = oldDamage;
            }
            yield break;
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject debrisObject = base.Drop(player);
            player.PostProcessProjectile -= this.PostProcessProjectile;
            return debrisObject;
        }
        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            player.PostProcessProjectile += this.PostProcessProjectile;
        }
        public override void OnDestroy()
        {
            if (Owner)
            {
                Owner.PostProcessProjectile -= this.PostProcessProjectile;
            }
            base.OnDestroy();
        }
    }
}
