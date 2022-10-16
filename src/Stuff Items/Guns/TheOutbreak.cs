using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using ItemAPI;
using SaveAPI;

namespace NevernamedsItems
{
    public class TheOutbreak : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("The Outbreak", "theoutbreak");
            Game.Items.Rename("outdated_gun_mods:the_outbreak", "nn:the_outbreak");
            gun.gameObject.AddComponent<TheOutbreak>();
            gun.SetShortDescription("Epidemical!");
            gun.SetLongDescription("A terrifying piece of bioweaponry. The final shot is a highly concentrated gel projectile containing a virulent load.");
            gun.SetupSprite(null, "theoutbreak_idle_001", 8);
            gun.SetAnimationFPS(gun.shootAnimation, 20);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Automatic;
            gun.DefaultModule.angleVariance = 6.5f;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1.5f;
            gun.DefaultModule.cooldownTime = 0.15f;
            gun.DefaultModule.numberOfShotsInClip = 13;
            gun.barrelOffset.transform.localPosition = new Vector3(2.43f, 0.81f, 0f);
            gun.SetBaseMaxAmmo(330);
            gun.gunClass = GunClass.POISON;


            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.speed *= 1.1f;
            projectile.baseData.damage = 6f;
            projectile.SetProjectileSpriteRight("theoutbreak_proj", 10, 10, true, tk2dBaseSprite.Anchor.MiddleCenter, 9, 9);
            projectile.hitEffects.alwaysUseMidair = true;
            projectile.hitEffects.overrideMidairDeathVFX = RainbowGuonStone.GreyGuonTransitionVFX;

            projectile.transform.parent = gun.barrelOffset;

            //END OF CLIP GLOB
            Projectile projectile2 = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            projectile2.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile2.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile2);
            projectile2.baseData.speed *= 0.8f;
            projectile2.baseData.damage = 14.1414f;
            projectile2.baseData.range *= 2;
            PrefabStatusEffectsToApply statusE = projectile2.gameObject.AddComponent<PrefabStatusEffectsToApply>();
            statusE.effects = new List<GameActorEffect>() { StaticStatusEffects.StandardPlagueEffect };

            GoopModifier goopmod = projectile2.gameObject.AddComponent<GoopModifier>();
            goopmod.SpawnGoopOnCollision = true;
            goopmod.CollisionSpawnRadius = 4f;
            goopmod.SpawnGoopInFlight = false;
            goopmod.goopDefinition = EasyGoopDefinitions.PlagueGoop;

            projectile2.hitEffects.alwaysUseMidair = true;
            projectile2.hitEffects.overrideMidairDeathVFX = RainbowGuonStone.RedGuonTransitionVFX;
            projectile2.AnimateProjectile(new List<string> {
                "theoutbreakfinalproj_1",
                "theoutbreakfinalproj_2",
                "theoutbreakfinalproj_1",
                "theoutbreakfinalproj_3",
            }, 12, true, new List<IntVector2> {
                new IntVector2(13, 8), //1
                new IntVector2(11, 10), //2            
                new IntVector2(13, 8), //3
                new IntVector2(10, 11), //4
            }, AnimateBullet.ConstructListOfSameValues(false, 4), AnimateBullet.ConstructListOfSameValues(tk2dBaseSprite.Anchor.MiddleCenter, 4), AnimateBullet.ConstructListOfSameValues(true, 4), AnimateBullet.ConstructListOfSameValues(false, 4),
            AnimateBullet.ConstructListOfSameValues<Vector3?>(null, 4), AnimateBullet.ConstructListOfSameValues<IntVector2?>(null, 4), AnimateBullet.ConstructListOfSameValues<IntVector2?>(null, 4), AnimateBullet.ConstructListOfSameValues<Projectile>(null, 4));

            gun.DefaultModule.usesOptionalFinalProjectile = true;
            gun.DefaultModule.finalProjectile = projectile2;
            gun.DefaultModule.numberOfFinalProjectiles = 1;
           // gun.DefaultModule.finalCustomAmmoType = gun.DefaultModule.customAmmoType;
           // gun.DefaultModule.finalAmmoType = gun.DefaultModule.ammoType;
           // gun.finalMuzzleFlashEffects = gun.muzzleFlashEffects;

            gun.quality = PickupObject.ItemQuality.A;
            ETGMod.Databases.Items.Add(gun, null, "ANY");
            TheOutbreakID = gun.PickupObjectId;

            gun.SetupUnlockOnCustomFlag(CustomDungeonFlags.PURCHASED_THEOUTBREAK, true);
            gun.AddItemToDougMetaShop(45);
            gun.AddToSubShop(ItemBuilder.ShopType.Goopton);

        }
        public static int TheOutbreakID;
        public override void PostProcessProjectile(Projectile projectile)
        {
            base.PostProcessProjectile(projectile);
            PlayerController player = gun.CurrentOwner as PlayerController;
            if (player.PlayerHasActiveSynergy("Toxic Avenger"))
            {

            }
        }
        public TheOutbreak()
        {

        }
    }
}