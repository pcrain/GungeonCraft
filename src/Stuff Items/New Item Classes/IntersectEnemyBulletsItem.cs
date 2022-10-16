using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace NevernamedsItems
{
    public class IntersectEnemyBulletsItem : PassiveItem
    {
        public void onFired(Projectile bullet, float eventchancescaler)
        {
            bullet.collidesWithProjectiles = true;
            SpeculativeRigidbody specRigidbody = bullet.specRigidbody;
            specRigidbody.OnPreRigidbodyCollision = (SpeculativeRigidbody.OnPreRigidbodyCollisionDelegate)Delegate.Combine(specRigidbody.OnPreRigidbodyCollision, new SpeculativeRigidbody.OnPreRigidbodyCollisionDelegate(this.HandlePreCollision));
        }
        private void onFiredBeam(BeamController sourceBeam)
        {

        }
        public override void Pickup(PlayerController player)
        {
            player.PostProcessProjectile += this.onFired;
            player.PostProcessBeam += this.onFiredBeam;
            base.Pickup(player);
        }
        private void HandlePreCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
        {
            if (otherRigidbody && otherRigidbody.projectile)
            {
                if (otherRigidbody.projectile.Owner is PlayerController)
                {
                    PhysicsEngine.SkipCollision = true;
                    return;
                }
                if (otherRigidbody.projectile.Owner is AIActor)
                {

                    if (UnityEngine.Random.value <= 0.3f)
                    {
                        DoIntersectionEffect(myRigidbody.projectile, otherRigidbody.projectile);
                    }


                }
                bool isShootableBullet = otherRigidbody.projectile.collidesWithProjectiles;
                if (!isShootableBullet)
                {
                    PhysicsEngine.SkipCollision = true;
                }
            }
        }

        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject result = base.Drop(player);
            player.PostProcessProjectile -= this.onFired;
            player.PostProcessBeam -= this.onFiredBeam;
            return result;
        }
        public override void OnDestroy()
        {
            if (Owner != null)
            {
                Owner.PostProcessProjectile -= this.onFired;
                Owner.PostProcessBeam -= this.onFiredBeam;
            }
            base.OnDestroy();
        }
        public virtual void DoIntersectionEffect(Projectile playerBullet, Projectile enemyBullet)
        {

        }
    }
}
