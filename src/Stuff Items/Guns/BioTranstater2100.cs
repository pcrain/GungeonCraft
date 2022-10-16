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

    public class BioTranstater2100 : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Bio-Transtater 2100", "biotranstater2100");
            Game.Items.Rename("outdated_gun_mods:biotranstater_2100", "nn:bio_transtater_2100");
            var behav = gun.gameObject.AddComponent<BioTranstater2100>();
            behav.overrideNormalFireAudio = "Play_WPN_dl45heavylaser_shot_01";
            behav.preventNormalFireAudio = true;
            gun.SetShortDescription("Don't Get Used To Yourself");
            gun.SetLongDescription("A highly advanced piece of alien technology developed by a race of cephalopodal beings native to a far off world."+"\n\nDeconstructs organisms on a cellular level, and rearranges them into something else."+"\nThis process is incredibly painful.");

            gun.SetupSprite(null, "biotranstater2100_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 10);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(90) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Automatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1.5f;
            gun.muzzleFlashEffects.type = VFXPoolType.None;
            gun.DefaultModule.cooldownTime = 0.25f;
            gun.DefaultModule.numberOfFinalProjectiles = 0;
            gun.DefaultModule.finalProjectile = null;
           
            gun.DefaultModule.numberOfShotsInClip = 10;
            gun.barrelOffset.transform.localPosition = new Vector3(1.43f, 0.68f, 0f);
            gun.SetBaseMaxAmmo(200);
            gun.ammo = 200;
            gun.gunClass = GunClass.PISTOL;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);

            projectile.baseData.damage = 10f;            
            projectile.transform.parent = gun.barrelOffset;
            SimpleRandomTransmogrifyComponent transmog = projectile.gameObject.AddComponent<SimpleRandomTransmogrifyComponent>();
            transmog.maintainHPPercent = true;
            transmog.chaosPalette = true;
            transmog.RandomStringList.AddRange(MagickeCauldron.chaosEnemyPalette);
            if (projectile.GetComponent<PierceProjModifier>()) Destroy(projectile.GetComponent<PierceProjModifier>());

            gun.DefaultModule.projectiles[0] = projectile;
            gun.quality = PickupObject.ItemQuality.B;
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            BioTranstater2100ID = gun.PickupObjectId;
        }
        public static int BioTranstater2100ID;      
        public BioTranstater2100()
        {

        }
    }
}