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
    public class GlueGunGlueGunnerSynergy : GunBehaviour
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Glue Gunner", "gluegunner");
            Game.Items.Rename("outdated_gun_mods:glue_gunner", "nn:hot_glue_gun+glue_gunner");
            gun.gameObject.AddComponent<GlueGunGlueGunnerSynergy>();
            gun.SetShortDescription("yes this is a btd reference");
            gun.SetLongDescription("im too tired to write a snarky description");

            gun.SetupSprite(null, "gluegunner_idle_001", 8);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(199) as Gun).gunSwitchGroup;

            gun.SetAnimationFPS(gun.shootAnimation, 12);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Automatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.DefaultModule.cooldownTime = 0.15f;
            gun.DefaultModule.numberOfShotsInClip = 15;
            gun.barrelOffset.transform.localPosition = new Vector3(1.37f, 0.56f, 0f);
            gun.SetBaseMaxAmmo(340);
            gun.ammo = 340;

            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            projectile.baseData.damage *= 1f;
            projectile.baseData.speed *= 1f;
            projectile.baseData.range *= 1f;

            ApplyLockdownBulletBehaviour applylockdown = projectile.gameObject.AddComponent<ApplyLockdownBulletBehaviour>();
            applylockdown.useSpecialBulletTint = false;
            applylockdown.duration = 6f;
            applylockdown.enemyTintColour = ExtendedColours.paleYellow;
            applylockdown.TintEnemy = true;
            applylockdown.TintCorpse = true;
            applylockdown.corpseTintColour = ExtendedColours.paleYellow;

            ExtremelySimpleStatusEffectBulletBehaviour burning = projectile.gameObject.AddComponent<ExtremelySimpleStatusEffectBulletBehaviour>();
            burning.onHitProcChance = 0.14f;
            burning.onFiredProcChance = 1f;
            burning.usesFireEffect = false;
            burning.usesPoisonEffect = true;
            burning.poisonEffect = StaticStatusEffects.irradiatedLeadEffect;
            projectile.SetProjectileSpriteRight("gluegunner_projectile", 19, 8, false, tk2dBaseSprite.Anchor.MiddleCenter, 18, 7);

            gun.DefaultModule.projectiles[0] = projectile;

            gun.quality = PickupObject.ItemQuality.EXCLUDED;
            ETGMod.Databases.Items.Add(gun, null, "ANY");
            GlueGunnerID = gun.PickupObjectId;

        }
        public static int GlueGunnerID;
        public GlueGunGlueGunnerSynergy()
        {

        }
    }
}