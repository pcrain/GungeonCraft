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

    public class Defender : GunBehaviour
    {


        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Defender", "defender");
            Game.Items.Rename("outdated_gun_mods:defender", "nn:defender");
            gun.gameObject.AddComponent<Defender>();
            gun.SetShortDescription("Starcadia");
            gun.SetLongDescription("An old ground-to-air defence system from the 80s, to be used in the event of an extraterrestial incursion.");

            gun.SetupSprite(null, "defender_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 15);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(124) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 0.6f;
            gun.DefaultModule.cooldownTime = 0.3f;
            gun.muzzleFlashEffects.type = VFXPoolType.None;
            gun.DefaultModule.numberOfShotsInClip = 10;
            gun.barrelOffset.transform.localPosition = new Vector3(1.75f, 0f, 0f);
            gun.SetBaseMaxAmmo(300);
            gun.gunClass = GunClass.NONE;
            /*foreach (tk2dSpriteAnimationClip clip in gun.GetComponent<tk2dSpriteAnimator>().Library.clips)
            {
                foreach (tk2dSpriteAnimationFrame frame in clip.frames)
                {
                    frame.spriteCollection.spriteDefinitions[frame.spriteId].ApplyOffset(new Vector2(1, -0.5f));
                }
            }*/

            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage *= 3f;
            projectile.baseData.speed *= 0.5f;
            projectile.pierceMinorBreakables = true;
            //projectile.shouldRotate = true;
            projectile.transform.parent = gun.barrelOffset;

            gun.quality = PickupObject.ItemQuality.EXCLUDED;
            gun.encounterTrackable.EncounterGuid = "this is the Defender";
            ETGMod.Databases.Items.Add(gun, null, "ANY");

        }        
        public Defender()
        {

        }
    }
}
