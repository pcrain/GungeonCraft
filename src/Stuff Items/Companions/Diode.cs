using System;
using System.Collections;
using System.Collections.Generic;
using Dungeonator;
using Gungeon;
using Alexandria.ItemAPI;
using UnityEngine;

namespace NevernamedsItems
{
    public class Diode : PassiveItem
    {
        public static void Init()
        {
            string name = "Diode";
            string resourcePath = "NevernamedsItems/Resources/Companions/Diode/diode_icon";
            GameObject gameObject = new GameObject();
            var companionItem = gameObject.AddComponent<CompanionItem>();
            ItemBuilder.AddSpriteToObject(name, resourcePath, gameObject);
            string shortDesc = "Electric Buddy";
            string longDesc = "This little guy connects to you via an electric arc." + "\n\nOne of the first creations of a young tinker, it wandered out of her workshop in search of adventure, and somehow wound up down here.";
            companionItem.SetupItem(shortDesc, longDesc, "nn");
            companionItem.quality = PickupObject.ItemQuality.C;
            companionItem.CompanionGuid = Diode.guid;

            Diode.BuildPrefab();
        }
        public static void BuildPrefab()
        {
            bool flag = Diode.prefab != null || CompanionBuilder.companionDictionary.ContainsKey(Diode.guid);
            if (!flag)
            {
                Diode.prefab = CompanionBuilder.BuildPrefab("Diode Companion", Diode.guid, "NevernamedsItems/Resources/Companions/Diode/diode_idle_001", new IntVector2(4, 0), new IntVector2(8, 8));
                var companionController = Diode.prefab.AddComponent<DiodeCompanionBehaviour>();

                AIAnimator animator = prefab.GetOrAddComponent<AIAnimator>();

                animator.AdvAddAnimation("idle",
                    DirectionalAnimation.DirectionType.TwoWayHorizontal,
                    CompanionBuilder.AnimationType.Idle,
                    new List<AnimationUtilityExtensions.DirectionalAnimationData>()
                    {
                        new AnimationUtilityExtensions.DirectionalAnimationData()
                        {
                            subAnimationName = "idle_right",
                            wrapMode = tk2dSpriteAnimationClip.WrapMode.Loop,
                            fps = 7,
                            pathDirectory = "NevernamedsItems/Resources/Companions/Diode/diode_idle",
                        },
                        new AnimationUtilityExtensions.DirectionalAnimationData()
                        {
                            subAnimationName = "idle_left",
                            wrapMode = tk2dSpriteAnimationClip.WrapMode.Loop,
                            fps = 7,
                            pathDirectory = "NevernamedsItems/Resources/Companions/Diode/diode_idle",
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
                            fps = 12,
                            pathDirectory = "NevernamedsItems/Resources/Companions/Diode/diode_moveright",
                        },
                        new AnimationUtilityExtensions.DirectionalAnimationData()
                        {
                            subAnimationName = "move_left",
                            wrapMode = tk2dSpriteAnimationClip.WrapMode.Loop,
                            fps = 12,
                            pathDirectory = "NevernamedsItems/Resources/Companions/Diode/diode_moveleft",
                        }
                    }
                    );


                companionController.CanInterceptBullets = false;
                companionController.companionID = CompanionController.CompanionIdentifier.NONE;
                companionController.aiActor.MovementSpeed = 7f;
                companionController.aiActor.healthHaver.PreventAllDamage = true;
                companionController.aiActor.CollisionDamage = 0f;
                companionController.aiActor.specRigidbody.CollideWithOthers = false;
                companionController.aiActor.specRigidbody.CollideWithTileMap = false;
                BehaviorSpeculator component = Diode.prefab.GetComponent<BehaviorSpeculator>();
                CustomCompanionBehaviours.SimpleCompanionApproach approach = new CustomCompanionBehaviours.SimpleCompanionApproach();
                approach.DesiredDistance = 2;
                component.MovementBehaviors.Add(approach);
                component.MovementBehaviors.Add(new CompanionFollowPlayerBehavior
                {
                    IdleAnimations = new string[]
                    {
                        "idle"
                    }
                });
            }
        }

