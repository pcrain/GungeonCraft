using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using ItemAPI;
using Alexandria.Misc;

namespace NevernamedsItems
{

    public class MaidenRifle : AdvancedGunBehavior
    {

        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Maiden Rifle", "maidenrifle");
            Game.Items.Rename("outdated_gun_mods:maiden_rifle", "nn:maiden_rifle");
            gun.gameObject.AddComponent<MaidenRifle>();
            gun.SetShortDescription("Lady Death");
            gun.SetLongDescription("Reverse engineered Lead Maiden technology." + "\n\nOriginally, the projectiles would rotate when in walls, but this was removed for efficiency, and definitely not because quaternions and vectors are more painful than tapdancing on an echidna.");

            gun.SetupSprite(null, "maidenrifle_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 17);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Automatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 2f;
            gun.DefaultModule.cooldownTime = 0.15f;
            gun.DefaultModule.numberOfShotsInClip = 30;
            gun.barrelOffset.transform.localPosition = new Vector3(1.68f, 0.62f, 0f);
            gun.SetBaseMaxAmmo(400);
            gun.ammo = 400;
            gun.gunClass = GunClass.FULLAUTO;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage *= 1.6f;
            projectile.pierceMinorBreakables = true;
            projectile.SetProjectileSpriteRight("friendlymaiden_projectile", 25, 12, true, tk2dBaseSprite.Anchor.MiddleCenter, 6, 8);
            LeadMaidenProjectileReAiming orAddComponent = projectile.gameObject.GetOrAddComponent<LeadMaidenProjectileReAiming>();

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("MaidenRifle Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/maidenrifle_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/maidenrifle_clipempty");

            gun.quality = PickupObject.ItemQuality.B;
            ETGMod.Databases.Items.Add(gun, null, "ANY");



            MaidenRifleID = gun.PickupObjectId;
            gun.AddToSubShop(ItemBuilder.ShopType.Cursula);

        }
        public static int MaidenRifleID;
        public MaidenRifle()
        {

        }
        public override void PostProcessProjectile(Projectile projectile)
        {
            PlayerController player = projectile.Owner as PlayerController;
            if (player != null)
            {
                if (player.PlayerHasActiveSynergy("Double Maiden"))
                {
                    projectile.baseData.range *= 2;
                    projectile.baseData.damage *= 1.2f;

                    PierceProjModifier keepComponent = projectile.gameObject.GetOrAddComponent<PierceProjModifier>();
                    keepComponent.penetratesBreakables = true;
                    keepComponent.penetration = 2;
                }
            }
            base.PostProcessProjectile(projectile);
        }

    }
    public class LeadMaidenProjectileReAiming : MonoBehaviour
    {
        public LeadMaidenProjectileReAiming()
        {
            this.timesReAimed = 0;
            this.maxTimedToReAim = 1;
            this.reAimCooldown = 5f;
            this.canReAimRightNow = true;
        }
        public int timesReAimed;
        public int maxTimedToReAim;
        public float reAimCooldown;
        private bool canReAimRightNow;
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
        }
        private void OnCollision(CollisionData tileCollision)
        {
            this.m_projectile.baseData.speed *= 0.0001f;
            this.m_projectile.UpdateSpeed();
            this.m_hitNormal = tileCollision.Normal.ToAngle();
            PhysicsEngine.PostSliceVelocity = new Vector2?(default(Vector2));
            SpeculativeRigidbody specRigidbody = this.m_projectile.specRigidbody;
            specRigidbody.OnCollision -= this.OnCollision;
        }
        private void Update()
        {
            if (canReAimRightNow && timesReAimed < maxTimedToReAim && projOwner != null && projOwner.CurrentGun != null)
            {
                if (projOwner.CurrentGun.IsReloading)
                {
                    timesReAimed += 1;
                    GameManager.Instance.StartCoroutine(OnReload());
                    canReAimRightNow = false;
                    Invoke("HandleCooldown", reAimCooldown);
                }
            }
        }
        private void HandleCooldown() { canReAimRightNow = true; }
        private IEnumerator OnReload()
        {
            yield return new WaitForSeconds(UnityEngine.Random.value);

            /*this.directionOfNearestEnemy = ReAimBullet.GetDegreeToNearestEnemy(this.m_projectile);
            Quaternion target = Quaternion.Euler(0, 0, directionOfNearestEnemy);
            
            for (int i = 0; i < 1000; i++)
            {
                this.m_projectile.transform.rotation = Quaternion.RotateTowards(this.m_projectile.transform.rotation, target, 3f);
                yield return new WaitForSeconds(0.01f);
                if (this.m_projectile.transform.rotation == target)
                {
                    ETGModConsole.Log("Broke out of reaiming");
                    break;
                }
            }

            this.m_projectile.SendInDirection(MiscToolbox.DegreeToVector2(this.directionOfNearestEnemy), false, true);*/
            Vector2 dirVec = m_projectile.GetVectorToNearestEnemy();
            m_projectile.SendInDirection(dirVec, false, true);
            this.m_projectile.baseData.speed *= 10000;
            this.m_projectile.UpdateSpeed();

            this.m_projectile.specRigidbody.CollideWithTileMap = false;
            this.m_projectile.UpdateCollisionMask();

            yield return new WaitForSeconds(0.25f);

            this.m_projectile.specRigidbody.CollideWithTileMap = true;
            this.m_projectile.UpdateCollisionMask();

            this.m_projectile.BulletScriptSettings.surviveTileCollisions = false;


            yield break;
        }
        private Projectile m_projectile;
        private float m_hitNormal;
        private PlayerController projOwner;
        //private float directionOfNearestEnemy;
    }
}