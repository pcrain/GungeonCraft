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
    public class RapidRiposte : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Rapid Riposte", "rapidriposte");
            Game.Items.Rename("outdated_gun_mods:rapid_riposte", "nn:rapid_riposte");
            gun.gameObject.AddComponent<RapidRiposte>();
            gun.SetShortDescription("Top Tier Gunsmanship");
            gun.SetLongDescription("Fires with the utmost precision, parrying projectiles back at their owners." + "\n\nAn old rapier, modified for gunslinging.");

            gun.SetupSprite(null, "rapidriposte_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 16);

            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(417) as Gun).gunSwitchGroup;
            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.DefaultModule.cooldownTime = 0.1f;
            gun.DefaultModule.angleVariance = 1;
            gun.DefaultModule.numberOfShotsInClip = 5;
            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(97) as Gun).muzzleFlashEffects;
            gun.barrelOffset.transform.localPosition = new Vector3(2.62f, 0.43f, 0f);
            gun.SetBaseMaxAmmo(130);
            gun.ammo = 130;
            gun.gunClass = GunClass.RIFLE;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            projectile.baseData.damage *= 3f;
            projectile.baseData.force *= 3f;
            projectile.baseData.speed *= 2f;
            AdvancedMirrorProjectileModifier mirror = projectile.gameObject.GetOrAddComponent<AdvancedMirrorProjectileModifier>();
            mirror.projectileSurvives = true;
            mirror.maxMirrors = 1;
            mirror.postProcessReflectedBullets = true;
            mirror.tintsBullets = false;

            projectile.SetProjectileSpriteRight("rapidriposte_projectile", 26, 8, true, tk2dBaseSprite.Anchor.MiddleCenter, 13, 4);


            gun.DefaultModule.projectiles[0] = projectile;

            gun.quality = PickupObject.ItemQuality.A;
            ETGMod.Databases.Items.Add(gun, null, "ANY");
            RapidRiposteID = gun.PickupObjectId;
        }
        public static int RapidRiposteID;
        public override void PostProcessProjectile(Projectile projectile)
        {
            if (projectile.GetComponent<AdvancedMirrorProjectileModifier>())
            {
                if (projectile.Owner is PlayerController && (projectile.Owner as PlayerController).PlayerHasActiveSynergy("Wouldn't You Agree?"))
                {
                    projectile.GetComponent<AdvancedMirrorProjectileModifier>().RapidRiposteWeebshitSynergy = true;
                }
            }
            base.PostProcessProjectile(projectile);
        }
        public RapidRiposte()
        {

        }
    }
}
