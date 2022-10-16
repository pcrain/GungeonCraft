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

    public class MastersGun : AdvancedGunBehavior
    {


        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Master's Gun", "mastersgun");
            Game.Items.Rename("outdated_gun_mods:master's_gun", "nn:masters_gun");
            gun.gameObject.AddComponent<MastersGun>();
            gun.SetShortDescription("Firing On All Cylinders");
            gun.SetLongDescription("A humongous firearm, created by the Gungeon's Master to fire the legendary master rounds, though he never truly finished it." + "\n\nAfter it's recent rediscovery, the Blacksmith managed to finish the spectacular weapon, and even forged Master-Sized bullet replicas for ammo." + "\n\nNothing can beat the gun's original purpose though, so getting your grubby hands on some master rounds would be good.");

            gun.SetupSprite(null, "mastersgun_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 11);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(37) as Gun).muzzleFlashEffects;
            gun.DefaultModule.cooldownTime = 0.4f;
            gun.DefaultModule.numberOfShotsInClip = 6;
            gun.barrelOffset.transform.localPosition = new Vector3(3.62f, 1.81f, 0f);
            gun.SetBaseMaxAmmo(50);
            gun.gunClass = GunClass.PISTOL;
            //DEFAULT BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage *= 10f;
            projectile.ignoreDamageCaps = true;
            projectile.baseData.speed *= 1f;
            projectile.pierceMinorBreakables = true;
            PierceProjModifier orAddComponent = projectile.gameObject.GetOrAddComponent<PierceProjModifier>();
            orAddComponent.penetratesBreakables = true;
            orAddComponent.penetration++;
            projectile.SetProjectileSpriteRight("mastersgun_projectile", 27, 12, false, tk2dBaseSprite.Anchor.MiddleCenter, 27, 12);
            projectile.transform.parent = gun.barrelOffset;

            //KEEP BULLET STATS
            keepProjectile = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            keepProjectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(keepProjectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(keepProjectile);
            keepProjectile.baseData.damage *= 16f;
            keepProjectile.ignoreDamageCaps = true;
            keepProjectile.baseData.speed *= 1f;
            keepProjectile.pierceMinorBreakables = true;
            PierceProjModifier keepComponent = keepProjectile.gameObject.GetOrAddComponent<PierceProjModifier>();
            keepComponent.penetratesBreakables = true;
            keepComponent.penetration++;
            BounceProjModifier Bouncing = keepProjectile.gameObject.GetOrAddComponent<BounceProjModifier>();
            Bouncing.numberOfBounces = 5;
            keepProjectile.SetProjectileSpriteRight("mastersgun_keep_projectile", 27, 12, false, tk2dBaseSprite.Anchor.MiddleCenter, 27, 12);
            keepProjectile.transform.parent = gun.barrelOffset;
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(37) as Gun).gunSwitchGroup;

            //PROPER BULLET STATS
            properProjectile = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            properProjectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(properProjectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(properProjectile);
            properProjectile.baseData.damage *= 16f;
            properProjectile.ignoreDamageCaps = true;
            properProjectile.baseData.speed *= 1f;
            properProjectile.pierceMinorBreakables = true;
            PierceProjModifier properComponent = properProjectile.gameObject.GetOrAddComponent<PierceProjModifier>();
            properComponent.penetratesBreakables = true;
            properComponent.penetration++;
            ApplyLockdownBulletBehaviour properLockdown = properProjectile.gameObject.GetOrAddComponent<ApplyLockdownBulletBehaviour>();
            properLockdown.duration = 6;

            properProjectile.SetProjectileSpriteRight("mastersgun_proper_projectile", 27, 12, false, tk2dBaseSprite.Anchor.MiddleCenter, 27, 12);
            properProjectile.transform.parent = gun.barrelOffset;

            //MINES BULLET STATS
            minesProjectile = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            minesProjectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(minesProjectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(minesProjectile);
            minesProjectile.baseData.damage *= 16f;
            minesProjectile.ignoreDamageCaps = true;
            minesProjectile.baseData.speed *= 1f;
            minesProjectile.pierceMinorBreakables = true;
            PierceProjModifier minesComponent = minesProjectile.gameObject.GetOrAddComponent<PierceProjModifier>();
            minesComponent.penetratesBreakables = true;
            minesComponent.penetration++;
            ExtremelySimplePoisonBulletBehaviour minesPoisoning = minesProjectile.gameObject.GetOrAddComponent<ExtremelySimplePoisonBulletBehaviour>();
            minesPoisoning.procChance = 1;
            minesPoisoning.useSpecialTint = false;
            minesProjectile.SetProjectileSpriteRight("mastersgun_mines_projectile", 27, 12, false, tk2dBaseSprite.Anchor.MiddleCenter, 27, 12);
            minesProjectile.transform.parent = gun.barrelOffset;

            //HOLLOW BULLET STATS
            hollowProjectile = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            hollowProjectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(hollowProjectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(hollowProjectile);
            hollowProjectile.baseData.damage *= 16f;
            hollowProjectile.ignoreDamageCaps = true;
            hollowProjectile.baseData.speed *= 1f;
            hollowProjectile.pierceMinorBreakables = true;
            PierceProjModifier hollowComponent = hollowProjectile.gameObject.GetOrAddComponent<PierceProjModifier>();
            hollowComponent.penetratesBreakables = true;
            hollowComponent.penetration++;
            SimpleFreezingBulletBehaviour freezing = hollowProjectile.gameObject.GetOrAddComponent<SimpleFreezingBulletBehaviour>();
            freezing.procChance = 1;
            freezing.useSpecialTint = false;
            freezing.freezeAmount = 150;
            freezing.freezeAmountForBosses = 100;
            hollowProjectile.SetProjectileSpriteRight("mastersgun_hollow_projectile", 27, 12, false, tk2dBaseSprite.Anchor.MiddleCenter, 27, 12);
            hollowProjectile.transform.parent = gun.barrelOffset;

            //FORGE BULLET STATS
            forgeProjectile = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            forgeProjectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(forgeProjectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(forgeProjectile);
            forgeProjectile.baseData.damage *= 20f;
            forgeProjectile.ignoreDamageCaps = true;
            forgeProjectile.baseData.speed *= 1f;
            forgeProjectile.pierceMinorBreakables = true;
            PierceProjModifier forgeComponent = forgeProjectile.gameObject.GetOrAddComponent<PierceProjModifier>();
            forgeComponent.penetratesBreakables = true;
            forgeComponent.penetration++;
            ExtremelySimpleStatusEffectBulletBehaviour burning = forgeProjectile.gameObject.GetOrAddComponent<ExtremelySimpleStatusEffectBulletBehaviour>();
            burning.onFiredProcChance = 1;
            burning.usesFireEffect = true;
            burning.fireEffect = StaticStatusEffects.hotLeadEffect;
            burning.useSpecialTint = false;
            forgeProjectile.SetProjectileSpriteRight("mastersgun_forge_projectile", 33, 18, false, tk2dBaseSprite.Anchor.MiddleLeft, 27, 12);
            forgeProjectile.transform.parent = gun.barrelOffset;

            gun.quality = PickupObject.ItemQuality.S; //S
            gun.encounterTrackable.EncounterGuid = "this is the Master's Gun";
            ETGMod.Databases.Items.Add(gun, null, "ANY");

        }
        public static Projectile keepProjectile;
        public static Projectile properProjectile;
        public static Projectile minesProjectile;
        public static Projectile hollowProjectile;
        public static Projectile forgeProjectile;

        public override Projectile OnPreFireProjectileModifier(Gun gun, Projectile projectile, ProjectileModule mod)
        {
            try
            {
                PlayerController playerController = this.gun.CurrentOwner as PlayerController;
                if (playerController)
                {

                    int roundType = UnityEngine.Random.Range(1, 6);
                    switch (roundType)
                    {
                        case 1: //Keep
                            if (playerController.HasPickupID(469))
                            {
                                return (keepProjectile);
                            }
                            break;
                        case 2: //Gungeon Proper
                            if (playerController.HasPickupID(471))
                            {
                                return (properProjectile);
                            }
                            break;
                        case 3: //Mines
                            if (playerController.HasPickupID(468))
                            {
                                return (minesProjectile);
                            }
                            break;
                        case 4: //Hollow
                            if (playerController.HasPickupID(470))
                            {
                                return (hollowProjectile);
                            }
                            break;
                        case 5: //Forge
                            if (playerController.HasPickupID(467))
                            {
                                return (forgeProjectile);
                            }
                            break;
                    }
                }
                return (projectile);
            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message);
                ETGModConsole.Log(e.StackTrace);
                return (projectile);
            }
        }
        public override void OnPostFired(PlayerController player, Gun gun)
        {
            gun.PreventNormalFireAudio = true;
            AkSoundEngine.PostEvent("Play_WPN_seriouscannon_shot_01", gameObject);
        }
        public MastersGun()
        {

        }
    }
}
