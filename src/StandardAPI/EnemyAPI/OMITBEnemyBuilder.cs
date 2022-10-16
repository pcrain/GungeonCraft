/*using Dungeonator;
using MonoMod.RuntimeDetour;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using DirectionType = DirectionalAnimation.DirectionType;
using FlipType = DirectionalAnimation.FlipType;

namespace ItemAPI
{
    public static class EnemyBuilder
    {
        private static GameObject behaviorSpeculatorPrefab;
        public static Dictionary<string, GameObject> Dictionary = new Dictionary<string, GameObject>();

        public static void Init()
        {
            var actor = EnemyDatabase.GetOrLoadByGuid("f905765488874846b7ff257ff81d6d0c");
            behaviorSpeculatorPrefab = GameObject.Instantiate(actor.gameObject);

            foreach (Transform child in behaviorSpeculatorPrefab.transform)
            {
                if (child != behaviorSpeculatorPrefab.transform)
                    GameObject.DestroyImmediate(child);
            }

            foreach (var comp in behaviorSpeculatorPrefab.GetComponents<Component>())
            {
                if (comp.GetType() != typeof(BehaviorSpeculator))
                {
                    GameObject.DestroyImmediate(comp);
                }
            }

            GameObject.DontDestroyOnLoad(behaviorSpeculatorPrefab);
            FakePrefab.MarkAsFakePrefab(behaviorSpeculatorPrefab);
            behaviorSpeculatorPrefab.SetActive(false);
            if (actor.GetComponent<EncounterTrackable>() != null)
            {
                UnityEngine.Object.Destroy(actor.GetComponent<EncounterTrackable>());
            }
            Hook enemyHook = new Hook(
                typeof(EnemyDatabase).GetMethod("GetOrLoadByGuid", BindingFlags.Public | BindingFlags.Static),
                typeof(EnemyBuilder).GetMethod("GetOrLoadByGuid")
            );
        }

        public static AIActor GetOrLoadByGuid(Func<string, AIActor> orig, string guid)
        {
            foreach (var id in Dictionary.Keys)
            {
                if (id == guid)
                    return Dictionary[id].GetComponent<AIActor>();
            }

            return orig(guid);
        }

        public static void SetupEntry(this AIActor enemy, string shortDesc, string longDesc, string portrait, string AmmonomiconSprite, string EnemyName)
        {
            SpriteBuilder.AddSpriteToCollection(AmmonomiconSprite, SpriteBuilder.ammonomiconCollection);
            if (enemy.GetComponent<EncounterTrackable>() != null)
            {
                UnityEngine.Object.Destroy(enemy.GetComponent<EncounterTrackable>());
            }
            enemy.encounterTrackable = enemy.gameObject.AddComponent<EncounterTrackable>();
            enemy.encounterTrackable.journalData = new JournalEntry();
            enemy.encounterTrackable.EncounterGuid = enemy.EnemyGuid;
            enemy.encounterTrackable.prerequisites = new DungeonPrerequisite[0];
            enemy.encounterTrackable.journalData.SuppressKnownState = false;
            enemy.encounterTrackable.journalData.IsEnemy = true;
            enemy.encounterTrackable.journalData.SuppressInAmmonomicon = false;
            enemy.encounterTrackable.journalData.AmmonomiconSprite = AmmonomiconSprite;
            enemy.encounterTrackable.journalData.enemyPortraitSprite = ResourceExtractor.GetTextureFromResource(portrait + ".png");
            enemy.encounterTrackable.ProxyEncounterGuid = "";
            PlanetsideModule.Strings.Enemies.Set("#" + EnemyName.ToUpper(), EnemyName);
            PlanetsideModule.Strings.Enemies.Set("#" + shortDesc.ToUpper(), shortDesc);
            PlanetsideModule.Strings.Enemies.Set("#" + longDesc.ToUpper(), longDesc);
            enemy.encounterTrackable.journalData.PrimaryDisplayName = "#" + EnemyName.ToUpper();
            enemy.encounterTrackable.journalData.NotificationPanelDescription = "#" + shortDesc.ToUpper();
            enemy.encounterTrackable.journalData.AmmonomiconFullEntry = "#" + longDesc.ToUpper();
            enemy.encounterTrackable.journalData.SuppressKnownState = false;
        }

        public static GameObject BuildPrefab(string name, string guid, string defaultSpritePath, IntVector2 hitboxOffset, IntVector2 hitBoxSize, bool HasAiShooter, bool UsesAttackGroup = false)
        {
            if (HasAiShooter)
            {
                var actor = EnemyDatabase.GetOrLoadByGuid("3cadf10c489b461f9fb8814abc1a09c1");
                behaviorSpeculatorPrefab = GameObject.Instantiate(actor.gameObject);
                foreach (Transform child in behaviorSpeculatorPrefab.transform)
                {
                    if (child != behaviorSpeculatorPrefab.transform)
                        GameObject.DestroyImmediate(child);
                }
                foreach (var comp in behaviorSpeculatorPrefab.GetComponents<Component>())
                {
                    if (comp.GetType() != typeof(BehaviorSpeculator))
                    {
                        GameObject.DestroyImmediate(comp);
                    }
                }
                GameObject.DontDestroyOnLoad(behaviorSpeculatorPrefab);
                FakePrefab.MarkAsFakePrefab(behaviorSpeculatorPrefab);
                behaviorSpeculatorPrefab.SetActive(false);

            }
            if (EnemyBuilder.Dictionary.ContainsKey(guid))
            {
                ETGModConsole.Log("EnemyBuilder: Yea something went wrong. Complain to Neighborino about it.");
                return null;
            }
            var prefab = GameObject.Instantiate(behaviorSpeculatorPrefab);
            prefab.name = name;

            //setup misc components
            var sprite = SpriteBuilder.SpriteFromResource(defaultSpritePath, prefab).GetComponent<tk2dSprite>();

            sprite.SetUpSpeculativeRigidbody(hitboxOffset, hitBoxSize).CollideWithOthers = true;
            prefab.AddComponent<tk2dSpriteAnimator>();
            prefab.AddComponent<AIAnimator>();

            //setup knockback
            var knockback = prefab.AddComponent<KnockbackDoer>();
            knockback.weight = 1;

            //setup health haver
            var healthHaver = prefab.AddComponent<HealthHaver>();
            healthHaver.RegisterBodySprite(sprite);
            healthHaver.PreventAllDamage = false;
            healthHaver.SetHealthMaximum(15000);
            healthHaver.FullHeal();

            //setup AI Actor
            var aiActor = prefab.AddComponent<AIActor>();
            aiActor.State = AIActor.ActorState.Normal;
            aiActor.EnemyGuid = guid;
            aiActor.CanTargetPlayers = true;
            aiActor.HasShadow = false;
            aiActor.specRigidbody.CollideWithOthers = true;
            aiActor.specRigidbody.CollideWithTileMap = true;
            aiActor.specRigidbody.PixelColliders.Clear();
            aiActor.specRigidbody.PixelColliders.Add(new PixelCollider

            {
                ColliderGenerationMode = PixelCollider.PixelColliderGeneration.Manual,
                CollisionLayer = CollisionLayer.EnemyCollider,
                IsTrigger = false,
                BagleUseFirstFrameOnly = false,
                SpecifyBagelFrame = string.Empty,
                BagelColliderNumber = 0,
                ManualOffsetX = 0,
                ManualOffsetY = 0,
                ManualWidth = 15,
                ManualHeight = 17,
                ManualDiameter = 0,
                ManualLeftX = 0,
                ManualLeftY = 0,
                ManualRightX = 0,
                ManualRightY = 0
            });
            aiActor.specRigidbody.PixelColliders.Add(new PixelCollider
            {

                ColliderGenerationMode = PixelCollider.PixelColliderGeneration.Manual,
                CollisionLayer = CollisionLayer.EnemyHitBox,
                IsTrigger = false,
                BagleUseFirstFrameOnly = false,
                SpecifyBagelFrame = string.Empty,
                BagelColliderNumber = 0,
                ManualOffsetX = 0,
                ManualOffsetY = 0,
                ManualWidth = 15,
                ManualHeight = 17,
                ManualDiameter = 0,
                ManualLeftX = 0,
                ManualLeftY = 0,
                ManualRightX = 0,
                ManualRightY = 0,
            });
            aiActor.CorpseObject = EnemyDatabase.GetOrLoadByGuid("01972dee89fc4404a5c408d50007dad5").CorpseObject;
            aiActor.PreventBlackPhantom = false;
            //setup behavior speculator
            var bs = prefab.GetComponent<BehaviorSpeculator>();
            bs.MovementBehaviors = new List<MovementBehaviorBase>();
            bs.TargetBehaviors = new List<TargetBehaviorBase>();
            bs.OverrideBehaviors = new List<OverrideBehaviorBase>();
            bs.OtherBehaviors = new List<BehaviorBase>();
            if (UsesAttackGroup)
            {
                bs.AttackBehaviorGroup.AttackBehaviors = new List<AttackBehaviorGroup.AttackGroupItem>();
            }
            else
            {
                bs.AttackBehaviors = new List<AttackBehaviorBase>();
            }
            //allows enemies to be tinted
            prefab.AddComponent<Tint>();
            prefab.AddComponent<EngageLate>();
            prefab.AddComponent<AIBulletBank>();
            //Add to enemy database
            EnemyDatabaseEntry enemyDatabaseEntry = new EnemyDatabaseEntry()
            {
                myGuid = guid,
                placeableWidth = 2,
                placeableHeight = 2,
                isNormalEnemy = true,
            };

            EnemyDatabase.Instance.Entries.Add(enemyDatabaseEntry);
            EnemyBuilder.Dictionary.Add(guid, prefab);
            //finalize
            GameObject.DontDestroyOnLoad(prefab);
            FakePrefab.MarkAsFakePrefab(prefab);
            prefab.SetActive(false);

            return prefab;
        }

        public static void AddEnemyToDatabase(GameObject EnemyPrefab, string EnemyGUID)
        {
            EnemyDatabaseEntry item = new EnemyDatabaseEntry
            {
                myGuid = EnemyGUID,
                placeableWidth = 2,
                placeableHeight = 2,
                isNormalEnemy = true,
                path = EnemyGUID,
                isInBossTab = false,
                encounterGuid = EnemyGUID
            };
            EnemyDatabase.Instance.Entries.Add(item);
            EncounterDatabaseEntry encounterDatabaseEntry = new EncounterDatabaseEntry(EnemyPrefab.GetComponent<AIActor>().encounterTrackable)
            {
                path = EnemyGUID,
                myGuid = EnemyPrefab.GetComponent<AIActor>().encounterTrackable.EncounterGuid
            };
            EncounterDatabase.Instance.Entries.Add(encounterDatabaseEntry);
        }

        public enum AnimationType { Move, Idle, Fidget, Flight, Hit, Talk, Other }
        public static tk2dSpriteAnimationClip AddAnimation(this GameObject obj, string name, string spriteDirectory, int fps, AnimationType type, DirectionType directionType = DirectionType.None, tk2dSpriteAnimationClip.WrapMode wrapMode = tk2dSpriteAnimationClip.WrapMode.Loop, FlipType flipType = FlipType.None, bool assignAnimation = true)
        {
            AIAnimator aiAnimator = obj.GetOrAddComponent<AIAnimator>();
            DirectionalAnimation animation = aiAnimator.GetDirectionalAnimation(name, directionType, type);
            if (animation == null)
            {
                animation = new DirectionalAnimation()
                {
                    AnimNames = new string[0],
                    Flipped = new FlipType[0],
                    Type = directionType,
                    Prefix = string.Empty
                };
            }

            animation.AnimNames = animation.AnimNames.Concat(new string[] { name }).ToArray();
            animation.Flipped = animation.Flipped.Concat(new FlipType[] { flipType }).ToArray();

            if (assignAnimation)
            {
                aiAnimator.AssignDirectionalAnimation(name, animation, type);
            }
            return BuildAnimation(aiAnimator, name, spriteDirectory, fps, wrapMode);
        }

        public static tk2dSpriteAnimationClip BuildAnimation(AIAnimator aiAnimator, string name, string spriteDirectory, int fps, tk2dSpriteAnimationClip.WrapMode wrapMode = tk2dSpriteAnimationClip.WrapMode.Loop)
        {
            tk2dSpriteCollectionData collection = aiAnimator.GetComponent<tk2dSpriteCollectionData>();
            if (!collection)
                collection = SpriteBuilder.ConstructCollection(aiAnimator.gameObject, $"{aiAnimator.name}_collection");

            string[] resources = ResourceExtractor.GetResourceNames();
            List<int> indices = new List<int>();
            for (int i = 0; i < resources.Length; i++)
            {
                if (resources[i].StartsWith(spriteDirectory.Replace('/', '.'), StringComparison.OrdinalIgnoreCase))
                {
                    indices.Add(SpriteBuilder.AddSpriteToCollection(resources[i], collection));
                }
            }
            tk2dSpriteAnimationClip clip = SpriteBuilder.AddAnimation(aiAnimator.spriteAnimator, collection, indices, name, tk2dSpriteAnimationClip.WrapMode.Loop);
            clip.fps = fps;
            clip.wrapMode = wrapMode;
            return clip;
        }

        public static DirectionalAnimation GetDirectionalAnimation(this AIAnimator aiAnimator, string name, DirectionType directionType, AnimationType type)
        {
            DirectionalAnimation result = null;
            switch (type)
            {
                case AnimationType.Idle:
                    result = aiAnimator.IdleAnimation;
                    break;
                case AnimationType.Move:
                    result = aiAnimator.MoveAnimation;
                    break;
                case AnimationType.Flight:
                    result = aiAnimator.FlightAnimation;
                    break;
                case AnimationType.Hit:
                    result = aiAnimator.HitAnimation;
                    break;
                case AnimationType.Talk:
                    result = aiAnimator.TalkAnimation;
                    break;
            }
            if (result != null)
                return result;

            return null;
        }

        public static void AssignDirectionalAnimation(this AIAnimator aiAnimator, string name, DirectionalAnimation animation, AnimationType type)
        {
            switch (type)
            {
                case AnimationType.Idle:
                    aiAnimator.IdleAnimation = animation;
                    break;
                case AnimationType.Move:
                    aiAnimator.MoveAnimation = animation;
                    break;
                case AnimationType.Flight:
                    aiAnimator.FlightAnimation = animation;
                    break;
                case AnimationType.Hit:
                    aiAnimator.HitAnimation = animation;
                    break;
                case AnimationType.Talk:
                    aiAnimator.TalkAnimation = animation;
                    break;
                case AnimationType.Fidget:
                    aiAnimator.IdleFidgetAnimations.Add(animation);
                    break;
                default:
                    if (aiAnimator.OtherAnimations == null)
                    {
                        aiAnimator.OtherAnimations = new List<AIAnimator.NamedDirectionalAnimation>();
                    }
                    aiAnimator.OtherAnimations.Add(new AIAnimator.NamedDirectionalAnimation()
                    {
                        anim = animation,
                        name = name
                    });
                    break;
            }
        }


        public static void DuplicateAIShooterAndAIBulletBank(GameObject targetObject, AIShooter sourceShooter, AIBulletBank sourceBulletBank, int startingGunOverrideID = 0, Transform gunAttachPointOverride = null, Transform bulletScriptAttachPointOverride = null, PlayerHandController overrideHandObject = null)
        {
            if (targetObject.GetComponent<AIShooter>() && targetObject.GetComponent<AIBulletBank>())
            {
                return;
            }
            if (!targetObject.GetComponent<AIBulletBank>())
            {
                AIBulletBank aibulletBank = targetObject.AddComponent<AIBulletBank>();
                aibulletBank.Bullets = new List<AIBulletBank.Entry>(0);
                if (sourceBulletBank.Bullets.Count > 0)
                {
                    foreach (AIBulletBank.Entry entry in sourceBulletBank.Bullets)
                    {
                        aibulletBank.Bullets.Add(new AIBulletBank.Entry
                        {
                            Name = entry.Name,
                            BulletObject = entry.BulletObject,
                            OverrideProjectile = entry.OverrideProjectile,
                            ProjectileData = new ProjectileData
                            {
                                damage = entry.ProjectileData.damage,
                                speed = entry.ProjectileData.speed,
                                range = entry.ProjectileData.range,
                                force = entry.ProjectileData.force,
                                damping = entry.ProjectileData.damping,
                                UsesCustomAccelerationCurve = entry.ProjectileData.UsesCustomAccelerationCurve,
                                AccelerationCurve = entry.ProjectileData.AccelerationCurve,
                                CustomAccelerationCurveDuration = entry.ProjectileData.CustomAccelerationCurveDuration,
                                onDestroyBulletScript = entry.ProjectileData.onDestroyBulletScript,
                                IgnoreAccelCurveTime = entry.ProjectileData.IgnoreAccelCurveTime
                            },
                            PlayAudio = entry.PlayAudio,
                            AudioSwitch = entry.AudioSwitch,
                            AudioEvent = entry.AudioEvent,
                            AudioLimitOncePerFrame = entry.AudioLimitOncePerFrame,
                            AudioLimitOncePerAttack = entry.AudioLimitOncePerAttack,
                            MuzzleFlashEffects = new VFXPool
                            {
                                effects = entry.MuzzleFlashEffects.effects,
                                type = entry.MuzzleFlashEffects.type
                            },
                            MuzzleLimitOncePerFrame = entry.MuzzleLimitOncePerFrame,
                            MuzzleInheritsTransformDirection = entry.MuzzleInheritsTransformDirection,
                            ShellTransform = entry.ShellTransform,
                            ShellPrefab = entry.ShellPrefab,
                            ShellForce = entry.ShellForce,
                            ShellForceVariance = entry.ShellForceVariance,
                            DontRotateShell = entry.DontRotateShell,
                            ShellGroundOffset = entry.ShellGroundOffset,
                            ShellsLimitOncePerFrame = entry.ShellsLimitOncePerFrame,
                            rampBullets = entry.rampBullets,
                            conditionalMinDegFromNorth = entry.conditionalMinDegFromNorth,
                            forceCanHitEnemies = entry.forceCanHitEnemies,
                            suppressHitEffectsIfOffscreen = entry.suppressHitEffectsIfOffscreen,
                            preloadCount = entry.preloadCount
                        });
                    }
                }
                aibulletBank.useDefaultBulletIfMissing = true;
                aibulletBank.transforms = new List<Transform>();
                if (sourceBulletBank.transforms != null && sourceBulletBank.transforms.Count > 0)
                {
                    foreach (Transform item in sourceBulletBank.transforms)
                    {
                        aibulletBank.transforms.Add(item);
                    }
                }
                aibulletBank.RegenerateCache();
            }
            if (!targetObject.GetComponent<AIShooter>())
            {
                AIShooter aishooter = targetObject.AddComponent<AIShooter>();
                aishooter.volley = sourceShooter.volley;
                if (startingGunOverrideID != 0)
                {
                    aishooter.equippedGunId = startingGunOverrideID;
                }
                else
                {
                    aishooter.equippedGunId = sourceShooter.equippedGunId;
                }
                aishooter.shouldUseGunReload = true;
                aishooter.volleyShootPosition = sourceShooter.volleyShootPosition;
                aishooter.volleyShellCasing = sourceShooter.volleyShellCasing;
                aishooter.volleyShellTransform = sourceShooter.volleyShellTransform;
                aishooter.volleyShootVfx = sourceShooter.volleyShootVfx;
                aishooter.usesOctantShootVFX = sourceShooter.usesOctantShootVFX;
                aishooter.bulletName = sourceShooter.bulletName;
                aishooter.customShootCooldownPeriod = sourceShooter.customShootCooldownPeriod;
                aishooter.doesScreenShake = sourceShooter.doesScreenShake;
                aishooter.rampBullets = sourceShooter.rampBullets;
                aishooter.rampStartHeight = sourceShooter.rampStartHeight;
                aishooter.rampTime = sourceShooter.rampTime;
                if (gunAttachPointOverride)
                {
                    aishooter.gunAttachPoint = gunAttachPointOverride;
                }
                else
                {
                    aishooter.gunAttachPoint = sourceShooter.gunAttachPoint;
                }
                if (bulletScriptAttachPointOverride)
                {
                    aishooter.bulletScriptAttachPoint = bulletScriptAttachPointOverride;
                }
                else
                {
                    aishooter.bulletScriptAttachPoint = sourceShooter.bulletScriptAttachPoint;
                }
                aishooter.overallGunAttachOffset = sourceShooter.overallGunAttachOffset;
                aishooter.flippedGunAttachOffset = sourceShooter.flippedGunAttachOffset;
                if (overrideHandObject)
                {
                    aishooter.handObject = overrideHandObject;
                }
                else
                {
                    aishooter.handObject = sourceShooter.handObject;
                }
                aishooter.AllowTwoHands = sourceShooter.AllowTwoHands;
                aishooter.ForceGunOnTop = sourceShooter.ForceGunOnTop;
                aishooter.IsReallyBigBoy = sourceShooter.IsReallyBigBoy;
                aishooter.BackupAimInMoveDirection = sourceShooter.BackupAimInMoveDirection;
                aishooter.RegenerateCache();

            }
        }
    }

    public class EngageLate : BraveBehaviour
    {
        private RoomHandler m_StartRoom;
        private void Update()
        {
            if (!base.aiActor.HasBeenEngaged) { CheckPlayerRoom(); }
        }
        private void CheckPlayerRoom()
        {

            if (GameManager.Instance.PrimaryPlayer.GetAbsoluteParentRoom() != null && GameManager.Instance.PrimaryPlayer.GetAbsoluteParentRoom() == m_StartRoom)
            {
                GameManager.Instance.StartCoroutine(LateEngage());
            }

        }
        private IEnumerator LateEngage()
        {
            yield return new WaitForSeconds(0.8f);
            base.aiActor.HasBeenEngaged = true;
            yield break;
        }
        private void Start()
        {
            m_StartRoom = aiActor.GetAbsoluteParentRoom();
        }


    }
    public class Tint : BraveBehaviour
    {
        private void Start()
        {
            DisableSuperTinting(base.aiActor);
        }
        public static void DisableSuperTinting(AIActor actor)
        {
            Material mat = actor.sprite.renderer.material;
            mat.mainTexture = actor.sprite.renderer.material.mainTexture;
            mat.EnableKeyword("BRIGHTNESS_CLAMP_ON");
            mat.DisableKeyword("BRIGHTNESS_CLAMP_OFF");
        }
    }

}*/