using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using Alexandria.ItemAPI;

namespace NevernamedsItems
{

    public class SpearOfJustice : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Spear of Justice", "spearofjustice");
            Game.Items.Rename("outdated_gun_mods:spear_of_justice", "nn:spear_of_justice");
            gun.gameObject.AddComponent<SpearOfJustice>();
            gun.SetShortDescription("NGAH!");
            gun.SetLongDescription("Weapon of an ancient gundead warrior, who believed she could escape the Gungeon by harnessing the power of Gungeoneer souls." + "\n\nShe never achieved her goal.");

            gun.SetupSprite(null, "spearofjustice_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 30);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);
            gun.gunSwitchGroup = (PickupObjectDatabase.GetById(417) as Gun).gunSwitchGroup;
            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.DefaultModule.cooldownTime = 0.5f;
            gun.muzzleFlashEffects.type = VFXPoolType.None;
            gun.DefaultModule.numberOfShotsInClip = 100;
            gun.DefaultModule.angleVariance = 0;
            gun.barrelOffset.transform.localPosition = new Vector3(3.25f, 1.68f, 0f);
            gun.SetBaseMaxAmmo(200);
            gun.gunClass = GunClass.SILLY;
            //BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage *= 4f;
            PierceProjModifier Piercing = projectile.gameObject.GetOrAddComponent<PierceProjModifier>();
            Piercing.penetratesBreakables = true;
            Piercing.penetration += 10;
            projectile.SetProjectileSpriteRight("spearofjustice_projectile", 52, 14, false, tk2dBaseSprite.Anchor.MiddleCenter, 39, 8);

            projectile.transform.parent = gun.barrelOffset;

            gun.quality = PickupObject.ItemQuality.A;
            gun.encounterTrackable.EncounterGuid = "this is the Spear of Justice";
            ETGMod.Databases.Items.Add(gun, false, "ANY");
            SpearOfJusticeID = gun.PickupObjectId;
        }
        public static int SpearOfJusticeID;
        protected override void OnPickedUpByPlayer(PlayerController player)
        {
            ETGMod.AIActor.OnPreStart += this.Greenify;
            base.OnPickedUpByPlayer(player);
        }
        public override void OnPostFired(PlayerController player, Gun gun)
        {
        }
        public override void PostProcessProjectile(Projectile projectile)
        {
            base.PostProcessProjectile(projectile);
            PlayerController playerController = projectile.Owner as PlayerController;
            if (playerController.PlayerHasActiveSynergy("Undying") && UnityEngine.Random.value <= 0.1f)
            {
                projectile.AdjustPlayerProjectileTint(Color.yellow, 1);
                HomingModifier homing = projectile.gameObject.AddComponent<HomingModifier>();
                homing.AngularVelocity = 250f;
                homing.HomingRadius = 250f;
            }
        }
        protected override void OnPostDroppedByPlayer(PlayerController player)
        {
            base.OnPostDroppedByPlayer(player);
            ETGMod.AIActor.OnPreStart -= this.Greenify;
        }
        public void Greenify(AIActor target)
        {
            PlayerController playerController = gun.CurrentOwner as PlayerController;
            if (playerController.PlayerHasActiveSynergy("No Running Away!") && UnityEngine.Random.value <= 0.25f)
            {
                target.ApplyEffect(this.NoMoveDebuff, 1f, null);
                GameActorHealthEffect tint = new GameActorHealthEffect()
                {
                    TintColor = Color.green,
                    DeathTintColor = Color.green,
                    AppliesTint = true,
                    AppliesDeathTint = true,
                    AffectsEnemies = true,
                    DamagePerSecondToEnemies = 0f,
                    duration = 10000000,
                    effectIdentifier = "SpearOfJusticeGreening",
                };
                target.ApplyEffect(tint);
            }
        }
        public AIActorDebuffEffect NoMoveDebuff = new AIActorDebuffEffect
        {
            SpeedMultiplier = 0.0f,
            OverheadVFX = null,
            duration = 1000000f
        };
        public override void OnDropped()
        {
            base.OnDropped();
            ETGMod.AIActor.OnPreStart -= this.Greenify;
        }
        protected override void Update()
        {
            base.Update();
            if (gun && gun.GunPlayerOwner())
            {
                if (gun.GunPlayerOwner().PlayerHasActiveSynergy("Undying"))
                {
                    if (gun.DefaultModule.cooldownTime == 0.5f)
                    {
                        gun.DefaultModule.cooldownTime = 0.25f;
                        gun.SetBaseMaxAmmo(400);
                    }
                }
                else
                {
                    if (gun.DefaultModule.cooldownTime == 0.25f)
                    {
                        gun.DefaultModule.cooldownTime = 0.5f;
                        gun.SetBaseMaxAmmo(200);
                    }
                }
            }

        }
        public SpearOfJustice()
        {

        }
    }
}
