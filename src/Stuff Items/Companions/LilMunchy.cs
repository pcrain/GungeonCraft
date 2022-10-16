using System;
using System.Collections;
using System.Collections.Generic;
using Dungeonator;
using Gungeon;
using Alexandria.ItemAPI;
using SaveAPI;
using UnityEngine;

namespace NevernamedsItems
{
    public class LilMunchy : PassiveItem
    {
        public static void Init()
        {
            string name = "Lil Munchy";
            string resourcePath = "NevernamedsItems/Resources/Companions/LilMunchy/lilmunchy_icon";
            GameObject gameObject = new GameObject();
            CompanionItem companionItem = gameObject.AddComponent<CompanionItem>();
            ItemBuilder.AddSpriteToObject(name, resourcePath, gameObject);
            string shortDesc = "Hungry Hungry";
            string longDesc = "A juvenile muncher that hasn't yet found a suitable generic dungeon room in which to take root."+"\n\nHe has an endless appetite!";
            companionItem.SetupItem(shortDesc, longDesc, "nn");
            companionItem.quality = PickupObject.ItemQuality.C;
            companionItem.CompanionGuid = LilMunchy.guid;
            LilMunchy.BuildPrefab();
        }

        public static void BuildPrefab()
        {
            if (!(LilMunchy.prefab != null || CompanionBuilder.companionDictionary.ContainsKey(LilMunchy.guid)))
            {
                LilMunchy.prefab = CompanionBuilder.BuildPrefab("Lil Munchy", LilMunchy.guid, "NevernamedsItems/Resources/Companions/LilMunchy/munchy_idle_front_001", new IntVector2(7, 2), new IntVector2(6, 6));
                var companionController = LilMunchy.prefab.AddComponent<LilMunchyController>();
                companionController.aiActor.MovementSpeed = 4f;

                AIAnimator animator = LilMunchy.prefab.GetOrAddComponent<AIAnimator>();

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
                            pathDirectory = "NevernamedsItems/Resources/Companions/LilMunchy/munchy_idle_back",
                        },
                        new AnimationUtilityExtensions.DirectionalAnimationData()
                        {
                            subAnimationName = "idle_east",
                            wrapMode = tk2dSpriteAnimationClip.WrapMode.Loop,
                            fps = 9,
                            pathDirectory = "NevernamedsItems/Resources/Companions/LilMunchy/munchy_idle_right",
                        },
                        new AnimationUtilityExtensions.DirectionalAnimationData()
                        {
                            subAnimationName = "idle_south",
                            wrapMode = tk2dSpriteAnimationClip.WrapMode.Loop,
                            fps = 9,
                            pathDirectory = "NevernamedsItems/Resources/Companions/LilMunchy/munchy_idle_front",
                        },
                        new AnimationUtilityExtensions.DirectionalAnimationData()
                        {
                            subAnimationName = "idle_west",
                            wrapMode = tk2dSpriteAnimationClip.WrapMode.Loop,
                            fps = 9,
                            pathDirectory = "NevernamedsItems/Resources/Companions/LilMunchy/munchy_idle_left",
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
                            pathDirectory = "NevernamedsItems/Resources/Companions/LilMunchy/munchy_move_back",
                        },
                        new AnimationUtilityExtensions.DirectionalAnimationData()
                        {
                            subAnimationName = "move_east",
                            wrapMode = tk2dSpriteAnimationClip.WrapMode.Loop,
                            fps = 9,
                            pathDirectory = "NevernamedsItems/Resources/Companions/LilMunchy/munchy_move_right",
                        },
                        new AnimationUtilityExtensions.DirectionalAnimationData()
                        {
                            subAnimationName = "move_south",
                            wrapMode = tk2dSpriteAnimationClip.WrapMode.Loop,
                            fps = 9,
                            pathDirectory = "NevernamedsItems/Resources/Companions/LilMunchy/munchy_move_front",
                        },
                        new AnimationUtilityExtensions.DirectionalAnimationData()
                        {
                            subAnimationName = "move_west",
                            wrapMode = tk2dSpriteAnimationClip.WrapMode.Loop,
                            fps = 9,
                            pathDirectory = "NevernamedsItems/Resources/Companions/LilMunchy/munchy_move_left",
                        },
                    }
                    );
                animator.AdvAddAnimation("eat",
                    DirectionalAnimation.DirectionType.Single,
                    CompanionBuilder.AnimationType.Other,
                    new List<AnimationUtilityExtensions.DirectionalAnimationData>()
                    {
                        new AnimationUtilityExtensions.DirectionalAnimationData()
                        {
                            subAnimationName = "eat",
                            wrapMode = tk2dSpriteAnimationClip.WrapMode.Once,
                            fps = 9,
                            pathDirectory = "NevernamedsItems/Resources/Companions/LilMunchy/munchy_eat",
                        },
                    }
                    );
                animator.AdvAddAnimation("puke",
                    DirectionalAnimation.DirectionType.Single,
                    CompanionBuilder.AnimationType.Other,
                    new List<AnimationUtilityExtensions.DirectionalAnimationData>()
                    {
                        new AnimationUtilityExtensions.DirectionalAnimationData()
                        {
                            subAnimationName = "puke",
                            wrapMode = tk2dSpriteAnimationClip.WrapMode.Once,
                            fps = 9,
                            pathDirectory = "NevernamedsItems/Resources/Companions/LilMunchy/munchy_puke",
                        },
                    }
                    );
                animator.AdvAddAnimation("disgust",
                    DirectionalAnimation.DirectionType.Single,
                    CompanionBuilder.AnimationType.Other,
                    new List<AnimationUtilityExtensions.DirectionalAnimationData>()
                    {
                        new AnimationUtilityExtensions.DirectionalAnimationData()
                        {
                            subAnimationName = "disgust",
                            wrapMode = tk2dSpriteAnimationClip.WrapMode.Once,
                            fps = 9,
                            pathDirectory = "NevernamedsItems/Resources/Companions/LilMunchy/munchy_disgust",
                        },
                    }
                    );



