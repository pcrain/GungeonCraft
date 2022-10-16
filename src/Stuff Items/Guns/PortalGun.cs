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

    public class PortalGun : AdvancedGunBehavior
    {

        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Portal Gun", "portalgun");
            Game.Items.Rename("outdated_gun_mods:portal_gun", "nn:portal_gun");
            gun.gameObject.AddComponent<PortalGun>();
            gun.SetShortDescription("For Science");
            gun.SetLongDescription("");

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
            gun.gunClass = GunClass.SILLY;

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



            gun.quality = PickupObject.ItemQuality.EXCLUDED;
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            PortalGunID = gun.PickupObjectId;
            //gun.AddToSubShop(ItemBuilder.ShopType.Trorc);

            allPortals = new List<OMITBPortalController>();

        }
        public static List<OMITBPortalController> allPortals;
        public static int PortalGunID;
        public PortalGun()
        {

        }

    }
    public class OMITBPortalController : MonoBehaviour
    {
        public OMITBPortalController()
        {

        }
        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            this.m_rigidBoy = base.GetComponent<SpeculativeRigidbody>();
            m_rigidBoy.specRigidbody.OnPreRigidbodyCollision += Collision;
            foreach (OMITBPortalController portal in PortalGun.allPortals)
            {
                if (portal && portal.m_projectile.transform.position.GetAbsoluteRoom() == this.m_projectile.transform.position.GetAbsoluteRoom() && portal.linkedOther == null)
                {
                    if (portal.isOrangePortal && !this.isOrangePortal)
                    {
                        linkedOther = portal;
                        portal.linkedOther = this;
                        portal.ActivatePortal();
                        this.ActivatePortal();
                    }
                    else if (!portal.isOrangePortal && this.isOrangePortal)
                    {
                        linkedOther = portal;
                        portal.linkedOther = this;
                        portal.ActivatePortal();
                        this.ActivatePortal();
                    }
                }

            }
        }
        public void ActivatePortal()
        {
            isActive = true;
        }
        private void Collision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
        {

        }
        bool isOrangePortal;
        bool isActive;
        private Projectile m_projectile;
        public OMITBPortalController linkedOther;
        private SpeculativeRigidbody m_rigidBoy;
    }
    public class PortalGunProjectileController : MonoBehaviour
    {
        public PortalGunProjectileController()
        {

        }
        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            this.m_rigidBoy = base.GetComponent<SpeculativeRigidbody>();
            m_projectile.OnDestruction += Collision;
        }
        private void Collision(Projectile self)
        {
            GameObject portal = SpawnManager.SpawnProjectile(portalPrefab, (self.specRigidbody.UnitCenter), Quaternion.identity);
            Projectile portalProjectile = portal.GetComponent<Projectile>();

        }
        private GameObject portalPrefab;
        private Projectile m_projectile;
        private SpeculativeRigidbody m_rigidBoy;
    }
}