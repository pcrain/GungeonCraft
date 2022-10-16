using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using ItemAPI;
using Dungeonator;
using Alexandria.Misc;

namespace NevernamedsItems
{
    public class Snaker : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Snaker", "snaker");
            Game.Items.Rename("outdated_gun_mods:snaker", "nn:snaker");
       var behav =     gun.gameObject.AddComponent<Snaker>();
            behav.overrideNormalReloadAudio = "Play_OBJ_mine_beep_01";
            behav.overrideNormalFireAudio = "Play_OBJ_mine_beep_01";
            behav.preventNormalFireAudio = true;
            behav.preventNormalReloadAudio = true;
            gun.SetShortDescription("Tail As Old As Time");
            gun.SetLongDescription("Firing creates red 'apples' around the room. Shooting through these 'apples' buffs shots." + "\n\nA very hungry snake... or maybe it's a worm? It's hard to tell with so few pixels.");

            gun.SetupSprite(null, "snaker_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 12);
            gun.SetAnimationFPS(gun.idleAnimation, 8);
            gun.SetAnimationFPS(gun.reloadAnimation, 24);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(328) as Gun).gunSwitchGroup;

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Automatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1.1f;
            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(334) as Gun).muzzleFlashEffects;

            gun.DefaultModule.cooldownTime = 0.45f;
            gun.DefaultModule.numberOfShotsInClip = 10;

            gun.barrelOffset.transform.localPosition = new Vector3(2.06f, 0.68f, 0f);
            gun.SetBaseMaxAmmo(200);
            gun.ammo = 200;
            gun.gunClass = GunClass.SILLY;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            projectile.gameObject.AddComponent<SnakerBulletController>();
            projectile.baseData.range *= 2f;
            projectile.BossDamageMultiplier = 5;
            projectile.baseData.damage *= 2;
            projectile.SetProjectileSpriteRight("snaker_projectile", 6, 6, true, tk2dBaseSprite.Anchor.MiddleCenter, 4, 4);
            

            //APPLE BULLET
            Projectile apple = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(86) as Gun).DefaultModule.projectiles[0]);
            apple.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(apple.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(apple);
            apple.baseData.damage = 0;
            
            apple.baseData.speed = 0;
            NoCollideBehaviour nocollide = apple.gameObject.GetOrAddComponent<NoCollideBehaviour>();
            apple.gameObject.AddComponent<SnakerAppleController>();
            nocollide.worksOnEnemies = true;
            nocollide.worksOnProjectiles = false;
            apple.specRigidbody.CollideWithTileMap = false;
            apple.SetProjectileSpriteRight("snakerapple_projectile", 12, 12, true, tk2dBaseSprite.Anchor.MiddleCenter, 12, 12);
            AppleBullet = apple;

            gun.DefaultModule.projectiles[0] = projectile;

            gun.quality = PickupObject.ItemQuality.B;
            ETGMod.Databases.Items.Add(gun, null, "ANY");
        }
        public static Projectile AppleBullet;
        public int ApplesEatenThisRoom = 0;
        private RoomHandler currentRoom;
        private RoomHandler lastRoom;

        public Snaker()
        {

        }
        protected override void Update()
        {
            if (gun.CurrentOwner && gun.CurrentOwner is PlayerController)
            {
                currentRoom = (gun.CurrentOwner as PlayerController).CurrentRoom;
                if (currentRoom != lastRoom)
                {
                    ApplesEatenThisRoom = 0;
                    lastRoom = currentRoom;
                }
            }
            base.Update();
        }
        public override void PostProcessProjectile(Projectile projectile)
        {
            SnakerBulletController cont = projectile.GetComponent<SnakerBulletController>();
            if (cont)
            {
                if (gun.GetComponent<Snaker>())
                {
                    cont.sourceGun = gun.GetComponent<Snaker>();
                }
            }
            if (projectile.Owner && projectile.Owner is PlayerController && (projectile.Owner as PlayerController).PlayerHasActiveSynergy("Snake World Champion"))
            {
                int amount = Math.Min(10, ApplesEatenThisRoom);
                projectile.StartCoroutine(this.DoShadowDelayed(amount, projectile));
            }
            base.PostProcessProjectile(projectile);
        }
        private IEnumerator DoShadowDelayed(int amount, Projectile projectile)
        {
            yield return null;
            projectile.SpawnChainedShadowBullets(amount, 0.01f);
            yield break;
        }
        public override void OnPostFired(PlayerController player, Gun gun)
        {
            if (player && player.CurrentRoom != null)
            {
                Vector3 positionToSpawn = player.CurrentRoom.GetRandomVisibleClearSpot(1, 1).ToVector3();
                GameObject apple = SpawnManager.SpawnProjectile(AppleBullet.gameObject, positionToSpawn, Quaternion.identity);
                Projectile proj = apple.GetComponent<Projectile>();
                if (proj)
                {
                    proj.Owner = player;
                    proj.Shooter = player.specRigidbody;
                    proj.TreatedAsNonProjectileForChallenge = true;
                    if (player.PlayerHasActiveSynergy("High Score"))
                    {
                        HungryProjectileModifier hungry = proj.gameObject.GetOrAddComponent<HungryProjectileModifier>();
                        hungry.HungryRadius = 1.5f;
                        hungry.MaximumBulletsEaten = 8;
                        hungry.DamagePercentGainPerSnack = 0.1f;
                        proj.AdjustPlayerProjectileTint(ExtendedColours.purple, 1);
                    }
                }
            }
            base.OnPostFired(player, gun);
        }
    }
    public class SnakerBulletController : MonoBehaviour
    {
        public SnakerBulletController()
        {
            this.hasEatenApple = false;
        }
        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            this.m_projectile.collidesWithProjectiles = true;
            this.m_projectile.collidesOnlyWithPlayerProjectiles = true;
            this.m_projectile.UpdateCollisionMask();
            this.m_projectile.specRigidbody.OnPreRigidbodyCollision += this.PreCollision;
        }
        private void PreCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
        {
            if (myRigidbody != null && otherRigidbody != null)
            {
                if (otherRigidbody.projectile && otherRigidbody.projectile.Owner is PlayerController)
                {
                    if (otherRigidbody.gameObject.GetComponent<SnakerAppleController>())
                    {
                        m_projectile.baseData.damage *= 3;
                        m_projectile.RuntimeUpdateScale(1.5f);
                        m_projectile.gameObject.GetOrAddComponent<HomingModifier>();
                        BounceProjModifier bounce = m_projectile.gameObject.GetOrAddComponent<BounceProjModifier>();
                        bounce.numberOfBounces++;
                        otherRigidbody.projectile.DieInAir(true, false, false, false);
                        if (sourceGun != null && !hasEatenApple)
                        {
                            sourceGun.ApplesEatenThisRoom++;
                        }
                        hasEatenApple = true;
                    }
                    PhysicsEngine.SkipCollision = true;
                }
            }
        }
        private bool hasEatenApple;
        public Snaker sourceGun;
        private Projectile m_projectile;
    }
    public class SnakerAppleController : MonoBehaviour
    {
        public SnakerAppleController()
        {
            this.timer = 40;
        }
        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            if (m_projectile && m_projectile.Owner && m_projectile.Owner is PlayerController) Owner = m_projectile.Owner as PlayerController;
        }
        private void Update()
        {
            if (!Owner || Owner.CurrentRoom != m_projectile.transform.position.GetAbsoluteRoom())
            {
                m_projectile.DieInAir(true, false, false, false);
            }
            if (timer > 0)
            {
                timer -= BraveTime.DeltaTime;
            }
            if (timer <= 0)
            {
                m_projectile.DieInAir(true, false, false, false);
            }
        }
        private float timer;
        private Projectile m_projectile;
        private PlayerController Owner;
    }
}

