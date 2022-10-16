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

    public class NNBazooka : GunBehaviour
    {


        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Bazooka", "bazooka");
            Game.Items.Rename("outdated_gun_mods:bazooka", "nn:bazooka");
            gun.gameObject.AddComponent<NNBazooka>();
            gun.SetShortDescription("Boom Boom Boom Boom");
            gun.SetLongDescription("It takes a lunatic to be a legend." + "\n\nThis powerful explosive weapon has one major drawback; it is capable of damaging it's bearer. You'd think more bombs would do that, but the Gungeon forgives.");

            gun.SetupSprite(null, "bazooka_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 12);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(39) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 4f;
            gun.DefaultModule.cooldownTime = 2f;
            gun.muzzleFlashEffects.type = VFXPoolType.None;
            gun.DefaultModule.numberOfShotsInClip = 3;
            gun.barrelOffset.transform.localPosition = new Vector3(2.5f, 0.68f, 0f);
            gun.SetBaseMaxAmmo(100);
            gun.gunClass = GunClass.EXPLOSIVE;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage *= 3f;
            projectile.baseData.speed *= 1.2f;
            projectile.ignoreDamageCaps = true;
            projectile.pierceMinorBreakables = true;

            if (projectile.GetComponent<ExplosiveModifier>())
            {
                Destroy(projectile.GetComponent<ExplosiveModifier>());
            }
            FuckingExplodeYouCunt explosiveModifier = projectile.gameObject.AddComponent<FuckingExplodeYouCunt>();

            projectile.SetProjectileSpriteRight("bazooka_projectile", 26, 7, false, tk2dBaseSprite.Anchor.MiddleCenter, 26, 7);

            projectile.transform.parent = gun.barrelOffset;

            // Here we just set the quality of the gun and the "EncounterGuid", which is used by Gungeon to identify the gun.
            gun.quality = PickupObject.ItemQuality.B;
            gun.encounterTrackable.EncounterGuid = "this is the Bazooka";
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            gun.AddToSubShop(ItemBuilder.ShopType.Trorc);
            BazookaID = gun.PickupObjectId;

            gun.SetupUnlockOnCustomFlag(CustomDungeonFlags.PURCHASED_BAZOOKA, true);
            gun.AddItemToTrorcMetaShop(20);
        }
        public static int BazookaID;
        public override void OnPostFired(PlayerController player, Gun gun)
        {
        }
        public NNBazooka()
        {

        }
    }
    public class FuckingExplodeYouCunt : MonoBehaviour
    {
        public FuckingExplodeYouCunt()
        {

        }
        private Projectile m_projectile;

        private void Awake()
        {
            this.m_projectile = base.GetComponent<Projectile>();
            m_projectile.OnDestruction += this.Destruction;
        }
        private void Destruction(Projectile projectile)
        {
            //ETGModConsole.Log("On Destroy was called");
            Exploder.DoDefaultExplosion(projectile.specRigidbody.UnitTopCenter, new Vector2(), null, false, CoreDamageTypes.None, true);
            //Exploder.DoDefaultExplosion(projectile.specRigidbody.UnitTopCenter, new Vector2(), null, false, CoreDamageTypes.None, true);
        }
        private void Update()
        {

        }
    }
}