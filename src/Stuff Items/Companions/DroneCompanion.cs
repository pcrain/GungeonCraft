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
    public class DroneCompanion : PassiveItem
    {
        public static void Init()
        {
            string name = "Drone";
            string resourcePath = "NevernamedsItems/Resources/Companions/DroneCompanion/drone_icon";
            GameObject gameObject = new GameObject();
            var companionItem = gameObject.AddComponent<CompanionItem>();
            ItemBuilder.AddSpriteToObject(name, resourcePath, gameObject);
            string shortDesc = "Beep Boop I'm A Drone";
            string longDesc = "This little drone seems friendly despite it's objective lack of most defining features." + "\n\nIt seems accustomed to descending...";
            companionItem.SetupItem(shortDesc, longDesc, "nn");
            companionItem.quality = PickupObject.ItemQuality.B;
            companionItem.CompanionGuid = DroneCompanion.guid;

            DroneCompanion.BuildPrefab();

            Projectile projectile2 = UnityEngine.Object.Instantiate<Projectile>((PickupObjectDatabase.GetById(56) as Gun).DefaultModule.projectiles[0]);
            projectile2.gameObject.SetActive(false);
            FakePrefab.MarkAsFakePrefab(projectile2.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(projectile2);
            projectile2.baseData.speed *= 1.2f;
            projectile2.baseData.range *= 1f;
            projectile2.baseData.damage *= 0.8f;
            projectile2.gameObject.AddComponent<DroneBulletComponent>();
            projectile2.SetProjectileSpriteRight("drone_projectile", 14, 8, true, tk2dBaseSprite.Anchor.MiddleCenter, 12, 6);
            DroneCompanionProjectile = projectile2;
            DroneID = companionItem.PickupObjectId;
            companionItem.SetupUnlockOnCustomFlag(CustomDungeonFlags.PURCHASED_DRONE, true);
        }
        public static int DroneID;
        public static Projectile DroneCompanionProjectile;
        private static tk2dSpriteCollectionData DroneAnimationCollection;
        public static void BuildPrefab()
        {
            bool flag = DroneCompanion.prefab != null || CompanionBuilder.companionDictionary.ContainsKey(DroneCompanion.guid);
            if (!flag)
            {
                DroneCompanion.prefab = CompanionBuilder.BuildPrefab("Drone Companion", DroneCompanion.guid, "NevernamedsItems/Resources/Companions/DroneCompanion/drone_idle_001", new IntVector2(5, 10), new IntVector2(10, 12));
                var companionController = DroneCompanion.prefab.AddComponent<DroneCompanionBehaviour>();
                companionController.CanCrossPits = true;
                companionController.CanInterceptBullets = false;
                companionController.companionID = CompanionController.CompanionIdentifier.NONE;
                companionController.aiActor.MovementSpeed = 7f;
                companionController.aiActor.healthHaver.PreventAllDamage = true;
                companionController.aiActor.CollisionDamage = 0f;
                companionController.aiActor.ActorShadowOffset = new Vector3(0, -1);
                companionController.aiActor.specRigidbody.CollideWithOthers = false;
                companionController.aiActor.specRigidbody.CollideWithTileMap = false;

                BehaviorSpeculator component = DroneCompanion.prefab.GetComponent<BehaviorSpeculator>();
                component.MovementBehaviors.Add(new CompanionFollowPlayerBehavior
                {
                    IdleAnimations = new string[]
                    {
                        "idle"
                    }
                });
                //SET UP ANIMATIONS
                AIAnimator aiAnimator = companionController.aiAnimator;
                aiAnimator.MoveAnimation = new DirectionalAnimation
                {
                    Type = DirectionalAnimation.DirectionType.TwoWayHorizontal,
                    Flipped = new DirectionalAnimation.FlipType[2],
                    AnimNames = new string[]
                        {
                        "run_right",
                        "run_left"
                        }
                };
                aiAnimator.IdleAnimation = new DirectionalAnimation
                {
                    Type = DirectionalAnimation.DirectionType.Single,
                    Prefix = "idle",
                    AnimNames = new string[1],
                    Flipped = new DirectionalAnimation.FlipType[1]
                };

                //ADD SPRITES TO THE ANIMATIONS
                bool flag3 = DroneCompanion.DroneAnimationCollection == null;
                if (flag3)
                {
                    DroneCompanion.DroneAnimationCollection = SpriteBuilder.ConstructCollection(DroneCompanion.prefab, "DroneCompanion_Collection");
                    UnityEngine.Object.DontDestroyOnLoad(DroneCompanion.DroneAnimationCollection);
                    for (int i = 0; i < DroneCompanion.spritePaths.Length; i++)
                    {
                        SpriteBuilder.AddSpriteToCollection(DroneCompanion.spritePaths[i], DroneCompanion.DroneAnimationCollection);
                    }
                    //Idling Animation
                    SpriteBuilder.AddAnimation(companionController.spriteAnimator, DroneCompanion.DroneAnimationCollection, new List<int>
                    {
                        0,
                        1,
                        2,
                        3,
                        4,
                        5,
                    }, "idle", tk2dSpriteAnimationClip.WrapMode.Loop).fps = 8f;
                    //Running Animation
                    SpriteBuilder.AddAnimation(companionController.spriteAnimator, DroneCompanion.DroneAnimationCollection, new List<int>
                    {
                        6,
                        7,
                        8
                    }, "run_right", tk2dSpriteAnimationClip.WrapMode.Loop).fps = 8f;
                    SpriteBuilder.AddAnimation(companionController.spriteAnimator, DroneCompanion.DroneAnimationCollection, new List<int>
                    {
                        9,
                        10,
                        11
                    }, "run_left", tk2dSpriteAnimationClip.WrapMode.Loop).fps = 8f;

                }
            }
        }
        private static string[] spritePaths = new string[]
        {
            "NevernamedsItems/Resources/Companions/DroneCompanion/drone_idle_001", //0
            "NevernamedsItems/Resources/Companions/DroneCompanion/drone_idle_002", //1
            "NevernamedsItems/Resources/Companions/DroneCompanion/drone_idle_003", //2
            "NevernamedsItems/Resources/Companions/DroneCompanion/drone_idle_004", //3
            "NevernamedsItems/Resources/Companions/DroneCompanion/drone_idle_005", //4
            "NevernamedsItems/Resources/Companions/DroneCompanion/drone_idle_006", //5         
            "NevernamedsItems/Resources/Companions/DroneCompanion/drone_run_right_001", //6
            "NevernamedsItems/Resources/Companions/DroneCompanion/drone_run_right_002", //7
            "NevernamedsItems/Resources/Companions/DroneCompanion/drone_run_right_003", //8
            "NevernamedsItems/Resources/Companions/DroneCompanion/drone_run_left_001", //9
            "NevernamedsItems/Resources/Companions/DroneCompanion/drone_run_left_002", //10
            "NevernamedsItems/Resources/Companions/DroneCompanion/drone_run_left_003", //11

        };
        public class DroneBulletComponent : MonoBehaviour
        {

        }
        public class DroneCompanionBehaviour : CompanionController
        {
            private void Start()
            {
                this.Owner = this.m_owner;
                Owner.PostProcessProjectile += this.OnOwnerFiredGun;
                Owner.OnEnteredCombat += OnEnteredCombat;
            }
            private void OnOwnerFiredGun(Projectile bullet, float h)
            {
                if (!bullet.TreatedAsNonProjectileForChallenge)
                {
                    if (bullet.gameObject.GetComponent<DroneBulletComponent>() == null && bullet.gameObject.GetComponent<BulletsWithGuns.BulletFromBulletWithGun>() == null)
                    {
                        TriggerShootBullet(bullet.baseData.range, bullet.baseData.speed);
                    }
                }
            }
            private void TriggerShootBullet(float range, float speed, float overrideDamageMult = 1)
            {
                if (base.specRigidbody && Owner && Owner.CurrentGun)
                {
                    float currentGunAngleVariance = Owner.CurrentGun.RawDefaultModule().angleVariance;
                    GameObject gameObject = ProjSpawnHelper.SpawnProjectileTowardsPoint(DroneCompanion.DroneCompanionProjectile.gameObject, base.specRigidbody.UnitBottomCenter, Owner.unadjustedAimPoint.XY(), 0, currentGunAngleVariance);
                    Projectile component = gameObject.GetComponent<Projectile>();
                    if (component != null)
                    {
                        component.Owner = Owner;
                        component.Shooter = base.specRigidbody;
                        //COMPANION SHIT
                        component.ApplyCompanionModifierToBullet(Owner);


                        component.TreatedAsNonProjectileForChallenge = true;
                        component.baseData.range = range;
                        component.baseData.damage *= overrideDamageMult;
                        component.baseData.damage *= Owner.stats.GetStatValue(PlayerStats.StatType.Damage);
                        component.baseData.speed = speed;
                        component.baseData.force *= Owner.stats.GetStatValue(PlayerStats.StatType.KnockbackMultiplier);
                        component.AdditionalScaleMultiplier *= Owner.stats.GetStatValue(PlayerStats.StatType.PlayerBulletScale);
                        component.UpdateSpeed();
                        Owner.DoPostProcessProjectile(component);
                    }
                    if (Owner.PlayerHasActiveSynergy("Wrong Kind Of Drone") && UnityEngine.Random.value <= 0.2f)
                    {
                        FireBeeBullet();
                    }
                }
            }
            private void FireBeeBullet()
            {
                Projectile projectile = ((Gun)ETGMod.Databases.Items[14]).DefaultModule.projectiles[0];               
                GameObject gameObject = ProjSpawnHelper.SpawnProjectileTowardsPoint(projectile.gameObject, base.specRigidbody.UnitBottomCenter, Owner.unadjustedAimPoint.XY(), 0, 0);
                Projectile component = gameObject.GetComponent<Projectile>();
                if (component != null)
                {
                    component.Owner = Owner;
                    component.Shooter = base.specRigidbody;
                    Owner.DoPostProcessProjectile(component);
                }
            }
            private void OnEnteredCombat()
            {
                if (Owner)
                {
                    base.aiActor.CompanionWarp(Owner.specRigidbody.UnitCenter);
                }
            }
            public override void OnDestroy()
            {
                if (Owner)
                {
                Owner.OnEnteredCombat -= OnEnteredCombat;
                    Owner.PostProcessProjectile -= this.OnOwnerFiredGun;
                }
                base.OnDestroy();
            }
            public override void Update()
            {
                if (Owner && Owner.CurrentGun && canFireBeamChargeProjectile)
                {
                    if (Owner.CurrentGun.DefaultModule.shootStyle == ProjectileModule.ShootStyle.Beam)
                    {
                        if (Owner.CurrentGun.IsFiring)
                        {
                            TriggerShootBullet(10000000, (20 * Owner.stats.GetStatValue(PlayerStats.StatType.ProjectileSpeed)), 0.5f);
                            canFireBeamChargeProjectile = false;
                            Invoke("ResetBeamChargeShit", 0.05f);

                        }
                    }
                    if (Owner.CurrentGun.DefaultModule.shootStyle == ProjectileModule.ShootStyle.Charged)
                    {
                        if (Owner.CurrentGun.IsCharging)
                        {
                            TriggerShootBullet(10000000,(20 * Owner.stats.GetStatValue(PlayerStats.StatType.ProjectileSpeed)));
                            canFireBeamChargeProjectile = false;
                            Invoke("ResetBeamChargeShit", 0.25f);
                        }
                    }
                }
                base.Update();
            }
            bool canFireBeamChargeProjectile = true;
            private void ResetBeamChargeShit() { canFireBeamChargeProjectile = true; }


            public PlayerController Owner;
        }
        public static GameObject prefab;
        private static readonly string guid = "drone_companion3873585485739893484";
    }
}