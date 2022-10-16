using System;
using System.Collections;
using System.Collections.Generic;
using Dungeonator;
using Gungeon;
using ItemAPI;
using SaveAPI;
using UnityEngine;
using Alexandria.Misc;

namespace NevernamedsItems
{
    public class ScrollOfExactKnowledge : CompanionItem
    {
        public static void Init()
        {
            string name = "Scroll of Exact Knowledge";
            string resourcePath = "NevernamedsItems/Resources/Companions/ScrollOfExactKnowledge/scrollofexactknowledge_icon";
            GameObject gameObject = new GameObject();
            CompanionItem companionItem = gameObject.AddComponent<ScrollOfExactKnowledge>();
            ItemBuilder.AddSpriteToObject(name, resourcePath, gameObject);
            string shortDesc = "Nerd";
            string longDesc = "Offers useful information on the Gungeon around you."+"\n\nSeems to have a fear of being alone, and enjoys company.";
            companionItem.SetupItem(shortDesc, longDesc, "nn");
            companionItem.quality = PickupObject.ItemQuality.C;
            companionItem.CompanionGuid = ScrollOfExactKnowledge.guid;
            ScrollOfExactKnowledge.BuildPrefab();
            ScrollOfExactKnowledgeID = companionItem.PickupObjectId;
            companionItem.SetupUnlockOnCustomFlag(CustomDungeonFlags.ALLJAMMED_BEATEN_OFFICE, true);

            CustomActions.OnChestPostSpawn += ScrollOfExactKnowledge.OnChestSpawned;
            CustomActions.OnRewardPedestalSpawned += OnPedestalSpawned;
        }
        public static int ScrollOfExactKnowledgeID;

