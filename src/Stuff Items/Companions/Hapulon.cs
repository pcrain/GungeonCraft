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
using System.Linq;

namespace NevernamedsItems
{
    public class Hapulon : PassiveItem
    {
        public static void Init()
        {
            string name = "Hapulon";
            string resourcePath = "NevernamedsItems/Resources/Companions/Hapulon/hapulon_icon";
            GameObject gameObject = new GameObject();
            CompanionItem companionItem = gameObject.AddComponent<CompanionItem>();
            ItemBuilder.AddSpriteToObject(name, resourcePath, gameObject);
            string shortDesc = "Bouncing Bundle of Joy";
            string longDesc = "The result of a catastrauphic malfunction in Blobulonian genetic engineering, the Hapulon knows nothing but love and joy. For this reason, they have been almost hunted to extinction.";
            companionItem.SetupItem(shortDesc, longDesc, "nn");
            companionItem.quality = PickupObject.ItemQuality.A;
            companionItem.CompanionGuid = Hapulon.guid;
            Hapulon.BuildPrefab();

            List<string> vfxPaths = new List<string>()
            {
                "NevernamedsItems/Resources/MiscVFX/CompanionVFX/loveburstaoe_vfx_001",
                "NevernamedsItems/Resources/MiscVFX/CompanionVFX/loveburstaoe_vfx_002",
                "NevernamedsItems/Resources/MiscVFX/CompanionVFX/loveburstaoe_vfx_003",
                "NevernamedsItems/Resources/MiscVFX/CompanionVFX/loveburstaoe_vfx_004",
                "NevernamedsItems/Resources/MiscVFX/CompanionVFX/loveburstaoe_vfx_005",
            };
            EasyVFXDatabase.LoveBurstAOE = VFXToolbox.CreateVFX("GundertaleSpare", vfxPaths, 16, new IntVector2(33, 16), tk2dBaseSprite.Anchor.MiddleCenter, true, 0.18f, 100, Color.yellow); ;
        }

        public static void BuildPrefab()
        {
            if (!(Hapulon.prefab != null || CompanionBuilder.companionDictionary.ContainsKey(Hapulon.guid)))
            {
                Hapulon.prefab = CompanionBuilder.BuildPrefab("Hapulon", Hapulon.guid, "NevernamedsItems/Resources/Companions/Hapulon/hapulon_idleleft_001", new IntVector2(1, 1), new IntVector2(13, 12));
                var companionController = Hapulon.prefab.AddComponent<HapulonController>();
                companionController.aiActor.MovementSpeed = 4f;

                AIAnimator animator = Hapulon.prefab.GetOrAddComponent<AIAnimator>();

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
                            pathDirectory = "NevernamedsItems/Resources/Companions/Hapulon/hapulon_idlebackright",
                        },
                        new AnimationUtilityExtensions.DirectionalAnimationData()
                        {
                            subAnimationName = "idle_front_right",
                            wrapMode = tk2dSpriteAnimationClip.WrapMode.Loop,
                            fps = 9,
                            pathDirectory = "NevernamedsItems/Resources/Companions/Hapulon/hapulon_idleright",
                        },
                        new AnimationUtilityExtensions.DirectionalAnimationData()
                        {
                            subAnimationName = "idle_front_left",
                            wrapMode = tk2dSpriteAnimationClip.WrapMode.Loop,
                            fps = 9,
                            pathDirectory = "NevernamedsItems/Resources/Companions/Hapulon/hapulon_idleleft",
                        },
                        new AnimationUtilityExtensions.DirectionalAnimationData()
                        {
                            subAnimationName = "idle_back_left",
                            wrapMode = tk2dSpriteAnimationClip.WrapMode.Loop,
                            fps = 9,
                            pathDirectory = "NevernamedsItems/Resources/Companions/Hapulon/hapulon_idlebackright",
                        },
                    }
                    );

