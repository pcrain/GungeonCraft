using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Gungeon;
using MonoMod;
using UnityEngine;
using ItemAPI;
using System.Reflection;

namespace NevernamedsItems
{
    class GoodMimic : AdvancedGunBehavior
    {
        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Good Mimic", "goodmimic");
            Game.Items.Rename("outdated_gun_mods:good_mimic", "nn:good_mimic");
            gun.gameObject.AddComponent<GoodMimic>();
            gun.SetShortDescription("All Grown Up");
            gun.SetLongDescription("Unlike most, this mimic thirsts for adventure rather than blood." + "\n\nBest to be polite though, just in case.");
            gun.SetupSprite(null, "goodmimic_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 10);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);

            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1.1f;
            gun.gunHandedness = GunHandedness.AutoDetect;
            gun.DefaultModule.cooldownTime = 0.1f;
            gun.DefaultModule.numberOfShotsInClip = 6;
            gun.SetBaseMaxAmmo(300);
            // Here we just set the quality of the gun and the "EncounterGuid", which is used by Gungeon to identify the gun.
            gun.quality = PickupObject.ItemQuality.EXCLUDED; //C

            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile);
            gun.DefaultModule.projectiles[0] = projectile;
            projectile.baseData.damage *= 1f;
            projectile.baseData.speed *= 1f;
            projectile.baseData.range *= 2f;

            gun.barrelOffset.transform.localPosition = new Vector3(1.0f, 0.37f, 0f);

            ETGMod.Databases.Items.Add(gun, null, "ANY");
            GoodMimicID = gun.PickupObjectId;
        }

        protected override void Update()
        {
            base.Update();
            if (this.gun.CurrentOwner == null)
            {
                this.TransformToTargetGunSpecial(PickupObjectDatabase.GetById(GoodMimicID) as Gun);
            }
        }

