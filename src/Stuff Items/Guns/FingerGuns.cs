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

    public class FingerGuns : GunBehaviour
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Finger Guns", "fingerguns");
            Game.Items.Rename("outdated_gun_mods:finger_guns", "nn:finger_guns");
            gun.gameObject.AddComponent<FingerGuns>();
            gun.SetShortDescription("Ayyyyy");
            gun.SetLongDescription("A universal gesture, laden with rogueish charm." + "\n\nEven this simple hand movement is enough for use in combat in the gungeon.");
            gun.SetupSprite(null, "fingerguns_idle_001", 8);
            gun.SetAnimationFPS(gun.shootAnimation, 15);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 0.8f;
            gun.DefaultModule.cooldownTime = 0.1f;
            gun.gunHandedness = GunHandedness.HiddenOneHanded;
            gun.DefaultModule.numberOfShotsInClip = 5;
            gun.barrelOffset.transform.localPosition = new Vector3(0.81f, 0.43f, 0f);
            gun.SetBaseMaxAmmo(250);
            gun.gunClass = GunClass.PISTOL;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.speed *= 0.9f;
            projectile.baseData.damage *= 1.4f;
            projectile.SetProjectileSpriteRight("fingerguns_projectile", 8, 3, true, tk2dBaseSprite.Anchor.MiddleCenter, 7, 2);
            projectile.sprite.renderer.enabled = false;

            projectile.transform.parent = gun.barrelOffset;

            gun.quality = PickupObject.ItemQuality.D;
            gun.encounterTrackable.EncounterGuid = "this is the Finger Guns";
            ETGMod.Databases.Items.Add(gun, null, "ANY");

        }
        public override void Update()
        {
            PlayerController player = gun.CurrentOwner as PlayerController;
            if (player.PlayerHasActiveSynergy("Polydactyly"))
            {
                if (!hasPolydactylySynergyAlready)
                {
                    gun.SetBaseMaxAmmo(350);
                    gun.DefaultModule.numberOfShotsInClip = 6;
                    hasPolydactylySynergyAlready = true;
                }
            }
            else
            {
                if (hasPolydactylySynergyAlready)
                {
                    gun.SetBaseMaxAmmo(250);
                    gun.DefaultModule.numberOfShotsInClip = 5;
                    hasPolydactylySynergyAlready = false;
                }
            }
        }
        public override void PostProcessProjectile(Projectile projectile)
        {
            base.PostProcessProjectile(projectile);
            PlayerController player = gun.CurrentOwner as PlayerController;
            if (player.PlayerHasActiveSynergy("Polydactyly")) projectile.baseData.damage *= 1.142857f;
        }
        private bool hasPolydactylySynergyAlready = false;
        public FingerGuns()
        {

        }
    }
}