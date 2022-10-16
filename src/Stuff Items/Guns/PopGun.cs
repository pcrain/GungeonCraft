using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using Alexandria.ItemAPI;
using Alexandria.Misc;

namespace NevernamedsItems
{

    public class PopGun : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Pop Gun", "popgun");
            Game.Items.Rename("outdated_gun_mods:pop_gun", "nn:pop_gun");
            var behav = gun.gameObject.AddComponent<PopGun>();
            behav.preventNormalReloadAudio = true;
            gun.SetShortDescription("Pop Goes");
            gun.SetLongDescription("A children's toy."+"\n\nFires pellets on a string to be reeled back into the barrel. Deals more damage while it's shots are being yanked back in.");

            gun.SetupSprite(null, "popgun_idle_001", 8);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(150) as Gun).gunSwitchGroup;

            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(28) as Gun).muzzleFlashEffects;

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 0.7f;
            gun.DefaultModule.cooldownTime = 0.7f;
            gun.DefaultModule.numberOfShotsInClip = 1;
            gun.barrelOffset.transform.localPosition = new Vector3(1.31f, 0.43f, 0f);
            gun.SetBaseMaxAmmo(100);
            gun.ammo = 100;
            gun.gunClass = GunClass.SILLY;

            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            projectile.baseData.damage = 10f;
            projectile.baseData.speed *= 1f;
            projectile.baseData.range *= 10f;

            projectile.gameObject.AddComponent<PopGunBullet>();

            BounceProjModifier bounce = projectile.gameObject.AddComponent<BounceProjModifier>();
            bounce.numberOfBounces += 10;

            PierceProjModifier pierce = projectile.gameObject.AddComponent<PierceProjModifier>();
            pierce.penetration = 10;

            projectile.pierceMinorBreakables = true;

            projectile.SetProjectileSpriteRight("popgun_proj", 7, 7, false, tk2dBaseSprite.Anchor.MiddleCenter, 6, 6);
            gun.DefaultModule.projectiles[0] = projectile;

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("PopGun Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/popgun_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/popgun_clipempty");

            gun.quality = PickupObject.ItemQuality.D;
            ETGMod.Databases.Items.Add(gun, null, "ANY");
            PopGunID = gun.PickupObjectId;

        }
        public static int PopGunID;
    }
    public class PopGunBullet : MonoBehaviour
    {
        private Projectile m_projectile;
        private PlayerController owner;
        private float initialSpeed;
        private bool isReturning;
        private bool hasStopped;
        private ArbitraryCableDrawer m_cable;
        private int connectedGunID;

        public PopGunBullet()
        {

        }
        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            owner = m_projectile.ProjectilePlayerOwner();
            initialSpeed = m_projectile.baseData.speed;
            
            StartCoroutine(DoSpeedChange());
        }
        private void Update()
        {
            if (owner && m_projectile && owner.CurrentGun)
            {
                if (owner.CurrentGun.IsReloading)
                {
                    if (!isReturning && hasStopped)
                    {
                        DoReturn();
                    }
                }
                if (isReturning)
                {
                    if (Vector2.Distance(owner.CurrentGun.barrelOffset.position, m_projectile.sprite.WorldCenter) < 1)
                    {
                        m_projectile.DieInAir();
                    }
                }
                if (connectedGunID != owner.CurrentGun.PickupObjectId)
                {
                    RecalcCable();
                    connectedGunID = owner.CurrentGun.PickupObjectId;
                }
            }
        }
        private void RecalcCable()
        {
            if (m_cable)
            {
                UnityEngine.Object.Destroy(m_cable);
                m_cable = null;
            }
            this.m_cable = m_projectile.gameObject.AddComponent<ArbitraryCableDrawer>();
            this.m_cable.Attach2Offset = m_projectile.specRigidbody.UnitCenter - m_projectile.transform.position.XY();
            this.m_cable.Initialize(this.owner.CurrentGun.barrelOffset, m_projectile.transform);
        }
        private IEnumerator DoSpeedChange()
        {
            float realTime = 1;
            realTime *= m_projectile.ProjectilePlayerOwner().stats.GetStatValue(PlayerStats.StatType.RangeMultiplier);

            float elapsed = 0f;
            while (elapsed < realTime)
            {
                elapsed += BraveTime.DeltaTime;
                float t = Mathf.Clamp01(elapsed / realTime);
                float speedMod = Mathf.Lerp(initialSpeed, 0, t);

                m_projectile.baseData.speed = speedMod;
                m_projectile.UpdateSpeed();

                if (isReturning) break;
                yield return null;
            }
            hasStopped = true;
        }
        private void OnDestroy()
        {
            if (isReturning) m_projectile.ModifyVelocity -= ModifyVelocity;
            if (m_cable)
            {
                UnityEngine.Object.Destroy(m_cable);
            }
        }
        private void DoReturn()
        {
            isReturning = true;
            if (owner && owner.CurrentGun)
            {
                Vector2 dirToBarrel = m_projectile.sprite.WorldCenter.CalculateVectorBetween(owner.CurrentGun.barrelOffset.position);
                m_projectile.SendInDirection(dirToBarrel, true);
                m_projectile.baseData.speed = initialSpeed;
                m_projectile.baseData.damage *= 2;
                m_projectile.UpdateSpeed();
                m_projectile.ModifyVelocity += ModifyVelocity;
            }
        }
        private Vector2 ModifyVelocity(Vector2 inVel)
        {
            Vector2 vector = inVel;

            Vector2 vector3 = (!m_projectile.sprite) ? base.transform.position.XY() : m_projectile.sprite.WorldCenter;

            if (owner && owner.CurrentGun != null)
            {
                Vector2 vector2 = owner.CurrentGun.barrelOffset.position - vector3.ToVector3ZUp();

                float num3 = vector2.ToAngle();
                float num4 = inVel.ToAngle();
                float num5 = 300 * this.m_projectile.LocalDeltaTime;
                float num6 = Mathf.MoveTowardsAngle(num4, num3, num5);
                if (this.m_projectile is HelixProjectile)
                {
                    float angleDiff = num6 - num4;
                    (this.m_projectile as HelixProjectile).AdjustRightVector(angleDiff);
                }
                else
                {
                    if (this.m_projectile.shouldRotate)
                    {
                        base.transform.rotation = Quaternion.Euler(0f, 0f, num6);
                    }
                    vector = BraveMathCollege.DegreesToVector(num6, inVel.magnitude);
                }
                if (this.m_projectile.OverrideMotionModule != null)
                {
                    this.m_projectile.OverrideMotionModule.AdjustRightVector(num6 - num4);
                }
            }
            if (vector == Vector2.zero || float.IsNaN(vector.x) || float.IsNaN(vector.y))
            {
                return inVel;
            }
            return vector;
        }
    }
}

