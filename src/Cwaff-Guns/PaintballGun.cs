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
            gun.SetShortDescription("The Colours, Duke!");
            gun.SetLongDescription("Small rubbery pellets loaded with lethal old-school lead paint."+"\n\nBrought to the Gungeon by an amateur artist who wished to flee his debts.");

            gun.SetupSprite(null, "paintballgun_idle_001", 8);
            gun.SetAnimationFPS(gun.shootAnimation, 14);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1.1f;
            gun.DefaultModule.cooldownTime = 0.1f;
            gun.DefaultModule.numberOfShotsInClip = 10;
            gun.SetBaseMaxAmmo(300);
            gun.gunClass = GunClass.PISTOL;

            //BULLET STATS
            Projectile projectile = ProjectileUtility.SetupProjectile(86);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage = 7.5f;
            RandomiseProjectileColourComponent paintballController = projectile.gameObject.AddComponent<RandomiseProjectileColourComponent>();
            paintballController.ApplyColourToHitEnemies = true;
            paintballController.paintballGun = true;
            gun.quality = PickupObject.ItemQuality.C;

            ETGMod.Databases.Items.Add(gun, false, "ANY");

        }
        public override void PostProcessProjectile(Projectile projectile)
        {
            RandomiseProjectileColourComponent rpcc =
                projectile.gameObject.GetComponent<RandomiseProjectileColourComponent>();

            GoopModifier goopmod         = projectile.gameObject.AddComponent<GoopModifier>();
            goopmod.SpawnGoopOnCollision = true;
            goopmod.CollisionSpawnRadius = 2f;
            goopmod.SpawnGoopInFlight    = false;
            goopmod.goopDefinition       = rpcc.setColorAndGetGoop();

            base.PostProcessProjectile(projectile);
        }
        public PaintballGun()
        {

        }
    }

    public class ColorSplash : MonoBehaviour
    {
        public ColorSplash()
        {
        }
        public void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            // if (this.m_projectile)
            // {
            //     this.m_projectile.specRigidbody.OnPreRigidbodyCollision += this.PrepareToExplode;
            //     // this.m_projectile.OnDestruction += this.Explode;
            // }
        }

        // private void PrepareToExplode(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
        // {
        //     GoopModifier goopmod         = m_projectile.gameObject.AddComponent<GoopModifier>();
        //     goopmod.SpawnGoopOnCollision = true;
        //     goopmod.CollisionSpawnRadius = 2f;
        //     goopmod.SpawnGoopInFlight    = false;
        //     goopmod.goopDefinition       = EasyGoopDefinitions.WaterGoop;
        // }

        // private void Explode(Projectile me)
        // {
        //     ETGModConsole.Log("Gooper o:");
        //     var ddgm = DeadlyDeadlyGoopManager.GetGoopManagerForGoopType(EasyGoopDefinitions.WaterGoop);
        //     ddgm.AddGoopCircle(m_projectile.sprite.WorldCenter, 4f);
        // }
        private Projectile m_projectile;
    }

    public class RandomiseProjectileColourComponent : MonoBehaviour
    {
        public static List<Color>          ListOfColors;
        public static List<GoopDefinition> ListOfGoops;
        public        bool                 ApplyColourToHitEnemies;
        public        int                  tintPriority;
        public        bool                 paintballGun;
        public        Color                selectedColour;
        private       Projectile           m_projectile;
        public RandomiseProjectileColourComponent()
        {
            ListOfColors = new List<Color> {
                ExtendedColours.pink,
                Color.red,
                ExtendedColours.orange,
                Color.yellow,
                Color.green,
                Color.blue,
                ExtendedColours.purple,
                Color.cyan,
            };
            ListOfGoops = new List<GoopDefinition> {
                EasyGoopDefinitions.PinkWater,
                EasyGoopDefinitions.RedWater,
                EasyGoopDefinitions.OrangeWater,
                EasyGoopDefinitions.YellowWater,
                EasyGoopDefinitions.GreenWater,
                EasyGoopDefinitions.BlueWater,
                EasyGoopDefinitions.PurpleWater,
                EasyGoopDefinitions.CyanWater,
            };
            ApplyColourToHitEnemies = false;
            tintPriority            = 1;
            paintballGun            = false;
        }
        public GoopDefinition setColorAndGetGoop() {
            int selectedIndex = UnityEngine.Random.Range(0, ListOfColors.Count);
            selectedColour = ListOfColors[selectedIndex];
            return ListOfGoops[selectedIndex];
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
    } //Randomises the colour of the projectile, and can make it apply the colour to enemies.
}
