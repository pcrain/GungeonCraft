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

    public class Lantaka : GunBehaviour
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Lantaka", "lantaka");
            Game.Items.Rename("outdated_gun_mods:lantaka", "nn:lantaka");
            gun.gameObject.AddComponent<Lantaka>();
            gun.SetShortDescription("Head on a Swivel");
            gun.SetLongDescription("Used once upon a time by ships to protect against pirates, this ancient gun sat in the back room of a museum for many years until a daring heist saw it make it's way to the Gungeon.");

            gun.SetupSprite(null, "lantaka_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 15);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(49) as Gun).gunSwitchGroup;
            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(9) as Gun).muzzleFlashEffects;

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.DefaultModule.cooldownTime = 1f;
            gun.DefaultModule.numberOfShotsInClip = 1;
            gun.barrelOffset.transform.localPosition = new Vector3(2.56f, 0.81f, 0f);
            gun.SetBaseMaxAmmo(150);
            gun.gunClass = GunClass.RIFLE;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.transform.parent = gun.barrelOffset;
            projectile.baseData.speed *= 2f;
            projectile.baseData.range *= 2f;
            projectile.baseData.damage *= 4f;
            projectile.BossDamageMultiplier *= 1.5f;
            projectile.BlackPhantomDamageMultiplier *= 2f;
            PierceProjModifier Piercing = projectile.gameObject.GetOrAddComponent<PierceProjModifier>();
            Piercing.penetratesBreakables = true;
            Piercing.penetration += 10;
            BounceProjModifier Bouncing = projectile.gameObject.GetOrAddComponent<BounceProjModifier>();
            Bouncing.numberOfBounces = 2;
            projectile.SetProjectileSpriteRight("lantaka_projectile", 15, 15, false, tk2dBaseSprite.Anchor.MiddleCenter, 14, 14);

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("Lantaka Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/lantaka_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/lantaka_clipempty");

            gun.quality = PickupObject.ItemQuality.C;
            gun.encounterTrackable.EncounterGuid = "this is the Lantaka";
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            LantakaID = gun.PickupObjectId;
        }
        public static int LantakaID;
        public override void PostProcessProjectile(Projectile projectile)
        {
            base.PostProcessProjectile(projectile);
            PlayerController player = projectile.Owner as PlayerController;
            if (player.PlayerHasActiveSynergy("Brothers in Copper")) projectile.OnHitEnemy += this.applyFire;
        }
        GameActorFireEffect fireEffect = Gungeon.Game.Items["hot_lead"].GetComponent<BulletStatusEffectItem>().FireModifierEffect;
        private void applyFire(Projectile bullet, SpeculativeRigidbody enemy, bool fatal)
        {
            enemy.gameActor.ApplyEffect(this.fireEffect, 1f, null);
        }
        public Lantaka()
        {

        }
    }
}