        public class DiodeCompanionBehaviour : CompanionController
        {
            public DiodeCompanionBehaviour()
            {
                this.DamagePerHit = 3.5f;
            }
            public override void OnDestroy()
            {
                if (extantLink != null)
                {
                    SpawnManager.Despawn(extantLink.gameObject);
                    extantLink = null;
                }
                base.OnDestroy();
            }
            private void Start()
            {
                this.Owner = this.m_owner;
                this.LinkVFXPrefab = FakePrefab.Clone(Game.Items["shock_rounds"].GetComponent<ComplexProjectileModifier>().ChainLightningVFX);
            }
            private void FixedUpdate()
            {

            }
            public override void Update()
            {
                if (this.LinkVFXPrefab == null)
                {
                    this.LinkVFXPrefab = FakePrefab.Clone(Game.Items["shock_rounds"].GetComponent<ComplexProjectileModifier>().ChainLightningVFX);
                }
                if (this.Owner && this.Owner.IsInCombat && this.extantLink == null)
                {
                    tk2dTiledSprite component = SpawnManager.SpawnVFX(this.LinkVFXPrefab, false).GetComponent<tk2dTiledSprite>();
                    this.extantLink = component;
                }
                else if (this.Owner && this.Owner.IsInCombat && this.extantLink != null)
                {
                    UpdateLink(this.Owner, this.extantLink);
                }
                else if (extantLink != null)
                {
                    SpawnManager.Despawn(extantLink.gameObject);
                    extantLink = null;
                }
                base.Update();
            }
            private void UpdateLink(PlayerController target, tk2dTiledSprite m_extantLink)
            {
                Vector2 unitCenter = base.specRigidbody.UnitCenter;
                Vector2 unitCenter2 = target.specRigidbody.HitboxPixelCollider.UnitCenter;
                m_extantLink.transform.position = unitCenter;
                Vector2 vector = unitCenter2 - unitCenter;
                float num = BraveMathCollege.Atan2Degrees(vector.normalized);
                int num2 = Mathf.RoundToInt(vector.magnitude / 0.0625f);
                m_extantLink.dimensions = new Vector2((float)num2, m_extantLink.dimensions.y);
                m_extantLink.transform.rotation = Quaternion.Euler(0f, 0f, num);
                m_extantLink.UpdateZDepth();
                this.ApplyLinearDamage(unitCenter, unitCenter2);
            }
            private void ApplyLinearDamage(Vector2 p1, Vector2 p2)
            {
                float num = this.DamagePerHit;
                if (PassiveItem.IsFlagSetForCharacter(this.Owner, typeof(BattleStandardItem))) num *= BattleStandardItem.BattleStandardCompanionDamageMultiplier;
                if (this.Owner.CurrentGun && this.Owner.CurrentGun.LuteCompanionBuffActive) num *= 2;
                for (int i = 0; i < StaticReferenceManager.AllEnemies.Count; i++)
                {
                    AIActor aiactor = StaticReferenceManager.AllEnemies[i];
                    if (!this.m_damagedEnemies.Contains(aiactor))
                    {
                        if (aiactor && aiactor.HasBeenEngaged && aiactor.IsNormalEnemy && aiactor.specRigidbody)
                        {
                            Vector2 zero = Vector2.zero;
                            if (BraveUtility.LineIntersectsAABB(p1, p2, aiactor.specRigidbody.HitboxPixelCollider.UnitBottomLeft, aiactor.specRigidbody.HitboxPixelCollider.UnitDimensions, out zero))
                            {
                                aiactor.healthHaver.ApplyDamage(num, Vector2.zero, "Chain Lightning", CoreDamageTypes.Electric, DamageCategory.Normal, false, null, false);
                                GameManager.Instance.StartCoroutine(this.HandleDamageCooldown(aiactor));
                            }
                        }
                    }
                }
            }
            private IEnumerator HandleDamageCooldown(AIActor damagedTarget)
            {
                this.m_damagedEnemies.Add(damagedTarget);
                yield return new WaitForSeconds(0.25f);
                this.m_damagedEnemies.Remove(damagedTarget);
                yield break;
            }
            public float DamagePerHit;
            private tk2dTiledSprite extantLink;
            private GameObject LinkVFXPrefab;
            private PlayerController Owner;
            private HashSet<AIActor> m_damagedEnemies = new HashSet<AIActor>();
        }
        public static GameObject prefab;

        private static readonly string guid = "diode_3032943893489384394893";
    }
}

