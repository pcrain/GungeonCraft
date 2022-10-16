using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using Alexandria.ItemAPI;

namespace NevernamedsItems
{
    public class IceBow : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Ice Bow", "icebow");
            Game.Items.Rename("outdated_gun_mods:ice_bow", "nn:ice_bow");
            var behav = gun.gameObject.AddComponent<IceBow>();
            gun.SetShortDescription("Arctery");
            gun.SetLongDescription("Freezes enemies."+"\n\nFerried to the Gungeon from a remote island on the edge of civilisation.");

            gun.SetupSprite(null, "icebow_idle_001", 8);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(12) as Gun, true, false);
            gun.SetAnimationFPS(gun.chargeAnimation, 3);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(8) as Gun).gunSwitchGroup;
            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Charged;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.DefaultModule.cooldownTime = 1f;
            gun.DefaultModule.angleVariance = 0;
            gun.muzzleFlashEffects.type = VFXPoolType.None;
            gun.DefaultModule.numberOfShotsInClip = 1;
            gun.barrelOffset.transform.localPosition = new Vector3(0.81f, 0.81f, 0f);
            gun.SetBaseMaxAmmo(100);
            gun.ammo = 100;

            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.chargeAnimation).wrapMode = tk2dSpriteAnimationClip.WrapMode.LoopSection;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.chargeAnimation).loopStart = 2;

            gun.carryPixelOffset = new IntVector2(10, 0);
            gun.gunClass = GunClass.ICE;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            projectile.baseData.damage = 17f;
            projectile.baseData.speed = 35f;
            projectile.baseData.range *= 2f;
            projectile.AppliesFreeze = true;
            projectile.FreezeApplyChance = 1;
            projectile.freezeEffect = StaticStatusEffects.chaosBulletsFreeze;
            PierceProjModifier pierce = projectile.gameObject.GetOrAddComponent<PierceProjModifier>();
            pierce.penetration++;
            pierce.penetratesBreakables = true;
            projectile.SetProjectileSpriteRight("icebow_projectile", 15, 5, true, tk2dBaseSprite.Anchor.MiddleCenter, 14, 4);

            ProjectileModule.ChargeProjectile chargeProj = new ProjectileModule.ChargeProjectile
            {
                Projectile = projectile,
                ChargeTime = 1,
            };
            gun.DefaultModule.chargeProjectiles = new List<ProjectileModule.ChargeProjectile> { chargeProj };

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("Ice Bow Arrows", "NevernamedsItems/Resources/CustomGunAmmoTypes/icebow_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/icebow_clipempty");

            gun.quality = PickupObject.ItemQuality.B;
            ETGMod.Databases.Items.Add(gun, null, "ANY");
            gun.SetTag("arrow_bolt_weapon");
            IceBowID = gun.PickupObjectId;
        }
        public static int IceBowID;
        public IceBow()
        {

        }     
    }
}