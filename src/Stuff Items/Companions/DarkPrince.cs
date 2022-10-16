using System;
using System.Collections;
using System.Collections.Generic;
using Dungeonator;
using Gungeon;
using Alexandria.ItemAPI;
using SaveAPI;
using UnityEngine;
using Alexandria.Misc;
using Alexandria.EnemyAPI;

namespace NevernamedsItems
{
    public class DarkPrince : PassiveItem
    {
        public static void Init()
        {
            string name = "Dark Prince";
            string resourcePath = "NevernamedsItems/Resources/Companions/DarkPrince/darkprince_icon";
            GameObject gameObject = new GameObject();
            CompanionItem companionItem = gameObject.AddComponent<CompanionItem>();
            ItemBuilder.AddSpriteToObject(name, resourcePath, gameObject);
            string shortDesc = "Wicked Heir";
            string longDesc = "This young Gunjurer is the unlikely heir to a vast magical empire, operating from beyond the curtain... he is also very impressionable, with his dark magics able to be put to fantastic use.";
            companionItem.SetupItem(shortDesc, longDesc, "nn");
            companionItem.quality = PickupObject.ItemQuality.S;
            companionItem.CompanionGuid = DarkPrince.guid;
            DarkPrince.BuildPrefab();

            companionItem.AddToSubShop(ItemBuilder.ShopType.Cursula);

            DarkPrinceProjectile = (PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0].gameObject.InstantiateAndFakeprefab();
            Projectile darkProj = DarkPrinceProjectile.GetComponent<Projectile>();
            darkProj.baseData.damage = 1;
            darkProj.baseData.speed = 100;
            darkProj.SetProjectileSpriteRight("laserwelder_proj", 10, 3, true, tk2dBaseSprite.Anchor.MiddleCenter, 10, 3);
            ProjWeaknessModifier weakness = darkProj.gameObject.AddComponent<ProjWeaknessModifier>();
            weakness.UsesSeparateStatsForBosses = true;
            weakness.isDarkPrince = true;
            HomingModifier homin = darkProj.gameObject.AddComponent<HomingModifier>();
            homin.AngularVelocity = 400;
            homin.HomingRadius = 200;

            List<string> BeamAnimPaths = new List<string>()
            {
                "NevernamedsItems/Resources/TrailSprites/darkprince_trail_001",
                "NevernamedsItems/Resources/TrailSprites/darkprince_trail_002",
                "NevernamedsItems/Resources/TrailSprites/darkprince_trail_003",
                "NevernamedsItems/Resources/TrailSprites/darkprince_trail_004",
                "NevernamedsItems/Resources/TrailSprites/darkprince_trail_005",
            };

            darkProj.AddTrailToProjectile(
                "NevernamedsItems/Resources/TrailSprites/darkprince_trail_001",
                new Vector2(21, 1),
                new Vector2(0, 10),
                BeamAnimPaths, 10,
                null, 20,
                0.1f,
                0.1f,
                -1,
                true
                );

        }
        public static GameObject DarkPrinceProjectile;

