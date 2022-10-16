using System;
using System.Collections;
using System.Collections.Generic;
using Dungeonator;
using Gungeon;
using Alexandria.ItemAPI;
using UnityEngine;
using Alexandria.Misc;

namespace NevernamedsItems
{
    public class Potty : CompanionItem
    {
        public static void Init()
        {
            string name = "Potto";
            string resourcePath = "NevernamedsItems/Resources/Companions/Potty/potty_spawnobject_002";
            GameObject gameObject = new GameObject();
            var companionItem = gameObject.AddComponent<Potty>();
            ItemBuilder.AddSpriteToObject(name, resourcePath, gameObject);
            string shortDesc = "Clay Companion";
            string longDesc = "Occasionally pops out some money on room clear, and hides some... other secrets." + "\n\nThis little pot appears to have gained sentience. It's empty head contains many things, but most of all it is full of friendship!";
            companionItem.SetupItem(shortDesc, longDesc, "nn");
            companionItem.quality = PickupObject.ItemQuality.C;
            companionItem.CompanionGuid = Potty.guid;

            Potty.BuildPrefab();
            Potty.BuildCursedPrefab();
        }
        public override void Pickup(PlayerController player)
        {
            this.CompanionGuid = Potty.guid;
            base.Pickup(player);
        }
        public override void Update()
        {
            if (this.ExtantCompanion && this.ExtantCompanion.GetComponent<PottyCompanionBehaviour>() && Owner)
            {
                if (this.ExtantCompanion.GetComponent<PottyCompanionBehaviour>().dealsRadialCurseDamage && !Owner.PlayerHasActiveSynergy("Cursed Ceramics"))
                {
                    this.CompanionGuid = Potty.guid;
                    ForceCompanionRegeneration(Owner, null);
                }
                else if (!this.ExtantCompanion.GetComponent<PottyCompanionBehaviour>().dealsRadialCurseDamage && Owner.PlayerHasActiveSynergy("Cursed Ceramics"))
                {
                    this.CompanionGuid = Potty.cursedguid;
                    ForceCompanionRegeneration(Owner, null);
                }
            }
            base.Update();
        }

