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
    public class Wolfgun : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Wolfgun", "wolfgun");
            Game.Items.Rename("outdated_gun_mods:wolfgun", "nn:doggun+discord_and_rhyme");
            gun.gameObject.AddComponent<Corgun>();
            gun.SetShortDescription("Bork Bork BORK!");
            gun.SetLongDescription("BORKBORKBORKBORKBORKBORKBORK"+"\n\nAlso, if you're reading this, you're a BORKING haxor.");

            gun.SetupSprite(null, "wolfgun_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 12);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 0.8f;
            gun.DefaultModule.cooldownTime = 0.4f;
            gun.muzzleFlashEffects.type = VFXPoolType.None;
            gun.DefaultModule.numberOfShotsInClip = 5;
            gun.barrelOffset.transform.localPosition = new Vector3(0.75f, 0.56f, 0f);
            gun.SetBaseMaxAmmo(400);

            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage *= 1.6f;
            projectile.baseData.speed *= 1f;
            projectile.baseData.range *= 10f;

            projectile.pierceMinorBreakables = true;
            //projectile.shouldRotate = true;
            PierceProjModifier orAddComponent = projectile.gameObject.GetOrAddComponent<PierceProjModifier>();
            orAddComponent.penetratesBreakables = true;
            orAddComponent.penetration = 5;
            BounceProjModifier Bouncing = projectile.gameObject.GetOrAddComponent<BounceProjModifier>();
            Bouncing.numberOfBounces = 5;

            HomingModifier homing = projectile.gameObject.AddComponent<HomingModifier>();
            homing.AngularVelocity = 140f;
            homing.HomingRadius = 100f;

            projectile.SetProjectileSpriteRight("wolfgun_projectile", 18, 12, false, tk2dBaseSprite.Anchor.MiddleCenter, 17, 11);

            projectile.transform.parent = gun.barrelOffset;

            gun.PreventNormalFireAudio = true;
            gun.OverrideNormalFireAudioEvent = "Play_PET_dog_bark_02";

            gun.quality = PickupObject.ItemQuality.EXCLUDED;
            gun.encounterTrackable.EncounterGuid = "this is the Wolfgun synergy";
            ETGMod.Databases.Items.Add(gun, null, "ANY");
            WolfgunID = gun.PickupObjectId;
        }
        public static int WolfgunID;
        public override void OnReload(PlayerController player, Gun gun)
        {
            base.OnReload(player, gun);
            AkSoundEngine.PostEvent("Stop_WPN_All", base.gameObject);
            AkSoundEngine.PostEvent("Play_PET_dog_bark_02", base.gameObject);
        }
        public Wolfgun()
        {

        }
    }
}