                animator.AdvAddAnimation("hop",
                    DirectionalAnimation.DirectionType.FourWay,
                    CompanionBuilder.AnimationType.Other,
                    new List<AnimationUtilityExtensions.DirectionalAnimationData>()
                    {
                        new AnimationUtilityExtensions.DirectionalAnimationData()
                        {
                            subAnimationName = "hop_back_right",
                            wrapMode = tk2dSpriteAnimationClip.WrapMode.Once,
                            fps = 12,
                            pathDirectory = "NevernamedsItems/Resources/Companions/Hapulon/hapulon_hopback",
                        },
                        new AnimationUtilityExtensions.DirectionalAnimationData()
                        {
                            subAnimationName = "hop_front_right",
                            wrapMode = tk2dSpriteAnimationClip.WrapMode.Once,
                            fps = 12,
                            pathDirectory = "NevernamedsItems/Resources/Companions/Hapulon/hapulon_hopright",
                        },
                        new AnimationUtilityExtensions.DirectionalAnimationData()
                        {
                            subAnimationName = "hop_front_left",
                            wrapMode = tk2dSpriteAnimationClip.WrapMode.Once,
                            fps = 12,
                            pathDirectory = "NevernamedsItems/Resources/Companions/Hapulon/hapulon_hopleft",
                        },
                        new AnimationUtilityExtensions.DirectionalAnimationData()
                        {
                            subAnimationName = "hop_back_left",
                            wrapMode = tk2dSpriteAnimationClip.WrapMode.Once,
                            fps = 12,
                            pathDirectory = "NevernamedsItems/Resources/Companions/Hapulon/hapulon_hopback",
                        },
                    }
                    );


                BehaviorSpeculator component = Hapulon.prefab.GetComponent<BehaviorSpeculator>();
                EnemyApproachingHopper hopper = new EnemyApproachingHopper();
                hopper.hopAnim = "hop";
                component.MovementBehaviors.Add(hopper);
            }
        }
        public class HapulonController : CompanionController
        {
            private void Start()
            {
                this.Owner = this.m_owner;
                hopBehaviour = base.aiActor.behaviorSpeculator.MovementBehaviors.OfType<EnemyApproachingHopper>().FirstOrDefault();
                hopBehaviour.onLanded += OnLanded;
                hopBehaviour.isValid = EnemyIsValid;
            }
            public bool EnemyIsValid(AIActor enemy)
            {
                if (enemy.CanTargetEnemies == true && enemy.CanTargetPlayers == false) return false;
                else return true;
            }
            private void OnLanded(AIActor self, Vector2 position)
            {
                if (Owner.IsInCombat) UnityEngine.Object.Instantiate<GameObject>(EasyVFXDatabase.LoveBurstAOE, base.sprite.WorldCenter + new Vector2(0, -0.5f), Quaternion.identity);
                List<AIActor> activeEnemies = base.specRigidbody.UnitCenter.GetAbsoluteRoom().GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
                if (activeEnemies != null)
                {
                    for (int i = 0; i < activeEnemies.Count; i++)
                    {
                        AIActor aiactor = activeEnemies[i];
                        if (aiactor.IsNormalEnemy)
                        {
                            if (Vector2.Distance(base.aiActor.CenterPosition, aiactor.CenterPosition) <= 2)
                            {
                                aiactor.gameActor.ApplyEffect(StaticStatusEffects.charmingRoundsEffect, 1f, null);
                            }
                        }
                    }
                }
                DeadlyDeadlyGoopManager.GetGoopManagerForGoopType(EasyGoopDefinitions.CharmGoopDef).TimedAddGoopCircle(base.specRigidbody.UnitBottomCenter, 1, 0.5f, false);

            }
            private PlayerController Owner;
            private EnemyApproachingHopper hopBehaviour;
        }
        public static GameObject prefab;
        private static readonly string guid = "omitb_hapulon_companion";
    }
}