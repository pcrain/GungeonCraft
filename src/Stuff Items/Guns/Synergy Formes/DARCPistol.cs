using Alexandria.ItemAPI;
using Gungeon;
using System.Collections.Generic;
using UnityEngine;

namespace NevernamedsItems
{
    //DARC weapons are +2 clipshots, +20% damage, and +10% firerate.
    public class DARCPistol : GunBehaviour
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("DARC Pistol", "darcpistol");
            Game.Items.Rename("outdated_gun_mods:darc_pistol", "nn:arc_pistol+darc_pistol");
            gun.gameObject.AddComponent<DARCPistol>();
            gun.SetShortDescription("Shocked And Loaded");
            gun.SetLongDescription("Developed by the ARC Private Security company for easy manufacture and deployment, this electrotech blaster is the epittome of the ARC brand.");

            gun.SetupSprite(null, "darcpistol_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 15);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(153) as Gun).gunSwitchGroup;

            //GUN STATS
            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(41) as Gun).muzzleFlashEffects;
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 0.7f;
            gun.DefaultModule.cooldownTime = 0.135f;
            gun.DefaultModule.numberOfShotsInClip = 7;
            gun.barrelOffset.transform.localPosition = new Vector3(22f / 16f, 11f / 16f, 0f);
            gun.SetBaseMaxAmmo(300);
            gun.gunClass = GunClass.PISTOL;

            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.speed *= 5f;
            projectile.baseData.damage = 7.2f;
            projectile.SetProjectileSpriteRight("darc_proj", 8, 2, false, tk2dBaseSprite.Anchor.MiddleCenter, 8, 2);
            LightningProjectileComp lightning = projectile.gameObject.GetOrAddComponent<LightningProjectileComp>();
            lightning.targetEnemies = true;
            lightning.arcBetweenEnemiesRange = 6;
            projectile.gameObject.AddComponent<PierceDeadActors>();
            projectile.gameObject.AddComponent<MaintainDamageOnPierce>();

            projectile.hitEffects.overrideMidairDeathVFX = VFXToolbox.CreateVFX("DARC Impact",
                 new List<string>()
                 {
                    "NevernamedsItems/Resources/MiscVFX/GunVFX/red_electricimpact_001",
                    "NevernamedsItems/Resources/MiscVFX/GunVFX/red_electricimpact_002",
                    "NevernamedsItems/Resources/MiscVFX/GunVFX/red_electricimpact_003",
                 },
                10, //FPS
                 new IntVector2(44, 43), //Dimensions
                 tk2dBaseSprite.Anchor.MiddleCenter, //Anchor
                 false, //Uses a Z height off the ground
                 0 //The Z height, if used
                   );
            projectile.hitEffects.alwaysUseMidair = true;

            List<string> BeamAnimPaths = new List<string>()
            {
                "NevernamedsItems/Resources/TrailSprites/darctrail_mid_001",
                "NevernamedsItems/Resources/TrailSprites/darctrail_mid_002",
                "NevernamedsItems/Resources/TrailSprites/darctrail_mid_003",
            };

            projectile.AddTrailToProjectile(
                "NevernamedsItems/Resources/TrailSprites/darctrail_mid_001",
                new Vector2(3, 2),
                new Vector2(1, 1),
                BeamAnimPaths, 20,
                BeamAnimPaths, 20,
                -1,
                0.0001f,
                -1,
                true
                );
            EmmisiveTrail emis = projectile.gameObject.GetOrAddComponent<EmmisiveTrail>();

            CombineEvaporateEffect origEvap = (PickupObjectDatabase.GetById(519) as Gun).alternateVolley.projectiles[0].projectiles[0].GetComponent<CombineEvaporateEffect>();
            CombineEvaporateEffect newEvap = projectile.gameObject.AddComponent<CombineEvaporateEffect>();
            newEvap.FallbackShader = origEvap.FallbackShader;
            newEvap.ParticleSystemToSpawn = origEvap.ParticleSystemToSpawn;

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("DARC Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/darcweapon_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/darcweapon_clipempty");

            gun.quality = PickupObject.ItemQuality.EXCLUDED;
            ETGMod.Databases.Items.Add(gun, false, "ANY");


            ID = gun.PickupObjectId;
        }
        public static int ID;
    }
}