using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

using UnityEngine;

// using SaveAPI;

namespace CwaffingTheGungy
{
    public class Commands
    {
        public static void Init()
        {
            // Base command for doing whatever I'm testing at the moment
            ETGModConsole.Commands.AddGroup("gg", delegate (string[] args)
            {
                LootEngine.SpawnItem(
                    // PickupObjectDatabase.GetById(IDs.Actives["borrowed_time"]).gameObject,
                    // PickupObjectDatabase.GetById(IDs.Passives["shine"]).gameObject,
                    // PickupObjectDatabase.GetById(IDs.Guns["ki_blast"]).gameObject,
                    // PickupObjectDatabase.GetById(IDs.Pickups["superstitious"]).gameObject,
                    // PickupObjectDatabase.GetById(IDs.Pickups["deadline"]).gameObject,
                    // PickupObjectDatabase.GetById(IDs.Passives["hld"]).gameObject,
                    // PickupObjectDatabase.GetById(IDs.Passives["siphon"]).gameObject,
                    // PickupObjectDatabase.GetById(IDs.Actives["bullet_that_can_kill_the_future"]).gameObject,
                    // PickupObjectDatabase.GetById(IDs.Passives["zoolanders_diary"]).gameObject,
                    // PickupObjectDatabase.GetById(IDs.Passives["rat_poison"]).gameObject,
                    // PickupObjectDatabase.GetById(IDs.Passives["johns_wick"]).gameObject,
                    // PickupObjectDatabase.GetById(IDs.Guns["testlightning"]).gameObject,
                    // PickupObjectDatabase.GetById(IDs.Guns["b_b_gun"]).gameObject,
                    // PickupObjectDatabase.GetById(IDs.Guns["test_gun"]).gameObject,
                    // PickupObjectDatabase.GetById(IDs.Passives["gyroscope"]).gameObject,
                    // PickupObjectDatabase.GetById(IDs.Passives["curators_badge"]).gameObject,
                    PickupObjectDatabase.GetById(IDs.Pickups["library_cardtridge"]).gameObject,
                    GameManager.Instance.PrimaryPlayer.CenterPosition,
                    Vector2.zero,
                    0);
                // ETGModConsole.Log("<size=100><color=#ff0000ff>Please specify a command. Type 'nn help' for a list of commands.</color></size>", false);
            });
            // Boss time o.o
            // ETGModConsole.Commands.AddGroup("bb", delegate (string[] args)
            // {
            //   PlayerController player = GameManager.Instance.PrimaryPlayer;
            //   AIActor orLoadByGuid = EnemyDatabase.GetOrLoadByGuid(RoomMimic.guid);
            //   AIActor aiactor = AIActor.Spawn(orLoadByGuid, player.gameObject.transform.position, player.gameObject.transform.position.GetAbsoluteRoom(), true, AIActor.AwakenAnimationType.Default, true);
            //   aiactor.GetComponent<GenericIntroDoer>().TriggerSequence(player);
            // });
            // Another base command for loading my latest debug flow
            ETGModConsole.Commands.AddGroup("ff", delegate (string[] args)
            {
                FlowCommands.LoadFlowFunction(new string[]{"simplest"});
            });
            // Another base command for testing npc shenanigans
            ETGModConsole.Commands.AddGroup("tt", delegate (string[] args)
            {
                var bundle = ResourceManager.LoadAssetBundle("shared_auto_001");
                // foreach (string s in bundle.GetAllAssetNames())
                //     ETGModConsole.Log(s);
                GameObject gk = bundle.LoadAsset<GameObject>("npc_gunslingking");
                bundle = null;
                // return;
                PlayerController p1 = GameManager.Instance.PrimaryPlayer;
                // TalkDoer td = new TalkDoer();
                // td.BeginConversation(p1);
                // GameObject go = ItsDaFuckinShopApi.SetUpGenericNpc(
                //     "Boomhildr",
                //      "cg",
                //      new List<string>() {
                //         "CwaffingTheGungy/Resources/NPCSprites/Boomhildr/boomhildr_idle_001",
                //         "CwaffingTheGungy/Resources/NPCSprites/Boomhildr/boomhildr_idle_002",
                //         "CwaffingTheGungy/Resources/NPCSprites/Boomhildr/boomhildr_idle_003",
                //         "CwaffingTheGungy/Resources/NPCSprites/Boomhildr/boomhildr_idle_004",
                //         "CwaffingTheGungy/Resources/NPCSprites/Boomhildr/boomhildr_idle_005",
                //      },
                //      new List<string>()
                //      {
                //     "CwaffingTheGungy/Resources/NPCSprites/Boomhildr/boomhildr_talk_001",
                //     "CwaffingTheGungy/Resources/NPCSprites/Boomhildr/boomhildr_talk_002",
                //      },
                //      7,
                //      new Vector3(0.5f, 4, 0)
                // );
                // UnityEngine.Object.Instantiate(go,p1.sprite.WorldCenter+(new Vector2(2,2)),Quaternion.identity);
                // GameObject npc = UnityEngine.Object.Instantiate(go,p1.sprite.WorldCenter,Quaternion.identity);

                // GameObject npc = UnityEngine.Object.Instantiate(Boomhildr.boomhildrNPCObj,p1.sprite.WorldCenter,Quaternion.identity);
                // TalkDoerLite td = npc.GetComponent<TalkDoerLite>();
                // td.ForceTimedSpeech(":D");
                // Dissect.DumpFieldsAndProperties<TalkDoerLite>(td);
                // ScriptableObject.CreateInstance<DungeonPlaceable>()

                // GameObject npc = UnityEngine.Object.Instantiate(gk,p1.sprite.WorldCenter,Quaternion.identity);
                // // Dissect.DumpFieldsAndProperties<GameObject>(npc);
                // // Dissect.DumpComponents(npc);
                // // Dissect.DumpFieldsAndProperties<TalkDoerLite>(npc.GetComponent<TalkDoerLite>());
                // TalkDoerLite td  = npc.GetComponent<TalkDoerLite>();
                // td.ShowText(p1.sprite.WorldCenter,td.sprite.transform,1f,"testaroo");

                // SpawnObjectManager.SpawnObject(gk,p1.CurrentRoom.GetRandomVisibleClearSpot(1, 1).ToVector3());
                // IPlayerInteractable ia;
                // ia.Interact()
                Vector3 v3 = p1.CurrentRoom.GetRandomVisibleClearSpot(1, 1).ToVector3();

                foreach (AdvancedShrineController a in StaticReferenceManager.AllAdvancedShrineControllers)
                {
                    if (a.IsLegendaryHeroShrine && a.transform.position.GetAbsoluteRoom() == p1.CurrentRoom)
                    {
                        ETGModConsole.Log("found it!");
                        v3 = a.transform.position + (new Vector2(a.sprite.GetCurrentSpriteDef().position3.x/2,-8)).ToVector3YUp(0);
                    }
                }

                GameObject bombyboi = SpawnObjectManager.SpawnObject(Bombo.npcobj,v3);
                GameObject bombyPos = new GameObject("ItemPoint3");
                    bombyPos.transform.parent = bombyboi.transform;
                    bombyPos.transform.position = v3 + new Vector3(2.625f, 1f, 1f);
                GameObject bombyItem = new GameObject("Fake shop item test");
                    bombyItem.transform.parent        = bombyPos.transform;
                    bombyItem.transform.localPosition = Vector3.zero;
                    bombyItem.transform.position      = Vector3.zero;
                GameObject bombyPickup;
                    if (UnityEngine.Random.Range(0,2) == 0)
                        bombyPickup = LootEngine.GetItemOfTypeAndQuality<PickupObject>(
                                        PickupObject.ItemQuality.S, GameManager.Instance.RewardManager.GunsLootTable, false).gameObject;
                    else
                        bombyPickup = LootEngine.GetItemOfTypeAndQuality<PickupObject>(
                                        PickupObject.ItemQuality.S, GameManager.Instance.RewardManager.ItemsLootTable, false).gameObject;
                    PickupObject po = bombyPickup.GetComponent<PickupObject>();
                FakeShopItem fsi = bombyItem.AddComponent<FakeShopItem>();
                    if (!p1.CurrentRoom.IsRegistered(fsi))
                        p1.CurrentRoom.RegisterInteractable(fsi);
                    fsi.purchasingScript = bombyboi.GetComponent<Bombo>().StrikeADealScript;
                    fsi.Initialize(po);

                // gk.GetComponent<TalkDoerLite>().InstantiateObject(p1.CurrentRoom,p1.CurrentRoom.GetRandomVisibleClearSpot(1, 1));
            });
        }
    }

