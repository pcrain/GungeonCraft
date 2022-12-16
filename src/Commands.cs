using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Gungeon;
using Dungeonator;
using SaveAPI;
using System.Collections;

using ItemAPI;

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
                    PickupObjectDatabase.GetById(IDs.Pickups["deadline"]).gameObject,
                    GameManager.Instance.PrimaryPlayer.CenterPosition,
                    Vector2.zero,
                    0);
                // ETGModConsole.Log("<size=100><color=#ff0000ff>Please specify a command. Type 'nn help' for a list of commands.</color></size>", false);
            });
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
                GameObject bombyboi = SpawnObjectManager.SpawnObject(Bombo.npcobj,v3);
                GameObject bombyPos = new GameObject("ItemPoint3");
                    bombyPos.transform.parent = bombyboi.transform;
                    bombyPos.transform.position = v3 + new Vector3(2.625f, 1f, 1f);
                GameObject bombyItem = new GameObject("Fake shop item test");
                    bombyItem.transform.parent        = bombyPos.transform;
                    bombyItem.transform.localPosition = Vector3.zero;
                    bombyItem.transform.position      = Vector3.zero;
                GameObject bombyPickup = PickupObjectDatabase.GetById(IDs.Pickups["natasha"]).gameObject;
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
}