        public static void OnPedestalSpawned(RewardPedestal target) 
        {
            if (GameManager.Instance.AnyPlayerHasPickupID(ScrollOfExactKnowledge.ScrollOfExactKnowledgeID))
            {
                if (GameManager.Instance.PrimaryPlayer != null)
                {
                    foreach (PassiveItem item in GameManager.Instance.PrimaryPlayer.passiveItems)
                    {
                        if (item.GetComponent<ScrollOfExactKnowledge>() != null)
                        {
                            item.GetComponent<ScrollOfExactKnowledge>().ReactToSpawnedPedestal(target);
                        }
                    }
                }
                if (GameManager.Instance.SecondaryPlayer != null)
                {
                    foreach (PassiveItem item in GameManager.Instance.SecondaryPlayer.passiveItems)
                    {
                        if (item.GetComponent<ScrollOfExactKnowledge>() != null)
                        {
                            item.GetComponent<ScrollOfExactKnowledge>().ReactToSpawnedPedestal(target);
                        }
                    }
                }
            }
        }
        public static void OnChestSpawned(Chest chest)
        {
            if (!Dungeon.IsGenerating)
            {
                if (GameManager.Instance.AnyPlayerHasPickupID(ScrollOfExactKnowledge.ScrollOfExactKnowledgeID))
                {
                    if (GameManager.Instance.PrimaryPlayer != null)
                    {
                        foreach (PassiveItem item in GameManager.Instance.PrimaryPlayer.passiveItems)
                        {
                            if (item.GetComponent<ScrollOfExactKnowledge>() != null)
                            {
                                item.GetComponent<ScrollOfExactKnowledge>().ReactToRuntimeSpawnedChest(chest);
                            }
                        }
                    }
                    if (GameManager.Instance.SecondaryPlayer != null)
                    {
                        foreach (PassiveItem item in GameManager.Instance.SecondaryPlayer.passiveItems)
                        {
                            if (item.GetComponent<ScrollOfExactKnowledge>() != null)
                            {
                                item.GetComponent<ScrollOfExactKnowledge>().ReactToRuntimeSpawnedChest(chest);
                            }
                        }
                    }
                }
            }
        }
        public override DebrisObject Drop(PlayerController player)
        {
            DebrisObject droppedSelf = base.Drop(player);
            TextBubble.DoAmbientTalk(droppedSelf.transform, new Vector3(0.5f, 2, 0), BraveUtility.RandomElement(DropDialogue), 2f);
            return droppedSelf;
        }
        private List<string> DropDialogue = new List<string>()
        {
            "No! Wait!",
            "Please reconsider!",
            "What did I do wrong?",
            "Have I really been that much of a burden?",
            "Not again...",
            "I'll stay here, I guess... guard the... floor.",
            "Abandoned again..."
        };
        public void ReactToRuntimeSpawnedChest(Chest chest)
        {
            if (this && this.Owner && this.ExtantCompanion)
            {
                if (this.ExtantCompanion.GetComponent<ScrollOfExactKnowledgeBehav>() != null)
                {
                    this.ExtantCompanion.GetComponent<ScrollOfExactKnowledgeBehav>().ReactToRuntimeSpawnedChest(chest);
                }
            }
        }
        public void ReactToSpawnedPedestal(RewardPedestal pedestal)
        {
            if (this && this.Owner && this.ExtantCompanion)
            {
                //ETGModConsole.Log("Reaction ran in item");

                if (this.ExtantCompanion.GetComponent<ScrollOfExactKnowledgeBehav>() != null)
                {
                    this.ExtantCompanion.GetComponent<ScrollOfExactKnowledgeBehav>().ReactToSpawnedPedestal(pedestal);
                }
            }
        }
        public static void BuildPrefab()
        {
            bool flag = ScrollOfExactKnowledge.prefab != null || CompanionBuilder.companionDictionary.ContainsKey(ScrollOfExactKnowledge.guid);
            if (!flag)
            {
                ScrollOfExactKnowledge.prefab = CompanionBuilder.BuildPrefab("Scroll of Exact Knowledge", ScrollOfExactKnowledge.guid, "NevernamedsItems/Resources/Companions/ScrollOfExactKnowledge/scrollofexactknowledge_idleright_001", new IntVector2(5, 4), new IntVector2(11, 15));
                var companionController = ScrollOfExactKnowledge.prefab.AddComponent<ScrollOfExactKnowledgeBehav>();
                companionController.aiActor.MovementSpeed = 6f;
                companionController.CanCrossPits = true;
                companionController.aiActor.ActorShadowOffset = new Vector3(0, -0.5f);
                ScrollOfExactKnowledge.prefab.AddAnimation("idle_right", "NevernamedsItems/Resources/Companions/ScrollOfExactKnowledge/scrollofexactknowledge_idleright", 7, CompanionBuilder.AnimationType.Idle, DirectionalAnimation.DirectionType.TwoWayHorizontal, DirectionalAnimation.FlipType.None);
                ScrollOfExactKnowledge.prefab.AddAnimation("idle_left", "NevernamedsItems/Resources/Companions/ScrollOfExactKnowledge/scrollofexactknowledge_idleleft", 7, CompanionBuilder.AnimationType.Idle, DirectionalAnimation.DirectionType.TwoWayHorizontal, DirectionalAnimation.FlipType.None);

                BehaviorSpeculator component = ScrollOfExactKnowledge.prefab.GetComponent<BehaviorSpeculator>();
                component.MovementBehaviors.Add(new CompanionFollowPlayerBehavior
                {
                    IdleAnimations = new string[]
                    {
                        "idle"
                    }
                });
            }
        }
        public class ScrollOfExactKnowledgeBehav : CompanionController
        {
            private void Start()
            {
                this.Owner = this.m_owner;
                self = this.aiActor;
                Invoke("DoWelcomingDialogue", 0.5f);
            }
            
