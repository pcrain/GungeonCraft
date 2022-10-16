using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Reflection;
using Gungeon;
using MonoMod;
using UnityEngine;
using Alexandria.ItemAPI;

namespace NevernamedsItems
{

    public class Kalashnirang : GunBehaviour
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Kalashnirang", "kalashnirang");
            Game.Items.Rename("outdated_gun_mods:kalashnirang", "nn:kalashnirang");
            gun.gameObject.AddComponent<Kalashnirang>();
            gun.SetShortDescription("What We Do Here");
            gun.SetLongDescription("Rapid-fires boomeraning bullets." + "\n\nFound among the remnants of an old abandoned circus in the Gungeon's third chamber. Where the performers went is a mystery.");
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(15) as Gun).gunSwitchGroup;

            gun.SetupSprite(null, "kalashnirang_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 15);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(617) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Automatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 0.5f;
            gun.DefaultModule.cooldownTime = 0.11f;
            gun.DefaultModule.numberOfShotsInClip = 30;
            gun.barrelOffset.transform.localPosition = new Vector3(1.37f, 0.68f, 0f);
            gun.SetBaseMaxAmmo(300);
            gun.gunClass = GunClass.FULLAUTO;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(617) as Gun).DefaultModule.chargeProjectiles[0].Projectile);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.transform.parent = gun.barrelOffset;
            PierceProjModifier orAddComponent = projectile.gameObject.GetOrAddComponent<PierceProjModifier>();
            orAddComponent.penetratesBreakables = true;
            orAddComponent.penetration = 5;

            gun.SetTag("kalashnikov");
            gun.quality = PickupObject.ItemQuality.C;
            ETGMod.Databases.Items.Add(gun, false, "ANY");

            KalashnirangID = gun.PickupObjectId;
        }
        public override void PostProcessProjectile(Projectile projectile)
        {
            PlayerController player = projectile.Owner as PlayerController;
            base.PostProcessProjectile(projectile);
            if (player.PlayerHasActiveSynergy("Rangaround"))
            {
                projectile.baseData.damage *= 2f;
                projectile.baseData.speed *= 2f;
            }
            projectile.specRigidbody.OnPreRigidbodyCollision += this.HandlePierce;
        }
        private void HandlePierce(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
        {
            FieldInfo field = typeof(Projectile).GetField("m_hasPierced", BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(myRigidbody.projectile, false);
        }
        public static int KalashnirangID;
        public Kalashnirang()
        {

        }
    }
}
