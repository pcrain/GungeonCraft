using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using ItemAPI;
using UnityEngine;
using Alexandria.Misc;

namespace CwaffingTheGungy
{
    public class PaintballGun : GunBehaviour
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Paintball Gun", "paintballgun");
            Game.Items.Rename("outdated_gun_mods:paintball_gun", "nn:paintball_gun");
            gun.gameObject.AddComponent<PaintballGun>();
            gun.SetShortDescription("Taste the Rainbow");
            gun.SetLongDescription("(shoots colored projectiles with colored goop that colors enemies...colors)");
            gun.SetupSprite(null, "paintballgun_idle_001", 8);
            gun.SetAnimationFPS(gun.shootAnimation, 14);
            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);
            gun.SetBaseMaxAmmo(300);
            gun.DefaultModule.ammoCost                  = 1;
            gun.DefaultModule.shootStyle                = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle             = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime                              = 1.1f;
            gun.DefaultModule.cooldownTime              = 0.1f;
            gun.DefaultModule.numberOfShotsInClip       = 10;
            gun.gunClass                                = GunClass.PISTOL;
            gun.quality                                 = PickupObject.ItemQuality.C;

            Projectile projectile                       = ProjectileUtility.SetupProjectile(86);
            projectile.baseData.damage                  = 7.5f;
            gun.DefaultModule.projectiles[0]            = projectile;

            PaintballColorizer paintballController      = projectile.gameObject.AddComponent<PaintballColorizer>();
            paintballController.ApplyColourToHitEnemies = true;
            paintballController.paintballGun            = true;

            ETGMod.Databases.Items.Add(gun, false, "ANY");

        }
        public override void PostProcessProjectile(Projectile projectile)
        {
            PaintballColorizer pbc =
                projectile.gameObject.GetComponent<PaintballColorizer>();

            GoopModifier goopmod           = projectile.gameObject.AddComponent<GoopModifier>();
            goopmod.SpawnGoopOnCollision   = true;
            goopmod.CollisionSpawnRadius   = 1f;
            goopmod.SpawnGoopInFlight      = true;
            goopmod.InFlightSpawnRadius    = 0.4f;
            goopmod.InFlightSpawnFrequency = 0.01f;
            goopmod.goopDefinition         = pbc.setColorAndGetGoop();

            base.PostProcessProjectile(projectile);
        }
        public PaintballGun()
        {

        }
    }

    public class PaintballColorizer : MonoBehaviour
    {
        public  bool       ApplyColourToHitEnemies;
        public  int        tintPriority;
        public  bool       paintballGun;
        public  Color      selectedColour;
        private Projectile m_projectile;
        public PaintballColorizer()
        {
            ApplyColourToHitEnemies = false;
            tintPriority            = 1;
            paintballGun            = false;
        }
        public GoopDefinition setColorAndGetGoop() {
            int selectedIndex = UnityEngine.Random.Range(0, EasyGoopDefinitions.ColorGoopColors.Count);
            selectedColour = EasyGoopDefinitions.ColorGoopColors[selectedIndex];
            return EasyGoopDefinitions.ColorGoops[selectedIndex];
        }
        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            this.m_projectile.AdjustPlayerProjectileTint(selectedColour, tintPriority);
            if (ApplyColourToHitEnemies)
                this.m_projectile.OnHitEnemy += this.OnHitEnemy;
        }
        private void OnHitEnemy(Projectile bullet, SpeculativeRigidbody enemy, bool what)
        {
            GameActorHealthEffect tint = new GameActorHealthEffect()
            {
                TintColor                = selectedColour,
                DeathTintColor           = selectedColour,
                AppliesTint              = true,
                AppliesDeathTint         = true,
                AffectsEnemies           = true,
                DamagePerSecondToEnemies = 0f,
                duration                 = 10000000,
                effectIdentifier         = "ProjectileAppliedTint",
            };
            enemy.aiActor.ApplyEffect(tint);
        }
    }
}
