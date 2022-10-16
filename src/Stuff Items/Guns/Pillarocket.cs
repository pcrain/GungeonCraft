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
    public class Pillarocket : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Pillarocket", "pillarocket");
            Game.Items.Rename("outdated_gun_mods:pillarocket", "nn:pillarocket");
            var behav = gun.gameObject.AddComponent<Pillarocket>();
            behav.preventNormalFireAudio = true;
            behav.preventNormalReloadAudio = true;
            behav.overrideNormalFireAudio = "Play_ENM_statue_stomp_01";
            behav.overrideNormalReloadAudio = "Play_ENM_statue_charge_01";
            gun.SetShortDescription("Ancient Shrine");
            gun.SetLongDescription("Fires vengeful effigies, under your command." + "\n\nInhabited by the souls of ancient gundead heroes.");

            gun.SetupSprite(null, "pillarocket_idle_001", 8);


            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(37) as Gun).gunSwitchGroup;
            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(39) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 4f;
            gun.DefaultModule.cooldownTime = 2f;
            gun.DefaultModule.angleVariance = 20;
            gun.DefaultModule.numberOfShotsInClip = 4;
            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(372) as Gun).muzzleFlashEffects;
            gun.barrelOffset.transform.localPosition = new Vector3(2.18f, 1.68f, 0f);
            gun.SetBaseMaxAmmo(30);
            gun.ammo = 30;

            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            projectile.baseData.speed = 6.5f;
            projectile.baseData.damage = 80f;
            projectile.baseData.range = 100;
            projectile.baseData.force = 10;
            projectile.pierceMinorBreakables = true;
            RemoteBulletsProjectileBehaviour remote = projectile.gameObject.GetOrAddComponent<RemoteBulletsProjectileBehaviour>();
            remote.trackingTime = 1000;
            PillarocketFiring firing = projectile.gameObject.GetOrAddComponent<PillarocketFiring>();

            projectile.AnimateProjectile(new List<string> {
                "pillarocket_proj_001",
                "pillarocket_proj_002",
                "pillarocket_proj_003",
                "pillarocket_proj_004",
                "pillarocket_proj_005",
                "pillarocket_proj_006",
                "pillarocket_proj_007",
                "pillarocket_proj_008",
                "pillarocket_proj_008",
                "pillarocket_proj_008",
                "pillarocket_proj_008",
                "pillarocket_proj_008",
                "pillarocket_proj_008",
                "pillarocket_proj_008",
                "pillarocket_proj_009",
                "pillarocket_proj_010",
                "pillarocket_proj_011",
                "pillarocket_proj_012",
                "pillarocket_proj_013",
                "pillarocket_proj_014",
            }, 20, //FPS
            true, //LOOPS
            AnimateBullet.ConstructListOfSameValues<IntVector2>(new IntVector2(15, 8), 20), //PIXELSIZES
            AnimateBullet.ConstructListOfSameValues(false, 20), //LIGHTENEDS
            AnimateBullet.ConstructListOfSameValues(tk2dBaseSprite.Anchor.MiddleLeft, 20), //ANCHORS
            AnimateBullet.ConstructListOfSameValues(false, 20), //ANCHORSCHANGECOLLIDERS
            AnimateBullet.ConstructListOfSameValues(false, 20),
            AnimateBullet.ConstructListOfSameValues<Vector3?>(null, 20),
            AnimateBullet.ConstructListOfSameValues<IntVector2?>(new IntVector2(15, 6), 20),
            AnimateBullet.ConstructListOfSameValues<IntVector2?>(null, 20),
            AnimateBullet.ConstructListOfSameValues<Projectile>(null, 20));

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("Pillarocket Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/pillarocket_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/pillarocket_clipempty");

            gun.DefaultModule.projectiles[0] = projectile;
            gun.gunClass = GunClass.EXPLOSIVE;
            gun.quality = PickupObject.ItemQuality.S;
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            Projectile akproj = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(15) as Gun).DefaultModule.projectiles[0]);
            akproj.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(akproj.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(akproj);
            akproj.SetProjectileSpriteRight("pillarocket_subprojectile", 5, 5, true, tk2dBaseSprite.Anchor.MiddleCenter, 3, 3);
            PillarocketAKProj = akproj;

            Projectile magnum = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(38) as Gun).DefaultModule.projectiles[0]);
            magnum.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(magnum.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(magnum);
            magnum.SetProjectileSpriteRight("pillarocket_subprojectile", 5, 5, true, tk2dBaseSprite.Anchor.MiddleCenter, 3, 3);
            PillarocketMagnumProj = magnum;

            Projectile shotproj = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(51) as Gun).DefaultModule.projectiles[0]);
            shotproj.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(shotproj.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(shotproj);
            shotproj.SetProjectileSpriteRight("pillarocket_subprojectile", 5, 5, true, tk2dBaseSprite.Anchor.MiddleCenter, 3, 3);
            PillarocketShotgunProj = shotproj;
            ID = gun.PickupObjectId;
        }
        public static int ID;
        public static Projectile PillarocketAKProj;
        public static Projectile PillarocketMagnumProj;
        public static Projectile PillarocketShotgunProj;
        public Pillarocket()
        {

        }
    }
    public class PillarocketFiring : MonoBehaviour
    {
        public PillarocketFiring()
        {
            hasStarted = false;
        }
        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            this.m_rigidBody = this.m_projectile.specRigidbody;
            if (this.m_projectile.Owner is PlayerController) this.m_player = this.m_projectile.Owner as PlayerController;
            int selectedType = UnityEngine.Random.Range(1, 4);
            if (selectedType == 1) //AK
            {
                this.TimeBetweenAttacks = 0.11f;
                this.AngleVariance = 4;
                this.numToFire = 1;
                projectileToFire = Pillarocket.PillarocketAKProj;
            }
            else if (selectedType == 2) //Magnum
            {
                this.TimeBetweenAttacks = 0.15f;
                this.AngleVariance = 7;
                this.numToFire = 1;
                projectileToFire = Pillarocket.PillarocketMagnumProj;

            }
            else if (selectedType == 3) //Shotgun
            {
                this.TimeBetweenAttacks = 0.6f;
                this.AngleVariance = 10;
                this.numToFire = 6;
                projectileToFire = Pillarocket.PillarocketShotgunProj;
            }
            hasStarted = true;
        }
        private void Update()
        {
            if (hasStarted && this.m_projectile && this.m_player && this.m_rigidBody)
            {
                if (timer > 0)
                {
                    timer -= BraveTime.DeltaTime;
                }
                if (timer <= 0)
                {
                    for (int i = 0; i < numToFire; i++)
                    {
                        if (m_projectile.sprite.WorldCenter.GetNearestEnemyToPosition())
                        {
                            Vector2 projectileSpawnPosition = this.m_projectile.sprite.WorldCenter;
                            Vector2 nearestEnemyPosition = projectileSpawnPosition.GetNearestEnemyToPosition().Position;
                            if (nearestEnemyPosition != Vector2.zero)
                            {
                                GameObject gameObject = ProjSpawnHelper.SpawnProjectileTowardsPoint(this.projectileToFire.gameObject, projectileSpawnPosition, nearestEnemyPosition, 0, AngleVariance);
                                Projectile component = gameObject.GetComponent<Projectile>();
                                if (component != null)
                                {
                                    component.Owner = this.m_player;
                                    component.TreatedAsNonProjectileForChallenge = true;
                                    component.Shooter = this.m_rigidBody;
                                    component.collidesWithPlayer = false;
                                    component.baseData.damage *= this.m_player.stats.GetStatValue(PlayerStats.StatType.Damage);
                                    component.baseData.speed *= this.m_player.stats.GetStatValue(PlayerStats.StatType.ProjectileSpeed);
                                    component.baseData.range *= this.m_player.stats.GetStatValue(PlayerStats.StatType.RangeMultiplier);
                                    component.AdditionalScaleMultiplier *= this.m_player.stats.GetStatValue(PlayerStats.StatType.PlayerBulletScale); ;
                                    component.baseData.force *= this.m_player.stats.GetStatValue(PlayerStats.StatType.KnockbackMultiplier);
                                    component.BossDamageMultiplier *= this.m_player.stats.GetStatValue(PlayerStats.StatType.DamageToBosses);
                                    this.m_player.DoPostProcessProjectile(component);
                                }
                            }
                        }
                    }
                    timer = TimeBetweenAttacks;
                }
            }
        }
        private bool hasStarted;
        private float timer;
        private float TimeBetweenAttacks;
        private float AngleVariance;
        private Projectile projectileToFire;
        private int numToFire;
        private Projectile m_projectile;
        private SpeculativeRigidbody m_rigidBody;
        private PlayerController m_player;
    }
}

