using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using ItemAPI;
using Dungeonator;

namespace NevernamedsItems
{
    public class Primos1 : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Primos 1", "primos1");
            Game.Items.Rename("outdated_gun_mods:primos_1", "nn:primos_1");
            gun.gameObject.AddComponent<Primos1>();
            gun.SetShortDescription("Pre-emptive Strike");
            gun.SetLongDescription("First shot in every room is significantly more powerful." + "\n\nIssued to only the highest ranking Primerdyne Marines.");

            gun.SetupSprite(null, "primos1_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 10);

            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(504) as Gun).gunSwitchGroup;
            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.DefaultModule.cooldownTime = 0.30f;
            gun.DefaultModule.numberOfShotsInClip = 10;
            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(334) as Gun).muzzleFlashEffects;
            gun.barrelOffset.transform.localPosition = new Vector3(2.5f, 0.81f, 0f);
            gun.SetBaseMaxAmmo(260);
            gun.ammo = 260;
            gun.gunClass = GunClass.RIFLE;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            projectile.baseData.damage *= 1.6f;
            projectile.baseData.force *= 1.2f;
            projectile.hitEffects.alwaysUseMidair = true;
            projectile.hitEffects.overrideMidairDeathVFX = EasyVFXDatabase.RedLaserCircleVFX;
            projectile.SetProjectileSpriteRight("primos1_projectile", 17, 17, true, tk2dBaseSprite.Anchor.MiddleCenter, 15, 15);
            PrimosBulletBehaviour primosbullet = projectile.gameObject.GetOrAddComponent<PrimosBulletBehaviour>();

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = "Thinline Bullets";

            gun.DefaultModule.projectiles[0] = projectile;

            gun.quality = PickupObject.ItemQuality.B;
            ETGMod.Databases.Items.Add(gun, null, "ANY");
        }
        public static RoomHandler lastFiredRoom = null;
        public Primos1()
        {

        }
    }
    public class PrimosBulletBehaviour : MonoBehaviour
    {
        public PrimosBulletBehaviour()
        {

        }
        public void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            RoomHandler currentRoom = MiscToolbox.GetAbsoluteRoomFromProjectile(this.m_projectile);
            if (currentRoom != Primos1.lastFiredRoom)
            {
                this.m_projectile.baseData.speed *= 0.5f;
                this.m_projectile.baseData.damage *= 6.3f;
                PierceProjModifier pierce = this.m_projectile.gameObject.GetOrAddComponent<PierceProjModifier>();
                pierce.penetration = 1000;
                BounceProjModifier bounce = this.m_projectile.gameObject.GetOrAddComponent<BounceProjModifier>();
                bounce.numberOfBounces = 1;
                this.m_projectile.RuntimeUpdateScale(2);
                this.m_projectile.UpdateSpeed();
                Primos1.lastFiredRoom = currentRoom;
            }
        }
        private Projectile m_projectile;
    }
}
