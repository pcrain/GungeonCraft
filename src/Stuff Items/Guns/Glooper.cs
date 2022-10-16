using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using ItemAPI;
using System.Reflection;

namespace NevernamedsItems
{
    public class Glooper : GunBehaviour
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Glooper", "glooper");
            Game.Items.Rename("outdated_gun_mods:glooper", "nn:glooper");
            gun.gameObject.AddComponent<Glooper>();
            gun.SetShortDescription("Slippery");
            gun.SetLongDescription("Made of strange soapy goo, this weapon slips out of your hands when reloaded.");

            gun.SetupSprite(null, "glooper_idle_001", 8);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(599) as Gun).gunSwitchGroup;

            gun.SetAnimationFPS(gun.shootAnimation, 12);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 0f;
            gun.DefaultModule.angleVariance = 12;
            gun.DefaultModule.cooldownTime = 0.2f;
            gun.DefaultModule.numberOfShotsInClip = 10;
            gun.muzzleFlashEffects.type = VFXPoolType.None;
            gun.barrelOffset.transform.localPosition = new Vector3(1.06f, 0.93f, 0f);
            gun.SetBaseMaxAmmo(200);
            gun.ammo = 200;
            gun.gunClass = GunClass.SHITTY;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            projectile.baseData.damage = 10f;
            projectile.baseData.speed *= 1f;
            projectile.baseData.range *= 1f;
            projectile.baseData.force *= 2.5f;

            projectile.AnimateProjectile(new List<string> {
                "glooperproj_1",
                "glooperproj_2",
                "glooperproj_1",
                "glooperproj_3",
            }, 8, true, new List<IntVector2> {
                new IntVector2(7, 7), //1
                new IntVector2(5, 9), //2            
                new IntVector2(7, 7), //3
                new IntVector2(9, 5), //4
            }, AnimateBullet.ConstructListOfSameValues(false, 4), AnimateBullet.ConstructListOfSameValues(tk2dBaseSprite.Anchor.MiddleCenter, 4), AnimateBullet.ConstructListOfSameValues(true, 4), AnimateBullet.ConstructListOfSameValues(false, 4),
            AnimateBullet.ConstructListOfSameValues<Vector3?>(null, 4), AnimateBullet.ConstructListOfSameValues<IntVector2?>(null, 4), AnimateBullet.ConstructListOfSameValues<IntVector2?>(null, 4), AnimateBullet.ConstructListOfSameValues<Projectile>(null, 4));
            gun.DefaultModule.projectiles[0] = projectile;

            gun.quality = PickupObject.ItemQuality.D;
            ETGMod.Databases.Items.Add(gun, null, "ANY");
            GlooperID = gun.PickupObjectId;
            gun.AddToSubShop(ItemBuilder.ShopType.Goopton);
        }
        public static int GlooperID;
        public override void OnReloadPressed(PlayerController player, Gun gun, bool bSOMETHING)
        {
            base.OnReloadPressed(player, gun, bSOMETHING);
            if (player.CurrentGun && player.CurrentGun.PickupObjectId == GlooperID)
            {
                ForceThrow(player);
            }
        }
        private void ForceThrow(PlayerController user)
        {
            user.CurrentGun.PrepGunForThrow();
            typeof(Gun).GetField("m_prepThrowTime", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(user.CurrentGun, 999);
            user.CurrentGun.CeaseAttack();
        }
        public Glooper()
        {

        }
    }
}
