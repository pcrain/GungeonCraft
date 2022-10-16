using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using ItemAPI;
using Alexandria.Misc;

namespace NevernamedsItems
{
    public class CopperSidearm : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Copper Sidearm", "coppersidearm");
            Game.Items.Rename("outdated_gun_mods:copper_sidearm", "nn:copper_sidearm");
            gun.gameObject.AddComponent<CopperSidearm>();
            gun.SetShortDescription("Conductive");
            gun.SetLongDescription("A prime conductor of electricity, the bullets from this gun connect back to the wielder by an electric arc."+"\n\nSmells faintly of wax.");

            gun.SetupSprite(null, "coppersidearm_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 16);
            gun.SetAnimationFPS(gun.reloadAnimation, 1);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(545) as Gun).gunSwitchGroup;

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.DefaultModule.cooldownTime = 0.2f;
            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(97) as Gun).muzzleFlashEffects;
            gun.DefaultModule.numberOfShotsInClip = 6;
            gun.barrelOffset.transform.localPosition = new Vector3(1.56f, 0.62f, 0f);
            gun.SetBaseMaxAmmo(200);
            gun.gunClass = GunClass.PISTOL;
            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(23) as Gun).muzzleFlashEffects;

            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage = 6f;
            projectile.baseData.speed *= 0.5f;
            projectile.baseData.range *= 10f;
            projectile.SetProjectileSpriteRight("coppersidearm_projectile", 10, 10, true, tk2dBaseSprite.Anchor.MiddleCenter, 8, 8);

            GameObject LinkVFXPrefab = FakePrefab.Clone(Game.Items["shock_rounds"].GetComponent<ComplexProjectileModifier>().ChainLightningVFX);
            FakePrefab.MarkAsFakePrefab(LinkVFXPrefab);
            UnityEngine.Object.DontDestroyOnLoad(LinkVFXPrefab);
            OwnerConnectLightningModifier litening = projectile.gameObject.AddComponent<OwnerConnectLightningModifier>();
            litening.linkPrefab = LinkVFXPrefab;

            PierceProjModifier piercing = projectile.gameObject.AddComponent < PierceProjModifier>();
            piercing.penetration = 1;

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("CopperSidearm Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/coppersidearm_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/coppersidearm_clipempty");

            gun.quality = PickupObject.ItemQuality.C;
            ETGMod.Databases.Items.Add(gun, false, "ANY");

            CopperSidearmID = gun.PickupObjectId;
        }
        public override void PostProcessProjectile(Projectile projectile)
        {
            if (projectile && projectile.ProjectilePlayerOwner())
            {
                if (projectile.ProjectilePlayerOwner().PlayerHasActiveSynergy("Cop-Out"))
                {
                    if (UnityEngine.Random.value <= 0.25f)
                    {
                        projectile.statusEffectsToApply.Add(StaticStatusEffects.hotLeadEffect);
                    }
                }
            }
            base.PostProcessProjectile(projectile);
        }
        public static int CopperSidearmID;
    }
}