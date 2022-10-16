using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using Alexandria.Misc;
using Alexandria.ItemAPI;

namespace NevernamedsItems
{
    public class W3irdstar : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("W3irdstar", "w3irdstar");
            Game.Items.Rename("outdated_gun_mods:w3irdstar", "nn:w3irdstar");
            var behav = gun.gameObject.AddComponent<W3irdstar>();
            behav.overrideNormalFireAudio = "Play_WPN_seriouscannon_shot_01";
            behav.preventNormalFireAudio = true;
            gun.SetShortDescription("Weird Champion");
            gun.SetLongDescription("This wiggly weapon proves that we are all made of... star... squooge.");

            gun.SetupSprite(null, "w3irdstar_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 10);
            gun.SetAnimationFPS(gun.idleAnimation, 10);
            gun.SetAnimationFPS(gun.reloadAnimation, 1);
            gun.SetAnimationFPS(gun.chargeAnimation, 6);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);

            GameObject bulletDeath = VFXToolbox.CreateVFX("W3irdstar MidairDeath",
                  new List<string>()
                  {
                    "NevernamedsItems/Resources/MiscVFX/GunVFX/W3irdstar/w3irdstar_impact_001",
                    "NevernamedsItems/Resources/MiscVFX/GunVFX/W3irdstar/w3irdstar_impact_002",
                    "NevernamedsItems/Resources/MiscVFX/GunVFX/W3irdstar/w3irdstar_impact_003",
                    "NevernamedsItems/Resources/MiscVFX/GunVFX/W3irdstar/w3irdstar_impact_004",
                  },
                 15, //FPS
                  new IntVector2(20, 20), //Dimensions
                  tk2dBaseSprite.Anchor.MiddleCenter, //Anchor
                  false, //Uses a Z height off the ground
                  0 //The Z height, if used
                    );

            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.chargeAnimation).wrapMode = tk2dSpriteAnimationClip.WrapMode.LoopSection;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.chargeAnimation).loopStart = 2;

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Charged;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.DefaultModule.cooldownTime = 0.45f;
            gun.DefaultModule.angleVariance = 0;
            gun.DefaultModule.numberOfShotsInClip = 2;
            gun.barrelOffset.transform.localPosition = new Vector3(35f/16f, 12f / 16f, 0f);
            gun.SetBaseMaxAmmo(80);
            gun.ammo = 80;
            gun.gunClass = GunClass.SILLY;
            
            Projectile baseproj = ProjectileUtility.SetupProjectile(56);
            gun.DefaultModule.projectiles[0] = baseproj;

            //BULLET STATS
            ImprovedHelixProjectile projectile = DataCloners.CopyFields<ImprovedHelixProjectile>(Instantiate(gun.DefaultModule.projectiles[0]));
            projectile.SpawnShadowBulletsOnSpawn = false;
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            projectile.baseData.damage = 20f;
            projectile.baseData.force *= 2f;
            projectile.baseData.speed *= 0.5f;
            projectile.baseData.range = 12f;
            
            projectile.hitEffects.overrideMidairDeathVFX = bulletDeath;
            projectile.hitEffects.alwaysUseMidair = true;
            projectile.SetProjectileSpriteRight("w3irdstar_largeproj", 20, 20, true, tk2dBaseSprite.Anchor.MiddleCenter, 10, 10);
            EvenRadialBurstHandler burst = projectile.gameObject.AddComponent<EvenRadialBurstHandler>();
            burst.numberToSpawn = 40;
            burst.PostProcess = false;


            //MINI BULLETS
            Projectile split = ProjectileUtility.SetupProjectile(56);

            ImprovedHelixProjectile projectile2 = DataCloners.CopyFields<ImprovedHelixProjectile>(split);
            projectile2.SpawnShadowBulletsOnSpawn = false;
            projectile2.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile2.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile2);
            projectile2.baseData.damage = 5f;
            projectile2.baseData.range = 20f;
            projectile2.hitEffects.overrideMidairDeathVFX = bulletDeath;
            projectile2.hitEffects.alwaysUseMidair = true;
            projectile2.SetProjectileSpriteRight("w3irdstar_smallproj", 12, 12, true, tk2dBaseSprite.Anchor.MiddleCenter, 6, 6);

            burst.projectileToSpawn = projectile2;

            ProjectileModule.ChargeProjectile chargeproj = new ProjectileModule.ChargeProjectile
            {
                Projectile = projectile,
                ChargeTime = 0.5f,
            };

            gun.DefaultModule.chargeProjectiles = new List<ProjectileModule.ChargeProjectile> { chargeproj };

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("W3irdstar Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/w3irdstar_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/w3irdstar_clipempty");



            gun.quality = PickupObject.ItemQuality.A;
            ETGMod.Databases.Items.Add(gun, false, "ANY");
            W3irdstarID = gun.PickupObjectId;
        }
        public static int W3irdstarID;
    }
    public class EvenRadialBurstHandler : MonoBehaviour
    {
        public EvenRadialBurstHandler()
        {
            this.projectileToSpawn = null;
            this.numberToSpawn = 10;
        }
        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            this.speculativeRigidBoy = base.GetComponent<SpeculativeRigidbody>();
            this.m_projectile.OnDestruction += this.DoBurstProjectiles;
            spawnAngle = UnityEngine.Random.Range(1, 360);
        }

        private float spawnAngle = 0f;
        private void DoBurstProjectiles(Projectile bullet)
        {
            Projectile proj = projectileToSpawn ? projectileToSpawn : ((Gun)PickupObjectDatabase.GetById(56)).DefaultModule.projectiles[0];
            for (int i = 0; i < numberToSpawn; i++)
            {
                this.SpawnProjectile(proj, m_projectile.sprite.WorldCenter, m_projectile.transform.eulerAngles.z + spawnAngle, null);
                spawnAngle +=  360 / numberToSpawn;
            }
        }
        private void SpawnProjectile(Projectile proj, Vector3 spawnPosition, float zRotation, SpeculativeRigidbody collidedRigidbody = null)
        {
            GameObject gameObject = SpawnManager.SpawnProjectile(proj.gameObject, spawnPosition, Quaternion.Euler(0f, 0f, zRotation), true);
            Projectile component = gameObject.GetComponent<Projectile>();
            if (component)
            {
                component.SpawnedFromOtherPlayerProjectile = true;             
                component.Owner = this.m_projectile.Owner;
                component.Shooter = this.m_projectile.Shooter;
                
                if (this.m_projectile.Owner is PlayerController)
                {
                    PlayerController playerOwner = this.m_projectile.Owner as PlayerController;
                    if (PostProcess) playerOwner.DoPostProcessProjectile(component);

                }
            }
        }

        private Projectile m_projectile;
        private SpeculativeRigidbody speculativeRigidBoy;
        public Projectile projectileToSpawn;
        public bool PostProcess;
        public int numberToSpawn;

    }
}