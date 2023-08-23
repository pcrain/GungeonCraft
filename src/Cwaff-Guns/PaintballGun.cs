using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using UnityEngine;
using MonoMod.RuntimeDetour;

using Gungeon;
using Dungeonator;
using Alexandria.ItemAPI;
using Alexandria.Misc;

namespace CwaffingTheGungy
{
    public class PaintballGun : GunBehaviour
    {
        public static string ItemName         = "Paintball Gun";
        public static string SpriteName       = "paintballgun";
        public static string ProjectileName   = "86"; //marine sidearm
        public static string ShortDescription = "Taste the Rainbow";
        public static string LongDescription  = "(shoots colored projectiles with colored goop that colors enemies...colors)";

        public static void Add()
        {
            Gun gun = Lazy.SetupGun(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription);
            var comp = gun.gameObject.AddComponent<PaintballGun>();

            gun.DefaultModule.ammoCost                  = 1;
            gun.DefaultModule.shootStyle                = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle             = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime                              = 1.1f;
            gun.DefaultModule.cooldownTime              = 0.1f;
            gun.DefaultModule.numberOfShotsInClip       = 10;
            gun.gunClass                                = GunClass.PISTOL;
            gun.quality                                 = PickupObject.ItemQuality.C;
            gun.SetBaseMaxAmmo(300);
            gun.SetAnimationFPS(gun.shootAnimation, 14);

            Projectile projectile                       = Lazy.PrefabProjectileFromGun(gun);

            PaintballColorizer paintballController      = projectile.gameObject.AddComponent<PaintballColorizer>();
            paintballController.ApplyColourToHitEnemies = true;
            paintballController.paintballGun            = true;
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
