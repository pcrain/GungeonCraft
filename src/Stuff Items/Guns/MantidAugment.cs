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
    public class MantidAugment : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Mantid Augment", "mantidaugment");
            Game.Items.Rename("outdated_gun_mods:mantid_augment", "nn:mantid_augment");
           var behav = gun.gameObject.AddComponent<MantidAugment>();
            behav.preventNormalFireAudio = true;

            gun.SetShortDescription("Flashy and Lethal");
            gun.SetLongDescription("A cybernetic augment concealed in the forearm, this cruel blade extends to slash at your enemies with inhuman speed.");

            gun.SetupSprite(null, "mantidaugment_idle_001", 8);
            gun.AddPassiveStatModifier(PlayerStats.StatType.Curse, 1f, StatModifier.ModifyMethod.ADDITIVE);
            gun.AddCurrentGunStatModifier(PlayerStats.StatType.MovementSpeed, 1f, StatModifier.ModifyMethod.ADDITIVE);
            gun.SetAnimationFPS(gun.shootAnimation, 20);
            gun.SetAnimationFPS(gun.reloadAnimation, 12);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);

            //GUN STATS
            gun.gunHandedness = GunHandedness.HiddenOneHanded;
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Automatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.BEAM;
            gun.reloadTime = 1f;
            gun.muzzleFlashEffects.type = VFXPoolType.None;
            gun.DefaultModule.cooldownTime = 0.01f;
            gun.DefaultModule.numberOfShotsInClip = 100;
            gun.barrelOffset.transform.localPosition = new Vector3(1.56f, 0.56f, 0f);
            gun.SetBaseMaxAmmo(2077);
            gun.ammo = 2077;
            gun.gunClass = GunClass.FULLAUTO;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            projectile.baseData.damage *= 2f;
            projectile.baseData.force *= 0.5f;
            projectile.sprite.renderer.enabled = false;
            NoCollideBehaviour nocollide = projectile.gameObject.AddComponent<NoCollideBehaviour>();
            nocollide.worksOnEnemies = true;
            nocollide.worksOnProjectiles = true;            
            projectile.specRigidbody.CollideWithTileMap = false;
            ProjectileSlashingBehaviour slashing = projectile.gameObject.AddComponent<ProjectileSlashingBehaviour>();
            slashing.DestroyBaseAfterFirstSlash = true;
            slashing.SlashDamageUsesBaseProjectileDamage = true;
            slashing.slashParameters = new SlashData();
            slashing.slashParameters.hitVFX = (PickupObjectDatabase.GetById(369) as Gun).DefaultModule.chargeProjectiles[0].Projectile.hitEffects.tileMapVertical;

            gun.DefaultModule.projectiles[0] = projectile;

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = "red_beam";

            gun.quality = PickupObject.ItemQuality.A;
            ETGMod.Databases.Items.Add(gun, null, "ANY");
            MantidAugmentID = gun.PickupObjectId;
        }
        public static int MantidAugmentID;
        public override void PostProcessProjectile(Projectile projectile)
        {
            if (projectile && projectile.Owner && projectile.Owner is PlayerController)
            {
                PlayerController player = projectile.Owner as PlayerController;
                ProjectileSlashingBehaviour slash = projectile.gameObject.GetComponent<ProjectileSlashingBehaviour>();
                if (slash)
                {
                    if (player.PlayerHasActiveSynergy("Bloodthirsty Blades") && UnityEngine.Random.value <= 0.07f)
                    {
                        slash.slashParameters.projInteractMode = SlashDoer.ProjInteractMode.DESTROY;
                    }
                }
            }
            base.PostProcessProjectile(projectile);
        }
        public MantidAugment()
        {

        }
    }
}

