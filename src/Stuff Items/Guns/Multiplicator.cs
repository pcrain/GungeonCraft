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
    public class Multiplicator : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Multiplicator", "multiplicator");
            Game.Items.Rename("outdated_gun_mods:multiplicator", "nn:multiplicator");
            gun.gameObject.AddComponent<Multiplicator>();
            gun.SetShortDescription("Times Gone By");
            gun.SetLongDescription("This gun is capable of merging multiple bullets together to multiply their damage. Reload on a full clip to select multiplication intensity."+"\n\nBrought to the Gungeon by a great mathematician who stole everything he ever published.");

            gun.SetupSprite(null, "multiplicator_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 12);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.Automatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.DefaultModule.cooldownTime = 0.3f;
            gun.DefaultModule.numberOfShotsInClip = 20;
            gun.barrelOffset.transform.localPosition = new Vector3(1.25f, 0.5f, 0f);
            gun.SetBaseMaxAmmo(300);
            gun.gunClass = GunClass.PISTOL;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage *= 1f;
            projectile.baseData.speed *= 1f;
            projectile.baseData.range *= 1f;
            projectile.SetProjectileSpriteRight("multiplicator_projectile", 14, 7, false, tk2dBaseSprite.Anchor.MiddleCenter, 14, 7);

            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("Multiplicator Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/multiplicator_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/multiplicator_clipempty");
            projectile.transform.parent = gun.barrelOffset;

            gun.quality = PickupObject.ItemQuality.B;
            gun.encounterTrackable.EncounterGuid = "this is the Multiplicator";
            ETGMod.Databases.Items.Add(gun, null, "ANY");
        }
        public override void PostProcessProjectile(Projectile projectile)
        {
            projectile.baseData.damage *= mode;
            base.PostProcessProjectile(projectile);
        }
        protected override void Update()
        {
            int ammocost = mode;
            if (gun.CurrentOwner)
            {
                if ((gun.CurrentOwner as PlayerController).PlayerHasActiveSynergy("Times Tables") && mode != 1)
                {
                    ammocost = (mode - 1);
                }
            }
            if (gun.DefaultModule.ammoCost != ammocost)
            {
                gun.DefaultModule.ammoCost = ammocost;
            }
            base.Update();
        }
        public override void OnReloadPressed(PlayerController player, Gun gun, bool bSOMETHING)
        {
            base.OnReloadPressed(player, gun, bSOMETHING);
            if ((gun.ClipCapacity == gun.ClipShotsRemaining) || (gun.CurrentAmmo == gun.ClipShotsRemaining))
            {
               if (mode != 10)
                {
                    mode += 1;
                    TextBubble.DoAmbientTalk(player.transform, new Vector3(1, 2, 0), "Using "+mode+" ammo for "+mode+"x damage.", 4f);
                }
                else
                {
                    mode = 1;
                    TextBubble.DoAmbientTalk(player.transform, new Vector3(1, 2, 0), "Using 1 ammo for normal damage.", 4f);
                }
            }            
        }
        public static int mode = 1;
        public Multiplicator()
        {

        }
    }
}