        private static tk2dSpriteCollectionData PottyAnimationCollection;
        public static void BuildPrefab()
        {
            bool flag = Potty.prefab != null || CompanionBuilder.companionDictionary.ContainsKey(Potty.guid);
            if (!flag)
            {
                Potty.prefab = CompanionBuilder.BuildPrefab("Potty Companion", Potty.guid, "NevernamedsItems/Resources/Companions/Potty/potty_idle_left_001", new IntVector2(4, 0), new IntVector2(9, 8));
                var companionController = Potty.prefab.AddComponent<PottyCompanionBehaviour>();
                companionController.CanInterceptBullets = false;
                companionController.companionID = CompanionController.CompanionIdentifier.NONE;
                companionController.aiActor.MovementSpeed = 6f;
                companionController.aiActor.healthHaver.PreventAllDamage = true;
                companionController.aiActor.CollisionDamage = 0f;
                companionController.aiActor.specRigidbody.CollideWithOthers = false;
                companionController.aiActor.specRigidbody.CollideWithTileMap = false;
                BehaviorSpeculator component = Potty.prefab.GetComponent<BehaviorSpeculator>();
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
                    Type = DirectionalAnimation.DirectionType.TwoWayHorizontal,
                    Flipped = new DirectionalAnimation.FlipType[2],
                    AnimNames = new string[]
                        {
                        "idle_right",
                        "idle_left"
                        }
                };
                aiAnimator.OtherAnimations = new List<AIAnimator.NamedDirectionalAnimation>
                {
                    new AIAnimator.NamedDirectionalAnimation
                    {
                        name = "spawnobject",
                        anim = new DirectionalAnimation
                        {
                            Type = DirectionalAnimation.DirectionType.Single,
                            Prefix = "spawnobject",
                            AnimNames = new string[1],
                            Flipped = new DirectionalAnimation.FlipType[1]
                        }
                    }, };

                //ADD SPRITES TO THE ANIMATIONS
                bool flag3 = Potty.PottyAnimationCollection == null;
                if (flag3)
                {
                    Potty.PottyAnimationCollection = SpriteBuilder.ConstructCollection(Potty.prefab, "PottyCompanion_Collection");
                    UnityEngine.Object.DontDestroyOnLoad(Potty.PottyAnimationCollection);
                    for (int i = 0; i < Potty.spritePaths.Length; i++)
                    {
                        SpriteBuilder.AddSpriteToCollection(Potty.spritePaths[i], Potty.PottyAnimationCollection);
                    }
                    //Idling Animation
                    SpriteBuilder.AddAnimation(companionController.spriteAnimator, Potty.PottyAnimationCollection, new List<int>
                    {
                        4,
                        5,
                        6,
                        7,
                    }, "idle_right", tk2dSpriteAnimationClip.WrapMode.Loop).fps = 8f;
                    SpriteBuilder.AddAnimation(companionController.spriteAnimator, Potty.PottyAnimationCollection, new List<int>
                    {
                        0,
                        1,
                        2,
                        3,
                    }, "idle_left", tk2dSpriteAnimationClip.WrapMode.Loop).fps = 8f;
                    //Running Animation
                    SpriteBuilder.AddAnimation(companionController.spriteAnimator, Potty.PottyAnimationCollection, new List<int>
                    {
                        14,
                        15,
                        16,
                        17,
                        18,
                        19
                    }, "run_right", tk2dSpriteAnimationClip.WrapMode.Loop).fps = 8f;
                    SpriteBuilder.AddAnimation(companionController.spriteAnimator, Potty.PottyAnimationCollection, new List<int>
                    {
                        8,
                        9,
                        10,
                        11,
                        12,
                        13
                    }, "run_left", tk2dSpriteAnimationClip.WrapMode.Loop).fps = 8f;
                    SpriteBuilder.AddAnimation(companionController.spriteAnimator, Potty.PottyAnimationCollection, new List<int>
                    {
                        20,
                        21,
                        22,
                        23,
                        24,
                        25,
                        26,
                      27
                    }, "spawnobject", tk2dSpriteAnimationClip.WrapMode.Once).fps = 12f;

                }
            }
        }
        private static string[] spritePaths = new string[]
        {
            //IdleLeft
            "NevernamedsItems/Resources/Companions/Potty/potty_idle_left_001", //0
            "NevernamedsItems/Resources/Companions/Potty/potty_idle_left_002", //1
            "NevernamedsItems/Resources/Companions/Potty/potty_idle_left_003", //2
            "NevernamedsItems/Resources/Companions/Potty/potty_idle_left_004", //3
            //IdleRight
            "NevernamedsItems/Resources/Companions/Potty/potty_idle_right_001", //4
            "NevernamedsItems/Resources/Companions/Potty/potty_idle_right_002", //5
            "NevernamedsItems/Resources/Companions/Potty/potty_idle_right_003", //6
            "NevernamedsItems/Resources/Companions/Potty/potty_idle_right_004", //7
            //RunLeft
            "NevernamedsItems/Resources/Companions/Potty/potty_run_left1", //8
            "NevernamedsItems/Resources/Companions/Potty/potty_run_left2", //9
            "NevernamedsItems/Resources/Companions/Potty/potty_run_left3", //10
            "NevernamedsItems/Resources/Companions/Potty/potty_run_left4", //11
            "NevernamedsItems/Resources/Companions/Potty/potty_run_left5", //12
            "NevernamedsItems/Resources/Companions/Potty/potty_run_left6", //13
            //RunRight
            "NevernamedsItems/Resources/Companions/Potty/potty_run_right1", //14
            "NevernamedsItems/Resources/Companions/Potty/potty_run_right2", //15
            "NevernamedsItems/Resources/Companions/Potty/potty_run_right3", //16
            "NevernamedsItems/Resources/Companions/Potty/potty_run_right4", //17
            "NevernamedsItems/Resources/Companions/Potty/potty_run_right5", //18
            "NevernamedsItems/Resources/Companions/Potty/potty_run_right6", //19
            //SpawnObject
            "NevernamedsItems/Resources/Companions/Potty/potty_spawnobject_001", //20
            "NevernamedsItems/Resources/Companions/Potty/potty_spawnobject_002", //21
            "NevernamedsItems/Resources/Companions/Potty/potty_spawnobject_003", //22
            "NevernamedsItems/Resources/Companions/Potty/potty_spawnobject_004", //23
            "NevernamedsItems/Resources/Companions/Potty/potty_spawnobject_005", //24
            "NevernamedsItems/Resources/Companions/Potty/potty_spawnobject_006", //25
            "NevernamedsItems/Resources/Companions/Potty/potty_spawnobject_007", //26
            "NevernamedsItems/Resources/Companions/Potty/potty_spawnobject_008", //27      
        };
        private static tk2dSpriteCollectionData CursedPottyAnimationCollection;
        public static void BuildCursedPrefab()
        {
            bool flag = Potty.cursedprefab != null || CompanionBuilder.companionDictionary.ContainsKey(Potty.cursedguid);
            if (!flag)
            {
                Potty.cursedprefab = CompanionBuilder.BuildPrefab("Cursed Potty Companion", Potty.cursedguid, "NevernamedsItems/Resources/Companions/Potty/cursedpotty_idle_left_001", new IntVector2(4, 0), new IntVector2(9, 8));
                var companionController = Potty.cursedprefab.AddComponent<PottyCompanionBehaviour>();
                companionController.CanInterceptBullets = false;
                companionController.companionID = CompanionController.CompanionIdentifier.NONE;
                companionController.aiActor.MovementSpeed = 6f;
                companionController.dealsRadialCurseDamage = true;
                companionController.aiActor.healthHaver.PreventAllDamage = true;
                companionController.aiActor.CollisionDamage = 0f;
                companionController.aiActor.specRigidbody.CollideWithOthers = false;
                companionController.aiActor.specRigidbody.CollideWithTileMap = false;
                BehaviorSpeculator component = Potty.cursedprefab.GetComponent<BehaviorSpeculator>();
                component.MovementBehaviors.Add(new CustomCompanionBehaviours.PottyCompanionApproach());
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
                    Type = DirectionalAnimation.DirectionType.TwoWayHorizontal,
                    Flipped = new DirectionalAnimation.FlipType[2],
                    AnimNames = new string[]
                        {
                        "idle_right",
                        "idle_left"
                        }
                };
                aiAnimator.OtherAnimations = new List<AIAnimator.NamedDirectionalAnimation>
                {
                    new AIAnimator.NamedDirectionalAnimation
                    {
                        name = "spawnobject",
                        anim = new DirectionalAnimation
                        {
                            Type = DirectionalAnimation.DirectionType.Single,
                            Prefix = "spawnobject",
                            AnimNames = new string[1],
                            Flipped = new DirectionalAnimation.FlipType[1]
                        }
                    }, };

                //ADD SPRITES TO THE ANIMATIONS
                bool flag3 = Potty.CursedPottyAnimationCollection == null;
                if (flag3)
                {
                    Potty.CursedPottyAnimationCollection = SpriteBuilder.ConstructCollection(Potty.cursedprefab, "CursedPottyCompanion_Collection");
                    UnityEngine.Object.DontDestroyOnLoad(Potty.CursedPottyAnimationCollection);
                    for (int i = 0; i < Potty.cursedspritePaths.Length; i++)
                    {
                        SpriteBuilder.AddSpriteToCollection(Potty.cursedspritePaths[i], Potty.CursedPottyAnimationCollection);
                    }
                    //Idling Animation
                    SpriteBuilder.AddAnimation(companionController.spriteAnimator, Potty.CursedPottyAnimationCollection, new List<int>
                    {
                        4,
                        5,
                        6,
                        7,
                    }, "idle_right", tk2dSpriteAnimationClip.WrapMode.Loop).fps = 8f;
                    SpriteBuilder.AddAnimation(companionController.spriteAnimator, Potty.CursedPottyAnimationCollection, new List<int>
                    {
                        0,
                        1,
                        2,
                        3,
                    }, "idle_left", tk2dSpriteAnimationClip.WrapMode.Loop).fps = 8f;
                    //Running Animation
                    SpriteBuilder.AddAnimation(companionController.spriteAnimator, Potty.CursedPottyAnimationCollection, new List<int>
                    {
                        14,
                        15,
                        16,
                        17,
                        18,
                        19
                    }, "run_right", tk2dSpriteAnimationClip.WrapMode.Loop).fps = 8f;
                    SpriteBuilder.AddAnimation(companionController.spriteAnimator, Potty.CursedPottyAnimationCollection, new List<int>
                    {
                        8,
                        9,
                        10,
                        11,
                        12,
                        13
                    }, "run_left", tk2dSpriteAnimationClip.WrapMode.Loop).fps = 8f;
                    SpriteBuilder.AddAnimation(companionController.spriteAnimator, Potty.CursedPottyAnimationCollection, new List<int>
                    {
                        20,
                        21,
                        22,
                        23,
                        24,
                        25,
                        26,
                      27
                    }, "spawnobject", tk2dSpriteAnimationClip.WrapMode.Once).fps = 12f;

                }
            }
        }
        private static string[] cursedspritePaths = new string[]
        {
            //IdleLeft
            "NevernamedsItems/Resources/Companions/Potty/cursedpotty_idle_left_001", //0
            "NevernamedsItems/Resources/Companions/Potty/cursedpotty_idle_left_002", //1
            "NevernamedsItems/Resources/Companions/Potty/cursedpotty_idle_left_003", //2
            "NevernamedsItems/Resources/Companions/Potty/cursedpotty_idle_left_004", //3
            //IdleRight
            "NevernamedsItems/Resources/Companions/Potty/cursedpotty_idle_right_001", //4
            "NevernamedsItems/Resources/Companions/Potty/cursedpotty_idle_right_002", //5
            "NevernamedsItems/Resources/Companions/Potty/cursedpotty_idle_right_003", //6
            "NevernamedsItems/Resources/Companions/Potty/cursedpotty_idle_right_004", //7
            //RunLeft
            "NevernamedsItems/Resources/Companions/Potty/cursedpotty_run_left_001", //8
            "NevernamedsItems/Resources/Companions/Potty/cursedpotty_run_left_002", //9
            "NevernamedsItems/Resources/Companions/Potty/cursedpotty_run_left_003", //10
            "NevernamedsItems/Resources/Companions/Potty/cursedpotty_run_left_004", //11
            "NevernamedsItems/Resources/Companions/Potty/cursedpotty_run_left_005", //12
            "NevernamedsItems/Resources/Companions/Potty/cursedpotty_run_left_006", //13
            //RunRight
            "NevernamedsItems/Resources/Companions/Potty/cursedpotty_run_right_001", //14
            "NevernamedsItems/Resources/Companions/Potty/cursedpotty_run_right_002", //15
            "NevernamedsItems/Resources/Companions/Potty/cursedpotty_run_right_003", //16
            "NevernamedsItems/Resources/Companions/Potty/cursedpotty_run_right_004", //17
            "NevernamedsItems/Resources/Companions/Potty/cursedpotty_run_right_005", //18
            "NevernamedsItems/Resources/Companions/Potty/cursedpotty_run_right_006", //19
            //SpawnObject
            "NevernamedsItems/Resources/Companions/Potty/cursedpotty_spawnobject_001", //20
            "NevernamedsItems/Resources/Companions/Potty/cursedpotty_spawnobject_002", //21
            "NevernamedsItems/Resources/Companions/Potty/cursedpotty_spawnobject_003", //22
            "NevernamedsItems/Resources/Companions/Potty/cursedpotty_spawnobject_004", //23
            "NevernamedsItems/Resources/Companions/Potty/cursedpotty_spawnobject_005", //24
            "NevernamedsItems/Resources/Companions/Potty/cursedpotty_spawnobject_006", //25
            "NevernamedsItems/Resources/Companions/Potty/cursedpotty_spawnobject_007", //26
            "NevernamedsItems/Resources/Companions/Potty/cursedpotty_spawnobject_008", //27      
        };
        public class PottyCompanionBehaviour : CompanionController
        {
            private void Start()
            {
                this.Owner = this.m_owner;
                Owner.OnRoomClearEvent += this.OnRoomClear;
                Owner.OnEnteredCombat += this.OnEnteredCombat;
                timer = 3;
                RadialTimer = 0.25f;
                ETGMod.AIActor.OnPreStart += this.AIActorPreSpawn;
            }
            private void OnEnteredCombat()
            {
                timer = 3;
                RadialTimer = 0.25f;
            }

            public override void OnDestroy()
            {
                if (Owner)
                {
                    Owner.OnRoomClearEvent -= this.OnRoomClear;
                    Owner.OnEnteredCombat -= this.OnEnteredCombat;
                }
                ETGMod.AIActor.OnPreStart -= this.AIActorPreSpawn;
                base.OnDestroy();
            }
            private void AIActorPreSpawn(AIActor enemy)
            {
                if (enemy.EnemyGuid == "c182a5cb704d460d9d099a47af49c913")
                {
                    if (enemy.CanTargetEnemies == false && enemy.CanTargetPlayers == true)
                    {
                        CompanionController orAddComponent = enemy.gameObject.GetOrAddComponent<CompanionController>();
                        orAddComponent.companionID = CompanionController.CompanionIdentifier.NONE;
                        orAddComponent.Initialize(Owner);
                        CompanionisedEnemyBulletModifiers companionisedBullets = enemy.gameObject.GetOrAddComponent<CompanionisedEnemyBulletModifiers>();
                        companionisedBullets.jammedDamageMultiplier = 2;
                        companionisedBullets.TintBullets = true;
                        companionisedBullets.TintColor = ExtendedColours.honeyYellow;
                        companionisedBullets.baseBulletDamage = 10;

                        enemy.ApplyEffect(GameManager.Instance.Dungeon.sharedSettingsPrefab.DefaultPermanentCharmEffect, 1f, null);
                        enemy.gameObject.AddComponent<KillOnRoomClear>();
                        enemy.IsHarmlessEnemy = true;
                        enemy.IgnoreForRoomClear = true;
                    }
                }
            }
            private float timer;
            private float RadialTimer;
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
                        //do stuff
                        bool doAnim = false;
                        if ((GameManager.Instance.Dungeon.tileIndices.tilesetId == GlobalDungeonData.ValidTilesets.HELLGEON) || Owner.PlayerHasActiveSynergy("They Grow Inside"))
                        {
                            float chance = 0.25f;
                            if (Owner.PlayerHasActiveSynergy("They Grow Inside")) chance = 0.50f;
                            if (UnityEngine.Random.value <= chance)
                            {
                                AIActor target = base.specRigidbody.UnitCenter.GetNearestEnemyToPosition();
                                doAnim = true;
                                if (target)
                                {
                                    VolleyReplacementSynergyProcessor shotgrubProcessor = (PickupObjectDatabase.GetById(347) as Gun).GetComponent<VolleyReplacementSynergyProcessor>();
                                    Projectile bullet = shotgrubProcessor.SynergyVolley.projectiles[0].projectiles[0].projectile;
                                    GameObject spawnedProj = ProjSpawnHelper.SpawnProjectileTowardsPoint(bullet.gameObject, base.sprite.WorldCenter, target.Position, 0, 15);
                                    Projectile component = spawnedProj.GetComponent<Projectile>();
                                    if (component != null)
                                    {
                                        component.Owner = Owner;
                                        component.Shooter = Owner.specRigidbody;
                                        //COMPANION SHIT
                                        component.TreatedAsNonProjectileForChallenge = true;
                                        component.baseData.damage *= Owner.stats.GetStatValue(PlayerStats.StatType.Damage);
                                        component.baseData.speed *= Owner.stats.GetStatValue(PlayerStats.StatType.ProjectileSpeed);
                                        component.baseData.force *= Owner.stats.GetStatValue(PlayerStats.StatType.KnockbackMultiplier);
                                        component.AdditionalScaleMultiplier *= Owner.stats.GetStatValue(PlayerStats.StatType.PlayerBulletScale);
                                        component.UpdateSpeed();
                                        base.HandleCompanionPostProcessProjectile(component);
                                    }
                                }
                            }
                        }
                        if (UnityEngine.Random.value <= 0.02f)
                        {
                            doAnim = true;
                            bool shouldJam = false;
                            if (Owner.PlayerHasActiveSynergy("Cursed Ceramics")) shouldJam = true;
                            AIActor fairy = CompanionisedEnemyUtility.SpawnCompanionisedEnemy(Owner, "c182a5cb704d460d9d099a47af49c913", base.specRigidbody.UnitCenter.ToIntVector2(), false, ExtendedColours.brown, 10, 2, shouldJam, true);
                            fairy.specRigidbody.CollideWithOthers = false;
                        }
                        if (UnityEngine.Random.value <= 0.05f && Owner.PlayerHasActiveSynergy("The Potter Boy"))
                        {
                            doAnim = true;
                            AIActor randomActiveEnemy = base.transform.position.GetAbsoluteRoom().GetRandomActiveEnemy(false);
                            if (randomActiveEnemy && randomActiveEnemy.IsNormalEnemy && randomActiveEnemy.healthHaver && !randomActiveEnemy.healthHaver.IsBoss)
                            {
                                randomActiveEnemy.Transmogrify(EnemyDatabase.GetOrLoadByGuid("76bc43539fc24648bff4568c75c686d1"), (GameObject)ResourceCache.Acquire("Global VFX/VFX_Item_Spawn_Poof"));
                            }
                        }
                        if (doAnim && this.aiAnimator)
                        {
                            this.aiAnimator.PlayUntilFinished("spawnobject", false, null, -1f, false);
                        }
                        timer = 2f;
                    }

                    if (RadialTimer > 0)
                    {
                        RadialTimer -= BraveTime.DeltaTime;
                    }
                    if (RadialTimer <= 0)
                    {
                        HandleRadialEffects();
                        RadialTimer = 0.05f;
                    }
                }
                base.Update();
            }
            private void HandleRadialEffects()
            {
                if (dealsRadialCurseDamage || Owner.PlayerHasActiveSynergy("Teapotto"))
                {
                    List<AIActor> activeEnemies = base.transform.position.GetAbsoluteRoom().GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
                    if (activeEnemies != null)
                    {
                        for (int i = 0; i < activeEnemies.Count; i++)
                        {
                            AIActor aiactor = activeEnemies[i];
                            if (aiactor.IsNormalEnemy)
                            {
                                float num = Vector2.Distance(base.specRigidbody.UnitCenter, aiactor.CenterPosition);
                                if (dealsRadialCurseDamage && num <= 4)
                                {
                                    float dmgToDo = 0.35f;
                                    dmgToDo += (Owner.stats.GetStatValue(PlayerStats.StatType.Curse)) * 0.1f;
                                    if (dmgToDo > 1.2f) dmgToDo = 1.2f;
                                    if (aiactor.healthHaver) aiactor.healthHaver.ApplyDamage(dmgToDo, Vector2.zero, "Cursed Ceramics", CoreDamageTypes.Magic, DamageCategory.DamageOverTime);
                                }

                                if (Owner.PlayerHasActiveSynergy("Teapotto") && num <= 4)
                                {
                                    aiactor.ApplyEffect(StaticStatusEffects.hotLeadEffect);
                                }
                                else if (Owner.PlayerHasActiveSynergy("Teapotto") && num <= 7 && Owner.CurrentGun && Owner.CurrentGun.PickupObjectId == 596)
                                {
                                    aiactor.ApplyEffect(StaticStatusEffects.hotLeadEffect);
                                }
                            }
                        }
                    }
                }
            }
            private void OnRoomClear(PlayerController playerController)
            {
                int maxCurrency = 4;
                int minCurrency = 1;
                if (Owner.PlayerHasActiveSynergy("What does it do?")) { maxCurrency = 7; minCurrency = 2; }
                if (Owner.PlayerHasActiveSynergy("Pot O' Gold") && UnityEngine.Random.value <= 0.01f) { maxCurrency = 51; minCurrency = 49; }
                LootEngine.SpawnCurrency(base.specRigidbody.UnitCenter, UnityEngine.Random.Range(minCurrency, maxCurrency));
                if (this.aiAnimator)
                {
                    this.aiAnimator.PlayUntilFinished("spawnobject", false, null, -1f, false);
                }
            }
            public PlayerController Owner;
            public bool dealsRadialCurseDamage;
        }
        public static GameObject prefab;
        public static GameObject cursedprefab;
        private static readonly string guid = "potty_companion23892838320020000";
        private static readonly string cursedguid = "cursedpotty_companion2307923873494782937839";
    }
}
