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

    public class G20 : AdvancedGunBehavior
    {
        public static void Add()
        {

            Gun gun = ETGMod.Databases.Items.NewGun("G20", "g20");
            Game.Items.Rename("outdated_gun_mods:g20", "nn:g20");
            gun.gameObject.AddComponent<G20>();
            gun.SetShortDescription("Roll and Die");
            gun.SetLongDescription("Randomises stats upon entering combat." + "\n\nThe preferred weapon of a young disciple of Icosahedrax, stolen by his michevious nephew.");
            gun.SetupSprite(null, "g20_idle_001", 8);
            gun.SetAnimationFPS(gun.shootAnimation, 14);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.DefaultModule.cooldownTime = 0.5f;
            gun.DefaultModule.numberOfShotsInClip = 10;
            gun.barrelOffset.transform.localPosition = new Vector3(1.56f, 0.87f, 0f);
            gun.SetBaseMaxAmmo(350);
            gun.gunClass = GunClass.PISTOL;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.transform.parent = gun.barrelOffset;
            projectile.baseData.speed *= 1f;
            projectile.baseData.damage *= 2f;
            projectile.baseData.range *= 1f;
            projectile.SetProjectileSpriteRight("g20_projectile", 11, 11, true, tk2dBaseSprite.Anchor.MiddleCenter, 10, 10);

            gun.quality = PickupObject.ItemQuality.D;
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            G20ID = gun.PickupObjectId;
        }
        public static int G20ID;
        public override void OnReloadPressedSafe(PlayerController player, Gun gun, bool manualReload)
        {
            if (player.PlayerHasActiveSynergy("Rerollin Rollin Rollin"))
            {
                if ((gun.ClipCapacity == gun.ClipShotsRemaining) || (gun.CurrentAmmo == gun.ClipShotsRemaining))
                {
                    if (gun.CurrentAmmo >= 10)
                    {
                        gun.CurrentAmmo -= 10;
                        EnteredCombat();
                    }
                }
            }
            base.OnReloadPressedSafe(player, gun, manualReload);
        }
        protected override void OnPickedUpByPlayer(PlayerController player)
        {
            player.OnEnteredCombat += this.EnteredCombat;
            base.OnPickedUpByPlayer(player);
        }
        protected override void OnPostDroppedByPlayer(PlayerController player)
        {
            player.OnEnteredCombat -= this.EnteredCombat;
            base.OnPostDroppedByPlayer(player);
        }
        protected override void Update()
        {
            if (ClipSize != -1 && gun.DefaultModule.numberOfShotsInClip != ClipSize)
            {
                gun.DefaultModule.numberOfShotsInClip = ClipSize;
            }
            if (CooldownTime != -1 && gun.DefaultModule.cooldownTime != CooldownTime)
            {
                gun.DefaultModule.cooldownTime = CooldownTime;
            }
            base.Update();
        }
        private void EnteredCombat()
        {
            gun.reloadTime = UnityEngine.Random.Range(10f, 191f) / 100f;

            CooldownTime = UnityEngine.Random.Range(10f, 81f) / 100f;
            ClipSize = UnityEngine.Random.Range(1, 31);

            gun.DefaultModule.numberOfShotsInClip = ClipSize;
            gun.DefaultModule.cooldownTime = CooldownTime;

            damageMod = UnityEngine.Random.Range(10f, 211f) / 100f;
            rangeMod = UnityEngine.Random.Range(10f, 191f) / 100f;
            speedMod = UnityEngine.Random.Range(10f, 191f) / 100f;
            knockbackMod = UnityEngine.Random.Range(10f, 191f) / 100f;
            scaleMod = UnityEngine.Random.Range(10f, 191f) / 100f;
        }
        private int ClipSize = -1;
        private float CooldownTime = -1;
        public override void PostProcessProjectile(Projectile projectile)
        {
            projectile.baseData.damage *= damageMod;
            projectile.baseData.force *= knockbackMod;
            projectile.baseData.speed *= speedMod;
            projectile.baseData.range *= rangeMod;
            projectile.UpdateSpeed();
            projectile.RuntimeUpdateScale(scaleMod);
            base.PostProcessProjectile(projectile);
        }
        private float damageMod = 1;
        private float rangeMod = 1;
        private float speedMod = 1;
        private float knockbackMod = 1;
        private float scaleMod = 1;

        public G20()
        {

        }
    }
}

