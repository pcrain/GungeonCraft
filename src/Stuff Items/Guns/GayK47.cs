using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using MonoMod.RuntimeDetour;
using System.Reflection;
using Alexandria.ItemAPI;

namespace NevernamedsItems
{
    public class GayK47 : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("GayK-47", "gayk47");
            Game.Items.Rename("outdated_gun_mods:gayk47", "nn:gayk47");
            gun.gameObject.AddComponent<GayK47>();
            gun.SetShortDescription("Somewhere");
            gun.SetLongDescription("This shiny AK comes in all types of colours."+"\n\nThinks that the Machine Pistol is cute, but is too embarrassed to say anything.");

            gun.SetupSprite(null, "gayk47_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 12);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(15) as Gun).gunSwitchGroup;
            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Automatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.DefaultModule.cooldownTime = 0.15f;
            gun.DefaultModule.numberOfShotsInClip = 20;
            gun.barrelOffset.transform.localPosition = new Vector3(2.25f, 0.5f, 0f);
            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(15) as Gun).muzzleFlashEffects;
            gun.SetBaseMaxAmmo(400);
            gun.ammo = 400;
            gun.gunClass = GunClass.SILLY;
            gun.gunScreenShake = (PickupObjectDatabase.GetById(15) as Gun).gunScreenShake;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage = 6f;
            projectile.baseData.speed *= 1f;
            projectile.baseData.range *= 2f;

            gun.SetTag("kalashnikov");

            projectile.gameObject.AddComponent<GayK47Mod>();
            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("Gayk47 Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/gayk47_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/gayk47_clipempty");
            gun.quality = PickupObject.ItemQuality.A;
            ETGMod.Databases.Items.Add(gun, false, "ANY");

            GayK47ID = gun.PickupObjectId;
        }
        
        public static int GayK47ID;
        public override void PostProcessProjectile(Projectile projectile)
        {
            base.PostProcessProjectile(projectile);
        }
        public GayK47()
        {

        }
    }
    public class GayK47Mod : MonoBehaviour
    {
        public GayK47Mod()
        {

        }      
        private void Start()
        {
            self = base.GetComponent<Projectile>();
            if (self)
            {
                int random = UnityEngine.Random.Range(1, 8);
                switch (random)
                {
                    case 1://RED
                        self.statusEffectsToApply.Add(StaticStatusEffects.hotLeadEffect);
                        self.AdjustPlayerProjectileTint(Color.red ,1);
                        break;
                    case 2://YELLOW
                        self.statusEffectsToApply.Add(StaticStatusEffects.tripleCrossbowSlowEffect);
                        self.AdjustPlayerProjectileTint(Color.yellow, 1);
                        break;
                    case 3://GREEN
                        self.statusEffectsToApply.Add(StaticStatusEffects.irradiatedLeadEffect);
                        self.AdjustPlayerProjectileTint(ExtendedColours.poisonGreen, 1);
                        break;
                    case 4:
                        self.statusEffectsToApply.Add(StaticStatusEffects.frostBulletsEffect);
                        self.AdjustPlayerProjectileTint(ExtendedColours.freezeBlue, 1);
                        break;
                    case 5:
                        self.statusEffectsToApply.Add(StaticStatusEffects.charmingRoundsEffect);
                        self.AdjustPlayerProjectileTint(ExtendedColours.charmPink, 1);
                        break;
                    case 6:
                        self.statusEffectsToApply.Add(StaticStatusEffects.StandardPlagueEffect);
                        self.AdjustPlayerProjectileTint(ExtendedColours.plaguePurple, 1);
                        break;
                    case 7:
                        self.statusEffectsToApply.Add(StaticStatusEffects.elimentalerCheeseEffect);
                        self.AdjustPlayerProjectileTint(ExtendedColours.vibrantOrange, 1);
                        break;
                }
            }
        }
        private Projectile self;
    }
}
