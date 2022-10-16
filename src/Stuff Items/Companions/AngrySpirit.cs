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
    public class AngrySpirit : PassiveItem
    {
        public static void Init()
        {
            string name = "Angry Spirit";
            string resourcePath = "NevernamedsItems/Resources/Companions/AngrySpirit/angryspirit_icon";
            GameObject gameObject = new GameObject();
            CompanionItem companionItem = gameObject.AddComponent<CompanionItem>();
            ItemBuilder.AddSpriteToObject(name, resourcePath, gameObject);
            string shortDesc = "Furious Friend";
            string longDesc = "Aches to kill, but cannot generate it's own bullets, merely copying the firepower of it's owner."+"\n\nDon't let him in.";
            companionItem.SetupItem(shortDesc, longDesc, "nn");
            companionItem.quality = PickupObject.ItemQuality.B;
            companionItem.CompanionGuid = AngrySpirit.guid;
            AngrySpirit.BuildPrefab();

            companionItem.AddToSubShop(ItemBuilder.ShopType.Cursula);

        }

        public static void BuildPrefab()
        {
            if (!(AngrySpirit.prefab != null || CompanionBuilder.companionDictionary.ContainsKey(AngrySpirit.guid)))
            {
                AngrySpirit.prefab = CompanionBuilder.BuildPrefab("Angry Spirit", AngrySpirit.guid, "NevernamedsItems/Resources/Companions/AngrySpirit/angryspirit_idleright_001", new IntVector2(2, 2), new IntVector2(12, 14));
                var companionController = AngrySpirit.prefab.AddComponent<AngrySpiritController>();
                companionController.aiActor.MovementSpeed = 6f;
                companionController.CanCrossPits = true;

                companionController.aiActor.SetIsFlying(true, "Flying Entity", false, true);
                companionController.aiActor.ActorShadowOffset = new Vector3(0, -0.25f);

                AIAnimator animator = AngrySpirit.prefab.GetOrAddComponent<AIAnimator>();

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
                            pathDirectory = "NevernamedsItems/Resources/Companions/AngrySpirit/angryspirit_idleright",
                        },
                        new AnimationUtilityExtensions.DirectionalAnimationData()
                        {
                            subAnimationName = "idle_left",
                            wrapMode = tk2dSpriteAnimationClip.WrapMode.Loop,
                            fps = 9,
                            pathDirectory = "NevernamedsItems/Resources/Companions/AngrySpirit/angryspirit_idleleft",
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
                            pathDirectory = "NevernamedsItems/Resources/Companions/AngrySpirit/angryspirit_attackright",
                        },
                        new AnimationUtilityExtensions.DirectionalAnimationData()
                        {
                            subAnimationName = "attack_left",
                            wrapMode = tk2dSpriteAnimationClip.WrapMode.Once,
                            fps = 12,
                            pathDirectory = "NevernamedsItems/Resources/Companions/AngrySpirit/angryspirit_attackleft",
                        }
                    }
                    );

                BehaviorSpeculator component = AngrySpirit.prefab.GetComponent<BehaviorSpeculator>();
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

                });
            }
        }
        public class AngrySpiritController : CompanionController
        {
            private void Start()
            {
                this.Owner = this.m_owner;
                Owner.PostProcessProjectile += OwnerPostProcess;
            }
            public override void OnDestroy()
            {
                if (Owner) Owner.PostProcessProjectile -= OwnerPostProcess;
                base.OnDestroy();
            }
            private void OwnerPostProcess(Projectile proj, float something)
            {
                if (UnityEngine.Random.value <= 0.2f)
                {
                    if (Owner.IsInCombat && base.aiActor.OverrideTarget && base.aiActor.OverrideTarget.sprite)
                    {
                        base.aiAnimator.PlayUntilFinished("attack");
                        GameObject spawned = ProjSpawnHelper.SpawnProjectileTowardsPoint(proj.gameObject, base.sprite.WorldCenter, base.aiActor.OverrideTarget.sprite.WorldCenter, 0, 5, Owner);
                        Projectile spawnedProj = spawned.GetComponent<Projectile>();
                        if (spawnedProj)
                        {
                            spawnedProj.Owner = Owner;
                            spawnedProj.Shooter = Owner.specRigidbody;
                            spawnedProj.ApplyCompanionModifierToBullet(Owner);

                        }
                    }
                }
            }
            public override void Update()
            {
                if (Owner && !Dungeon.IsGenerating && Owner.IsInCombat && base.transform.position.GetAbsoluteRoom() == Owner.CurrentRoom)
                {

                }
            }
            public PlayerController Owner;
        }
        public static GameObject prefab;
        private static readonly string guid = "angryspirit8373429576528727654857yt7wey34gfh4w6iw4t";
    }
}