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

namespace NevernamedsItems
{

    public class GreekFire : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Greek Fire", "greekfire");
            Game.Items.Rename("outdated_gun_mods:greek_fire", "nn:greek_fire");
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(37) as Gun).gunSwitchGroup;
            gun.SetShortDescription("Fire of Man");
            gun.SetLongDescription("Used by ancient civilisations as a primitive flamethrower."+"\n\nThe potent chemical mixture used to create the flames makes it remarkably dangerous.");
            gun.SetupSprite(null, "greekfire_idle_001", 8);
            gun.SetAnimationFPS(gun.shootAnimation, 16);
            gun.SetAnimationFPS(gun.chargeAnimation, 3);

            for (int i = 0; i < 4; i++)
            {
                gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(698) as Gun, true, false);
            }

            //GUN STATS
            foreach (ProjectileModule mod in gun.Volley.projectiles)
            {
                mod.ammoCost = 1;
                mod.shootStyle = ProjectileModule.ShootStyle.Charged;
                mod.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
                mod.cooldownTime = 1f;
                mod.angleVariance = 20f;
                mod.numberOfShotsInClip = 1;
                Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(mod.projectiles[0]);
                mod.projectiles[0] = projectile;
                projectile.gameObject.SetActive(false);
                FakePrefab.MarkAsFakePrefab(projectile.gameObject);
                UnityEngine.Object.DontDestroyOnLoad(projectile);
                projectile.baseData.damage *= 5f;
                projectile.AdditionalScaleMultiplier *= 2f;
                projectile.baseData.range *= 0.5f;
                if (mod != gun.DefaultModule) { mod.ammoCost = 0; }
                projectile.transform.parent = gun.barrelOffset;
                PrefabStatusEffectsToApply statusE =  projectile.gameObject.AddComponent<PrefabStatusEffectsToApply>();
                statusE.effects = new List<GameActorEffect>() { StaticStatusEffects.greenFireEffect };
                if (projectile.gameObject.GetComponent<GoopModifier>())
                {
                    Destroy(projectile.gameObject.GetComponent<GoopModifier>());
                }
                GoopModifier goopSpawned = projectile.gameObject.AddComponent<GoopModifier>();
                goopSpawned.SpawnGoopOnCollision = true;
                goopSpawned.InFlightSpawnFrequency = 0.05f;
                goopSpawned.InFlightSpawnRadius = 0.5f;
                goopSpawned.SpawnGoopInFlight = true;
                goopSpawned.CollisionSpawnRadius = 3f;
                goopSpawned.goopDefinition = EasyGoopDefinitions.GreenFireDef;
                PierceProjModifier orAddComponent = projectile.gameObject.GetOrAddComponent<PierceProjModifier>();
                orAddComponent.penetratesBreakables = true;
                orAddComponent.penetration++;
                ProjectileModule.ChargeProjectile chargeProj = new ProjectileModule.ChargeProjectile
                {
                    Projectile = projectile,
                    ChargeTime = 1f,
                };
                mod.chargeProjectiles = new List<ProjectileModule.ChargeProjectile> { chargeProj };
            }
            gun.reloadTime = 1f;
            gun.SetBaseMaxAmmo(65);
            gun.quality = PickupObject.ItemQuality.B;
            gun.gunClass = GunClass.FIRE;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.chargeAnimation).wrapMode = tk2dSpriteAnimationClip.WrapMode.LoopSection;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.chargeAnimation).loopStart = 1;

            gun.encounterTrackable.EncounterGuid = "this is Greek Fire";
            ETGMod.Databases.Items.Add(gun, null, "ANY");
            gun.barrelOffset.transform.localPosition = new Vector3(1.37f, 0.37f, 0f);
            GreekFireID = gun.PickupObjectId;
        }
        public static int GreekFireID;
        public override void PostProcessProjectile(Projectile projectile)
        {
            base.PostProcessProjectile(projectile);
        }
        public GreekFire()
        {

        }
    }
}