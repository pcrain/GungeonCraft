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
    public class PaintballCannon : AdvancedGunBehavior
    {
        public static string ItemName         = "Paintball Cannon";
        public static string SpriteName       = "paintball_cannon";
        public static string ProjectileName   = "86"; //marine sidearm
        public static string ShortDescription = "The T is Silent";
        public static string LongDescription  = "(shoots colored projectiles with colored goop that colors enemies...colors)";

        public static void Add()
        {
            Gun gun = Lazy.SetupGun(ItemName, SpriteName, ProjectileName, ShortDescription, LongDescription);
                // gun.gunSwitchGroup                       = (ItemHelper.Get(Items.TShirtCannon) as Gun).gunSwitchGroup;
                gun.gunSwitchGroup                       = (ItemHelper.Get(Items.GunslingersAshes) as Gun).gunSwitchGroup; // silent reload
                gun.DefaultModule.ammoCost               = 1;
                gun.DefaultModule.shootStyle             = ProjectileModule.ShootStyle.SemiAutomatic;
                gun.DefaultModule.sequenceStyle          = ProjectileModule.ProjectileSequenceStyle.Random;
                gun.reloadTime                           = 0.9f;
                gun.DefaultModule.cooldownTime           = 0.18f;
                gun.DefaultModule.numberOfShotsInClip    = 12;
                gun.gunClass                             = GunClass.PISTOL;
                gun.quality                              = PickupObject.ItemQuality.C;
                gun.barrelOffset.transform.localPosition = new Vector3(1.625f, 0.5f, 0f); // should match "Casing" in JSON file
                gun.SetFireAudio("paintball_shoot_sound");
                gun.SetBaseMaxAmmo(300);
                gun.SetAnimationFPS(gun.shootAnimation, 14);
                gun.SetAnimationFPS(gun.reloadAnimation, 4);

            var comp = gun.gameObject.AddComponent<PaintballCannon>();
                comp.SetReloadAudio("paintball_reload_sound");

            Projectile projectile = Lazy.PrefabProjectileFromGun(gun);
                projectile.baseData.damage = 7f;

            PaintballColorizer paintballController = projectile.gameObject.AddComponent<PaintballColorizer>();
                paintballController.ApplyColourToHitEnemies = true;
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
        public  Color      selectedColour;
        private Projectile m_projectile;
        public PaintballColorizer()
        {
            ApplyColourToHitEnemies = false;
            tintPriority            = 1;
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
                effectIdentifier         = "Paintballed",
            };
            enemy.aiActor.RemoveEffect("Paintballed");
            enemy.aiActor.ApplyEffect(tint);
        }
    }
}
