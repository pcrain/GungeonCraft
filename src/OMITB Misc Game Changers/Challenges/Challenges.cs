using System;
using System.Collections;
using NpcApi;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using GungeonAPI;
using SaveAPI;
using Alexandria.Misc;
using Alexandria.ItemAPI;
using Alexandria.EnemyAPI;

namespace NevernamedsItems
{
    class Challenges
    {
        public static void Init()
        {
            ChallengeUnlocks.Init();


            //Generic stuff for when you type in nnchallenges
            ETGMod.Databases.Strings.Core.Set("#NNCHALLENGES_TITLE", "List of Custom Challenges");
            ETGMod.Databases.Strings.Core.Set("#NNCHALLENGES_DESCRIPTION", "Custom challenges present in Once More Into The Breach.");
            ETGMod.Databases.Strings.Core.Set("#NNCHALLENGES_TECHEXPLANATION", "Type 'nnchallenges [challengeid]' to start the challenge (can only be done from the Breach).");
            ETGMod.Databases.Strings.Core.Set("#NNCHALLENGES_DISABLEEXPLANATION", "Challenges will be automatically disabled if Blessed Mode or Rainbow Mode are enabled, if a shortcut is taken from the Breach, or if the player is the Gunslinger or Paradox.");

            //Console Messages After Enabling Challenges
            ETGMod.Databases.Strings.Core.Set("#NNCHALLENGES_NONBREACHDENIAL", "Challenges can only be activated or deactivated from the Breach.");
            ETGMod.Databases.Strings.Core.Set("#NNCHALLENGES_RAINBOWDENIAL", "Challenges cannot be played in Rainbow Mode.");
            ETGMod.Databases.Strings.Core.Set("#NNCHALLENGES_BLESSEDDENIAL", "Challenges cannot be played in Blessed Mode.");
            ETGMod.Databases.Strings.Core.Set("#NNCHALLENGES_SLINGERDENIAL", "Challenges cannot be played as the Gunslinger.");
            ETGMod.Databases.Strings.Core.Set("#NNCHALLENGES_PARADOXDENIAL", "Challenges cannot be played as the Paradox.");
            ETGMod.Databases.Strings.Core.Set("#NNCHALLENGES_NOCHARDENIAL", "Please select a character before enabling the challenge.");
            ETGMod.Databases.Strings.Core.Set("#NNCHALLENGES_NOWPLAYING", "You are now playing");

            //Challenge Names
            ETGMod.Databases.Strings.Core.Set("#CHAL_TOIL_NAME", "Toil and Trouble");
            ETGMod.Databases.Strings.Core.Set("#CHAL_WHATARMY_NAME", "What Army?");
            ETGMod.Databases.Strings.Core.Set("#CHAL_INVIS_NAME", "Invisible-O");
            ETGMod.Databases.Strings.Core.Set("#CHAL_COOL_NAME", "Keep It Cool");

            //Challenge Descriptions
            ETGMod.Databases.Strings.Core.Set("#CHAL_TOIL_DESC", "All enemy spawns are doubled. Non-doubleable bosses gain a health boost.");
            ETGMod.Databases.Strings.Core.Set("#CHAL_WHATARMY_DESC", "All enemy spawns are randomised.");
            ETGMod.Databases.Strings.Core.Set("#CHAL_INVIS_DESC", "The player, their gun, and their bullets are all completely invisible!");
            ETGMod.Databases.Strings.Core.Set("#CHAL_COOL_DESC", "Permanent ice physics. 45% chance to fire freezing shots.");

            CurrentChallenge = ChallengeType.NONE;

            ETGModConsole.Commands.AddGroup("nnchallenges", delegate (string[] args)
            {
                ETGModConsole.Log($"<size=100><color=#ff0000ff>{StringTableManager.GetString("NNCHALLENGES_TITLE")}</color></size>", false);
                ETGModConsole.Log(StringTableManager.GetString("#NNCHALLENGES_DESCRIPTION"), false);
                ETGModConsole.Log(StringTableManager.GetString("#NNCHALLENGES_TECHEXPLANATION"), false);

                ETGModConsole.Log($"{StringTableManager.GetString("#CHAL_TOIL_NAME")}:  [id]<color=#ff0000ff>toilandtrouble</color> - {StringTableManager.GetString("#CHAL_TOIL_DESC")}", false);
                ETGModConsole.Log($"{StringTableManager.GetString("#CHAL_WHATARMY_NAME")}: [id]<color=#ff0000ff>whatarmy</color> - {StringTableManager.GetString("#CHAL_WHATARMY_DESC")}", false);
                ETGModConsole.Log($"{StringTableManager.GetString("#CHAL_INVIS_NAME")}:  [id]<color=#ff0000ff>invisibleo</color> - {StringTableManager.GetString("#CHAL_INVIS_DESC")}", false);
                ETGModConsole.Log($"{StringTableManager.GetString("#CHAL_COOL_NAME")}:  [id]<color=#ff0000ff>keepitcool</color> - {StringTableManager.GetString("#CHAL_COOL_DESC")}", false);
                
                ETGModConsole.Log(StringTableManager.GetString("#NNCHALLENGES_DISABLEEXPLANATION"), false);
            });

            ETGModConsole.Commands.GetGroup("nnchallenges").AddUnit("clear", delegate (string[] args)
            {
                if (GameManager.Instance.IsFoyer)
                {
                    ETGModConsole.Log("Challenge Removed");
                    CurrentChallenge = ChallengeType.NONE;
                }
                else
                {
                    ETGModConsole.Log($"<color=#ff0000ff>{StringTableManager.GetString("#NNCHALLENGES_NONBREACHDENIAL")}</color>", false);
                }
            });
            ETGModConsole.Commands.GetGroup("nnchallenges").AddUnit("whatarmy", delegate (string[] args)
            {
                string failure = FetchFailureType();
                if (failure == "none")
                {
                    ETGModConsole.Log($"{StringTableManager.GetString("#NNCHALLENGES_NOWPLAYING")}; {StringTableManager.GetString("#CHAL_WHATARMY_NAME")}");
                    CurrentChallenge = ChallengeType.WHAT_ARMY;
                }
                else
                {
                    ETGModConsole.Log($"<color=#ff0000ff>{failure}</color>");
                }
            });
            ETGModConsole.Commands.GetGroup("nnchallenges").AddUnit("toilandtrouble", delegate (string[] args)
            {
                string failure = FetchFailureType();
                if (failure == "none")
                {
                    ETGModConsole.Log($"{StringTableManager.GetString("#NNCHALLENGES_NOWPLAYING")}; {StringTableManager.GetString("#CHAL_TOIL_NAME")}");
                    CurrentChallenge = ChallengeType.TOIL_AND_TROUBLE;
                }
                else
                {
                    ETGModConsole.Log($"<color=#ff0000ff>{failure}</color>");
                }
            });
            ETGModConsole.Commands.GetGroup("nnchallenges").AddUnit("invisibleo", delegate (string[] args)
            {
                string failure = FetchFailureType();
                if (failure == "none")
                {
                    ETGModConsole.Log($"{StringTableManager.GetString("#NNCHALLENGES_NOWPLAYING")}; {StringTableManager.GetString("#CHAL_INVIS_NAME")}");
                    CurrentChallenge = ChallengeType.INVISIBLEO;
                }
                else
                {
                    ETGModConsole.Log($"<color=#ff0000ff>{failure}</color>");
                }
            });
            ETGModConsole.Commands.GetGroup("nnchallenges").AddUnit("keepitcool", delegate (string[] args)
            {
                string failure = FetchFailureType();
                if (failure == "none")
                {
                    ETGModConsole.Log($"{StringTableManager.GetString("#NNCHALLENGES_NOWPLAYING")}; {StringTableManager.GetString("#CHAL_COOL_NAME")}");
                    CurrentChallenge = ChallengeType.KEEP_IT_COOL;
                }
                else
                {
                    ETGModConsole.Log($"<color=#ff0000ff>{failure}</color>");
                }
            });

            ETGMod.AIActor.OnPostStart += Challenges.AIActorPostSpawn;
        }
        public static ChallengeType CurrentChallenge;
        public static void AIActorPostSpawn(AIActor AIActor)
        {
            if (CurrentChallenge == ChallengeType.WHAT_ARMY)
            {
                bool isParachuting = (AIActor.gameObject.transform.parent != null && AIActor.gameObject.transform.parent.gameObject.name.Contains("EX_Parachute"));
                if (AIActor && AIActor.healthHaver && !AIActor.healthHaver.IsBoss && !AIActor.healthHaver.IsSubboss && !AIActor.IsSecretlyTheMineFlayer())
                {
                    if (AIActor.gameObject.GetComponent<HasBeenAffectedByCurrentChallenge>() == null && AIActor.gameObject.GetComponent<CompanionController>() == null && !isParachuting)
                    {
                        float proc = 1;
                        if (AIActor.GetAbsoluteParentRoom().area.PrototypeRoomCategory == PrototypeDungeonRoom.RoomCategory.BOSS && AIActor.GetAbsoluteParentRoom().RoomContainsMineFlayer()) proc = 0.2f;
                        if (UnityEngine.Random.value <= proc)
                        {
                            List<string> ChaosPalette = MagickeCauldron.GenerateChaosPalette();
                            string guid = BraveUtility.RandomElement(ChaosPalette);
                            var enemyPrefab = EnemyDatabase.GetOrLoadByGuid(guid);
                            AIActor aiactor = AIActor.Spawn(enemyPrefab, AIActor.gameActor.CenterPosition.ToIntVector2(VectorConversions.Floor), AIActor.GetAbsoluteParentRoom(), true, AIActor.AwakenAnimationType.Default, true);
                            aiactor.gameObject.AddComponent<HasBeenAffectedByCurrentChallenge>();
                            aiactor.AssignedCurrencyToDrop = AIActor.AssignedCurrencyToDrop;
                            aiactor.AdditionalSafeItemDrops = AIActor.AdditionalSafeItemDrops;
                            aiactor.AdditionalSimpleItemDrops = AIActor.AdditionalSimpleItemDrops;
                            aiactor.CanTargetEnemies = AIActor.CanTargetEnemies;
                            aiactor.CanTargetPlayers = AIActor.CanTargetPlayers;
                            if (aiactor.EnemyGuid == "556e9f2a10f9411cb9dbfd61e0e0f1e1") aiactor.HandleReinforcementFallIntoRoom(0f);
                            else if (AIActor.IsInReinforcementLayer)
                            {                         
                                aiactor.invisibleUntilAwaken = true;
                                aiactor.specRigidbody.CollideWithOthers = false;
                                aiactor.IsGone = true;
                                aiactor.HandleReinforcementFallIntoRoom(0f);
                            }
                            if (aiactor.EnemyGuid == "22fc2c2c45fb47cf9fb5f7b043a70122") aiactor.CollisionDamage = 0f;
                            if (AIActor.GetComponent<ExplodeOnDeath>() != null) UnityEngine.Object.Destroy(AIActor.GetComponent<ExplodeOnDeath>());
                            AIActor.EraseFromExistence(true);
                        }
                    }
                }
            }
            else if (CurrentChallenge == ChallengeType.TOIL_AND_TROUBLE)
            {
                if (AIActor && AIActor.healthHaver && !AIActor.healthHaver.IsBoss && !AIActor.healthHaver.IsSubboss && !AIActor.IsSecretlyTheMineFlayer())
                {
                    if (AIActor.GetComponent<CompanionController>() == null && AIActor.GetComponent<HasBeenAffectedByCurrentChallenge>() == null && AIActor.GetComponent<DisplacedImageController>() == null && AIActor.GetComponent<WitchsBrew.HasBeenDoubledByWitchsBrew>() == null && AIActor.GetComponent< MirrorImageController>() == null)
                    {
                        GameManager.Instance.StartCoroutine(ToilEnemyDupe(AIActor));
                    }
                }
                else if (AIActor && AIActor.healthHaver && (AIActor.healthHaver.IsBoss || AIActor.healthHaver.IsSubboss) && !AIActor.IsSecretlyTheMineFlayer())
                {
                    if (AIActor.GetComponent<HasBeenAffectedByCurrentChallenge>() == null)
                    {
                        if (ValidDoubleableBosses.Contains(AIActor.EnemyGuid))
                        {
                            string guid = AIActor.EnemyGuid;
                            var enemyPrefab = EnemyDatabase.GetOrLoadByGuid(guid);
                            AIActor aiactor = AIActor.Spawn(enemyPrefab, AIActor.gameActor.CenterPosition.ToIntVector2(VectorConversions.Floor), AIActor.GetAbsoluteParentRoom(), true, AIActor.AwakenAnimationType.Default, true);

                            HasBeenAffectedByCurrentChallenge challengitude = aiactor.gameObject.AddComponent<HasBeenAffectedByCurrentChallenge>();
                            challengitude.linkedOther = AIActor;
                            HasBeenAffectedByCurrentChallenge challengitude2 = AIActor.gameObject.AddComponent<HasBeenAffectedByCurrentChallenge>();
                            challengitude2.linkedOther = aiactor;
                            aiactor.AssignedCurrencyToDrop = AIActor.AssignedCurrencyToDrop;
                            aiactor.AdditionalSafeItemDrops = AIActor.AdditionalSafeItemDrops;
                            aiactor.AdditionalSimpleItemDrops = AIActor.AdditionalSimpleItemDrops;

                            if (AIActor.GetComponent<BroController>())
                            {
                                aiactor.gameObject.GetOrAddComponent<BroController>();
                            }


                            float actorOrigHP = AIActor.healthHaver.GetMaxHealth();
                            float actorNewHP = aiactor.healthHaver.GetMaxHealth();
                            AIActor.healthHaver.SetHealthMaximum(actorOrigHP * 0.5f);
                            AIActor.healthHaver.ForceSetCurrentHealth(actorOrigHP * 0.5f);
                            aiactor.healthHaver.SetHealthMaximum(actorNewHP * 0.5f);
                            aiactor.healthHaver.ForceSetCurrentHealth(actorNewHP * 0.5f);
                        }
                        else
                        {
                            float actorHP = AIActor.healthHaver.GetMaxHealth();
                            AIActor.healthHaver.SetHealthMaximum(actorHP * 1.5f);
                            AIActor.healthHaver.ForceSetCurrentHealth(actorHP * 1.5f);
                        }
                    }
                }
            }
        }
        private static IEnumerator ToilEnemyDupe(AIActor AIActor)
        {
            yield return null;

            string guid = AIActor.EnemyGuid;
            var enemyPrefab = EnemyDatabase.GetOrLoadByGuid(guid);
            AIActor aiactor = AIActor.Spawn(enemyPrefab, AIActor.gameActor.CenterPosition.ToIntVector2(VectorConversions.Floor), AIActor.GetAbsoluteParentRoom(), true, AIActor.AwakenAnimationType.Default, true);

            HasBeenAffectedByCurrentChallenge challengitude = aiactor.gameObject.AddComponent<HasBeenAffectedByCurrentChallenge>();
            challengitude.linkedOther = AIActor;
            HasBeenAffectedByCurrentChallenge challengitude2 = AIActor.gameObject.AddComponent<HasBeenAffectedByCurrentChallenge>();
            challengitude2.linkedOther = aiactor;
            aiactor.procedurallyOutlined = true;
            AIActor.procedurallyOutlined = true;
            aiactor.IsWorthShootingAt = AIActor.IsWorthShootingAt;
            aiactor.IgnoreForRoomClear = AIActor.IgnoreForRoomClear;
            aiactor.AssignedCurrencyToDrop = AIActor.AssignedCurrencyToDrop;
            aiactor.AdditionalSafeItemDrops = AIActor.AdditionalSafeItemDrops;
            aiactor.AdditionalSimpleItemDrops = AIActor.AdditionalSimpleItemDrops;
            aiactor.CanTargetEnemies = AIActor.CanTargetEnemies;
            aiactor.CanTargetPlayers = AIActor.CanTargetPlayers;
            if (AIActor.IsInReinforcementLayer) aiactor.HandleReinforcementFallIntoRoom(0f);
            if (AIActor.GetComponent<KillOnRoomClear>() != null)
            {
                KillOnRoomClear kill = aiactor.gameObject.GetOrAddComponent<KillOnRoomClear>();
                kill.overrideDeathAnim = AIActor.GetComponent<KillOnRoomClear>().overrideDeathAnim;
                kill.preventExplodeOnDeath = AIActor.GetComponent<KillOnRoomClear>().preventExplodeOnDeath;
            }
            if (aiactor.EnemyGuid == "22fc2c2c45fb47cf9fb5f7b043a70122")
            {
                aiactor.CollisionDamage = 0f;
                aiactor.specRigidbody.AddCollisionLayerIgnoreOverride(CollisionMask.LayerToMask(CollisionLayer.PlayerHitBox));
                aiactor.specRigidbody.AddCollisionLayerIgnoreOverride(CollisionMask.LayerToMask(CollisionLayer.Projectile));
            }
            else if (aiactor.EnemyGuid == "249db525a9464e5282d02162c88e0357")
            {
                if (aiactor.gameObject.GetComponent<SpawnEnemyOnDeath>())
                {
                    UnityEngine.Object.Destroy(aiactor.gameObject.GetComponent<SpawnEnemyOnDeath>());
                }
            }
            else if (aiactor.HasTag("mimic"))
            {
                if (AIActor.AdditionalSafeItemDrops != null && aiactor.AdditionalSafeItemDrops != null)
                {
                    List<PickupObject> newDrops = new List<PickupObject>();
                    PickupObject.ItemQuality qual = PickupObject.ItemQuality.D;
                    int itemsToReAdd = 0;
                    for (int i = (aiactor.AdditionalSafeItemDrops.Count - 1); i >= 0; i--)
                    {
                        if (!BabyGoodChanceKin.lootIDlist.Contains(aiactor.AdditionalSafeItemDrops[i].PickupObjectId))
                        {
                            qual = aiactor.AdditionalSafeItemDrops[i].quality;
                            itemsToReAdd++;
                        }
                        else
                        {
                            newDrops.Add(PickupObjectDatabase.GetById(aiactor.AdditionalSafeItemDrops[i].PickupObjectId));
                        }
                    }
                    if (itemsToReAdd > 0)
                    {
                        for (int i = 0; i < itemsToReAdd; i++)
                        {
                            PickupObject item = LootEngine.GetItemOfTypeAndQuality<PassiveItem>(qual, null, false);
                            if (UnityEngine.Random.value <= 0.5f) item = LootEngine.GetItemOfTypeAndQuality<Gun>(qual, null, false);
                            newDrops.Add(item);
                        }
                        aiactor.AdditionalSafeItemDrops = newDrops;
                    }
                }
            }


            GameManager.Instance.StartCoroutine(Shrimk(aiactor));
            GameManager.Instance.StartCoroutine(Shrimk(AIActor));

            aiactor.specRigidbody.Reinitialize();
            yield break;
        }
        private static IEnumerator Shrimk(AIActor actor)
        {
            while (!actor.HasBeenEngaged || !actor.HasBeenAwoken) { yield return null; }
            int cachedLayer = actor.gameObject.layer;
            actor.gameObject.layer = LayerMask.NameToLayer("Unpixelated");
            int cachedOutlineLayer = SpriteOutlineManager.ChangeOutlineLayer(actor.sprite, LayerMask.NameToLayer("Unpixelated"));
            actor.EnemyScale = TargetScale;
            actor.gameObject.layer = cachedLayer;
            SpriteOutlineManager.ChangeOutlineLayer(actor.sprite, cachedOutlineLayer);
            yield break;
        }
        public static Vector2 TargetScale = new Vector2(0.75f, 0.75f);
        public static void OnLevelLoaded()
        {
            if (CurrentChallenge != ChallengeType.NONE)
            {
                //ETGModConsole.Log("Challenge was not null!");
                PlayerController player1 = GameManager.Instance.PrimaryPlayer;
                if (player1 == null) ETGModConsole.Log("ERRA PLAYA NULLA");
                PlayerController player2 = null;
                if (GameManager.Instance.SecondaryPlayer != null) player2 = GameManager.Instance.SecondaryPlayer;
                if (GameStatsManager.Instance.IsRainbowRun)
                {
                    ETGModConsole.Log("<color=#ff0000ff>Challenge Voided: Played Rainbow Run</color>", false);
                    TextBubble.DoAmbientTalk(player1.transform, new Vector3(1, 2, 0), "Challenge Voided: Played Rainbow Run", 4f);
                    CurrentChallenge = ChallengeType.NONE;
                }
                else if (player1.CharacterUsesRandomGuns || (player2 && player2.CharacterUsesRandomGuns))
                {
                    ETGModConsole.Log("<color=#ff0000ff>Challenge Voided: Played Blessed Run</color>", false);
                    CurrentChallenge = ChallengeType.NONE;
                    TextBubble.DoAmbientTalk(player1.transform, new Vector3(1, 2, 0), "Challenge Voided: Played Blessed Run", 4f);
                }
                else if (player1.characterIdentity == PlayableCharacters.Gunslinger || (player2 && player2.characterIdentity == PlayableCharacters.Gunslinger))
                {
                    ETGModConsole.Log("<color=#ff0000ff>Challenge Voided: Played as Gunslinger</color>", false);
                    CurrentChallenge = ChallengeType.NONE;
                    TextBubble.DoAmbientTalk(player1.transform, new Vector3(1, 2, 0), "Challenge Voided: Played as Gunslinger", 4f);
                }
                else if (player1.characterIdentity == PlayableCharacters.Eevee || (player2 && player2.characterIdentity == PlayableCharacters.Eevee))
                {
                    ETGModConsole.Log("<color=#ff0000ff>Challenge Voided: Played as Paradox</color>", false);
                    CurrentChallenge = ChallengeType.NONE;
                    TextBubble.DoAmbientTalk(player1.transform, new Vector3(1, 2, 0), "Challenge Voided: Played as Paradox", 4f);
                }
                else if (GameManager.Instance.CurrentGameMode == GameManager.GameMode.SHORTCUT)
                {
                    ETGModConsole.Log("<color=#ff0000ff>Challenge Voided: Took a Shortcut</color>", false);
                    CurrentChallenge = ChallengeType.NONE;
                    TextBubble.DoAmbientTalk(player1.transform, new Vector3(1, 2, 0), "Challenge Voided: Took a Shortcut", 4f);
                }
                else if (GameManager.Instance.CurrentGameMode == GameManager.GameMode.BOSSRUSH)
                {
                    ETGModConsole.Log("<color=#ff0000ff>Challenge Voided: Entered Bossrush</color>", false);
                    CurrentChallenge = ChallengeType.NONE;
                    TextBubble.DoAmbientTalk(player1.transform, new Vector3(1, 2, 0), "Challenge Voided: Entered Bossrush", 4f);
                }
                else if (GameManager.Instance.InTutorial)
                {
                    ETGModConsole.Log("<color=#ff0000ff>Challenge Voided: Entered Tutorial</color>", false);
                    CurrentChallenge = ChallengeType.NONE;
                    TextBubble.DoAmbientTalk(player1.transform, new Vector3(1, 2, 0), "Challenge Voided: Entered Tutorial", 4f);
                }
            }
        }
    
        private static string FetchFailureType()
        {
            string failureMessage = "none";
            bool skipChecks = false;
            PlayerController player1 = GameManager.Instance.PrimaryPlayer;
            if (player1 == null)
            {
                Debug.LogWarning("Attempted to set a challenge without a valid player, caught it though.");
                failureMessage = StringTableManager.GetString("#NNCHALLENGES_NOCHARDENIAL");
                skipChecks = true;
            }
            PlayerController player2 = null;
            if (GameManager.Instance.SecondaryPlayer != null) player2 = GameManager.Instance.SecondaryPlayer;

            if (!skipChecks)
            {
                if (!GameManager.Instance.IsFoyer) failureMessage = StringTableManager.GetString("#NNCHALLENGES_NONBREACHDENIAL");
                else if (GameStatsManager.Instance.IsRainbowRun) failureMessage = StringTableManager.GetString("#NNCHALLENGES_RAINBOWDENIAL");
                else if (player1.CharacterUsesRandomGuns || (player2 && player2.CharacterUsesRandomGuns))
                {
                    failureMessage = StringTableManager.GetString("#NNCHALLENGES_BLESSEDDENIAL");
                }
                else if (player1.characterIdentity == PlayableCharacters.Gunslinger || (player2 && player2.characterIdentity == PlayableCharacters.Gunslinger))
                {
                    failureMessage = StringTableManager.GetString("#NNCHALLENGES_SLINGERDENIAL");
                }
                else if (player1.characterIdentity == PlayableCharacters.Eevee || (player2 && player2.characterIdentity == PlayableCharacters.Eevee))
                {
                    StringTableManager.GetString("#NNCHALLENGES_PARADOXDENIAL");
                }
            }
            return failureMessage;
        }
        public class HasBeenAffectedByCurrentChallenge : MonoBehaviour
        {
            public HasBeenAffectedByCurrentChallenge()
            {
                linkedOther = null;
            }
            private void Start()
            {
                self = base.GetComponent<AIActor>();
                if (self && self.specRigidbody && CurrentChallenge == ChallengeType.TOIL_AND_TROUBLE)
                {
                    self.specRigidbody.OnPreRigidbodyCollision += this.OnPreCollide;
                }
            }
            private void OnPreCollide(SpeculativeRigidbody myRigidbody, PixelCollider myCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherCollider)
            {
                if (CurrentChallenge == ChallengeType.TOIL_AND_TROUBLE && linkedOther != null & linkedOther.specRigidbody != null)
                {
                    if (otherRigidbody == linkedOther.specRigidbody)
                    {
                        PhysicsEngine.SkipCollision = true;
                    }
                }
            }
            private AIActor self;
            public AIActor linkedOther;
        }
        private static List<string> ValidDoubleableBosses = new List<string>()
        {
            EnemyGuidDatabase.Entries["blockner_rematch"],
            EnemyGuidDatabase.Entries["fuselier"],
            EnemyGuidDatabase.Entries["shadow_agunim"],
            EnemyGuidDatabase.Entries["gatling_gull"],
            EnemyGuidDatabase.Entries["bullet_king"],
            EnemyGuidDatabase.Entries["blobulord"],
            EnemyGuidDatabase.Entries["beholster"],
            EnemyGuidDatabase.Entries["gorgun"],
            EnemyGuidDatabase.Entries["ammoconda"],
            EnemyGuidDatabase.Entries["old_king"],
            EnemyGuidDatabase.Entries["treadnaught"],
            EnemyGuidDatabase.Entries["mine_flayer"],
            //EnemyGuidDatabase.Entries["cannonbalrog"],
            EnemyGuidDatabase.Entries["door_lord"],
            EnemyGuidDatabase.Entries["helicopter_agunim"],
            "6c43fddfd401456c916089fdd1c99b1c", //High Priest
            "ea40fcc863d34b0088f490f4e57f8913", //Smiley
        "c00390483f394a849c36143eb878998f", //shades
        };
    }

    public enum ChallengeType
    {
        NONE,
        TOIL_AND_TROUBLE,
        WHAT_ARMY,
        INVISIBLEO,
        KEEP_IT_COOL,
    }
}
