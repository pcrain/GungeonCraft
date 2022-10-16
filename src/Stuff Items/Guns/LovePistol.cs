
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

    public class LovePistol : GunBehaviour
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Love Pistol", "lovepistol");
            Game.Items.Rename("outdated_gun_mods:love_pistol", "nn:love_pistol");
            gun.gameObject.AddComponent<LovePistol>();
            gun.SetShortDescription(";)");
            gun.SetLongDescription("A low powered pistol, formerly kept in the back pocket of Hespera, the Pride of Venus, for times of need.");
            gun.SetupSprite(null, "lovepistol_idle_001", 8);
            gun.SetAnimationFPS(gun.shootAnimation, 15);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 0.8f;
            gun.DefaultModule.cooldownTime = 0.2f;
            gun.DefaultModule.numberOfShotsInClip = 9;
            gun.barrelOffset.transform.localPosition = new Vector3(1.12f, 0.62f, 0f);
            gun.SetBaseMaxAmmo(400);
            gun.gunClass = GunClass.CHARM;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.speed *= 0.9f;
            projectile.baseData.damage *= 0.8f;
            LovePistolCharmBehaviour charmBehaviour = projectile.gameObject.AddComponent<LovePistolCharmBehaviour>();
            projectile.SetProjectileSpriteRight("lovepistol_projectile", 7, 6, true, tk2dBaseSprite.Anchor.MiddleCenter, 7, 6);

            projectile.transform.parent = gun.barrelOffset;
            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("Love Pistol Hearts", "NevernamedsItems/Resources/CustomGunAmmoTypes/lovepistol_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/lovepistol_clipempty");
            gun.quality = PickupObject.ItemQuality.B;
            gun.encounterTrackable.EncounterGuid = "this is the Love Pistol";
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            gun.AddToSubShop(ItemBuilder.ShopType.Cursula);
            LovePistolID = gun.PickupObjectId;
        }
        public static int LovePistolID;
        public override void PostProcessProjectile(Projectile projectile)
        {
            PlayerController playerController = projectile.Owner as PlayerController;
            if (playerController.PlayerHasActiveSynergy("Toxic Love")) projectile.baseData.damage *= 2;
            base.PostProcessProjectile(projectile);
        }
        public LovePistol()
        {

        }
    }
    public class LovePistolCharmBehaviour : MonoBehaviour
    {
        public LovePistolCharmBehaviour()
        {

        }
        private void Awake()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            m_projectile.OnHitEnemy += this.OnHitEnemy;
        }
        GameActorCharmEffect charmEffect = Gungeon.Game.Items["charming_rounds"].GetComponent<BulletStatusEffectItem>().CharmModifierEffect;

        private void OnHitEnemy(Projectile bullet, SpeculativeRigidbody enemy, bool fatal)
        {
            if (enemy && enemy.aiActor && enemy.gameActor)
            {
                PlayerController playerController = this.m_projectile.Owner as PlayerController;
                if (playerController)
                {
                    if (playerController.PlayerHasActiveSynergy("Everlasting Love"))
                    {
                        enemy.aiActor.ApplyEffect(GameManager.Instance.Dungeon.sharedSettingsPrefab.DefaultPermanentCharmEffect, 1f, null);
                    }
                    else
                    {
                        enemy.gameActor.ApplyEffect(this.charmEffect, 1f, null);
                    }
                }
            }

        }
        private Projectile m_projectile;
    }
}