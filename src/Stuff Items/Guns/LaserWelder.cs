using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Reflection;
using Gungeon;
using MonoMod;
using UnityEngine;
using Alexandria.ItemAPI;
using Alexandria.EnemyAPI;

namespace NevernamedsItems
{

    public class LaserWelder : GunBehaviour
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Laser Welder", "laserwelder");
            Game.Items.Rename("outdated_gun_mods:laser_welder", "nn:laser_welder");
            gun.gameObject.AddComponent<LaserWelder>();
            gun.SetShortDescription("Cleanup in Detail");
            gun.SetLongDescription("Blasts enemies apart in a burst of gruesome viscera... someone's gonna have to clean up that mess..." + "\n\nAllows for the repair of objects.");

            gun.SetupSprite(null, "laserwelder_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 15);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(153) as Gun).gunSwitchGroup;

            //GUN STATS
            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(58) as Gun).muzzleFlashEffects;
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Automatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 0.4f;
            gun.DefaultModule.cooldownTime = 0.11f;
            gun.DefaultModule.numberOfShotsInClip = 5;
            gun.barrelOffset.transform.localPosition = new Vector3(53f / 16f, 11f / 16f, 0f);
            gun.SetBaseMaxAmmo(230);
            gun.gunClass = GunClass.EXPLOSIVE;
            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.MEDIUM_BLASTER;

            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage = 5;
            projectile.baseData.force = 4f;
            projectile.baseData.speed = 600f;
            projectile.AppliesFire = false;
            projectile.gameObject.AddComponent<LaserWelderComp>();
            // projectile.sprite.renderer.enabled = false;
            projectile.hitEffects = (PickupObjectDatabase.GetById(32) as Gun).DefaultModule.projectiles[0].hitEffects;

            projectile.SetProjectileSpriteRight("laserwelder_proj", 10, 3, true, tk2dBaseSprite.Anchor.MiddleCenter, 10, 3);
            projectile.hitEffects.CenterDeathVFXOnProjectile = true;

            List<string> BeamAnimPaths = new List<string>()
            {
                "NevernamedsItems/Resources/TrailSprites/laserwelder_trail_001",
                "NevernamedsItems/Resources/TrailSprites/laserwelder_trail_002",
                "NevernamedsItems/Resources/TrailSprites/laserwelder_trail_003",
                "NevernamedsItems/Resources/TrailSprites/laserwelder_trail_004",
                "NevernamedsItems/Resources/TrailSprites/laserwelder_trail_005",
            };
            List<string> ImpactAnimPaths = new List<string>()
            {
                "NevernamedsItems/Resources/TrailSprites/laserwelder_trailstart_001",
                "NevernamedsItems/Resources/TrailSprites/laserwelder_trailstart_002",
                "NevernamedsItems/Resources/TrailSprites/laserwelder_trailstart_003",
                "NevernamedsItems/Resources/TrailSprites/laserwelder_trailstart_004",
                "NevernamedsItems/Resources/TrailSprites/laserwelder_trailstart_005",
            };

            projectile.AddTrailToProjectile(
                "NevernamedsItems/Resources/TrailSprites/laserwelder_trail_001",
                new Vector2(17, 5),
                new Vector2(0, 6),
                BeamAnimPaths, 20,
                ImpactAnimPaths, 20,
                0.1f,
                -1,
                -1,
                true
                );

            gun.quality = PickupObject.ItemQuality.B;
            ETGMod.Databases.Items.Add(gun, false, "ANY");

            ID = gun.PickupObjectId;

