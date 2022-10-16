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
    public class VariableGun : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Variable", "variable");
            Game.Items.Rename("outdated_gun_mods:variable", "nn:variable");
            gun.gameObject.AddComponent<VariableGun>();
            gun.SetShortDescription("Boollet Value");
            gun.SetLongDescription("Deals double damage if the target you're shooting is different to the last enemy you shot." + "\n\nFavoured sidearm of a famous programmer, who came to the gungeon after being driven insane by the inane questions asked of him by other people.");

            gun.SetupSprite(null, "variable_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 10);

            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(35) as Gun).gunSwitchGroup;
            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Ordered;
            gun.reloadTime = 1f;
            gun.DefaultModule.cooldownTime = 0.1f;
            gun.DefaultModule.numberOfShotsInClip = 6;
            gun.barrelOffset.transform.localPosition = new Vector3(1.43f, 0.81f, 0f);
            gun.SetBaseMaxAmmo(256);
            gun.ammo = 256;
            gun.gunClass = GunClass.PISTOL;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            projectile.baseData.damage *= 1.6f;
            projectile.baseData.force *= 1.2f;
            projectile.hitEffects.alwaysUseMidair = true;
            projectile.hitEffects.overrideMidairDeathVFX = EasyVFXDatabase.WhiteCircleVFX;
            projectile.SetProjectileSpriteRight("variable1_projectile", 10, 10, true, tk2dBaseSprite.Anchor.MiddleCenter,8, 8);

            Projectile projectile2 = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(86) as Gun).DefaultModule.projectiles[0]);
            projectile2.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile2.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile2);
            projectile2.baseData.damage *= 1.6f;
            projectile2.baseData.force *= 1.2f;
            projectile2.hitEffects.alwaysUseMidair = true;
            projectile2.hitEffects.overrideMidairDeathVFX = EasyVFXDatabase.WhiteCircleVFX;
            projectile2.SetProjectileSpriteRight("variable2_projectile", 10, 10, true, tk2dBaseSprite.Anchor.MiddleCenter, 8, 8);

            gun.DefaultModule.projectiles[0] = projectile;
            gun.DefaultModule.projectiles.Add(projectile2);
            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("Variable Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/variable_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/variable_clipempty");
            gun.quality = PickupObject.ItemQuality.B;
            ETGMod.Databases.Items.Add(gun, null, "ANY");
        }
        public static AIActor LastHitEnemy = null;
        public static AIActor LegacyHitEnemy = null;
        protected override void OnPickedUpByPlayer(PlayerController player)
        {
            if (!everPickedUpByPlayer)
            {
                LastHitEnemy = null;
                LegacyHitEnemy = null;
            }
            base.OnPickedUpByPlayer(player);
        }
        private void HandlePreCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
        {
            if (otherRigidbody && otherRigidbody.aiActor && otherRigidbody.healthHaver && myRigidbody.projectile && myRigidbody.projectile.Owner is PlayerController)
            {
                if ((myRigidbody.projectile.Owner as PlayerController).PlayerHasActiveSynergy("Backwards Compatible") && otherRigidbody.aiActor != LastHitEnemy && otherRigidbody.aiActor != LegacyHitEnemy && LastHitEnemy != LegacyHitEnemy)
                {
                    float curDamage = myRigidbody.projectile.baseData.damage;
                    myRigidbody.projectile.baseData.damage *= 3f;
                    GameManager.Instance.StartCoroutine(this.ChangeProjectileDamage(myRigidbody.projectile, curDamage, true));
                }
                else if (otherRigidbody.aiActor != LastHitEnemy)
                {
                myRigidbody.projectile.baseData.damage *= 2f;
                GameManager.Instance.StartCoroutine(this.ChangeProjectileDamage(myRigidbody.projectile, 0.5f, false));
                }
            }
        }
        private void OnHitEnemy(Projectile bullet, SpeculativeRigidbody enemy, bool fatal)
        {
            if (enemy.aiActor != null)
            {
                if (LastHitEnemy != null)
                {
                    LegacyHitEnemy = LastHitEnemy;
                }
                LastHitEnemy = enemy.aiActor;   
            }
        }
        private IEnumerator ChangeProjectileDamage(Projectile bullet, float postmultiplier, bool directSet)
        {
            yield return new WaitForSeconds(0.1f);
            if (bullet != null)
            {
                if (directSet) bullet.baseData.damage = postmultiplier;
                else bullet.baseData.damage *= postmultiplier;
            }
            yield break;
        }
        public override void PostProcessProjectile(Projectile projectile)
        {
            if (projectile.Owner is PlayerController)
            {
                PlayerController owner = projectile.Owner as PlayerController;
                projectile.specRigidbody.OnPreRigidbodyCollision += this.HandlePreCollision;
                projectile.OnHitEnemy += this.OnHitEnemy;
            }
            base.PostProcessProjectile(projectile);
        }
        public VariableGun()
        {

        }
    }
}