                BehaviorSpeculator component = LilMunchy.prefab.GetComponent<BehaviorSpeculator>();

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
        public class LilMunchyController : CompanionController, IPlayerInteractable
        {
            private void Start()
            {
                this.Owner = this.m_owner;
                this.curRoom = GameManager.Instance.Dungeon.data.GetAbsoluteRoomFromPosition(base.transform.position.IntXY(VectorConversions.Round));
                this.curRoom.RegisterInteractable(this);
            }
            private RoomHandler curRoom;
            public override void Update()
            {
                if (base.aiActor && base.aiActor.CompanionOwner)
                {
                    if (curRoom != base.aiActor.CompanionOwner.CurrentRoom)
                    {
                        if (curRoom != null)
                        {
                            ReAssign(curRoom, base.aiActor.CompanionOwner.CurrentRoom);
                        }
                        else
                        {
                            base.aiActor.CompanionOwner.CurrentRoom.RegisterInteractable(this);
                        }
                        curRoom = base.aiActor.CompanionOwner.CurrentRoom;
                    }
                }
            }
            private void ReAssign(RoomHandler oldRoom, RoomHandler newRoom)
            {
                oldRoom.DeregisterInteractable(this);
                newRoom.RegisterInteractable(this);
            }
            public float GetDistanceToPoint(Vector2 point)
            {
                //A method to get the distance from the interactable from any point
                if (!base.sprite) return float.MaxValue;
                Bounds bounds = base.sprite.GetBounds();
                bounds.SetMinMax(bounds.min + base.transform.position, bounds.max + base.transform.position);
                float num = Mathf.Max(Mathf.Min(point.x, bounds.max.x), bounds.min.x);
                float num2 = Mathf.Max(Mathf.Min(point.y, bounds.max.y), bounds.min.y);
                return Mathf.Sqrt((point.x - num) * (point.x - num) + (point.y - num2) * (point.y - num2));
            }
            public void OnEnteredRange(PlayerController interactor)
            {
                //A method that runs whenever the player enters the interaction range of the interactable. This is what outlines it in white to show that it can be interacted with
                SpriteOutlineManager.RemoveOutlineFromSprite(base.sprite, true);
                SpriteOutlineManager.AddOutlineToSprite(base.sprite, Color.white);
            }
            public void OnExitRange(PlayerController interactor)
            {
                //A method that runs whenever the player exits the interaction range of the interactable. This is what removed the white outline to show that it cannot be currently interacted with
                SpriteOutlineManager.RemoveOutlineFromSprite(base.sprite, true);
                SpriteOutlineManager.AddOutlineToSprite(base.sprite, Color.black);
            }
            public void Interact(PlayerController interactor)
            {
                if (!isDoingSomething && !interactor.IsInCombat)
                {
                    if (interactor.CurrentGun != null)
                    {
                        if (interactor.CurrentGun.CanBeDropped && !interactor.CurrentGun.InfiniteAmmo)
                        {
                            StartCoroutine(EatGun(interactor));
                        }
                        else
                        {
                            StartCoroutine(DoDisgust());
                        }
                    }
                    else
                    {
                        StartCoroutine(DoDisgust());
                    }
                }
            }
            private IEnumerator EatGun(PlayerController interactor)
            {
                isDoingSomething = true;
                base.aiActor.MovementSpeed = 0;
                base.aiAnimator.PlayUntilFinished("eat");
                ItemQuality cachedQual = interactor.CurrentGun.quality;
                interactor.inventory.RemoveGunFromInventory(interactor.CurrentGun);

                Vector2 vector = (!base.aiActor.sprite) ? Vector2.up : (Vector2.up * (base.aiActor.sprite.WorldTopCenter.y - base.aiActor.sprite.WorldBottomCenter.y));
                GameObject heart = base.aiActor.PlayEffectOnActor(StaticStatusEffects.charmingRoundsEffect.OverheadVFX, vector, true, false, false);

                yield return new WaitForSeconds(0.5f);
                if (heart) UnityEngine.Object.Destroy(heart);

                if (lastEatenGunQuality != ItemQuality.COMMON)
                {

                    base.aiAnimator.PlayUntilFinished("puke");
                    yield return new WaitForSeconds(1.22f);
                    DetermineAndSpawnMunchedGun(interactor, cachedQual);
                }
                else
                {
                    lastEatenGunQuality = cachedQual;
                    base.aiActor.MovementSpeed = base.aiActor.BaseMovementSpeed;
                    isDoingSomething = false;

                }
                yield break;
            }
            PickupObject.ItemQuality lastEatenGunQuality = ItemQuality.COMMON;
            private void DetermineAndSpawnMunchedGun(PlayerController player, ItemQuality recentQuality)
            {
                int randomQual = UnityEngine.Random.Range((int)recentQuality, (int)lastEatenGunQuality + 1);
                GameObject item = GameManager.Instance.RewardManager.GetItemForPlayer(player, GameManager.Instance.RewardManager.GunsLootTable,
                    (ItemQuality)randomQual, null, false, null, false, null, false, RewardManager.RewardSource.UNSPECIFIED);
                LootEngine.SpawnItem(item, base.sprite.WorldCenter, Vector2.zero, 0);
                lastEatenGunQuality = ItemQuality.COMMON;
                base.aiActor.MovementSpeed = base.aiActor.BaseMovementSpeed;
                isDoingSomething = false;
            }
            private IEnumerator DoDisgust()
            {
                isDoingSomething = true;
                base.aiAnimator.PlayUntilFinished("disgust");
                base.aiActor.MovementSpeed = 0;
                Vector2 vector = (!base.aiActor.sprite) ? Vector2.up : (Vector2.up * (base.aiActor.sprite.WorldTopCenter.y - base.aiActor.sprite.WorldBottomCenter.y));
                GameObject crossX = base.aiActor.PlayEffectOnActor(EasyVFXDatabase.LastBulletStandingX, vector, true, false, false);
                yield return new WaitForSeconds(0.5556f);
                AkSoundEngine.PostEvent("Play_ENM_lizard_bubble_01", base.gameObject);
                base.aiActor.MovementSpeed = base.aiActor.BaseMovementSpeed;
                UnityEngine.Object.Destroy(crossX);
                isDoingSomething = false;
                yield break;
            }
            private bool isDoingSomething = false;
            public string GetAnimationState(PlayerController interactor, out bool shouldBeFlipped)
            {
                //Some boilerplate code for determining if the interactable should be flipped
                shouldBeFlipped = false;
                return string.Empty;
            }
            public float GetOverrideMaxDistance()
            {
                return 1.5f;
            }
            public PlayerController Owner;
        }
        public static GameObject prefab;
        private static readonly string guid = "lilmunchy83y8ye7w86655746847635";
    }
}