        protected override void OnPickedUpByPlayer(PlayerController player)
        {
            player.OnAnyEnemyReceivedDamage += this.ProjectileHitEnemy;
            base.OnPickedUpByPlayer(player);
        }
        protected override void OnPostDroppedByPlayer(PlayerController player)
        {
            player.OnAnyEnemyReceivedDamage -= this.ProjectileHitEnemy;
            base.OnPostDroppedByPlayer(player);
        }
        bool canChangeToBossWeapon = true;
        public void ProjectileHitEnemy(float damage, bool fatal, HealthHaver enemy)
        {
            CustomEnemyTagsSystem tags = enemy.gameObject.GetComponent<CustomEnemyTagsSystem>();
            if (tags != null && tags.ignoreForGoodMimic == true) { return; }
            if (enemy != null && enemy.aiActor != null && ((fatal && !enemy.IsBoss) || (enemy.IsBoss && canChangeToBossWeapon)))
            {

                int GunID = -1;
                if (enemy.aiActor.aiShooter != null && enemy.aiActor.aiShooter.CurrentGun != null)
                {
                    if (SpecialOverrideGuns.ContainsKey(enemy.aiActor.EnemyGuid)) { GunID = SpecialOverrideGuns[enemy.aiActor.EnemyGuid]; }
                    else { GunID = enemy.aiActor.aiShooter.CurrentGun.PickupObjectId; }
                }
                else if (Entries.ContainsKey(enemy.aiActor.EnemyGuid))
                {
                    GunID = Entries[enemy.aiActor.EnemyGuid];
                }
                if (GunID > 0)
                {
                    if (enemy.IsBoss)
                    {
                        canChangeToBossWeapon = false;
                        Invoke("ResetBossWeaponCooldown", 2.5f);
                    }
                    TransformToTargetGunSpecial((PickupObjectDatabase.GetById(GunID) as Gun));
                }
            }
        }
        private void ResetBossWeaponCooldown() { canChangeToBossWeapon = true; }
        public void TransformToTargetGunSpecial(Gun targetGun)
        {
            int clipShotsRemaining = this.gun.ClipShotsRemaining;
            if (((VFXPool)m_currentlyPlayingChargeVFX_info.GetValue(this.gun)) != null)
            {
                ((VFXPool)m_currentlyPlayingChargeVFX_info.GetValue(this.gun)).DestroyAll();
                m_currentlyPlayingChargeVFX_info.SetValue(this.gun, null);
            }
            ProjectileVolleyData volley = this.gun.Volley;
            this.gun.RawSourceVolley = targetGun.RawSourceVolley;
            this.gun.singleModule = targetGun.singleModule;
            this.gun.modifiedVolley = null;
            if (targetGun.sprite)
            {
                this.gun.DefaultSpriteID = targetGun.sprite.spriteId;
                this.gun.GetSprite().SetSprite(targetGun.sprite.Collection, this.gun.DefaultSpriteID);
                if (base.spriteAnimator && targetGun.spriteAnimator)
                {
                    base.spriteAnimator.Library = targetGun.spriteAnimator.Library;
                }
                tk2dSpriteDefinition.AttachPoint[] attachPoints = this.gun.GetSprite().Collection.GetAttachPoints(this.gun.DefaultSpriteID);
                tk2dSpriteDefinition.AttachPoint attachPoint;
                if (attachPoints != null)
                {
                    attachPoint = Array.Find(attachPoints, (tk2dSpriteDefinition.AttachPoint a) => a.name == "PrimaryHand");
                }
                else
                {
                    attachPoint = null;
                }
                tk2dSpriteDefinition.AttachPoint attachPoint2 = attachPoint;
                if (attachPoint2 != null)
                {
                    m_defaultLocalPosition_info.SetValue(this.gun, -attachPoint2.position);
                }
            }
            if (targetGun.GetBaseMaxAmmo() != this.gun.GetBaseMaxAmmo() && targetGun.GetBaseMaxAmmo() > 0)
            {
                int num = (!this.gun.InfiniteAmmo) ? this.gun.AdjustedMaxAmmo : this.gun.GetBaseMaxAmmo();
                this.gun.SetBaseMaxAmmo(targetGun.GetBaseMaxAmmo());
                if (this.gun.AdjustedMaxAmmo > 0 && num > 0 && this.gun.ammo > 0 && !this.gun.InfiniteAmmo)
                {
                    this.gun.ammo = Mathf.FloorToInt((float)this.gun.ammo / (float)num * (float)this.gun.AdjustedMaxAmmo);
                    this.gun.ammo = Mathf.Min(this.gun.ammo, this.gun.AdjustedMaxAmmo);
                }
                else
                {
                    this.gun.ammo = Mathf.Min(this.gun.ammo, this.gun.GetBaseMaxAmmo());
                }
            }
            this.gun.gunSwitchGroup = targetGun.gunSwitchGroup;
            this.gun.isAudioLoop = targetGun.isAudioLoop;
            this.gun.gunClass = targetGun.gunClass;
            if (!string.IsNullOrEmpty(this.gun.gunSwitchGroup))
            {
                AkSoundEngine.SetSwitch("WPN_Guns", this.gun.gunSwitchGroup, base.gameObject);
            }
            this.gun.currentGunDamageTypeModifiers = targetGun.currentGunDamageTypeModifiers;
            this.gun.carryPixelOffset = targetGun.carryPixelOffset;
            this.gun.carryPixelUpOffset = targetGun.carryPixelUpOffset;
            this.gun.carryPixelDownOffset = targetGun.carryPixelDownOffset;
            this.gun.leftFacingPixelOffset = targetGun.leftFacingPixelOffset;
            this.gun.UsesPerCharacterCarryPixelOffsets = targetGun.UsesPerCharacterCarryPixelOffsets;
            this.gun.PerCharacterPixelOffsets = targetGun.PerCharacterPixelOffsets;
            this.gun.gunPosition = targetGun.gunPosition;
            this.gun.forceFlat = targetGun.forceFlat;
            if (targetGun.GainsRateOfFireAsContinueAttack != this.gun.GainsRateOfFireAsContinueAttack)
            {
                this.gun.GainsRateOfFireAsContinueAttack = targetGun.GainsRateOfFireAsContinueAttack;
                this.gun.RateOfFireMultiplierAdditionPerSecond = targetGun.RateOfFireMultiplierAdditionPerSecond;
            }
            if (this.gun.barrelOffset && targetGun.barrelOffset)
            {
                this.gun.barrelOffset.localPosition = targetGun.barrelOffset.localPosition;
                m_originalBarrelOffsetPosition_info.SetValue(this.gun, targetGun.barrelOffset.localPosition);
            }
            if (this.gun.muzzleOffset && targetGun.muzzleOffset)
            {
                this.gun.muzzleOffset.localPosition = targetGun.muzzleOffset.localPosition;
                m_originalMuzzleOffsetPosition_info.SetValue(this.gun, targetGun.muzzleOffset.localPosition);
            }
            if (this.gun.chargeOffset && targetGun.chargeOffset)
            {
                this.gun.chargeOffset.localPosition = targetGun.chargeOffset.localPosition;
                m_originalChargeOffsetPosition_info.SetValue(this.gun, targetGun.chargeOffset.localPosition);
            }
            this.gun.reloadTime = targetGun.reloadTime;
            this.gun.blankDuringReload = targetGun.blankDuringReload;
            this.gun.blankReloadRadius = targetGun.blankReloadRadius;
            this.gun.reflectDuringReload = targetGun.reflectDuringReload;
            this.gun.blankKnockbackPower = targetGun.blankKnockbackPower;
            this.gun.blankDamageToEnemies = targetGun.blankDamageToEnemies;
            this.gun.blankDamageScalingOnEmptyClip = targetGun.blankDamageScalingOnEmptyClip;
            this.gun.doesScreenShake = targetGun.doesScreenShake;
            this.gun.gunScreenShake = targetGun.gunScreenShake;
            this.gun.directionlessScreenShake = targetGun.directionlessScreenShake;
            this.gun.AppliesHoming = targetGun.AppliesHoming;
            this.gun.AppliedHomingAngularVelocity = targetGun.AppliedHomingAngularVelocity;
            this.gun.AppliedHomingDetectRadius = targetGun.AppliedHomingDetectRadius;
            this.gun.GoopReloadsFree = targetGun.GoopReloadsFree;
            this.gun.gunHandedness = targetGun.gunHandedness;
            m_cachedGunHandedness_info.SetValue(this.gun, null);
            this.gun.shootAnimation = targetGun.shootAnimation;
            this.gun.usesContinuousFireAnimation = targetGun.usesContinuousFireAnimation;
            this.gun.reloadAnimation = targetGun.reloadAnimation;
            this.gun.emptyReloadAnimation = targetGun.emptyReloadAnimation;
            this.gun.idleAnimation = targetGun.idleAnimation;
            this.gun.chargeAnimation = targetGun.chargeAnimation;
            this.gun.dischargeAnimation = targetGun.dischargeAnimation;
            this.gun.emptyAnimation = targetGun.emptyAnimation;
            this.gun.introAnimation = targetGun.introAnimation;
            this.gun.finalShootAnimation = targetGun.finalShootAnimation;
            this.gun.enemyPreFireAnimation = targetGun.enemyPreFireAnimation;
            this.gun.dodgeAnimation = targetGun.dodgeAnimation;
            this.gun.muzzleFlashEffects = targetGun.muzzleFlashEffects;
            this.gun.usesContinuousMuzzleFlash = targetGun.usesContinuousMuzzleFlash;
            this.gun.finalMuzzleFlashEffects = targetGun.finalMuzzleFlashEffects;
            this.gun.reloadEffects = targetGun.reloadEffects;
            this.gun.emptyReloadEffects = targetGun.emptyReloadEffects;
            this.gun.activeReloadSuccessEffects = targetGun.activeReloadSuccessEffects;
            this.gun.activeReloadFailedEffects = targetGun.activeReloadFailedEffects;
            this.gun.shellCasing = targetGun.shellCasing;
            this.gun.shellsToLaunchOnFire = targetGun.shellsToLaunchOnFire;
            this.gun.shellCasingOnFireFrameDelay = targetGun.shellCasingOnFireFrameDelay;
            this.gun.shellsToLaunchOnReload = targetGun.shellsToLaunchOnReload;
            this.gun.reloadShellLaunchFrame = targetGun.reloadShellLaunchFrame;
            this.gun.clipObject = targetGun.clipObject;
            this.gun.clipsToLaunchOnReload = targetGun.clipsToLaunchOnReload;
            this.gun.reloadClipLaunchFrame = targetGun.reloadClipLaunchFrame;
            this.gun.IsTrickGun = targetGun.IsTrickGun;
            this.gun.TrickGunAlternatesHandedness = targetGun.TrickGunAlternatesHandedness;
            this.gun.alternateVolley = targetGun.alternateVolley;
            this.gun.alternateShootAnimation = targetGun.alternateShootAnimation;
            this.gun.alternateReloadAnimation = targetGun.alternateReloadAnimation;
            this.gun.alternateIdleAnimation = targetGun.alternateIdleAnimation;
            this.gun.alternateSwitchGroup = targetGun.alternateSwitchGroup;
            this.gun.rampBullets = targetGun.rampBullets;
            this.gun.rampStartHeight = targetGun.rampStartHeight;
            this.gun.rampTime = targetGun.rampTime;
            this.gun.usesDirectionalAnimator = targetGun.usesDirectionalAnimator;
            this.gun.usesDirectionalIdleAnimations = targetGun.usesDirectionalIdleAnimations;
            Component[] targetGunComponents = targetGun.GetComponents<Component>();
            Component[] thisGunComponents = this.GetComponents<Component>();
            foreach (Component targetGunComponent in targetGunComponents)
            {
                if (this.gun.GetComponent(targetGunComponent.GetType()) == null)
                {
                    Component comp = this.gun.gameObject.AddComponent(targetGunComponent.GetType());
                    comp.SetFields(targetGunComponent, includeProperties: true, includeFields: true);
                }
            }
            foreach (Component thisGunComponent in thisGunComponents)
            {
                if (thisGunComponent != this && targetGun.GetComponent(thisGunComponent.GetType()) == null)
                {
                    Destroy(thisGunComponent);
                }
            }
            if (base.aiAnimator)
            {
                Destroy(base.aiAnimator);
                base.aiAnimator = null;
            }
            if (targetGun.aiAnimator)
            {
                AIAnimator aianimator = base.gameObject.AddComponent<AIAnimator>();
                AIAnimator aiAnimator = targetGun.aiAnimator;
                aianimator.facingType = aiAnimator.facingType;
                aianimator.DirectionParent = aiAnimator.DirectionParent;
                aianimator.faceSouthWhenStopped = aiAnimator.faceSouthWhenStopped;
                aianimator.faceTargetWhenStopped = aiAnimator.faceTargetWhenStopped;
                aianimator.directionalType = aiAnimator.directionalType;
                aianimator.RotationQuantizeTo = aiAnimator.RotationQuantizeTo;
                aianimator.RotationOffset = aiAnimator.RotationOffset;
                aianimator.ForceKillVfxOnPreDeath = aiAnimator.ForceKillVfxOnPreDeath;
                aianimator.SuppressAnimatorFallback = aiAnimator.SuppressAnimatorFallback;
                aianimator.IsBodySprite = aiAnimator.IsBodySprite;
                aianimator.IdleAnimation = aiAnimator.IdleAnimation;
                aianimator.MoveAnimation = aiAnimator.MoveAnimation;
                aianimator.FlightAnimation = aiAnimator.FlightAnimation;
                aianimator.HitAnimation = aiAnimator.HitAnimation;
                aianimator.OtherAnimations = aiAnimator.OtherAnimations;
                aianimator.OtherVFX = aiAnimator.OtherVFX;
                aianimator.OtherScreenShake = aiAnimator.OtherScreenShake;
                aianimator.IdleFidgetAnimations = aiAnimator.IdleFidgetAnimations;
                base.aiAnimator = aianimator;
            }
            MultiTemporaryOrbitalSynergyProcessor component = targetGun.GetComponent<MultiTemporaryOrbitalSynergyProcessor>();
            MultiTemporaryOrbitalSynergyProcessor component2 = base.GetComponent<MultiTemporaryOrbitalSynergyProcessor>();
            if (!component && component2)
            {
                Destroy(component2);
            }
            else if (component && !component2)
            {
                MultiTemporaryOrbitalSynergyProcessor multiTemporaryOrbitalSynergyProcessor = base.gameObject.AddComponent<MultiTemporaryOrbitalSynergyProcessor>();
                multiTemporaryOrbitalSynergyProcessor.RequiredSynergy = component.RequiredSynergy;
                multiTemporaryOrbitalSynergyProcessor.OrbitalPrefab = component.OrbitalPrefab;
            }
            if (this.gun.RawSourceVolley != null)
            {
                for (int i = 0; i < this.gun.RawSourceVolley.projectiles.Count; i++)
                {
                    this.gun.RawSourceVolley.projectiles[i].ResetRuntimeData();
                }
            }
            else
            {
                this.gun.singleModule.ResetRuntimeData();
            }
            if (volley != null)
            {
                this.gun.RawSourceVolley = DuctTapeItem.TransferDuctTapeModules(volley, this.gun.RawSourceVolley, this.gun);
            }
            if (this.gun.CurrentOwner is PlayerController)
            {
                PlayerController playerController = this.gun.CurrentOwner as PlayerController;
                if (playerController.stats != null)
                {
                    playerController.stats.RecalculateStats(playerController, false, false);
                }
            }
            if (base.gameObject.activeSelf)
            {
                base.StartCoroutine((IEnumerator)HandleFrameDelayedTransformation_info.Invoke(this.gun, new object[0]));
            }
            this.gun.DidTransformGunThisFrame = true;
        }

