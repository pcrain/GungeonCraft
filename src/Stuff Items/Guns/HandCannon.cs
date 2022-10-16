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

    public class HandCannon : GunBehaviour
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Hand Cannon", "handcannon");
            Game.Items.Rename("outdated_gun_mods:hand_cannon", "nn:hand_cannon");
            gun.gameObject.AddComponent<HandCannon>();
            gun.SetShortDescription("Protogun");
            gun.SetLongDescription("The earliest recorded type of real firearm. Though it is little more than a small cannon on a stick, it can spit some damaging shrapnel.");

            gun.SetupSprite(null, "handcannon_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 15);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 2.5f;
            gun.DefaultModule.cooldownTime = 1f;
            gun.muzzleFlashEffects.type = VFXPoolType.None;
            gun.DefaultModule.numberOfShotsInClip = 1;
            gun.barrelOffset.transform.localPosition = new Vector3(2.87f, 0.5f, 0f);
            gun.SetBaseMaxAmmo(190);
            gun.gunClass = GunClass.SHITTY;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage *= 7f;
            PierceProjModifier Piercing = projectile.gameObject.GetOrAddComponent<PierceProjModifier>();
            Piercing.penetratesBreakables = true;
            Piercing.penetration += 10;
            BounceProjModifier Bouncing = projectile.gameObject.GetOrAddComponent<BounceProjModifier>();
            Bouncing.numberOfBounces = 10;
            projectile.baseData.range *= 0.5f;
            projectile.transform.parent = gun.barrelOffset;
            projectile.SetProjectileSpriteRight("handcannon_projectile", 17, 15, false, tk2dBaseSprite.Anchor.MiddleCenter, 15, 13);

            projectile.hitEffects.overrideMidairDeathVFX = (PickupObjectDatabase.GetById(37) as Gun).DefaultModule.chargeProjectiles[0].Projectile.hitEffects.overrideMidairDeathVFX;
            projectile.hitEffects.alwaysUseMidair = true;

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("HandCannon Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/handcannon_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/handcannon_clipempty");
            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(53) as Gun).muzzleFlashEffects;
            gun.quality = PickupObject.ItemQuality.D;
            gun.encounterTrackable.EncounterGuid = "this is the Hand Cannon";
            ETGMod.Databases.Items.Add(gun, null, "ANY");
            HandCannonID = gun.PickupObjectId;
        }
        public static int HandCannonID;
        public override void PostProcessProjectile(Projectile projectile)
        {
            base.PostProcessProjectile(projectile);
            PlayerController playerController = projectile.Owner as PlayerController;
            projectile.specRigidbody.OnPreRigidbodyCollision += this.HandlePierce;
            if (playerController.PlayerHasActiveSynergy("Good Old Guns"))
            {
                projectile.baseData.damage *= 1.5f;
                projectile.baseData.range *= 3f;
            }
        }
        private void HandlePierce(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
        {
            FieldInfo field = typeof(Projectile).GetField("m_hasPierced", BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(myRigidbody.projectile, false);
        }

        public override void OnPostFired(PlayerController player, Gun gun)
        {
            gun.PreventNormalFireAudio = true;
            AkSoundEngine.PostEvent("Play_WPN_seriouscannon_shot_01", gameObject);

        }
        public HandCannon()
        {

        }
    }
}
