using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Alexandria.Misc;
using UnityEngine;
using Alexandria.ItemAPI;
using Dungeonator;
using System.Collections;

namespace NevernamedsItems
{
    class OrganDonorCard : PlayerItem
    {
        public static void Init()
        {
            string itemName = "Organ Donor Card";
            string resourceName = "NevernamedsItems/Resources/organdonorcard_icon";
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent<OrganDonorCard>();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);
            string shortDesc = "Gift of Life";
            string longDesc = "Donate your hearts to someone who needs them." + "\n\nCompensates you handsomely- You might even make a new friend! (The hole left in your  body is also a nifty place to store active items)" + "\n\nNot reccomended for use by perverted Turtles.";
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "nn");
            ItemBuilder.SetCooldownType(item, ItemBuilder.CooldownType.Timed, 5);
            ItemBuilder.AddPassiveStatModifier(item, PlayerStats.StatType.AdditionalItemCapacity, 1f, StatModifier.ModifyMethod.ADDITIVE);
            item.consumable = false;
            item.quality = ItemQuality.D;

            heartCompanionBullet = ProjectileUtility.SetupProjectile(56);
            heartCompanionBullet.baseData.damage = 20f;
            heartCompanionBullet.pierceMinorBreakables = true;
            heartCompanionBullet.baseData.range *= 10f;
            heartCompanionBullet.SetProjectileSpriteRight("viscerifle_heart_projectile", 16, 7, false, tk2dBaseSprite.Anchor.MiddleCenter, 16, 7);
            GoopModifier blood = heartCompanionBullet.gameObject.AddComponent<GoopModifier>();
            blood.goopDefinition = EasyGoopDefinitions.BlobulonGoopDef;
            blood.SpawnGoopInFlight = true;
            blood.InFlightSpawnFrequency = 0.05f;
            blood.InFlightSpawnRadius = 1f;
            blood.SpawnGoopOnCollision = true;
            blood.CollisionSpawnRadius = 2f;

