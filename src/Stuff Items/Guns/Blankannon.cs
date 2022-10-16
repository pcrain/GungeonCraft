using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using Alexandria.ItemAPI;
using Alexandria.Misc;

namespace NevernamedsItems
{
    public class Blankannon : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Blankannon", "blankannon");
            Game.Items.Rename("outdated_gun_mods:blankannon", "nn:blankannon");
            gun.gameObject.AddComponent<Blankannon>();
            gun.SetShortDescription("Fires Blanks");
            gun.SetLongDescription("It takes a very delicate pin to fire blanks instead of simply destroying them." + "\n\nThis elaborate device was put together from the scrapped parts of a laser-accurate surgical machine.");

            gun.SetupSprite(null, "blankannon_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 12);
            gun.AddPassiveStatModifier(PlayerStats.StatType.AdditionalBlanksPerFloor, 2f, StatModifier.ModifyMethod.ADDITIVE);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 0;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 0.8f;

            gun.DefaultModule.angleVariance = 0f;
            gun.DefaultModule.cooldownTime = 0.5f;
            gun.DefaultModule.numberOfShotsInClip = 10;
            gun.barrelOffset.transform.localPosition = new Vector3(1.98f, 0.75f, 0f);
            gun.SetBaseMaxAmmo(0);
            gun.gunClass = GunClass.SHITTY;
            //HEART BULLET STATS
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage *= 20f;
            projectile.pierceMinorBreakables = true;
            projectile.baseData.speed *= 1f;
            projectile.ignoreDamageCaps = true;
            projectile.baseData.range *= 10f;
            BlankProjModifier blankingArmour = projectile.gameObject.GetOrAddComponent<BlankProjModifier>();
            blankingArmour.blankType = EasyBlankType.FULL;
            projectile.SetProjectileSpriteRight("blankannon_projectile", 10, 7, false, tk2dBaseSprite.Anchor.MiddleCenter, 10, 7);
            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("Blank UI Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/blankannon_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/blankannon_clipempty");
            projectile.transform.parent = gun.barrelOffset;

            gun.quality = PickupObject.ItemQuality.C; //C
            ETGMod.Databases.Items.Add(gun, false, "ANY");

            gun.SetTag("override_cangainammo_check");

            BlankannonId = gun.PickupObjectId;
        }
        public static int BlankannonId;
        public override bool CollectedAmmoPickup(PlayerController player, Gun self, AmmoPickup pickup)
        {
            LootEngine.GivePrefabToPlayer(PickupObjectDatabase.GetById(224).gameObject, player);
            pickup.ForcePickupWithoutGainingAmmo(player);
            return false;
        }
        public override void OnPostFired(PlayerController player, Gun gun)
        {
            int blanksToRemove = 1;
            if (player.HasPickupID(TitaniumClip.TitaniumClipID)) blanksToRemove = 2;
            if (player.PlayerHasActiveSynergy("Secrets of the Ancients") && UnityEngine.Random.value <= 0.25) blanksToRemove = 0;
            if (player.Blanks >= blanksToRemove) player.Blanks -= blanksToRemove;
            else player.Blanks = 0;
            if (player.PlayerHasActiveSynergy("Paned Expression"))
            {
                player.AcquirePassiveItemPrefabDirectly((PickupObjectDatabase.GetById(565)) as PassiveItem);
            }
            DoMicroBlank(gun.barrelOffset.position);
            base.OnPostFired(player, gun);
        }
        protected override void OnPickedUpByPlayer(PlayerController player)
        {
            base.OnPickedUpByPlayer(player);
            if (!everPickedUpByPlayer)
            {
                LootEngine.GivePrefabToPlayer(PickupObjectDatabase.GetById(224).gameObject, player);
                LootEngine.GivePrefabToPlayer(PickupObjectDatabase.GetById(224).gameObject, player);
            }
            RecalculateClip(player);
        }
        public override void PostProcessProjectile(Projectile projectile)
        {
            try
            {
                PlayerController player = gun.CurrentOwner as PlayerController;
                if (gun && player)
                {
                    if (player.PlayerHasActiveSynergy("Paned Expression"))
                    {
                        foreach (PassiveItem item in player.passiveItems)
                        {
                            if (item.PickupObjectId == 565) projectile.baseData.damage *= 1.04f;
                        }
                    }
                }
                base.PostProcessProjectile(projectile);
            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message);
                ETGModConsole.Log(e.StackTrace);
            }
        }
        private void DoMicroBlank(Vector2 center)
        {
            try
            {
                PlayerController owner = gun.CurrentOwner as PlayerController;
                if (gun && owner)
                {
                    GameObject silencerVFX = (GameObject)ResourceCache.Acquire("Global VFX/BlankVFX_Ghost");
                    AkSoundEngine.PostEvent("Play_OBJ_silenceblank_small_01", base.gameObject);
                    GameObject gameObject = new GameObject("silencer");
                    SilencerInstance silencerInstance = gameObject.AddComponent<SilencerInstance>();
                    float additionalTimeAtMaxRadius = 0.25f;
                    silencerInstance.TriggerSilencer(center, 25f, 5f, silencerVFX, 0f, 3f, 3f, 3f, 250f, 5f, additionalTimeAtMaxRadius, owner, false, false);
                }
            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message);
                ETGModConsole.Log(e.StackTrace);
            }
        }
        protected override void Update()
        {
            if (gun.GunPlayerOwner())
            {
                if (gun.CurrentAmmo != gun.GunPlayerOwner().Blanks)
                {
                    this.RecalculateClip(gun.GunPlayerOwner());
                }
            }
            base.Update();
        }
        private void RecalculateClip(PlayerController gunOwner)
        {
            int total = gunOwner.Blanks;
            gun.CurrentAmmo = total;
            gun.ForceImmediateReload(true);
            gunOwner.stats.RecalculateStats(gunOwner);
        }
        public Blankannon()
        {

        }
    }
}