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
    public class Repetitron : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Repetitron", "repetiton");
            Game.Items.Rename("outdated_gun_mods:repetitron", "nn:repetitron");
            var behav = gun.gameObject.AddComponent<Repetitron>();
            gun.SetShortDescription("We've Done This Before");
            gun.SetLongDescription("Fires bullets... again... and again... and again."+"\n\nThis gun is powered by a miniature recursive sub-space anomaly. Do not look at the operational end.");

            gun.SetupSprite(null, "repetiton_idle_001", 8);

            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(89) as Gun).gunSwitchGroup;
            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.DefaultModule.cooldownTime = 0.2f;
            gun.DefaultModule.angleVariance = 4;
            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(89) as Gun).muzzleFlashEffects;
            gun.DefaultModule.numberOfShotsInClip = 5;
            gun.barrelOffset.transform.localPosition = new Vector3(2.0f, 0.25f, 0f);
            gun.SetBaseMaxAmmo(200);
            gun.ammo = 200;
            gun.gunClass = GunClass.SILLY;

            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            projectile.baseData.damage *= 1f;
            projectile.baseData.force *= 1f;
            projectile.baseData.speed *= 1f;
            projectile.hitEffects.alwaysUseMidair = true;
            projectile.hitEffects.overrideMidairDeathVFX = EasyVFXDatabase.GreenLaserCircleVFX;
            projectile.baseData.range *= 0.5f;
            projectile.SetProjectileSpriteRight("repetiton_projectile", 10, 7, true, tk2dBaseSprite.Anchor.MiddleCenter, 9, 6);

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("Repetitron Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/repetitron_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/repetitron_clipempty");

            gun.DefaultModule.projectiles[0] = projectile;

            gun.quality = PickupObject.ItemQuality.A;
            ETGMod.Databases.Items.Add(gun, null, "ANY");
        }
        public Repetitron()
        {

        }
        public override void OnReloadPressed(PlayerController player, Gun gun, bool manualReload)
        {
            storedProjectiles.Clear();
            base.OnReloadPressed(player, gun, manualReload);
        }
        public override void OnPostFired(PlayerController player, Gun gun)
        {
            if (storedProjectiles.Count > 0)
            {
                player.StartCoroutine(HandleReSpawn());
            }
            base.OnPostFired(player, gun);
        }
        private IEnumerator HandleReSpawn()
        {
            int count = storedProjectiles.Count;
            for (int i = 0; i < count; i++)
            { 
                SpawnProjectile(storedProjectiles[i]);
                yield return null;
            }
                yield break;
        }
        public override void OnSwitchedAwayFromThisGun()
        {
            storedProjectiles.Clear();

            base.OnSwitchedAwayFromThisGun();
        }
        private void SpawnProjectile(ProjAndPositionData data)
        {
            UnityEngine.Object.Instantiate<GameObject>(EasyVFXDatabase.GreenLaserCircleVFX, new Vector3(data.position.x, data.position.y), Quaternion.identity);
            GameObject obj = SpawnManager.SpawnProjectile(data.projectile, new Vector3(data.position.x, data.position.y, 0), Quaternion.Euler(0, 0, data.angle));
            Projectile component = obj.GetComponent<Projectile>();
            if (component != null)
            {
                component.Owner = Owner;
                component.Shooter = Owner.specRigidbody;
                component.collidesWithPlayer = false;
            }
        }
        public override void PostProcessProjectile(Projectile projectile)
        {
            GameManager.Instance.StartCoroutine(HandleAddToList(projectile));
            base.PostProcessProjectile(projectile);
        }
        private IEnumerator HandleAddToList(Projectile proj)
        {
            yield return null;
            ProjAndPositionData newData = new ProjAndPositionData();
            newData.projectile = FakePrefab.Clone(proj.gameObject);
            newData.position = proj.specRigidbody.UnitCenter;
            newData.angle = proj.Direction.ToAngle();
            yield return new WaitForSeconds(0.01f);
            storedProjectiles.Add(newData);
            yield break;
        }
        private List<ProjAndPositionData> storedProjectiles = new List<ProjAndPositionData>();
        public class ProjAndPositionData
        {
            public GameObject projectile;
            public Vector2 position;
            public float angle;
        }
    }
}
