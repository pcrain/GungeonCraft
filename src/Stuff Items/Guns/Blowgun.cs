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

    public class Blowgun : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Blowgun", "blowgun");
            Game.Items.Rename("outdated_gun_mods:blowgun", "nn:blowgun");
         var behav =   gun.gameObject.AddComponent<Blowgun>();
            behav.preventNormalReloadAudio = true;
            gun.SetShortDescription("Huff and Puff");
            gun.SetLongDescription("Relies on lung strength to propel poisonous darts." + "\n\nRobots may need to hold it up to a cooling vent or something.");

            gun.SetupSprite(null, "blowgun_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 15);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(150) as Gun).gunSwitchGroup;

            //GUN STATS
            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(26) as Gun).muzzleFlashEffects;
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 0.5f;
            gun.DefaultModule.cooldownTime = 0.2f;
            gun.DefaultModule.numberOfShotsInClip = 1;
            gun.barrelOffset.transform.localPosition = new Vector3(1.12f, 0.18f, 0f);
            gun.SetBaseMaxAmmo(200);
            gun.gunClass = GunClass.POISON;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.transform.parent = gun.barrelOffset;
            projectile.baseData.speed *= 2f;
            projectile.damageTypes |= CoreDamageTypes.Poison;
            ExtremelySimplePoisonBulletBehaviour poisoning = projectile.gameObject.AddComponent<ExtremelySimplePoisonBulletBehaviour>();
            poisoning.procChance = 1;
            poisoning.useSpecialTint = false;
            projectile.SetProjectileSpriteRight("blowgun_projectile", 16, 7, false, tk2dBaseSprite.Anchor.MiddleCenter, 15, 8);

            VFXPool HorizVFXOBj = VFXToolbox.CreateVFXPool("Blowgun Tilemap VFX Horiz",
                new List<string>()
            {
                "NevernamedsItems/Resources/MiscVFX/GunVFX/PoisonDarts/poisondart_impacthoriz_001",
                "NevernamedsItems/Resources/MiscVFX/GunVFX/PoisonDarts/poisondart_impacthoriz_002",
                "NevernamedsItems/Resources/MiscVFX/GunVFX/PoisonDarts/poisondart_impacthoriz_003",
                "NevernamedsItems/Resources/MiscVFX/GunVFX/PoisonDarts/poisondart_impacthoriz_004",
            },
                10,
                new IntVector2(12, 11),
                tk2dBaseSprite.Anchor.MiddleLeft,
                false,
                0,
                true);

            projectile.hitEffects.deathTileMapHorizontal = HorizVFXOBj;
            projectile.hitEffects.tileMapHorizontal = HorizVFXOBj;

            projectile.hitEffects.deathTileMapVertical = HorizVFXOBj;
            projectile.hitEffects.tileMapVertical = HorizVFXOBj;

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("Blowgun Darts", "NevernamedsItems/Resources/CustomGunAmmoTypes/blowgun_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/blowgun_clipempty");

            gun.quality = PickupObject.ItemQuality.D;
            gun.encounterTrackable.EncounterGuid = "this is the Blowgun";
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            foreach (tk2dSpriteAnimationClip clip in AdjustGunPosition.GetGunAnimationClips(gun))
            {
                foreach (tk2dSpriteAnimationFrame frame in clip.frames)
                {
                    frame.spriteCollection.spriteDefinitions[frame.spriteId].ApplyOffset(new Vector2(0, 0f));
                }
            }

            BlowgunID = gun.PickupObjectId;
        }
        public override void PostProcessProjectile(Projectile projectile)
        {
            PlayerController player = projectile.Owner as PlayerController;
            base.PostProcessProjectile(projectile);
            if (player.PlayerHasActiveSynergy("Old and New"))
            {
                projectile.AppliesStun = true;
                projectile.AppliedStunDuration += 10f;
                projectile.StunApplyChance = 1f;
            }
        }
        public static int BlowgunID;
        public Blowgun()
        {

        }
    }
}