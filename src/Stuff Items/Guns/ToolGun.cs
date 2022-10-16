using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using ItemAPI;
using MonoMod.RuntimeDetour;
using System.Reflection;

namespace NevernamedsItems
{
    public class ToolGun : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Tool Gun", "toolgun");
            Game.Items.Rename("outdated_gun_mods:tool_gun", "nn:tool_gun");
            gun.gameObject.AddComponent<ToolGun>();
            gun.SetShortDescription("sv_cheats 1");
            gun.SetLongDescription("Pressing reload with a full clip cycles firing modes."+"\n\nAn incredibly advanced piece of technology capable of manipulating reality around you. Used almost entirely for practical jokes.");

            gun.SetupSprite(null, "toolgun_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 12);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(153) as Gun).gunSwitchGroup;
            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(153) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 0.8f;
            gun.DefaultModule.cooldownTime = 0.11f;
            gun.DefaultModule.numberOfShotsInClip = 10;
            gun.DefaultModule.angleVariance = 0;
            gun.barrelOffset.transform.localPosition = new Vector3(1.31f, 0.62f, 0f);
            gun.SetBaseMaxAmmo(200);
            gun.ammo = 200;
            gun.gunClass = GunClass.PISTOL;

            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage *= 1.5f;
            projectile.baseData.speed *= 2f;
            projectile.baseData.range *= 2f;
            projectile.AdditionalScaleMultiplier *= 0.3f;
            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("Toolgun Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/toolgun_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/toolgun_clipempty");
            gun.quality = PickupObject.ItemQuality.S; //S
            ETGMod.Databases.Items.Add(gun, null, "ANY");
            ToolGunID = gun.PickupObjectId;
        }
        public override void PostProcessProjectile(Projectile projectile)
        {
            base.PostProcessProjectile(projectile);
            switch (currentMode)
            {
                case 1: //Increase
                    EnemyScaleUpdaterMod scaleIncrease = projectile.gameObject.GetOrAddComponent<EnemyScaleUpdaterMod>();
                    scaleIncrease.targetScale = new Vector2(1.25f, 1.25f);
                    scaleIncrease.multiplyExisting = true;
                    scaleIncrease.addScaleEffectsToEnemy = true;
                    break;
                case 2: //Decrease
                    EnemyScaleUpdaterMod scaleDecrease = projectile.gameObject.GetOrAddComponent<EnemyScaleUpdaterMod>();
                    scaleDecrease.targetScale = new Vector2(0.75f, 0.75f);
                    scaleDecrease.multiplyExisting = true;
                    scaleDecrease.addScaleEffectsToEnemy = true;
                    break;
                case 3: //ApplyStatus
                    StatusEffectBulletMod effectMod = projectile.gameObject.AddComponent<StatusEffectBulletMod>();
                    effectMod.pickRandom = true;
                    effectMod.datasToApply.AddRange(new List<StatusEffectBulletMod.StatusData>()
                    {
                        new StatusEffectBulletMod.StatusData()
                        {
                            effect = StaticStatusEffects.chaosBulletsFreeze,
                            applyChance = 1,
                            applyTint = false,
                        },
                        new StatusEffectBulletMod.StatusData()
                        {
                            effect = StaticStatusEffects.charmingRoundsEffect,
                            applyChance = 1,
                            applyTint = false,
                        },
                        new StatusEffectBulletMod.StatusData()
                        {
                            effect = StaticStatusEffects.greenFireEffect,
                            applyChance = 1,
                            applyTint = false,
                        },
                        new StatusEffectBulletMod.StatusData()
                        {
                            effect = StaticStatusEffects.StandardPlagueEffect,
                            applyChance = 1,
                            applyTint = false,
                        },
                        new StatusEffectBulletMod.StatusData()
                        {
                            effect = StaticStatusEffects.tripleCrossbowSlowEffect,
                            applyChance = 1,
                            applyTint = false,
                        },
                        new StatusEffectBulletMod.StatusData()
                        {
                            effect = StaticStatusEffects.hotLeadEffect,
                            applyChance = 1,
                            applyTint = false,
                        },
                        new StatusEffectBulletMod.StatusData()
                        {
                            effect = StaticStatusEffects.irradiatedLeadEffect,
                            applyChance = 1,
                            applyTint = false,
                        },
                        new StatusEffectBulletMod.StatusData()
                        {
                            effect = StatusEffectHelper.GenerateLockdown(4),
                            applyChance = 1,
                            applyTint = false,
                        },
                    });
                    break;
                case 4: //SpawnKin
                    SpawnEnemyOnDestructionMod kinSpawn = projectile.gameObject.GetOrAddComponent<SpawnEnemyOnDestructionMod>();
                    kinSpawn.pickRandom = true;
                    kinSpawn.EnemiesToSpawn.AddRange(new List<string>()
                    {
                        EnemyGuidDatabase.Entries["bullet_kin"],
                        EnemyGuidDatabase.Entries["mutant_bullet_kin"],
                        EnemyGuidDatabase.Entries["cardinal"],
                        EnemyGuidDatabase.Entries["shroomer"],
                        EnemyGuidDatabase.Entries["ashen_bullet_kin"],
                        EnemyGuidDatabase.Entries["fallen_bullet_kin"],
                        EnemyGuidDatabase.Entries["ak47_bullet_kin"],
                        EnemyGuidDatabase.Entries["bandana_bullet_kin"],
                        EnemyGuidDatabase.Entries["veteran_bullet_kin"],
                        EnemyGuidDatabase.Entries["treadnaughts_bullet_kin"],
                        EnemyGuidDatabase.Entries["brollet"],
                        EnemyGuidDatabase.Entries["skullet"],
                        EnemyGuidDatabase.Entries["skullmet"],
                        EnemyGuidDatabase.Entries["gummy_spent"],
                        EnemyGuidDatabase.Entries["red_shotgun_kin"],
                        EnemyGuidDatabase.Entries["blue_shotgun_kin"],
                        EnemyGuidDatabase.Entries["veteran_shotgun_kin"],
                        EnemyGuidDatabase.Entries["mutant_shotgun_kin"],
                        EnemyGuidDatabase.Entries["executioner"],
                        EnemyGuidDatabase.Entries["ashen_shotgun_kin"],
                        EnemyGuidDatabase.Entries["bullat"],
                        EnemyGuidDatabase.Entries["shotgat"],
                        EnemyGuidDatabase.Entries["grenat"],
                        EnemyGuidDatabase.Entries["spirat"],
                        EnemyGuidDatabase.Entries["grenade_kin"],
                        EnemyGuidDatabase.Entries["dynamite_kin"],
                        EnemyGuidDatabase.Entries["m80_kin"],
                        EnemyGuidDatabase.Entries["tazie"],
                        EnemyGuidDatabase.Entries["rubber_kin"],
                        EnemyGuidDatabase.Entries["sniper_shell"],
                        EnemyGuidDatabase.Entries["professional"],
                    });
                    break;
                case 5: //SpawnTable
                    SpawnGameObjectOnDestructionMod tableSpawn = projectile.gameObject.GetOrAddComponent<SpawnGameObjectOnDestructionMod>();
                    tableSpawn.objectsToPickFrom.AddRange(new List<GameObject>() {
                        EasyPlaceableObjects.TableVertical,
                        EasyPlaceableObjects.TableHorizontal,
                        EasyPlaceableObjects.TableHorizontalStone,
                        EasyPlaceableObjects.TableVerticalStone,
                        EasyPlaceableObjects.FoldingTable,
                    });
                    break;
                case 6: //SpawnBarrel
                    SpawnGameObjectOnDestructionMod barrelSpawn = projectile.gameObject.GetOrAddComponent<SpawnGameObjectOnDestructionMod>();
                    barrelSpawn.objectsToPickFrom.AddRange(new List<GameObject>() {
                        EasyPlaceableObjects.ExplosiveBarrel,
                        EasyPlaceableObjects.MetalExplosiveBarrel,
                        EasyPlaceableObjects.OilBarrel,
                        EasyPlaceableObjects.PoisonBarrel,
                        EasyPlaceableObjects.WaterBarrel
                    });
                    break;
                case 7: //Erase
                    EraseEnemyBehav erase = projectile.gameObject.GetOrAddComponent<EraseEnemyBehav>();
                    erase.doSparks = true;
                    break;
            }
        }
        protected override void Update()
        {
            base.Update();
            if (gun.DefaultModule.ammoCost != overrideAmmoConsumption)
            {
                gun.DefaultModule.ammoCost = overrideAmmoConsumption;
            }
        }
        public override void OnReloadPressed(PlayerController player, Gun gun, bool bSOMETHING)
        {
            base.OnReloadPressed(player, gun, bSOMETHING);
            if ((gun.ClipCapacity == gun.ClipShotsRemaining) || (gun.CurrentAmmo == gun.ClipShotsRemaining))
            {
                if (currentMode == 7)
                {
                    currentMode = 1;
                    overrideAmmoConsumption = ModeAmmoCosts[currentMode];
                    VFXToolbox.DoStringSquirt(ModeNames[1], player.transform.position, Color.red);
                }
                else
                {
                    currentMode++;
                    overrideAmmoConsumption = ModeAmmoCosts[currentMode];
                    VFXToolbox.DoStringSquirt(ModeNames[currentMode], player.transform.position, Color.red);
                }
            }
        }
        int overrideAmmoConsumption = 1;
        int currentMode = 1;
        // INCREASE, 1
        // DECREASE, 2
        // APPLYSTATUS, 3
        // SPAWNKIN, 4
        // SPAWNTABLE, 5
        //  SPAWNBARREL, 6
        // DELETE,     7
        Dictionary<int, string> ModeNames = new Dictionary<int, string>()
        {
            {1, "Increase"},
            {2, "Decrease"},
            {3, "Apply Status"},
            {4, "Spawn: Kin"},
            {5, "Spawn: Table"},
            {6, "Spawn: Barrel"},
            {7, "Erase"},
        };
        Dictionary<int, int> ModeAmmoCosts = new Dictionary<int, int>()
        {
            {1, 1},
            {2, 2},
            {3, 1},
            {4, 5},
            {5, 4},
            {6, 3},
            {7, 25},
        };

        public static int ToolGunID;
        public ToolGun()
        {

        }
    }
}
