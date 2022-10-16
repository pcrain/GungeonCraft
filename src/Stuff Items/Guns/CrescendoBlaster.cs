using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using ItemAPI;

namespace NevernamedsItems
{
    public class CrescendoBlaster : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Crescendo Blaster", "crescendoblaster");
            Game.Items.Rename("outdated_gun_mods:crescendo_blaster", "nn:crescendo_blaster");
            gun.gameObject.AddComponent<CrescendoBlaster>();
            gun.SetShortDescription("Rise and Fall");
            gun.SetLongDescription("Raises and lowers in damage as it fires." + "\n\nPowered by exotic morphous crystals from a distance moon.");

            gun.SetupSprite(null, "crescendoblaster_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 10);

            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(504) as Gun).gunSwitchGroup;
            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Ordered;
            gun.reloadTime = 1f;
            gun.DefaultModule.burstCooldownTime = 0.1f;
            gun.DefaultModule.cooldownTime = 0.25f;
            gun.DefaultModule.numberOfShotsInClip = 10;
            gun.DefaultModule.angleFromAim = 0f;
            gun.barrelOffset.transform.localPosition = new Vector3(1.56f, 0.68f, 0f);
            gun.SetBaseMaxAmmo(220);
            gun.ammo = 220;
            gun.gunClass = GunClass.PISTOL;
            //BULLET ONE
            Projectile threedamageproj = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(86) as Gun).DefaultModule.projectiles[0]);
            threedamageproj.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(threedamageproj.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(threedamageproj);
            threedamageproj.baseData.damage *= 0.6f;
            threedamageproj.SetProjectileSpriteRight("crescendoblaster_projectile", 25, 25, true, tk2dBaseSprite.Anchor.MiddleCenter, 20, 20);
            threedamageproj.AdditionalScaleMultiplier *= 0.16f;
            projOneSMALLEST = threedamageproj;

            Projectile sixdamageproj = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(86) as Gun).DefaultModule.projectiles[0]);
            sixdamageproj.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(sixdamageproj.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(sixdamageproj);
            sixdamageproj.baseData.damage *= 1.2f;
            sixdamageproj.SetProjectileSpriteRight("crescendoblaster_projectile", 25, 25, true, tk2dBaseSprite.Anchor.MiddleCenter, 20, 20);
            sixdamageproj.AdditionalScaleMultiplier *= 0.32f;
            projTwo = sixdamageproj;

            Projectile ninedamageproj = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(86) as Gun).DefaultModule.projectiles[0]);
            ninedamageproj.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(ninedamageproj.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(ninedamageproj);
            ninedamageproj.baseData.damage *= 1.8f;
            ninedamageproj.SetProjectileSpriteRight("crescendoblaster_projectile", 25, 25, true, tk2dBaseSprite.Anchor.MiddleCenter, 20, 20);
            ninedamageproj.AdditionalScaleMultiplier *= 0.48f;
            projThree = ninedamageproj;

            Projectile twelvedamageproj = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(86) as Gun).DefaultModule.projectiles[0]);
            twelvedamageproj.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(twelvedamageproj.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(twelvedamageproj);
            twelvedamageproj.baseData.damage *= 2.4f;
            twelvedamageproj.SetProjectileSpriteRight("crescendoblaster_projectile", 25, 25, true, tk2dBaseSprite.Anchor.MiddleCenter, 20, 20);
            twelvedamageproj.AdditionalScaleMultiplier *= 0.64f;
            projFour = twelvedamageproj;

            Projectile sixteendamageproj = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(86) as Gun).DefaultModule.projectiles[0]);
            sixteendamageproj.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(sixteendamageproj.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(sixteendamageproj);
            sixteendamageproj.baseData.damage *= 3.2f;
            sixteendamageproj.SetProjectileSpriteRight("crescendoblaster_projectile", 25, 25, true, tk2dBaseSprite.Anchor.MiddleCenter, 20, 20);
            sixteendamageproj.AdditionalScaleMultiplier *= 0.8f;
            projFive = sixteendamageproj;

            Projectile twentydamageprojectile = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(86) as Gun).DefaultModule.projectiles[0]);
            twentydamageprojectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(twentydamageprojectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(twentydamageprojectile);
            twentydamageprojectile.baseData.damage *= 4f;
            twentydamageprojectile.SetProjectileSpriteRight("crescendoblaster_projectile", 25, 25, true, tk2dBaseSprite.Anchor.MiddleCenter, 20, 20);
            twentydamageprojectile.AdditionalScaleMultiplier *= 1f;
            BigCrescendoBullet bigbullet = twentydamageprojectile.gameObject.AddComponent<BigCrescendoBullet>();
            projSixBIGGEST = twentydamageprojectile;

            gun.DefaultModule.projectiles[0] = projOneSMALLEST; //0
            gun.DefaultModule.projectiles.Add(projTwo); // 1
            gun.DefaultModule.projectiles.Add(projThree); // 2
            gun.DefaultModule.projectiles.Add(projFour); // 3
            gun.DefaultModule.projectiles.Add(projFive); // 4
            gun.DefaultModule.projectiles.Add(projSixBIGGEST); // 5
            gun.DefaultModule.projectiles.Add(projFive); // 6
            gun.DefaultModule.projectiles.Add(projFour); // 7
            gun.DefaultModule.projectiles.Add(projThree); // 8 
            gun.DefaultModule.projectiles.Add(projTwo); // 9

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("Crescendo Blaster Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/crescendoblaster_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/crescendoblaster_clipempty");

            gun.quality = PickupObject.ItemQuality.C;
            ETGMod.Databases.Items.Add(gun, null, "ANY");
            CrescendoBlasterID = gun.PickupObjectId;
        }
        public static int CrescendoBlasterID;
        public static Projectile projOneSMALLEST;
        public static Projectile projTwo;
        public static Projectile projThree;
        public static Projectile projFour;
        public static Projectile projFive;
        public static Projectile projSixBIGGEST;

        private void RemoveExtraSynergyBullets()
        {
            try
            {
                this.gun.RawSourceVolley.projectiles[0].numberOfShotsInClip = 10;
                this.gun.RawSourceVolley.projectiles[0].projectiles[6] = projFive;
                this.gun.RawSourceVolley.projectiles[0].projectiles[7] = projFour;
                this.gun.RawSourceVolley.projectiles[0].projectiles[8] = projThree;
                this.gun.RawSourceVolley.projectiles[0].projectiles[9] = projTwo;
                this.gun.RawSourceVolley.projectiles[0].projectiles.RemoveAt(11); // 10 
                this.gun.RawSourceVolley.projectiles[0].projectiles.RemoveAt(10); // 11
                if (this.gun.CurrentOwner != null && this.gun.CurrentOwner is PlayerController)
                {
                    (this.gun.CurrentOwner as PlayerController).stats.RecalculateStats((this.gun.CurrentOwner as PlayerController), true, false);
                }
            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message);
                ETGModConsole.Log(e.StackTrace);
            }
        }
        private void GiveExtraSynergyBullets()
        {
            this.gun.RawSourceVolley.projectiles[0].numberOfShotsInClip = 12;
            this.gun.RawSourceVolley.projectiles[0].projectiles[6] = projSixBIGGEST;
            this.gun.RawSourceVolley.projectiles[0].projectiles[7] = projSixBIGGEST;
            this.gun.RawSourceVolley.projectiles[0].projectiles[8] = projFive;
            this.gun.RawSourceVolley.projectiles[0].projectiles[9] = projFour;
            this.gun.RawSourceVolley.projectiles[0].projectiles.Add(projThree); // 10 
            this.gun.RawSourceVolley.projectiles[0].projectiles.Add(projTwo); // 11
            if (this.gun.CurrentOwner != null && this.gun.CurrentOwner is PlayerController)
            {
                (this.gun.CurrentOwner as PlayerController).stats.RecalculateStats((this.gun.CurrentOwner as PlayerController), true, false);
            }
        }
        bool doClipSizeUpgradeBurst = false;
        private void MakeBurstGun()
        {
            this.gun.RawSourceVolley.projectiles[0].shootStyle = ProjectileModule.ShootStyle.Burst;
            doClipSizeUpgradeBurst = true;
            if (this.gun.CurrentOwner != null && this.gun.CurrentOwner is PlayerController)
            {
                (this.gun.CurrentOwner as PlayerController).stats.RecalculateStats((this.gun.CurrentOwner as PlayerController), true, false);
            }
        }
        private void UnmakeBurstGun()
        {
            this.gun.RawSourceVolley.projectiles[0].shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            doClipSizeUpgradeBurst = false;
            if (this.gun.CurrentOwner != null && this.gun.CurrentOwner is PlayerController)
            {
                (this.gun.CurrentOwner as PlayerController).stats.RecalculateStats((this.gun.CurrentOwner as PlayerController), true, false);
            }
        }
        protected override void Update()
        {
            if (this.gun & this.gun.CurrentOwner != null & this.gun.CurrentOwner is PlayerController)
            {

                PlayerController owner = this.gun.CurrentOwner as PlayerController;
                BigCrescendoBullet bigcrescendo = this.gun.RawSourceVolley.projectiles[0].projectiles[6].GetComponent<BigCrescendoBullet>();
                if (bigcrescendo == null && owner.PlayerHasActiveSynergy("Fortissimo"))
                {
                    //ETGModConsole.Log("Updated to more crescendo bullets.");
                    GiveExtraSynergyBullets();
                }
                else if (bigcrescendo != null && !owner.PlayerHasActiveSynergy("Fortissimo"))
                {
                   //ETGModConsole.Log("Updated to less crescendo bullets.");
                    RemoveExtraSynergyBullets();
                }
                if (this.gun.DefaultModule.shootStyle != ProjectileModule.ShootStyle.Burst && owner.PlayerHasActiveSynergy("Allegro"))
                {
                    MakeBurstGun();
                }
                else if (this.gun.DefaultModule.shootStyle == ProjectileModule.ShootStyle.Burst && !owner.PlayerHasActiveSynergy("Allegro"))
                {
                    UnmakeBurstGun();
                }
                if (doClipSizeUpgradeBurst)
                {
                    if (this.gun.DefaultModule.burstShotCount != this.gun.DefaultModule.numberOfShotsInClip)
                    {
                        RectifyNonMatchingBurstCount();
                    }
                }
            }
            base.Update();
        }
        private void RectifyNonMatchingBurstCount()
        {
            this.gun.RawSourceVolley.projectiles[0].burstShotCount = this.gun.RawSourceVolley.projectiles[0].numberOfShotsInClip;
            if (this.gun.CurrentOwner != null && this.gun.CurrentOwner is PlayerController)
            {
                (this.gun.CurrentOwner as PlayerController).stats.RecalculateStats((this.gun.CurrentOwner as PlayerController), true, false);
            }
        }
        public class BigCrescendoBullet : MonoBehaviour
        {
            public BigCrescendoBullet()
            {
            }
        }

        public CrescendoBlaster()
        {

        }
    }
}