        public static void BuildPrefab()
        {
            if (!(DarkPrince.prefab != null || CompanionBuilder.companionDictionary.ContainsKey(DarkPrince.guid)))
            {
                DarkPrince.prefab = CompanionBuilder.BuildPrefab("Dark Prince", DarkPrince.guid, "NevernamedsItems/Resources/Companions/DarkPrince/darkprince_idlefrontright_001", new IntVector2(6, 1), new IntVector2(5, 5));
                var companionController = DarkPrince.prefab.AddComponent<DarkPrinceController>();
                companionController.aiActor.MovementSpeed = 4f;

                AIAnimator animator = DarkPrince.prefab.GetOrAddComponent<AIAnimator>();

                animator.AdvAddAnimation("idle",
                    DirectionalAnimation.DirectionType.FourWay,
                    CompanionBuilder.AnimationType.Idle,
                    new List<AnimationUtilityExtensions.DirectionalAnimationData>()
                    {
                        new AnimationUtilityExtensions.DirectionalAnimationData()
                        {
                            subAnimationName = "idle_back_right",
                            wrapMode = tk2dSpriteAnimationClip.WrapMode.Loop,
                            fps = 9,
                            pathDirectory = "NevernamedsItems/Resources/Companions/DarkPrince/darkprince_idlebackright",
                        },
                        new AnimationUtilityExtensions.DirectionalAnimationData()
                        {
                            subAnimationName = "idle_front_right",
                            wrapMode = tk2dSpriteAnimationClip.WrapMode.Loop,
                            fps = 9,
                            pathDirectory = "NevernamedsItems/Resources/Companions/DarkPrince/darkprince_idlefrontright",
                        },
                        new AnimationUtilityExtensions.DirectionalAnimationData()
                        {
                            subAnimationName = "idle_front_left",
                            wrapMode = tk2dSpriteAnimationClip.WrapMode.Loop,
                            fps = 9,
                            pathDirectory = "NevernamedsItems/Resources/Companions/DarkPrince/darkprince_idlefrontleft",
                        },
                        new AnimationUtilityExtensions.DirectionalAnimationData()
                        {
                            subAnimationName = "idle_back_left",
                            wrapMode = tk2dSpriteAnimationClip.WrapMode.Loop,
                            fps = 9,
                            pathDirectory = "NevernamedsItems/Resources/Companions/DarkPrince/darkprince_idlebackleft",
                        },
                    }
                    );

                animator.AdvAddAnimation("move",
                    DirectionalAnimation.DirectionType.FourWay,
                    CompanionBuilder.AnimationType.Move,
                    new List<AnimationUtilityExtensions.DirectionalAnimationData>()
                    {
                        new AnimationUtilityExtensions.DirectionalAnimationData()
                        {
                            subAnimationName = "move_back_right",
                            wrapMode = tk2dSpriteAnimationClip.WrapMode.Loop,
                            fps = 9,
                            pathDirectory = "NevernamedsItems/Resources/Companions/DarkPrince/darkprince_walkbackright",
                        },
                        new AnimationUtilityExtensions.DirectionalAnimationData()
                        {
                            subAnimationName = "move_front_right",
                            wrapMode = tk2dSpriteAnimationClip.WrapMode.Loop,
                            fps = 9,
                            pathDirectory = "NevernamedsItems/Resources/Companions/DarkPrince/darkprince_walkfrontright",
                        },
                        new AnimationUtilityExtensions.DirectionalAnimationData()
                        {
                            subAnimationName = "move_front_left",
                            wrapMode = tk2dSpriteAnimationClip.WrapMode.Loop,
                            fps = 9,
                            pathDirectory = "NevernamedsItems/Resources/Companions/DarkPrince/darkprince_walkfrontleft",
                        },
                        new AnimationUtilityExtensions.DirectionalAnimationData()
                        {
                            subAnimationName = "move_back_left",
                            wrapMode = tk2dSpriteAnimationClip.WrapMode.Loop,
                            fps = 9,
                            pathDirectory = "NevernamedsItems/Resources/Companions/DarkPrince/darkprince_walkbackleft",
                        },
                    }
                    );

                animator.AdvAddAnimation("attack",
                    DirectionalAnimation.DirectionType.FourWay,
                    CompanionBuilder.AnimationType.Other,
                    new List<AnimationUtilityExtensions.DirectionalAnimationData>()
                    {
                        new AnimationUtilityExtensions.DirectionalAnimationData()
                        {
                            subAnimationName = "attack_back_right",
                            wrapMode = tk2dSpriteAnimationClip.WrapMode.Once,
                            fps = 9,
                            pathDirectory = "NevernamedsItems/Resources/Companions/DarkPrince/darkprince_attackbackright",
                        },
                        new AnimationUtilityExtensions.DirectionalAnimationData()
                        {
                            subAnimationName = "attack_front_right",
                            wrapMode = tk2dSpriteAnimationClip.WrapMode.Once,
                            fps = 9,
                            pathDirectory = "NevernamedsItems/Resources/Companions/DarkPrince/darkprince_attackfrontright",
                        },
                        new AnimationUtilityExtensions.DirectionalAnimationData()
                        {
                            subAnimationName = "attack_front_left",
                            wrapMode = tk2dSpriteAnimationClip.WrapMode.Once,
                            fps = 9,
                            pathDirectory = "NevernamedsItems/Resources/Companions/DarkPrince/darkprince_attackfrontleft",
                        },
                        new AnimationUtilityExtensions.DirectionalAnimationData()
                        {
                            subAnimationName = "attack_back_left",
                            wrapMode = tk2dSpriteAnimationClip.WrapMode.Once,
                            fps = 9,
                            pathDirectory = "NevernamedsItems/Resources/Companions/DarkPrince/darkprince_attackbackleft",
                        },
                    }
                    );


                BehaviorSpeculator component = DarkPrince.prefab.GetComponent<BehaviorSpeculator>();
                CustomCompanionBehaviours.SimpleCompanionApproach approach = new CustomCompanionBehaviours.SimpleCompanionApproach();
                approach.DesiredDistance = 7f;
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

                });
            }
        }
        public class DebuffedByDarkPrince : MonoBehaviour { }
        
        public class DarkPrinceController : CompanionController
        {
            private void Start()
            {
                this.Owner = this.m_owner;
                attackTimer = 2f;
            }
            public override void Update()
            {
                if (base.aiActor.OverrideTarget)
                {
                    if (base.aiActor.OverrideTarget.GetComponent<DebuffedByDarkPrince>() != null) base.aiActor.OverrideTarget = null;
                }
                if (Owner && !Dungeon.IsGenerating && Owner.IsInCombat && base.transform.position.GetAbsoluteRoom() == Owner.CurrentRoom)
                {
                    if (attackTimer > 0)
                    {
                        attackTimer -= BraveTime.DeltaTime;
                    }
                    else
                    {
                        if (base.aiActor.OverrideTarget)
                        {
                            Vector2 pointOnTarget = base.aiActor.OverrideTarget.ClosestPointOnRigidBody(base.sprite.WorldCenter);
                            if (pointOnTarget != Vector2.zero)
                            {
                                if (!isAttacking)
                                {
                                    StartCoroutine(Attack(pointOnTarget));
                                }
                            }

                        }
                    }
                }
            }
            private IEnumerator Attack(Vector2 pointOnTarget)
            {
                base.aiActor.MovementSpeed = 0;
                isAttacking = true;
                base.aiAnimator.PlayUntilFinished("attack");

                yield return new WaitForSeconds(0.33f);
                AkSoundEngine.PostEvent("Play_ENM_wizardred_shoot_01", base.gameObject);
                GameObject projobj = ProjSpawnHelper.SpawnProjectileTowardsPoint(DarkPrinceProjectile, base.sprite.WorldCenter, pointOnTarget);
                Projectile proj = projobj.GetComponent<Projectile>();
                if (proj)
                {
                    proj.Owner = Owner;
                    proj.Shooter = base.specRigidbody;

                    proj.baseData.damage *= Owner.stats.GetStatValue(PlayerStats.StatType.Damage);
                    proj.baseData.speed *= Owner.stats.GetStatValue(PlayerStats.StatType.ProjectileSpeed);
                    proj.baseData.range *= Owner.stats.GetStatValue(PlayerStats.StatType.RangeMultiplier);
                    proj.baseData.force *= Owner.stats.GetStatValue(PlayerStats.StatType.KnockbackMultiplier);
                    proj.BossDamageMultiplier *= Owner.stats.GetStatValue(PlayerStats.StatType.DamageToBosses);
                    proj.UpdateSpeed();
                    proj.ApplyCompanionModifierToBullet(Owner);
                    Owner.DoPostProcessProjectile(proj);
                }
                yield return new WaitForSeconds(0.1f);
                base.aiActor.MovementSpeed = base.aiActor.BaseMovementSpeed;
                isAttacking = false;
                attackTimer = 2;
                yield break;
            }
            public PlayerController Owner;
            private float attackTimer;
            private bool isAttacking;
        }
        public static GameObject prefab;
        private static readonly string guid = "darkprincei98qy7wiyf4ugwu6sudtsfu6sdf";
    }
}