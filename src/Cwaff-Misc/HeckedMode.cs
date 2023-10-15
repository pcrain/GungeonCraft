using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using UnityEngine;
using MonoMod.RuntimeDetour;
using MonoMod.Cil;
using Mono.Cecil.Cil; //Instruction

using ItemAPI;
using Dungeonator;

namespace CwaffingTheGungy
{
    public static class HeckedMode
    {
        public static bool HeckedModeEnabled = true;

        public readonly static List<int> HeckedModeGunWhiteList = new(){
            (int)Items.HegemonyRifle,
            (int)Items.Ak47,
        };

        private static Hook _EnemyAwakeHook;
        // private static Hook _EnemyShootHook;
        private static ILHook _DisablePrefireAnimationHook;
        private static ILHook _DisablePrefireStateHook;

        public static void Init()
        {
            _EnemyAwakeHook = new Hook(
                typeof(AIActor).GetMethod("Awake", BindingFlags.Public | BindingFlags.Instance),
                typeof(HeckedMode).GetMethod("OnEnemyPreAwake"));
            _DisablePrefireAnimationHook = new ILHook(
                typeof(ShootGunBehavior).GetMethod("Start", BindingFlags.Instance | BindingFlags.Public),
                DisablePrefireAnimationDuringHeckedModeIL
                );
            _DisablePrefireStateHook = new ILHook(
                typeof(ShootGunBehavior).GetMethod("ContinuousUpdate", BindingFlags.Instance | BindingFlags.Public),
                DisablePrefireStateDuringHeckedModeIL
                );
            // _EnemyShootHook = new Hook(
            //     typeof(AIShooter).GetMethod("Shoot", BindingFlags.Public | BindingFlags.Instance),
            //     typeof(HeckedMode).GetMethod("OnEnemyShoot"));
        }

        // public static void OnEnemyShoot(Action<AIShooter, string> action, AIShooter shooter, string overrideBulletName)
        // {
        //     if (shooter.aiActor.EnemyGuid == Enemies.BulletKin)
        //     {
        //         overrideBulletName = null;
        //     }
        //     action(shooter, overrideBulletName);
        // }

        // disable prefire animations in hecked mode since they mess with fire rate
        private static bool HeckedModeShouldSkipPrefireAnimationCheck(ShootGunBehavior sgb, string s)
        {
            return HeckedModeEnabled || string.IsNullOrEmpty(s);
        }

        private static bool HeckedModeShouldSkipPrefireStateCheck(bool wouldSkipAnyway)
        {
            return HeckedModeEnabled || wouldSkipAnyway;
        }

        private static void DisablePrefireStateDuringHeckedModeIL(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt<AIShooter>("get_IsPreFireComplete")))
                return; // couldn't find the appropriate hook

