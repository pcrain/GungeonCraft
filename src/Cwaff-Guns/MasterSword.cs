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
using Alexandria.Misc;

namespace CwaffingTheGungy
{

    public class MasterSword : AdvancedGunBehavior
    {
        public static void Add()
        {

            Gun gun = ETGMod.Databases.Items.NewGun("Master Sword", "carnwennan");
            Game.Items.Rename("outdated_gun_mods:master_sword", "cg:master_sword");
            var behav = gun.gameObject.AddComponent<MasterSword>();

            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(417) as Gun).gunSwitchGroup;

            gun.SetShortDescription("Dangerous Alone");
            gun.SetLongDescription("(shoots beams until you get hit, stylish hat)");
            gun.SetupSprite(null, "carnwennan_idle_001", 8);
            gun.SetAnimationFPS(gun.shootAnimation, 12);
            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);
            gun.SetBaseMaxAmmo(50);
            // gun.AddPassiveStatModifier(PlayerStats.StatType.Curse, 1f, StatModifier.ModifyMethod.ADDITIVE);

            gun.DefaultModule.ammoCost               = 1;
            gun.DefaultModule.shootStyle             = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle          = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime                           = 1.05f;
            gun.DefaultModule.cooldownTime           = 0.3f;
            gun.muzzleFlashEffects.type              = VFXPoolType.None;
            gun.DefaultModule.numberOfShotsInClip    = 5;
            gun.barrelOffset.transform.localPosition = new Vector3(9f / 16f, 4f / 16f, 0f);
            gun.quality                              = PickupObject.ItemQuality.D;
            gun.gunClass                             = GunClass.SILLY;
            gun.DefaultModule.ammoType               = GameUIAmmoType.AmmoType.BEAM;

            //BULLET STATS
            Projectile projectile               = ProjectileUtility.SetupProjectile(86);
            gun.DefaultModule.projectiles[0]    = projectile;
            gun.InfiniteAmmo                    = true;
            projectile.baseData.damage          = 4f;
            projectile.baseData.speed          *= 1.2f;
            projectile.sprite.renderer.enabled  = false;


            CarnwennanSlashModifier slash              = projectile.gameObject.AddComponent<CarnwennanSlashModifier>();
            slash.DestroyBaseAfterFirstSlash           = true;
            slash.slashParameters                      = new SlashData();
            slash.slashParameters.soundEvent           = null;
            slash.slashParameters.projInteractMode     = SlashDoer.ProjInteractMode.IGNORE;
            slash.slashParameters.playerKnockbackForce = 0;
            slash.SlashDamageUsesBaseProjectileDamage  = true;
            slash.slashParameters.enemyKnockbackForce  = 10;
            slash.slashParameters.doVFX                = false;
            slash.slashParameters.doHitVFX             = true;
            slash.slashParameters.slashRange           = 2f;

            tk2dSpriteAnimationClip reloadClip = gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.reloadAnimation);
            foreach (tk2dSpriteAnimationFrame frame in reloadClip.frames)
            {
                tk2dSpriteDefinition def = frame.spriteCollection.spriteDefinitions[frame.spriteId];
                if (def != null)
                {
                    def.MakeOffset(new Vector2(-0.81f, -2.18f));
                }
            }
            tk2dSpriteAnimationClip fireClip = gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation);
            foreach (tk2dSpriteAnimationFrame frame in fireClip.frames)
            {
                tk2dSpriteDefinition def = frame.spriteCollection.spriteDefinitions[frame.spriteId];
                if (def != null)
                {
                    def.MakeOffset(new Vector2(-0.81f, -2.18f));
                }
            }

            ETGMod.Databases.Items.Add(gun, false, "ANY");
            ID = gun.PickupObjectId;

            // gun.SetupUnlockOnCustomFlag(CustomDungeonFlags.BOSSRUSH_BULLET, true);
        }
        public MasterSword() {

        }
        public static int ID;
        public class CarnwennanSlashModifier : ProjectileSlashingBehaviour
        {
            public override void SlashHitTarget(GameActor target, bool fatal)
            {
                // float ran = UnityEngine.Random.value;
                // //ETGModConsole.Log(ran.ToString());
                // if (fatal && ran <= 0.25f)
                //     {
                //     LootEngine.SpawnItem(PickupObjectDatabase.GetById(BraveUtility.RandomElement(LootEngineItem.validIDs)).gameObject, target.sprite.WorldCenter, Vector2.zero, 0, true, true);
                //     }
                base.SlashHitTarget(target, fatal);
            }
        }
    }
}
