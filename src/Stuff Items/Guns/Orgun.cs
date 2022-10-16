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

    public class Orgun : GunBehaviour
    {


        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Orgun", "orgun");
            Game.Items.Rename("outdated_gun_mods:orgun", "nn:orgun");
            gun.gameObject.AddComponent<Orgun>();
            gun.SetShortDescription("My Heart Will Go On");
            gun.SetLongDescription("Hespera, the Pride of Venus, always wished that her fighting spirit, her courage... her heart, if you will, would go on to inspire and aid others." + "\n\nShe never realised how literal that would be.");

            gun.SetupSprite(null, "orgun_idle_001", 8);
            //ItemBuilder.AddPassiveStatModifier(gun, PlayerStats.StatType.GlobalPriceMultiplier, 0.925f, StatModifier.ModifyMethod.MULTIPLICATIVE);

            gun.SetAnimationFPS(gun.shootAnimation, 8);
            gun.SetAnimationFPS(gun.idleAnimation, 5);
            gun.AddPassiveStatModifier(PlayerStats.StatType.Health, 1f, StatModifier.ModifyMethod.ADDITIVE);

            for (int i = 0; i < 6; i++)
            {
                gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(56) as Gun, true, false);
            }

            //GUN STATS
            foreach (ProjectileModule mod in gun.Volley.projectiles)
            {
                mod.ammoCost = 1;
                mod.shootStyle = ProjectileModule.ShootStyle.Burst;
                mod.burstShotCount = 2;
                mod.burstCooldownTime = 0.2f;
                mod.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
                mod.cooldownTime = 0.5f;
                mod.angleVariance = 20f;
                mod.numberOfShotsInClip = 6;
                Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(mod.projectiles[0]);
                mod.projectiles[0] = projectile;
                projectile.gameObject.SetActive(false);
                FakePrefab.MarkAsFakePrefab(projectile.gameObject);
                UnityEngine.Object.DontDestroyOnLoad(projectile);
                projectile.hitEffects.overrideMidairDeathVFX = (PickupObjectDatabase.GetById(369) as Gun).DefaultModule.chargeProjectiles[0].Projectile.hitEffects.tileMapVertical.effects[0].effects[0].effect;
                projectile.hitEffects.alwaysUseMidair = true;
                projectile.baseData.damage *= 1.4f;
                projectile.baseData.speed *= 1.2f;
                GoopModifier blood = projectile.gameObject.AddComponent<GoopModifier>();
                blood.goopDefinition = EasyGoopDefinitions.BlobulonGoopDef;
                blood.SpawnGoopInFlight = true;
                blood.InFlightSpawnFrequency = 0.05f;
                blood.InFlightSpawnRadius = 1f;
                blood.SpawnGoopOnCollision = true;
                blood.CollisionSpawnRadius = 2f;
                projectile.SetProjectileSpriteRight("orgun_projectile", 12, 7, true, tk2dBaseSprite.Anchor.MiddleCenter, 7, 7);
                if (mod != gun.DefaultModule) { mod.ammoCost = 0; }
                projectile.transform.parent = gun.barrelOffset;
            }

            gun.reloadTime = 1.3f;
            gun.barrelOffset.transform.localPosition = new Vector3(2.62f, 0.81f, 0f);
            gun.SetBaseMaxAmmo(80);
            gun.gunClass = GunClass.SHOTGUN;
            //BULLET STATS
            gun.DefaultModule.ammoType = GameUIAmmoType.AmmoType.CUSTOM;
            gun.DefaultModule.customAmmoType = CustomClipAmmoTypeToolbox.AddCustomAmmoType("Orgun Bullets", "NevernamedsItems/Resources/CustomGunAmmoTypes/orgun_clipfull", "NevernamedsItems/Resources/CustomGunAmmoTypes/orgun_clipempty");


            // Here we just set the quality of the gun and the "EncounterGuid", which is used by Gungeon to identify the gun.
            gun.quality = PickupObject.ItemQuality.A;
            gun.encounterTrackable.EncounterGuid = "this is the Orgun";
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            OrgunID = gun.PickupObjectId;
            gun.SetupUnlockOnCustomMaximum(CustomTrackedMaximums.MAX_HEART_CONTAINERS_EVER, 7, DungeonPrerequisite.PrerequisiteOperation.GREATER_THAN);
        }
        public static int OrgunID;
        public override void Update()
        {
            if (gun && gun.CurrentOwner && gun.CurrentOwner is PlayerController)
            {
                PlayerController player = gun.CurrentOwner as PlayerController;
                currentItems = player.passiveItems.Count;
                currentGuns = player.inventory.AllGuns.Count;
                currentClip = gun.DefaultModule.numberOfShotsInClip;
                currentActives = player.activeItems.Count;
                if (currentItems != lastItems || currentGuns != lastGuns || currentClip != lastClip || currentActives != lastActives)
                {
                    CalculateHeartAttackStats(player);
                    lastGuns = currentGuns;
                    lastItems = currentItems;
                    lastClip = currentClip;
                    lastActives = currentActives;
                }
                if (player.PlayerHasActiveSynergy("Headache"))
                {
                    if (!hasElectricityImmunity)
                    {
                        this.m_electricityImmunity = new DamageTypeModifier();
                        this.m_electricityImmunity.damageMultiplier = 0f;
                        this.m_electricityImmunity.damageType = CoreDamageTypes.Electric;
                        player.healthHaver.damageTypeModifiers.Add(this.m_electricityImmunity);
                        hasElectricityImmunity = true;
                    }
                    if (!hadHeadacheLastTimeWeChecked)
                    {
                        CalculateHeartAttackStats(player);
                        hadHeadacheLastTimeWeChecked = true;
                    }
                }
                else
                {
                    if (hasElectricityImmunity)
                    {
                        player.healthHaver.damageTypeModifiers.Remove(this.m_electricityImmunity);
                        hasElectricityImmunity = false;
                    }
                    if (hadHeadacheLastTimeWeChecked)
                    {
                        CalculateHeartAttackStats(player);
                        hadHeadacheLastTimeWeChecked = false;
                    }
                }
            }
        }
        private DamageTypeModifier m_electricityImmunity;
        private bool hasElectricityImmunity;
        private bool hadHeadacheLastTimeWeChecked;
        public override void OnInitializedWithOwner(GameActor actor)
        {
            base.OnInitializedWithOwner(actor);
            PlayerController player = actor as PlayerController;

            CalculateHeartAttackStats(player);


        }
        private void CalculateHeartAttackStats(PlayerController player)
        {
            int MaxClipWithSynergy = 6;
            int MaxAmmoWithSynergy = 80;
            if (player.PlayerHasActiveSynergy("Headache")) MaxAmmoWithSynergy = 120;
            if (player.PlayerHasActiveSynergy("Heart Attack"))
            {
                foreach (PassiveItem item in player.passiveItems)
                {
                    if (HeartAttackItems.Contains(item.PickupObjectId))
                    {
                        MaxClipWithSynergy += 2;
                        MaxAmmoWithSynergy += 50;
                    }
                }
                foreach (Gun gun in player.inventory.AllGuns)
                {
                    if (HeartAttackItems.Contains(gun.PickupObjectId))
                    {
                        MaxClipWithSynergy += 2;
                        MaxAmmoWithSynergy += 50;
                    }
                }
                foreach (PlayerItem activeitem in player.activeItems)
                {
                    if (HeartAttackItems.Contains(activeitem.PickupObjectId))
                    {
                        MaxClipWithSynergy += 2;
                        MaxAmmoWithSynergy += 50;
                    }
                }
                foreach (ProjectileModule mod in gun.Volley.projectiles)
                {
                    mod.numberOfShotsInClip = MaxClipWithSynergy;
                }
                gun.SetBaseMaxAmmo(MaxAmmoWithSynergy);
            }
            else
            {
                foreach (ProjectileModule mod in gun.Volley.projectiles)
                {
                    mod.numberOfShotsInClip = 6;
                }
                if (player.PlayerHasActiveSynergy("Headache")) gun.SetBaseMaxAmmo(120);
                else gun.SetBaseMaxAmmo(80);
            }
        }
        private int currentItems, lastItems;
        private int currentActives, lastActives;
        private int currentGuns, lastGuns;
        private int currentClip, lastClip;
        public static List<int> HeartAttackItems = new List<int>()
        {
            421, //Heart Holster
            422, //Heart Lunchbox
            423, //Heart Locket
            424, //Heart Bottle
            425, //Heart Purse
            164, //Heart Synthesizer
            364, //Heart of Ice
            815, //Lichs Eye Bullets
            CheeseHeart.CheeseHeartID,
            ForsakenHeart.ForsakenHeartID,
            HeartOfGold.HeartOfGoldID,
            HeartPadlock.HeartPadlockID,
        };
        public override void PostProcessProjectile(Projectile projectile)
        {
            PlayerController player = projectile.Owner as PlayerController;
            base.PostProcessProjectile(projectile);
            CalculateHeartAttackStats(player);
            if (player.PlayerHasActiveSynergy("Headache"))
            {
                projectile.damageTypes |= CoreDamageTypes.Electric;
            }
        }
        public override void OnReloadPressed(PlayerController player, Gun gun, bool bSOMETHING)
        {
            if (player.PlayerHasActiveSynergy("Headache")) DeadlyDeadlyGoopManager.GetGoopManagerForGoopType(EasyGoopDefinitions.WaterGoop).TimedAddGoopCircle(player.sprite.WorldCenter, 7, 1, false);
            base.OnReloadPressed(player, gun, bSOMETHING);
            CalculateHeartAttackStats(player);
        }
        public override void OnPostFired(PlayerController player, Gun gun)
        {
            gun.PreventNormalFireAudio = true;
            AkSoundEngine.PostEvent("Play_WPN_shotgun_shot_01", gameObject);
        }
        public Orgun()
        {

        }
    }
}