            LaserWelderExplosion = new ExplosionData()
            {
                breakSecretWalls = false,
                effect = EasyVFXDatabase.BloodExplosion,
                doDamage = true,
                damageRadius = 2.5f,
                damageToPlayer = 0,
                damage = 30,
                debrisForce = 20,
                doExplosionRing = true,
                doDestroyProjectiles = true,
                doForce = true,
                doScreenShake = true,
                playDefaultSFX = true,
                ignoreList = new List<SpeculativeRigidbody>() { },
                pushRadius = 4,
                ss = GameManager.Instance.Dungeon.sharedSettingsPrefab.DefaultSmallExplosionData.ss,
                force = 20,
            };
        }
        public static ExplosionData LaserWelderExplosion;
        public static int ID;
    }
    public class LaserWelderComp : MonoBehaviour
    {
        public LaserWelderComp()
        {
            RepairableEnemies = new Dictionary<string, string>(){
            {"e5cffcfabfae489da61062ea20539887", "01972dee89fc4404a5c408d50007dad5"}, // Shroomer -> Bullet Kin
            {"d4a9836f8ab14f3fadd0f597438b1f1f", "01972dee89fc4404a5c408d50007dad5"}, // Mutant Bullet Kin -> Bullet Kin
            {"844657ad68894a4facb1b8e1aef1abf9", "01972dee89fc4404a5c408d50007dad5"}, // Confirmed -> Bullet Kin
            {"7f665bd7151347e298e4d366f8818284", "128db2f0781141bcb505d8f00f9e4d47"}, // Mutant Shotgun Kin -> Red Shotgun Kin
            {"1a78cfb776f54641b832e92c44021cf2", "01972dee89fc4404a5c408d50007dad5"}, // Ashen Bullet Kin -> Bullet Kin
            {"1bd8e49f93614e76b140077ff2e33f2b", "b54d89f9e802455cbb2b8a96a31e8259"}, // Ashen Shotgun Kin -> Blue Shotgun Kin
             };
        }
        private Projectile self;
        private void Start()
        {
            self = base.GetComponent<Projectile>();
            self.OnHitEnemy += OnHitEnemy;
            if (self.specRigidbody) self.specRigidbody.OnPreRigidbodyCollision += HandlePreCollision;
        }
        private void HandlePreCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
        {
            try
            {
                if (otherRigidbody)
                {
                    foreach (MajorBreakable breakable in otherRigidbody.GetComponentsInChildren<MajorBreakable>())
                    {
                        breakable.TemporarilyInvulnerable = true;
                        //ETGModConsole.Log(breakable.sprite.CurrentSprite.name);
                        if (breakable.HitPoints < breakable.MaxHitPoints)
                        {
                            float missingHP = breakable.MaxHitPoints - breakable.HitPoints;

                            breakable.HitPoints += (missingHP * 0.2f);
                            if (breakable.GetComponent<Chest>())
                            {
                               
                                breakable.GetComponent<Chest>().ForceKillFuse();
                                int idleID = OMITBReflectionHelpers.ReflectGetField<int>(typeof(Chest), "m_cachedSpriteForCoop", breakable.GetComponent<Chest>());
                                OMITBReflectionHelpers.ReflectSetField<bool>(typeof(MajorBreakable), "m_inZeroHPState", false, breakable);
                                breakable.sprite.SetSprite(idleID);
                            }
                        }
                        breakable.StartCoroutine(ResetInvul(breakable));
                    }
                }
            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message);
                ETGModConsole.Log(e.StackTrace);
            }
        }
        private IEnumerator ResetInvul(MajorBreakable breakable)
        {
            yield return new WaitForSeconds(0.1f);
            breakable.TemporarilyInvulnerable = false;
            yield break;
        }
        private void OnHitEnemy(Projectile self, SpeculativeRigidbody enemy, bool fatal)
        {
            if (enemy && enemy.aiActor && enemy.healthHaver)
            {
                //ETGModConsole.Log("OnHitEnemyRan");
                if (fatal)
                {
                    Vector2 pos = enemy.sprite.WorldCenter;

                    Exploder.Explode(pos, LaserWelder.LaserWelderExplosion, Vector2.zero);
                    UnityEngine.Object.Instantiate<GameObject>(EasyVFXDatabase.TeleporterPrototypeTelefragVFX, enemy.UnitCenter, Quaternion.identity);
                    if (!enemy.healthHaver.IsBoss) enemy.aiActor.EraseFromExistenceWithRewards();
                }
                else if (RepairableEnemies.ContainsKey(enemy.aiActor.EnemyGuid))
                {
                    //ETGModConsole.Log("Met requirements for transmog");
                    enemy.aiActor.AdvancedTransmogrify(EnemyDatabase.GetOrLoadByGuid(RepairableEnemies[enemy.aiActor.EnemyGuid]), null, null, false, null, null, true, true, true, true, true, false);
                }
                //ETGModConsole.Log("OnHitEnemyFinished");
            }
        }
        public static Dictionary<string, string> RepairableEnemies;
    }
}
