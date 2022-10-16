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
    public class Protean : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Protean", "protean");
            Game.Items.Rename("outdated_gun_mods:protean", "nn:protean");
            gun.gameObject.AddComponent<Protean>();
            gun.SetShortDescription("Inexplicable");
            gun.SetLongDescription("These loosely bound motes of gun floated into existence through a tear in the curtain eons before the Great Bullet struck."+"\n\nIt's fractal bullets carve hauntingly beautiful patterns in the air, at least for those who can understand them.");

            gun.SetupSprite(null, "protean_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 12);
            gun.SetAnimationFPS(gun.idleAnimation, 12);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(479) as Gun).gunSwitchGroup;

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 0.8f;
            gun.DefaultModule.cooldownTime = 0.4f;
            gun.muzzleFlashEffects = (PickupObjectDatabase.GetById(97) as Gun).muzzleFlashEffects;
            gun.DefaultModule.numberOfShotsInClip = 6;
            gun.barrelOffset.transform.localPosition = new Vector3(1.56f, 0.62f, 0f);
            gun.SetBaseMaxAmmo(200);
            gun.gunClass = GunClass.PISTOL;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage *= 3f;
            projectile.baseData.speed *= 0.7f;
            projectile.baseData.range *= 10f;
            BounceProjModifier Bouncing = projectile.gameObject.GetOrAddComponent<BounceProjModifier>();
            Bouncing.numberOfBounces = 5;
            RepeatedRandomReAim erraticmovement = projectile.gameObject.AddComponent<RepeatedRandomReAim>();

            projectile.pierceMinorBreakables = true;


            projectile.AnimateProjectile(new List<string> {
                "proteanproj_1",
                "proteanproj_2",
                "proteanproj_3",
                "proteanproj_4",
                "proteanproj_5",
                "proteanproj_6",
                "proteanproj_7",
                "proteanproj_8",
            }, 8, true, new List<IntVector2> {
                new IntVector2(9, 9), //1
                new IntVector2(9, 9), //2            
                new IntVector2(9,9), //3
                new IntVector2(9, 9), //4
                new IntVector2(9, 9), //5
                new IntVector2(9, 9), //6
                new IntVector2(9,9), //7
                new IntVector2(9, 9), //8
            }, AnimateBullet.ConstructListOfSameValues(true, 8), AnimateBullet.ConstructListOfSameValues(tk2dBaseSprite.Anchor.MiddleCenter, 8), AnimateBullet.ConstructListOfSameValues(true, 8), AnimateBullet.ConstructListOfSameValues(false, 8),
                        AnimateBullet.ConstructListOfSameValues<Vector3?>(null, 8), AnimateBullet.ConstructListOfSameValues<IntVector2?>(null, 8), AnimateBullet.ConstructListOfSameValues<IntVector2?>(null, 8), AnimateBullet.ConstructListOfSameValues<Projectile>(null, 8));

            projectile.transform.parent = gun.barrelOffset;


            gun.quality = PickupObject.ItemQuality.C;
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            ProteanID = gun.PickupObjectId;
        }
        public static int ProteanID;
        public Protean()
        {

        }
    }
    public class RepeatedRandomReAim : MonoBehaviour
    {
        public RepeatedRandomReAim()
        {
        }
        private void Start()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            StartCoroutine(DoReAim());
        }
        private IEnumerator DoReAim()
        {
            while (m_projectile != null)
            {
                yield return new WaitForSeconds(0.2f);
                m_projectile.SendInRandomDirection();
            }
        }

        private Projectile m_projectile;
    }
}