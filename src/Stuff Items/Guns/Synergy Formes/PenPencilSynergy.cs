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

    public class PenPencilSynergy : GunBehaviour
    {

        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Pen", "pen");
            Game.Items.Rename("outdated_gun_mods:pen", "nn:pencil+mightier_than_the_gun");
            gun.gameObject.AddComponent<PenPencilSynergy>();
            gun.SetShortDescription("draw me like one of your french girls");
            gun.SetLongDescription("massive fuck'n pen");

            gun.SetupSprite(null, "pen_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 17);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Automatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 0.1f;
            gun.muzzleFlashEffects.type = VFXPoolType.None;

            gun.DefaultModule.cooldownTime = 0.0001f;
            gun.DefaultModule.numberOfShotsInClip = 6000;
            gun.barrelOffset.transform.localPosition = new Vector3(2.25f, 0.31f, 0f);
            gun.SetBaseMaxAmmo(6000);
            gun.ammo = 6000;
            gun.PreventNormalFireAudio = true;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage = 1f;
            projectile.baseData.force *= 0.1f;
            projectile.baseData.speed *= 0.0001f;
            projectile.SetProjectileSpriteRight("pen_projectile", 4, 4, false, tk2dBaseSprite.Anchor.MiddleCenter, 4, 4);

            projectile.transform.parent = gun.barrelOffset;

            // Here we just set the quality of the gun and the "EncounterGuid", which is used by Gungeon to identify the gun.
            gun.quality = PickupObject.ItemQuality.EXCLUDED;
            gun.encounterTrackable.EncounterGuid = "this is the pen";
            ETGMod.Databases.Items.Add(gun, null, "ANY");
            penID = gun.PickupObjectId;
        }
        public static int penID;
        public override void PostProcessProjectile(Projectile projectile)
        {
            ActiveBullets.Add(projectile);
            projectile.specRigidbody.OnPreTileCollision += this.onhit;
            base.PostProcessProjectile(projectile);
        }
        private void onhit(SpeculativeRigidbody myrigidbody, PixelCollider mypixelcollider, PhysicsEngine.Tile tile, PixelCollider tilepixelcollider)
        {
            myrigidbody.projectile.ForceDestruction();
        }
        public override void OnReloadPressed(PlayerController player, Gun gun, bool bSOMETHING)
        {
            if (ActiveBullets.Count > 0)
            {
                foreach (Projectile bullet in ActiveBullets)
                {
                    if (bullet)
                    {
                        Vector2 vector = player.CenterPosition;
                        Vector2 normalized = (player.unadjustedAimPoint.XY() - vector).normalized;
                        bullet.SendInDirection(normalized, false, true);
                        bullet.baseData.speed *= 10000;
                        //bullet.Speed *= 1000;
                        bullet.UpdateSpeed();
                        BulletsToRemoveFromActiveBullets.Add(bullet);
                    }
                }
                foreach (Projectile bullet in BulletsToRemoveFromActiveBullets)
                {
                    ActiveBullets.Remove(bullet);
                }
                BulletsToRemoveFromActiveBullets.Clear();
            }
            base.OnReloadPressed(player, gun, bSOMETHING);
        }
        public static List<Projectile> ActiveBullets = new List<Projectile>() { };
        public static List<Projectile> BulletsToRemoveFromActiveBullets = new List<Projectile>() { };


        public PenPencilSynergy()
        {
        }
    }
}

