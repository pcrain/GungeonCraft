using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using Alexandria.ItemAPI;

namespace NevernamedsItems
{
    public class Gunger : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Gunger", "gunger");
            Game.Items.Rename("outdated_gun_mods:gunger", "nn:gunger");
            gun.gameObject.AddComponent<Gunger>();
            gun.SetShortDescription("Hungry Gun");
            gun.SetLongDescription("Reloading this strange creature near guns on the ground will cause them to be... consumed?" + "\n\nThese creatures are worshipped as gods in some cultures, though they know it not.");

            gun.SetupSprite(null, "gunger_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 12);
            gun.SetAnimationFPS(gun.idleAnimation, 9);
            gun.SetAnimationFPS(gun.reloadAnimation, 12);

            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(599) as Gun).gunSwitchGroup;
            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1.2f;
            gun.DefaultModule.cooldownTime = 0.5f;
            gun.muzzleFlashEffects.type = VFXPoolType.None;
            gun.DefaultModule.numberOfShotsInClip = 10;
            gun.barrelOffset.transform.localPosition = new Vector3(2.31f, 0.62f, 0f);
            gun.SetBaseMaxAmmo(300);
            gun.ammo = 300;
            gun.gunClass = GunClass.SILLY;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage *= 2f;
            projectile.baseData.speed *= 0.8f;
            projectile.baseData.range *= 1f;
            projectile.baseData.force *= 1.2f;
            GungerBaseProjectile gungerbase = projectile.gameObject.AddComponent<GungerBaseProjectile>();

            projectile.SetProjectileSpriteRight("gunger_projectile", 17, 9, false, tk2dBaseSprite.Anchor.MiddleCenter, 16, 8);

            projectile.transform.parent = gun.barrelOffset;

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("Gunger Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/gunger_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/gunger_clipempty");

            gun.quality = PickupObject.ItemQuality.A;
            ETGMod.Databases.Items.Add(gun, false, "ANY");
        }
        public override void PostProcessProjectile(Projectile projectile)
        {
            if (gun.ClipShotsRemaining == gun.ClipCapacity - 1)
            {
                if (projectile.Owner is PlayerController && (projectile.Owner as PlayerController).PlayerHasActiveSynergy("Famished"))
                {
                    HungryProjectileModifier hungry = projectile.gameObject.GetOrAddComponent<HungryProjectileModifier>();
                    hungry.HungryRadius = 1.5f;
                    projectile.AdjustPlayerProjectileTint(ExtendedColours.purple, 1);
                }
            }
            base.PostProcessProjectile(projectile);
        }
        public override void OnReloadPressedSafe(PlayerController player, Gun gun, bool manualReload)
        {
            IPlayerInteractable nearestInteractable = player.CurrentRoom.GetNearestInteractable(player.CenterPosition, 1f, player);
            if (nearestInteractable != null && nearestInteractable is Gun)
            {
                Gun gunness = nearestInteractable as Gun;
                if (gunness != null && !InvalidGuns.Contains(gunness.PickupObjectId) && gunness.DefaultModule.shootStyle != ProjectileModule.ShootStyle.Beam)
                {
                    EatAndAbsorbGun(gun, gunness);
                }
                else { }//ETGModConsole.Log("Gunness is null, on the exclusion list, or a beam"); }
            }
            else { }//ETGModConsole.Log("Nearest Interactible is either null or not a debris object"); }
            base.OnReloadPressedSafe(player, gun, manualReload);
        }
        public static List<int> InvalidGuns = new List<int>()
        { }; //ADD STUFF LIKE GUNDERTALE HERE UPON REINTEGRATION
        protected override void OnPickedUpByPlayer(PlayerController player)
        {
            if (!everPickedUpByPlayer)
            {
                foreach (Projectile proj in this.gun.RawSourceVolley.projectiles[0].projectiles)
                {
                    GungerBaseProjectile baseProj = proj.GetComponent<GungerBaseProjectile>();
                    if (baseProj == null)
                    {
                        this.gun.RawSourceVolley.projectiles[0].projectiles.Remove(proj);
                    }
                }
                if (player != null)
                {
                    player.stats.RecalculateStats(player, true, false);
                }
            }
            base.OnPickedUpByPlayer(player);
        }
        private void EatAndAbsorbGun(Gun baseGun, Gun absorbedgun)
        {
            //ETGModConsole.Log("EatAndAbsorbGun was run");
            int absorbedGunID = absorbedgun.PickupObjectId;
            Projectile ProjToAdd = null;
            if (absorbedgun.DefaultModule.shootStyle == ProjectileModule.ShootStyle.Charged)
            {
                //ProjToAdd = absorbedgun.RawSourceVolley.projectiles[0].chargeProjectiles[0].Projectile;
                ProjToAdd = absorbedgun.RawDefaultModule().chargeProjectiles[0].Projectile;
            }
            else
            {
                //ProjToAdd = absorbedgun.RawSourceVolley.projectiles[0].projectiles[0];
                ProjToAdd = absorbedgun.RawDefaultModule().projectiles[0].projectile;

            }
            if (ProjToAdd != null) baseGun.RawSourceVolley.projectiles[0].projectiles.Add(ProjToAdd);
            if (baseGun.CurrentOwner != null && baseGun.CurrentOwner is PlayerController)
            {
                (baseGun.CurrentOwner as PlayerController).stats.RecalculateStats((baseGun.CurrentOwner as PlayerController), true, false);
            }
            Destroy(absorbedgun.gameObject);
        }
        public class GungerBaseProjectile : MonoBehaviour
        {
            public GungerBaseProjectile()
            {
            }
        }
        public Gunger()
        {

        }
    }
}