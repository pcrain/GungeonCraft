using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using ItemAPI;
using SaveAPI;

namespace NevernamedsItems
{

    public class IrregularShotgun : GunBehaviour
    {


        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Irregular Shotgun", "irregularshotgun");
            Game.Items.Rename("outdated_gun_mods:irregular_shotgun", "nn:irregular_shotgun");
            gun.gameObject.AddComponent<IrregularShotgun>();
            gun.SetShortDescription("weo8onerco;ma8437465");
            gun.SetLongDescription("This shotgun has seen the void, andddddddddddddddddddddfdfd 9d8dy73287675246c53xydf56ui87no8mp9,ufdauhjkds8888888888888");

            gun.SetupSprite(null, "irregularshotgun_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 13);
            gun.SetAnimationFPS(gun.reloadAnimation, 7);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(35) as Gun, true, false);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(93) as Gun).gunSwitchGroup;

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1.183773644f;
            gun.DefaultModule.cooldownTime = 0.5522464433456f;
            gun.DefaultModule.numberOfShotsInClip = 7;
            gun.barrelOffset.transform.localPosition = new Vector3(42f/16f, 12f/16f, 0f);
            gun.SetBaseMaxAmmo(127);
            gun.gunClass = GunClass.SHOTGUN;

            Projectile genericProj = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            genericProj.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(genericProj.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(genericProj);
            genericProj.baseData.damage = 40;
            genericProj.AdditionalScaleMultiplier = 1.2f;

            //Singular Projectile
            Projectile singularProj = UnityEngine.Object.Instantiate<Projectile>(genericProj);
            singularProj.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(singularProj.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(singularProj);
            gun.DefaultModule.projectiles[0] = singularProj;

            //2Shot
            AddProj(2, gun, genericProj, 0.5f, 0.95f);
            AddProj(3, gun, genericProj, 0.33f, 0.9f);
            AddProj(4, gun, genericProj, 0.25f, 0.85f);
            AddProj(5, gun, genericProj, 0.2f, 0.8f);
            AddProj(6, gun, genericProj, 0.166f, 0.75f);
            AddProj(7, gun, genericProj, 0.1428f, 0.7f);
            AddProj(8, gun, genericProj, 0.125f, 0.65f);
            AddProj(9, gun, genericProj, 0.1111f, 0.6f);
            AddProj(10, gun, genericProj, 0.1f, 0.55f);


            gun.quality = PickupObject.ItemQuality.B;
            ETGMod.Databases.Items.Add(gun, null, "ANY");
            ID = gun.PickupObjectId;
        }
        public static void AddProj(int amt, Gun gun, Projectile genericProj, float dmgMult, float scaleMult)
        {
            Projectile addedProj = UnityEngine.Object.Instantiate<Projectile>(genericProj);
            addedProj.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(addedProj.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(addedProj);
            gun.DefaultModule.projectiles.Add(addedProj);
            SneakyShotgunComponent MultiShotty = addedProj.gameObject.AddComponent<SneakyShotgunComponent>();
            MultiShotty.numToFire = amt;
            MultiShotty.projPrefabToFire = genericProj;
            MultiShotty.damageMult = dmgMult;
            MultiShotty.scaleMult = scaleMult;
        }
        public static int ID;
        public IrregularShotgun()
        {

        }
    }
}
