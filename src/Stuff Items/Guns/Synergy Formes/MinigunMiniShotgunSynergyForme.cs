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

    public class MinigunMiniShotgunSynergyForme : GunBehaviour
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Mini Shotgun", "minishotgun");
            Game.Items.Rename("outdated_gun_mods:mini_shotgun", "nn:mini_gun+mini_shotgun");
            gun.gameObject.AddComponent<MinigunMiniShotgunSynergyForme>();
            gun.SetShortDescription("Tiny Toys");
            gun.SetLongDescription("This shotgun is the size of my self confidence." + "\n\nIf you're reading this, you're a cheatsy haxor.");
            gun.SetupSprite(null, "minishotgun_idle_001", 8);
            gun.SetAnimationFPS(gun.shootAnimation, 16);
            for (int i = 0; i < 3; i++)
            {
                gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);
            }

            //GUN STATS
            foreach (ProjectileModule mod in gun.Volley.projectiles)
            {
                mod.ammoCost = 1;
                mod.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
                mod.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
                mod.cooldownTime = 0.4f;
                mod.angleVariance = 10f;
                mod.numberOfShotsInClip = 4;
                Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(mod.projectiles[0]);
                mod.projectiles[0] = projectile;
                projectile.gameObject.SetActive(false);
                FakePrefab.MarkAsFakePrefab(projectile.gameObject);
                UnityEngine.Object.DontDestroyOnLoad(projectile);
                projectile.baseData.damage = 7f;
                projectile.AdditionalScaleMultiplier *= 0.5f;
                if (mod != gun.DefaultModule) { mod.ammoCost = 0; }
                projectile.transform.parent = gun.barrelOffset;
            }
            gun.reloadTime = 1.1f;
            gun.SetBaseMaxAmmo(200);
            gun.quality = PickupObject.ItemQuality.EXCLUDED;
            gun.encounterTrackable.EncounterGuid = "this is the Mini Shotgun Synergy Form";
            ETGMod.Databases.Items.Add(gun, null, "ANY");
            gun.barrelOffset.transform.localPosition = new Vector3(0.68f, 0.18f, 0f);
            MiniShotgunID = gun.PickupObjectId;
        }
        public static int MiniShotgunID;
        public override void OnPostFired(PlayerController player, Gun gun)
        {
            gun.PreventNormalFireAudio = true;
            AkSoundEngine.PostEvent("Play_WPN_smileyrevolver_shot_01", gameObject);
        }
        public MinigunMiniShotgunSynergyForme()
        {

        }
    }
}