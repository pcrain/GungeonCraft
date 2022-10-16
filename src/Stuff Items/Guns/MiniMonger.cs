using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using Alexandria.ItemAPI;

namespace NevernamedsItems
{
    public class MiniMonger : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Mini Monger", "minimonger");
            Game.Items.Rename("outdated_gun_mods:mini_monger", "nn:mini_monger");
            var behav = gun.gameObject.AddComponent<MiniMonger>();
            behav.preventNormalFireAudio = true;
            behav.overrideNormalFireAudio = "Play_ENM_demonwall_barf_01";
            behav.preventNormalReloadAudio = true;
            behav.overrideNormalReloadAudio = "Play_ENM_demonwall_intro_01";
            gun.SetShortDescription("Great Wall");
            gun.SetLongDescription("A scale model of the fearsome Wallmonger, used as a mock-up during it's original construction." + "\n\nWhile the Wallmonger contains hundreds of tortured souls, this only contains two or three.");
            gun.AddPassiveStatModifier(PlayerStats.StatType.Curse, 1f, StatModifier.ModifyMethod.ADDITIVE);
            gun.SetupSprite(null, "minimonger_idle_001", 8);


            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).frames[0].eventAudio = "Play_ENM_demonwall_barf_01";
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).frames[0].triggerEvent = true;

            gun.SetAnimationFPS(gun.shootAnimation, 10);
            gun.SetAnimationFPS(gun.chargeAnimation, 5);

            for (int i = 0; i < 5; i++)
            {
                gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);
            }

            //GUN STATS
            foreach (ProjectileModule mod in gun.Volley.projectiles)
            {
                mod.ammoCost = 1;
                mod.shootStyle = ProjectileModule.ShootStyle.Charged;
                mod.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
                mod.cooldownTime = 2f;
                mod.angleVariance = 15f;
                mod.numberOfShotsInClip = 4;
                Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(mod.projectiles[0]);
                mod.projectiles[0] = projectile;
                projectile.gameObject.SetActive(false);
                FakePrefab.MarkAsFakePrefab(projectile.gameObject);
                UnityEngine.Object.DontDestroyOnLoad(projectile);
                projectile.baseData.speed *= 0.5f;
                projectile.baseData.damage *= 1.6f;
                AutoDoShadowChainOnSpawn chain = projectile.gameObject.GetOrAddComponent<AutoDoShadowChainOnSpawn>();
                chain.NumberInChain = 5;
                chain.pauseLength = 0.1f;
                projectile.SetProjectileSpriteRight("pillarocket_subprojectile", 5, 5, true, tk2dBaseSprite.Anchor.MiddleCenter, 3, 3);

                if (mod != gun.DefaultModule) { mod.ammoCost = 0; }
                ProjectileModule.ChargeProjectile chargeProj = new ProjectileModule.ChargeProjectile
                {
                    Projectile = projectile,
                    ChargeTime = 1f,
                };
                mod.chargeProjectiles = new List<ProjectileModule.ChargeProjectile> { chargeProj };
            }
            gun.reloadTime = 2f;
            gun.SetBaseMaxAmmo(50);
            gun.gunClass = GunClass.SHOTGUN;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.chargeAnimation).wrapMode = tk2dSpriteAnimationClip.WrapMode.LoopSection;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.chargeAnimation).loopStart = 3;

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = "Punishment Ray Lasers";

            gun.quality = PickupObject.ItemQuality.A;
            ETGMod.Databases.Items.Add(gun, null, "ANY");
            MiniMongerID = gun.PickupObjectId;
        }
        public static int MiniMongerID;
        public override void OnReloadPressed(PlayerController player, Gun gun, bool manualReload)
        {
            if (gun.ClipShotsRemaining < gun.ClipCapacity)
            {
                DeadlyDeadlyGoopManager goopManagerForGoopType = DeadlyDeadlyGoopManager.GetGoopManagerForGoopType(EasyGoopDefinitions.FireDef);
                Vector2 vector = gun.sprite.WorldCenter;
                Vector2 normalized = (player.unadjustedAimPoint.XY() - vector).normalized;
                goopManagerForGoopType.TimedAddGoopLine(gun.sprite.WorldCenter, gun.sprite.WorldCenter + normalized * 10, 1.5f, 0.5f);
            }
            base.OnReloadPressed(player, gun, manualReload);
        }
        protected override void Update()
        {
            base.Update();
        }
        public override void OnSwitchedAwayFromThisGun()
        {
            if (gun.CurrentOwner && gun.CurrentOwner is PlayerController)
            {
                RemoveFireImmunity(gun.CurrentOwner as PlayerController);
            }
            base.OnSwitchedAwayFromThisGun();
        }
        public override void OnSwitchedToThisGun()
        {
            if (gun.CurrentOwner && gun.CurrentOwner is PlayerController)
            {
                GiveFireImmunity(gun.CurrentOwner as PlayerController);
            }
            base.OnSwitchedToThisGun();
        }
        protected override void OnPickedUpByPlayer(PlayerController player)
        {
            GiveFireImmunity(player);
            base.OnPickedUpByPlayer(player);
        }
        protected override void OnPostDroppedByPlayer(PlayerController player)
        {
            RemoveFireImmunity(player);
            base.OnPostDroppedByPlayer(player);
        }
        public override void OnDestroy()
        {
            if (gun && gun.CurrentOwner && gun.CurrentOwner is PlayerController)
            {
                RemoveFireImmunity(gun.CurrentOwner as PlayerController);
            }
            base.OnDestroy();
        }

        private void GiveFireImmunity(PlayerController playerController)
        {
            if (this.m_fireImmunity == null)
            {
                this.m_fireImmunity = new DamageTypeModifier();
                this.m_fireImmunity.damageMultiplier = 0f;
                this.m_fireImmunity.damageType = CoreDamageTypes.Fire;
            }
            playerController.healthHaver.damageTypeModifiers.Add(this.m_fireImmunity);
        }
        private DamageTypeModifier m_fireImmunity;

        private void RemoveFireImmunity(PlayerController playerController)
        {
            playerController.healthHaver.damageTypeModifiers.Remove(this.m_fireImmunity);
        }
        public MiniMonger()
        {

        }
    }
}
