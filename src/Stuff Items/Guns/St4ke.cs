using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using ItemAPI;

namespace NevernamedsItems
{

    public class St4ke : AdvancedGunBehavior
    {

        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("St4ke", "st4ke");
            Game.Items.Rename("outdated_gun_mods:st4ke", "nn:st4ke");
            gun.gameObject.AddComponent<St4ke>();
            gun.SetShortDescription("For Robot Vampires");
            gun.SetLongDescription("Fires miniature tesla coils that stick into walls."+"\n\nFollowing the success of her remote diote transmitter experiment, the tinker set about seeing if she could make both ends of the tesla-relay mobile.");

            gun.SetupSprite(null, "st4ke_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 17);

            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(153) as Gun).gunSwitchGroup;

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.DefaultModule.cooldownTime = 0.15f;
            gun.DefaultModule.numberOfShotsInClip = 4;
            gun.barrelOffset.transform.localPosition = new Vector3(1.68f, 0.62f, 0f);
            gun.SetBaseMaxAmmo(175);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(153) as Gun).gunSwitchGroup;
            gun.ammo = 175;
            gun.gunClass = GunClass.PISTOL;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage = 8f;
            projectile.pierceMinorBreakables = true;
            projectile.SetProjectileSpriteRight("st4keproj", 13, 7, true, tk2dBaseSprite.Anchor.MiddleCenter, 9, 5);
            St4keProj orAddComponent = projectile.gameObject.GetOrAddComponent<St4keProj>();
            PierceProjModifier piercing = projectile.gameObject.GetOrAddComponent<PierceProjModifier>();
            piercing.penetration++;

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("Stk4ke Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/st4ke_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/st4ke_clipempty");

            gun.quality = PickupObject.ItemQuality.A;
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            St4keID = gun.PickupObjectId;
            gun.AddToSubShop(ItemBuilder.ShopType.Trorc);

            LinkVFXPrefab = FakePrefab.Clone(Game.Items["shock_rounds"].GetComponent<ComplexProjectileModifier>().ChainLightningVFX);
            FakePrefab.MarkAsFakePrefab(LinkVFXPrefab);
            UnityEngine.Object.DontDestroyOnLoad(LinkVFXPrefab);
        }
        public static int St4keID;
        public static GameObject LinkVFXPrefab;
        public St4ke()
        {

        }
      
    }
    public class St4keProj : MonoBehaviour
    {
        public St4keProj()
        {
            DamagePerHit = 7;
            IsElectricitySource = false;               
        }
        public static  Projectile lastFiredSt4keBullet = null;
        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            if (this.m_projectile.Owner is PlayerController)
            {
                this.projOwner = this.m_projectile.Owner as PlayerController;
            }
            SpeculativeRigidbody specRigidBody = this.m_projectile.specRigidbody;
            this.m_projectile.BulletScriptSettings.surviveTileCollisions = true;
            specRigidBody.OnCollision += this.OnCollision;

            if (lastFiredSt4keBullet == null || lastFiredSt4keBullet.gameObject == null || !lastFiredSt4keBullet.isActiveAndEnabled)
            {
                lastFiredSt4keBullet = m_projectile;
            }
            else
            {
                if (!lastFiredSt4keBullet.GetComponent<St4keProj>().IsElectricitySource)
                {
                    IsElectricitySource = true;
                    electricTarget = lastFiredSt4keBullet;
                }
                lastFiredSt4keBullet = m_projectile;
            }
        }
        private void Update()
        {
            if (m_projectile && electricTarget && this.extantLink == null)
            {
                tk2dTiledSprite component = SpawnManager.SpawnVFX(St4ke.LinkVFXPrefab, false).GetComponent<tk2dTiledSprite>();
                this.extantLink = component;
            }
            else if (m_projectile && electricTarget && this.extantLink != null)
            {
                UpdateLink(electricTarget, this.extantLink);
            }
            else if (extantLink != null)
            {
                SpawnManager.Despawn(extantLink.gameObject);
                extantLink = null;
            }
        }
        private void UpdateLink(Projectile target, tk2dTiledSprite m_extantLink)
        {
            Vector2 unitCenter = m_projectile.specRigidbody.UnitCenter;
            Vector2 unitCenter2 = target.specRigidbody.HitboxPixelCollider.UnitCenter;
            m_extantLink.transform.position = unitCenter;
            Vector2 vector = unitCenter2 - unitCenter;
            float num = BraveMathCollege.Atan2Degrees(vector.normalized);
            int num2 = Mathf.RoundToInt(vector.magnitude / 0.0625f);
            m_extantLink.dimensions = new Vector2((float)num2, m_extantLink.dimensions.y);
            m_extantLink.transform.rotation = Quaternion.Euler(0f, 0f, num);
            m_extantLink.UpdateZDepth();
            this.ApplyLinearDamage(unitCenter, unitCenter2);
        }
        private void ApplyLinearDamage(Vector2 p1, Vector2 p2)
        {
            float num = this.DamagePerHit;
            num *= projOwner.stats.GetStatValue(PlayerStats.StatType.Damage);
            for (int i = 0; i < StaticReferenceManager.AllEnemies.Count; i++)
            {
                AIActor aiactor = StaticReferenceManager.AllEnemies[i];
                if (!this.m_damagedEnemies.Contains(aiactor))
                {
                    if (aiactor && aiactor.HasBeenEngaged && aiactor.IsNormalEnemy && aiactor.specRigidbody)
                    {
                        Vector2 zero = Vector2.zero;
                        if (BraveUtility.LineIntersectsAABB(p1, p2, aiactor.specRigidbody.HitboxPixelCollider.UnitBottomLeft, aiactor.specRigidbody.HitboxPixelCollider.UnitDimensions, out zero))
                        {
                            aiactor.healthHaver.ApplyDamage(num, Vector2.zero, "Chain Lightning", CoreDamageTypes.Electric, DamageCategory.Normal, false, null, false);
                            GameManager.Instance.StartCoroutine(this.HandleDamageCooldown(aiactor));
                        }
                    }
                }
            }
        }
        private void OnDestroy()
        {
            if (extantLink)
            {
                SpawnManager.Despawn(extantLink.gameObject);
            }
        }
        private IEnumerator HandleDamageCooldown(AIActor damagedTarget)
        {
            this.m_damagedEnemies.Add(damagedTarget);
            yield return new WaitForSeconds(0.25f);
            this.m_damagedEnemies.Remove(damagedTarget);
            yield break;
        }
        public float DamagePerHit;
        private tk2dTiledSprite extantLink;
        private HashSet<AIActor> m_damagedEnemies = new HashSet<AIActor>();
        private void OnCollision(CollisionData tileCollision)
        {
            this.m_projectile.baseData.speed *= 0f;
            this.m_projectile.UpdateSpeed();
            this.m_hitNormal = tileCollision.Normal.ToAngle();
            PhysicsEngine.PostSliceVelocity = new Vector2?(default(Vector2));
            SpeculativeRigidbody specRigidbody = this.m_projectile.specRigidbody;
            specRigidbody.OnCollision -= this.OnCollision;
            BulletLifeTimer orAddComponent = m_projectile.gameObject.GetOrAddComponent<BulletLifeTimer>();
            orAddComponent.secondsTillDeath = 20;
        }

        private Projectile electricTarget;
        public bool IsElectricitySource;
        private Projectile m_projectile;
        private float m_hitNormal;
        private PlayerController projOwner;
    }
}