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

    public class BarcodeScanner : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Barcode Scanner", "barcodescanner");
            Game.Items.Rename("outdated_gun_mods:barcode_scanner", "nn:barcode_scanner");
          var behav =  gun.gameObject.AddComponent<BarcodeScanner>();
            behav.overrideNormalReloadAudio = "Play_OBJ_mine_beep_01";
            behav.overrideNormalFireAudio = "Play_OBJ_mine_beep_01";
            behav.preventNormalFireAudio = true;
            behav.preventNormalReloadAudio = true;
            gun.SetShortDescription("Beep");
            gun.SetLongDescription("Often used in more technologically adept shops to scan items for purchase, but Bello has no idea how to use a computer.");

            gun.SetupSprite(null, "barcodescanner_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 5);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 0.5f;
            gun.muzzleFlashEffects.type = VFXPoolType.None;
            gun.DefaultModule.cooldownTime = 0.35f;
            gun.DefaultModule.numberOfShotsInClip = 500;
            gun.barrelOffset.transform.localPosition = new Vector3(0.43f, 0.56f, 0f);
            gun.SetBaseMaxAmmo(500);
            gun.ammo = 500;
            gun.gunClass = GunClass.SHITTY;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);

            projectile.baseData.damage = 6f;
            projectile.SetProjectileSpriteRight("barcodescanner_projectile", 4, 14, false, tk2dBaseSprite.Anchor.MiddleCenter, 2, 12);
            projectile.hitEffects.alwaysUseMidair = true;
            projectile.hitEffects.overrideMidairDeathVFX = EasyVFXDatabase.RedLaserCircleVFX;
            projectile.transform.parent = gun.barrelOffset;
            projectile.gameObject.AddComponent<BarcodeScannerProjectile>();
            gun.AddPassiveStatModifier(PlayerStats.StatType.GlobalPriceMultiplier, 0.9f, StatModifier.ModifyMethod.MULTIPLICATIVE);

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("Barcode Scanner Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/barcodescanner_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/barcodescanner_clipempty");

            gun.DefaultModule.projectiles[0] = projectile;
            gun.quality = PickupObject.ItemQuality.D;
            ETGMod.Databases.Items.Add(gun, false, "ANY");

            BarcodeScannerID = gun.PickupObjectId;
        }
        public static int BarcodeScannerID;
        public BarcodeScanner()
        {

        }
    }
    class BarcodeScannerProjectile : MonoBehaviour
    {
        public BarcodeScannerProjectile()
        {

        }
        private void Start()
        {
            m_projectile = base.GetComponent<Projectile>();
            if (m_projectile)
            {
                m_projectile.OnHitEnemy += this.OnHitEnemy;
            }
        }
        private void OnHitEnemy(Projectile proj, SpeculativeRigidbody enemy, bool fatal)
        {
            if (fatal && enemy && enemy.aiActor)
            {
                enemy.aiActor.AssignedCurrencyToDrop += UnityEngine.Random.Range(1, 4);
            }
        }
        private Projectile m_projectile;
    }
}