    // A slightly rewritten version of old Anywhere Mod by stellatedHexahedron
    //   blatantly stolen from Apache by me o:
    public class FlowCommands {
        private static List<string> knownFlows = new List<string>();
        private static List<string> knownTilesets = new List<string>();
        private static List<string> knownScenes = new List<string>();

        private static string[] ReturnMatchesFromList(string matchThis, List<string> inThis) {
            List<string> result = new List<string>();
            string matchString = matchThis.ToLower();
            foreach (string text in inThis) {
                string textString = text.ToLower();
                bool flag = StringAutocompletionExtensions.AutocompletionMatch(textString, matchString);
                if (flag) { result.Add(textString); }
            }
            return result.ToArray();
        }

        public static void Install() {
            if (CwaffDungeonFlow.KnownFlows != null && CwaffDungeonFlow.KnownFlows.Count > 0) {
                foreach (DungeonFlow flow in CwaffDungeonFlow.KnownFlows) {
                    if (flow.name != null && flow.name != string.Empty) { knownFlows.Add(flow.name.ToLower()); }
                }
            }

            foreach (GameLevelDefinition dungeonFloors in GameManager.Instance.dungeonFloors) {
                if (dungeonFloors.dungeonPrefabPath != null && dungeonFloors.dungeonPrefabPath != string.Empty) {
                    knownTilesets.Add(dungeonFloors.dungeonPrefabPath.ToLower());
                }
                if (dungeonFloors.dungeonSceneName != null && dungeonFloors.dungeonSceneName != string.Empty) {
                    knownScenes.Add(dungeonFloors.dungeonSceneName.ToLower());
                }
            }

            foreach (GameLevelDefinition customFloors in GameManager.Instance.customFloors) {
                if (customFloors.dungeonPrefabPath != null && customFloors.dungeonPrefabPath != string.Empty) {
                    knownTilesets.Add(customFloors.dungeonPrefabPath.ToLower());
                }
                if (customFloors.dungeonSceneName != null && customFloors.dungeonSceneName != string.Empty) {
                    knownScenes.Add(customFloors.dungeonSceneName.ToLower());
                }
            }

            ETGModConsole.Commands.AddUnit("load_flow", new Action<string[]>(LoadFlowFunction), new AutocompletionSettings(delegate(int index, string input) {
                if (index == 0) {
                    return ReturnMatchesFromList(input.ToLower(), knownFlows);
                } else if (index == 1) {
                    return ReturnMatchesFromList(input.ToLower(), knownTilesets);
                } else if (index == 2) {
                    return ReturnMatchesFromList(input.ToLower(), knownScenes);
                } else {
                    return new string[0];
                }
            }));
        }

