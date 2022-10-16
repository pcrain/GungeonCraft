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
    public class EpimethianBullets : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Epimethian Bullets";
            string resourceName = "NevernamedsItems/Resources/epimethianbullets_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<EpimethianBullets>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Afterthought";
            string longDesc = "Deals 50% more damage to enemies who have less than half their health."+"\n\nCreated by the mighty titan bullet Epimetheus.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.B;
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
            if (otherRigidbody && otherRigidbody.healthHaver)
            {
                float MaxEnemyHealth = otherRigidbody.healthHaver.GetMaxHealth();
                float HalfEnemyHealth = MaxEnemyHealth * 0.5f;
                float CurrentEnemyHealth = otherRigidbody.healthHaver.GetCurrentHealth();
                if (CurrentEnemyHealth < HalfEnemyHealth)
                {
                    float originalDamage = myRigidbody.projectile.baseData.damage;
                    myRigidbody.projectile.baseData.damage *= 1.5f;
                    GameManager.Instance.StartCoroutine(this.ChangeProjectileDamage(myRigidbody.projectile, originalDamage));
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
            if (Owner) Owner.PostProcessProjectile -= this.PostProcessProjectile;
            base.OnDestroy();
        }
    }
}