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
    public class UnusCentum : GunBehaviour
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Unus Centum", "unuscentum");
            Game.Items.Rename("outdated_gun_mods:unus_centum", "nn:unus_centum");
            gun.gameObject.AddComponent<UnusCentum>();
            gun.SetShortDescription("Forget Me Not");
            gun.SetLongDescription("Has 100 shots. Cannot gain ammo." + "\n\nThe sidearm of a glistening sentinel, who came to the Gungeon not to flee his inevitable death, but to embrace it.");

            gun.SetupSprite(null, "unuscentum_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 12);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.CanGainAmmo = false;
            gun.DefaultModule.cooldownTime = 0.1f;
            gun.DefaultModule.numberOfShotsInClip = 10;
            gun.barrelOffset.transform.localPosition = new Vector3(1.37f, 0.68f, 0f);
            gun.SetBaseMaxAmmo(100);
            gun.gunClass = GunClass.PISTOL;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage *= 8f;
            projectile.baseData.speed *= 2f;
            projectile.baseData.range *= 10f;
            projectile.SetProjectileSpriteRight("demolitionist_projectile", 16, 7, false, tk2dBaseSprite.Anchor.MiddleCenter, 15, 6);

            projectile.transform.parent = gun.barrelOffset;

            gun.quality = PickupObject.ItemQuality.C;
            gun.encounterTrackable.EncounterGuid = "this is the Unus Centum";
            ETGMod.Databases.Items.Add(gun, null, "ANY");
            UnusCentumID = gun.PickupObjectId;
        }
        public override void OnPostFired(PlayerController player, Gun gun)
        {
            base.OnPostFired(player, gun);
            if (gun.ammo == 0)
            {
                player.inventory.DestroyCurrentGun();
            }
           gun.PreventNormalFireAudio = true;
            AkSoundEngine.PostEvent("Play_WPN_deck4rd_shot_01", gameObject);
        }
        public static int UnusCentumID;
        public UnusCentum()
        {

        }
    }
}