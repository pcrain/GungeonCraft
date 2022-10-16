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
    public class Pumhart : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Pumhart", "pumhart");
            Game.Items.Rename("outdated_gun_mods:pumhart", "nn:pumhart");
            var behav = gun.gameObject.AddComponent<Pumhart>();
            gun.SetShortDescription("Mega Bore");
            gun.SetLongDescription("A magnificent example of bombard weaponry, built to slay giants!"+"\n\nNormally impossible for a single person to wield, Gunymede's reduced gravity somewhat allows this ridiculous feat.");

            gun.SetupSprite(null, "pumhart_idle_001", 8);
            gun.SetAnimationFPS(gun.chargeAnimation, 1);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);
            gun.AddCurrentGunStatModifier(PlayerStats.StatType.MovementSpeed, 0.6f, StatModifier.ModifyMethod.MULTIPLICATIVE);
            gun.AddCurrentGunStatModifier(PlayerStats.StatType.DodgeRollSpeedMultiplier, 0.6f, StatModifier.ModifyMethod.MULTIPLICATIVE);
            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Charged;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 5f;
            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(37) as Gun).muzzleFlashEffects;
            gun.DefaultModule.cooldownTime = 5f;
            gun.DefaultModule.angleVariance = 0;
            gun.DefaultModule.numberOfShotsInClip = 1;
            gun.barrelOffset.transform.localPosition = new Vector3(6.43f, 1.81f, 0f);
            gun.SetBaseMaxAmmo(10);
            gun.ammo = 10;
            gun.gunClass = GunClass.CHARGE;

            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            projectile.baseData.damage = 150f;
            projectile.baseData.force *= 10f;
            projectile.baseData.speed *= 1f;
            projectile.baseData.range = 10000f;
            PierceProjModifier pierce = projectile.gameObject.GetOrAddComponent<PierceProjModifier>();
            pierce.penetration += 140;
            MaintainDamageOnPierce maintainDMG = projectile.gameObject.GetOrAddComponent<MaintainDamageOnPierce>();
            pierce.penetratesBreakables = true;
            BlockEnemyProjectilesMod block = projectile.gameObject.GetOrAddComponent<BlockEnemyProjectilesMod>();
            block.projectileSurvives = true;
            BounceProjModifier bounce = projectile.gameObject.GetOrAddComponent<BounceProjModifier>();
            bounce.numberOfBounces += 140;
            bounce.additionalScreenShake = (PickupObjectDatabase.GetById(37) as Gun).DefaultModule.chargeProjectiles[0].Projectile.GetComponent<BounceProjModifier>().additionalScreenShake;
            projectile.hitEffects.overrideMidairDeathVFX = (PickupObjectDatabase.GetById(37) as Gun).DefaultModule.chargeProjectiles[0].Projectile.hitEffects.overrideMidairDeathVFX;
            projectile.hitEffects.alwaysUseMidair = true;
            projectile.SetProjectileSpriteRight("pumhart_proj", 48, 48, false, tk2dBaseSprite.Anchor.MiddleCenter, 30, 30);

            ProjectileModule.ChargeProjectile chargeProj = new ProjectileModule.ChargeProjectile
            {
                Projectile = projectile,
                ChargeTime = 5,
            };
            gun.DefaultModule.chargeProjectiles = new List<ProjectileModule.ChargeProjectile> { chargeProj };

            gun.quality = PickupObject.ItemQuality.S;
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.chargeAnimation).wrapMode = tk2dSpriteAnimationClip.WrapMode.LoopSection;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.chargeAnimation).loopStart = 4;

            tk2dSpriteAnimationClip chargeClip = gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.chargeAnimation);
            foreach (tk2dSpriteAnimationFrame frame in chargeClip.frames)
            {
                tk2dSpriteDefinition def = frame.spriteCollection.spriteDefinitions[frame.spriteId];
                if (def != null)
                {
                    def.MakeOffset(new Vector2(-2.12f, -1.75f));
                }
            }
            tk2dSpriteAnimationClip fireClip = gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation);
            foreach (tk2dSpriteAnimationFrame frame in fireClip.frames)
            {
                tk2dSpriteDefinition def = frame.spriteCollection.spriteDefinitions[frame.spriteId];
                if (def != null)
                {
                    def.MakeOffset(new Vector2(-2.12f, -1.75f));
                }
            }
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(37) as Gun).gunSwitchGroup;

            gun.gunScreenShake =  new ScreenShakeSettings(5f, 1f, 0.5f, 0.5f); 
            PumhartID = gun.PickupObjectId;
        }
        public static int PumhartID;
        public Pumhart()
        {

        }
    }     
}
