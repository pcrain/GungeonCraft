using System;
using System.Collections;
using System.Collections.Generic;
using Alexandria.Misc;
using Dungeonator;
using Gungeon;
using Alexandria.ItemAPI;
using UnityEngine;

namespace NevernamedsItems
{
    public class GregTheEgg : CompanionItem
    {
        public static void Init()
        {
            string name = "Greg The Egg";
            string resourcePath = "NevernamedsItems/Resources/Companions/Greg/gregtheegg_icon";
            GameObject gameObject = new GameObject();
            var companionItem = gameObject.AddComponent<GregTheEgg>();
            ItemBuilder.AddSpriteToObject(name, resourcePath, gameObject);
            string shortDesc = "I'm About To Break!";
            string longDesc = "A strange friendly egg." + "\n\nLegends of this creature span history, with him accompanying hundreds of ancient heroes upon their adventures.";
            companionItem.SetupItem(shortDesc, longDesc, "nn");
            companionItem.quality = PickupObject.ItemQuality.B;
            companionItem.CompanionGuid = GregTheEgg.guid;

            GregTheEgg.BuildPrefab();
        }
        float cachedGregHealth = 3000;

        private static tk2dSpriteCollectionData GregAnimationCollection;
        public static void BuildPrefab()
        {
            bool flag = GregTheEgg.prefab != null || CompanionBuilder.companionDictionary.ContainsKey(GregTheEgg.guid);
            if (!flag)
            {
                GregTheEgg.prefab = CompanionBuilder.BuildPrefab("Greg The Egg", GregTheEgg.guid, "NevernamedsItems/Resources/Companions/Greg/gregtheegg_idle_001", new IntVector2(1, 2), new IntVector2(9, 12));
                var companionController = GregTheEgg.prefab.AddComponent<GregTheEggBehaviour>();
                companionController.CanInterceptBullets = true;
                companionController.companionID = CompanionController.CompanionIdentifier.NONE;
                companionController.aiActor.MovementSpeed = 5f;
                companionController.aiActor.healthHaver.PreventAllDamage = false;
                companionController.aiActor.CollisionDamage = 0f;
                companionController.aiActor.specRigidbody.CollideWithOthers = true;
                companionController.aiActor.specRigidbody.CollideWithTileMap = false;
                companionController.aiActor.healthHaver.ForceSetCurrentHealth(3000f); //4000
                companionController.aiActor.healthHaver.SetHealthMaximum(3000f, null, false); //4000



                companionController.aiActor.specRigidbody.PixelColliders.Clear();
                companionController.aiActor.specRigidbody.PixelColliders.Add(new PixelCollider
                {
                    ColliderGenerationMode = PixelCollider.PixelColliderGeneration.Manual,
                    CollisionLayer = CollisionLayer.EnemyCollider,
                    IsTrigger = false,
                    BagleUseFirstFrameOnly = false,
                    SpecifyBagelFrame = string.Empty,
                    BagelColliderNumber = 0,
                    ManualOffsetX = 1,
                    ManualOffsetY = 2,
                    ManualWidth = 9,
                    ManualHeight = 12,
                    ManualDiameter = 0,
                    ManualLeftX = 0,
                    ManualLeftY = 0,
                    ManualRightX = 0,
                    ManualRightY = 0
                });
                companionController.aiAnimator.specRigidbody.PixelColliders.Add(new PixelCollider
                {
                    ColliderGenerationMode = PixelCollider.PixelColliderGeneration.Manual,
                    CollisionLayer = CollisionLayer.PlayerHitBox,
                    IsTrigger = false,
                    BagleUseFirstFrameOnly = false,
                    SpecifyBagelFrame = string.Empty,
                    BagelColliderNumber = 0,
                    ManualOffsetX = 1,
                    ManualOffsetY = 2,
                    ManualWidth = 9,
                    ManualHeight = 12,
                    ManualDiameter = 0,
                    ManualLeftX = 0,
                    ManualLeftY = 0,
                    ManualRightX = 0,
                    ManualRightY = 0
                });
                BehaviorSpeculator component = GregTheEgg.prefab.GetComponent<BehaviorSpeculator>();
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
                    Type = DirectionalAnimation.DirectionType.Single,
                    Prefix = "run",
                    AnimNames = new string[1],
                    Flipped = new DirectionalAnimation.FlipType[1]
                };
                aiAnimator.OtherAnimations = new List<AIAnimator.NamedDirectionalAnimation>
                {
                    new AIAnimator.NamedDirectionalAnimation
                    {
                        name = "die",
                        anim = new DirectionalAnimation
                        {
                            Type = DirectionalAnimation.DirectionType.Single,
                            Prefix = "die",
                            AnimNames = new string[1],
                            Flipped = new DirectionalAnimation.FlipType[1]
                        }
                    },
                    new AIAnimator.NamedDirectionalAnimation
                    {
                        name = "spawnloot",
                        anim = new DirectionalAnimation
                        {
                            Type = DirectionalAnimation.DirectionType.Single,
                            Prefix = "spawnloot",
                            AnimNames = new string[1],
                            Flipped = new DirectionalAnimation.FlipType[1]
                        }
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
                bool flag3 = GregTheEgg.GregAnimationCollection == null;
                if (flag3)
                {
                    GregTheEgg.GregAnimationCollection = SpriteBuilder.ConstructCollection(GregTheEgg.prefab, "GregTheEgg_Collection");
                    UnityEngine.Object.DontDestroyOnLoad(GregTheEgg.GregAnimationCollection);
                    for (int i = 0; i < GregTheEgg.spritePaths.Length; i++)
                    {
                        SpriteBuilder.AddSpriteToCollection(GregTheEgg.spritePaths[i], GregTheEgg.GregAnimationCollection);
                    }
                    //Loot Spawning Animation
                    SpriteBuilder.AddAnimation(companionController.spriteAnimator, GregTheEgg.GregAnimationCollection, new List<int>
                    {
                        13,
                        14,
                        15,
                        14,
                        15,
                        14,
                        15,
                        14,
                        15,
                        14,
                        15,
                    }, "spawnloot", tk2dSpriteAnimationClip.WrapMode.Once).fps = 12f;
                    //Idling Animation
                    SpriteBuilder.AddAnimation(companionController.spriteAnimator, GregTheEgg.GregAnimationCollection, new List<int>
                    {
                        9,
                        10,
                        11,
                        12
                    }, "idle", tk2dSpriteAnimationClip.WrapMode.Loop).fps = 6f;
                    //Running Animation
                    SpriteBuilder.AddAnimation(companionController.spriteAnimator, GregTheEgg.GregAnimationCollection, new List<int>
                    {
                        16,
                        17,
                        18,
                        19,
                        20,
                        21,
                        22,
                        23
                    }, "run", tk2dSpriteAnimationClip.WrapMode.Loop).fps = 14f;
                    //Cracking and Dying Animation
                    SpriteBuilder.AddAnimation(companionController.spriteAnimator, GregTheEgg.GregAnimationCollection, new List<int>
                    {
                        0,
                        1,
                        2,
                        3,
                        4,
                        5,
                        6,
                        7,
                        8
                    }, "die", tk2dSpriteAnimationClip.WrapMode.Once).fps = 12f;

                }
            }
        }
        private static string[] spritePaths = new string[]
        {
            "NevernamedsItems/Resources/Companions/Greg/gregtheegg_die_001", //0
            "NevernamedsItems/Resources/Companions/Greg/gregtheegg_die_002", //1
            "NevernamedsItems/Resources/Companions/Greg/gregtheegg_die_003", //2
            "NevernamedsItems/Resources/Companions/Greg/gregtheegg_die_004", //3
            "NevernamedsItems/Resources/Companions/Greg/gregtheegg_die_005", //4 
            "NevernamedsItems/Resources/Companions/Greg/gregtheegg_die_006", //5 
            "NevernamedsItems/Resources/Companions/Greg/gregtheegg_die_007", //6
            "NevernamedsItems/Resources/Companions/Greg/gregtheegg_die_008", //7 
            "NevernamedsItems/Resources/Companions/Greg/gregtheegg_die_009", //8
            "NevernamedsItems/Resources/Companions/Greg/gregtheegg_idle_001", //9
            "NevernamedsItems/Resources/Companions/Greg/gregtheegg_idle_002", //10
            "NevernamedsItems/Resources/Companions/Greg/gregtheegg_idle_003", //11
            "NevernamedsItems/Resources/Companions/Greg/gregtheegg_idle_004", //12
            "NevernamedsItems/Resources/Companions/Greg/gregtheegg_spawnloot_001", //13
            "NevernamedsItems/Resources/Companions/Greg/gregtheegg_spawnloot_002", //14
            "NevernamedsItems/Resources/Companions/Greg/gregtheegg_spawnloot_003", //15
            "NevernamedsItems/Resources/Companions/Greg/gregtheegg_run_001", //16
            "NevernamedsItems/Resources/Companions/Greg/gregtheegg_run_002", //17
            "NevernamedsItems/Resources/Companions/Greg/gregtheegg_run_003", //18
            "NevernamedsItems/Resources/Companions/Greg/gregtheegg_run_004", //19
            "NevernamedsItems/Resources/Companions/Greg/gregtheegg_run_005", //20
            "NevernamedsItems/Resources/Companions/Greg/gregtheegg_run_006", //21
            "NevernamedsItems/Resources/Companions/Greg/gregtheegg_run_007", //22
            "NevernamedsItems/Resources/Companions/Greg/gregtheegg_run_008", //23
        };
        public class GoodMimicStoredGregHealth : MonoBehaviour
        {
            public float cachedHealth = 3000;
        }
        public class GregTheEggBehaviour : CompanionController
        {
            private void Start()
            {
                this.Owner = this.m_owner;
                Owner.OnRoomClearEvent += OnRoomClear;
                base.healthHaver.OnPreDeath += this.OnPreDeath;
                base.specRigidbody.OnPreRigidbodyCollision += this.OnPreCollision;

                gregHologram = base.gameObject.GetOrAddComponent<CustomHologramDoer>();

                foreach (PassiveItem item in Owner.passiveItems)
                {
                    //ETGModConsole.Log("An item was found in the owner's items");
                    if (item && item.GetComponent<GregTheEgg>())
                    {
                        if (item.GetComponent<GregTheEgg>().ExtantCompanion)
                        {
                            if (item.GetComponent<GregTheEgg>().ExtantCompanion == base.gameObject)
                            {
                                ConnectedItem = item.GetComponent<GregTheEgg>();
                                //ETGModConsole.Log("ConnectedItem was assigned");
                            }
                            //else ETGModConsole.Log("Extand companion was not this Greg.");
                        }
                        // else ETGModConsole.Log("Greg did not have an extant companion");
                    }
                    else if (item && item.GetComponent<CompanionItem>())
                    {
                        //ETGModConsole.Log("CompanionItem found");
                        if (item.GetComponent<CompanionItem>().ExtantCompanion)
                        {
                            //  ETGModConsole.Log("Companion item has Extant Companion");

                            if (item.GetComponent<CompanionItem>().ExtantCompanion == base.gameObject)
                            {
                                //         ETGModConsole.Log("Extant Companion is this");

                                ConnectedItemIfGoodMimic = item.GetComponent<CompanionItem>();
                                item.gameObject.GetOrAddComponent<GoodMimicStoredGregHealth>();
                            }
                        }


                    }

                    //else ETGModConsole.Log("Item was not a greg.");
                }
            }
            public override void OnDestroy()
            {
                if (Owner)
                {
                    base.healthHaver.OnPreDeath -= this.OnPreDeath;
                    base.specRigidbody.OnPreRigidbodyCollision -= this.OnPreCollision;
                    Owner.OnRoomClearEvent -= OnRoomClear;
                }
                base.OnDestroy();
            }
            public override void Update()
            {
                if (base.healthHaver && ConnectedItem)
                {
                    if (base.healthHaver.GetCurrentHealth() < ConnectedItem.cachedGregHealth)
                    {
                        //       ETGModConsole.Log("Cached Greg Health (" + ConnectedItem.cachedGregHealth + ") was updated to " + base.healthHaver.GetCurrentHealth());
                        ConnectedItem.cachedGregHealth = base.healthHaver.GetCurrentHealth();
                    }
                    else if (base.healthHaver.GetCurrentHealth() > ConnectedItem.cachedGregHealth)
                    {
                        //       ETGModConsole.Log("Greg Companion Health (" + base.healthHaver.GetCurrentHealth() + ") was altered to match the cached " + ConnectedItem.cachedGregHealth);
                        base.healthHaver.ForceSetCurrentHealth(ConnectedItem.cachedGregHealth);
                    }
                }
                else if (base.healthHaver && ConnectedItemIfGoodMimic)
                {
                    if (ConnectedItemIfGoodMimic.GetComponent<GoodMimicStoredGregHealth>())
                    {
                        float cachedHP = ConnectedItemIfGoodMimic.GetComponent<GoodMimicStoredGregHealth>().cachedHealth;
                        if (base.healthHaver.GetCurrentHealth() < cachedHP)
                        {
                            //         ETGModConsole.Log("Cached Mimic Greg Health (" + cachedHP + ") was updated to " + base.healthHaver.GetCurrentHealth());
                            ConnectedItemIfGoodMimic.GetComponent<GoodMimicStoredGregHealth>().cachedHealth = base.healthHaver.GetCurrentHealth();
                        }
                        else if (base.healthHaver.GetCurrentHealth() > cachedHP)
                        {
                            //          ETGModConsole.Log("Mimic Greg Companion Health (" + base.healthHaver.GetCurrentHealth() + ") was altered to match the cached " + cachedHP);
                            base.healthHaver.ForceSetCurrentHealth(cachedHP);
                        }
                    }
                }
                if (Owner.PlayerHasActiveSynergy("Scrambled Gregs") && base.aiActor && base.healthHaver && base.healthHaver.IsAlive)
                {
                    if (Owner && Owner.IsInCombat && !isOnScramblerCooldown)
                    {
                        Projectile projectile2 = (PickupObjectDatabase.GetById(445) as Gun).DefaultModule.projectiles[0];
                        
                        GameObject gameObject = projectile2.InstantiateAndFireTowardsPosition(base.GetComponent<tk2dSprite>().WorldCenter, base.GetComponent<tk2dSprite>().WorldCenter.GetPositionOfNearestEnemy(ActorCenter.SPRITE), 0, 7, Owner);
                        Projectile component = gameObject.GetComponent<Projectile>();
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
                        isOnScramblerCooldown = true;
                        Invoke("resetFireCooldown", 5);
                    }
                }
                //Century Greg Synergy
                if (Owner && base.transform && base.specRigidbody && base.gameObject && base.healthHaver && base.healthHaver.IsAlive && Owner.PlayerHasActiveSynergy("Century Greg"))
                {
                    Chest chest = null;
                    float num2 = float.MaxValue;
                    for (int j = 0; j < StaticReferenceManager.AllChests.Count; j++)
                    {
                        Chest chest2 = StaticReferenceManager.AllChests[j];
                        if (chest2 && chest2.sprite && !chest2.IsOpen && !chest2.IsBroken && !chest2.IsSealed)
                        {
                            float num3 = Vector2.Distance(base.transform.position, chest2.sprite.WorldCenter);
                            if (num3 < num2)
                            {
                                chest = chest2;
                                num2 = num3;
                            }
                        }
                    }
                    if (num2 > 5f)
                    {
                        chest = null;
                    }
                    if (lastPredictedChest != chest)
                    {
                        if (lastPredictedChest)
                        {
                            //if (gregHologram == null) ETGModConsole.Log("base.m_hologram is null");
                            gregHologram.HideSprite();
                        }
                        if (chest)
                        {
                            List<PickupObject> list = chest.PredictContents(Owner);
                            if (list.Count > 0 && list[0].encounterTrackable)
                            {
                                tk2dSpriteCollectionData encounterIconCollection = AmmonomiconController.ForceInstance.EncounterIconCollection;
                                gregHologram.ShowSprite(encounterIconCollection, encounterIconCollection.GetSpriteIdByName(list[0].encounterTrackable.journalData.AmmonomiconSprite));
                            }
                        }
                        lastPredictedChest = chest;
                    }
                }
                else if (gregHologram.extantSprite)
                {
                    gregHologram.HideSprite();
                }
                base.Update();
            }
           private Chest lastPredictedChest;
            private void OnPreCollision(SpeculativeRigidbody myRigidbody, PixelCollider myPixelCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherPixelCollider)
            {
                if (base.specRigidbody && base.aiActor && base.healthHaver && base.healthHaver.IsAlive && Owner && base.sprite)
                {
                    if (otherRigidbody.projectile && !(otherRigidbody.projectile.Owner is PlayerController))
                    {
                        if (Owner.PlayerHasActiveSynergy("Deviled Gregs"))
                        {
                            if (UnityEngine.Random.value <= 0.15f)
                            {
                                float spawnAngle = 0f;
                                for (int i = 0; i < 8; i++)
                                {
                                    GameObject gameObject = SpawnManager.SpawnProjectile((PickupObjectDatabase.GetById(336) as Gun).DefaultModule.projectiles[0].gameObject, base.sprite.WorldCenter, Quaternion.Euler(0f, 0f, spawnAngle), true);
                                    Projectile component = gameObject.GetComponent<Projectile>();
                                    if (component)
                                    {
                                        component.SpawnedFromOtherPlayerProjectile = false;
                                        if (Owner != null)
                                        {
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
                                    spawnAngle += 45;
                                }
                            }
                        }
                        if (Owner.PlayerHasActiveSynergy("Hard Boiled Gregs"))
                        {
                            PassiveReflectItem.ReflectBullet(otherRigidbody.projectile, true, Owner.specRigidbody.gameActor, 10f, 1f, 1f, 0f);
                            if (base.healthHaver) base.healthHaver.ApplyDamage(13, Vector2.zero, "Greggo");


                            PhysicsEngine.SkipCollision = true;
                        }
                    }
                }
            }
            private void resetFireCooldown() { isOnScramblerCooldown = false; }
            bool isOnScramblerCooldown = false;
            private void OnPreDeath(Vector2 something)
            {
                if (ConnectedItem && base.transform && Owner)
                {
                    Owner.StartCoroutine(GiveGregDeathPayout(Owner, base.transform.position, ConnectedItem, false, Owner.PlayerHasActiveSynergy("Free Range Gregs"), Owner.PlayerHasActiveSynergy("Greg Salad")));
                }
                else if (ConnectedItemIfGoodMimic && base.transform && Owner)
                {
                    Owner.StartCoroutine(GiveGregDeathPayout(Owner, base.transform.position, ConnectedItemIfGoodMimic, true, Owner.PlayerHasActiveSynergy("Free Range Gregs"), Owner.PlayerHasActiveSynergy("Greg Salad")));
                }
                Owner.OnRoomClearEvent -= OnRoomClear;
                base.healthHaver.OnPreDeath -= this.OnPreDeath;
            }
            public IEnumerator GiveGregDeathPayout(PlayerController playerOwner, Vector3 positionToSpawn, PickupObject itemInPlayerInventory, bool isGoodMimic, bool hasSynergyFreeRange, bool hasSynergyGregSalad)
            {
                itemInPlayerInventory.CanBeDropped = false;
                PickupObject itemToSpawn = null;
                if (hasSynergyFreeRange)
                {
                    float randomType = UnityEngine.Random.value;
                    if (randomType <= 0.25f) itemToSpawn = LootEngine.GetItemOfTypeAndQuality<PlayerItem>(PickupObject.ItemQuality.A, GameManager.Instance.RewardManager.ItemsLootTable, true);
                    else if (randomType <= 0.625f) itemToSpawn = LootEngine.GetItemOfTypeAndQuality<Gun>(PickupObject.ItemQuality.A, GameManager.Instance.RewardManager.GunsLootTable, true);
                    else itemToSpawn = LootEngine.GetItemOfTypeAndQuality<PassiveItem>(PickupObject.ItemQuality.A, GameManager.Instance.RewardManager.ItemsLootTable, true);
                }
                else
                {
                    itemToSpawn = LootEngine.GetItemOfTypeAndQuality<CompanionItem>(PickupObject.ItemQuality.A, GameManager.Instance.RewardManager.ItemsLootTable, true);
                }
                yield return new WaitForSeconds(1f);
                LootEngine.SpawnItem(itemToSpawn.gameObject, positionToSpawn, Vector2.zero, 1f, false, true, true);
                if (hasSynergyGregSalad)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        LootEngine.SpawnItem(PickupObjectDatabase.GetById(BraveUtility.RandomElement(lootIDlist)).gameObject, positionToSpawn, Vector2.zero, 1f, false, true, false);
                    }
                }
                for (int i = 0; i < playerOwner.passiveItems.Count; i++)
                {
                    if (playerOwner.passiveItems[i] == itemInPlayerInventory)
                    {
                        playerOwner.RemovePassiveItemAtIndex(i);
                        // if (isGoodMimic) playerOwner.AcquirePassiveItemPrefabDirectly(PickupObjectDatabase.GetById(664) as PassiveItem);
                    }
                }
            }
            public void OnRoomClear(PlayerController player)
            {
                try
                {
                    if (base.aiActor && base.aiActor.healthHaver && base.aiActor.healthHaver.IsAlive)
                    {
                        if (UnityEngine.Random.value < 0.15f)
                        {
                            if (this.aiAnimator)
                            {
                                this.aiAnimator.PlayUntilFinished("spawnloot", false, null, -1f, false);
                            }
                            Invoke("SpawnMundaneLoot", 0.9f);
                        }
                    }
                }
                catch (Exception e)
                {
                    ETGModConsole.Log(e.Message);
                    ETGModConsole.Log(e.StackTrace);
                }
            }
            private void SpawnMundaneLoot()
            {
                if (base.aiActor && base.aiActor.healthHaver && base.aiActor.healthHaver.IsAlive)
                {
                    int lootID = BraveUtility.RandomElement(lootIDlist);
                    LootEngine.SpawnItem(PickupObjectDatabase.GetById(lootID).gameObject, base.aiActor.sprite.WorldCenter, Vector2.zero, 1f, false, true, false);
                }
            }

            public PlayerController Owner;
            public GregTheEgg ConnectedItem;
            public CompanionItem ConnectedItemIfGoodMimic;
            private CustomHologramDoer gregHologram;
        }
        public static List<int> lootIDlist = new List<int>()
        {
            78, //Ammo
            600, //Spread Ammo
            565, //Glass Guon Stone
            73, //Half Heart
            85, //Heart
            120, //Armor
            224, //Blank
            67, //Key
        };
        public static GameObject prefab;
        private static readonly string guid = "greg_the_egg9327892378594676";
    }
}