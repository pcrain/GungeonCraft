using System;
using System.Collections;
using System.Collections.Generic;
using Dungeonator;
using Gungeon;
using Alexandria.ItemAPI;
using SaveAPI;
using UnityEngine;
using Alexandria.Misc;

namespace NevernamedsItems
{
    public class FunGuy : CompanionItem
    {
        public static void Init()
        {
            string name = "Fun Guy";
            string resourcePath = "NevernamedsItems/Resources/Companions/FunGuy/funguy_icon";
            GameObject gameObject = new GameObject();
            CompanionItem companionItem = gameObject.AddComponent<FunGuy>();
            ItemBuilder.AddSpriteToObject(name, resourcePath, gameObject);
            string shortDesc = "Myc-inda Fella";
            string longDesc = "This young fungun has become dissilusioned with the ways of the Gungeon. Or at least, it seems that way. It's unclear if it really knows what's going on.";
            companionItem.SetupItem(shortDesc, longDesc, "nn");
            companionItem.quality = PickupObject.ItemQuality.C;
            companionItem.CompanionGuid = FunGuy.guid;
            FunGuy.BuildPrefab();

            synergyFungusProj = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(86) as Gun).DefaultModule.projectiles[0]);
            synergyFungusProj.gameObject.MakeFakePrefab();
            synergyFungusProj.SetProjectileSpriteRight("yellowtriangleproj_001", 19, 17, true, tk2dBaseSprite.Anchor.MiddleCenter, 9, 9);
            FungoRandomBullets orAddComponent = synergyFungusProj.gameObject.GetOrAddComponent<FungoRandomBullets>();
            synergyFungusProj.baseData.speed *= 0.2f;
            synergyFungusProj.baseData.damage = 12.5f;
            PierceProjModifier pierce = synergyFungusProj.gameObject.AddComponent<PierceProjModifier>();
            pierce.penetration = 1;
            pierce.penetratesBreakables = true;
            synergyFungusProj.gameObject.AddComponent<PierceDeadActors>();
            synergyFungusProj.pierceMinorBreakables = true;
            synergyFungusProj.AnimateProjectile(new List<string> {
                "yellowtriangleproj_001",
                "yellowtriangleproj_002",
                "yellowtriangleproj_003",
                "yellowtriangleproj_004",
            }, 10, true, new List<IntVector2> {
                new IntVector2(19, 17), 
                new IntVector2(19, 17), 
                new IntVector2(19, 17), 
                new IntVector2(19, 17), 
            }, 
            AnimateBullet.ConstructListOfSameValues(true, 4), 
            AnimateBullet.ConstructListOfSameValues(tk2dBaseSprite.Anchor.MiddleCenter, 4), 
            AnimateBullet.ConstructListOfSameValues(true, 4), 
            AnimateBullet.ConstructListOfSameValues(false, 4),
            AnimateBullet.ConstructListOfSameValues<Vector3?>(null, 4), 
            AnimateBullet.ConstructListOfSameValues<IntVector2?>(new IntVector2(9, 9),4), 
            AnimateBullet.ConstructListOfSameValues<IntVector2?>(new IntVector2(5, 3), 4), 
            AnimateBullet.ConstructListOfSameValues<Projectile>(null, 4));
        }

        public static Projectile synergyFungusProj;
        public override void Update()
        {
            if (this.ExtantCompanion && Owner)
            {
                if (this.ExtantCompanion.GetComponent<AIActor>().EnemyGuid == synergyguid && !Owner.PlayerHasActiveSynergy("Pointiest One"))
                {
                    this.CompanionGuid = guid;
                    ForceCompanionRegeneration(Owner, null);
                }
                else if (this.ExtantCompanion.GetComponent<AIActor>().EnemyGuid != synergyguid && Owner.PlayerHasActiveSynergy("Pointiest One"))
                {
                    this.CompanionGuid = synergyguid;
                    ForceCompanionRegeneration(Owner, null);
                }
            }
            base.Update();
        }

        public static void BuildPrefab()
        {
            if (!(FunGuy.prefab != null || CompanionBuilder.companionDictionary.ContainsKey(FunGuy.guid)))
            {
                FunGuy.prefab = CompanionBuilder.BuildPrefab("Fun Guy", FunGuy.guid, "NevernamedsItems/Resources/Companions/FunGuy/funguy_idleright_001", new IntVector2(8, 2), new IntVector2(9, 11));
                var companionController = FunGuy.prefab.AddComponent<FunGuyController>();
                companionController.aiActor.MovementSpeed = 5f;
                companionController.specRigidbody.CollideWithTileMap = false;
                AIAnimator animator = FunGuy.prefab.GetOrAddComponent<AIAnimator>();

                animator.AdvAddAnimation("idle",
                    DirectionalAnimation.DirectionType.TwoWayHorizontal,
                    CompanionBuilder.AnimationType.Idle,
                    new List<AnimationUtilityExtensions.DirectionalAnimationData>()
                    {
                        new AnimationUtilityExtensions.DirectionalAnimationData()
                        {
                            subAnimationName = "idle_right",
                            wrapMode = tk2dSpriteAnimationClip.WrapMode.Loop,
                            fps = 9,
                            pathDirectory = "NevernamedsItems/Resources/Companions/FunGuy/funguy_idleright",
                        },
                        new AnimationUtilityExtensions.DirectionalAnimationData()
                        {
                            subAnimationName = "idle_left",
                            wrapMode = tk2dSpriteAnimationClip.WrapMode.Loop,
                            fps = 9,
                            pathDirectory = "NevernamedsItems/Resources/Companions/FunGuy/funguy_idleleft",
                        }
                    }
                    );
                animator.AdvAddAnimation("move",
                    DirectionalAnimation.DirectionType.TwoWayHorizontal,
                    CompanionBuilder.AnimationType.Move,
                    new List<AnimationUtilityExtensions.DirectionalAnimationData>()
                    {
                        new AnimationUtilityExtensions.DirectionalAnimationData()
                        {
                            subAnimationName = "move_right",
                            wrapMode = tk2dSpriteAnimationClip.WrapMode.Loop,
                            fps = 9,
                            pathDirectory = "NevernamedsItems/Resources/Companions/FunGuy/funguy_walkright",
                        },
                        new AnimationUtilityExtensions.DirectionalAnimationData()
                        {
                            subAnimationName = "move_left",
                            wrapMode = tk2dSpriteAnimationClip.WrapMode.Loop,
                            fps = 9,
                            pathDirectory = "NevernamedsItems/Resources/Companions/FunGuy/funguy_moveleft",
                        }
                    }
                    );
                animator.AdvAddAnimation("attack",
                    DirectionalAnimation.DirectionType.TwoWayHorizontal,
                    CompanionBuilder.AnimationType.Other,
                    new List<AnimationUtilityExtensions.DirectionalAnimationData>()
                    {
                        new AnimationUtilityExtensions.DirectionalAnimationData()
                        {
                            subAnimationName = "attack_right",
                            wrapMode = tk2dSpriteAnimationClip.WrapMode.Once,
                            fps = 12,
                            pathDirectory = "NevernamedsItems/Resources/Companions/FunGuy/funguy_attackright",
                        },
                        new AnimationUtilityExtensions.DirectionalAnimationData()
                        {
                            subAnimationName = "attack_left",
                            wrapMode = tk2dSpriteAnimationClip.WrapMode.Once,
                            fps = 12,
                            pathDirectory = "NevernamedsItems/Resources/Companions/FunGuy/funguy_attackleft",
                        }
                    }
                    );

                BehaviorSpeculator component = FunGuy.prefab.GetComponent<BehaviorSpeculator>();
                CustomCompanionBehaviours.SimpleCompanionApproach approach = new CustomCompanionBehaviours.SimpleCompanionApproach();
                approach.DesiredDistance = 4f;
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
            if (!(FunGuy.synergyPrefab != null || CompanionBuilder.companionDictionary.ContainsKey(FunGuy.synergyguid)))
            {
                FunGuy.synergyPrefab = CompanionBuilder.BuildPrefab("Fun Guy Synergy", FunGuy.synergyguid, "NevernamedsItems/Resources/Companions/FunGuy/funguysyn_idleright_001", new IntVector2(8, 2), new IntVector2(9, 11));
                var companionController = FunGuy.synergyPrefab.AddComponent<FunGuyController>();
                companionController.aiActor.MovementSpeed = 5f;
                companionController.specRigidbody.CollideWithTileMap = false;
                AIAnimator animator = FunGuy.synergyPrefab.GetOrAddComponent<AIAnimator>();

                animator.AdvAddAnimation("idle",
                    DirectionalAnimation.DirectionType.TwoWayHorizontal,
                    CompanionBuilder.AnimationType.Idle,
                    new List<AnimationUtilityExtensions.DirectionalAnimationData>()
                    {
                        new AnimationUtilityExtensions.DirectionalAnimationData()
                        {
                            subAnimationName = "idle_right",
                            wrapMode = tk2dSpriteAnimationClip.WrapMode.Loop,
                            fps = 9,
                            pathDirectory = "NevernamedsItems/Resources/Companions/FunGuy/funguysyn_idleright",
                        },
                        new AnimationUtilityExtensions.DirectionalAnimationData()
                        {
                            subAnimationName = "idle_left",
                            wrapMode = tk2dSpriteAnimationClip.WrapMode.Loop,
                            fps = 9,
                            pathDirectory = "NevernamedsItems/Resources/Companions/FunGuy/funguysyn_idleleft",
                        }
                    }
                    );
                animator.AdvAddAnimation("move",
                    DirectionalAnimation.DirectionType.TwoWayHorizontal,
                    CompanionBuilder.AnimationType.Move,
                    new List<AnimationUtilityExtensions.DirectionalAnimationData>()
                    {
                        new AnimationUtilityExtensions.DirectionalAnimationData()
                        {
                            subAnimationName = "move_right",
                            wrapMode = tk2dSpriteAnimationClip.WrapMode.Loop,
                            fps = 9,
                            pathDirectory = "NevernamedsItems/Resources/Companions/FunGuy/funguysyn_moveright",
                        },
                        new AnimationUtilityExtensions.DirectionalAnimationData()
                        {
                            subAnimationName = "move_left",
                            wrapMode = tk2dSpriteAnimationClip.WrapMode.Loop,
                            fps = 9,
                            pathDirectory = "NevernamedsItems/Resources/Companions/FunGuy/funguysyn_moveleft",
                        }
                    }
                    );
                animator.AdvAddAnimation("attack",
                    DirectionalAnimation.DirectionType.TwoWayHorizontal,
                    CompanionBuilder.AnimationType.Other,
                    new List<AnimationUtilityExtensions.DirectionalAnimationData>()
                    {
                        new AnimationUtilityExtensions.DirectionalAnimationData()
                        {
                            subAnimationName = "attack_right",
                            wrapMode = tk2dSpriteAnimationClip.WrapMode.Once,
                            fps = 12,
                            pathDirectory = "NevernamedsItems/Resources/Companions/FunGuy/funguysyn_attackright",
                        },
                        new AnimationUtilityExtensions.DirectionalAnimationData()
                        {
                            subAnimationName = "attack_left",
                            wrapMode = tk2dSpriteAnimationClip.WrapMode.Once,
                            fps = 12,
                            pathDirectory = "NevernamedsItems/Resources/Companions/FunGuy/funguysyn_attackleft",
                        }
                    }
                    );

                BehaviorSpeculator component = FunGuy.synergyPrefab.GetComponent<BehaviorSpeculator>();
                CustomCompanionBehaviours.SimpleCompanionApproach approach = new CustomCompanionBehaviours.SimpleCompanionApproach();
                approach.DesiredDistance = 4f;
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
        public class FunGuyController : CompanionController
        {
            private void Start()
            {
                this.Owner = this.m_owner;
                attackTimer = 4f;
                isAttacking = false;
            }
            public override void Update()
            {
                if (Owner && !Dungeon.IsGenerating && Owner.IsInCombat && base.transform.position.GetAbsoluteRoom() == Owner.CurrentRoom)
                {
                    if (attackTimer > 0)
                    {
                        attackTimer -= BraveTime.DeltaTime;
                    }
                    else
                    {
                        if (base.aiActor.OverrideTarget != null && !isAttacking)
                        {
                            Vector2 closestPointOnTarget = BraveMathCollege.ClosestPointOnRectangle(base.sprite.WorldCenter, base.aiActor.OverrideTarget.HitboxPixelCollider.UnitBottomLeft, base.aiActor.OverrideTarget.HitboxPixelCollider.UnitDimensions);
                            if (Vector2.Distance(base.sprite.WorldCenter, closestPointOnTarget) < 5)
                            {
                                StartCoroutine(Attack());
                            }
                        }
                    }
                }
            }
            private IEnumerator Attack()
            {
                isAttacking = true;
                base.aiActor.MovementSpeed = 0;
                    AkSoundEngine.PostEvent("Play_ENM_mushroom_cloud_01", base.gameObject);
                base.aiAnimator.PlayUntilFinished("attack");
                yield return new WaitForSeconds(0.25f);

                for (int i = 0; i < 15; i++)
                {
                    GameObject toFire = (PickupObjectDatabase.GetById(FungoCannon.FungoCannonID) as Gun).DefaultModule.chargeProjectiles[0].Projectile.gameObject;
                    if (Owner.PlayerHasActiveSynergy("Pointiest One")) toFire = FunGuy.synergyFungusProj.gameObject;
                    GameObject obj = UnityEngine.Object.Instantiate<GameObject>(toFire, base.sprite.WorldCenter, Quaternion.Euler(new Vector3(0f, 0f, UnityEngine.Random.Range(0, 360))));
                    Projectile proj = obj.GetComponent<Projectile>();
                    if (proj)
                    {
                        proj.Owner = Owner;
                        proj.Shooter = Owner.specRigidbody;

                        proj.baseData.damage *= Owner.stats.GetStatValue(PlayerStats.StatType.Damage);
                        proj.baseData.speed *= Owner.stats.GetStatValue(PlayerStats.StatType.ProjectileSpeed);
                        proj.baseData.range *= Owner.stats.GetStatValue(PlayerStats.StatType.RangeMultiplier);
                        proj.baseData.force *= Owner.stats.GetStatValue(PlayerStats.StatType.KnockbackMultiplier);
                        proj.BossDamageMultiplier *= Owner.stats.GetStatValue(PlayerStats.StatType.DamageToBosses);
                        proj.UpdateSpeed();
                        proj.ApplyCompanionModifierToBullet(Owner);
                        Owner.DoPostProcessProjectile(proj);
                    }
                }
             if (Owner.PlayerHasActiveSynergy("Mush Ado About Nothing"))   Owner.DoEasyBlank(base.sprite.WorldCenter, EasyBlankType.MINI);

                    base.aiActor.MovementSpeed = base.aiActor.BaseMovementSpeed;
                attackTimer = 4f;
                isAttacking = false;
                yield break;
            }
            private float attackTimer;
            private bool isAttacking;
            public PlayerController Owner;
        }
        public static GameObject prefab;
        public static GameObject synergyPrefab;
        private static readonly string guid = "funguy32o6697439ysa7if7d6syf6e6thasiuft64";
        private static readonly string synergyguid = "funguysynergy327498ryye78ighidsgdstdistkd7t";
    }
}