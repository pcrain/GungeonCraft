using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using Alexandria.ItemAPI;
using Alexandria.Misc;

namespace NevernamedsItems
{
    public class GatlingGun : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Gatling Gun", "gatlinggun");
            Game.Items.Rename("outdated_gun_mods:gatling_gun", "nn:gatling_gun");
            gun.gameObject.AddComponent<GatlingGun>();
            gun.SetShortDescription("Grandaddy");
            gun.SetLongDescription("An old rapid-fire weapon made obsolete by the more modern minigun.");

            gun.SetupSprite(null, "gatlinggun_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 16);

            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(84) as Gun).gunSwitchGroup;
            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(84) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Automatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1.5f;
            gun.DefaultModule.cooldownTime = 0.1f;
            gun.DefaultModule.angleVariance = 5;
            gun.DefaultModule.numberOfShotsInClip = 16;
            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(84) as Gun).muzzleFlashEffects;
            gun.barrelOffset.transform.localPosition = new Vector3(2.43f, 0.37f, 0f);
            gun.SetBaseMaxAmmo(240);
            gun.ammo = 240;
            gun.gunClass = GunClass.FULLAUTO;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);

            gun.DefaultModule.projectiles[0] = projectile;

            gun.usesContinuousFireAnimation = true;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).wrapMode = tk2dSpriteAnimationClip.WrapMode.LoopSection;
            gun.GetComponent<tk2dSpriteAnimator>().GetClipByName(gun.shootAnimation).loopStart = 0;

            gun.quality = PickupObject.ItemQuality.C;
            ETGMod.Databases.Items.Add(gun, false, "ANY");

            GatlingGunID = gun.PickupObjectId;
        }
        public static int GatlingGunID;
        public override void PostProcessProjectile(Projectile projectile)
        {
            if (projectile.ProjectilePlayerOwner() && projectile.ProjectilePlayerOwner().PlayerHasActiveSynergy("Gattlesnake"))
            {
                if (UnityEngine.Random.value <= 0.25f)
                {
                    projectile.StartCoroutine(this.DoShadowDelayed(projectile));
                }
            }
            base.PostProcessProjectile(projectile);
        }
        private IEnumerator DoShadowDelayed(Projectile projectile)
        {
            yield return null;
            projectile.SpawnChainedShadowBullets(1, 0.05f);
            yield break;
        }
        public GatlingGun()
        {

        }
    }
}
