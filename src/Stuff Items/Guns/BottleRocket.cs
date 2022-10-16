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
    public class BottleRocket : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Bottle Rocket", "bottlerocket");
            Game.Items.Rename("outdated_gun_mods:bottle_rocket", "nn:bottle_rocket");
            var behav = gun.gameObject.AddComponent<BottleRocket>();
            behav.preventNormalFireAudio = true;
            gun.SetShortDescription("Waterarms Afficionado");
            gun.SetLongDescription("A pressurised bottle of fluid. Pumping it any fuller will send it flying off in erratic directions."+"\n\nProne to exploding.");

            gun.SetupSprite(null, "bottlerocket_idle_001", 8);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(150) as Gun).gunSwitchGroup;

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.doesScreenShake = true;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Charged;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 0.5f;
            gun.DefaultModule.cooldownTime = 1f;
            gun.DefaultModule.angleVariance = 5;
            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(359) as Gun).muzzleFlashEffects;

            gun.DefaultModule.numberOfShotsInClip = 1;
            gun.barrelOffset.transform.localPosition = new Vector3(1.43f, 0.62f, 0f);
            gun.SetBaseMaxAmmo(100);
            gun.ammo = 100;
            gun.gunClass = GunClass.CHARGE;
            gun.SetAnimationFPS(gun.chargeAnimation, 8);
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.chargeAnimation).wrapMode = tk2dSpriteAnimationClip.WrapMode.LoopSection;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.chargeAnimation).loopStart = 0;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.chargeAnimation).frames[3].eventAudio = "Play_WPN_trashgun_impact_01";
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.chargeAnimation).frames[3].triggerEvent = true;

            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).frames[0].eventAudio = "Play_ENV_water_splash_01";
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).frames[0].triggerEvent = true;

            //Explosion           
            bottleRocketBoom = new ExplosionData()
            {
                effect = (PickupObjectDatabase.GetById(36) as Gun).DefaultModule.chargeProjectiles[1].Projectile.hitEffects.overrideMidairDeathVFX,
                ignoreList = new List<SpeculativeRigidbody>(),
                ss = StaticExplosionDatas.tetrisBlockExplosion.ss,
                damageRadius = 3.5f,
                damageToPlayer = 0f,
                doDamage = true,
                damage = 19,
                doDestroyProjectiles = false,
                doForce = true,
                debrisForce = 20f,
                preventPlayerForce = true,
                explosionDelay = 0.1f,
                usesComprehensiveDelay = false,
                doScreenShake = true,
                playDefaultSFX = false,
                force = 20,
                breakSecretWalls = false,

            };

            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            projectile.baseData.damage = 19f;
            projectile.baseData.force *= 2f;
            projectile.baseData.speed *= 1.4f;
            projectile.baseData.range *= 2f;
            projectile.hitEffects.overrideMidairDeathVFX = (PickupObjectDatabase.GetById(33) as Gun).DefaultModule.projectiles[0].hitEffects.overrideMidairDeathVFX;
            projectile.hitEffects.alwaysUseMidair = true;
            projectile.AnimateProjectile(new List<string> {
                "bottlerocketproj_001",
                "bottlerocketproj_002",
            }, 10, true, new List<IntVector2> {
                 new IntVector2(24, 16), //1
                  new IntVector2(24, 16), //2
            },
            AnimateBullet.ConstructListOfSameValues(false, 2),
            AnimateBullet.ConstructListOfSameValues(tk2dBaseSprite.Anchor.MiddleCenter, 2),
            AnimateBullet.ConstructListOfSameValues(true, 2),
            AnimateBullet.ConstructListOfSameValues(false, 2),
            AnimateBullet.ConstructListOfSameValues<Vector3?>(null, 2),
            AnimateBullet.ConstructListOfSameValues<IntVector2?>(new IntVector2(15, 8), 2),
            AnimateBullet.ConstructListOfSameValues<IntVector2?>(null, 2),
            AnimateBullet.ConstructListOfSameValues<Projectile>(null, 2));
            projectile.SetProjectileSpriteRight("bottlerocketproj_001", 24, 16, true, tk2dBaseSprite.Anchor.MiddleCenter, 15, 8);
            projectile.gameObject.AddComponent<ProjectileMotionDrift>();
            projectile.gameObject.AddComponent<BounceProjModifier>();
            GoopModifier gooper = projectile.gameObject.AddComponent<GoopModifier>();
            gooper.SpawnGoopInFlight = true;
            gooper.SpawnGoopOnCollision = true;
            gooper.CollisionSpawnRadius = 5;
            gooper.InFlightSpawnRadius = 1;
            gooper.InFlightSpawnFrequency = 0.05f;
            gooper.goopDefinition = EasyGoopDefinitions.WaterGoop;
            CustomImpactSoundBehav impactSound = projectile.gameObject.AddComponent<CustomImpactSoundBehav>();
            impactSound.ImpactSFX = "Play_ENM_blobulord_splash_01";
            ExplosiveModifier boom = projectile.gameObject.GetOrAddComponent<ExplosiveModifier>();
            boom.explosionData = bottleRocketBoom;
            boom.doExplosion = true;
            BeamBulletsBehaviour squirt = projectile.gameObject.AddComponent<BeamBulletsBehaviour>();
            squirt.firetype = BeamBulletsBehaviour.FireType.BACKWARDS;
            highPressureBeam = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(10) as Gun).DefaultModule.projectiles[0]);
            highPressureBeam.baseData.speed *= 2;
            highPressureBeam.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(highPressureBeam.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(highPressureBeam);
            squirt.beamToFire = highPressureBeam;
            ProjectileModule.ChargeProjectile chargeProj = new ProjectileModule.ChargeProjectile
            {
                Projectile = projectile,
                ChargeTime = 0.5f,
            };
            gun.DefaultModule.chargeProjectiles = new List<ProjectileModule.ChargeProjectile> { chargeProj };

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("Bottle Rocket Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/bottlerocket_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/bottlerocket_clipempty");

            gun.quality = PickupObject.ItemQuality.C;
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            highPressurePoison = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(208) as Gun).DefaultModule.projectiles[0]);
            highPressurePoison.baseData.speed *= 2;
            highPressurePoison.GetComponent<GoopModifier>().goopDefinition = EasyGoopDefinitions.PlayerFriendlyPoisonGoop;
            highPressurePoison.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(highPressurePoison.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(highPressurePoison);
            ID = gun.PickupObjectId;
        }
        public static int ID;
        public static Projectile highPressureBeam;
        public static Projectile highPressurePoison;
        public static ExplosionData bottleRocketBoom;
        public BottleRocket()
        {

        }
        public override void PostProcessProjectile(Projectile projectile)
        {
            if (projectile && projectile.ProjectilePlayerOwner() && projectile.ProjectilePlayerOwner().PlayerHasActiveSynergy("Toxic Solutions"))
            {
                projectile.GetComponent<GoopModifier>().goopDefinition = EasyGoopDefinitions.PlayerFriendlyPoisonGoop;
                projectile.GetComponent<BeamBulletsBehaviour>().beamToFire = highPressurePoison;
                projectile.AdjustPlayerProjectileTint(ExtendedColours.poisonGreen, 1);
            }
            base.PostProcessProjectile(projectile);
        }

    }
    public class ProjectileMotionDrift : MonoBehaviour
    {
        public ProjectileMotionDrift()
        {
            this.maxDriftPerTile = 4f;
            this.minDriftPerTile = 0.1f;
            this.randomInverseOnStart = true;
            this.randomiseInverseEachCheck = false;
        }
        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            this.m_projectile.ModifyVelocity += ModVeloc;
            if (!recalcDriftPerCheck) staticAngle = UnityEngine.Random.Range(minDriftPerTile, maxDriftPerTile);
            if (randomInverseOnStart && UnityEngine.Random.value <= 0.5f) staticAngle *= -1;
        }
        private Vector2 ModVeloc(Vector2 inVel)
        {
            Vector2 vector = inVel;
            if (m_projectile.GetElapsedDistance() > (distanceLastChecked + 1))
            {
                float addition;
                if (recalcDriftPerCheck) addition = UnityEngine.Random.Range(minDriftPerTile, maxDriftPerTile);
                else addition = staticAngle;
                if (randomiseInverseEachCheck && UnityEngine.Random.value <= 0.5f) addition *= -1;
                float targetAngle = inVel.ToAngle() + addition;

                vector = BraveMathCollege.DegreesToVector(targetAngle, inVel.magnitude);
                base.transform.rotation = Quaternion.Euler(0f, 0f, targetAngle);
                distanceLastChecked = m_projectile.GetElapsedDistance();
            }
            return vector;
        }

        private Projectile m_projectile;
        public float maxDriftPerTile;
        public float minDriftPerTile;
        public bool randomiseInverseEachCheck;
        public bool randomInverseOnStart;
        private float distanceLastChecked;
        public bool recalcDriftPerCheck;

        private float staticAngle;
    }
}