        public static void LoadFlowFunction(string[] args) {
            if (args == null | args.Length == 0 | args[0].ToLower() == "help") {
            ETGModConsole.Log("WARNING: this command can crash gungeon! \nIf the game hangs on loading screen, use console to load a different level!\nUsage: load_flow [FLOW NAME] [TILESET NAME]. [TILESET NAME] is optional. Press tab for a list of each.\nOnce you run the command and you press escape, you should see the loading screen. If nothing happens when you use the command, the flow you tried to load doesn't exist or the path to it needs to be manually specified. Example: \"load_flow NPCParadise\".\nIf it hangs on loading screen then the tileset you tried to use doesn't exist, is no longer functional, or the flow uses rooms that are not compatible with the chosen tileset.\nAlso, you should probably know that if you run this command from the breach, the game never gives you the ability to shoot or use active items, so you should probably start a run first.");
            } else if (args != null && args.Length > 3) {
                ETGModConsole.Log("ERROR: Too many arguments specified! DungoenFlow name, dungoen prefab name, and dungoen scene name are the expected arguments!");
            } else {
                bool tilesetSpecified = args.Length > 1;
                bool sceneSpecified = args.Length > 2;

                if (tilesetSpecified && !knownTilesets.Contains(args[1]) && DungeonDatabase.GetOrLoadByName(args[1])) {
                    knownTilesets.Add(args[1]);
                }

                bool invalidTileset = tilesetSpecified && !knownTilesets.Contains(args[1]);
                string flowName = args[0].Replace('-', ' ');

                if (invalidTileset) {
                    ETGModConsole.Log("Not a valid tileset!");
                } else {
                    try {
                        string LogMessage = ("Attempting to load Dungeon Flow \"" + args[0] + "\"");

                        if (tilesetSpecified) { LogMessage += (" with tileset \"" + args[1] + "\""); }
                        if (sceneSpecified) { LogMessage += (" and scene \"" + args[2] + "\""); }
                        LogMessage += ".";
                        // ETGModConsole.Log("Attempting to load Dungeon Flow \"" + args[0] + (tilesetSpecified ? ("\" with tileset \"" + args[1]) : string.Empty) + "\"" + (sceneSpecified ? ("\" and scene " + args[2]) : string.Empty) + ".");
                        ETGModConsole.Log(LogMessage);
                        if (args.Length == 1) {
                            GameManager.Instance.LoadCustomFlowForDebug(flowName);
                        } else if (args.Length == 2) {
                            string tilesetName = args[1];
                            GameManager.Instance.LoadCustomFlowForDebug(flowName, tilesetName);
                        } else if (args.Length == 3) {
                            string tilesetName = args[1];
                            string sceneName = args[2];
                            GameManager.Instance.LoadCustomFlowForDebug(flowName, tilesetName, sceneName);
                        } else {
                            ETGModConsole.Log("If you're trying to go nowhere, you're succeeding.");
                        }
                    } catch (Exception ex) {
                        ETGModConsole.Log("WHOOPS! Something went wrong! Most likely you tried to load a broken flow, or the tileset is incomplete and doesn't have the right tiles for the flow.");
                        ETGModConsole.Log("In order to get the game back into working order, the mod is now loading NPCParadise, with the castle tileset.");
                        Debug.Log("WHOOPS! Something went wrong! Most likely you tried to load a broken flow, or the tileset is incomplete and doesn't have the right tiles for the flow.");
                        Debug.Log("In order to get the game back into working order, the mod is now loading NPCParadise, with the castle tileset.");
                        Debug.LogException(ex);
                        GameManager.Instance.LoadCustomFlowForDebug("npcparadise", "Base_Castle", "tt_castle");
                    }
                }
            }
        }
    }
}

