using System;
using System.Collections.Generic;
using Dungeonator;
using Gungeon;
using Alexandria.ItemAPI;
using Alexandria.Misc;
using UnityEngine;

namespace NevernamedsItems
{
    public class Gusty : PassiveItem
    {
        public static void Init()
        {
            string name = "Gusty";
            string resourcePath = "NevernamedsItems/Resources/Companions/Gusty/gusty_icon";
            GameObject gameObject = new GameObject();
            CompanionItem companionItem = gameObject.AddComponent<CompanionItem>();
            ItemBuilder.AddSpriteToObject(name, resourcePath, gameObject);
            string shortDesc = "Windbag";
            string longDesc = "Plomphs around, and doesn't let anyone get in his way." + "\n\nStore in a cool, dry place.";
            companionItem.SetupItem(shortDesc, longDesc, "nn");
            companionItem.quality = PickupObject.ItemQuality.D;
            companionItem.CompanionGuid = Gusty.guid;
            Gusty.BuildPrefab();
        }

        public static void BuildPrefab()
        {
            bool flag = Gusty.prefab != null || CompanionBuilder.companionDictionary.ContainsKey(Gusty.guid);
            if (!flag)
            {
                Gusty.prefab = CompanionBuilder.BuildPrefab("Gusty", Gusty.guid, "NevernamedsItems/Resources/Companions/Gusty/gusty_idle_001", new IntVector2(5, 3), new IntVector2(11, 11));
                var companionController = Gusty.prefab.AddComponent<GustyBehav>();
                companionController.aiActor.MovementSpeed = 5f;
                companionController.CanCrossPits = true;
                companionController.aiActor.ActorShadowOffset = new Vector3(0, -0.5f);
                Gusty.prefab.AddAnimation("flight", "NevernamedsItems/Resources/Companions/Gusty/gusty_idle", 7, CompanionBuilder.AnimationType.Idle, DirectionalAnimation.DirectionType.Single, DirectionalAnimation.FlipType.None);
                Gusty.prefab.AddAnimation("idle", "NevernamedsItems/Resources/Companions/Gusty/gusty_idle", 7, CompanionBuilder.AnimationType.Flight, DirectionalAnimation.DirectionType.Single, DirectionalAnimation.FlipType.None);
                Gusty.prefab.AddAnimation("attack", "NevernamedsItems/Resources/Companions/Gusty/gusty_attack", 14, CompanionBuilder.AnimationType.Idle, DirectionalAnimation.DirectionType.Single, DirectionalAnimation.FlipType.None);
                companionController.aiAnimator.GetDirectionalAnimation("idle").Prefix = "idle";
                prefab.GetComponent<tk2dSpriteAnimator>().GetClipByName("attack").wrapMode = tk2dSpriteAnimationClip.WrapMode.Once;
                BehaviorSpeculator component = Gusty.prefab.GetComponent<BehaviorSpeculator>();
                CustomCompanionBehaviours.SimpleCompanionApproach approach = new CustomCompanionBehaviours.SimpleCompanionApproach();
                approach.DesiredDistance = 2f;
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
        public class GustyBehav : CompanionController
        {
            private void Start()
            {
                this.Owner = this.m_owner;
                timer = 1.5f;
                Owner.OnPreDodgeRoll += this.OnOwnerDodgeRolled;
            }
            public override void OnDestroy()
            {
                if (Owner)
                {
                    Owner.OnPreDodgeRoll -= this.OnOwnerDodgeRolled;
                }

                base.OnDestroy();
            }
            private void OnOwnerDodgeRolled(PlayerController dodger)
            {
                if (dodger && dodger.PlayerHasActiveSynergy("Gale Force"))
                {
                    DoPloomph();
                }
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
                        DoPloomph();
                    }
                }
            }
            private void DoPloomph()
            {
                this.aiAnimator.PlayUntilFinished("attack", false, null, -1f, false);
                ExtendedPlayerComponent.QueriedCompanionStats query = Owner.GetExtComp().QueryCompanionStats(this.gameObject, 2.5f, 1f, 0, 0, 0, 0, KnockBackForce, 0);

                Exploder.DoRadialKnockback(this.specRigidbody.UnitCenter, query.modifiedKnockback, 10);
                Exploder.DoRadialDamage(query.modifiedDamage, this.specRigidbody.UnitCenter, 10, false, true, false, null);

                timer = 1.5f / query.modifiedFirerate;
            }
            private float KnockBackForce
            {
                get
                {
                    float initial = 70f;
                    if (Owner) initial *= Owner.stats.GetStatValue(PlayerStats.StatType.KnockbackMultiplier);
                    return initial;
                }
            }
            private float timer;
            public PlayerController Owner;
        }
        public static GameObject prefab;
        private static readonly string guid = "gusty94374329784374984563489";
    }
}