            private void DoWelcomingDialogue()
            {
                TextBubble.DoAmbientTalk(base.transform, new Vector3(0.5f, 2, 0), BraveUtility.RandomElement(WelcomingDialogues), 1.5f);
            }
            private List<string> WelcomingDialogues = new List<string>()
            {
                "Ahh, it's good to be back!",
                "Back in the saddle!",
                "Ready and rearing to go!",
                "Let's do this, you and me!",
                "I'll do what I can to help!",
                "Thank goodness, I was getting so lonely.",
            };
            public override void Update()
            {
                if (self && Owner && !Dungeon.IsGenerating)
                {
                    if (Owner.CurrentRoom != null && Owner.CurrentRoom != lastCheckedRoom)
                    {
                        OnRoomChanged(Owner.CurrentRoom);
                        //ETGModConsole.Log(Owner.CurrentRoom.GetRoomName());
                        lastCheckedRoom = Owner.CurrentRoom;
                    }
                    if (Owner.PlayerHasActiveSynergy("Restoration") != hadRestorationLastChecked)
                    {
                        HandleSynergyDialogue(Owner.PlayerHasActiveSynergy("Restoration"));
                        hadRestorationLastChecked = Owner.PlayerHasActiveSynergy("Restoration");
                    }
                }
            }
            private bool hadRestorationLastChecked;
            private void OnRoomChanged(RoomHandler room)
            {
                //ETGModConsole.Log("OnRoomChanged Ran");
                List<string> dialoguesToSay = new List<string>();
                int previousStatements = 0;
              
                Chest[] allChests = FindObjectsOfType<Chest>();
                List<Chest> chestsInRoom = new List<Chest>();
                foreach (Chest chest in allChests)
                {
                    if (chest.transform.position.GetAbsoluteRoom() == Owner.CurrentRoom && !chest.IsOpen && !chest.IsBroken)
                    {
                        //ETGModConsole.Log("Found a chest in the room");

                        chestsInRoom.Add(chest);
                    }
                }
                if (chestsInRoom.Count > 0)
                {
                    foreach (Chest possibleMimic in chestsInRoom)
                    {
                        if (possibleMimic.IsMimic)
                        {
                            if (chestsInRoom.Count > 1) dialoguesToSay.Add("One of these chests is a mimic... be careful!");
                            else dialoguesToSay.Add("Be careful, that chest's a mimic!");
                            previousStatements++;
                        }
                    }
                    foreach (Chest possibleSecretRainbow in chestsInRoom)
                    {
                        if (possibleSecretRainbow.ChestIdentifier == Chest.SpecialChestIdentifier.SECRET_RAINBOW)
                        {
                            if (chestsInRoom.Count > 1) dialoguesToSay.Add("That Chest is a Secret Rainbow Chest!");
                            else if (previousStatements > 0) dialoguesToSay.Add("One of these Chests is also secretly a Rainbow Chest!");
                            else dialoguesToSay.Add("One of these Chests is secretly a Rainbow Chest!");
                            previousStatements++;
                        }
                    }
                    if (chestsInRoom.Count > 1)
                    {
                        int amtOfDetectedGuns = 0;
                        int amtOfDetectedPassives = 0;
                        int amtOfDetectedActives = 0;
                        int weirdNonItems = 0;
                        foreach (Chest chest in chestsInRoom)
                        {
                            int type = GetChestType(chest);
                            //ETGModConsole.Log("Detected Type: " + type);
                            if (type == 0) amtOfDetectedGuns++;
                            if (type == 1) amtOfDetectedPassives++;
                            if (type == 2) amtOfDetectedActives++;
                            if (type == -1) weirdNonItems++;
                        }

                        List<string> detectedItemCounts = new List<string>();

                        //Count guns
                        if (amtOfDetectedGuns > 1) detectedItemCounts.Add(string.Format("{0} guns", amtOfDetectedGuns));
                        else if (amtOfDetectedGuns > 0) detectedItemCounts.Add("1 gun");

                        //Count passives
                        if (amtOfDetectedPassives > 1) detectedItemCounts.Add(string.Format("{0} passive items", amtOfDetectedPassives));
                        else if (amtOfDetectedPassives > 0) detectedItemCounts.Add("1 passive item");

                        //Count actives
                        if (amtOfDetectedActives > 1) detectedItemCounts.Add(string.Format("{0} active items", amtOfDetectedActives));
                        else if (amtOfDetectedActives > 0) detectedItemCounts.Add("1 active item");

                        //Other shit
                        if (weirdNonItems > 1) detectedItemCounts.Add(string.Format("{0} pickups, I think.", weirdNonItems));
                        else if (weirdNonItems > 0) detectedItemCounts.Add("1 pickup, I think.");

                        //ETGModConsole.Log("Full Detected Item Counts Dialogue Strings!");
                        foreach (string itemCountsDiag in detectedItemCounts)
                        {
                            ETGModConsole.Log(itemCountsDiag);
                        }

                        string detectedShitDialogue = "";
                        if (detectedItemCounts.Count > 0)
                        {
                            for (int i = 0; i < detectedItemCounts.Count; i++)
                            {
                                //ETGModConsole.Log("Iteration: " + i);
                                //ETGModConsole.Log("Index: " + (i - 1));
                                string prefix = "";
                                if (i == (detectedItemCounts.Count - 1)) prefix = ", and ";
                                else if (i != 0) prefix = ", ";
                                //ETGModConsole.Log("Prefix: " + prefix);
                                if (detectedItemCounts[i] != null) detectedShitDialogue += (prefix + detectedItemCounts[i]);
                                //ETGModConsole.Log("Detected Shit Proceedural: " + detectedShitDialogue);

                            }

                            string finalComment = string.Format("Hmm.. out of these chests I'm sensing {0}.", detectedShitDialogue);
                            dialoguesToSay.Add(finalComment);
                        }
                        else
                        {
                            dialoguesToSay.Add("Oh dear... I think I have experienced a bug while counting items!");
                        }
                    }
                    else
                    {
                        //ETGModConsole.Log("There was but one chest");
                        string contentsComment = "I think I may have glitched out...";
                        int type = GetChestType(chestsInRoom[0]);
                        if (type == 0)
                        {
                            contentsComment = "That chest definitely has a gun in it!";
                        }
                        else if (type == 1)
                        {
                            contentsComment = "That chest contains a passive item, I think.";
                        }
                        else if (type == 2)
                        {
                            contentsComment = "That chest contains an active item!";
                        }
                        else if (type == -1)
                        {
                            contentsComment = "That chest doesn't seem to contain... any items at all?";
                        }
                        else if (type == -2 && !GameStatsManager.Instance.IsRainbowRun)
                        {
                            contentsComment = "Oooh! A Rainbow Chest! Lucky you!";
                        }
                        if (previousStatements > 0)
                        {
                            contentsComment = "Also, " + contentsComment;
                        }
                        dialoguesToSay.Add(contentsComment);
                        previousStatements++;
                    }
                }

                foreach (AIActor enemy in StaticReferenceManager.AllEnemies)
                {
                    if (enemy.EnemyGuid == "479556d05c7c44f3b6abb3b2067fc778")
                    {
                        if (enemy.specRigidbody && enemy.Position.GetAbsoluteRoom() == room)
                        {
                            if (enemy.GetComponent<WallMimicController>() != null)
                            {
                                bool isHidden = OMITBReflectionHelpers.ReflectGetField<bool>(typeof(WallMimicController), "m_isHidden", enemy.GetComponent<WallMimicController>());
                                if (isHidden)
                                {
                                    dialoguesToSay.Add("The walls in here look hungry...");
                                }
                            }
                        }
                    }
                }

                foreach (RoomHandler adjacentRoom in room.connectedRooms)
                {

                    if (adjacentRoom.area.PrototypeRoomCategory == PrototypeDungeonRoom.RoomCategory.BOSS && !adjacentRoom.hasEverBeenVisited)
                    {
                        List<string> Bossnames = new List<string>();
                        foreach (AIActor roomEnemy in adjacentRoom.GetActiveEnemies(RoomHandler.ActiveEnemyType.All))
                        {
                            if (roomEnemy.healthHaver && roomEnemy.healthHaver.IsBoss)
                            {
                                string bossName = roomEnemy.GetActorName();
                                if (!string.IsNullOrEmpty(roomEnemy.healthHaver.overrideBossName))
                                {
                                    bossName = StringTableManager.GetEnemiesString(roomEnemy.healthHaver.overrideBossName, -1);
                                }
                                Bossnames.Add(bossName);
                            }
                        }
                        if (Bossnames.Count > 0)
                        {
                            dialoguesToSay.Add(string.Format("Looks like the {0}... good luck.", BraveUtility.RandomElement(Bossnames)));
                        }
                        else
                        {
                            dialoguesToSay.Add("There's a boss nearby...  but I can't tell what it is?");
                        }
                    }
                    else if (adjacentRoom.area.PrototypeRoomCategory == PrototypeDungeonRoom.RoomCategory.SECRET && !adjacentRoom.hasEverBeenVisited)
                    {
                        dialoguesToSay.Add("The walls in here look suspicious.");
                    }
                    else if (adjacentRoom.area.PrototypeRoomCategory == PrototypeDungeonRoom.RoomCategory.REWARD && !adjacentRoom.hasEverBeenVisited)
                    {
                        dialoguesToSay.Add("I think I can sense some loot around here.");
                    }
                    if (adjacentRoom.area.PrototypeRoomCategory != PrototypeDungeonRoom.RoomCategory.SECRET)
                    {
                        Minimap.Instance.RevealMinimapRoom(adjacentRoom, true, true, false);
                    }
                }

                if (dialoguesToSay.Count > 0)
                {
                    StartCoroutine(Say(dialoguesToSay));
                }
            }
            public void ReactToSpawnedPedestal(RewardPedestal pedestal)
            {
                if (pedestal.IsMimic)
                {
                    TextBubble.DoAmbientTalk(base.transform, new Vector3(0.5f, 2, 0), "Be careful... that pedestal's breathing!", 2.5f);
                }
            }
            public void ReactToRuntimeSpawnedChest(Chest chest)
            {
                string contentsComment = null;
                int type = GetChestType(chest);
                if (type == 0)
                {
                    contentsComment = "That chest definitely has a gun in it!";
                }
                else if (type == 1)
                {
                    contentsComment = "That chest contains a passive item, I think.";
                }
                else if (type == 2)
                {
                    contentsComment = "That chest contains an active item!";
                }
                else if (type == -1)
                {
                    contentsComment = "That chest doesn't seem to contain... any items at all?";
                }
                else if (type == -2 && !GameStatsManager.Instance.IsRainbowRun)
                {
                    contentsComment = "Oooh! A Rainbow Chest! Lucky you!";
                }
                if (!string.IsNullOrEmpty(contentsComment)) TextBubble.DoAmbientTalk(base.transform, new Vector3(0.5f, 2, 0), contentsComment, 1.5f);
            }
            private int GetChestType(Chest chest)
            {
                if (chest.IsRainbowChest)
                {
                    return -2;
                }
                var contents = chest.PredictContents(Owner);
                foreach (var item in contents)
                {
                    if (item is Gun) return 0;
                    if (item is PlayerItem) return 2;
                    if (item is PassiveItem) return 1;
                }
                return -1;
            }
            
            private void HandleSynergyDialogue(bool hasSynergy)
            {
                if (hasSynergy)
                {
                    TextBubble.DoAmbientTalk(base.transform, new Vector3(0.5f, 2, 0), "Ooh! That tingles!", 2f);
                }
                else
                {
                    TextBubble.DoAmbientTalk(base.transform, new Vector3(0.5f, 2, 0), "Aww, okay then", 2f);
                }
            }
            private IEnumerator Say(List<string> text)
            {
                foreach (string text2 in text)
                {
                    TextBubble.DoAmbientTalk(base.transform, new Vector3(0.5f, 2, 0), text2, 2f);
                    yield return new WaitForSeconds(2f);
                }
                yield break;
            }
            private AIActor self;
            public PlayerController Owner;
            private RoomHandler lastCheckedRoom;
        }

        public static GameObject prefab;
        private static readonly string guid = "scrollofexactknowledge817498687264735345";
    }
}
