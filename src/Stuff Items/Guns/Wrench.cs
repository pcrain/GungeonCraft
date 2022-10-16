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
    public class Wrench : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Wrench", "wrench");
            Game.Items.Rename("outdated_gun_mods:wrench", "nn:wrench");
            gun.gameObject.AddComponent<Wrench>();
            gun.SetShortDescription("Mod The Gun");
            gun.SetLongDescription("While appearing unremarkable, this wonky wrench is actually an artefact of great power."+"\n\nResponsible for the tear in the dimensional curtain through which new strange artefacts migrate to the Gungeon to this very day."+"\n\nGrows stronger for each esoteric artefact in your possession.");

            gun.SetupSprite(null, "wrench_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 12);
            gun.SetAnimationFPS(gun.reloadAnimation, 1);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.DefaultModule.cooldownTime = 0.5f;
            gun.muzzleFlashEffects.type = VFXPoolType.None;
            gun.DefaultModule.numberOfShotsInClip = 10;
            gun.barrelOffset.transform.localPosition = new Vector3(1.43f, 0.31f, 0f);
            gun.SetBaseMaxAmmo(400);

            //IF PROJECTILE STATS
            Projectile ifProjectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            ifProjectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(ifProjectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(ifProjectile);
            ifProjectile.baseData.damage *= 1.6f;
            ifProjectile.baseData.speed *= 1f;
            ifProjectile.baseData.range *= 10f;
            ifProjectile.SetProjectileSpriteRight("wrench_if_projectile", 12, 7, false, tk2dBaseSprite.Anchor.MiddleCenter, 12, 7);

            //ELSE PROJECTILE
            Projectile elseProjectile = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            elseProjectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(elseProjectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(elseProjectile);
            elseProjectile.baseData.damage *= 1.6f;
            elseProjectile.baseData.speed *= 1f;
            elseProjectile.baseData.range *= 10f;
            elseProjectile.SetProjectileSpriteRight("wrench_else_projectile",20, 7, false, tk2dBaseSprite.Anchor.MiddleCenter, 20, 7);

            //FLOAT PROJECTILE
            Projectile floatProjectile = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            floatProjectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(floatProjectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(floatProjectile);
            floatProjectile.baseData.damage *= 1.6f;
            floatProjectile.baseData.speed *= 1f;
            floatProjectile.baseData.range *= 10f;
            floatProjectile.SetProjectileSpriteRight("wrench_float_projectile", 19, 7, false, tk2dBaseSprite.Anchor.MiddleCenter, 19, 7);

            //BOOL PROJECTILE
            Projectile boolProjectile = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            boolProjectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(boolProjectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(boolProjectile);
            boolProjectile.baseData.damage *= 1.6f;
            boolProjectile.baseData.speed *= 1f;
            boolProjectile.baseData.range *= 10f;
            boolProjectile.SetProjectileSpriteRight("wrench_bool_projectile", 15,7, false, tk2dBaseSprite.Anchor.MiddleCenter, 15, 7);

            //INT PROJECTILE
            Projectile intProjectile = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            intProjectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(intProjectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(intProjectile);
            intProjectile.baseData.damage *= 1.6f;
            intProjectile.baseData.speed *= 1f;
            intProjectile.baseData.range *= 10f;
            intProjectile.SetProjectileSpriteRight("wrench_int_projectile", 9, 7, false, tk2dBaseSprite.Anchor.MiddleCenter, 9, 7);

            gun.DefaultModule.projectiles[0] = ifProjectile;
            gun.DefaultModule.projectiles.Add(elseProjectile);
            gun.DefaultModule.projectiles.Add(floatProjectile);
            gun.DefaultModule.projectiles.Add(boolProjectile);
            gun.DefaultModule.projectiles.Add(intProjectile);
            gun.gunClass = GunClass.SILLY;
            gun.quality = PickupObject.ItemQuality.B;
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            WrenchID = gun.PickupObjectId;
        }
        public static int WrenchID;
        private int currentItems, lastItems;
        private int currentActives, lastActives;
        private int currentGuns, lastGuns;
        private int AmountOfModdedShit;
        public override void PostProcessProjectile(Projectile projectile)
        {
            if (AmountOfModdedShit >= 1)
            {
                projectile.baseData.damage *= (0.10f * AmountOfModdedShit) + 1;
            }
            base.PostProcessProjectile(projectile);
        }

        protected override void Update()
        {
            PlayerController Owner = gun.CurrentOwner as PlayerController;
            if (Owner)
            {
                currentItems = Owner.passiveItems.Count;
                currentActives = Owner.activeItems.Count;
                currentGuns = Owner.inventory.AllGuns.Count;
                if (currentItems != lastItems || currentActives != lastActives || currentGuns != lastGuns)
                {
                    int moddedPassives = 0;
                    int moddedActives = 0;
                    int moddedGuns = 0;
                    foreach (PassiveItem item in Owner.passiveItems) { if (item.PickupObjectId > 823 || item.PickupObjectId < 0) { moddedPassives += 1; } }
                    foreach (PlayerItem active in Owner.activeItems) { if (active.PickupObjectId > 823 || active.PickupObjectId < 0) { moddedActives += 1; } }
                    foreach (Gun gun in Owner.inventory.AllGuns) { if (gun.PickupObjectId > 823 || gun.PickupObjectId < 0) { moddedGuns += 1; } }
                    AmountOfModdedShit = moddedPassives + moddedActives + moddedGuns;

                    lastItems = currentItems;
                    lastActives = currentActives;
                    lastGuns = currentGuns;
                }
            }
            base.Update();
        }
        public Wrench()
        {

        }
    }
    public class WrenchNullRefException : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Null Reference Exwrenchion", "wrenchnull");
            Game.Items.Rename("outdated_gun_mods:null_reference_exwrenchion", "nn:wrench+null_reference_exception");
            gun.gameObject.AddComponent<WrenchNullRefException>();
            gun.SetShortDescription("Mod The Gun");
            gun.SetLongDescription("i am so tired while coding this");

            gun.SetupSprite(null, "wrenchnull_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 12);
            gun.SetAnimationFPS(gun.reloadAnimation, 1);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Burst;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.OrderedGroups;
            gun.DefaultModule.orderedGroupCounts.Add(1);
            gun.DefaultModule.orderedGroupCounts.Add(1);
            gun.DefaultModule.orderedGroupCounts.Add(1);
            gun.DefaultModule.burstShotCount = 3;
            gun.DefaultModule.burstCooldownTime = 0.1f;
            gun.reloadTime = 1f;
            gun.DefaultModule.cooldownTime = 0.5f;
            gun.muzzleFlashEffects.type = VFXPoolType.None;
            gun.DefaultModule.numberOfShotsInClip = 12;
            gun.barrelOffset.transform.localPosition = new Vector3(1.43f, 0.31f, 0f);
            gun.SetBaseMaxAmmo(400);

            //IF PROJECTILE STATS
            Projectile ifProjectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            ifProjectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(ifProjectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(ifProjectile);
            ifProjectile.baseData.damage *= 1.8f;
            ifProjectile.baseData.speed *= 1f;
            ifProjectile.baseData.range *= 10f;
            ifProjectile.SetProjectileSpriteRight("wrench_null_projectile", 13, 7, false, tk2dBaseSprite.Anchor.MiddleCenter, 12, 7);

            //ELSE PROJECTILE
            Projectile elseProjectile = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            elseProjectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(elseProjectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(elseProjectile);
            elseProjectile.baseData.damage *= 1.8f;
            elseProjectile.baseData.speed *= 1f;
            elseProjectile.baseData.range *= 10f;
            elseProjectile.SetProjectileSpriteRight("wrench_reference_projectile", 36, 7, false, tk2dBaseSprite.Anchor.MiddleCenter, 30, 7);

            //FLOAT PROJECTILE
            Projectile floatProjectile = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            floatProjectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(floatProjectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(floatProjectile);
            floatProjectile.baseData.damage *= 1.8f;
            floatProjectile.baseData.speed *= 1f;
            floatProjectile.baseData.range *= 10f;
            floatProjectile.SetProjectileSpriteRight("wrench_exception_projectile", 35, 9, false, tk2dBaseSprite.Anchor.MiddleCenter, 30, 7);           

            gun.DefaultModule.projectiles[0] = ifProjectile;
            gun.DefaultModule.projectiles.Add(elseProjectile);
            gun.DefaultModule.projectiles.Add(floatProjectile);

            gun.quality = PickupObject.ItemQuality.EXCLUDED;
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            NullWrenchID = gun.PickupObjectId;
        }
        public static int NullWrenchID;
        public WrenchNullRefException()
        {

        }
    }
}
