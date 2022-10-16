using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Reflection;
using Gungeon;
using MonoMod;
using UnityEngine;
using ItemAPI;
using SaveAPI;

namespace NevernamedsItems
{

    public class BulletBlade : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Bullet Blade", "bulletblade");
            Game.Items.Rename("outdated_gun_mods:bullet_blade", "nn:bullet_blade");
            var behav = gun.gameObject.AddComponent<BulletBlade>();
            behav.preventNormalFireAudio = true;
            behav.preventNormalReloadAudio = true;
            //behav.overrideNormalFireAudio = "Play_OBJ_gate_slam_01";//"Play_ENM_gunnut_swing_01";

            gun.SetShortDescription("Forged of Pure Bullet");
            gun.SetLongDescription("The hefty blade of the fearsome armoured sentinels that tread the Gungeon's Halls." + "\n\nHas claimed the life of many a careless gungeoneer with it's wide spread.");
            gun.SetupSprite(null, "bulletblade_idle_001", 8);
            gun.SetAnimationFPS(gun.shootAnimation, 12);
            gun.SetAnimationFPS(gun.chargeAnimation, 6);

            gun.AddPassiveStatModifier(PlayerStats.StatType.Curse, 1f, StatModifier.ModifyMethod.ADDITIVE);

            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).frames[0].eventAudio = "Play_ENM_gunnut_shockwave_01";
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).frames[0].triggerEvent = true;

            for (int i = 0; i < 45; i++)
            {
                gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);
            }

            //GUN STATS
            foreach (ProjectileModule mod in gun.Volley.projectiles)
            {
                mod.ammoCost = 1;
                mod.shootStyle = ProjectileModule.ShootStyle.Charged;
                mod.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
                mod.cooldownTime = 2.5f;
                mod.angleVariance = 70f;
                mod.numberOfShotsInClip = 1;
                Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(mod.projectiles[0]);
                mod.projectiles[0] = projectile;
                projectile.gameObject.SetActive(false);
                FakePrefab.MarkAsFakePrefab(projectile.gameObject);
                UnityEngine.Object.DontDestroyOnLoad(projectile);
                projectile.baseData.damage *= 1.6f;
                projectile.baseData.speed *= 0.6f;
                projectile.baseData.range *= 1f;
                projectile.SetProjectileSpriteRight("enemystyle_projectile", 10, 10, true, tk2dBaseSprite.Anchor.MiddleCenter, 8, 8);
                if (mod != gun.DefaultModule) { mod.ammoCost = 0; }
                projectile.transform.parent = gun.barrelOffset;

                ProjectileModule.ChargeProjectile chargeProj = new ProjectileModule.ChargeProjectile
                {
                    Projectile = projectile,
                    ChargeTime = 1f,
                };
                mod.chargeProjectiles = new List<ProjectileModule.ChargeProjectile> { chargeProj };
            }
            gun.reloadTime = 1f;
            gun.SetBaseMaxAmmo(50);
            gun.quality = PickupObject.ItemQuality.B;
            gun.gunClass = GunClass.CHARGE;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.chargeAnimation).wrapMode = tk2dSpriteAnimationClip.WrapMode.LoopSection;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.chargeAnimation).loopStart = 4;

            tk2dSpriteAnimationClip chargeClip = gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.chargeAnimation);
            foreach (tk2dSpriteAnimationFrame frame in chargeClip.frames)
            {
                tk2dSpriteDefinition def = frame.spriteCollection.spriteDefinitions[frame.spriteId];
                if (def != null)
                {
                    def.MakeOffset(new Vector2(-0.56f, -2.31f));
                }
            }
            tk2dSpriteAnimationClip fireClip = gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation);
            foreach (tk2dSpriteAnimationFrame frame in fireClip.frames)
            {
                tk2dSpriteDefinition def = frame.spriteCollection.spriteDefinitions[frame.spriteId];
                if (def != null)
                {
                    def.MakeOffset(new Vector2(-0.56f, -2.31f));
                }
            }

            gun.encounterTrackable.EncounterGuid = "this is the Bullet Blade";
            ETGMod.Databases.Items.Add(gun, null, "ANY");
            gun.barrelOffset.transform.localPosition = new Vector3(3.18f, -0.31f, 0f);
            BulletBladeID = gun.PickupObjectId;

            gun.SetupUnlockOnCustomFlag(CustomDungeonFlags.JAMMEDGUNNUT_QUEST_REWARDED, true);
        }
        public static int BulletBladeID;
        public override void PostProcessProjectile(Projectile projectile)
        {
            base.PostProcessProjectile(projectile);
        }
        public BulletBlade()
        {

        }
    }
}