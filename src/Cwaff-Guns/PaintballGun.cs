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

            // ColorSplash colorSplashController = projectile.gameObject.AddComponent<ColorSplash>();

            // GoopModifier goopmod         = projectile.gameObject.AddComponent<GoopModifier>();
            // goopmod.SpawnGoopOnCollision = true;
            // goopmod.CollisionSpawnRadius = 2f;
            // goopmod.SpawnGoopInFlight    = false;
            // goopmod.goopDefinition       = EasyGoopDefinitions.WaterGoop;

            ETGMod.Databases.Items.Add(gun, false, "ANY");

        }
        public override void PostProcessProjectile(Projectile projectile)
        {
            // if (projectile.ProjectilePlayerOwner())
            // {
            //   ProjectileSlashingBehaviour slash =  projectile.gameObject.AddComponent<ProjectileSlashingBehaviour>();
            //     slash.DestroyBaseAfterFirstSlash = false;
            //     slash.timeBetweenSlashes = 1;
            //     slash.SlashDamageUsesBaseProjectileDamage = true;
            //     slash.slashParameters.playerKnockbackForce = 0;
            // }

            RandomiseProjectileColourComponent rpcc =
                projectile.gameObject.GetComponent<RandomiseProjectileColourComponent>();

            if (rpcc != null)
            {
                // ETGModConsole.Log("Gooper o:");
            }

            GoopModifier goopmod         = projectile.gameObject.AddComponent<GoopModifier>();
            goopmod.SpawnGoopOnCollision = true;
            goopmod.CollisionSpawnRadius = 2f;
            goopmod.SpawnGoopInFlight    = false;
            // goopmod.goopDefinition       = EasyGoopDefinitions.WaterGoop;
            // GoopDefinition colorWater    = UnityEngine.Object.Instantiate<GoopDefinition>(EasyGoopDefinitions.WaterGoop);
            // colorWater.baseColor32       = rpcc.selectedColour;
            // goopmod.goopDefinition       = rpcc.selectedGoop;
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
        public RandomiseProjectileColourComponent()
        {
            ListOfColours = new List<Color> {
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
        public static List<Color> ListOfColours;
        public static List<GoopDefinition> ListOfGoops;
        public bool ApplyColourToHitEnemies;
        public int tintPriority;
        public bool paintballGun;
        public GoopDefinition setColorAndGetGoop() {
            this.selectedIndex = UnityEngine.Random.Range(0, ListOfColours.Count);
            return EasyGoopDefinitions.rainbowGoopDefs[this.selectedIndex];
        }
        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            ETGModConsole.Log("Selected Index is"+this.selectedIndex);
            selectedColour = ListOfColours[this.selectedIndex];
            selectedGoop   = EasyGoopDefinitions.rainbowGoopDefs[this.selectedIndex];
            m_projectile.AdjustPlayerProjectileTint(selectedColour, tintPriority);
            if (ApplyColourToHitEnemies) m_projectile.OnHitEnemy += this.OnHitEnemy;
        }
        private Projectile m_projectile;
        public Color selectedColour;
        public GoopDefinition selectedGoop;
        public int selectedIndex = -1;
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
        private void Update()
        {

        }
    } //Randomises the colour of the projectile, and can make it apply the colour to enemies.
}
