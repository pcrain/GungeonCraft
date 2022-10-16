using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Alexandria.ItemAPI;
using SaveAPI;

namespace NevernamedsItems
{
    public class MirrorBullets : PassiveItem
    {
        public static void Init()
        {
            string itemName = "Mirror Bullets";
            string resourceName = "NevernamedsItems/Resources/mirrorbullets_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<MirrorBullets>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Upon Further Reflection...";
            string longDesc = "Scoring a direct hit on enemy bullets sends them right back at their owners." + "\n\nOf all the greatest horrors, and most abhorrent foes one may face, perhaps the most dangerous is the one that stares back at you from the mirror.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            item.quality = PickupObject.ItemQuality.S;
            item.SetTag("bullet_modifier");
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.Curse, 3, StatModifier.ModifyMethod.ADDITIVE);
            item.SetupUnlockOnCustomFlag(CustomDungeonFlags.ALLJAMMED_BEATEN_HOLLOW, true);
        }
        public void onFired(Projectile bullet, float eventchancescaler)
        {
            MirrorProjectileModifier mirrorProjectileModifier = bullet.gameObject.AddComponent<MirrorProjectileModifier>();
            mirrorProjectileModifier.MirrorRadius = 3f;
        }
        private void onFiredBeam(BeamController sourceBeam)
        {
            sourceBeam.AdjustPlayerBeamTint(Color.white, 1);
            sourceBeam.gameObject.AddComponent<EnemyBulletReflectorBeam>();
        }
        public override void Pickup(PlayerController player)
        {
            player.PostProcessProjectile += this.onFired;
            player.PostProcessBeam += this.onFiredBeam;
            base.Pickup(player);
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
            if (Owner)
            {
                Owner.PostProcessProjectile -= this.onFired;
                Owner.PostProcessBeam -= this.onFiredBeam;
            }
            base.OnDestroy();
        }
    }
    public class MirrorProjectileModifier : MonoBehaviour
    {
        // Token: 0x06007293 RID: 29331 RVA: 0x002CA2F4 File Offset: 0x002C84F4
        public MirrorProjectileModifier()
        {
            this.MirrorRadius = 3f;

        }

        // Token: 0x06007294 RID: 29332 RVA: 0x002CA328 File Offset: 0x002C8528
        private void Awake()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            this.m_projectile.AdjustPlayerProjectileTint(Color.white, 2, 0f);
            this.m_projectile.collidesWithProjectiles = true;
            SpeculativeRigidbody specRigidbody = this.m_projectile.specRigidbody;
            specRigidbody.OnPreRigidbodyCollision = (SpeculativeRigidbody.OnPreRigidbodyCollisionDelegate)Delegate.Combine(specRigidbody.OnPreRigidbodyCollision, new SpeculativeRigidbody.OnPreRigidbodyCollisionDelegate(this.HandlePreCollision));
        }

        // Token: 0x06007295 RID: 29333 RVA: 0x002CA3A0 File Offset: 0x002C85A0
        private void Update()
        {
            Vector2 b = this.m_projectile.transform.position.XY();
            for (int i = 0; i < StaticReferenceManager.AllProjectiles.Count; i++)
            {
                Projectile projectile = StaticReferenceManager.AllProjectiles[i];
                if (projectile && projectile.Owner is AIActor)
                {
                    float sqrMagnitude = (projectile.transform.position.XY() - b).sqrMagnitude;
                    if (sqrMagnitude < this.MirrorRadius)
                    {
                        //this.ReflectBullet(projectile, true, , 10f, 1f, 1f, 0f);
                    }
                }
            }
        }

        // Token: 0x06007296 RID: 29334 RVA: 0x002CA444 File Offset: 0x002C8644
        private void HandlePreCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
        {
            if (otherRigidbody && otherRigidbody.projectile)
            {
                if (otherRigidbody.projectile.Owner is AIActor)
                {
                    myRigidbody.projectile.DieInAir(false, true, true, false);
                    this.ReflectBullet(otherRigidbody.projectile, true, myRigidbody.projectile.Owner, 10f, 1f, 1f, 0f);
                }
                PhysicsEngine.SkipCollision = true;
            }
        }

        // Token: 0x06007297 RID: 29335 RVA: 0x002CA4A0 File Offset: 0x002C86A0
        public void ReflectBullet(Projectile p, bool retargetReflectedBullet, GameActor newOwner, float minReflectedBulletSpeed, float scaleModifier = 1f, float damageModifier = 1f, float spread = 0f)
        {
            p.RemoveBulletScriptControl();
            AkSoundEngine.PostEvent("Play_OBJ_metalskin_deflect_01", GameManager.Instance.gameObject);
            bool flag = retargetReflectedBullet && p.Owner && p.Owner.specRigidbody;
            if (flag)
            {
                p.Direction = (p.Owner.specRigidbody.GetUnitCenter(ColliderType.HitBox) - p.specRigidbody.UnitCenter).normalized;
            }
            bool flag2 = spread != 0f;
            if (flag2)
            {
                p.Direction = p.Direction.Rotate(UnityEngine.Random.Range(-spread, spread));
            }
            bool flag3 = p.Owner && p.Owner.specRigidbody;
            if (flag3)
            {
                p.specRigidbody.DeregisterSpecificCollisionException(p.Owner.specRigidbody);
            }
            p.Owner = newOwner;
            p.SetNewShooter(newOwner.specRigidbody);
            p.allowSelfShooting = false;
            p.collidesWithPlayer = false;
            p.collidesWithEnemies = true;
            bool flag4 = scaleModifier != 1f;
            if (flag4)
            {
                SpawnManager.PoolManager.Remove(p.transform);
                p.RuntimeUpdateScale(scaleModifier);
            }
            bool flag5 = p.Speed < minReflectedBulletSpeed;
            if (flag5)
            {
                p.Speed = minReflectedBulletSpeed;
            }
            bool flag6 = p.baseData.damage < ProjectileData.FixedFallbackDamageToEnemies;
            if (flag6)
            {
                p.baseData.damage = ProjectileData.FixedFallbackDamageToEnemies;
            }
            p.baseData.damage *= damageModifier;
            bool flag7 = p.baseData.damage < 10f;
            if (flag7)
            {
                p.baseData.damage = 15f;
            }
            p.UpdateCollisionMask();
            p.Reflected();
            p.SendInDirection(p.Direction, true, true);
        }
        // Token: 0x040073E3 RID: 29667
        public float MirrorRadius;

        // Token: 0x040073E5 RID: 29669
        private Projectile m_projectile;
    }

}
