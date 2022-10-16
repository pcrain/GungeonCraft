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

    public class HandMortar : GunBehaviour
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Hand Mortar", "handmortar");
            Game.Items.Rename("outdated_gun_mods:hand_mortar", "nn:hand_mortar");
            gun.gameObject.AddComponent<HandMortar>();
            gun.SetShortDescription("Kingdom Come");
            gun.SetLongDescription("The classy and classical predecessor to the modern grenade launchers, some old grenadiers still swear by their effectiveness.");

            gun.SetupSprite(null, "handmortar_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 15);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 2f;
            gun.DefaultModule.angleVariance = 10;
            gun.DefaultModule.cooldownTime = 1f;
            gun.DefaultModule.numberOfShotsInClip = 3;
            gun.barrelOffset.transform.localPosition = new Vector3(2.81f, 1.0f, 0f);
            gun.SetBaseMaxAmmo(70);
            gun.gunClass = GunClass.EXPLOSIVE;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage *= 6f;

            BounceProjModifier Bouncing = projectile.gameObject.GetOrAddComponent<BounceProjModifier>();
            Bouncing.numberOfBounces = 2;
            ExplosiveModifier explosiveModifier = projectile.gameObject.AddComponent<ExplosiveModifier>();
            explosiveModifier.doExplosion = true;
            explosiveModifier.explosionData = HandMortarExplosion;
            projectile.baseData.speed *= 0.6f;

            projectile.baseData.range *= 0.8f;
            projectile.transform.parent = gun.barrelOffset;
            projectile.SetProjectileSpriteRight("handmortar_projectile", 8, 8, false, tk2dBaseSprite.Anchor.MiddleCenter, 8, 8);

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("HandMortar Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/handmortar_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/handmortar_clipempty");

            gun.quality = PickupObject.ItemQuality.A;
            gun.encounterTrackable.EncounterGuid = "this is the Hand Mortar";
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            ID = gun.PickupObjectId;
        }
        public static int ID;
        static ExplosionData bigExplosion = GameManager.Instance.Dungeon.sharedSettingsPrefab.DefaultExplosionData;
        static ExplosionData HandMortarExplosion = new ExplosionData()
        {
            effect = bigExplosion.effect,
            ignoreList = bigExplosion.ignoreList,
            ss = bigExplosion.ss,
            damageRadius = 2.5f,
            damageToPlayer = 0f,
            doDamage = true,
            damage = 50,
            doDestroyProjectiles = true,
            doForce = true,
            debrisForce = 30f,
            preventPlayerForce = true,
            explosionDelay = 0.1f,
            usesComprehensiveDelay = false,
            doScreenShake = true,
            playDefaultSFX = true,
        };
        public override void PostProcessProjectile(Projectile projectile)
        {
            base.PostProcessProjectile(projectile);
            PlayerController playerController = projectile.Owner as PlayerController;
            if (playerController.PlayerHasActiveSynergy("Good Old Guns"))
            {
                projectile.baseData.damage *= 1.5f;
                projectile.baseData.range *= 3f;
            }
            if (playerController.PlayerHasActiveSynergy("The Classics"))
            {
                ExplosiveModifier explosion = projectile.gameObject.GetComponent<ExplosiveModifier>();
                if (explosion)
                {
                    explosion.explosionData.damageRadius *= 2f;
                }
            }
        }
        public override void OnPostFired(PlayerController player, Gun gun)
        {
            gun.PreventNormalFireAudio = true;
            AkSoundEngine.PostEvent("Play_WPN_seriouscannon_shot_01", gameObject);

        }
        public HandMortar()
        {

        }
    }
}