        private static FieldInfo m_currentlyPlayingChargeVFX_info = typeof(Gun).GetField("m_currentlyPlayingChargeVFX", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo m_defaultLocalPosition_info = typeof(Gun).GetField("m_defaultLocalPosition", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo m_originalBarrelOffsetPosition_info = typeof(Gun).GetField("m_originalBarrelOffsetPosition", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo m_originalMuzzleOffsetPosition_info = typeof(Gun).GetField("m_originalMuzzleOffsetPosition", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo m_originalChargeOffsetPosition_info = typeof(Gun).GetField("m_originalChargeOffsetPosition", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo m_cachedGunHandedness_info = typeof(Gun).GetField("m_cachedGunHandedness", BindingFlags.NonPublic | BindingFlags.Instance);
        private static MethodInfo HandleFrameDelayedTransformation_info = typeof(Gun).GetMethod("HandleFrameDelayedTransformation", BindingFlags.NonPublic | BindingFlags.Instance);
        public static int GoodMimicID;

        public static Dictionary<string, int> Entries { get; set; } = new Dictionary<string, int>()
        {
             {EnemyGuidDatabase.Entries["shelleton"], 464}, //Shelleton --> Shellegun
             {EnemyGuidDatabase.Entries["gunzookie"], 599},
             {EnemyGuidDatabase.Entries["gunzockie"], 599},
             {EnemyGuidDatabase.Entries["gigi"], 445}, //Gigi --> Scrambler
             {EnemyGuidDatabase.Entries["bird_parrot"], 445}, //Parrot --> Scrambler
             {EnemyGuidDatabase.Entries["skusket"], 45}, //Skusket --> Skull Spitter
             {EnemyGuidDatabase.Entries["black_skusket"], 45}, //Black Skusket --> Skull Spitter
             {EnemyGuidDatabase.Entries["skusket_head"], 45}, //Skusket Head --> Skull Spitter
             {EnemyGuidDatabase.Entries["muzzle_wisp"], 125}, //Muzzle Wisp --> Flame Hand
             {EnemyGuidDatabase.Entries["muzzle_flare"], 698}, //Muzzle Flare --> Flame Hand+Maximize Spell
             {EnemyGuidDatabase.Entries["grenade_kin"], 19}, //Pinhead --> Grenade Launcher
             {EnemyGuidDatabase.Entries["mountain_cube"], 130}, //Mountain Cube --> Glacier
             {EnemyGuidDatabase.Entries["shambling_round"], 23}, //Shambing Round --> Dungeon Eagle
             {EnemyGuidDatabase.Entries["fuselier"], 332}, //Fuselier --> Lil' Bomber
             {EnemyGuidDatabase.Entries["bullet_shark"], Gunshark.GunsharkID}, //Bullet Shark --> Gunshark
             {EnemyGuidDatabase.Entries["bookllet"], Bookllet.BooklletID}, //Bookllet --> Bookllet
             {EnemyGuidDatabase.Entries["blue_bookllet"], Bookllet.BooklletID},
             {EnemyGuidDatabase.Entries["green_bookllet"], Bookllet.BooklletID},
             {EnemyGuidDatabase.Entries["necronomicon"], Bookllet.BooklletID},
             {EnemyGuidDatabase.Entries["bombshee"], 3}, // Bombshee --> Screecher
             {EnemyGuidDatabase.Entries["bullet_king"], 551}, // Bullet King --> Crown of Guns
             {EnemyGuidDatabase.Entries["gatling_gull"], 84}, // Gatling Gull --> Vulcan Cannon
             {EnemyGuidDatabase.Entries["treadnaught"], 486}, // Treadnaught --> Treadnaught Barrel
             {EnemyGuidDatabase.Entries["cannonbalrog"], 37}, // Cannonbalrog --> Serious Cannon
             {EnemyGuidDatabase.Entries["dragun"], 146}, // Dragun --> Dragunfire
             {EnemyGuidDatabase.Entries["dragun_advanced"], 670}, // Advanced Dragun --> High Dragunfire
             {EnemyGuidDatabase.Entries["helicopter_agunim"], 707}, // Advanced Dragun --> Vulcan Cannon
 
             {EnemyGuidDatabase.Entries["tazie"], StunGun.StunGunID}, // Tazie --> Stun Gun
             {EnemyGuidDatabase.Entries["gun_nut"], BulletBlade.BulletBladeID}, //Gun Nut --> Bullet Blade
             {EnemyGuidDatabase.Entries["spectral_gun_nut"], BulletBladeGhostForme.GhostBladeID}, //Gun Nut --> Bullet Blade
             {EnemyGuidDatabase.Entries["hooded_bullet"], 761}, //Confirmed --> High Kaliber
             {EnemyGuidDatabase.Entries["fungun"], FungoCannon.FungoCannonID}, //Fungun --> Fungo Cannon
             {EnemyGuidDatabase.Entries["spogre"], FungoCannon.FungoCannonID}, //Spogre --> Fungo Cannon
             {EnemyGuidDatabase.Entries["key_bullet_kin"], Rekeyter.RekeyterID}, //Keybullet Kin --> Rekeyter  
             {EnemyGuidDatabase.Entries["lore_gunjurer"], Lorebook.LorebookID}, //Lore Gunjurer --> Lorebook  
             {EnemyGuidDatabase.Entries["blizzbulon"], Icicle.IcicleID}, //Blizzbulon --> Icicle
             {EnemyGuidDatabase.Entries["phaser_spider"], PhaserSpiderling.PhaserSpiderlingID}, //Phaser Spider --> Phaser Spiderling
             {EnemyGuidDatabase.Entries["lead_maiden"], MaidenRifle.MaidenRifleID}, //Lead Maiden --> Maiden Rifle
             {EnemyGuidDatabase.Entries["bullat"], Bullatterer.BullattererID}, //Bullat --> Bullatterer
             {EnemyGuidDatabase.Entries["spirat"], Bullatterer.BullattererID}, //Spirat --> Bullatterer
             {EnemyGuidDatabase.Entries["grenat"], Bullatterer.BullattererID}, //Grenat --> Bullatterer
             {EnemyGuidDatabase.Entries["shotgat"], Bullatterer.BullattererID}, //Shotgat --> Bullatterer
             {EnemyGuidDatabase.Entries["king_bullat"], KingBullatterer.KingBullattererID}, //King Bullat --> Bullatterer+King Bullatterer
             {EnemyGuidDatabase.Entries["gargoyle"], KingBullatterer.KingBullattererID}, //Gargoyle --> Bullatterer+King Bullatterer
             {EnemyGuidDatabase.Entries["dynamite_kin"], DynamiteLauncher.DynamiteLauncherID}, //Nitra --> Dynamite launcher
             {EnemyGuidDatabase.Entries["misfire_beast"], Beastclaw.BeastclawID}, //Misfire Beast --> Beastclaw


             {ModdedGUIDDatabase.ExpandTheGungeonGUIDs["com4nd0_boss"], HeavyAssaultRifle.HeavyAssaultRifleID}, //Com4nd0 Boss --> Heavy Assault Rifle
             {ModdedGUIDDatabase.ExpandTheGungeonGUIDs["parasitic_abomination"], 333}, //Parasitic Abomination --> Mutation
             {ModdedGUIDDatabase.ExpandTheGungeonGUIDs["cronenberg"], 333}, //Cronenberg --> Mutation
             {ModdedGUIDDatabase.ExpandTheGungeonGUIDs["agressive_cronenberg"], 333}, //Agressive Cronenberg --> Mutation
 
 
             {EnemyGuidDatabase.Entries["great_bullet_shark"], GunsharkMegasharkSynergyForme.GunsharkMegasharkSynergyFormeID}, //Great Bullet Shark --> Gunshark+Megashark
             
 
        };
        public static Dictionary<string, int> SpecialOverrideGuns { get; set; } = new Dictionary<string, int>()
        {
             {EnemyGuidDatabase.Entries["shroomer"], ShroomedGun.ShroomedGunID}, //Shroomer --> ShroomedGun             
        };
    }
}