            // we have a brfalse immediately after us that skips the method we want to call, so just replace that with out own method
            cursor.Emit(OpCodes.Call, typeof(HeckedMode).GetMethod("HeckedModeShouldSkipPrefireStateCheck", BindingFlags.Static | BindingFlags.NonPublic)); // replace it with our own
        }

        private static void DisablePrefireAnimationDuringHeckedModeIL(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<Gun>("enemyPreFireAnimation")))
                return; // couldn't find the appropriate hook

            cursor.Remove(); // remove the string.IsNullOrEmpty check
            cursor.Emit(OpCodes.Ldarg_0); // load the player instance as arg0
            cursor.Emit(OpCodes.Call, typeof(HeckedMode).GetMethod("HeckedModeShouldSkipPrefireAnimationCheck", BindingFlags.Static | BindingFlags.NonPublic)); // replace it with our own
        }

        public static void OnEnemyPreAwake(Action<AIActor> action, AIActor enemy)
        {
            Items replacementGunId = (Items)HeckedModeGunWhiteList[HeckedModeGunWhiteList.Count-1];
            Gun replacementGun = ItemHelper.Get(replacementGunId) as Gun;
            // replacementGun.enemyPreFireAnimation = null;

            if ((enemy.EnemyGuid != Enemies.BulletKin || (enemy.aiShooter is not AIShooter shooter)))
            {
                action(enemy);
                return;
            }

            shooter.equippedGunId = (int)replacementGunId;
            shooter.customShootCooldownPeriod = 0f;
            shooter.bulletName = null;
            foreach (AttackBehaviorBase attack in shooter.behaviorSpeculator.AttackBehaviors)
            {
                if (attack is not ShootGunBehavior pewpew)
                    continue;
                // ETGModConsole.Log($"  found attack behavior with cooldown {pewpew.Cooldown}");
                pewpew.WeaponType            = WeaponType.AIShooterProjectile;
                pewpew.OverrideBulletName    = null; // must be null to allow firing normal gun projectiles
                pewpew.m_preFireTime         = 0f; // NECESSARY: some guns have custom enemy animations that prevent them from firing at their full rate

                pewpew.Cooldown              = 0f;
                pewpew.GroupCooldownVariance = 0f;
                pewpew.RespectReload         = true;
                pewpew.EmptiesClip           = true;
                pewpew.LeadAmount            = 0f; // don't let them shoot ahead of us...that's too mean for now
                pewpew.LeadChance            = 0f; // don't let them shoot ahead of us...that's too mean for now
                pewpew.TimeBetweenShots      = -1f; //0f; //replacementGun.DefaultModule.cooldownTime;
                pewpew.TimeBetweenShots      = replacementGun.DefaultModule.cooldownTime;
                pewpew.MagazineCapacity      = replacementGun.ClipCapacity;
                pewpew.ReloadSpeed           = replacementGun.reloadTime;
                pewpew.Range                 = replacementGun.DefaultModule.projectiles[0].baseData.range;
                /* Default bulletkin behavior

                "GroupCooldownVariance"        : 0.200000002980232,
                "LineOfSight"                  : true,
                "WeaponType"                   : "AIShooterProjectile",
                "OverrideBulletName"           : "default",
                "BulletScript"                 : null,
                "FixTargetDuringAttack"        : false,
                "StopDuringAttack"             : false,
                "LeadAmount"                   : 0,
                "LeadChance"                   : 1,
                "RespectReload"                : true,
                "MagazineCapacity"             : 6,
                "ReloadSpeed"                  : 2,
                "EmptiesClip"                  : false,
                "SuppressReloadAnim"           : false,
                "TimeBetweenShots"             : -1,
                "PreventTargetSwitching"       : false,
                "OverrideAnimation"            : null,
                "OverrideDirectionalAnimation" : null,
                "HideGun"                      : false,
                "UseLaserSight"                : false,
                "UseGreenLaser"                : false,
                "PreFireLaserTime"             : -1,
                "AimAtFacingDirectionWhenSafe" : false,
                "Cooldown"                     : 1.60000002384186,
                "CooldownVariance"             : 0,
                "AttackCooldown"               : 0,
                "GlobalCooldown"               : 0,
                "InitialCooldown"              : 0,
                "InitialCooldownVariance"      : 0,
                "GroupName"                    : null,
                "GroupCooldown"                : 0,
                "MinRange"                     : 0,
                "Range"                        : 12,
                "MinWallDistance"              : 0,
                "MaxEnemiesInRoom"             : 0,
                "MinHealthThreshold"           : 0,
                "MaxHealthThreshold"           : 1,
                "HealthThresholds"             : [],
                "AccumulateHealthThresholds"   : true,
                "targetAreaStyle"              : null,
                "IsBlackPhantom"               : false,
                "resetCooldownOnDamage"        : null,
                "RequiresLineOfSight"          : false,
                "MaxUsages"                    : 0,
                "$type"                        : "ShootGunBehavior"

                */
            }
            action(enemy);
        }

    }
}


