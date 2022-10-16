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

    public class NNMinigun : GunBehaviour
    {


        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Mini Gun", "mini_gun");
            Game.Items.Rename("outdated_gun_mods:mini_gun", "nn:mini_gun");
            gun.gameObject.AddComponent<NNMinigun>();
            gun.SetShortDescription("Misleading Name");
            gun.SetLongDescription("A tiny toy gun, probably pulled from the grip of a tiny toy soldier.");            
            gun.SetupSprite(null, "mini_gun_idle_001", 8);
            gun.SetAnimationFPS(gun.shootAnimation, 20);
            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1.1f;
            gun.gunClass = GunClass.PISTOL;
            gun.DefaultModule.cooldownTime = 0.1f;
            gun.DefaultModule.numberOfShotsInClip = 4;
            gun.SetBaseMaxAmmo(100);
            gun.quality = PickupObject.ItemQuality.D;
            gun.encounterTrackable.EncounterGuid = "this is the Mini Gun";
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            gun.barrelOffset.transform.localPosition = new Vector3(0.37f, 0.18f, 0f);

            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.AdditionalScaleMultiplier *= 0.5f;
            projectile.baseData.damage = 7f;

            MiniGunID = gun.PickupObjectId;
        }
        public static int MiniGunID;
        public override void PostProcessProjectile(Projectile projectile)
        {
            if (projectile && projectile.ProjectilePlayerOwner())
            {
                if (projectile.ProjectilePlayerOwner().PlayerHasActiveSynergy("Micro Aggressions"))
                {
                    if (UnityEngine.Random.value <= 0.1f)
                    {
                        projectile.AdjustPlayerProjectileTint(ExtendedColours.vibrantOrange, 2);
                        projectile.statusEffectsToApply.Add(StatusEffectHelper.GenerateSizeEffect(5, new Vector2(0.4f, 0.4f)));
                    }
                }
            }
            base.PostProcessProjectile(projectile);
        }
        public override void OnPostFired(PlayerController player, Gun gun)
        {
            gun.PreventNormalFireAudio = true;
            AkSoundEngine.PostEvent("Play_WPN_smileyrevolver_shot_01", gameObject);
        }
        public NNMinigun()
        {

        }
    }
}

