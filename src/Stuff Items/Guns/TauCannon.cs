using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using ItemAPI;
using ChestType = Chest.GeneralChestType;
using Dungeonator;

namespace NevernamedsItems
{
    public class TauCannon : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Tau Cannon", "taucannon");
            Game.Items.Rename("outdated_gun_mods:tau_cannon", "nn:tau_cannon");
            gun.gameObject.AddComponent<TauCannon>();
            gun.SetShortDescription("Questionable Ethics");
            gun.SetLongDescription("An ingenious way devised by scientists in the field of materials handling to dispose of spent uranium."+"\n\nWill violently overcharge if powered up for too long.");

            gun.SetupSprite(null, "taucannon_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 15);
            gun.SetAnimationFPS(gun.chargeAnimation, 6);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(593) as Gun).gunSwitchGroup;
            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);

            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.chargeAnimation).wrapMode = tk2dSpriteAnimationClip.WrapMode.LoopSection;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.chargeAnimation).loopStart = 12;

            //GUN STATS
            gun.doesScreenShake = true;
            gun.gunScreenShake = (PickupObjectDatabase.GetById(37) as Gun).gunScreenShake;
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Charged;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(5) as Gun).muzzleFlashEffects;
            gun.DefaultModule.angleVariance = 2f;
            gun.DefaultModule.cooldownTime = 0.2f;
            gun.DefaultModule.numberOfShotsInClip = -1;
            gun.barrelOffset.transform.localPosition = new Vector3(3.18f, 0.75f, 0f);
            gun.SetBaseMaxAmmo(100);
            gun.ammo = 100;
            gun.gunClass = GunClass.CHARGE;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            projectile.baseData.damage = 15;
            projectile.baseData.speed = 50;
            projectile.hitEffects.alwaysUseMidair = true;
            projectile.hitEffects.overrideMidairDeathVFX = EasyVFXDatabase.YellowLaserCircleVFX;
            projectile.SetProjectileSpriteRight("tau_projectile1", 4, 2, false, tk2dBaseSprite.Anchor.MiddleCenter, 4, 2);
            EasyTrailBullet trail1 = projectile.gameObject.AddComponent<EasyTrailBullet>();
            trail1.TrailPos = projectile.transform.position;
            trail1.StartWidth = 0.12f;
            trail1.EndWidth = 0.12f;
            trail1.LifeTime = 100f;
            trail1.BaseColor = ExtendedColours.honeyYellow;
            trail1.EndColor = ExtendedColours.honeyYellow;

            //CHARGE BULLET STATS
            Projectile chargeprojectile = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            chargeprojectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(chargeprojectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(chargeprojectile);
            chargeprojectile.baseData.speed = 50;
            chargeprojectile.baseData.damage = 25;
            chargeprojectile.baseData.force *= 2;
            chargeprojectile.hitEffects.alwaysUseMidair = true;
            chargeprojectile.hitEffects.overrideMidairDeathVFX = EasyVFXDatabase.YellowLaserCircleVFX;
            chargeprojectile.SetProjectileSpriteRight("tau_projectile2", 4, 2, false, tk2dBaseSprite.Anchor.MiddleCenter, 4, 2);
            EasyTrailBullet trail2 = chargeprojectile.gameObject.AddComponent<EasyTrailBullet>();
            trail2.TrailPos = chargeprojectile.transform.position;
            trail2.StartWidth = 0.12f;
            trail2.EndWidth = 0.12f;
            trail2.LifeTime = 100f;
            trail2.BaseColor = ExtendedColours.paleYellow;
            trail2.EndColor = ExtendedColours.paleYellow;

            //Charge 2
            Projectile chargeprojectile2 = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            chargeprojectile2.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(chargeprojectile2.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(chargeprojectile2);
            chargeprojectile2.baseData.speed = 50;
            chargeprojectile2.baseData.force *= 10;
            chargeprojectile2.baseData.damage = 50;
            chargeprojectile2.hitEffects.alwaysUseMidair = true;
            chargeprojectile2.hitEffects.overrideMidairDeathVFX = EasyVFXDatabase.YellowLaserCircleVFX;
            chargeprojectile2.SetProjectileSpriteRight("tau_projectile3", 4, 2, false, tk2dBaseSprite.Anchor.MiddleCenter, 4, 2);
            EasyTrailBullet trail3 = chargeprojectile2.gameObject.AddComponent<EasyTrailBullet>();
            trail3.TrailPos = chargeprojectile2.transform.position;
            trail3.StartWidth = 0.12f;
            trail3.EndWidth = 0.12f;
            trail3.LifeTime = 100f;
            trail3.BaseColor = Color.white;
            trail3.EndColor = Color.white;

            ProjectileModule.ChargeProjectile chargeProj1 = new ProjectileModule.ChargeProjectile
            {
                Projectile = projectile,
                ChargeTime = 0f,
                VfxPool = null,
            };
            ProjectileModule.ChargeProjectile chargeProj2 = new ProjectileModule.ChargeProjectile
            {
                Projectile = chargeprojectile,
                ChargeTime = 1f,
            };
            ProjectileModule.ChargeProjectile chargeProj3 = new ProjectileModule.ChargeProjectile
            {
                Projectile = chargeprojectile2,
                ChargeTime = 2f,
            };

            gun.DefaultModule.chargeProjectiles = new List<ProjectileModule.ChargeProjectile> { chargeProj1, chargeProj2, chargeProj3 };

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("TauCannon Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/taucannon_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/taucannon_clipempty");

            gun.quality = PickupObject.ItemQuality.B;
            ETGMod.Databases.Items.Add(gun, null, "ANY");
            TauCannonID = gun.PickupObjectId;
        }
        float timeChargine = 0;
        protected override void Update()
        {
            if (gun && gun.IsCharging)
            {
                timeChargine += BraveTime.DeltaTime;
            }
            if (timeChargine > 5)
            {
                Exploder.Explode(gun.sprite.WorldCenter, StaticExplosionDatas.explosiveRoundsExplosion, Vector2.zero);
                timeChargine = 0;
            }
            if (!gun.IsCharging && timeChargine > 0)
            {
                timeChargine = 0;
            }
            base.Update();
        }
        public static int TauCannonID;
        public TauCannon()
        {

        }
    }
}
