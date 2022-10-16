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

    public class StickGun : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Stick Gun", "stickgun");
            Game.Items.Rename("outdated_gun_mods:stick_gun", "nn:stick_gun");
            var behav = gun.gameObject.AddComponent<StickGun>();
            behav.preventNormalReloadAudio = true;
            behav.preventNormalFireAudio = true;
            behav.overrideNormalFireAudio = "Play_PencilScratch";
            gun.SetShortDescription("Scribble");
            gun.SetLongDescription("Carried by a brave stickman as he ventured through the pages of a bored child's homework." + "\n\nHe may be long erased, but his legacy lives on.");

            gun.SetupSprite(null, "stickgun_idle_001", 8);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 0.7f;
            gun.DefaultModule.cooldownTime = 0.1f;
            gun.DefaultModule.numberOfShotsInClip = 3;
            gun.barrelOffset.transform.localPosition = new Vector3(0.93f, 0.68f, 0f);
            gun.SetBaseMaxAmmo(222);
            gun.ammo = 222;
            gun.gunClass = GunClass.SHITTY;
            gun.gunHandedness = GunHandedness.OneHanded;

            //VFX
            gun.muzzleFlashEffects = VFXToolbox.CreateVFXPool("Stick Gun Muzzleflash",
                new List<string>()
                {
                    "NevernamedsItems/Resources/MiscVFX/GunVFX/StickGun/stickgun_muzzleflash_001",
                    "NevernamedsItems/Resources/MiscVFX/GunVFX/StickGun/stickgun_muzzleflash_002",
                    "NevernamedsItems/Resources/MiscVFX/GunVFX/StickGun/stickgun_muzzleflash_003",
                },
                6, //FPS
                new IntVector2(12, 13), //Dimensions
                tk2dBaseSprite.Anchor.MiddleLeft, //Anchor
                false, //Uses a Z height off the ground
                0, //The Z height, if used
                false,
               VFXAlignment.Fixed
                  );

            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            projectile.baseData.damage = 6f;
            projectile.baseData.speed *= 1.5f;

            PierceProjModifier pierce = projectile.gameObject.AddComponent<PierceProjModifier>();
            pierce.penetration = 1;

            projectile.SetProjectileSpriteRight("stickgun_projectile", 12, 2, false, tk2dBaseSprite.Anchor.MiddleCenter, 12, 2);
            gun.DefaultModule.projectiles[0] = projectile;

            //Projectile VFX

            projectile.hitEffects.overrideMidairDeathVFX = VFXToolbox.CreateVFX("Stick Gun MidairDeath",
                  new List<string>()
                  {
                    "NevernamedsItems/Resources/MiscVFX/GunVFX/StickGun/stickgun_impact_001",
                    "NevernamedsItems/Resources/MiscVFX/GunVFX/StickGun/stickgun_impact_002",
                    "NevernamedsItems/Resources/MiscVFX/GunVFX/StickGun/stickgun_impact_003",
                    "NevernamedsItems/Resources/MiscVFX/GunVFX/StickGun/stickgun_impact_004",
                  },
                  6, //FPS
                  new IntVector2(16, 15), //Dimensions
                  tk2dBaseSprite.Anchor.MiddleCenter, //Anchor
                  false, //Uses a Z height off the ground
                  0 //The Z height, if used
                    );

            projectile.hitEffects.tileMapHorizontal = VFXToolbox.CreateVFXPool("Stick Gun TileMapHoriz",
                new List<string>()
                {
                    "NevernamedsItems/Resources/MiscVFX/GunVFX/StickGun/stickgun_tilemaphoriz_001",
                    "NevernamedsItems/Resources/MiscVFX/GunVFX/StickGun/stickgun_tilemaphoriz_002",
                    "NevernamedsItems/Resources/MiscVFX/GunVFX/StickGun/stickgun_tilemaphoriz_003"
                },
                6, //FPS
                new IntVector2(12, 21), //Dimensions
                tk2dBaseSprite.Anchor.MiddleLeft, //Anchor
                false, //Uses a Z height off the ground
                0 //The Z height, if used
                  );

            projectile.hitEffects.tileMapVertical = VFXToolbox.CreateVFXPool("Stick Gun TileMapVert",
                new List<string>()
                {
                    "NevernamedsItems/Resources/MiscVFX/GunVFX/StickGun/stickgun_tilemapvert_001",
                    "NevernamedsItems/Resources/MiscVFX/GunVFX/StickGun/stickgun_tilemapvert_002",
                    "NevernamedsItems/Resources/MiscVFX/GunVFX/StickGun/stickgun_tilemapvert_003"
                },
                6, //FPS
                new IntVector2(18, 21), //Dimensions
                tk2dBaseSprite.Anchor.MiddleLeft, //Anchor
                false, //Uses a Z height off the ground
                0 //The Z height, if used
                  );

            projectile.hitEffects.enemy = VFXToolbox.CreateVFXPool("Stick Gun EnemyImpact",
                new List<string>()
                {
                    "NevernamedsItems/Resources/MiscVFX/GunVFX/StickGun/stickgun_enemyimpact_001",
                    "NevernamedsItems/Resources/MiscVFX/GunVFX/StickGun/stickgun_enemyimpact_002",
                    "NevernamedsItems/Resources/MiscVFX/GunVFX/StickGun/stickgun_enemyimpact_003",
                    "NevernamedsItems/Resources/MiscVFX/GunVFX/StickGun/stickgun_enemyimpact_004"
                },
                6, //FPS
                new IntVector2(15, 16), //Dimensions
                tk2dBaseSprite.Anchor.MiddleCenter, //Anchor
                false, //Uses a Z height off the ground
                0, //The Z height, if used
                false,
                VFXAlignment.Fixed
                  );

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("StickGun Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/stickgun_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/stickgun_clipempty");

            gun.quality = PickupObject.ItemQuality.D;
            ETGMod.Databases.Items.Add(gun, null, "ANY");
            StickGunID = gun.PickupObjectId;

        }
        public static int StickGunID;
    }
}
