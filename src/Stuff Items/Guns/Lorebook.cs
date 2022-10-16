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

    public class Lorebook : AdvancedGunBehavior
    {


        public static void Add()
        {
            Gun gun = ETGMod.Databases.Items.NewGun("Lorebook", "lorebook");
            Game.Items.Rename("outdated_gun_mods:lorebook", "nn:lorebook");

            var behav = gun.gameObject.AddComponent<Lorebook>();
            behav.preventNormalFireAudio = true;
            behav.preventNormalReloadAudio = true;
            behav.overrideNormalFireAudio = "Play_ENM_wizard_summon_01";
            behav.overrideNormalReloadAudio = "Play_ENM_book_blast_01";

            gun.SetShortDescription("Party Cohesion");
            gun.SetLongDescription("Summons brave and noble bullet warriors of several different classes to destroy everything in sight and wreak havoc."+"\n(Like real heroes!)"+"\n\nThis magical tome of stories and scenarios was stolen from one of the most evil creatures in the Gungeon; a Lore Gunjurer.");

            gun.SetupSprite(null, "lorebook_idle_001", 8);

            gun.SetAnimationFPS(gun.shootAnimation, 18);

            gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);
            //gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(86) as Gun, true, false);

            //GUN STATS
            gun.DefaultModule.ammoCost = 1;
            gun.DefaultModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;
            gun.DefaultModule.sequenceStyle = ProjectileModule.ProjectileSequenceStyle.Random;
            gun.reloadTime = 1f;
            gun.DefaultModule.cooldownTime = 0.6f;
            gun.muzzleFlashEffects.type = VFXPoolType.None;
            gun.DefaultModule.numberOfShotsInClip = 4;
            gun.barrelOffset.transform.localPosition = new Vector3(0.93f, 0.87f, 0f);
            gun.SetBaseMaxAmmo(110);
            gun.gunClass = GunClass.SILLY;
            Projectile wizardSpellProjectile = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(86) as Gun).DefaultModule.projectiles[0]);
            wizardSpellProjectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(wizardSpellProjectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(wizardSpellProjectile);
            wizardSpellProjectile.baseData.damage *= 1f;
            wizardSpellProjectile.baseData.speed *= 0.7f;
            wizardSpellProjectile.baseData.range *= 5f;
            wizardSpellProjectile.SetProjectileSpriteRight("smallspark_projectile", 7, 7, true, tk2dBaseSprite.Anchor.MiddleCenter, 6, 6);

            Projectile wizardProjectile = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(86) as Gun).DefaultModule.projectiles[0]);
            wizardProjectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(wizardProjectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(wizardProjectile);
            wizardProjectile.baseData.damage *= 3f;
            wizardProjectile.baseData.speed *= 0.08f;
            wizardProjectile.baseData.range *= 5f;
            LorebookFantasyBullet wizardClassing = wizardProjectile.gameObject.GetOrAddComponent<LorebookFantasyBullet>();
            wizardClassing.Class = LorebookFantasyBullet.PartyMember.WIZARD;
            wizardProjectile.pierceMinorBreakables = true;
            SpawnProjModifier wizardshot = wizardProjectile.gameObject.GetOrAddComponent<SpawnProjModifier>();
            wizardshot.usesComplexSpawnInFlight = true;
            wizardshot.spawnOnObjectCollisions = false;
            wizardshot.spawnProjecitlesOnDieInAir = false;
            wizardshot.spawnProjectilesOnCollision = false;
            wizardshot.spawnProjectilesInFlight = true;
            wizardshot.projectileToSpawnInFlight = wizardSpellProjectile;
            wizardshot.inFlightAimAtEnemies = true;
            wizardshot.inFlightSpawnCooldown = 1.15f;
            wizardshot.numberToSpawnOnCollison = 0;
            wizardshot.numToSpawnInFlight = 1;
            wizardshot.PostprocessSpawnedProjectiles = true;
            HomingModifier wizardHoming = wizardProjectile.gameObject.AddComponent<HomingModifier>();
            wizardHoming.AngularVelocity = 120f;
            wizardHoming.HomingRadius = 1000f;
            BounceProjModifier wizardBouncing = wizardProjectile.gameObject.GetOrAddComponent<BounceProjModifier>();
            wizardBouncing.numberOfBounces = 10;
            wizardProjectile.AnimateProjectile(new List<string> {
                "playerwizardprojectile_001",
                "playerwizardprojectile_002",
                "playerwizardprojectile_003",
                "playerwizardprojectile_004",
            }, 10, true, new List<IntVector2> {
                new IntVector2(17, 22), //1
                new IntVector2(17, 22), //2            
                new IntVector2(17, 22), //3
                new IntVector2(17, 22), //3
            }, AnimateBullet.ConstructListOfSameValues(true, 4),
            AnimateBullet.ConstructListOfSameValues(tk2dBaseSprite.Anchor.MiddleCenter, 4),
            AnimateBullet.ConstructListOfSameValues(true, 4),
            AnimateBullet.ConstructListOfSameValues(false, 4),
            AnimateBullet.ConstructListOfSameValues<Vector3?>(null, 4),
            new List<IntVector2?> {
                new IntVector2(11, 16), //1
                new IntVector2(11, 16), //2            
                new IntVector2(11, 16), //3
                new IntVector2(11, 16), //3
            },
            AnimateBullet.ConstructListOfSameValues<IntVector2?>(null, 4),
            AnimateBullet.ConstructListOfSameValues<Projectile>(null, 4));
            wizardProjectile.shouldFlipHorizontally = true;

            Projectile bardProjectile = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(86) as Gun).DefaultModule.projectiles[0]);
            bardProjectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(bardProjectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(bardProjectile);
            bardProjectile.baseData.damage *= 4f;
            bardProjectile.baseData.range *= 5f;
            bardProjectile.baseData.speed *= 0.25f;
            LorebookFantasyBullet bardClassing = bardProjectile.gameObject.GetOrAddComponent<LorebookFantasyBullet>();
            bardClassing.Class = LorebookFantasyBullet.PartyMember.BARD;
            bardProjectile.pierceMinorBreakables = true;
            HomingModifier bardHoming = bardProjectile.gameObject.AddComponent<HomingModifier>();
            bardHoming.AngularVelocity = 120f;
            bardHoming.HomingRadius = 1000f;
            ExtremelySimpleStatusEffectBulletBehaviour bardCharming = bardProjectile.gameObject.AddComponent<ExtremelySimpleStatusEffectBulletBehaviour>();
            bardCharming.usesCharmEffect = true;
            bardCharming.charmEffect = StaticStatusEffects.charmingRoundsEffect;
            BounceProjModifier bardBouncing = bardProjectile.gameObject.GetOrAddComponent<BounceProjModifier>();
            bardBouncing.numberOfBounces = 10;
            bardProjectile.AnimateProjectile(new List<string> {
                "playerbardbullet_001",
                "playerbardbullet_002",
                "playerbardbullet_003",
                "playerbardbullet_004",
            }, 10, true, new List<IntVector2> {
                new IntVector2(16, 15), //1
                new IntVector2(16, 15), //2            
                new IntVector2(16, 15), //3
                new IntVector2(16, 15), //3
            }, AnimateBullet.ConstructListOfSameValues(true, 4),
            AnimateBullet.ConstructListOfSameValues(tk2dBaseSprite.Anchor.MiddleCenter, 4),
            AnimateBullet.ConstructListOfSameValues(true, 4),
            AnimateBullet.ConstructListOfSameValues(false, 4),
            AnimateBullet.ConstructListOfSameValues<Vector3?>(null, 4),
            new List<IntVector2?> {
                new IntVector2(10, 9), //1
                new IntVector2(10, 9), //2            
                new IntVector2(10, 9), //3
                new IntVector2(10, 9), //3
            },
            AnimateBullet.ConstructListOfSameValues<IntVector2?>(null, 4),
            AnimateBullet.ConstructListOfSameValues<Projectile>(null, 4));
            bardProjectile.shouldFlipHorizontally = true;


            Projectile rogueProjectile = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(86) as Gun).DefaultModule.projectiles[0]);
            rogueProjectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(rogueProjectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(rogueProjectile);
            rogueProjectile.baseData.damage *= 4f;
            rogueProjectile.baseData.speed *= 0.3f;
            rogueProjectile.baseData.range *= 5f;
            LorebookFantasyBullet rogueClassing = rogueProjectile.gameObject.GetOrAddComponent<LorebookFantasyBullet>();
            rogueClassing.Class = LorebookFantasyBullet.PartyMember.ROGUE;
            rogueProjectile.shouldFlipHorizontally = true;
            rogueProjectile.pierceMinorBreakables = true;
            HomingModifier rogueHoming = rogueProjectile.gameObject.AddComponent<HomingModifier>();
            rogueHoming.AngularVelocity = 120f;
            rogueHoming.HomingRadius = 1000f;
            BounceProjModifier rogueBouncing = rogueProjectile.gameObject.GetOrAddComponent<BounceProjModifier>();
            rogueBouncing.numberOfBounces = 10;
            //Set Up Teleport Effect
            GameObject BaseEnemyRogueBullet = EnemyDatabase.GetOrLoadByGuid("56fb939a434140308b8f257f0f447829").bulletBank.GetBullet("rogue").BulletObject;
            Projectile BaseEnemyProjectileComponent = BaseEnemyRogueBullet.GetComponent<Projectile>();
            if (BaseEnemyProjectileComponent != null)
            {
                TeleportProjModifier tp = BaseEnemyProjectileComponent.GetComponent<TeleportProjModifier>();
                if (tp != null)
                {
                    PlayerProjectileTeleportModifier rogueTeleport = rogueProjectile.gameObject.AddComponent<PlayerProjectileTeleportModifier>();
                    rogueTeleport.teleportVfx = tp.teleportVfx;
                    rogueTeleport.teleportCooldown = tp.teleportCooldown;
                    rogueTeleport.teleportPauseTime = tp.teleportPauseTime;
                    rogueTeleport.trigger = PlayerProjectileTeleportModifier.TeleportTrigger.DistanceFromTarget;
                    rogueTeleport.distToTeleport = tp.distToTeleport * 2f;
                    rogueTeleport.behindTargetDistance = tp.behindTargetDistance;
                    rogueTeleport.leadAmount = tp.leadAmount;
                    rogueTeleport.minAngleToTeleport = tp.minAngleToTeleport;
                    rogueTeleport.numTeleports = tp.numTeleports;
                    rogueTeleport.type = PlayerProjectileTeleportModifier.TeleportType.BehindTarget;
                }
                else { ETGModConsole.Log("Base Eney TeleportProjModifier was null???"); }
            }
            else { ETGModConsole.Log("Base Enemy Rogue Bullet had no projectile component???"); }
            rogueProjectile.AnimateProjectile(new List<string> {
                "playerroguebullet_001",
                "playerroguebullet_002",
                "playerroguebullet_003",
                "playerroguebullet_004",
            }, 10, true, new List<IntVector2> {
                new IntVector2(16, 15), //1
                new IntVector2(16, 15), //2            
                new IntVector2(16, 15), //3
                new IntVector2(16, 15), //3
            }, AnimateBullet.ConstructListOfSameValues(true, 4),
            AnimateBullet.ConstructListOfSameValues(tk2dBaseSprite.Anchor.MiddleCenter, 4),
            AnimateBullet.ConstructListOfSameValues(true, 4),
            AnimateBullet.ConstructListOfSameValues(false, 4),
            AnimateBullet.ConstructListOfSameValues<Vector3?>(null, 4),
            new List<IntVector2?> {
                new IntVector2(10, 9), //1
                new IntVector2(10, 9), //2            
                new IntVector2(10, 9), //3
                new IntVector2(10, 9), //3
            },
            AnimateBullet.ConstructListOfSameValues<IntVector2?>(null, 4),
            AnimateBullet.ConstructListOfSameValues<Projectile>(null, 4));

            //BULLET STATS
            Projectile knightProjectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            knightProjectile.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(knightProjectile.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(knightProjectile);
            knightProjectile.baseData.range *= 5f;
            knightProjectile.baseData.damage *= 5f;
            LorebookFantasyBullet knightClassing = knightProjectile.gameObject.GetOrAddComponent<LorebookFantasyBullet>();
            knightClassing.Class = LorebookFantasyBullet.PartyMember.KNIGHT;
            knightProjectile.pierceMinorBreakables = true;
            knightProjectile.baseData.speed *= 0.25f;
            HomingModifier knightHoming = knightProjectile.gameObject.AddComponent<HomingModifier>();
            knightHoming.AngularVelocity = 120f;
            knightHoming.HomingRadius = 1000f;
            BounceProjModifier knightBouncing = knightProjectile.gameObject.GetOrAddComponent<BounceProjModifier>();
            knightBouncing.numberOfBounces = 10;
            knightProjectile.AnimateProjectile(new List<string> {
                "playerknightbullet_001",
                "playerknightbullet_002",
                "playerknightbullet_003",
                "playerknightbullet_004",
            }, 10, true, new List<IntVector2> {
                new IntVector2(19, 15), //1
                new IntVector2(19, 15), //2            
                new IntVector2(19, 15), //3
                new IntVector2(19, 15), //3
            }, AnimateBullet.ConstructListOfSameValues(true, 4),
            AnimateBullet.ConstructListOfSameValues(tk2dBaseSprite.Anchor.MiddleCenter, 4),
            AnimateBullet.ConstructListOfSameValues(true, 4),
            AnimateBullet.ConstructListOfSameValues(false, 4),
            AnimateBullet.ConstructListOfSameValues<Vector3?>(null, 4),
            new List<IntVector2?> {
                new IntVector2(13, 9), //1
                new IntVector2(13, 9), //2            
                new IntVector2(13, 9), //3
                new IntVector2(13, 9), //3
            },
            AnimateBullet.ConstructListOfSameValues<IntVector2?>(null, 4),
            AnimateBullet.ConstructListOfSameValues<Projectile>(null, 4));
            knightProjectile.shouldFlipHorizontally = true;


            gun.DefaultModule.projectiles[0] = knightProjectile;
            gun.DefaultModule.projectiles.Add(wizardProjectile);
            gun.DefaultModule.projectiles.Add(bardProjectile);
            gun.DefaultModule.projectiles.Add(rogueProjectile);






            gun.quality = PickupObject.ItemQuality.B;
            ETGMod.Databases.Items.Add(gun, null, "ANY");

            LorebookID = gun.PickupObjectId;
        }
        public static int LorebookID;
        public override void PostProcessProjectile(Projectile projectile)
        {
            if (projectile.Owner is PlayerController)
            {
                PlayerController player = projectile.Owner as PlayerController;
                LorebookFantasyBullet loreness = projectile.gameObject.GetComponent<LorebookFantasyBullet>();
                if (loreness != null)
                {
                    if (loreness.Class == LorebookFantasyBullet.PartyMember.KNIGHT)
                    {
                        if (player.PlayerHasActiveSynergy("Level 20 Fighter"))
                        {
                            projectile.baseData.damage *= 1.25f;
                            projectile.baseData.speed *= 1.15f;
                            projectile.UpdateSpeed();
                            PierceProjModifier knightPiercing = projectile.gameObject.GetOrAddComponent<PierceProjModifier>();
                            knightPiercing.penetration += 5;
                            MakeLookLikeJammedBullet(projectile);
                        }
                    }
                    else if (loreness.Class == LorebookFantasyBullet.PartyMember.ROGUE)
                    {
                        if (player.PlayerHasActiveSynergy("Level 20 Rogue"))
                        {
                            projectile.baseData.damage *= 1.25f;
                            ExtremelySimpleStatusEffectBulletBehaviour roguePoisoning = projectile.gameObject.GetOrAddComponent<ExtremelySimpleStatusEffectBulletBehaviour>();
                            roguePoisoning.usesPoisonEffect = true;
                            roguePoisoning.poisonEffect = StaticStatusEffects.irradiatedLeadEffect;
                            MakeLookLikeJammedBullet(projectile);

                        }
                    }
                    else if (loreness.Class == LorebookFantasyBullet.PartyMember.WIZARD)
                    {
                        if (player.PlayerHasActiveSynergy("Level 20 Wizard"))
                        {
                            projectile.baseData.damage *= 1.25f;
                            SpawnProjModifier WizardShooting = projectile.gameObject.GetComponent<SpawnProjModifier>();
                            if (WizardShooting != null)
                            {
                                WizardShooting.inFlightSpawnCooldown = 0.35f;
                            }
                            MakeLookLikeJammedBullet(projectile);

                        }
                    }
                    else if (loreness.Class == LorebookFantasyBullet.PartyMember.BARD)
                    {
                        if (player.PlayerHasActiveSynergy("Level 20 Bard"))
                        {
                            projectile.baseData.damage *= 1.25f;
                            GameActorCharmEffect UpgradedCharm = new GameActorCharmEffect
                            {
                                duration = StaticStatusEffects.charmingRoundsEffect.duration * 3,
                                TintColor = StaticStatusEffects.charmingRoundsEffect.TintColor,
                                DeathTintColor = StaticStatusEffects.charmingRoundsEffect.DeathTintColor,
                                effectIdentifier = "Charm",
                                AppliesTint = true,
                                AppliesDeathTint = true,
                                resistanceType = EffectResistanceType.Charm,

                                //Eh
                                OverheadVFX = StaticStatusEffects.charmingRoundsEffect.OverheadVFX,
                                AffectsEnemies = true,
                                AffectsPlayers = false,
                                AppliesOutlineTint = false,
                                OutlineTintColor = StaticStatusEffects.charmingRoundsEffect.OutlineTintColor,
                                PlaysVFXOnActor = false,                               
                            };
                            ExtremelySimpleStatusEffectBulletBehaviour BardCharming = projectile.gameObject.GetComponent<ExtremelySimpleStatusEffectBulletBehaviour>();
                            BardCharming.charmEffect = UpgradedCharm;
                            BardCharming.usesCharmEffect = true;
                            MakeLookLikeJammedBullet(projectile);
                        }
                    }
                }
            }
            base.PostProcessProjectile(projectile);
        }
        public void MakeLookLikeJammedBullet(Projectile bullet)
        {
            bullet.AdjustPlayerProjectileTint(ExtendedColours.maroon, 1);
           // bullet.sprite.renderer.material.SetFloat("_EmissivePower", -40f);
        }
        public Lorebook()
        {

        }

    }
    public class LorebookFantasyBullet : MonoBehaviour
    {
        public LorebookFantasyBullet()
        {
        }
        public enum PartyMember
        {
            KNIGHT,
            WIZARD,
            BARD,
            ROGUE,
        }
        public PartyMember Class;
        private void Start()
        {
        }
        //private Projectile m_projectile;
    }
}