            BuildPrefab();
        }
        public static GameObject heartCompanionPrefab;
        public static Projectile heartCompanionBullet;
        public static string heartCompanionGUID = "omitb_organdonorcard_heartcompanion";
        public int numberOfHeartBuddies;
        private List<GameObject> ExtantCompanions;
        public override void Pickup(PlayerController player)
        {
            if (ExtantCompanions == null) { ExtantCompanions = new List<GameObject>(); }
            if (!m_pickedUpThisRun) { numberOfHeartBuddies = 0; }
            StartCoroutine(RecalculateCompanions(player));
            GameManager.Instance.OnNewLevelFullyLoaded += this.OnNewFloor;

            base.Pickup(player);
        }
        public override void OnPreDrop(PlayerController user)
        {
            DestroyAllCompanions();
            GameManager.Instance.OnNewLevelFullyLoaded -= this.OnNewFloor;
            base.OnPreDrop(user);
        }
        public override void OnDestroy()
        {
            DestroyAllCompanions();
            GameManager.Instance.OnNewLevelFullyLoaded -= this.OnNewFloor;
            base.OnDestroy();
        }
        private void OnNewFloor()
        {
            if (LastOwner) StartCoroutine(RecalculateCompanions(LastOwner));
        }

        public IEnumerator SpawnNewCompanion(PlayerController user)
        {
            
            GameObject spawnedCompanion = UnityEngine.Object.Instantiate<GameObject>(EnemyDatabase.GetOrLoadByGuid(heartCompanionGUID).gameObject, user.specRigidbody.UnitCenter, Quaternion.identity);
            
            OrganDonorCardCompanionController compController = spawnedCompanion.GetOrAddComponent<OrganDonorCardCompanionController>();
            
            ExtantCompanions.Add(spawnedCompanion);
            
            compController.Initialize(user);
            
            if (compController.specRigidbody) PhysicsEngine.Instance.RegisterOverlappingGhostCollisionExceptions(compController.specRigidbody, null, false);
            
            yield return null;
           
            if (compController.knockbackDoer) compController.knockbackDoer.ApplyKnockback(UnityEngine.Random.insideUnitCircle, 50);
            
            if (compController.aiAnimator) compController.aiAnimator.PlayUntilFinished("spawn", false, null, -1f, false);
           
            yield break;
        }
        public void DestroyAllCompanions()
        {
            if (ExtantCompanions.Count <= 0) { return; }
            for (int i = ExtantCompanions.Count - 1; i >= 0; i--)
            {
                if (ExtantCompanions[i] && ExtantCompanions[i].gameObject) { UnityEngine.Object.Destroy(ExtantCompanions[i].gameObject); }
            }
            ExtantCompanions.Clear();
        }
        public IEnumerator RecalculateCompanions(PlayerController player)
        {
            yield return null;
            for (int i = 0; i < numberOfHeartBuddies; i++)
            {
                StartCoroutine(SpawnNewCompanion(player));
                yield return new WaitForSeconds(0.1f);
            }
            yield break;
        }
        public override void DoEffect(PlayerController user)
        {
            int cashToGive = 20;
            if (LastOwner.PlayerHasActiveSynergy("Heart so wet and cold...")) cashToGive *= 2;
            if (LastOwner.PlayerHasActiveSynergy("Do-Gooder")) cashToGive *= 2;

            LootEngine.SpawnCurrency(user.specRigidbody.UnitCenter, cashToGive);

            SpawnManager.SpawnVFX(EasyVFXDatabase.BloodExplosion, user.specRigidbody.UnitCenter, Quaternion.identity);

            PlayableCharacters characterIdentity = user.characterIdentity;

            if (user.ForceZeroHealthState)
            {
                user.healthHaver.Armor = user.healthHaver.Armor - 2;
            }
            else
            {
                StatModifier health = new StatModifier()
                {
                    amount = -1,
                    statToBoost = PlayerStats.StatType.Health,
                    modifyType = StatModifier.ModifyMethod.ADDITIVE,
                };
                user.ownerlessStatModifiers.Add(health);
            }

            StatModifier itemCap = new StatModifier()
            {
                amount = 1,
                statToBoost = PlayerStats.StatType.AdditionalItemCapacity,
                modifyType = StatModifier.ModifyMethod.ADDITIVE,
            };

            user.ownerlessStatModifiers.Add(itemCap);
            user.stats.RecalculateStats(user);

            StartCoroutine(SpawnNewCompanion(user));
            numberOfHeartBuddies++;
        }
        public override bool CanBeUsed(PlayerController user)
        {
            if (user.ForceZeroHealthState) return user.healthHaver.Armor > 2;
            else return user.healthHaver.GetMaxHealth() > 1f;
        }
        public static void BuildPrefab()
        {
            if (!(heartCompanionPrefab != null || CompanionBuilder.companionDictionary.ContainsKey(heartCompanionGUID)))
            {
                heartCompanionPrefab = CompanionBuilder.BuildPrefab("OrganDonor HeartBud", heartCompanionGUID, "NevernamedsItems/Resources/Companions/OrganDonorCompanions/organdonorcompanion_idlesouth_001", new IntVector2(8, 2), new IntVector2(5, 5));
                var companionController = heartCompanionPrefab.AddComponent<OrganDonorCardCompanionController>();
                companionController.aiActor.MovementSpeed = 6f;

                AIAnimator animator = heartCompanionPrefab.GetOrAddComponent<AIAnimator>();

                animator.AdvAddAnimation("idle",
                    DirectionalAnimation.DirectionType.FourWayCardinal,
                    CompanionBuilder.AnimationType.Idle,
                    new List<AnimationUtilityExtensions.DirectionalAnimationData>()
                    {
                        new AnimationUtilityExtensions.DirectionalAnimationData()
                        {
                            subAnimationName = "idle_north",
                            wrapMode = tk2dSpriteAnimationClip.WrapMode.Loop,
                            fps = 9,
                            pathDirectory = "NevernamedsItems/Resources/Companions/OrganDonorCompanions/organdonorcompanion_idlenorth",
                        },
                        new AnimationUtilityExtensions.DirectionalAnimationData()
                        {
                            subAnimationName = "idle_east",
                            wrapMode = tk2dSpriteAnimationClip.WrapMode.Loop,
                            fps = 9,
                            pathDirectory = "NevernamedsItems/Resources/Companions/OrganDonorCompanions/organdonorcompanion_idleeast",
                        },
                        new AnimationUtilityExtensions.DirectionalAnimationData()
                        {
                            subAnimationName = "idle_south",
                            wrapMode = tk2dSpriteAnimationClip.WrapMode.Loop,
                            fps = 9,
                            pathDirectory = "NevernamedsItems/Resources/Companions/OrganDonorCompanions/organdonorcompanion_idlesouth",
                        },
                        new AnimationUtilityExtensions.DirectionalAnimationData()
                        {
                            subAnimationName = "idle_west",
                            wrapMode = tk2dSpriteAnimationClip.WrapMode.Loop,
                            fps = 9,
                            pathDirectory = "NevernamedsItems/Resources/Companions/OrganDonorCompanions/organdonorcompanion_idlewest",
                        },
                    }
                    );
                animator.AdvAddAnimation("move",
                    DirectionalAnimation.DirectionType.FourWayCardinal,
                    CompanionBuilder.AnimationType.Move,
                    new List<AnimationUtilityExtensions.DirectionalAnimationData>()
                    {
                        new AnimationUtilityExtensions.DirectionalAnimationData()
                        {
                            subAnimationName = "move_north",
                            wrapMode = tk2dSpriteAnimationClip.WrapMode.Loop,
                            fps = 9,
                            pathDirectory = "NevernamedsItems/Resources/Companions/OrganDonorCompanions/organdonorcompanion_movenorth",
                        },
                        new AnimationUtilityExtensions.DirectionalAnimationData()
                        {
                            subAnimationName = "move_east",
                            wrapMode = tk2dSpriteAnimationClip.WrapMode.Loop,
                            fps = 9,
                            pathDirectory = "NevernamedsItems/Resources/Companions/OrganDonorCompanions/organdonorcompanion_moveeast",
                        },
                        new AnimationUtilityExtensions.DirectionalAnimationData()
                        {
                            subAnimationName = "move_south",
                            wrapMode = tk2dSpriteAnimationClip.WrapMode.Loop,
                            fps = 9,
                            pathDirectory = "NevernamedsItems/Resources/Companions/OrganDonorCompanions/organdonorcompanion_movesouth",
                        },
                        new AnimationUtilityExtensions.DirectionalAnimationData()
                        {
                            subAnimationName = "move_west",
                            wrapMode = tk2dSpriteAnimationClip.WrapMode.Loop,
                            fps = 9,
                            pathDirectory = "NevernamedsItems/Resources/Companions/OrganDonorCompanions/organdonorcompanion_movewest",
                        },
                    }
                    );
                animator.AdvAddAnimation("spawn",
                    DirectionalAnimation.DirectionType.Single,
                    CompanionBuilder.AnimationType.Other,
                    new List<AnimationUtilityExtensions.DirectionalAnimationData>()
                    {
                        new AnimationUtilityExtensions.DirectionalAnimationData()
                        {
                            subAnimationName = "spawn",
                            wrapMode = tk2dSpriteAnimationClip.WrapMode.Once,
                            fps = 9,
                            pathDirectory = "NevernamedsItems/Resources/Companions/OrganDonorCompanions/organdonorcompanion_spawn",
                        },
                    }
                    );
                animator.AdvAddAnimation("attack",
                    DirectionalAnimation.DirectionType.Single,
                    CompanionBuilder.AnimationType.Other,
                    new List<AnimationUtilityExtensions.DirectionalAnimationData>()
                    {
                        new AnimationUtilityExtensions.DirectionalAnimationData()
                        {
                            subAnimationName = "attack",
                            wrapMode = tk2dSpriteAnimationClip.WrapMode.Once,
                            fps = 9,
                            pathDirectory = "NevernamedsItems/Resources/Companions/OrganDonorCompanions/organdonorcompanion_attack",
                        },
                    }
                    );
                BehaviorSpeculator component = heartCompanionPrefab.GetComponent<BehaviorSpeculator>();

                CustomCompanionBehaviours.SimpleCompanionApproach approach = new CustomCompanionBehaviours.SimpleCompanionApproach();
                approach.DesiredDistance = 5f;
                component.MovementBehaviors.Add(approach);
                component.MovementBehaviors.Add(new CompanionFollowPlayerBehavior
                {
                    IdleAnimations = new string[]
                    {
                        "idle"
                    },
                    CatchUpRadius = 6,
                    CatchUpMaxSpeed = 10,
                    CatchUpAccelTime = 1,
                    CatchUpSpeed = 7,
                    PathInterval = 0.1f
                });
            }
        }

        public class OrganDonorCardCompanionController : CompanionController
        {
            private void Start()
            {
                this.Owner = this.m_owner;
                timer = 3f;

            }

            public IEnumerator ForceAttack()
            {
                this.aiAnimator.PlayUntilFinished("attack", false, null, -1f, false);
                yield return new WaitForSeconds(0.22f);
                if (this.gameObject != null)
                {
                    GameObject obj = heartCompanionBullet.InstantiateAndFireTowardsPosition(base.specRigidbody.UnitCenter, base.specRigidbody.UnitCenter.GetNearestEnemyToPosition().sprite.WorldCenter, 0, 10, Owner);
                    Projectile self = obj.GetComponent<Projectile>();
                    self.Owner = Owner;
                    self.Shooter = base.specRigidbody;
                    self.baseData.damage *= Owner.stats.GetStatValue(PlayerStats.StatType.Damage);
                    self.baseData.speed *= Owner.stats.GetStatValue(PlayerStats.StatType.ProjectileSpeed);
                    self.ApplyCompanionModifierToBullet(Owner);
                }
                yield break;
            }

            public override void Update()
            {
                if (Owner && !Dungeon.IsGenerating && Owner.IsInCombat && base.transform.position.GetAbsoluteRoom() == Owner.CurrentRoom)
                {
                    if (timer > 0)
                    {
                        timer -= BraveTime.DeltaTime;
                    }
                    if (timer <= 0)
                    {
                        StartCoroutine(ForceAttack());
                        timer = 3f;
                    }
                }
            }
            private float timer;
            public PlayerController Owner;
        }
    }
}
