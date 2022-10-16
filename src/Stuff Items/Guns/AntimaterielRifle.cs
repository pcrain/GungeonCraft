using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using ItemAPI;
using SaveAPI;

namespace NevernamedsItems
{

    public class AntimaterielRifle : GunBehaviour
    {


        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Antimateriel Rifle", "antimaterielrifle");
            Game.Items.Rename("outdated_gun_mods:antimateriel_rifle", "nn:antimateriel_rifle");
            gun.gameObject.AddComponent<AntimaterielRifle>();
            gun.SetShortDescription("Shreddin' Vapours");
            gun.SetLongDescription("Used in rebel attacks on remote Hegemony of Man outposts, this high-tech tool of destruction is geared to take out heavy targets."+"\n\nIgnores boss DPS cap.");

            gun.SetupSprite(null, "antimaterielrifle_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 15);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);
            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(334) as Gun).muzzleFlashEffects;

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Automatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 2f;
            gun.DefaultModule.cooldownTime = 0.04f;
            gun.muzzleFlashEffects.type = VFXPoolType.None;
            gun.DefaultModule.numberOfShotsInClip = 55;
            gun.barrelOffset.transform.localPosition = new Vector3(2.75f, 0.56f, 0f);
            gun.SetBaseMaxAmmo(720);
            gun.gunClass = GunClass.FULLAUTO;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage *= 1f;
            projectile.baseData.speed *= 1.2f;
            projectile.ignoreDamageCaps = true;
            projectile.pierceMinorBreakables = true;
            PierceProjModifier orAddComponent = projectile.gameObject.GetOrAddComponent<PierceProjModifier>();
            orAddComponent.penetratesBreakables = true;
            orAddComponent.penetration++;
            projectile.SetProjectileSpriteRight("antimaterielrifle_projectile", 15, 7, true, tk2dBaseSprite.Anchor.MiddleCenter, 15, 7);

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = "Thinline Bullets";

            gun.quality = PickupObject.ItemQuality.S;
            ETGMod.Databases.Items.Add(gun, null, "ANY");
            gun.AddToSubShop(ItemBuilder.ShopType.Trorc);
            AntimaterielRifleID = gun.PickupObjectId;
            gun.SetupUnlockOnCustomFlag(CustomDungeonFlags.PURCHASED_ANTIMATERIELRIFLE, true);
            gun.AddItemToTrorcMetaShop(28);
        }
        public static int AntimaterielRifleID;
        public override void OnPostFired(PlayerController player, Gun gun)
        {          
            gun.PreventNormalFireAudio = true;
            AkSoundEngine.PostEvent("Play_WPN_plasmarifle_shot_01", gameObject);
        }
        public AntimaterielRifle()
        {

        }
    }
}