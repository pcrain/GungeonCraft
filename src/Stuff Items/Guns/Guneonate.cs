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
    public class Guneonate : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Guneonate", "guneonate");
            Game.Items.Rename("outdated_gun_mods:guneonate", "nn:guneonate");
            var behav = gun.gameObject.AddComponent<Guneonate>();
            behav.overrideNormalFireAudio = "Play_VO_bashellisk_hiss_01";
            behav.overrideNormalReloadAudio = "Play_BOSS_bashellisk_swallow_01";
            behav.preventNormalFireAudio = true;
            behav.preventNormalReloadAudio = true;
            gun.SetShortDescription("Babyconda");
            gun.SetLongDescription("A hatchling ammoconda, formed from fresh discarded bullet casings." + "\n\nIt seems to have self esteem issues.");

            gun.SetupSprite(null, "guneonate_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 10);
            gun.SetAnimationFPS(gun.idleAnimation, 10);
            gun.SetAnimationFPS(gun.reloadAnimation, 1);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.DefaultModule.cooldownTime = 0.45f;
            gun.DefaultModule.angleVariance = 0;
            gun.DefaultModule.numberOfShotsInClip = 3;
            gun.barrelOffset.transform.localPosition = new Vector3(2.0f, 0.25f, 0f);
            gun.SetBaseMaxAmmo(200);
            gun.ammo = 200;
            gun.gunClass = GunClass.SILLY;

            //BULLET STATS
            ImprovedHelixProjectile projectile = DataCloners.CopyFields<ImprovedHelixProjectile>(Instantiate(gun.DefaultModule.projectiles[0]));
            projectile.SpawnShadowBulletsOnSpawn = true;
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            projectile.baseData.damage *= 2f;
            projectile.baseData.force *= 1f;
            projectile.baseData.speed *= 0.5f;
            projectile.baseData.range *= 2f;
            PierceProjModifier pierce = projectile.gameObject.GetOrAddComponent<PierceProjModifier>();
            pierce.penetration++;
            pierce.penetratesBreakables = true;
            //AutoDoShadowChainOnSpawn snakeness = projectile.gameObject.GetOrAddComponent<AutoDoShadowChainOnSpawn>();

            projectile.NumberInChain = 5;
            projectile.pauseLength = 0.05f;
            projectile.SetProjectileSpriteRight("12x12_yellowenemy_projectile", 12, 12, true, tk2dBaseSprite.Anchor.MiddleCenter, 10, 10);


            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("Guneonate Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/guneonate_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/guneonate_clipempty");

            gun.DefaultModule.projectiles[0] = projectile;

            gun.quality = PickupObject.ItemQuality.S;
            ETGMod.Databases.Items.Add(gun, null, "ANY");
            GuneonateID = gun.PickupObjectId;
        }
        public static int GuneonateID;
        public Guneonate()
        {

        }
    }
}