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

namespace NevernamedsItems
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
            if (projectile && projectile.ProjectilePlayerOwner() && projectile.ProjectilePlayerOwner().PlayerHasActiveSynergy("Paint It Black"))
            {
                projectile.baseData.damage *= 1.2f;
                projectile.BossDamageMultiplier *= 1.1f;
            }
            base.PostProcessProjectile(projectile);
        }
        public override void OnPostFired(PlayerController player, Gun gun)
        {
            gun.PreventNormalFireAudio = true;
            AkSoundEngine.PostEvent("Play_WPN_smileyrevolver_shot_01", gameObject);
        }
        public PaintballGun()
        {

        }
    }  
}
