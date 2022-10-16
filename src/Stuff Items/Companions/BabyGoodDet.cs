using System;
using System.Collections;
using System.Collections.Generic;
using Alexandria.ItemAPI;
using Dungeonator;
using Gungeon;
using SaveAPI;
using UnityEngine;

namespace NevernamedsItems
{
    public class BabyGoodDet : PassiveItem
    {
        public static void Init()
        {
            string name = "Baby Good Det";
            string resourcePath = "NevernamedsItems/Resources/Companions/BabyGoodDet/babygooddet_icon";
            GameObject gameObject = new GameObject();
            CompanionItem companionItem = gameObject.AddComponent<CompanionItem>();
            ItemBuilder.AddSpriteToObject(name, resourcePath, gameObject);
            string shortDesc = "Laserbrained";
            string longDesc = "A scale model Det, constructed as part of some asinine Gundead school project."+"\n\nFires powerful lasers, but can have a hard time hitting her target.";
            companionItem.SetupItem(shortDesc, longDesc, "nn");
            companionItem.quality = PickupObject.ItemQuality.C;
            companionItem.CompanionGuid = BabyGoodDet.guid;
            BabyGoodDet.BuildPrefab();

            companionItem.SetupUnlockOnCustomFlag(CustomDungeonFlags.PURCHASED_BABYGOODDET, true);
            companionItem.AddItemToDougMetaShop(50);
        }

        public static void BuildPrefab()
        {
            bool flag = BabyGoodDet.prefab != null || CompanionBuilder.companionDictionary.ContainsKey(BabyGoodDet.guid);
            if (!flag)
            {
                BabyGoodDet.prefab = CompanionBuilder.BuildPrefab("Baby Good Det", BabyGoodDet.guid, "NevernamedsItems/Resources/Companions/BabyGoodDet/babygooddet_idle_001", new IntVector2(5, 3), new IntVector2(9, 8));
                var companionController = BabyGoodDet.prefab.AddComponent<BabyDetBehav>();
                companionController.aiActor.MovementSpeed = 5f;
                companionController.CanCrossPits = true;
                companionController.aiActor.SetIsFlying(true, "Flying Entity", false, true);
                companionController.aiActor.ActorShadowOffset = new Vector3(0, -0.5f);
                BabyGoodDet.prefab.AddAnimation("flight", "NevernamedsItems/Resources/Companions/BabyGoodDet/babygooddet_idle", 7, CompanionBuilder.AnimationType.Idle, DirectionalAnimation.DirectionType.Single, DirectionalAnimation.FlipType.None);
                BabyGoodDet.prefab.AddAnimation("idle", "NevernamedsItems/Resources/Companions/BabyGoodDet/babygooddet_idle", 7, CompanionBuilder.AnimationType.Flight, DirectionalAnimation.DirectionType.Single, DirectionalAnimation.FlipType.None);
                BabyGoodDet.prefab.AddAnimation("attack", "NevernamedsItems/Resources/Companions/BabyGoodDet/babygooddet_attack", 6, CompanionBuilder.AnimationType.Idle, DirectionalAnimation.DirectionType.Single, DirectionalAnimation.FlipType.None);
                BabyGoodDet.prefab.AddAnimation("contattack", "NevernamedsItems/Resources/Companions/BabyGoodDet/babygooddet_contattack", 6, CompanionBuilder.AnimationType.Idle, DirectionalAnimation.DirectionType.Single, DirectionalAnimation.FlipType.None);
                
                companionController.aiAnimator.GetDirectionalAnimation("idle").Prefix = "idle";
                prefab.GetComponent<tk2dSpriteAnimator>().GetClipByName("attack").wrapMode = tk2dSpriteAnimationClip.WrapMode.Once;
                prefab.GetComponent<tk2dSpriteAnimator>().GetClipByName("contattack").wrapMode = tk2dSpriteAnimationClip.WrapMode.Loop;
                BehaviorSpeculator component = BabyGoodDet.prefab.GetComponent<BehaviorSpeculator>();
                CustomCompanionBehaviours.SimpleCompanionApproach approach = new CustomCompanionBehaviours.SimpleCompanionApproach();
                approach.DesiredDistance = 5f;
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
        public class BabyDetBehav : CompanionController
        {
            private void Start()
            {
                this.Owner = this.m_owner;
                timer = 1.5f;
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
                        if (!isAttacking) StartCoroutine(DoLaserAttack());
                    }
                }
            }
            bool isAttacking = false;
            private IEnumerator DoLaserAttack()
            {
                isAttacking = true;
                this.aiAnimator.PlayUntilFinished("attack", false, null, -1f, false);
                yield return new WaitForSeconds(0.5f);
                //Start Firing Lasers
                this.aiActor.MovementSpeed *= 0.0001f;
                this.aiAnimator.PlayForDuration("contattack", 1);
                Projectile beamToFire = LaserBullets.SimpleRedBeam.projectile;
                if (UnityEngine.Random.value <= 0.5f)
                {
                    int angle = 135;
                    for (int i = 0; i < 4; i++)
                    {
                        BeamController beam = BeamAPI.FreeFireBeamFromAnywhere(beamToFire, this.m_owner, this.gameObject, Vector2.zero,  angle, 1, true);
                        Projectile beamprojcomponent = beam.GetComponent<Projectile>();
                        beamprojcomponent.baseData.damage *= 3f;
                        if (PassiveItem.IsFlagSetForCharacter(this.Owner, typeof(BattleStandardItem))) beamprojcomponent.baseData.damage *= BattleStandardItem.BattleStandardCompanionDamageMultiplier;
                        if (this.Owner.CurrentGun && this.Owner.CurrentGun.LuteCompanionBuffActive) beamprojcomponent.baseData.damage *= 2;
                        angle -= 90;
                    }                    
                }
                else
                {
                    int angle = 180;
                    for (int i = 0; i < 4; i++)
                    {
                        BeamController beam = BeamAPI.FreeFireBeamFromAnywhere(beamToFire, this.m_owner, this.gameObject, Vector2.zero,  angle, 1, true);
                        Projectile beamprojcomponent = beam.GetComponent<Projectile>();
                        beamprojcomponent.baseData.damage *= 3f;
                        if (PassiveItem.IsFlagSetForCharacter(this.Owner, typeof(BattleStandardItem))) beamprojcomponent.baseData.damage *= BattleStandardItem.BattleStandardCompanionDamageMultiplier;
                        if (this.Owner.CurrentGun && this.Owner.CurrentGun.LuteCompanionBuffActive) beamprojcomponent.baseData.damage *= 2;
                        angle -= 90;
                    }
                }
                yield return new WaitForSeconds(1f);
                //Stop shit
                isAttacking = false;
                timer = 1.5f;
                this.aiActor.MovementSpeed /= 0.0001f;
                yield break;
            }
            private float timer;
            public PlayerController Owner;
        }
        public static GameObject prefab;
        private static readonly string guid = "babygooddet849495496394626832697243782667wei";
    }
}